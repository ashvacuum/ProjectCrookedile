using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires when the player recovers a card from the discard pile to hand.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class CardRecoveredTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<CardRecoveredEvent>())
                return false;
            return ctx.As<CardRecoveredEvent>().IsPlayer;
        }

        public override Type EventType => typeof(CardRecoveredEvent);

        public override string TriggerLabel => "When you recover a card from discard";
    }
}
