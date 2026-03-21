using System.Collections.Generic;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Cycles through eligible moves in order: 0 → 1 → 2 → 0 …
    /// </summary>
    public class SequentialMoveSelector : IMovePatternSelector
    {
        private int _index;

        public EnemyMoveData SelectMove(IReadOnlyList<EnemyMoveData> eligibleMoves)
        {
            var move = eligibleMoves[_index % eligibleMoves.Count];
            _index++;
            return move;
        }

        public void Reset() => _index = 0;
    }
}
