using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns the enemy row: slot instantiation/pooling and every per-slot event reaction
    /// (intent badges, hostility pulses, defeat teardown, summon spawns, acting signals,
    /// status refreshes, turncoat pulses). Extracted from BattleUI.
    ///
    /// Self-subscribes to enemy events; BattleUI only drives the coalesced
    /// <see cref="RefreshAll"/> (stats + focus highlight) and reads
    /// <see cref="GetSlotTransform"/> for VFX aiming.
    /// </summary>
    public class EnemyRowPanel : MonoBehaviour
    {
        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into.")]
        [SerializeField]
        private Transform enemySlotContainer;

        [Tooltip("Prefab with an EnemySlotUI component — instantiated once per enemy.")]
        [SerializeField]
        private GameObject enemySlotPrefab;

        private BattleManager _bm;
        private readonly List<EnemySlotUI> _slots = new List<EnemySlotUI>();

        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        /// <summary>Supplies the battle context. Called by BattleUI.Initialize.</summary>
        public void Bind(BattleManager bm) => _bm = bm;

        #region Event subscription

        private void OnEnable()
        {
            Sub<BattleStartedEvent>(_ => BuildSlots());
            Sub<EnemyIntentDeclaredEvent>(OnIntentDeclared);
            Sub<HostilityChangedEvent>(OnHostilityChanged);
            Sub<EnemyDefeatedEvent>(OnEnemyDefeated);
            Sub<EnemySummonedEvent>(OnEnemySummoned);
            Sub<EnemyActingEvent>(OnEnemyActing);
            Sub<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            Sub<EnemyTurncoatEvent>(OnEnemyTurncoat);
        }

        private void OnDisable()
        {
            foreach (var unsub in _eventUnsubscribers)
                unsub();
            _eventUnsubscribers.Clear();
        }

        private void Sub<T>(System.Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _eventUnsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void OnIntentDeclared(EnemyIntentDeclaredEvent evt)
        {
            if (evt.EnemyIndex < _slots.Count)
                _slots[evt.EnemyIndex]?.UpdateIntent(evt.Move);
        }

        private void OnHostilityChanged(HostilityChangedEvent evt)
        {
            // Player hostility (index -1) has no slot; only refresh real enemy slots.
            if (evt.EnemyIndex < 0 || evt.EnemyIndex >= _slots.Count)
                return;
            _slots[evt.EnemyIndex]?.Refresh();
            _slots[evt.EnemyIndex]?.PulseHostility();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            if (evt.EnemyIndex >= _slots.Count)
                return;

            var slot = _slots[evt.EnemyIndex];
            if (slot == null)
                return;

            if (BattlePoolManager.Instance != null)
                BattlePoolManager.Instance.ReturnSlot(slot);
            else
                Destroy(slot.gameObject);

            _slots[evt.EnemyIndex] = null;
        }

        private void OnEnemySummoned(EnemySummonedEvent evt) => AddSlot(evt.EnemyIndex);

        private void OnEnemyActing(EnemyActingEvent evt)
        {
            if (evt.EnemyIndex >= _slots.Count)
                return;
            _slots[evt.EnemyIndex]?.PulseIntent();
            _slots[evt.EnemyIndex]?.ClearIntent();
        }

        private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
        {
            // Refresh the specific enemy slot so its status display reflects the change.
            if (evt.IsToPlayer || evt.EnemyIndex < 0 || evt.EnemyIndex >= _slots.Count)
                return;

            _slots[evt.EnemyIndex]?.Refresh();

            // Stunning an enemy neutralises its turn — clear the intent immediately so the
            // player sees the threat is handled rather than a move that will never fire.
            if (evt.Behavior is StunnedStatus && evt.Stacks > 0)
                _slots[evt.EnemyIndex]?.ClearIntent();
        }

        private void OnEnemyTurncoat(EnemyTurncoatEvent evt)
        {
            if (evt.EnemyIndex < 0 || evt.EnemyIndex >= _slots.Count)
                return;
            _slots[evt.EnemyIndex]?.Refresh();
            _slots[evt.EnemyIndex]?.PulseHostility();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Refreshes every slot and applies the focus highlight. Called from BattleUI's
        /// coalesced stats refresh so multiple events in one resolution repaint once.
        /// </summary>
        public void RefreshAll(int focusedEnemyIndex)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i]?.Refresh();
                _slots[i]?.SetSelected(i == focusedEnemyIndex);
            }
        }

        /// <summary>
        /// RectTransform of the slot at <paramref name="index"/>, or null when out of range
        /// or torn down. Used by BattleFeedbackController to aim VFX at enemy panels.
        /// </summary>
        public RectTransform GetSlotTransform(int index)
        {
            if (index < 0 || index >= _slots.Count || _slots[index] == null)
                return null;
            return _slots[index].GetComponent<RectTransform>();
        }

        #endregion

        #region Slot lifecycle

        private void BuildSlots()
        {
            // Return all current slots to the pool (or destroy if no pool).
            foreach (var slot in _slots)
            {
                if (slot == null)
                    continue;
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnSlot(slot);
                else
                    Destroy(slot.gameObject);
            }

            _slots.Clear();

            if (enemySlotContainer == null || _bm == null)
                return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null)
                return;

            for (int i = 0; i < _bm.Enemies.Count; i++)
                SpawnSlot(i);
        }

        private void AddSlot(int index)
        {
            if (enemySlotContainer == null || _bm == null)
                return;
            if (BattlePoolManager.Instance == null && enemySlotPrefab == null)
                return;
            if (index >= _bm.Enemies.Count)
                return;
            SpawnSlot(index);
        }

        private void SpawnSlot(int index)
        {
            EnemySlotUI slot =
                BattlePoolManager.Instance != null
                    ? BattlePoolManager.Instance.RentSlot(enemySlotContainer)
                    : Instantiate(enemySlotPrefab, enemySlotContainer).GetComponent<EnemySlotUI>();

            if (slot == null)
                return;

            slot.Initialize(index, _bm, _bm.PlayerOrigin, _bm.Enemies[index].EnemyData);
            _slots.Add(slot);
        }

        #endregion
    }
}
