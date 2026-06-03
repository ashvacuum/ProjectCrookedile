using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants session-level Support (player) or Denial (enemy) depending on who casts it.
    /// Respects Dexterity/Frail modifiers when the player gains Support.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "GainComposureEffect"
    )]
    public class GainShieldEffect : BattleEffect
    {
        [Tooltip("Base amount to gain. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 3;

        [Tooltip("Where to read the amount from at runtime.")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        /// <summary>Authored fixed Support amount, for editor/preview display. 0 when context-sourced.</summary>
        public int PreviewSupportAmount =>
            _amountSource == EffectContextValue.FixedAmount ? _amount : 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount =
                amountOverride
                ?? (
                    _amountSource == EffectContextValue.FixedAmount
                        ? _amount
                        : ctx.GetValue(_amountSource)
                );

            if (ctx.IsPlayerCard)
                ApplyGainSupport(amount, ctx);
            else
                ApplyGainDenial(amount, ctx);
        }

        public override string GetDescription()
        {
            string amountStr =
                _amountSource == EffectContextValue.FixedAmount
                    ? _amount.ToString()
                    : _amountSource.ToString();
            return $"Gain {amountStr} Support";
        }
    }
}
