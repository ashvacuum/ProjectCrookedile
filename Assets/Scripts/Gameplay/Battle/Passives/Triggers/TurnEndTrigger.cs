using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires at the end of each player turn.</summary>
    [Serializable]
    public class TurnEndTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<TurnEndedEvent>()) return false;
            return ctx.As<TurnEndedEvent>().WasPlayerTurn;
        }

        public override string TriggerLabel => "At turn end";
    }
}
