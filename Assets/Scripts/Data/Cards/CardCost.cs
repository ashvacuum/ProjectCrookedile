using System;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Represents a resource cost required to play a card.
    /// Cards can have multiple costs (e.g., 200₱ + 1 Lagay + 2 Energy).
    /// </summary>
    [Serializable]
    public class CardCost
    {
        [Tooltip("Type of resource required (None, Campaign Funds, Lagay, Energy, Origin Currency)")]
        [SerializeField] private CostType _costType;

        [Tooltip("Amount of the resource required")]
        [SerializeField] private int _amount;

        /// <summary>
        /// Type of cost (e.g., Campaign Funds, Lagay, Energy).
        /// </summary>
        public CostType CostType => _costType;

        /// <summary>
        /// Amount of the resource required.
        /// </summary>
        public int Amount => _amount;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public CardCost() { }

        /// <summary>
        /// Creates a new card cost.
        /// </summary>
        /// <param name="costType">Type of cost</param>
        /// <param name="amount">Amount required</param>
        public CardCost(CostType costType, int amount)
        {
            _costType = costType;
            _amount = amount;
        }

        /// <summary>
        /// Checks if the player can afford this cost.
        /// </summary>
        /// <param name="currentAmount">Player's current amount of this resource</param>
        /// <returns>True if player has enough of the resource</returns>
        public bool CanAfford(int currentAmount)
        {
            return currentAmount >= _amount;
        }

        /// <summary>
        /// Gets a formatted string representation of this cost.
        /// </summary>
        /// <returns>String like "2 AP" or "200₱"</returns>
        public override string ToString()
        {
            string symbol = _costType switch
            {
                CostType.ActionPoints => " AP",
                CostType.Funds => "₱",
                CostType.Influence => " Influence",
                CostType.None => "",
                _ => ""
            };

            return _amount > 0 ? $"{_amount}{symbol}" : "Free";
        }
    }
}
