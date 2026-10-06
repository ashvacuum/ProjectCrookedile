using System;
using System.Collections.Generic;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class CharmOffensiveEffect : BattleEffect
    {
        [Tooltip(
            "Sway and Support gained per Energy actually spent; Hostility falls by one per energy spent."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _swayAndComposurePerEnergy = 2;

        public override TargetType Target => TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            int x = ctx.BattleManager.CardPlay.LastEnergyPaid;
            ApplyOpinion(ctx.Target, ctx.Caster, x * _swayAndComposurePerEnergy, ctx);
            ApplyGainSupport(x * _swayAndComposurePerEnergy, ctx);
            ctx.LastHostilityLost += ctx.Target.ReduceHostility(x);
        }

        public override string GetDescription() =>
            $"Deal {_swayAndComposurePerEnergy}X Sway, gain {_swayAndComposurePerEnergy}X Support, Soothe X (X = Energy spent)";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_swayAndComposurePerEnergy <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
