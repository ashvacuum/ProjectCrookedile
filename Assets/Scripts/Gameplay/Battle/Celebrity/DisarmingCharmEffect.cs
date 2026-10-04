using System;
using System.Collections.Generic;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class DisarmingCharmEffect : BattleEffect
    {
        [Tooltip("Glamour spent to weaken a hostile opponent.")]
        [MinValue(1)]
        [SerializeField]
        private int _glamourCost = 2;

        [Tooltip("Permanent Weakened stacks applied after paying Glamour.")]
        [MinValue(1)]
        [SerializeField]
        private int _weakenedStacks = 3;

        public override TargetType Target => TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            if (
                ctx.Target == null
                || !ctx.Target.IsHostile
                || ctx.TargetStatusEffects == null
                || ctx.PlayerStatusEffects.GetStacks<GlamourStatus>() < _glamourCost
            )
                return;

            ctx.PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(_glamourCost);
            ctx.TargetStatusEffects.ApplyStatus(
                StatusRegistry.Get<WeakenedStatus>(),
                _weakenedStacks,
                StatusDurationType.Permanent
            );
        }

        public override string GetDescription() =>
            $"Spend {_glamourCost} Glamour to apply {_weakenedStacks} Weakened to a hostile opponent";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_glamourCost <= 0 || _weakenedStacks <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
