using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player's Support stack count satisfies the configured comparison.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ShieldCondition"
    )]
    public class SupportCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the player's Support against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The Support stack threshold.")]
        [SerializeField]
        private int _value = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            int support = ctx.BattleManager?.CurrentSupport ?? 0;
            return _comparison switch
            {
                ComparisonType.AtLeast => support >= _value,
                ComparisonType.AtMost => support <= _value,
                ComparisonType.Equals => support == _value,
                _ => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.AtLeast => $"you have {_value}+ Support",
                ComparisonType.AtMost => $"you have {_value} or less Support",
                ComparisonType.Equals => $"you have exactly {_value} Support",
                _ => $"Support {_value}",
            };
    }
}
