using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player's resolve is healed (restored).</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ResolveHealedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<HealingAppliedEvent>())
                return false;
            return ctx.As<HealingAppliedEvent>().IsToPlayer;
        }

        public override string TriggerLabel => "When your resolve is healed";
    }
}
