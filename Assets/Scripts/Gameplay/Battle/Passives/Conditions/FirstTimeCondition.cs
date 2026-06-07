using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes only the FIRST time this passive's trigger fires in a battle (and never again).
    /// The complement to <see cref="NthEventCondition"/> (which only supports N ≥ 2).
    ///
    /// Used by the Faith Leader starter passive ("first opinion shield each battle gets a bonus"):
    /// pair with <c>ShieldGainedTrigger</c> + a <c>GainShieldEffect</c> bonus. Per-passive fire
    /// counts reset each battle, so this is genuinely once-per-battle.
    /// </summary>
    [Serializable]
    public class FirstTimeCondition : PassiveConditionBase
    {
        public override bool Evaluate(PassiveEvaluationContext ctx) => ctx.TriggerFireCount == 1;

        public override string ConditionLabel => "the first time";
    }
}
