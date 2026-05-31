using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player loses any Shield (Support) stacks.</summary>
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
            if (!ctx.Is<ShieldChangedEvent>())
                return false;
            var e = ctx.As<ShieldChangedEvent>();
            return e.IsPlayer && e.NewValue < e.OldValue;
        }

        public override string TriggerLabel => "When you lose Support";
    }
}
