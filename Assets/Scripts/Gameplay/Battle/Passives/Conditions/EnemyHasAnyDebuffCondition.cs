using System;
using System.Linq;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Passes when at least one active enemy has at least one debuff status effect.
    /// </summary>
    [Serializable]
    public class EnemyHasAnyDebuffCondition : PassiveConditionBase
    {
        public override bool Evaluate(PassiveEvaluationContext ctx)
        {
            if (ctx.Enemies == null) return false;
            return ctx.Enemies.Any(e => e != null && e.StatusEffects != null && e.StatusEffects.HasAnyDebuff());
        }

        public override string ConditionLabel => "an enemy has a debuff";
    }
}
