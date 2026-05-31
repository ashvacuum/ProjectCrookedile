using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Consumes all session Support, reducing it to zero.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ConsumeAllComposureEffect"
    )]
    public class ConsumeAllShieldEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.BattleManager == null)
                return;
            int support = ctx.BattleManager.CurrentSupport;
            if (support > 0)
                ctx.BattleManager.AbsorbThroughSupport(support);
            ctx.LastSupportLost += support;
            GameLogger.LogInfo<ConsumeAllShieldEffect>($"Consumed {support} Support");
        }

        public override string GetDescription() => "Consume all Support";
    }
}
