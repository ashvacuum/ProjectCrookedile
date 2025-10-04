using System;
using UnityEngine;

namespace Crookedile.Gameplay
{
    /// <summary>
    /// Tracks all battle-specific stats for a single combatant (player or opponent).
    /// Griftlands-inspired negotiation resources:
    /// - Resolve (HP) - Reduce to 0 = defeat
    /// - Composure (Offensive buff) - Each stack = +1 damage
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

        [Tooltip("Composure - Offensive buff stacks (each stack = +1 damage, consumed on attack)")]
        [SerializeField] private int _currentComposure;

        [Tooltip("Hostility - Self-inflicted debuff (opponent deals more damage based on this)")]
        [SerializeField] private int _currentHostility;

        [Header("Turn Resources")]
        [Tooltip("Action Points available this turn to play cards")]
        [SerializeField] private int _currentActionPoints;

        [Tooltip("Maximum Action Points per turn (3-4 depending on origin)")]
        [SerializeField] private int _maxActionPoints = 3;

        [Header("Next Turn Buffs")]
        [Tooltip("Action Points to gain at start of next turn")]
        [SerializeField] private int _actionPointsNextTurn;

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
        /// Percentage of Resolve remaining (0.0 to 1.0)
        /// </summary>
        public float ResolvePercentage => _maxResolve > 0 ? (float)_currentResolve / _maxResolve : 0f;

        /// <summary>
        /// Hostility damage multiplier for incoming damage.
        /// Formula: 1.0 + (Hostility × 0.5)
        /// Example: 3 Hostility = 1 + (3 × 0.5) = 2.5× damage multiplier
        /// </summary>
        public float HostilityDamageMultiplier
        {
            get
            {
                return 1.0f + (_currentHostility * 0.5f);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public BattleStats() { }

        /// <summary>
        /// Creates battle stats with specified values.
        /// </summary>
        public BattleStats(int maxResolve, int maxActionPoints = 3)
        {
            _maxResolve = maxResolve;
            _currentResolve = maxResolve;
            _maxActionPoints = maxActionPoints;
            _currentActionPoints = maxActionPoints;
            _currentComposure = 0;
            _currentHostility = 0;
            _actionPointsNextTurn = 0;
        }

        #endregion

        #region Resolve Management

        /// <summary>
        /// Damages Resolve, applying Composure bonus for attacker.
        /// If this is player attacking, apply player's Composure as bonus damage.
        /// </summary>
        /// <param name="baseDamage">Base Resolve damage</param>
        /// <param name="attackerComposure">Attacker's Composure (adds to damage)</param>
        /// <returns>Actual damage dealt</returns>
        public int DamageResolve(int baseDamage, int attackerComposure = 0)
        {
            // Calculate total damage (base + Composure bonus)
            int totalDamage = baseDamage + attackerComposure;

            // Apply damage to Resolve
            int finalDamage = Mathf.Min(totalDamage, _currentResolve);
            _currentResolve -= finalDamage;

            Debug.Log($"Resolve damaged: {baseDamage} base + {attackerComposure} Composure = {finalDamage} damage. Resolve: {_currentResolve}/{_maxResolve}");
            return finalDamage;
        }

        /// <summary>
        /// Damages Resolve with Hostility multiplier applied (for opponent attacking player).
        /// </summary>
        /// <param name="baseDamage">Base Resolve damage from opponent</param>
        /// <returns>Actual damage dealt after Hostility multiplier</returns>
        public int DamageResolveWithHostility(int baseDamage)
        {
            // Apply Hostility multiplier
            float actualDamage = baseDamage * HostilityDamageMultiplier;
            int finalDamage = Mathf.Min(Mathf.RoundToInt(actualDamage), _currentResolve);
            _currentResolve -= finalDamage;

            Debug.Log($"Resolve damaged with Hostility: {baseDamage} base × {HostilityDamageMultiplier:F2}x = {finalDamage} damage. Resolve: {_currentResolve}/{_maxResolve}");
            return finalDamage;
        }

        /// <summary>
        /// Restores Resolve (healing).
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        public int RestoreResolve(int amount)
        {
            int healAmount = Mathf.Min(amount, _maxResolve - _currentResolve);
            _currentResolve += healAmount;
            Debug.Log($"Resolve restored: +{healAmount}. Resolve: {_currentResolve}/{_maxResolve}");
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
            _currentComposure += amount;
            Debug.Log($"Gained {amount} Composure. Current: {_currentComposure}");
        }

        /// <summary>
        /// Loses Composure stacks.
        /// </summary>
        /// <param name="amount">Composure to lose</param>
        /// <returns>Actual amount lost</returns>
        public int LoseComposure(int amount)
        {
            int loseAmount = Mathf.Min(amount, _currentComposure);
            _currentComposure -= loseAmount;
            Debug.Log($"Lost {loseAmount} Composure. Current: {_currentComposure}");
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
            return consumed;
        }

        #endregion

        #region Hostility Management

        /// <summary>
        /// Gains Hostility (self-inflicted debuff).
        /// </summary>
        /// <param name="amount">Hostility to gain</param>
        public void GainHostility(int amount)
        {
            _currentHostility += amount;
            Debug.Log($"Gained {amount} Hostility. Current: {_currentHostility} (Damage multiplier: {HostilityDamageMultiplier:F2}x)");
        }

        /// <summary>
        /// Reduces Hostility.
        /// </summary>
        /// <param name="amount">Hostility to reduce</param>
        /// <returns>Actual amount reduced</returns>
        public int ReduceHostility(int amount)
        {
            int reduceAmount = Mathf.Min(amount, _currentHostility);
            _currentHostility -= reduceAmount;
            Debug.Log($"Reduced {reduceAmount} Hostility. Current: {_currentHostility} (Damage multiplier: {HostilityDamageMultiplier:F2}x)");
            return reduceAmount;
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

            _currentActionPoints -= cost;
            Debug.Log($"Spent {cost} Action Points. Remaining: {_currentActionPoints}");
            return true;
        }

        /// <summary>
        /// Gains extra Action Points this turn.
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        public void GainActionPoints(int amount)
        {
            _currentActionPoints += amount;
            Debug.Log($"Gained {amount} Action Points. Current: {_currentActionPoints}");
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
            _currentActionPoints = _maxActionPoints + _actionPointsNextTurn;
            Debug.Log($"Action Points refreshed: {_maxActionPoints} + {_actionPointsNextTurn} banked = {_currentActionPoints}");
            _actionPointsNextTurn = 0; // Reset banked AP
        }

        #endregion

        #region Turn Management

        /// <summary>
        /// Called at the start of a turn to refresh resources.
        /// </summary>
        public void StartTurn()
        {
            RefreshActionPoints();
        }

        /// <summary>
        /// Called at the end of a turn to clear temporary effects.
        /// </summary>
        public void EndTurn()
        {
            // Composure persists between turns (unlike Block)
            // Hostility persists between turns (ongoing debuff)
            // Only Action Points refresh
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
