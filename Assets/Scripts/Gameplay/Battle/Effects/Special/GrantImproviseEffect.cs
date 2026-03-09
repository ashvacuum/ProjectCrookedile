using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Discards the player's entire hand and draws the same number of cards back.
    ///
    /// Designed for use in the Actor's <c>OriginPassive._passives</c> list.
    /// The trigger and OneShot settings on the BattlePassive entry control
    /// when and how often this fires (e.g. TurnStartTrigger each turn).
    /// </summary>
    [Serializable]
    public class DiscardHandAndRedrawEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return; // guard: enemies have no deck

            var hand = new List<CardData>(ctx.Deck.Hand);
            int count = hand.Count;

            if (count == 0)
            {
                GameLogger.LogInfo<DiscardHandAndRedrawEffect>("Improvise: no cards in hand.");
                return;
            }

            foreach (var card in hand)
                ctx.Deck.DiscardCard(card);

            int drawn = ctx.Deck.DrawCards(count);
            GameLogger.LogInfo<DiscardHandAndRedrawEffect>(
                $"Improvise: discarded {count} card(s), drew {drawn} back.");
        }

        public override string GetDescription() =>
            "Discard your hand and draw the same number of cards.";
    }
}
