using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants Composure equal to the number of currently Hostile enemies (Hostility > 0).
    /// Dexterity/Frail modifiers still apply to the gained amount.
    /// Rewards the player for maintaining a hostile crowd — each aggressor converts to defence.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ComposureEqualToHostilityEffect : BattleEffect
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

            ApplyGainComposure(ctx.Caster, hostileCount, ctx);
            GameLogger.LogInfo<ComposureEqualToHostilityEffect>(
                $"Gained Composure equal to hostile enemy count ({hostileCount})"
            );
        }

        public override string GetDescription() =>
            "Gain Composure equal to the number of Hostile enemies";
    }
}
