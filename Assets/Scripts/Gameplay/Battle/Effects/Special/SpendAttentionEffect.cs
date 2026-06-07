using System;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Celebrity cash-out for the Attention/Aggro line — spends banked Attention as a big opinion
    /// hit ("everyone's talking about me" → political gain). Consumes the Attention and raises Opinion
    /// by the amount spent times <see cref="_opinionPerAttention"/>. No-op when nothing is banked.
    /// </summary>
    [Serializable]
    public class SpendAttentionEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Opinion raised per Attention spent.")]
        [SerializeField]
        private int _opinionPerAttention = 1;

        [MinValue(0)]
        [Tooltip("Attention to spend. 0 = spend all currently banked.")]
        [SerializeField]
        private int _attentionToSpend = 0;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.BattleManager == null)
                return;

            int banked = ctx.BattleManager.CurrentAttention;
            int spend = _attentionToSpend <= 0 ? banked : Mathf.Min(_attentionToSpend, banked);
            if (spend <= 0)
                return;

            ctx.BattleManager.SpendAttention(spend);
            int burst = spend * _opinionPerAttention;
            ctx.BattleManager.RaiseOpinion(burst);
            ctx.LastHealAmount += burst;
            GameLogger.LogInfo<SpendAttentionEffect>(
                $"Spent {spend} Attention → +{burst} Opinion"
            );
        }

        public override string GetDescription() =>
            _attentionToSpend <= 0
                ? $"Spend all banked Attention; raise Opinion by {_opinionPerAttention} per Attention spent"
                : $"Spend up to {_attentionToSpend} Attention; raise Opinion by {_opinionPerAttention} per Attention spent";
    }
}
