using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class LineOfCreditEffect : BattleEffect
    {
        [Tooltip("Debt cancelled from the next positive Debt gain this turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _debtReduction = 2;

        [Tooltip("Energy discount applied to a random card in hand this turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _cardDiscount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            ctx.BattleManager.Celebrity.ArmCredit(_debtReduction, _cardDiscount);
        }

        public override string GetDescription() =>
            $"The next time you take on Debt this turn, cancel up to {_debtReduction} of that new Debt and discount a random card in hand by {_cardDiscount} this turn";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_debtReduction <= 0 || _cardDiscount <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
