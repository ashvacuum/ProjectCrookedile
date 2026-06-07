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
        [MinValue(1)]
        [SerializeField]
        private int _amount = 5;

        [Tooltip(
            "Where to read the amount from at runtime. FixedAmount uses the authored Amount field."
        )]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = ResolveAmount(ctx, amountOverride, _amount, _amountSource);

            if (amount <= 0)
                return;

            ctx.BattleManager?.RaiseOpinion(amount);
            GameLogger.LogInfo<RaiseOpinionEffect>($"Raised Opinion by {amount}");
        }

        public override string GetDescription() =>
            $"Raise Opinion by {DescribeAmount(_amount, _amountSource)}";
    }
}
