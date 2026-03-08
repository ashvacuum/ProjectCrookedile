using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player's hand card count satisfies the configured comparison.
    /// </summary>
    [Serializable]
    public class HandSizeCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the current hand size against the threshold.")]
        [SerializeField] private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The hand size threshold.")]
        [SerializeField] private int _value = 3;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Deck == null) return false;
            int hand = ctx.Deck.HandCount;
            return _comparison switch
            {
                ComparisonType.AtLeast => hand >= _value,
                ComparisonType.AtMost  => hand <= _value,
                ComparisonType.Equals  => hand == _value,
                _                     => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.Equals  => $"you have exactly {_value} cards in hand",
                ComparisonType.AtLeast => $"you have {_value}+ cards in hand",
                ComparisonType.AtMost  => $"you have {_value} or fewer cards in hand",
                _                     => $"hand size {_value}",
            };
    }
}
