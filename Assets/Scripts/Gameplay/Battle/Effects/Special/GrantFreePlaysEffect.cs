using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class GrantFreePlaysEffect : BattleEffect
    {
        [Tooltip("Next cards played from hand this turn that cost zero energy.")]
        [MinValue(1)]
        [SerializeField]
        private int _cardCount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var cardPlay = ctx.BattleManager?.CardPlay;
            if (cardPlay == null)
                return;

            int amount = amountOverride ?? _cardCount;
            cardPlay.GrantFreePlays(amount);
        }

        public override string GetDescription() =>
            _cardCount == 1
                ? "Your next card this turn costs 0"
                : $"Your next {_cardCount} cards this turn cost 0";
    }
}
