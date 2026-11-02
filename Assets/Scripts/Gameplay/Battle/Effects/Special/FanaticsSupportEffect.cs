using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Command card "defend now": every following Fanatic adds Support as if you had played a
    /// defend, whatever else the card did (FaithLeaderConfig follow amount + over-stacking).
    /// </summary>
    [Serializable]
    public class FanaticsSupportEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            ctx.BattleManager?.CommandFanatics(attack: false, defend: true);

        public override string GetDescription() => "Every Fanatic adds Support.";
    }
}
