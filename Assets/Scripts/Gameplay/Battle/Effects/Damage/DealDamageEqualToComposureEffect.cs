using System;
using UnityEngine;
using Crookedile.Data;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Deals Resolve damage equal to the caster's current Composure.
    /// All damage modifiers (Strength, Vulnerable, Hostility multiplier) still apply.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DealDamageEqualToComposureEffect : BattleEffect
    {
        [Tooltip("Who receives the damage.")]
        [SerializeField] private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int composure = ctx.Caster.CurrentComposure;
            foreach (var (target, _) in ctx.GetTargets(_target))
                ApplyResolveDamage(target, ctx.Caster, composure, ctx);
        }

        public override string GetDescription() =>
            $"Deal damage equal to your Composure to {_target}";
    }
}
