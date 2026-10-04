using System;
using System.Collections.Generic;
using Crookedile.Data.Enemy;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Boss
{
    [Serializable]
    public sealed class BossMoveBundle
    {
        [Tooltip("Unique name used by the behavior tree to choose this plan.")]
        [SerializeField]
        private string _name = "Opening statements";

        [Tooltip("Complete plan, executed in this order. Exactly two or three moves.")]
        [InlineEditor]
        [SerializeField]
        private List<EnemyMoveData> _moves = new List<EnemyMoveData>();

        [Tooltip(
            "Player turns that must pass before this bundle can be chosen again. 0 allows consecutive use."
        )]
        [Min(0)]
        [SerializeField]
        private int _cooldownTurns;

        public string Name => _name;
        public IReadOnlyList<EnemyMoveData> Moves => _moves;
        public int CooldownTurns => _cooldownTurns;

        public bool IsValid()
        {
            if (_moves == null || _moves.Count < 2 || _moves.Count > 3)
                return false;

            foreach (var move in _moves)
                if (move == null)
                    return false;

            return true;
        }
    }
}
