using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Shifts Hostility on the chosen target(s) by a SIGNED amount — the converged replacement for
    /// RaiseTargetHostility / ReduceHostility / RaiseAllOpponentsHostility.
    ///
    /// Positive = rile (routes through GainHostility — blocked by Fanatic).
    /// Negative = de-escalate (routes through ReduceHostility — blocked by Hardened).
    /// The two directions keep their asymmetric guards, so this is NOT the same as GainHostility(-n).
    ///
    /// "Lower all opponents' Hostility" = target AllOpponents with a negative amount.
    /// </summary>
    [Serializable]
    public class ShiftHostilityEffect : BattleEffect
    {
        [Tooltip(
            "Signed Hostility shift. Positive = rile up (respects Fanatic); "
                + "negative = de-escalate (respects Hardened). Ignored when Amount Source is not Fixed."
        )]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [SerializeField]
        private int _amount = 2;

        [Tooltip("Where to read the amount from at runtime.")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip("Optional scaling: multiply the amount by this context value. None = no scaling.")]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        [Tooltip(
            "Which enemies to shift. Opponent = focused enemy; Adjacent / AllHostile / AllReceptive / "
                + "RandomReceptive (Sway) / AllOpponents address groups."
        )]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

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
            if (amount == 0)
                return;

            int touched = 0;
            foreach (var (stats, _) in ctx.GetTargets(_target))
            {
                if (amount > 0)
                    stats.GainHostility(amount); // rile — Fanatic blocks
                else
                    stats.ReduceHostility(-amount); // de-escalate — Hardened blocks
                touched++;
            }

            GameLogger.LogInfo<ShiftHostilityEffect>(
                $"Shifted Hostility by {amount} on {touched} target(s) ({_target})"
            );
        }

        public override string GetDescription()
        {
            string verb = _amount < 0 ? "Lower" : "Raise";
            string amountStr = DescribeScaledAmount(
                Mathf.Abs(_amount),
                _amountSource,
                _perXSource,
                _multiplier
            );
            return _target == TargetType.Opponent
                ? $"{verb} target's Hostility by {amountStr}"
                : $"{verb} Hostility by {amountStr} ({_target})";
        }
    }
}
