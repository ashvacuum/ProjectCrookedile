using System;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Defines a bonus effect that activates when a card is played by a specific origin.
    /// Examples:
    /// - Strongman: Intimidate deals +2 damage
    /// - Celebrity: Charm gains +5 Clout
    /// - Religious Leader: Sermon grants +2 Faith
    /// - Nepo Baby: Dynasty Network costs 1 less Influence
    /// </summary>
    [Serializable]
    public class OriginBonus
    {
        [Tooltip("Which origin must play this card to activate the bonus")]
        [SerializeField] private OriginType _requiredOrigin;

        [Tooltip("The bonus effect that is applied when the origin matches")]
        [SerializeField] private CardEffect _bonusEffect;

        [Tooltip("Human-readable description of the bonus (e.g., '+2 damage if Strongman')")]
        [SerializeField] private string _bonusDescription;

        /// <summary>
        /// Which origin is required to activate this bonus.
        /// </summary>
        public OriginType RequiredOrigin => _requiredOrigin;

        /// <summary>
        /// The bonus effect that applies when origin matches.
        /// </summary>
        public CardEffect BonusEffect => _bonusEffect;

        /// <summary>
        /// Human-readable description of the bonus.
        /// </summary>
        public string BonusDescription => _bonusDescription;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public OriginBonus() { }

        /// <summary>
        /// Creates a new origin bonus.
        /// </summary>
        /// <param name="requiredOrigin">Origin that must play the card</param>
        /// <param name="bonusEffect">Effect to apply as bonus</param>
        /// <param name="bonusDescription">Description text</param>
        public OriginBonus(OriginType requiredOrigin, CardEffect bonusEffect, string bonusDescription)
        {
            _requiredOrigin = requiredOrigin;
            _bonusEffect = bonusEffect;
            _bonusDescription = bonusDescription;
        }

        /// <summary>
        /// Checks if this bonus should apply based on the player's origin.
        /// </summary>
        /// <param name="playerOrigin">The player's current origin type</param>
        /// <returns>True if the bonus should activate</returns>
        public bool IsApplicable(OriginType playerOrigin)
        {
            return playerOrigin == _requiredOrigin;
        }

        /// <summary>
        /// Gets the formatted bonus text for display on cards.
        /// </summary>
        /// <returns>Formatted string like "[Strongman]: +2 damage"</returns>
        public string GetFormattedText()
        {
            return $"[{_requiredOrigin}]: {_bonusDescription}";
        }

        /// <summary>
        /// Gets just the origin icon/name for compact display.
        /// </summary>
        /// <returns>Origin name or symbol</returns>
        public string GetOriginSymbol()
        {
            return _requiredOrigin switch
            {
                OriginType.NepoBaby => "💪",
                OriginType.Actor => "⭐",
                OriginType.FaithLeader => "✝",
                _ => "?"
            };
        }
    }
}
