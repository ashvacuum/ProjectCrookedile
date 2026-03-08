using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player marks a card in hand to be retained at end of turn.</summary>
    [Serializable]
    public class CardRetainedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardRetainedEvent>()) return false;
            return ctx.As<CardRetainedEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you retain a card";
    }
}
