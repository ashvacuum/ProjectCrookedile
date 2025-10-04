using System;
using UnityEngine;

namespace Crookedile.Gameplay
{
    /// <summary>
    /// Tracks all battle-specific stats for a single combatant (player or opponent).
    /// Manages Ego (true HP), Confidence (shield/multiplier), Action Points, and Block.
    /// </summary>
    [Serializable]
    public class BattleStats
    {
        [Header("Health Resources")]
        [Tooltip("True HP - lose battle if this reaches 0")]
        [SerializeField] private int _currentEgo;

        [Tooltip("Maximum Ego value")]
        [SerializeField] private int _maxEgo;

        [Tooltip("Shield/multiplier - protects Ego and affects damage vulnerability")]
        [SerializeField] private int _currentConfidence;

        [Tooltip("Maximum Confidence value")]
        [SerializeField] private int _maxConfidence;

        [Header("Turn Resources")]
        [Tooltip("Action Points available this turn to play cards")]
        [SerializeField] private int _currentActionPoints;

        [Tooltip("Maximum Action Points per turn")]
        [SerializeField] private int _maxActionPoints = 3;

        [Tooltip("Temporary damage reduction (expires at end of turn)")]
        [SerializeField] private int _currentBlock;

        #region Properties

        public int CurrentEgo => _currentEgo;
        public int MaxEgo => _maxEgo;
        public int CurrentConfidence => _currentConfidence;
        public int MaxConfidence => _maxConfidence;
        public int CurrentActionPoints => _currentActionPoints;
        public int MaxActionPoints => _maxActionPoints;
        public int CurrentBlock => _currentBlock;

        /// <summary>
        /// Is this combatant defeated? (Ego <= 0)
        /// </summary>
        public bool IsDefeated => _currentEgo <= 0;

        /// <summary>
        /// Percentage of Confidence remaining (0.0 to 1.0)
        /// </summary>
        public float ConfidencePercentage => _maxConfidence > 0 ? (float)_currentConfidence / _maxConfidence : 0f;

        /// <summary>
        /// Ego vulnerability multiplier based on missing Confidence.
        /// Low Confidence = higher vulnerability = more Ego damage taken.
        /// Formula: 1.0 + (MissingConfidence / MaxConfidence)
        /// Example: 75% missing Confidence = 1.75x Ego damage multiplier
        /// </summary>
        public float EgoVulnerabilityMultiplier
        {
            get
            {
                if (_maxConfidence <= 0) return 1.0f;
                float missingConfidence = _maxConfidence - _currentConfidence;
                return 1.0f + (missingConfidence / _maxConfidence);
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
        public BattleStats(int maxEgo, int maxConfidence, int maxActionPoints = 3)
        {
            _maxEgo = maxEgo;
            _currentEgo = maxEgo;
            _maxConfidence = maxConfidence;
            _currentConfidence = maxConfidence;
            _maxActionPoints = maxActionPoints;
            _currentActionPoints = maxActionPoints;
            _currentBlock = 0;
        }

        #endregion

        #region Ego Management

        /// <summary>
        /// Damages Ego, applying vulnerability multiplier and block.
        /// Ego can only be damaged if Confidence <= 0 (shield broken).
        /// </summary>
        /// <param name="baseDamage">Base Ego damage before multipliers</param>
        /// <returns>Actual damage dealt after all calculations</returns>
        public int DamageEgo(int baseDamage)
        {
            if (_currentConfidence > 0)
            {
                Debug.LogWarning("Cannot damage Ego while Confidence is above 0!");
                return 0;
            }

            // Apply vulnerability multiplier
            float actualDamage = baseDamage * EgoVulnerabilityMultiplier;

            // Apply block reduction
            int damageAfterBlock = Mathf.Max(0, Mathf.RoundToInt(actualDamage) - _currentBlock);
            _currentBlock = Mathf.Max(0, _currentBlock - Mathf.RoundToInt(actualDamage));

            // Apply damage to Ego
            int finalDamage = Mathf.Min(damageAfterBlock, _currentEgo);
            _currentEgo -= finalDamage;

            Debug.Log($"Ego damaged: {baseDamage} base → {actualDamage:F1} after {EgoVulnerabilityMultiplier:F2}x vulnerability → {finalDamage} after block. Ego: {_currentEgo}/{_maxEgo}");
            return finalDamage;
        }

        /// <summary>
        /// Restores Ego (direct healing).
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        public int RestoreEgo(int amount)
        {
            int healAmount = Mathf.Min(amount, _maxEgo - _currentEgo);
            _currentEgo += healAmount;
            Debug.Log($"Ego restored: +{healAmount}. Ego: {_currentEgo}/{_maxEgo}");
            return healAmount;
        }

        #endregion

        #region Confidence Management

        /// <summary>
        /// Damages Confidence (shield breaking).
        /// Confidence must be broken before Ego can be damaged.
        /// </summary>
        /// <param name="damage">Damage amount</param>
        /// <returns>Actual damage dealt</returns>
        public int DamageConfidence(int damage)
        {
            // Apply block reduction
            int damageAfterBlock = Mathf.Max(0, damage - _currentBlock);
            _currentBlock = Mathf.Max(0, _currentBlock - damage);

            // Apply damage to Confidence
            int finalDamage = Mathf.Min(damageAfterBlock, _currentConfidence);
            _currentConfidence -= finalDamage;

            Debug.Log($"Confidence damaged: {damage} → {finalDamage} after block. Confidence: {_currentConfidence}/{_maxConfidence}");
            return finalDamage;
        }

        /// <summary>
        /// Restores Confidence (rebuild shield).
        /// </summary>
        /// <param name="amount">Amount to restore</param>
        /// <returns>Actual amount restored</returns>
        public int RestoreConfidence(int amount)
        {
            int restoreAmount = Mathf.Min(amount, _maxConfidence - _currentConfidence);
            _currentConfidence += restoreAmount;
            Debug.Log($"Confidence restored: +{restoreAmount}. Confidence: {_currentConfidence}/{_maxConfidence}");
            return restoreAmount;
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
        /// Refreshes Action Points to max at the start of a new turn.
        /// </summary>
        public void RefreshActionPoints()
        {
            _currentActionPoints = _maxActionPoints;
            Debug.Log($"Action Points refreshed: {_currentActionPoints}");
        }

        #endregion

        #region Block Management

        /// <summary>
        /// Gains Block (temporary damage reduction).
        /// </summary>
        /// <param name="amount">Block to gain</param>
        public void GainBlock(int amount)
        {
            _currentBlock += amount;
            Debug.Log($"Gained {amount} Block. Current Block: {_currentBlock}");
        }

        /// <summary>
        /// Clears all Block (called at end of turn).
        /// </summary>
        public void ClearBlock()
        {
            if (_currentBlock > 0)
            {
                Debug.Log($"Block expired: {_currentBlock} → 0");
            }
            _currentBlock = 0;
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
            ClearBlock();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a debug-friendly summary of current stats.
        /// </summary>
        public string GetStatusString()
        {
            return $"Ego: {_currentEgo}/{_maxEgo} | " +
                   $"Confidence: {_currentConfidence}/{_maxConfidence} | " +
                   $"AP: {_currentActionPoints}/{_maxActionPoints} | " +
                   $"Block: {_currentBlock} | " +
                   $"Vulnerability: {EgoVulnerabilityMultiplier:F2}x";
        }

        #endregion
    }
}
