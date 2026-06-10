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
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class PlayerHasStatusCondition : PassiveConditionBase
    {
        [Tooltip("The status to check for on the player.")]
        [SerializeReference]
        private StatusBehavior _status;

        [Tooltip("Minimum stack count required.")]
        [MinValue(1)]
        [SerializeField]
        private int _minStacks = 1;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.PlayerStatusEffects == null || _status == null)
                return false;
            return ctx.PlayerStatusEffects.GetStacks(_status) >= _minStacks;
        }

        public override string ConditionLabel =>
            _minStacks <= 1
                ? $"you have {_status?.DisplayName ?? "(none)"}"
                : $"you have {_minStacks}+ {_status?.DisplayName ?? "(none)"}";
    }
}
