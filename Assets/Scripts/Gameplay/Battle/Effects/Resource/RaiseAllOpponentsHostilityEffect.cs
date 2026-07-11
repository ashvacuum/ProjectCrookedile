using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises the Hostility of every living enemy simultaneously.
    /// Intended for use on Status card passives (e.g. Hounded: end-of-turn trigger)
    /// and any enemy move that wants to agitate the whole room at once.
    ///
    /// Uses TargetType.AllOpponents, which resolves to all non-defeated enemies
    /// when the source is a player card/passive.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseAllOpponentsHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 3;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;

            var targets = ctx.GetTargets(TargetType.AllOpponents);
            int count = 0;
            foreach (var (stats, _) in targets)
            {
                ctx.LastHostilityGained += stats.GainHostility(amount);
                count++;
            }

            GameLogger.LogInfo<RaiseAllOpponentsHostilityEffect>(
                $"Raised Hostility on {count} opponent(s) by {amount}"
            );
        }

        public override string GetDescription() => $"Raise all enemies' Hostility by {_amount}";
    }
}
