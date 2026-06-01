using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's hostility crosses from neutral/hostile into receptive (≥0 → &lt;0).</summary>
    [Serializable]
    public class EnemyBecameReceptiveTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) =>
            ctx.Is<EnemyBecameReceptiveEvent>();

        public override Type EventType => typeof(EnemyBecameReceptiveEvent);

        public override string TriggerLabel => "When an enemy becomes receptive";
    }
}
