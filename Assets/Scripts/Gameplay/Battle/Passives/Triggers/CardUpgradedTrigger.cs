using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player upgrades a card in-battle.</summary>
    [Serializable]
    public class CardUpgradedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<CardUpgradedEvent>();
        public override string TriggerLabel => "When you upgrade a card";
    }
}
