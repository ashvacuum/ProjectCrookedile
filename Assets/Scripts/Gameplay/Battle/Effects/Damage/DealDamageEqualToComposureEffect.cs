using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Opinion equal to the current session Support value.
    /// All pressure modifiers still apply.
    /// Direction is always player → opinion up (Support value is a player resource).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "DealDamageEqualToComposureEffect"
    )]
    public class RaiseOpinionEqualToShieldEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int support = ctx.BattleManager?.CurrentSupport ?? 0;
            ApplyPressure(ctx.Target, ctx.Caster, support, ctx);
        }

        public override string GetDescription() => "Raise Opinion equal to your Support";
    }
}
