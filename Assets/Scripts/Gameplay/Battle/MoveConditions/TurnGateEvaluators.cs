using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Eligible from turn <see cref="EnemyMoveData.ConditionTurn"/> onward.
    /// The Escalator clock: author the "gets worse" move with this condition.
    /// </summary>
    public class OnTurnOrAfterEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            // Unknown turn (no provider wired) — default to eligible so the move isn't lost.
            int turn = self.CurrentBattleTurn;
            return turn <= 0 || turn >= move.ConditionTurn;
        }
    }

    /// <summary>
    /// Eligible only before turn <see cref="EnemyMoveData.ConditionTurn"/> —
    /// opening behavior that gets replaced once the clock runs out.
    /// </summary>
    public class BeforeTurnEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            int turn = self.CurrentBattleTurn;
            return turn <= 0 || turn < move.ConditionTurn;
        }
    }

    /// <summary>
    /// Eligible only on turns divisible by <see cref="EnemyMoveData.ConditionTurn"/> —
    /// periodic moves (e.g. a big Condemn every 3rd turn).
    /// </summary>
    public class EveryNTurnsEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(
            EnemyMoveData move,
            IReadOnlyList<EnemyController> allEnemies,
            EnemyController self
        )
        {
            int turn = self.CurrentBattleTurn;
            int n = move.ConditionTurn;
            return turn <= 0 || (n > 0 && turn % n == 0);
        }
    }
}
