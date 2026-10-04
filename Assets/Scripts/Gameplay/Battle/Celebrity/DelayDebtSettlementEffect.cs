using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class DelayDebtSettlementEffect : BattleEffect
    {
        [Tooltip("Upcoming normal Debt settlements to skip; promise deadlines are unaffected.")]
        [MinValue(1)]
        [SerializeField]
        private int _settlementsToDelay = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;

            int amount = amountOverride ?? _settlementsToDelay;
            state.SettlementsDelayed += amount;
        }

        public override string GetDescription() =>
            _settlementsToDelay == 1
                ? "Delay Debt settlement by 1 turn"
                : $"Delay Debt settlement by {_settlementsToDelay} turns";
    }
}
