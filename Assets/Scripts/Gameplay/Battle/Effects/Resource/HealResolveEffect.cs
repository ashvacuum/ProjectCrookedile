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
    /// Does not set <c>ctx.LastHealAmount</c>; use <see cref="RestoreResolveEffect"/> if chaining off that value.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class HealResolveEffect : BattleEffect
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
            GameLogger.LogInfo<HealResolveEffect>($"Raised Opinion by {amount}");
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
