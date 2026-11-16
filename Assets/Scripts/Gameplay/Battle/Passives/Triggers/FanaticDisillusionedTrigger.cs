using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Fires when a Fanatic's buff runs out and it reverts to neutral. The triggering enemy is the
    /// former Fanatic, so a TriggeringEnemy-targeted effect acts on it.
    /// </summary>
    [Serializable]
    public class FanaticDisillusionedTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx) =>
            ctx.Is<FanaticDisillusionedEvent>();

        public override Type EventType => typeof(FanaticDisillusionedEvent);

        public override string TriggerLabel => "When a Fanatic is disillusioned";
    }
}
