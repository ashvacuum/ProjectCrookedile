using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires once at the start of a battle, after the opening hand is dealt.</summary>
    [Serializable]
    public class BattleStartTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<BattleStartedEvent>();
        public override string TriggerLabel => "At battle start";
    }
}
