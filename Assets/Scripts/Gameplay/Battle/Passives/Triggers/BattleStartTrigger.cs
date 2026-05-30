using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires once at the start of a battle, after the opening hand is dealt.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class BattleStartTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<BattleStartedEvent>();
        public override string TriggerLabel => "At battle start";
    }
}
