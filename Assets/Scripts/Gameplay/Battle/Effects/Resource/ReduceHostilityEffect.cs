using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Reduces the focused enemy's Hostility by the given amount.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ReduceHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField] private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            int actual = ctx.Target.ReduceHostility(amount);
            GameLogger.LogInfo<ReduceHostilityEffect>($"Reduced {actual} Hostility");
        }

        public override string GetDescription() => $"Reduce target's Hostility by {_amount}";
    }
}
