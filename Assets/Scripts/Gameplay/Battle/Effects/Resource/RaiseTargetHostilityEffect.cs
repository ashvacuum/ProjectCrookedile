using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Hostility on the chosen target(s), making them deal more damage.
    /// With <see cref="TargetType.RandomReceptive"/> (and enough amount to cross 0) this is a Sway —
    /// converting a receptive enemy to hostile, which can trigger the Turncoat cascade.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseTargetHostilityEffect : BattleEffect
    {
        [Tooltip("Base Hostility to add. Ignored when Amount Source is not Fixed.")]
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
            "Which enemies to rile. Opponent = the focused enemy; Adjacent / AllReceptive / "
                + "RandomReceptive (Sway) / AllOpponents address groups."
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
            foreach (var (stats, _) in ctx.GetTargets(_target))
                stats.GainHostility(amount);
            GameLogger.LogInfo<RaiseTargetHostilityEffect>(
                $"Raised Hostility by {amount} ({_target})"
            );
        }

        public override string GetDescription()
        {
            string amountStr = DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier);
            return _target == TargetType.Opponent
                ? $"Raise target's Hostility by {amountStr}"
                : $"Raise Hostility by {amountStr} ({_target})";
        }
    }
}
