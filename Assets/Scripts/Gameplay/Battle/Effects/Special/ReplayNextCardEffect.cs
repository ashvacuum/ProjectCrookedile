using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class ReplayNextCardEffect : BattleEffect
    {
        [Tooltip("Extra plays of the next non-Policy card played from hand this turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _extraPlays = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var cardPlay = ctx.BattleManager?.CardPlay;
            if (cardPlay == null)
                return;

            int amount = amountOverride ?? _extraPlays;
            cardPlay.ArmReplayOfNextCard(amount);
        }

        public override string GetDescription() =>
            _extraPlays == 1
                ? "Your next card this turn is played twice"
                : $"Your next card this turn is played {_extraPlays + 1} times";
    }
}
