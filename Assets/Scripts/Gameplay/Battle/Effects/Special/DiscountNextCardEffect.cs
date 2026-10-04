using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class DiscountNextCardEffect : BattleEffect
    {
        [Tooltip("Energy discount on the next card played from hand this turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _discount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var cardPlay = ctx.BattleManager?.CardPlay;
            if (cardPlay == null)
                return;

            int amount = amountOverride ?? _discount;
            cardPlay.DiscountNextCard(amount);
        }

        public override string GetDescription() =>
            $"Your next card this turn costs {_discount} less";
    }
}
