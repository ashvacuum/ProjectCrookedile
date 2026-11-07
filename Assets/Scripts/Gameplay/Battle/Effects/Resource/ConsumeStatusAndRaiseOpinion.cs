using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public class ConsumeStatusAndRaiseOpinion : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var damage = 0;

            foreach (var enemies in ctx.AllEnemies)
            {
                var guilt = enemies.StatusEffects.GetStacks<GuiltStatus>();
                damage += guilt;
                enemies.StatusEffects.RemoveStacks<GuiltStatus>(guilt);
            }

            ApplyOpinion(ctx.Target, ctx.Caster, damage, ctx);
        }

        public override string GetDescription() =>
            "Remove all Guilt from all enemies and increase the opinion";
    }
}
