using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Starts a three-turn scandal on a non-hostile opponent.</summary>
    [Serializable]
    public sealed class PoisonPerceptionEffect : BattleEffect
    {
        [Tooltip("Enemy turns before the debuff expires.")]
        [Min(1)]
        [SerializeField]
        private int _turns = 3;

        [Tooltip("Hostility added at each enemy-turn start, respecting normal resistance.")]
        [Min(1)]
        [SerializeField]
        private int _hostilityPerTurn = 2;

        [Tooltip("Sway dealt once when the afflicted opponent becomes hostile.")]
        [Min(1)]
        [SerializeField]
        private int _swayOnHostile = 10;

        public override TargetType Target => TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.Target == null || ctx.Target.IsHostile)
                return;

            // Reapplication refreshes the three-turn deadline rather than adding another payoff.
            ctx.TargetStatusEffects?.RemoveStacksNotify<PoisonedPerceptionStatus>(int.MaxValue);
            ctx.TargetStatusEffects?.ApplyStatus(
                new PoisonedPerceptionStatus(_hostilityPerTurn, _swayOnHostile),
                _turns,
                StatusDurationType.DecreasePerTurn
            );
        }

        public override string GetDescription() =>
            $"Apply Poisoned Perception to a non-hostile opponent: +{_hostilityPerTurn} Hostility each enemy turn for {_turns} turns; becoming hostile removes it and deals {_swayOnHostile} Sway";

        internal static PoisonedPerceptionStatus GetBehavior(StatusEffectManager statuses)
        {
            foreach (var status in statuses.ActiveEffects)
                if (status.Behavior is PoisonedPerceptionStatus poison)
                    return poison;
            return null;
        }

        internal static void ResolvePayoff(EffectExecutionContext ctx)
        {
            if (
                ctx.TargetStatusEffects.GetStacks<PoisonedPerceptionStatus>() <= 0
                || !ctx.Target.IsHostile
            )
                return;

            int sway = GetBehavior(ctx.TargetStatusEffects).SwayOnHostile;
            ctx.TargetStatusEffects.RemoveStacksNotify<PoisonedPerceptionStatus>(int.MaxValue);
            ApplyOpinion(ctx.Target, ctx.Caster, sway, ctx);
        }
    }
}
