using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Fires at the start of each player turn.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class TurnStartTrigger : PassiveTriggerBase
    {
        public override bool Matches(PassiveEventContext ctx)
        {
            if (!ctx.Is<TurnStartedEvent>())
                return false;
            return ctx.As<TurnStartedEvent>().IsPlayerTurn;
        }

        public override string TriggerLabel => "At turn start";
    }
}
