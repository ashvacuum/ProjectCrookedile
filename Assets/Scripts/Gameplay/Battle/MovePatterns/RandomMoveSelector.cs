using System.Collections.Generic;
using Crookedile.Data.Enemy;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Picks any eligible move at random each turn.
    /// </summary>
    public class RandomMoveSelector : IMovePatternSelector
    {
        public EnemyMoveData SelectMove(IReadOnlyList<EnemyMoveData> eligibleMoves)
            => eligibleMoves[Random.Range(0, eligibleMoves.Count)];

        public void Reset() { }
    }
}
