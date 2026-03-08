using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Gates the passive to specific player turn numbers.
    /// Examples: "only on turn 1" (Equals, 1) or "first 3 turns" (AtMost, 3).
    /// </summary>
    [Serializable]
    public class TurnNumberCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the current player turn number against the threshold.")]
        [SerializeField] private ComparisonType _comparison = ComparisonType.AtMost;

        [Tooltip("The turn number threshold to compare against.")]
        [SerializeField] private int _turnNumber = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            int turn = ctx.PlayerTurnNumber;
            return _comparison switch
            {
                ComparisonType.AtLeast => turn >= _turnNumber,
                ComparisonType.AtMost  => turn <= _turnNumber,
                ComparisonType.Equals  => turn == _turnNumber,
                _                     => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.Equals  => $"on turn {_turnNumber}",
                ComparisonType.AtMost  => $"within the first {_turnNumber} turns",
                ComparisonType.AtLeast => $"on turn {_turnNumber} or later",
                _                     => $"turn {_turnNumber}",
            };
    }
}
