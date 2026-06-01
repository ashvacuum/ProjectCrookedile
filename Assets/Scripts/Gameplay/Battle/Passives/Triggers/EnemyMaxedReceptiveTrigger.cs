using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's hostility hits its minimum cap (fully receptive) for the first time in a reduction.</summary>
    [Serializable]
    public class EnemyMaxedReceptiveTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyMaxedReceptiveEvent>();

        public override Type EventType => typeof(EnemyMaxedReceptiveEvent);

        public override string TriggerLabel => "When an enemy becomes fully receptive";
    }
}
