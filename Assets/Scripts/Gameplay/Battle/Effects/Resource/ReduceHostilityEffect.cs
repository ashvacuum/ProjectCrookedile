using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Reduces Hostility (de-escalates) on the chosen target(s). With Adjacent / AllHostile this
    /// calms a group; Hardened enemies ignore the reduction.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ReduceHostilityEffect : BattleEffect
    {
        [Tooltip("Base Hostility to remove. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        [Tooltip("Where to read the amount from at runtime.")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip(
            "Optional scaling: multiply the amount by this context value. None = no scaling."
        )]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        [Tooltip(
            "Which enemies to de-escalate. Opponent = the focused enemy; Adjacent / AllHostile / "
                + "AllOpponents calm groups."
        )]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = ResolveScaledAmount(
                ctx,
                amountOverride,
                _amount,
                _amountSource,
                _perXSource,
                _multiplier
            );
            if (amount <= 0)
                return;
            int total = 0;
            foreach (var (stats, _) in ctx.GetTargets(_target))
                total += stats.ReduceHostility(amount);
            GameLogger.LogInfo<ReduceHostilityEffect>($"Reduced {total} Hostility ({_target})");
        }

        public override string GetDescription()
        {
            string amountStr = DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier);
            return _target == TargetType.Opponent
                ? $"Reduce target's Hostility by {amountStr}"
                : $"Reduce Hostility by {amountStr} ({_target})";
        }
    }
}
