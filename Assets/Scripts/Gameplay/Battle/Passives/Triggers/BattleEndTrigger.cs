using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the battle concludes (victory or defeat).</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class BattleEndTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<BattleEndedEvent>();
        public override string TriggerLabel => "At battle end";
    }
}
