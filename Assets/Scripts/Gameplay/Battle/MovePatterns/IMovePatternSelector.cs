using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Strategy for selecting the next enemy move from an eligible set.
    /// Implement this interface to add a new move pattern without modifying EnemyController.
    /// </summary>
    public interface IMovePatternSelector
    {
        /// <summary>
        /// Selects one move from the eligible set and returns it.
        /// The selector is responsible for managing its own state (e.g. current index).
        /// </summary>
        EnemyMoveData SelectMove(IReadOnlyList<EnemyMoveData> eligibleMoves);

        /// <summary>Resets internal state. Called when the enemy controller is (re-)initialised.</summary>
        void Reset();
    }
}
