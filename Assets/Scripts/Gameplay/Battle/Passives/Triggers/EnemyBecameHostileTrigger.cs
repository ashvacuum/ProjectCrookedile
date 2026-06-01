using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's hostility crosses from neutral/receptive into hostile (≤0 → >0).</summary>
    [Serializable]
    public class EnemyBecameHostileTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyBecameHostileEvent>();

        public override Type EventType => typeof(EnemyBecameHostileEvent);

        public override string TriggerLabel => "When an enemy becomes hostile";
    }
}
