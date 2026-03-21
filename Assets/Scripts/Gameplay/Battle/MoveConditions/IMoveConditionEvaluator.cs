using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Strategy for evaluating whether an enemy move is eligible to be selected this turn.
    /// Implement this interface to add a new move condition without modifying EnemyController.
    /// </summary>
    public interface IMoveConditionEvaluator
    {
        /// <summary>
        /// Returns true if the given move satisfies its condition given the current battle state.
        /// </summary>
        bool IsMet(EnemyMoveData move, IReadOnlyList<EnemyController> allEnemies, EnemyController self);
    }
}
