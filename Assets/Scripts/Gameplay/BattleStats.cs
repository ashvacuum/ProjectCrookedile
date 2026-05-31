using System;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.Gameplay
{
    /// <summary>
    /// Tracks per-combatant battle stats.
    /// Opinion-meter model — there is no HP. Session-level buffers (Support, Denial) live on BattleManager.
    ///   Hostility  — enemy-only bidirectional axis: positive = hostile, negative = receptive.
    ///   Action Points — player-only energy to play cards.
    ///   Hardened   — flag: ReduceHostility is a no-op (won't listen to reason).
    ///   Fanatic    — flag: GainHostility is a no-op (can't be riled up).
    /// </summary>
    [Serializable]
    public class BattleStats
    {
        [Header("Hostility")]
        [Tooltip("Bidirectional hostility axis. >0 = hostile, <0 = receptive, 0 = neutral.")]
        [SerializeField]
        private int _currentHostility;

        private int _maxHostility = 10;
        private int _minHostility = -10;

        [Header("Turn Resources")]
        [SerializeField]
        private int _currentActionPoints;

        [SerializeField]
        private int _maxActionPoints = 3;

        [SerializeField]
        private int _actionPointsNextTurn;

        private bool _isPlayer;

        #region Properties

        /// <summary>ReduceHostility is a no-op while true (won't listen to reason).</summary>
        public bool IsHardened { get; private set; }

        /// <summary>GainHostility is a no-op while true (can't be riled up).</summary>
        public bool IsFanatic { get; private set; }
        public int CurrentHostility => _currentHostility;
        public int CurrentActionPoints => _currentActionPoints;
        public int MaxActionPoints => _maxActionPoints;
        public int ActionPointsNextTurn => _actionPointsNextTurn;

        /// <summary>
        /// Enemies are never "defeated" in the resolve sense — they persist until they leave
        /// the debate or the opinion meter resolves the battle.
        /// Always false; retained so existing guard code compiles without changes.
        /// </summary>
        public bool IsDefeated => false;

        /// <summary>True when the enemy is actively hostile (positive hostility).</summary>
        public bool IsHostile => _currentHostility > 0;

        /// <summary>True when the enemy is receptive / de-escalated (negative hostility).</summary>
        public bool IsReceptive => _currentHostility < 0;

        /// <summary>
        /// Hostility pressure multiplier for attacks on the opinion meter.
        /// Formula: max(0.1, 1.0 + Hostility × 0.5)
        /// </summary>
        public float HostilityDamageMultiplier => Mathf.Max(0.1f, 1.0f + _currentHostility * 0.5f);

        #endregion

        #region Constructors

        public BattleStats() { }

        /// <summary>Creates stats for a player combatant.</summary>
        public BattleStats(int maxActionPoints, bool isPlayer = true)
        {
            _maxActionPoints = maxActionPoints;
            _currentActionPoints = maxActionPoints;
            _currentHostility = 0;
            _actionPointsNextTurn = 0;
            _isPlayer = isPlayer;
        }

        #endregion

        #region Hardened / Fanatic

        /// <summary>Sets whether this combatant ignores hostility reductions (Hardened status).</summary>
        public void SetHardened(bool value) => IsHardened = value;

        /// <summary>Sets whether this combatant ignores hostility gains (Fanatic status).</summary>
        public void SetFanatic(bool value) => IsFanatic = value;

        #endregion

        #region Hostility Management

        /// <summary>Sets per-enemy hostility clamps. Called by EnemyController after construction.</summary>
        public void SetHostilityLimits(int min, int max)
        {
            _minHostility = min;
            _maxHostility = max;
        }

        /// <summary>Shifts hostility upward (more hostile). No-op when Fanatic.</summary>
        public void GainHostility(int amount)
        {
            if (IsFanatic)
                return;
            int old = _currentHostility;
            _currentHostility = Mathf.Min(_maxHostility, _currentHostility + amount);
            EventBus.Publish(
                new HostilityChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentHostility,
                    IsPlayer = _isPlayer,
                }
            );
            if (old < _maxHostility && _currentHostility == _maxHostility)
                EventBus.Publish(new EnemyMaxedHostilityEvent());
        }

        /// <summary>Shifts hostility downward (more receptive). No-op when Hardened.</summary>
        public int ReduceHostility(int amount)
        {
            if (IsHardened)
                return 0;
            int old = _currentHostility;
            _currentHostility = Mathf.Max(_minHostility, _currentHostility - amount);
            int actual = old - _currentHostility;
            EventBus.Publish(
                new HostilityChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentHostility,
                    IsPlayer = _isPlayer,
                }
            );
            return actual;
        }

        /// <summary>Sets hostility to an exact value (used for enemy initialisation).</summary>
        public void SetHostility(int value)
        {
            int old = _currentHostility;
            _currentHostility = Mathf.Clamp(value, _minHostility, _maxHostility);
            if (old != _currentHostility)
                EventBus.Publish(
                    new HostilityChangedEvent
                    {
                        OldValue = old,
                        NewValue = _currentHostility,
                        IsPlayer = _isPlayer,
                    }
                );
        }

        #endregion

        #region Action Points Management

        /// <summary>Spends Action Points to play a card.</summary>
        public bool SpendActionPoints(int cost)
        {
            if (_currentActionPoints < cost)
                return false;
            int old = _currentActionPoints;
            _currentActionPoints -= cost;
            EventBus.Publish(
                new ActionPointsChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentActionPoints,
                    IsPlayer = _isPlayer,
                }
            );
            return true;
        }

        /// <summary>Gains extra Action Points this turn.</summary>
        public void GainActionPoints(int amount)
        {
            int old = _currentActionPoints;
            _currentActionPoints = Mathf.Max(0, _currentActionPoints + amount);
            EventBus.Publish(
                new ActionPointsChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentActionPoints,
                    IsPlayer = _isPlayer,
                }
            );
        }

        /// <summary>Banks Action Points to gain at the start of next turn.</summary>
        public void GainActionPointsNextTurn(int amount) => _actionPointsNextTurn += amount;

        /// <summary>Refreshes Action Points to max at the start of a new turn.</summary>
        public void RefreshActionPoints()
        {
            int old = _currentActionPoints;
            _currentActionPoints = Mathf.Max(0, _maxActionPoints + _actionPointsNextTurn);
            _actionPointsNextTurn = 0;
            EventBus.Publish(
                new ActionPointsChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentActionPoints,
                    IsPlayer = _isPlayer,
                }
            );
        }

        #endregion

        #region Turn Management

        /// <summary>Called at the start of a turn to refresh resources.</summary>
        public void StartTurn()
        {
            if (_maxActionPoints > 0)
                RefreshActionPoints();
        }

        /// <summary>Called at the end of a turn.</summary>
        public void EndTurn() { }

        #endregion

        #region Utility

        public string GetStatusString() =>
            $"Hostility: {_currentHostility} ({HostilityDamageMultiplier:F2}x) | "
            + $"AP: {_currentActionPoints}/{_maxActionPoints}";

        #endregion
    }
}
