using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Directly raises the Opinion Meter by a fixed or context-sourced amount, bypassing Denial,
    /// and records the amount in <c>ctx.LastHealAmount</c> for downstream effect chaining.
    /// Use when a subsequent effect needs to read how much opinion was restored (e.g. lifegain → shield).
    /// For raises that don't need chaining, use <see cref="RaiseOpinionEffect"/> instead.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Crookedile.Runtime",
        "RestoreResolveEffect"
    )]
    public class RaiseOpinionTrackedEffect : BattleEffect
    {
        [Tooltip("Amount of Opinion to raise directly. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 5;

        [Tooltip(
            "Where to read the heal amount from at runtime. FixedAmount uses the authored Amount field."
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

            ctx.BattleManager?.RaiseOpinion(amount);
            GameLogger.LogInfo<RaiseOpinionTrackedEffect>($"Raised Opinion by {amount}");
            ctx.LastHealAmount += amount;
        }

        public override string GetDescription() =>
            $"Raise Opinion by {DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier)}";
    }
}
