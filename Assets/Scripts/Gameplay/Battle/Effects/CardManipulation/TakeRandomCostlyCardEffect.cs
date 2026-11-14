using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Puts a random card with at least the given printed cost into hand: from the draw pile (a
    /// Pull), or from the discard pile when the draw pile has none.
    /// </summary>
    [Serializable]
    public class TakeRandomCostlyCardEffect : BattleEffect
    {
        [Min(0)]
        [Tooltip("Only cards with at least this printed cost can be taken.")]
        [SerializeField]
        private int _minimumPrintedCost = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.IsHandFull)
                return;

            var fromDraw = Costly(ctx.Deck.DrawPile);
            if (fromDraw.Count > 0)
            {
                var card = fromDraw[BattleRandom.Range(0, fromDraw.Count)];
                if (ctx.Deck.PullFromDrawPile(card))
                    GameLogger.LogInfo<TakeRandomCostlyCardEffect>(
                        $"Took {card.CardName} from the draw pile"
                    );
                return;
            }

            var fromDiscard = Costly(ctx.Deck.DiscardPile);
            if (fromDiscard.Count > 0)
            {
                var card = fromDiscard[BattleRandom.Range(0, fromDiscard.Count)];
                if (ctx.Deck.MoveFromDiscardToHand(card))
                    GameLogger.LogInfo<TakeRandomCostlyCardEffect>(
                        $"Took {card.CardName} from the discard pile"
                    );
            }
        }

        private List<CardData> Costly(IReadOnlyList<CardData> pile)
        {
            var cards = new List<CardData>();
            foreach (var card in pile)
                if (card != null && card.PrintedCost >= _minimumPrintedCost)
                    cards.Add(card);
            return cards;
        }

        public override string GetDescription() =>
            $"Take a random card costing {_minimumPrintedCost}+ from your draw pile (or your discard pile if there is none)";
    }
}
