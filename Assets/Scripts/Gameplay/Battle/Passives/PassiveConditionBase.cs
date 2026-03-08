using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Abstract base for all passive conditions.
    ///
    /// Each concrete subclass checks one specific runtime state (e.g. hand size, enemy status,
    /// turn number). Multiple conditions can be stacked on a single <see cref="BattlePassive"/>;
    /// ALL must pass before the passive fires.
    ///
    /// To add a new condition: create a new <c>[Serializable]</c> subclass — no existing code changes.
    ///
    /// Subclass naming convention: <c>{WhatIsChecked}Condition</c>, e.g. <c>HandSizeCondition</c>.
    /// </summary>
    [Serializable]
    public abstract class PassiveConditionBase
    {
        /// <summary>
        /// Returns true if the runtime state represented by <paramref name="ctx"/> satisfies
        /// this condition.
        /// </summary>
        public abstract bool Evaluate(PassiveEvaluationContext ctx);

        /// <summary>Human-readable label for UI / GetDescription().</summary>
        public abstract string ConditionLabel { get; }
    }
}
