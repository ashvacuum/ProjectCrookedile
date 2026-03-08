using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants Composure equal to the caster's current Hostility.
    /// Dexterity/Frail modifiers still apply to the gained amount.
    /// Useful for enemies or cards that convert aggression into defence.
    /// </summary>
    [Serializable]
    public class ComposureEqualToHostilityEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int hostility = ctx.Caster.CurrentHostility;
            ApplyGainComposure(ctx.Caster, hostility, ctx);
            GameLogger.LogInfo<ComposureEqualToHostilityEffect>(
                $"Gained Composure equal to Hostility ({hostility})");
        }

        public override string GetDescription() =>
            "Gain Composure equal to your Hostility";
    }
}
