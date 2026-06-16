using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when any enemy's hostility hits its maximum cap for the first time in a raise.</summary>
    [Serializable]
    public class EnemyMaxedHostilityTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) => ctx.Is<EnemyMaxedHostilityEvent>();

        public override Type EventType => typeof(EnemyMaxedHostilityEvent);

        public override string TriggerLabel => "When an enemy maxes out their hostility";
    }
}
