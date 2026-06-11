using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Celebrity "court attention" — banks Attention into the player's spotlight pool. Attention is
    /// the build half of the Attention/Aggro line: accumulate it now (usually by provoking the room),
    /// then detonate it later with <see cref="SpendAttentionEffect"/> as a big opinion-meter hit.
    /// </summary>
    [Serializable]
    public class GainAttentionEffect : BattleEffect
    {
        [Tooltip("Base Attention to bank. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

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

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = ResolveScaledAmount(ctx, amountOverride, _amount, _amountSource, _perXSource, _multiplier);
            if (amount <= 0)
                return;
            ctx.BattleManager?.GainAttention(amount);
            GameLogger.LogInfo<GainAttentionEffect>($"Banked {amount} Attention");
        }

        public override string GetDescription() =>
            $"Gain {DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier)} Attention";
    }
}
