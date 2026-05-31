using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Opinion equal to the current session Support value.
    /// All pressure modifiers still apply.
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
        [Tooltip("Who receives the pressure.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int support = ctx.BattleManager?.CurrentSupport ?? 0;
            foreach (var (target, _) in ctx.GetTargets(_target))
                ApplyResolveDamage(target, ctx.Caster, support, ctx);
        }

        public override string GetDescription() => "Raise Opinion equal to your Support";
    }
}
