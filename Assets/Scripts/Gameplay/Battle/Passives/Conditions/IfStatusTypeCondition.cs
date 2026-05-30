using System;
using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// For status-event triggers (<see cref="StatusAppliedToEnemyTrigger"/>,
    /// <see cref="StatusAppliedToPlayerTrigger"/>): passes only if the status type in
    /// the triggering event matches the configured type.
    ///
    /// Prefer using the filter field on the trigger itself for simpler setups.
    /// This condition is useful when combining with other conditions in a list.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class IfStatusTypeCondition : PassiveConditionBase
    {
        [Tooltip("The passive fires only if the triggering event applied this status type.")]
        [SerializeField] private StatusEffectType _requiredStatus = StatusEffectType.Weakened;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (!ctx.EventCtx.Is<StatusEffectAppliedEvent>()) return true; // not a status event — don't block
            return ctx.EventCtx.As<StatusEffectAppliedEvent>().StatusType == _requiredStatus;
        }

        public override string ConditionLabel => $"the status is {_requiredStatus}";
    }
}
