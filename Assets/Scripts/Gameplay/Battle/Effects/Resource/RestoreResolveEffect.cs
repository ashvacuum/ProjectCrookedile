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
    /// For raises that don't need chaining, use <see cref="HealResolveEffect"/> instead.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RestoreResolveEffect : BattleEffect
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

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount =
                amountOverride
                ?? (
                    _amountSource == EffectContextValue.FixedAmount
                        ? _amount
                        : ctx.GetValue(_amountSource)
                );

            if (amount <= 0)
                return;

            EventBus.Publish(new OpinionRaisedDirectlyEvent { Amount = amount });
            GameLogger.LogInfo<RestoreResolveEffect>($"Raised Opinion by {amount}");
            ctx.LastHealAmount += amount;
        }

        public override string GetDescription()
        {
            string amountStr =
                _amountSource == EffectContextValue.FixedAmount
                    ? _amount.ToString()
                    : _amountSource.ToString();
            return $"Raise Opinion by {amountStr}";
        }
    }
}
