using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Full-hand redraw: discard the whole hand and draw that many cards (Do-Over). With
    /// <see cref="_asOptionalOffer"/> the player is asked first and may keep the hand — Nepo
    /// Baby's once-per-battle Mulligan passive.
    /// </summary>
    [Serializable]
    public class RedrawHandEffect : BattleEffect
    {
        [Tooltip(
            "Ask first: the player picks any card to redraw the whole hand, or confirms with "
                + "none to keep it."
        )]
        [SerializeField]
        private bool _asOptionalOffer = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Deck == null || ctx.Deck.HandCount == 0)
                return;

            if (!_asOptionalOffer)
            {
                Redraw(ctx);
                return;
            }

            // The choice panel picks cards, so "redraw?" is asked as "pick any card".
            ResolveCardSelection(
                ctx.Deck.Hand,
                CardSelectionMode.PlayerChoice,
                CardType.Pressure, // ignored by PlayerChoice
                "Mulligan: pick any card to redraw your whole hand, or confirm none to keep it",
                1,
                chosen =>
                {
                    if (chosen.Count > 0)
                        Redraw(ctx);
                },
                allowFewer: true
            );
        }

        private static void Redraw(EffectExecutionContext ctx)
        {
            var hand = new List<CardData>(ctx.Deck.Hand);
            foreach (var card in hand)
                ctx.Deck.DiscardCard(card);
            int drawn = ctx.Deck.DrawCards(hand.Count);
            GameLogger.LogInfo<RedrawHandEffect>(
                $"Redrew the hand: discarded {hand.Count}, drew {drawn}"
            );
        }

        public override string GetDescription() =>
            _asOptionalOffer
                ? "You may discard your hand and draw that many cards"
                : "Discard your hand and draw that many cards";
    }
}
