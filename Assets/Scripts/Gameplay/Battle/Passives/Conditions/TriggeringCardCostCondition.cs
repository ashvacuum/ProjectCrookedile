using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// For card-event triggers: passes when the triggering card's printed cost (base energy,
    /// ignoring discounts) satisfies the comparison. Fails for events that name no card.
    /// </summary>
    [Serializable]
    public class TriggeringCardCostCondition : PassiveConditionBase
    {
        [Tooltip("How to compare the card's printed cost against the threshold.")]
        [SerializeField]
        private ComparisonType _comparison = ComparisonType.AtLeast;

        [Tooltip("The printed-cost threshold.")]
        [SerializeField]
        private int _value = 2;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            var card = ctx.EventCtx.GetCard();
            if (card == null)
                return false;
            int cost = card.PrintedCost;
            return _comparison switch
            {
                ComparisonType.AtLeast => cost >= _value,
                ComparisonType.AtMost => cost <= _value,
                ComparisonType.Equals => cost == _value,
                _ => true,
            };
        }

        public override string ConditionLabel =>
            _comparison switch
            {
                ComparisonType.AtLeast => $"it costs {_value}+",
                ComparisonType.AtMost => $"it costs {_value} or less",
                _ => $"it costs exactly {_value}",
            };
    }
}
