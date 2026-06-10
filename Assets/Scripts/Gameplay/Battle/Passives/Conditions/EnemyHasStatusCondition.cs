using System;
using System.Linq;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when at least one active enemy has the specified status effect with
    /// at least the required number of stacks.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class EnemyHasStatusCondition : PassiveConditionBase
    {
        [Tooltip("The status to check for on any enemy.")]
        [SerializeReference]
        private StatusBehavior _status;

        [Tooltip("Minimum stack count required.")]
        [MinValue(1)]
        [SerializeField]
        private int _minStacks = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Enemies == null || _status == null)
                return false;
            return ctx.Enemies.Any(e =>
                e != null
                && e.StatusEffects != null
                && e.StatusEffects.GetStacks(_status) >= _minStacks
            );
        }

        public override string ConditionLabel =>
            _minStacks <= 1
                ? $"an enemy has {_status?.DisplayName ?? "(none)"}"
                : $"an enemy has {_minStacks}+ {_status?.DisplayName ?? "(none)"}";
    }
}
