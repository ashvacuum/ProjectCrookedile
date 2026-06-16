using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Directly raises the Opinion Meter by a fixed or context-sourced amount, bypassing Denial.
    /// Use for cards that win the crowd over without going through the damage pipeline —
    /// rallying speeches, concessions, crowd appeals, etc.
    /// Does not set <c>ctx.LastHealAmount</c>; use <see cref="RaiseOpinionTrackedEffect"/> if chaining off that value.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Crookedile.Runtime",
        "HealResolveEffect"
    )]
    public class RaiseOpinionEffect : BattleEffect
    {
        [Tooltip("Opinion to raise. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [SerializeField]
        private int _amount = 5;

        [Tooltip(
            "Where to read the amount from at runtime. FixedAmount uses the authored Amount field."
        )]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip(
            "Optional scaling: multiply the amount by this context value "
                + "(e.g. HostileEnemyCount = 'per hostile enemy'). None = no scaling."
        )]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = ResolveScaledAmount(ctx, amountOverride, _amount, _amountSource, _perXSource, _multiplier);

            if (amount <= 0)
                return;

            ApplyPressure(ctx.Target, ctx.Caster, amount, ctx);
            GameLogger.LogInfo<RaiseOpinionEffect>($"Raised Opinion by {amount}");
        }

        public override string GetDescription() =>
            $"Raise Opinion by {DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier)}";
    }
}
