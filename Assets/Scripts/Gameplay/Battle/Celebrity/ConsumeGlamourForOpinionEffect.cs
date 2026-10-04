using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Trades future Glamour ticks for one immediate pressure hit, subject to the usual shields and modifiers.</summary>
    [Serializable]
    public sealed class ConsumeGlamourForOpinionEffect : BattleEffect
    {
        [Tooltip(
            "Sway per Glamour consumed. All current Glamour is spent before the hit resolves."
        )]
        [Min(1)]
        [SerializeField]
        private int _swayPerGlamour = 2;

        public override TargetType Target => TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.PlayerStatusEffects == null)
                return;

            int glamour = ctx.PlayerStatusEffects.GetStacks<GlamourStatus>();
            if (glamour <= 0)
                return;

            ctx.PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(glamour);
            ApplyOpinion(ctx.Target, ctx.Caster, glamour * _swayPerGlamour, ctx);
        }

        public override string GetDescription() =>
            $"Consume all Glamour. Deal {_swayPerGlamour} Sway per Glamour consumed";
    }
}
