using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// A card effect asking the player to pick cards. Effects hand it to
    /// <see cref="BattleManager.RequestCardChoice"/>, which passes it to whoever answers prompts
    /// (the battle UI, or a test or playtest bot). The answerer calls <see cref="OnConfirmed"/>.
    /// </summary>
    public class CardChoiceRequest
    {
        /// <summary>Header text shown in the panel (e.g. "Choose a card from Discard").</summary>
        public string Title;

        /// <summary>All cards available to pick from.</summary>
        public IReadOnlyList<CardData> Choices;

        /// <summary>
        /// Number of cards to select. When <see cref="AllowFewer"/> is false this is an exact
        /// requirement (Confirm activates only at exactly this many). When true it is a maximum —
        /// the player may confirm with any number from 0 up to this (e.g. the Nepo Baby mulligan).
        /// </summary>
        public int RequiredCount;

        /// <summary>
        /// When true, <see cref="RequiredCount"/> is treated as a maximum and the player may confirm
        /// with fewer (including zero). When false, an exact count is required. Default false.
        /// </summary>
        public bool AllowFewer;

        /// <summary>
        /// Invoked with the confirmed selection once the player presses Confirm.
        /// An empty list means the player cancelled — all callbacks must treat an empty list as a no-op.
        /// </summary>
        public Action<List<CardData>> OnConfirmed;
    }
}
