using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Arms a price break or an extra play for the next card(s) played from hand this turn.
    /// Everything armed here expires at the start of the next player turn.
    /// </summary>
    [Serializable]
    public class TurnPriceBreakEffect : BattleEffect
    {
        public enum PriceBreak
        {
            NextCardsCostZero, // the next N cards played this turn cost 0 (Legacy Admission)
            NextCardDiscount, // the next card played this turn costs N less (Smooth Operator)
            NextCardPlaysAgain, // the next non-Policy card played this turn replays N times (Executive Privilege)
        }

        [Tooltip("What the next card(s) get.")]
        [SerializeField]
        private PriceBreak _kind = PriceBreak.NextCardsCostZero;

        [MinValue(1)]
        [Tooltip("How many cards cost 0, how much the discount is, or how many extra plays.")]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var cardPlay = ctx.BattleManager?.CardPlay;
            if (cardPlay == null)
                return;
            int amount = amountOverride ?? _amount;
            switch (_kind)
            {
                case PriceBreak.NextCardsCostZero:
                    cardPlay.GrantFreePlays(amount);
                    break;
                case PriceBreak.NextCardDiscount:
                    cardPlay.DiscountNextCard(amount);
                    break;
                case PriceBreak.NextCardPlaysAgain:
                    cardPlay.ArmReplayOfNextCard(amount);
                    break;
            }
        }

        public override string GetDescription() =>
            _kind switch
            {
                PriceBreak.NextCardsCostZero => _amount == 1
                    ? "Your next card this turn costs 0"
                    : $"Your next {_amount} cards this turn cost 0",
                PriceBreak.NextCardDiscount => $"Your next card this turn costs {_amount} less",
                _ => _amount == 1
                    ? "Your next card this turn is played twice"
                    : $"Your next card this turn is played {_amount + 1} times",
            };
    }
}
