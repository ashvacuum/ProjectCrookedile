using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when Debt comes due and energy cannot cover it, after the meter takes the hit.</summary>
    [Serializable]
    public class DebtUnpaidTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<DebtUnpaidEvent>();
        public override Type EventType => typeof(DebtUnpaidEvent);
        public override string TriggerLabel => "When Debt goes unpaid";
    }
}
