using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy is removed from active combat.</summary>
    [Serializable]
    [Obsolete]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class EnemyDefeatedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyDefeatedEvent>();

        public override Type EventType => typeof(EnemyDefeatedEvent);

        public override string TriggerLabel => "When an enemy is defeated";
    }
}
