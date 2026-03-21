using System.Collections.Generic;
using Crookedile.Data.Enemy;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Like Sequential but picks a random starting offset on the first turn,
    /// then cycles through eligible moves in order from that offset.
    /// </summary>
    public class RandomSequentialMoveSelector : IMovePatternSelector
    {
        private int  _index;
        private bool _initialized;

        public EnemyMoveData SelectMove(IReadOnlyList<EnemyMoveData> eligibleMoves)
        {
            if (!_initialized)
            {
                _index       = Random.Range(0, eligibleMoves.Count);
                _initialized = true;
            }

            var move = eligibleMoves[_index % eligibleMoves.Count];
            _index++;
            return move;
        }

        public void Reset()
        {
            _index       = 0;
            _initialized = false;
        }
    }
}
