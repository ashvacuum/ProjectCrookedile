using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player recovers a card from the discard pile to hand.</summary>
    [Serializable]
    public class CardRecoveredTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardRecoveredEvent>()) return false;
            return ctx.As<CardRecoveredEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you recover a card from discard";
    }
}
