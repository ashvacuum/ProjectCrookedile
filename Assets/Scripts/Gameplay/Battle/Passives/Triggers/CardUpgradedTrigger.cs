using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player upgrades a card in-battle.</summary>
    [Serializable]
    public class CardUpgradedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardUpgradedEvent>()) return false;
            return ctx.As<CardUpgradedEvent>().IsPlayer;
        }

        public override string TriggerLabel => "When you upgrade a card";
    }
}
