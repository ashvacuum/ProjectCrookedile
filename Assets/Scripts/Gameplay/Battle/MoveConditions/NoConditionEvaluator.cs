using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Default evaluator — always returns true. Used for <see cref="EnemyMoveCondition.None"/>.
    /// </summary>
    public class NoConditionEvaluator : IMoveConditionEvaluator
    {
        public bool IsMet(EnemyMoveData move, IReadOnlyList<EnemyController> allEnemies, EnemyController self)
            => true;
    }
}
