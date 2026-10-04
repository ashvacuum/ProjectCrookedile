using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class DebtWaiverEffect : BattleEffect
    {
        [Tooltip("Upcoming unpaid-Debt damage instances negated this battle.")]
        [MinValue(1)]
        [SerializeField]
        private int _waiverCount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state == null)
                return;

            int amount = amountOverride ?? _waiverCount;
            state.DebtWaivers += amount;
        }

        public override string GetDescription() =>
            _waiverCount == 1
                ? "The first time this fight unpaid Debt would deal Opinion damage, it deals none"
                : $"The first {_waiverCount} times this fight unpaid Debt would deal Opinion damage, it deals none";
    }
}
