using System;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.Gameplay
{
    /// <summary>
    /// Tracks all battle-specific stats for a single combatant (player or opponent).
    /// Opinion-meter model — there is no HP. Resources:
    ///   Composure  — temporary shield that absorbs opinion-meter changes before they land.
    ///   Hostility  — bidirectional axis: positive = hostile (attacks harder), negative = receptive (may skip/shift).
    ///   Action Points — energy to play cards (player only; enemies use 0).
    /// </summary>
    [Serializable]
    public class BattleStats
    {
        [Header("Composure")]
        [Tooltip(
            "Temporary shield. Absorbs incoming pressure before it touches the Opinion Meter."
        )]
        [SerializeField]
        private int _currentComposure;

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

        public int CurrentComposure => _currentComposure;
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
            _currentComposure = 0;
            _currentHostility = 0;
            _actionPointsNextTurn = 0;
            _isPlayer = isPlayer;
        }

        #endregion

        #region Composure Management

        /// <summary>
        /// Absorbs incoming pressure through the composure shield.
        /// Returns the remainder that was NOT absorbed — this is what affects the opinion meter.
        /// </summary>
        public int AbsorbThroughComposure(int pressure)
        {
            if (pressure <= 0)
                return 0;

            if (_currentComposure > 0)
            {
                int absorbed = Mathf.Min(pressure, _currentComposure);
                int oldComposure = _currentComposure;
                _currentComposure -= absorbed;
                pressure -= absorbed;
                EventBus.Publish(
                    new ComposureChangedEvent
                    {
                        OldValue = oldComposure,
                        NewValue = _currentComposure,
                        IsPlayer = _isPlayer,
                    }
                );
            }

            return pressure;
        }

        /// <summary>Gains Composure stacks (defensive shield).</summary>
        public void GainComposure(int amount)
        {
            int old = _currentComposure;
            _currentComposure += amount;
            EventBus.Publish(
                new ComposureChangedEvent
                {
                    OldValue = old,
                    NewValue = _currentComposure,
                    IsPlayer = _isPlayer,
                }
            );
        }

        /// <summary>Loses Composure stacks.</summary>
        public int LoseComposure(int amount)
        {
            int old = _currentComposure;
            int loseAmount = Mathf.Min(amount, _currentComposure);
            _currentComposure -= loseAmount;
            if (loseAmount > 0)
                EventBus.Publish(
                    new ComposureChangedEvent
                    {
                        OldValue = old,
                        NewValue = _currentComposure,
                        IsPlayer = _isPlayer,
                    }
                );
            return loseAmount;
        }

        /// <summary>Consumes all Composure stacks.</summary>
        public int ConsumeAllComposure()
        {
            int consumed = _currentComposure;
            _currentComposure = 0;
            if (consumed > 0)
                EventBus.Publish(
                    new ComposureChangedEvent
                    {
                        OldValue = consumed,
                        NewValue = 0,
                        IsPlayer = _isPlayer,
                    }
                );
            return consumed;
        }

        #endregion

        #region Hostility Management

        /// <summary>Sets per-enemy hostility clamps. Called by EnemyController after construction.</summary>
        public void SetHostilityLimits(int min, int max)
        {
            _minHostility = min;
            _maxHostility = max;
        }

        /// <summary>Shifts hostility upward (more hostile).</summary>
        public void GainHostility(int amount)
        {
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
        }

        /// <summary>Shifts hostility downward (more receptive).</summary>
        public int ReduceHostility(int amount)
        {
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

        /// <summary>Called at the end of a turn (composure persists; cleared by next-turn start).</summary>
        public void EndTurn() { }

        #endregion

        #region Utility

        public string GetStatusString() =>
            $"Composure: {_currentComposure} | "
            + $"Hostility: {_currentHostility} ({HostilityDamageMultiplier:F2}x) | "
            + $"AP: {_currentActionPoints}/{_maxActionPoints}";

        #endregion
    }
}
