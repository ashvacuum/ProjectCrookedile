using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player's composure stack count satisfies the configured comparison.
    /// </summary>
    [Serializable]
    public class ComposureCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the player's composure against the threshold.")]
        [SerializeField] private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The composure stack threshold.")]
        [SerializeField] private int _value = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.PlayerStats == null) return false;
            int composure = ctx.PlayerStats.Composure;
            return _comparison switch
            {
                ComparisonType.AtLeast => composure >= _value,
                ComparisonType.AtMost  => composure <= _value,
                ComparisonType.Equals  => composure == _value,
                _                     => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.AtLeast => $"you have {_value}+ composure",
                ComparisonType.AtMost  => $"you have {_value} or less composure",
                ComparisonType.Equals  => $"you have exactly {_value} composure",
                _                     => $"composure {_value}",
            };
    }
}
