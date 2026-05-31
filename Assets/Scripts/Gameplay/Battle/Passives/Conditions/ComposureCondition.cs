using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player's Shield (Support) stack count satisfies the configured comparison.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ComposureCondition"
    )]
    public class ShieldCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the player's Support against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The Support stack threshold.")]
        [SerializeField]
        private int _value = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.PlayerStats == null)
                return false;
            int shield = ctx.PlayerStats.CurrentShield;
            return _comparison switch
            {
                ComparisonType.AtLeast => shield >= _value,
                ComparisonType.AtMost => shield <= _value,
                ComparisonType.Equals => shield == _value,
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
