using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the session's Support decreases.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "ComposureLostTrigger"
    )]
    public class ShieldLostTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<SupportChangedEvent>())
                return false;
            var e = ctx.As<SupportChangedEvent>();
            return e.NewValue < e.OldValue;
        }

        public override Type EventType => typeof(SupportChangedEvent);

        public override string TriggerLabel => "When you lose Support";
    }
}
