using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes only when the opinion meter is at or below a percentage of max.
    /// Repurposed from the old resolve-below condition now that resolve is removed.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ResolveBelowCondition : PassiveConditionBase
    {
        [Tooltip("Fire only when opinion is at or below this percentage of max.")]
        [MinValue(1), MaxValue(100)]
        [SerializeField]
        private int _percentThreshold = 50;

        public override bool Evaluate(PassiveEvaluationContext ctx) =>
            ctx.OpinionPercentage * 100f <= _percentThreshold;

        public override string ConditionLabel =>
            $"opinion is at or below {_percentThreshold}%";
    }
}
