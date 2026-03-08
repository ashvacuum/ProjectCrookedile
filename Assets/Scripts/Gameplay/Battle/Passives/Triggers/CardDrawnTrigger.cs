using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires each time the player draws a card.</summary>
    [Serializable]
    public class CardDrawnTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardDrawnEvent>()) return false;
            return ctx.As<CardDrawnEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you draw a card";
    }
}
