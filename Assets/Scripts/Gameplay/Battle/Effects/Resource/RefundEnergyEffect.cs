using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Refunds energy for the latest card play (Old Boys' Club). When
    /// <see cref="NepoBabyConfig.RefundCappedAtEnergyPaid"/> is on, the refund never exceeds the
    /// energy actually paid, so a 0-cost or free card refunds nothing.
    /// </summary>
    [Serializable]
    public class RefundEnergyEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Energy refunded.")]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            if (NepoBabyConfig.Current.RefundCappedAtEnergyPaid)
                amount = Mathf.Min(amount, ctx.BattleManager?.CardPlay?.LastEnergyPaid ?? 0);
            if (amount > 0)
                ctx.PlayerStats.GainActionPoints(amount);
        }

        public override string GetDescription() =>
            $"Refund {_amount} energy (never more than you paid)";
    }
}
