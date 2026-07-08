using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Eligible while the opinion meter is at or above <see cref="EnemyMoveData.ConditionPercent"/> —
    /// desperation moves that come out when the player is close to winning, and boss phase shifts.
    /// </summary>
    public class OpinionAtOrAboveEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            // Unknown meter (no provider wired) — default to eligible so the move isn't lost.
            float percent = self.CurrentOpinionPercent;
            return percent < 0f || percent >= move.ConditionPercent;
        }
    }

    /// <summary>
    /// Eligible while the opinion meter is at or below <see cref="EnemyMoveData.ConditionPercent"/> —
    /// finisher moves that come out when the player is losing the room.
    /// </summary>
    public class OpinionAtOrBelowEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            float percent = self.CurrentOpinionPercent;
            return percent < 0f || percent <= move.ConditionPercent;
        }
    }
}
