using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// The player's Support stops expiring at the start of their turn for the rest of the battle,
    /// so it carries over and stacks (Friends in High Places). See <see cref="OpinionLedger.SupportPersists"/>.
    /// </summary>
    [Serializable]
    public class RetainSupportEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.BattleManager?.Opinion != null)
                ctx.BattleManager.Opinion.SupportPersists = true;
        }

        public override string GetDescription() =>
            "For the rest of the battle, your Support no longer expires at the start of your turn";
    }
}
