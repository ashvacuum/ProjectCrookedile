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

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int baseDamage =
                amountOverride
                ?? (
                    _amountSource == EffectContextValue.FixedAmount
                        ? _amount
                        : ctx.GetValue(_amountSource)
                );

            BattleStats pressureTarget = ctx.IsPlayerCard ? ctx.Target : ctx.PlayerStats;
            ApplyPressure(pressureTarget, ctx.Caster, baseDamage, ctx);
        }

        public override string GetDescription()
        {
            string amountStr =
                _amountSource == EffectContextValue.FixedAmount
                    ? _amount.ToString()
                    : _amountSource.ToString();
            return $"Apply {amountStr} pressure to the Opinion Meter";
        }
    }
}
