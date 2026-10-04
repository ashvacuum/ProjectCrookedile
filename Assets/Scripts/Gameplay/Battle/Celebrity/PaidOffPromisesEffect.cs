using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class PaidOffPromisesEffect : BattleEffect
    {
        [Tooltip("Maximum existing Debt reserved until the end of next turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _debtAmount = 3;

        [Tooltip(
            "Minimum printed energy cost required to fulfil the promise next turn; X uses energy actually spent."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _minimumPrintedCost = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            ctx.BattleManager.Celebrity.MakePromise(_debtAmount, _minimumPrintedCost);
        }

        public override string GetDescription() =>
            $"Defer up to {_debtAmount} Debt until the end of your next turn. Play a card with printed cost {_minimumPrintedCost}+ next turn to cancel it (X uses energy spent)";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_debtAmount <= 0 || _minimumPrintedCost <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
