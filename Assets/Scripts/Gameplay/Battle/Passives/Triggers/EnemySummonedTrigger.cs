using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when a new enemy (minion) is summoned during the battle.</summary>
    [Serializable]
    public class EnemySummonedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemySummonedEvent>();
        public override string TriggerLabel => "When an enemy is summoned";
    }
}
