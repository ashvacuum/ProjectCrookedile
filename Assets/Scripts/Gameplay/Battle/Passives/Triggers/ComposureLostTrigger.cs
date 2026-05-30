using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player loses any composure stacks.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ComposureLostTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<ComposureChangedEvent>())
                return false;
            var e = ctx.As<ComposureChangedEvent>();
            return e.IsPlayer && e.NewValue < e.OldValue;
        }

        public override string TriggerLabel => "When you lose composure";
    }
}
