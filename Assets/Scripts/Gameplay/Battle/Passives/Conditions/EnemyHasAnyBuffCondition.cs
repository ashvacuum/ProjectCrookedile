using System;
using System.Linq;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when at least one active enemy has at least one buff status effect.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class EnemyHasAnyBuffCondition : PassiveConditionBase
    {
        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Enemies == null) return false;
            return ctx.Enemies.Any(e => e != null && e.StatusEffects != null && e.StatusEffects.HasAnyBuff());
        }

        public override string ConditionLabel => "an enemy has a buff";
    }
}
