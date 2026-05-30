using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's resolve reaches zero (enemy is defeated).</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class EnemyDefeatedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyDefeatedEvent>();
        public override string TriggerLabel => "When an enemy is defeated";
    }
}
