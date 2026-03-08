using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when the player has the specified status effect with at least the required stacks.
    /// </summary>
    [Serializable]
    public class PlayerHasStatusCondition : PassiveConditionBase
    {
        [Tooltip("The status type to check for on the player.")]
        [SerializeField] private StatusEffectType _statusType = StatusEffectType.Confused;

        [Tooltip("Minimum stack count required.")]
        [MinValue(1)]
        [SerializeField] private int _minStacks = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.PlayerStatusEffects == null) return false;
            return ctx.PlayerStatusEffects.GetStacks(_statusType) >= _minStacks;
        }

        public override string ConditionLabel =>
            _minStacks <= 1
                ? $"you have {_statusType}"
                : $"you have {_minStacks}+ {_statusType}";
    }
}
