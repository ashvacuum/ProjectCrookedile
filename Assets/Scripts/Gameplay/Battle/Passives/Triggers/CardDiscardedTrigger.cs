using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player discards a card (not via exhaust).</summary>
    [Serializable]
    public class CardDiscardedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardDiscardedEvent>()) return false;
            return ctx.As<CardDiscardedEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you discard a card";
    }
}
