using System;
using Crookedile.Data;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Nepo Baby's starter passive ("the mulligan" — privilege, starts ahead). Lets the player
    /// discard ANY number of cards from hand and redraw that many. Authored on a battle-start
    /// OriginPassive so it fires once the opening hand is dealt. No-op when the hand is empty.
    ///
    /// Uses the player-choice picker in "up to" mode (select 0..hand size, confirm with any number).
    /// </summary>
    [Serializable]
    public class MulliganEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
                return;

            int handSize = ctx.Deck.HandCount;
            ResolveCardSelection(
                ctx.Deck.Hand,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored — PlayerChoice offers the whole hand
                "Discard any number of cards to redraw",
                handSize,
                chosen =>
                {
                    int discarded = 0;
                    foreach (var card in chosen)
                        if (ctx.Deck.DiscardCard(card))
                            discarded++;

                    if (discarded > 0)
                    {
                        ctx.Deck.DrawCards(discarded);
                        GameLogger.LogInfo<MulliganEffect>(
                            $"Mulligan: discarded {discarded} card(s) and redrew {discarded}"
                        );
                    }
                },
                allowFewer: true
            );
        }

        public override string GetDescription() =>
            "Discard any number of cards and redraw that many";
    }
}
