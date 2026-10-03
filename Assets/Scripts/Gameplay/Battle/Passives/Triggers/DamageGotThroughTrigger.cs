using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when an enemy push takes Opinion off the meter after Support absorbed what it
    /// could — damage that got through. Its amount is <c>LastDamageTaken</c>.
    /// </summary>
    [Serializable]
    public class DamageGotThroughTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<DamageDealtEvent>())
                return false;
            var e = ctx.As<DamageDealtEvent>();
            return e.IsToPlayer && e.Applied > 0;
        }

        public override Type EventType => typeof(DamageDealtEvent);

        public override string TriggerLabel => "When Opinion damage gets through your Support";
    }
}
