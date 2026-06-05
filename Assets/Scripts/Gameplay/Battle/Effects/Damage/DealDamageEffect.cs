using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Applies pressure to the shared Opinion Meter by a fixed or context-sourced amount.
    /// Player cards raise opinion (routed through Denial); enemy cards lower opinion (routed through Support).
    /// Direction is determined by <see cref="EffectExecutionContext.IsPlayerCard"/> — no target field needed.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DealDamageEffect : BattleEffect
    {
        [Tooltip("Base pressure amount. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 5;

        [Tooltip(
            "Where to read the pressure amount from at runtime.\n"
                + "FixedAmount = use the authored Amount field.\n"
                + "Other options read accumulated values from the effect context (e.g. LastDamageDealt for chaining)."
        )]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip(
            "Which part of the crowd this pressure addresses (player cards). Opponent = the focused "
                + "enemy; Adjacent = focused + neighbours; AllHostile / AllReceptive / AllOpponents = "
                + "the matching group. Amount is applied per target. Enemy cards always pressure the player."
        )]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        /// <summary>Exposes the chosen pattern so single-target (Opponent) cards still bump hostility.</summary>
        public override TargetType Target => _target;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int baseDamage = ResolveAmount(ctx, amountOverride, _amount, _amountSource);

            if (ctx.IsPlayerCard)
            {
                // Address the chosen part of the crowd — pressure applies per target.
                foreach (var (targetStats, _) in ctx.GetTargets(_target))
                    ApplyPressure(targetStats, ctx.Caster, baseDamage, ctx);
            }
            else
            {
                // Enemy cards always pressure the player.
                ApplyPressure(ctx.PlayerStats, ctx.Caster, baseDamage, ctx);
            }
        }

        public override DamagePreview? GetDamagePreview() =>
            _amountSource == EffectContextValue.FixedAmount
                ? new DamagePreview { Type = DamagePreviewType.Fixed, Amount = _amount }
                : null;

        public override string GetDescription()
        {
            string amountStr = DescribeAmount(_amount, _amountSource);
            string targetStr = _target switch
            {
                TargetType.Opponent => "",
                TargetType.Adjacent => " to the target and its neighbours",
                TargetType.AllHostile => " to all hostile enemies",
                TargetType.AllReceptive => " to all receptive enemies",
                TargetType.AllOpponents => " to all enemies",
                _ => $" ({_target})",
            };
            return $"Apply {amountStr} pressure to the Opinion Meter{targetStr}";
        }
    }
}
