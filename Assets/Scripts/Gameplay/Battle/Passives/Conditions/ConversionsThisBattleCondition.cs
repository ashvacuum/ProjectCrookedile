using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Gates the passive on how many enemies you've converted this battle. "Before your first
    /// conversion" is (AtMost, 0).
    /// </summary>
    [Serializable]
    public class ConversionsThisBattleCondition : PassiveConditionBase
    {
        [Tooltip("How to compare this battle's conversion count against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtMost;

        [Tooltip("The conversion count to compare against.")]
        [Min(0)]
        [SerializeField]
        private int _conversions = 0;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            int count = ctx.BattleManager != null ? ctx.BattleManager.ConversionsThisBattle : 0;
            return _comparison switch
            {
                ComparisonType.AtLeast => count >= _conversions,
                ComparisonType.AtMost => count <= _conversions,
                ComparisonType.Equals => count == _conversions,
                _ => true,
            };
        }

        public override string ConditionLabel =>
            _comparison == ComparisonType.AtMost && _conversions == 0
                ? "before your first conversion this battle"
                : _comparison switch
                {
                    ComparisonType.AtLeast => $"after {_conversions}+ conversions this battle",
                    ComparisonType.AtMost => $"with at most {_conversions} conversions this battle",
                    _ => $"with exactly {_conversions} conversions this battle",
                };
    }
}
