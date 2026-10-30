using System;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class SupportPaysDebtEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.Celebrity;
            if (state != null)
                state.SupportPaysDebt = true;
        }

        public override string GetDescription() =>
            "For the rest of this fight, Debt your energy can't pay comes out of Support before Opinion";
    }
}
