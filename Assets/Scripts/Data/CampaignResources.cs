using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Tracks all campaign/overworld resources that persist across battles.
    /// Simplified to 3 core resources: Funds, Heat, and Influence.
    /// </summary>
    [Serializable]
    public class CampaignResources
    {
        [Header("Core Resources")]
        [Tooltip("₱ - Campaign funds for buying cards, upgrades, and bribes")]
        [SerializeField] private int _funds;

        [Tooltip("H - Scandal meter (0-100). Lose if this reaches 100.")]
        [SerializeField] private int _heat;

        [Tooltip("Political influence/power. Win condition and currency for favors.")]
        [SerializeField] private int _influence;

        [Header("Limits")]
        [SerializeField] private int _maxHeat = 100;

        #region Properties

        public int Funds => _funds;
        public int Heat => _heat;
        public int Influence => _influence;
        public int MaxHeat => _maxHeat;

        /// <summary>
        /// Is the player at critical Heat level? (near game over)
        /// </summary>
        public bool IsCriticalHeat => _heat >= 80;

        /// <summary>
        /// Has the player lost due to scandal? (Heat >= 100)
        /// </summary>
        public bool IsScandalized => _heat >= _maxHeat;

        /// <summary>
        /// Heat percentage (0.0 to 1.0)
        /// </summary>
        public float HeatPercentage => (float)_heat / _maxHeat;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public CampaignResources() { }

        /// <summary>
        /// Creates campaign resources with starting values.
        /// </summary>
        public CampaignResources(int startingFunds, int startingHeat, int startingInfluence)
        {
            _funds = startingFunds;
            _heat = Mathf.Clamp(startingHeat, 0, _maxHeat);
            _influence = startingInfluence;
        }

        #endregion

        #region Funds Management

        /// <summary>
        /// Gains funds legitimately (no Heat gain).
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        /// <returns>Actual amount gained</returns>
        public int GainFundsLegitimate(int amount)
        {
            int gainAmount = Mathf.Max(0, amount);
            _funds += gainAmount;
            Debug.Log($"Gained {gainAmount}₱ legitimately. Total: {_funds}₱");
            return gainAmount;
        }

        /// <summary>
        /// Gains funds through corrupt means (also increases Heat).
        /// Examples: bribes, kickbacks, protection money.
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        /// <param name="heatGain">Heat increase from corruption (default 10% of funds)</param>
        /// <returns>Actual amount gained</returns>
        public int GainFundsCorrupt(int amount, int heatGain = -1)
        {
            int gainAmount = Mathf.Max(0, amount);
            _funds += gainAmount;

            // Calculate Heat gain (10% of funds by default)
            int actualHeatGain = heatGain >= 0 ? heatGain : Mathf.Max(1, gainAmount / 10);
            GainHeat(actualHeatGain);

            Debug.Log($"Gained {gainAmount}₱ through corruption (+{actualHeatGain}H). Total: {_funds}₱, Heat: {_heat}/{_maxHeat}");
            return gainAmount;
        }

        /// <summary>
        /// Spends funds.
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool SpendFunds(int amount)
        {
            if (_funds < amount)
            {
                Debug.LogWarning($"Insufficient funds: {_funds}₱ < {amount}₱");
                return false;
            }

            _funds -= amount;
            Debug.Log($"Spent {amount}₱. Remaining: {_funds}₱");
            return true;
        }

        /// <summary>
        /// Checks if player can afford the cost.
        /// </summary>
        public bool CanAffordFunds(int amount)
        {
            return _funds >= amount;
        }

        #endregion

        #region Heat Management

        /// <summary>
        /// Gains Heat (scandal increases).
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        /// <returns>Actual amount gained (capped at max)</returns>
        public int GainHeat(int amount)
        {
            int oldHeat = _heat;
            _heat = Mathf.Min(_heat + amount, _maxHeat);
            int actualGain = _heat - oldHeat;

            if (actualGain > 0)
            {
                Debug.Log($"Gained {actualGain}H. Heat: {_heat}/{_maxHeat}");
                if (IsCriticalHeat)
                {
                    Debug.LogWarning($"CRITICAL HEAT! Heat at {_heat}/{_maxHeat}");
                }
            }

            return actualGain;
        }

        /// <summary>
        /// Reduces Heat (scandal decreases).
        /// </summary>
        /// <param name="amount">Amount to reduce</param>
        /// <returns>Actual amount reduced</returns>
        public int LoseHeat(int amount)
        {
            int oldHeat = _heat;
            _heat = Mathf.Max(_heat - amount, 0);
            int actualLoss = oldHeat - _heat;

            if (actualLoss > 0)
            {
                Debug.Log($"Reduced {actualLoss}H. Heat: {_heat}/{_maxHeat}");
            }

            return actualLoss;
        }

        #endregion

        #region Influence Management

        /// <summary>
        /// Gains Influence (political power).
        /// </summary>
        /// <param name="amount">Amount to gain</param>
        /// <returns>Actual amount gained</returns>
        public int GainInfluence(int amount)
        {
            int gainAmount = Mathf.Max(0, amount);
            _influence += gainAmount;
            Debug.Log($"Gained {gainAmount} Influence. Total: {_influence}");
            return gainAmount;
        }

        /// <summary>
        /// Spends Influence (calling in favors, using political power).
        /// </summary>
        /// <param name="amount">Amount to spend</param>
        /// <returns>True if successful, false if insufficient Influence</returns>
        public bool SpendInfluence(int amount)
        {
            if (_influence < amount)
            {
                Debug.LogWarning($"Insufficient Influence: {_influence} < {amount}");
                return false;
            }

            _influence -= amount;
            Debug.Log($"Spent {amount} Influence. Remaining: {_influence}");
            return true;
        }

        /// <summary>
        /// Checks if player can afford the Influence cost.
        /// </summary>
        public bool CanAffordInfluence(int amount)
        {
            return _influence >= amount;
        }

        /// <summary>
        /// Loses Influence (betrayal, broken promises).
        /// </summary>
        public int LoseInfluence(int amount)
        {
            int oldInfluence = _influence;
            _influence = Mathf.Max(_influence - amount, 0);
            int actualLoss = oldInfluence - _influence;

            if (actualLoss > 0)
            {
                Debug.Log($"Lost {actualLoss} Influence. Remaining: {_influence}");
            }

            return actualLoss;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a debug-friendly summary of current resources.
        /// </summary>
        public string GetStatusString()
        {
            return $"₱{_funds} | Heat: {_heat}/{_maxHeat} | Influence: {_influence}";
        }

        /// <summary>
        /// Resets resources to starting values.
        /// </summary>
        public void Reset(int startingFunds, int startingHeat, int startingInfluence)
        {
            _funds = startingFunds;
            _heat = Mathf.Clamp(startingHeat, 0, _maxHeat);
            _influence = startingInfluence;
            Debug.Log($"Resources reset: {GetStatusString()}");
        }

        #endregion
    }
}
