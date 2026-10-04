using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Brings a Glamour tick forward into the player's action phase, including its normal decay.</summary>
    [Serializable]
    public sealed class TriggerGlamourTickEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.IsPlayerCard)
                ctx.BattleManager?.TickGlamour();
        }

        public override string GetDescription() =>
            "Trigger your Glamour tick now, including its normal decay";
    }
}
