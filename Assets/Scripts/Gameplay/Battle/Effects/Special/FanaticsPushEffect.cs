using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Command card "attack now": every following Fanatic pushes the meter as if you had played an
    /// attack, whatever else the card did (FaithLeaderConfig follow amount + over-stacking).
    /// </summary>
    [Serializable]
    public class FanaticsPushEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            ctx.BattleManager?.CommandFanatics(attack: true, defend: false);

        public override string GetDescription() => "Every Fanatic pushes the meter.";
    }
}
