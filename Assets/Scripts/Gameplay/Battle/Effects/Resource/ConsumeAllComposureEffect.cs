using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Consumes all of the caster's Composure, reducing it to zero.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ConsumeAllComposureEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int consumed = ctx.Caster.ConsumeAllComposure();
            ctx.LastComposureLost += consumed;
            GameLogger.LogInfo<ConsumeAllComposureEffect>($"Consumed {consumed} Composure");
        }

        public override string GetDescription() => "Consume all Composure";
    }
}
