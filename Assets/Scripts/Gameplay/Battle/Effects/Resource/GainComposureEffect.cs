using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants Composure to the caster. Respects Dexterity/Frail status modifiers.
    /// The amount can be sourced from the runtime context (e.g. equal to last damage dealt).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class GainComposureEffect : BattleEffect
    {
        [Tooltip("Base Composure to gain. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 3;

        [Tooltip(
            "Where to read the Composure amount from at runtime. FixedAmount uses the authored Amount field."
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
            ApplyGainComposure(ctx.Caster, amount, ctx);
        }

        public override string GetDescription()
        {
            string amountStr =
                _amountSource == EffectContextValue.FixedAmount
                    ? _amount.ToString()
                    : _amountSource.ToString();
            return $"Gain {amountStr} Composure";
        }
    }
}
