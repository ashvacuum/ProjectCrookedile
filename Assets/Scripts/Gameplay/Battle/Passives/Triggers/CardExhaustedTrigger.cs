using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player exhausts a card (removed from battle for good).</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class CardExhaustedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardExhaustedEvent>())
                return false;
            return ctx.As<CardExhaustedEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you exhaust a card";
    }
}
