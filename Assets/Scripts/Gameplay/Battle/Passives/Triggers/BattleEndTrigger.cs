using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the battle concludes (victory or defeat).</summary>
    [Serializable]
    public class BattleEndTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<BattleEndedEvent>();
        public override string TriggerLabel => "At battle end";
    }
}
