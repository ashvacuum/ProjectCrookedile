using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class OpenTabEffect : BattleEffect
    {
        [Tooltip("Free Borrow cards per turn for the rest of the battle.")]
        [MinValue(1)]
        [SerializeField]
        private int _freeBorrowsPerTurn = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;

            int amount = amountOverride ?? _freeBorrowsPerTurn;
            state.FreeBorrowsPerTurn += amount;
        }

        public override string GetDescription() =>
            _freeBorrowsPerTurn == 1
                ? "The first Borrow card each turn costs 0"
                : $"The first {_freeBorrowsPerTurn} Borrow cards each turn cost 0";
    }
}
