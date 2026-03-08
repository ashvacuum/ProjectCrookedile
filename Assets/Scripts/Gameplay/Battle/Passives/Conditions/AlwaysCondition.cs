using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>A condition that always passes — equivalent to no condition gating.</summary>
    [Serializable]
    public class AlwaysCondition : PassiveConditionBase
    {
        public override bool Evaluate(PassiveEvaluationContext ctx) => true;
        public override string ConditionLabel => "always";
    }
}
