using System;
using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Shared walk over the caster's living allies (every living enemy except the caster).
    /// With no roster passed in, there is nothing to read, so these conditions report false.
    /// </summary>
    internal static class RoomState
    {
        public static int CountAllies(
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self,
            Func<EnemyController, bool> match
        )
        {
            if (allEnemies == null)
                return 0;
            int count = 0;
            foreach (var enemy in allEnemies)
                if (enemy != null && enemy != self && !enemy.IsDefeated && match(enemy))
                    count++;
            return count;
        }
    }

    /// <summary>Eligible while another living enemy is Receptive — "drag one back to Hostile".</summary>
    public class AnyAllyReceptiveEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        ) => RoomState.CountAllies(allEnemies, self, e => e.Stats.IsReceptive) > 0;
    }

    /// <summary>
    /// Eligible while there is at least one other living enemy and every one of them is
    /// Receptive — a cornered move, or a punish window when the flock has been "won".
    /// </summary>
    public class AllAlliesReceptiveEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            int allies = RoomState.CountAllies(allEnemies, self, _ => true);
            return allies > 0
                && RoomState.CountAllies(allEnemies, self, e => e.Stats.IsReceptive) == allies;
        }
    }

    /// <summary>Eligible while another living enemy is Hostile.</summary>
    public class AnyAllyHostileEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        ) => RoomState.CountAllies(allEnemies, self, e => e.Stats.IsHostile) > 0;
    }

    /// <summary>Eligible while at least <see cref="EnemyMoveData.ConditionCount"/> other enemies are alive.</summary>
    public class AlliesAtLeastEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        ) => RoomState.CountAllies(allEnemies, self, _ => true) >= move.ConditionCount;
    }
}
