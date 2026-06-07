using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when Opinion is raised directly (via RaiseOpinionEffect / RaiseOpinionTrackedEffect).</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Crookedile.Runtime",
        "OpinionHealedTrigger"
    )]
    public class OpinionRaisedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<HealingAppliedEvent>())
                return false;
            return ctx.As<HealingAppliedEvent>().IsToPlayer;
        }

        public override Type EventType => typeof(HealingAppliedEvent);

        public override string TriggerLabel => "When Opinion is raised directly";
    }
}
