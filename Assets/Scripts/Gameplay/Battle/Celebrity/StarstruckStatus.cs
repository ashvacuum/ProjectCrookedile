using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Lands only on a non-hostile opponent. Stacks are remaining enemy turns; each enemy-turn start
    /// Aggravates. Becoming hostile consumes every stack before the one-time Sway payoff.
    /// </summary>
    [Serializable]
    public sealed class StarstruckStatus : StatusBehavior
    {
        [Tooltip("Hostility added at each enemy-turn start, respecting normal resistance.")]
        [Min(1)]
        [SerializeField]
        private int _hostilityPerTurn = 2;

        [Tooltip("Sway dealt once when the afflicted opponent becomes hostile.")]
        [Min(1)]
        [SerializeField]
        private int _swayOnHostile = 10;

        public int HostilityPerTurn => _hostilityPerTurn;
        public int SwayOnHostile => _swayOnHostile;

        // Ids are permanent, so this one keeps the status's original name.
        public override string Id => "poisoned_perception";
        public override string DisplayName => "Starstruck";
        public override bool IsDebuff => true;
        public override bool RefreshesOnReapply => true;

        public override bool CanApplyTo(BattleStats owner) => owner == null || !owner.IsHostile;

        public override string Describe(int stacks) =>
            $"Aggravate {_hostilityPerTurn} at the start of each enemy turn for {stacks} turns. Becoming hostile removes this and deals {_swayOnHostile} Sway.";

        internal static StarstruckStatus Find(StatusEffectManager statuses)
        {
            foreach (var status in statuses.ActiveEffects)
                if (status.Behavior is StarstruckStatus starstruck)
                    return starstruck;
            return null;
        }

        internal static void ResolvePayoff(EffectExecutionContext ctx)
        {
            var starstruck = Find(ctx.TargetStatusEffects);
            if (starstruck == null || !ctx.Target.IsHostile)
                return;

            ctx.TargetStatusEffects.RemoveStacksNotify<StarstruckStatus>(int.MaxValue);
            BattleEffect.ApplyOpinion(ctx.Target, ctx.Caster, starstruck._swayOnHostile, ctx);
        }
    }
}
