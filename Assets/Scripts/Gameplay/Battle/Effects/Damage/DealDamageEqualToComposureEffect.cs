using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Opinion equal to the caster's current Shield (Support).
    /// All pressure modifiers (Strength, Vulnerable, Hostility multiplier) still apply.
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
            int shield = ctx.Caster.CurrentShield;
            foreach (var (target, _) in ctx.GetTargets(_target))
                ApplyResolveDamage(target, ctx.Caster, shield, ctx);
        }

        public override string GetDescription() =>
            $"Raise Opinion equal to your Support (through {_target}'s Denial)";
    }
}
