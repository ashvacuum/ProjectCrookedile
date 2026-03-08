using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes only on every N-th time this passive's trigger fires.
    /// Example: N=3 means the passive fires on the 3rd, 6th, 9th trigger event, etc.
    /// The fire count is tracked per passive (not globally).
    /// </summary>
    [Serializable]
    public class NthEventCondition : PassiveConditionBase
    {
        [Tooltip("The passive fires every N-th trigger event.")]
        [MinValue(2)]
        [SerializeField] private int _n = 3;

        public override bool Evaluate(PassiveEvaluationContext ctx)
            => _n > 0 && ctx.TriggerFireCount % _n == 0;

        public override string ConditionLabel => $"every {_n}th time";
    }
}
