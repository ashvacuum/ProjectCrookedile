using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires at the start of each enemy action during the opponent turn.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class EnemyActingTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyActingEvent>();

        public override Type EventType => typeof(EnemyActingEvent);

        public override string TriggerLabel => "When an enemy acts";
    }
}
