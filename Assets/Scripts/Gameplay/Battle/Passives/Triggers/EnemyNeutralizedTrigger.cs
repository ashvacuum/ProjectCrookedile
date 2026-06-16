using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's hostility returns to exactly 0 from any non-zero value.</summary>
    [Serializable]
    public class EnemyNeutralizedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyNeutralizedEvent>();

        public override Type EventType => typeof(EnemyNeutralizedEvent);

        public override string TriggerLabel => "When an enemy is neutralized";
    }
}
