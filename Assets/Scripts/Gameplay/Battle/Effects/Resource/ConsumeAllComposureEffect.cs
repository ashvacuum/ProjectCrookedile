using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Consumes all of the caster's Shield, reducing it to zero.</summary>
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
            int consumed = ctx.Caster.ConsumeAllShield();
            ctx.LastShieldLost += consumed;
            GameLogger.LogInfo<ConsumeAllShieldEffect>($"Consumed {consumed} Shield");
        }

        public override string GetDescription() => "Consume all Support";
    }
}
