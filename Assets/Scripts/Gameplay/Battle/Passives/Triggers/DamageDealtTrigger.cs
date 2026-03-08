using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player deals resolve damage greater than zero to an enemy.</summary>
    [Serializable]
    public class DamageDealtTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<DamageDealtEvent>()) return false;
            var e = ctx.As<DamageDealtEvent>();
            return !e.IsToPlayer && e.Amount > 0;
        }

        public override string TriggerLabel => "When you deal damage";
    }
}
