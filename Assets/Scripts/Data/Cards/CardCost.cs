using System;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Represents the Action Point cost to play a card in battle.
    /// Cards only cost Action Points (or are free). Funds/Influence are meta resources.
    /// Supports dynamic costs that change during battle.
    /// </summary>
    [Serializable]
    public class CardCost
    {
        [Tooltip("Type of cost (None or ActionPoints)")]
        [SerializeField] private CostType _costType;

        [Tooltip("Base amount of Action Points required (0 = free)")]
        [SerializeField] private int _baseAmount;

        [Tooltip("If true, this card costs all remaining Action Points (X cost)")]
        [SerializeField] private bool _isXCost;

        [Header("Dynamic Cost Modifiers")]
        [Tooltip("Cost reduction/increase per turn (-1 = decreases by 1 each turn, +1 = increases)")]
        [SerializeField] private int _costChangePerTurn;

        [Tooltip("Minimum cost (can't go below this, even with reductions)")]
        [SerializeField] private int _minimumCost = 0;

        [Tooltip("Maximum cost (can't go above this, even with increases)")]
        [SerializeField] private int _maximumCost = 99;

        // Runtime cost modifier (applied during battle)
        [NonSerialized] private int _runtimeCostModifier = 0;
        [NonSerialized] private int _turnsInHand = 0;

        /// <summary>
        /// Type of cost (None or ActionPoints).
        /// </summary>
        public CostType CostType => _costType;

        /// <summary>
        /// Base amount of Action Points required (before modifiers).
        /// </summary>
        public int BaseAmount => _baseAmount;

        /// <summary>
        /// Current amount of Action Points required (after all modifiers).
        /// </summary>
        public int CurrentAmount
        {
            get
            {
                int cost = _baseAmount;

                // Apply per-turn modifier
                cost += (_costChangePerTurn * _turnsInHand);

                // Apply runtime modifiers (from buffs/debuffs/effects)
                cost += _runtimeCostModifier;

                // Clamp to min/max
                return Mathf.Clamp(cost, _minimumCost, _maximumCost);
            }
        }

        /// <summary>
        /// Is this an X cost card (costs all remaining AP)?
        /// </summary>
        public bool IsXCost => _isXCost;

        /// <summary>
        /// Cost change per turn (negative = cheaper each turn, positive = more expensive).
        /// </summary>
        public int CostChangePerTurn => _costChangePerTurn;

        /// <summary>
        /// How many turns this card has been in hand.
        /// </summary>
        public int TurnsInHand => _turnsInHand;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public CardCost() { }

        /// <summary>
        /// Creates a new card cost.
        /// </summary>
        /// <param name="costType">Type of cost</param>
        /// <param name="amount">Base amount required</param>
        /// <param name="isXCost">If true, costs all remaining AP</param>
        /// <param name="costChangePerTurn">Cost change per turn in hand</param>
        public CardCost(CostType costType, int amount, bool isXCost = false, int costChangePerTurn = 0)
        {
            _costType = costType;
            _baseAmount = amount;
            _isXCost = isXCost;
            _costChangePerTurn = costChangePerTurn;
            _minimumCost = 0;
            _maximumCost = 99;
        }

        /// <summary>
        /// Creates a free card (0 cost).
        /// </summary>
        public static CardCost Free()
        {
            return new CardCost(CostType.None, 0, false);
        }

        /// <summary>
        /// Creates an X cost card (costs all remaining AP).
        /// </summary>
        public static CardCost XCost()
        {
            return new CardCost(CostType.ActionPoints, 0, true);
        }

        /// <summary>
        /// Checks if the player can afford this cost.
        /// </summary>
        /// <param name="currentActionPoints">Player's current Action Points</param>
        /// <returns>True if player has enough AP</returns>
        public bool CanAfford(int currentActionPoints)
        {
            // Free cards can always be played
            if (_costType == CostType.None)
                return true;

            // X cost cards require at least 1 AP
            if (_isXCost)
                return currentActionPoints > 0;

            // Normal cost - use CurrentAmount (includes all modifiers)
            return currentActionPoints >= CurrentAmount;
        }

        /// <summary>
        /// Gets the actual cost to pay based on current AP.
        /// For X cost cards, returns all remaining AP.
        /// </summary>
        /// <param name="currentActionPoints">Player's current Action Points</param>
        /// <returns>Actual AP cost to pay</returns>
        public int GetActualCost(int currentActionPoints)
        {
            if (_costType == CostType.None)
                return 0;

            if (_isXCost)
                return currentActionPoints; // X cost = all remaining AP

            return CurrentAmount; // Use current amount (includes all modifiers)
        }

        /// <summary>
        /// Called when this card is drawn into hand.
        /// Resets turn counter.
        /// </summary>
        public void OnDrawn()
        {
            _turnsInHand = 0;
        }

        /// <summary>
        /// Called at the start of each turn while this card is in hand.
        /// Updates the turn counter for dynamic cost calculation.
        /// </summary>
        public void OnTurnInHand()
        {
            _turnsInHand++;
        }

        /// <summary>
        /// Applies a temporary cost modifier (from buffs/debuffs).
        /// </summary>
        /// <param name="modifier">Amount to modify cost by (negative = cheaper, positive = more expensive)</param>
        public void ApplyCostModifier(int modifier)
        {
            _runtimeCostModifier += modifier;
        }

        /// <summary>
        /// Clears all runtime cost modifiers.
        /// </summary>
        public void ClearCostModifiers()
        {
            _runtimeCostModifier = 0;
        }

        /// <summary>
        /// Resets this cost to its base state (called when card is played or discarded).
        /// </summary>
        public void Reset()
        {
            _turnsInHand = 0;
            _runtimeCostModifier = 0;
        }

        /// <summary>
        /// Gets a formatted string representation of this cost.
        /// </summary>
        /// <param name="showModifiers">If true, shows base cost and modifiers separately</param>
        /// <returns>String like "2 AP", "X AP", "3 AP (was 5)", or "Free"</returns>
        public string ToString(bool showModifiers = false)
        {
            if (_costType == CostType.None)
            {
                return "Free";
            }

            if (_isXCost)
            {
                return "X AP";
            }

            int current = CurrentAmount;

            if (current <= 0)
            {
                return "Free";
            }

            // Show modifiers if requested and cost has changed
            if (showModifiers && current != _baseAmount)
            {
                return $"{current} AP (was {_baseAmount})";
            }

            return $"{current} AP";
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }
}
