using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player takes resolve damage greater than zero.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DamageTakenTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<DamageDealtEvent>()) return false;
            var e = ctx.As<DamageDealtEvent>();
            return e.IsToPlayer && e.Amount > 0;
        }

        public override string TriggerLabel => "When you take damage";
    }
}
