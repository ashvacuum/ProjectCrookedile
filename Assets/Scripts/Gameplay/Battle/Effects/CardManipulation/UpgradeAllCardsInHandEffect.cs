using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Upgrades every upgradeable card currently in the player's hand for this battle.</summary>
    [Serializable]
    public class UpgradeAllCardsInHandEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null) return;

            // Collect (old, upgraded) pairs first — don't modify the list while iterating
            var pairs = new List<(CardData old, CardData upgraded)>();
            foreach (var card in ctx.Deck.Hand)
                if (card != null && card.CanUpgrade)
                    pairs.Add((card, card.GetCurrentVersion()));

            foreach (var (old, upgraded) in pairs)
                ctx.Deck.SwapCardInHand(old, upgraded);

            GameLogger.LogInfo<UpgradeAllCardsInHandEffect>($"Upgraded {pairs.Count} cards in hand");
        }

        public override string GetDescription() => "Upgrade all cards in your hand this battle";
    }
}
