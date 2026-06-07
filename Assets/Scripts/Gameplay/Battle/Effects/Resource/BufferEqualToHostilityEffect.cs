using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants session Support equal to the number of currently Hostile enemies.
    /// Dexterity/Frail modifiers apply.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ComposureEqualToHostilityEffect"
    )]
    public class BufferEqualToHostilityEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int hostileCount = 0;
            if (ctx.AllEnemies != null)
                foreach (var enemy in ctx.AllEnemies)
                    if (!enemy.IsDefeated && enemy.Stats.IsHostile)
                        hostileCount++;

            ApplyGainSupport(hostileCount, ctx);
            GameLogger.LogInfo<BufferEqualToHostilityEffect>(
                $"Gained Support equal to hostile enemy count ({hostileCount})"
            );
        }

        public override string GetDescription() =>
            "Gain Support equal to the number of Hostile enemies";
    }
}
