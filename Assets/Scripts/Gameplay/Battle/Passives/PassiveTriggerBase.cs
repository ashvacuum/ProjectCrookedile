using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Abstract base for all passive triggers.
    ///
    /// Each concrete subclass corresponds to exactly one type of battle event and contains
    /// only the configuration needed to match that event (e.g. a card-type filter for
    /// <c>CardPlayedTrigger</c>).
    ///
    /// To add a new trigger: create a new <c>[Serializable]</c> subclass — no existing code changes.
    ///
    /// Subclass naming convention: <c>{EventMoment}Trigger</c>, e.g. <c>TurnStartTrigger</c>.
    /// </summary>
    [Serializable]
    public abstract class PassiveTriggerBase
    {
        /// <summary>
        /// Returns true if <paramref name="ctx"/> represents the event this trigger listens for,
        /// AND any trigger-specific filter criteria are satisfied.
        /// </summary>
        public abstract bool Matches(PassiveEventContext ctx);

        /// <summary>
        /// The single <see cref="Core.IGameEvent"/> type this trigger listens for. Used by
        /// <see cref="PassiveResolver"/> to bucket passives by event type so dispatch only visits
        /// passives that can possibly match — <see cref="Matches"/> remains the authority on the
        /// trigger's additional filter criteria (amount, direction, card type, etc.).
        /// </summary>
        public abstract Type EventType { get; }

        /// <summary>Human-readable label for UI / GetDescription().</summary>
        public abstract string TriggerLabel { get; }
    }
}
