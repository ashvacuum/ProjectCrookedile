using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player's opinion takes pressure greater than zero.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DamageTakenTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<DamageDealtEvent>())
                return false;
            var e = ctx.As<DamageDealtEvent>();
            return e.IsToPlayer && e.Amount > 0;
        }

        public override Type EventType => typeof(DamageDealtEvent);

        public override string TriggerLabel => "When you take damage";
    }
}
