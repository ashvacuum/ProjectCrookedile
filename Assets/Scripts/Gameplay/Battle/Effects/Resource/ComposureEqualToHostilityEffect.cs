using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants Shield equal to the number of currently Hostile enemies (Hostility > 0).
    /// Dexterity/Frail modifiers still apply to the gained amount.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ComposureEqualToHostilityEffect"
    )]
    public class ShieldEqualToHostilityEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int hostileCount = 0;
            if (ctx.AllEnemies != null)
            {
                foreach (var enemy in ctx.AllEnemies)
                    if (!enemy.IsDefeated && enemy.Stats.IsHostile)
                        hostileCount++;
            }

            ApplyGainShield(ctx.Caster, hostileCount, ctx);
            GameLogger.LogInfo<ShieldEqualToHostilityEffect>(
                $"Gained Shield equal to hostile enemy count ({hostileCount})"
            );
        }

        public override string GetDescription() =>
            "Gain Support equal to the number of Hostile enemies";
    }
}
