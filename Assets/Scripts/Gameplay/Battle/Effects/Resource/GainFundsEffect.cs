using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Adds campaign Funds to the run from inside a battle. No-op without a run (standalone test
    /// battles). ponytail: paid the moment it fires, not banked until day end; add a pending-Funds
    /// field on RunState if the timing ever matters.
    /// </summary>
    [Serializable]
    public class GainFundsEffect : BattleEffect
    {
        [Tooltip("Funds added to the run each time this fires.")]
        [Min(1)]
        [SerializeField]
        private int _amount = 10;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            RunState.Current?.AdjustFunds(amountOverride ?? _amount);

        public override string GetDescription() => $"Gain {_amount} Funds";
    }
}
