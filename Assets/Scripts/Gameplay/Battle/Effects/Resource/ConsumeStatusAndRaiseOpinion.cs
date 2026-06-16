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
                var shame = enemies.StatusEffects.GetStacks<ShameStatus>();
                var doubt = enemies.StatusEffects.GetStacks<DoubtStatus>();

                damage += guilt + shame + doubt;

                enemies.StatusEffects.RemoveStacks<GuiltStatus>(guilt);
                enemies.StatusEffects.RemoveStacks<ShameStatus>(shame);
                enemies.StatusEffects.RemoveStacks<DoubtStatus>(doubt);
            }

            ApplyPressure(ctx.Target, ctx.Caster, damage, ctx);
        }

        public override string GetDescription() =>
            "Remove All Guilt, Shame and Doubt from all enemies and increase the opinion";
    }
}
