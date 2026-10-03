using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Moves Hostility off the focused enemy onto another living enemy ("Not My Problem"): what
    /// the focused enemy actually loses is what the other one gains. Hardened or Warded enemies
    /// can stop either half.
    /// ponytail: the receiving enemy is picked automatically (a random other enemy) — the
    /// card-choice panel picks cards, not enemies; let the player choose once targeting can.
    /// </summary>
    [Serializable]
    public class MoveHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("The most Hostility to move.")]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.Target == null || ctx.AllEnemies == null)
                return;

            var others = new List<EnemyController>();
            foreach (var enemy in ctx.AllEnemies)
                if (!enemy.IsDefeated && enemy.Stats != ctx.Target)
                    others.Add(enemy);
            if (others.Count == 0)
                return;

            int moved = ctx.Target.ReduceHostility(amountOverride ?? _amount);
            if (moved <= 0)
                return;
            var receiver = others[RandomHelper.Range(0, others.Count)];
            receiver.Stats.GainHostility(moved);
            ctx.LastHostilityLost += moved;
            GameLogger.LogInfo<MoveHostilityEffect>(
                $"Moved {moved} Hostility onto {receiver.EnemyData.EnemyName}"
            );
        }

        public override string GetDescription() =>
            $"Move up to {_amount} Hostility from the target to another enemy";
    }
}
