using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// <summary>
    /// Scene-level singleton pool manager for the battle UI.
    ///
    /// Owns one <see cref="ObjectPool{T}"/> per <see cref="CardType"/> (Pressure, Rhetoric, Policy,
    /// Status, Curse) so each card type can use a wholly different prefab layout.
    /// Also owns a pool for <see cref="EnemySlotUI"/>.
    ///
    /// Accessible globally via <c>BattlePoolManager.Instance</c> — no injection required.
    /// Survives scene reloads via <c>DontDestroyOnLoad</c>; duplicate instances are auto-destroyed.
    ///
    /// Borrow/return pattern:
    ///   1. <c>RentCard(cardType, parent)</c>  — activates the right button and re-parents it.
    ///   2. Use the button normally.
    ///   3. <c>ReturnCard(btn)</c>             — reads <c>btn.PooledCardType</c>, resets state,
    ///                                           re-parents to this transform, deactivates.
    /// </summary>
    public class BattlePoolManager : Singleton<BattlePoolManager>
    {
        [Header("Card Prefabs — one per CardType")]
        [Tooltip("CardButton prefab used for Pressure cards.")]
        [SerializeField]
        private CardButton _pressureCardPrefab;

        [Tooltip("CardButton prefab used for Rhetoric cards.")]
        [SerializeField]
        private CardButton _rhetoricCardPrefab;

        [Tooltip("CardButton prefab used for Policy cards.")]
        [SerializeField]
        private CardButton _policyCardPrefab;

        [Tooltip("CardButton prefab used for Status cards.")]
        [SerializeField]
        private CardButton _statusCardPrefab;

        [Tooltip("CardButton prefab used for Curse cards.")]
        [SerializeField]
        private CardButton _curseCardPrefab;

        [Tooltip("EnemySlotUI component on the root of the enemy slot prefab.")]
        [SerializeField]
        private EnemySlotUI _enemySlotPrefab;

        [Header("Pool Sizes")]
        [Tooltip("Pre-warmed Pressure card button count (default 15).")]
        [SerializeField]
        private int _pressurePoolSize = 15;

        [Tooltip("Pre-warmed Rhetoric card button count (default 15).")]
        [SerializeField]
        private int _rhetoricPoolSize = 15;

        [Tooltip("Pre-warmed Policy card button count (default 10).")]
        [SerializeField]
        private int _policyPoolSize = 10;

        [Tooltip("Pre-warmed Status card button count (default 8).")]
        [SerializeField]
        private int _statusPoolSize = 8;

        [Tooltip("Pre-warmed Curse card button count (default 8).")]
        [SerializeField]
        private int _cursePoolSize = 8;

        [Tooltip("Pre-warmed enemy slot count. Should equal max enemies per battle (default 5).")]
        [SerializeField]
        private int _enemySlotPoolSize = 5;

        private ObjectPool<CardButton> _pressurePool;
        private ObjectPool<CardButton> _rhetoricPool;
        private ObjectPool<CardButton> _policyPool;
        private ObjectPool<CardButton> _statusPool;
        private ObjectPool<CardButton> _cursePool;
        private ObjectPool<EnemySlotUI> _slotPool;

        protected override void OnAwake()
        {
            if (_pressureCardPrefab != null)
                _pressurePool = new ObjectPool<CardButton>(
                    _pressureCardPrefab,
                    _pressurePoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Pressure card prefab is not assigned — Pressure pool not created."
                );

            if (_rhetoricCardPrefab != null)
                _rhetoricPool = new ObjectPool<CardButton>(
                    _rhetoricCardPrefab,
                    _rhetoricPoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Rhetoric card prefab is not assigned — Rhetoric pool not created."
                );

            if (_policyCardPrefab != null)
                _policyPool = new ObjectPool<CardButton>(
                    _policyCardPrefab,
                    _policyPoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Policy card prefab is not assigned — Policy pool not created."
                );

            if (_statusCardPrefab != null)
                _statusPool = new ObjectPool<CardButton>(
                    _statusCardPrefab,
                    _statusPoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Status card prefab is not assigned — Status pool not created."
                );

            if (_curseCardPrefab != null)
                _cursePool = new ObjectPool<CardButton>(
                    _curseCardPrefab,
                    _cursePoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Curse card prefab is not assigned — Curse pool not created."
                );

            if (_enemySlotPrefab != null)
                _slotPool = new ObjectPool<EnemySlotUI>(
                    _enemySlotPrefab,
                    _enemySlotPoolSize,
                    transform
                );
            else
                Debug.LogWarning(
                    "[BattlePoolManager] Enemy slot prefab is not assigned — slot pool not created."
                );
        }

        #region Card Buttons
        /// <summary>
        /// Rents a <see cref="CardButton"/> of the matching <paramref name="cardType"/> from the
        /// appropriate pool and re-parents it under <paramref name="parent"/>.
        /// Returns <c>null</c> if the relevant pool was not initialized.
        /// </summary>
        public CardButton RentCard(CardType cardType, Transform parent)
        {
            ObjectPool<CardButton> pool = PoolForType(cardType);
            if (pool == null)
                return null;
            CardButton btn = pool.Get();
            btn.transform.SetParent(parent, false);
            return btn;
        }

        /// <summary>
        /// Returns a <see cref="CardButton"/> to its correct pool.
        /// Reads <see cref="CardButton.PooledCardType"/> — set by <c>Initialize</c> — so the
        /// caller never needs to track which pool the button came from.
        /// Also resets <see cref="CanvasGroup"/> interaction flags before deactivating.
        /// </summary>
        public void ReturnCard(CardButton btn)
        {
            if (btn == null)
                return;

            ObjectPool<CardButton> pool = PoolForType(btn.PooledCardType);
            if (pool == null)
                return;

            // Restore interaction flags that display-only panels may have disabled.
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
                cg.alpha = 1f;
            }

            btn.transform.localScale = Vector3.one;
            btn.transform.SetParent(transform, false);
            pool.Return(btn);
        }

        #endregion

        #region Enemy Slots
        /// <summary>
        /// Rents an <see cref="EnemySlotUI"/> from the pool and re-parents it under
        /// <paramref name="parent"/>. Returns <c>null</c> if the pool was not initialized.
        /// </summary>
        public EnemySlotUI RentSlot(Transform parent)
        {
            if (_slotPool == null)
                return null;
            EnemySlotUI slot = _slotPool.Get();
            slot.transform.SetParent(parent, false);
            return slot;
        }

        /// <summary>
        /// Returns an <see cref="EnemySlotUI"/> to the pool.
        /// Clears the static <see cref="EnemySlotUI.TargetedSlot"/> reference if this
        /// slot was being targeted, then re-parents it under this transform.
        /// </summary>
        public void ReturnSlot(EnemySlotUI slot)
        {
            if (slot == null || _slotPool == null)
                return;

            // Clear static targeting reference so the next battle starts clean.
            if (EnemySlotUI.TargetedSlot == slot)
                EnemySlotUI.ClearTargetedSlot();

            slot.transform.SetParent(transform, false);
            _slotPool.Return(slot);
        }

        #endregion

        #region Helpers
        private ObjectPool<CardButton> PoolForType(CardType cardType)
        {
            return cardType switch
            {
                CardType.Rhetoric => _rhetoricPool,
                CardType.Policy => _policyPool,
                CardType.Status => _statusPool,
                CardType.Curse => _cursePool,
                _ => _pressurePool, // Pressure fallback
            };
        }
    }
}
        #endregion
