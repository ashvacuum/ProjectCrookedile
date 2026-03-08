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
    public class EnemyHasStatusCondition : PassiveConditionBase
    {
        [Tooltip("The status type to check for on any enemy.")]
        [SerializeField] private StatusEffectType _statusType = StatusEffectType.Weakened;

        [Tooltip("Minimum stack count required.")]
        [MinValue(1)]
        [SerializeField] private int _minStacks = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Enemies == null) return false;
            return ctx.Enemies.Any(e =>
                e != null &&
                e.StatusEffects != null &&
                e.StatusEffects.GetStacks(_statusType) >= _minStacks);
        }

        public override string ConditionLabel =>
            _minStacks <= 1
                ? $"an enemy has {_statusType}"
                : $"an enemy has {_minStacks}+ {_statusType}";
    }
}
