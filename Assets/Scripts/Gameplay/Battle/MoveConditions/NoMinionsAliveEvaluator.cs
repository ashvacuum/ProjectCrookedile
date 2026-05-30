using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Move is eligible only when no living enemy of the minion type specified on the move is alive.
    /// Typically used on a SummonMinion move so a boss only re-summons once all prior minions die.
    /// </summary>
    public class NoMinionsAliveEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            // If we can't evaluate, default to eligible so the move isn't silently lost.
            if (allEnemies == null || move.MinionToSummon == null)
                return true;

            return !allEnemies.Any(e =>
                e != self && !e.IsDefeated && e.EnemyData == move.MinionToSummon
            );
        }
    }
}
