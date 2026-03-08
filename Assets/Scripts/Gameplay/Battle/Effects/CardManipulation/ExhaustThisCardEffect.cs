using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Flags the played card to be moved to the exhaust pile after all its effects resolve,
    /// rather than going to the discard pile. Exhausted cards do not return to the draw pile.
    /// BattleManager checks <see cref="EffectExecutionContext.ShouldExhaust"/> after resolution.
    /// </summary>
    [Serializable]
    public class ExhaustThisCardEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            ctx.ShouldExhaust = true;
            GameLogger.LogInfo<ExhaustThisCardEffect>("Card flagged for exhaust after play");
        }

        public override string GetDescription() => "Exhaust this card";
    }
}
