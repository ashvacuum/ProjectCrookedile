using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
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
    public class EnemyRowPanel : BattlePanel
    {
        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into.")]
        [SerializeField]
        private Transform enemySlotContainer;
        private readonly List<EnemySlotUI> _slots = new List<EnemySlotUI>();

        #region Event subscription

        private void OnEnable()
        {
            On<BattleStartedEvent>(_ => BuildSlots());
            On<EnemyIntentDeclaredEvent>(OnIntentDeclared);
            On<HostilityChangedEvent>(OnHostilityChanged);
            On<EnemyDefeatedEvent>(OnEnemyDefeated);
            On<EnemySummonedEvent>(OnEnemySummoned);
            On<EnemyActingEvent>(OnEnemyActing);
            On<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            On<EnemyTurncoatEvent>(OnEnemyTurncoat);
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
            // Return all current slots to the pool.
            foreach (var slot in _slots)
                if (slot != null)
                    BattlePoolManager.Instance?.ReturnSlot(slot);

            _slots.Clear();

            if (enemySlotContainer == null || Battle == null)
                return;

            for (int i = 0; i < Battle.Enemies.Count; i++)
                SpawnSlot(i);
        }

        private void AddSlot(int index)
        {
            if (enemySlotContainer == null || Battle == null)
                return;
            if (index >= Battle.Enemies.Count)
                return;
            SpawnSlot(index);
        }

        private void SpawnSlot(int index)
        {
            EnemySlotUI slot = BattlePoolManager.Instance?.RentSlot(enemySlotContainer);
            if (slot == null)
            {
                GameLogger.LogWarning(
                    "EnemyRow",
                    "BattlePoolManager missing — the pool is REQUIRED, enemy slot not spawned"
                );
                return;
            }

            slot.Initialize(index, Battle, Battle.Enemies[index].EnemyData);
            _slots.Add(slot);
        }

        #endregion
    }
}
