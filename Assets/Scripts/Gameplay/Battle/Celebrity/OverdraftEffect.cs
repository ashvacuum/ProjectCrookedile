using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class OverdraftEffect : BattleEffect
    {
        [Tooltip(
            "Extra multiples of Borrow energy and Debt this turn; 1 doubles both, 2 triples both."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _extraBorrowMultiples = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;

            int amount = amountOverride ?? _extraBorrowMultiples;
            state.BorrowBonusMultiplesThisTurn += amount;
        }

        public override string GetDescription() =>
            _extraBorrowMultiples == 1 ? "This turn Borrow cards give double energy and double Debt"
            : _extraBorrowMultiples == 2
                ? "This turn Borrow cards give triple energy and triple Debt"
            : $"This turn Borrow cards give {_extraBorrowMultiples + 1}x energy and Debt";
    }
}
