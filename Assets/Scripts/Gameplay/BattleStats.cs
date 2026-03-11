using System;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Gameplay
{
    /// <summary>
    /// Tracks all battle-specific stats for a single combatant (player or opponent).
    /// Griftlands-inspired negotiation resources:
    /// - Resolve (HP) - Reduce to 0 = defeat
    /// - Composure (Defensive shield) - Absorbs incoming damage before Resolve; reset each turn start
    /// - Hostility (Self-inflicted debuff) - Opponent deals more damage
    /// - Action Points - Energy to play cards
    /// </summary>
    [Serializable]
    public class BattleStats
    {
        [Header("Core Resources")]
        [Tooltip("Resolve - HP for negotiations (reduce to 0 = defeat)")]
        [SerializeField] private int _currentResolve;

        [Tooltip("Maximum Resolve value")]
        [SerializeField] private int _maxResolve;

        [Tooltip("Composure - Defensive shield (absorbs incoming damage before Resolve; reset each turn start)")]
        [SerializeField] private int _currentComposure;

        [Tooltip("Hostility - Self-inflicted debuff (opponent deals more damage based on this)")]
        [SerializeField] private int _currentHostility;

        private int _maxHostility = 10;   // overridden per-enemy by SetHostilityLimits
        private int _minHostility = -10;  // overridden per-enemy by SetHostilityLimits

        [Header("Turn Resources")]
        [Tooltip("Action Points available this turn to play cards")]
        [SerializeField] private int _currentActionPoints;

        [Tooltip("Maximum Action Points per turn (3-4 depending on origin)")]
        [SerializeField] private int _maxActionPoints = 3;

        [Header("Next Turn Buffs")]
        [Tooltip("Action Points to gain at start of next turn")]
        [SerializeField] private int _actionPointsNextTurn;

        // Set in constructor — tells event publishers whose stats these are.
        private bool _isPlayer;

        #region Properties

        public int CurrentResolve => _currentResolve;
        public int MaxResolve => _maxResolve;
        public int CurrentComposure => _currentComposure;
        public int CurrentHostility => _currentHostility;
        public int CurrentActionPoints => _currentActionPoints;
        public int MaxActionPoints => _maxActionPoints;
        public int ActionPointsNextTurn => _actionPointsNextTurn;

        /// <summary>
        /// Is this combatant defeated? (Resolve <= 0)
        /// </summary>
        public bool IsDefeated => _currentResolve <= 0;

        /// <summary>
        /// True when the enemy is actively hostile (positive hostility).
        /// Damage multiplier applies. Reducing to 0 removes the bonus but does NOT unlock receptive behavior.
        /// </summary>
        public bool IsHostile => _currentHostility > 0;

        /// <summary>
        /// True when the enemy is in a receptive/de-escalated state (negative hostility).
        /// Requires pushing past 0 into negative territory — neutral (0) still attacks normally.
        /// </summary>
        public bool IsReceptive => _currentHostility < 0;

        /// <summary>
        /// Percentage of Resolve remaining (0.0 to 1.0)
        /// </summary>
        public float ResolvePercentage => _maxResolve > 0 ? (float)_currentResolve / _maxResolve : 0f;

        /// <summary>
        /// Hostility damage multiplier for incoming damage.
        /// Formula: max(0.1, 1.0 + Hostility × 0.5)
        /// Example: 3 Hostility = 1 + (3 × 0.5) = 2.5× damage | −4 Hostility = max(0.1, −1.0) = 0.1×
        /// Floored at 0.1 so even maximally receptive enemies still deal some damage.
        /// </summary>
        public float HostilityDamageMultiplier => Mathf.Max(0.1f, 1.0f + _currentHostility * 0.5f);

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public BattleStats() { }

        /// <summary>
        /// Creates battle stats with specified values. Current Resolve is set to <paramref name="maxResolve"/>.
        /// </summary>
        public BattleStats(int maxResolve, int maxActionPoints = 3, bool isPlayer = true)
        {
            _maxResolve          = maxResolve;
            _currentResolve      = maxResolve;
            _maxActionPoints     = maxActionPoints;
            _currentActionPoints = maxActionPoints;
            _currentComposure    = 0;
            _currentHostility    = 0;
            _actionPointsNextTurn = 0;
            _isPlayer            = isPlayer;
        }

        /// <summary>
        /// Creates battle stats where current Resolve differs from the maximum.
        /// Used when HP carries over from a previous battle in the same run.
        /// </summary>
        /// <param name="maxResolve">The combatant's maximum HP cap.</param>
        /// <param name="currentResolve">Starting HP for this battle (clamped to [0, maxResolve]).</param>
        /// <param name="maxActionPoints">Action Points available each turn.</param>
        /// <param name="isPlayer">Whether these stats belong to the player (affects event publishing).</param>
        public BattleStats(int maxResolve, int currentResolve, int maxActionPoints,
                           bool isPlayer = true)
        {
            _maxResolve          = maxResolve;
            _currentResolve      = Mathf.Clamp(currentResolve, 0, maxResolve);
            _maxActionPoints     = maxActionPoints;
            _currentActionPoints = maxActionPoints;
            _currentComposure    = 0;
            _currentHostility    = 0;
            _actionPointsNextTurn = 0;
            _isPlayer            = isPlayer;
        }

        #endregion

        #region Resolve Management

        /// <summary>
        /// Damages this combatant. Composure acts as a shield — incoming damage depletes
        /// Composure first; any remainder reduces Resolve.
        /// </summary>
        /// <param name="damage">Total incoming damage (after all external modifiers)</param>
        /// <returns>Total damage actually absorbed (Composure absorbed + Resolve reduced)</returns>
        public int DamageResolve(int damage)
        {
            if (damage <= 0) return 0;

            int totalAbsorbed = 0;

            // Composure absorbs damage first (shield)
            if (_currentComposure > 0)
            {
                int composureAbsorb = Mathf.Min(damage, _currentComposure);
                int oldComposure    = _currentComposure;
                _currentComposure  -= composureAbsorb;
                damage             -= composureAbsorb;
                totalAbsorbed      += composureAbsorb;
                Debug.Log($"Composure absorbed {composureAbsorb} damage. Composure: {_currentComposure}");
                EventBus.Publish(new ComposureChangedEvent { OldValue = oldComposure, NewValue = _currentComposure, IsPlayer = _isPlayer });
            }

            // Remaining damage hits Resolve
            if (damage > 0)
            {
                int actual     = Mathf.Min(damage, _currentResolve);
                if (actual > 0)
                {
                    int oldResolve  = _currentResolve;
                    _currentResolve -= actual;
                    totalAbsorbed   += actual;
                    Debug.Log($"Resolve damaged: {actual}. Resolve: {_currentResolve}/{_maxResolve}");
                    EventBus.Publish(new ResolveChangedEvent { OldValue = oldResolve, NewValue = _currentResolve, IsPlayer = _isPlayer });
                }
            }

            return totalAbsorbed;
        }

        /// <summary>
        /// Damages Resolve directly, bypassing the Composure shield entirely.
        /// Use for reflected damage (Thorns) where the attacker's mental defences
        /// should not cushion the retaliation.
        /// Publishes <see cref="ResolveChangedEvent"/> so the UI damage number still appears.
        /// </summary>
        /// <param name="damage">Incoming damage (not reduced by Composure)</param>
        /// <returns>Actual Resolve lost (capped at current Resolve)</returns>
        public int DamageResolveBypass(int damage)
        {
            if (damage <= 0) return 0;

            int actual = Mathf.Min(damage, _currentResolve);
            if (actual > 0)
            {
                int oldResolve  = _currentResolve;
                _currentResolve -= actual;
                Debug.Log($"Bypass damage (ignores Composure): {actual}. Resolve: {_currentResolve}/{_maxResolve}");
                EventBus.Publish(new ResolveChangedEvent { OldValue = oldResolve, NewValue = _currentResolve, IsPlayer = _isPlayer });
            }

            return actual;
        }

        /// <summary>
        /// Damages Resolve with Hostility multiplier applied (for opponent attacking player).
        /// Delegates to <see cref="DamageResolve"/> after applying the multiplier.
        /// </summary>
        /// <param name="baseDamage">Base Resolve damage from opponent</param>
        /// <returns>Actual damage dealt after Hostility multiplier and Composure shield</returns>
        public int DamageResolveWithHostility(int baseDamage)
        {
            float multiplied = baseDamage * HostilityDamageMultiplier;
            Debug.Log($"DamageResolveWithHostility: {baseDamage} base × {HostilityDamageMultiplier:F2}x = {Mathf.RoundToInt(multiplied)} damage");
            return DamageResolve(Mathf.RoundToInt(multiplied));
        }

        /// <summary>
        /// Restores Resolve (healing).
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        public int RestoreResolve(int amount)
        {
            int oldResolve = _currentResolve;
            int healAmount = Mathf.Min(amount, _maxResolve - _currentResolve);
            _currentResolve += healAmount;
            Debug.Log($"Resolve restored: +{healAmount}. Resolve: {_currentResolve}/{_maxResolve}");
            if (healAmount > 0)
                EventBus.Publish(new ResolveChangedEvent { OldValue = oldResolve, NewValue = _currentResolve, IsPlayer = _isPlayer });
            return healAmount;
        }

        #endregion

        #region Composure Management

        /// <summary>
        /// Gains Composure stacks (offensive buff).
        /// </summary>
        /// <param name="amount">Composure to gain</param>
        public void GainComposure(int amount)
        {
            int old = _currentComposure;
            _currentComposure += amount;
            Debug.Log($"Gained {amount} Composure. Current: {_currentComposure}");
            EventBus.Publish(new ComposureChangedEvent { OldValue = old, NewValue = _currentComposure, IsPlayer = _isPlayer });
        }

        /// <summary>
        /// Loses Composure stacks.
        /// </summary>
        /// <param name="amount">Composure to lose</param>
        /// <returns>Actual amount lost</returns>
        public int LoseComposure(int amount)
        {
            int old        = _currentComposure;
            int loseAmount = Mathf.Min(amount, _currentComposure);
            _currentComposure -= loseAmount;
            Debug.Log($"Lost {loseAmount} Composure. Current: {_currentComposure}");
            if (loseAmount > 0)
                EventBus.Publish(new ComposureChangedEvent { OldValue = old, NewValue = _currentComposure, IsPlayer = _isPlayer });
            return loseAmount;
        }

        /// <summary>
        /// Consumes all Composure stacks (Faith Leader Blessing).
        /// </summary>
        /// <returns>Amount of Composure consumed</returns>
        public int ConsumeAllComposure()
        {
            int consumed = _currentComposure;
            _currentComposure = 0;
            Debug.Log($"Consumed all Composure: {consumed}");
            if (consumed > 0)
                EventBus.Publish(new ComposureChangedEvent { OldValue = consumed, NewValue = 0, IsPlayer = _isPlayer });
            return consumed;
        }

        #endregion

        #region Hostility Management

        /// <summary>
        /// Sets per-enemy hostility clamps. Called by EnemyController after construction.
        /// Player stats keep the default ±10 fallback (hostility is enemy-only).
        /// </summary>
        public void SetHostilityLimits(int min, int max)
        {
            _minHostility = min;
            _maxHostility = max;
        }

        /// <summary>
        /// Shifts hostility up (makes enemy more hostile). Clamped at per-enemy max.
        /// </summary>
        public void GainHostility(int amount)
        {
            int old = _currentHostility;
            _currentHostility += amount;
            _currentHostility = Mathf.Min(_maxHostility, _currentHostility);
            Debug.Log($"Gained {amount} Hostility. Current: {_currentHostility}");
            EventBus.Publish(new HostilityChangedEvent { OldValue = old, NewValue = _currentHostility, IsPlayer = _isPlayer });
        }

        /// <summary>
        /// Shifts hostility down (makes enemy more receptive). Clamped at per-enemy min.
        /// </summary>
        /// <returns>The actual hostility reduction applied (may be less than <paramref name="amount"/> due to clamping).</returns>
        public int ReduceHostility(int amount)
        {
            int old = _currentHostility;
            _currentHostility -= amount;
            _currentHostility = Mathf.Max(_minHostility, _currentHostility);
            int actual = old - _currentHostility;
            Debug.Log($"Reduced {actual} Hostility (requested {amount}). Current: {_currentHostility}");
            EventBus.Publish(new HostilityChangedEvent { OldValue = old, NewValue = _currentHostility, IsPlayer = _isPlayer });
            return actual;
        }

        /// <summary>
        /// Sets hostility to an exact value. Used to initialize enemy starting hostility.
        /// </summary>
        public void SetHostility(int value)
        {
            int old = _currentHostility;
            _currentHostility = Mathf.Clamp(value, _minHostility, _maxHostility);
            Debug.Log($"Hostility set to: {_currentHostility}");
            if (old != _currentHostility)
                EventBus.Publish(new HostilityChangedEvent { OldValue = old, NewValue = _currentHostility, IsPlayer = _isPlayer });
        }

        #endregion

        #region Action Points Management

        /// <summary>
        /// Spends Action Points to play a card.
        /// </summary>
        /// <param name="cost">Action Points to spend</param>
        /// <returns>True if successful, false if insufficient Action Points</returns>
        public bool SpendActionPoints(int cost)
        {
            if (_currentActionPoints < cost)
            {
                Debug.LogWarning($"Insufficient Action Points: {_currentActionPoints}/{cost}");
                return false;
            }

            int old = _currentActionPoints;
            _currentActionPoints -= cost;
            Debug.Log($"Spent {cost} Action Points. Remaining: {_currentActionPoints}");
            EventBus.Publish(new ActionPointsChangedEvent { OldValue = old, NewValue = _currentActionPoints, IsPlayer = _isPlayer });
            return true;
        }

        /// <summary>
        /// Gains extra Action Points this turn.
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        public void GainActionPoints(int amount)
        {
            int old = _currentActionPoints;
            _currentActionPoints += amount;
            Debug.Log($"Gained {amount} Action Points. Current: {_currentActionPoints}");
            EventBus.Publish(new ActionPointsChangedEvent { OldValue = old, NewValue = _currentActionPoints, IsPlayer = _isPlayer });
        }

        /// <summary>
        /// Gains Action Points for next turn (Nepo Baby Backroom Deal).
        /// </summary>
        /// <param name="amount">Amount to gain next turn</param>
        public void GainActionPointsNextTurn(int amount)
        {
            _actionPointsNextTurn += amount;
            Debug.Log($"Will gain {amount} AP next turn. Total next turn: {_actionPointsNextTurn}");
        }

        /// <summary>
        /// Refreshes Action Points to max at the start of a new turn.
        /// Applies any banked AP from previous turn effects.
        /// </summary>
        public void RefreshActionPoints()
        {
            int old = _currentActionPoints;
            _currentActionPoints = _maxActionPoints + _actionPointsNextTurn;
            Debug.Log($"Action Points refreshed: {_maxActionPoints} + {_actionPointsNextTurn} banked = {_currentActionPoints}");
            _actionPointsNextTurn = 0; // Reset banked AP
            EventBus.Publish(new ActionPointsChangedEvent { OldValue = old, NewValue = _currentActionPoints, IsPlayer = _isPlayer });
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Called at the start of a turn to refresh resources.
        /// AP refresh is skipped for combatants with no AP (enemies use maxActionPoints: 0).
        /// </summary>
        public void StartTurn()
        {
            // Only refresh AP for combatants that actually have it.
            // Enemies are created with maxActionPoints: 0 and never use the AP system —
            // skipping this prevents a no-op RefreshActionPoints() Debug.Log per enemy per turn.
            if (_maxActionPoints > 0)
                RefreshActionPoints();
        }

        /// <summary>
        /// Called at the end of a turn to clear temporary effects.
        /// </summary>
        public void EndTurn()
        {
            // Composure is reset at the START of the next turn (in BattleManager.StartTurn)
            // Hostility persists between turns (ongoing debuff)
            // Action Points refresh at start of next turn via StartTurn() → RefreshActionPoints()
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a debug-friendly summary of current stats.
        /// </summary>
        public string GetStatusString()
        {
            return $"Resolve: {_currentResolve}/{_maxResolve} | " +
                   $"Composure: {_currentComposure} | " +
                   $"Hostility: {_currentHostility} ({HostilityDamageMultiplier:F2}x damage) | " +
                   $"AP: {_currentActionPoints}/{_maxActionPoints}";
        }

        #endregion
    }
}
