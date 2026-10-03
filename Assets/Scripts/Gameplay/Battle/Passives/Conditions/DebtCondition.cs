using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the Celebrity's outstanding Debt satisfies the configured comparison.
    /// Default (At Least 1) reads "if you have Debt".
    /// </summary>
    [Serializable]
    public class DebtCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the player's outstanding Debt against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The Debt threshold. At Least 1 = \"if you have any Debt\".")]
        [SerializeField]
        private int _value = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            int debt = ctx.BattleManager?.Celebrity.Debt ?? 0;
            return _comparison switch
            {
                ComparisonType.AtLeast => debt >= _value,
                ComparisonType.AtMost => debt <= _value,
                ComparisonType.Equals => debt == _value,
                _ => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.AtLeast when _value == 1 => "you have Debt",
                ComparisonType.AtLeast => $"you have {_value}+ Debt",
                ComparisonType.AtMost when _value == 0 => "you have no Debt",
                ComparisonType.AtMost => $"you have {_value} or less Debt",
                ComparisonType.Equals => $"you have exactly {_value} Debt",
                _ => $"Debt {_value}",
            };
    }
}
