using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// For damage-event triggers (<see cref="DamageDealtTrigger"/>, <see cref="DamageTakenTrigger"/>):
    /// passes only if the damage amount in the triggering event is at least the configured minimum.
    /// </summary>
    [Serializable]
    public class DamageMinimumCondition : PassiveConditionBase
    {
        [Tooltip("The passive fires only if the triggering damage amount is at least this value.")]
        [MinValue(1)]
        [SerializeField] private int _minAmount = 5;

        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (!ctx.EventCtx.Is<DamageDealtEvent>()) return true; // not a damage event — don't block
            return ctx.EventCtx.As<DamageDealtEvent>().Amount >= _minAmount;
        }

        public override string ConditionLabel => $"the damage is at least {_minAmount}";
    }
}
