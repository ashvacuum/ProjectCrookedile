using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes only when the player's current resolve is at or below a percentage of max resolve.
    /// Example: threshold 50 means the player is at half health or lower.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ResolveBelowCondition : PassiveConditionBase
    {
        [Tooltip("Fire only when current resolve is at or below this percentage of max resolve.")]
        [MinValue(1), MaxValue(100)]
        [SerializeField] private int _percentThreshold = 50;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.PlayerStats == null) return false;
            return ctx.PlayerStats.CurrentResolve * 100 <= ctx.PlayerStats.MaxResolve * _percentThreshold;
        }

        public override string ConditionLabel => $"your resolve is at or below {_percentThreshold}%";
    }
}
