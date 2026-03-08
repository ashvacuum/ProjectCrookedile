using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player's discard pile count satisfies the configured comparison.
    /// </summary>
    [Serializable]
    public class DiscardSizeCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the discard pile size against the threshold.")]
        [SerializeField] private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The discard pile count threshold.")]
        [SerializeField] private int _value = 5;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Deck == null) return false;
            int discardCount = ctx.Deck.DiscardCount;
            return _comparison switch
            {
                ComparisonType.AtLeast => discardCount >= _value,
                ComparisonType.AtMost  => discardCount <= _value,
                ComparisonType.Equals  => discardCount == _value,
                _                     => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.AtLeast => $"{_value}+ cards in discard",
                ComparisonType.AtMost  => $"{_value} or fewer cards in discard",
                ComparisonType.Equals  => $"exactly {_value} cards in discard",
                _                     => $"discard size {_value}",
            };
    }
}
