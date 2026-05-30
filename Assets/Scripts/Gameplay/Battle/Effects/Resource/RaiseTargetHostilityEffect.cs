using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises the focused enemy's Hostility, making them deal more damage.
    /// Higher Hostility increases the enemy's damage multiplier.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseTargetHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            ctx.Target.GainHostility(amount);
            GameLogger.LogInfo<RaiseTargetHostilityEffect>(
                $"Raised target Hostility by {amount} (now {ctx.Target.CurrentHostility})"
            );
        }

        public override string GetDescription() => $"Raise target's Hostility by {_amount}";
    }
}
