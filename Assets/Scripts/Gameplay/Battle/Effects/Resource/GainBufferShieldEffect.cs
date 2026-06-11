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
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class GainBufferShieldEffect : BattleEffect
    {
        [Tooltip("Base amount to gain. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 3;

        [Tooltip("Where to read the amount from at runtime.")]
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

        /// <summary>Authored fixed Support amount, for editor/preview display. 0 when context-sourced.</summary>
        public int PreviewSupportAmount =>
            _amountSource == EffectContextValue.FixedAmount ? _amount : 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = ResolveScaledAmount(ctx, amountOverride, _amount, _amountSource, _perXSource, _multiplier);

            if (ctx.IsPlayerCard)
                ApplyGainSupport(amount, ctx);
            else
                ApplyGainDenial(amount, ctx);
        }

        public override string GetDescription()
        {
            return $"Gain {DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier)} Support";
        }
    }
}
