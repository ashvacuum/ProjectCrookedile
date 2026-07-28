using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// Abstract base for everything an event choice can do to the run. Mirrors the
    /// <c>BattleEffect</c> pattern one layer up: each concrete owns only the fields it needs,
    /// stored via <c>[SerializeReference]</c> on <see cref="EventOption"/> so Odin renders a
    /// type-picker dropdown for heterogeneous lists.
    ///
    /// To add a new outcome: create a <c>[Serializable]</c> class inheriting from this and
    /// implement <see cref="Apply"/> and <see cref="GetDescription"/>. No other file changes.
    /// </summary>
    [Serializable]
    // Live description above each outcome's fields, so designers see what a choice does
    // without reading code. Same affordance as BattleEffect's InfoBox.
    [InfoBox(
        "@$value == null ? \"(no outcome chosen)\" : $value.EditorSafeDescription()",
        InfoMessageType.None
    )]
    public abstract class RunOutcome
    {
        /// <summary>Mutates <paramref name="state"/>. Called once when its option is chosen.</summary>
        public abstract void Apply(RunState state);

        /// <summary>Human-readable summary, shown in the inspector and usable as option subtext.</summary>
        public abstract string GetDescription();

        /// <summary>
        /// Guarded wrapper for the inspector InfoBox — one outcome with a latent null-deref in its
        /// description must not break the whole event asset's inspector. Referenced by reflection
        /// from <c>[InfoBox]</c>, so it must stay public.
        /// </summary>
        public string EditorSafeDescription()
        {
            try
            {
                return GetDescription();
            }
            catch (Exception e)
            {
                return $"(description error: {e.GetType().Name})";
            }
        }
    }

    // ponytail: three tiny outcomes share this file. Split to one-per-file like
    // Gameplay/Battle/Effects/ once there are enough to need folders.

    /// <summary>Adds to (or subtracts from) the run's Funds.</summary>
    [Serializable]
    public class AdjustFundsOutcome : RunOutcome
    {
        [Tooltip("Signed. Negative charges the player; the total clamps at zero, never negative.")]
        [SerializeField]
        private int _amount = 10;

        public override void Apply(RunState state) => state.AdjustFunds(_amount);

        public override string GetDescription() =>
            _amount >= 0 ? $"Gain {_amount} Funds" : $"Lose {-_amount} Funds";
    }

    /// <summary>Adds to (or subtracts from) the run's Credibility.</summary>
    [Serializable]
    public class AdjustCredibilityOutcome : RunOutcome
    {
        [Tooltip("Signed. Negative costs credibility; the total clamps at zero, never negative.")]
        [SerializeField]
        private int _amount = 5;

        public override void Apply(RunState state) => state.AdjustCredibility(_amount);

        public override string GetDescription() =>
            _amount >= 0 ? $"Gain {_amount} Credibility" : $"Lose {-_amount} Credibility";
    }

    /// <summary>Grants a relic. Duplicates are ignored by <see cref="RunState.AddRelic"/>.</summary>
    [Serializable]
    public class GrantRelicOutcome : RunOutcome
    {
        [SerializeField]
        private RelicData _relic;

        public override void Apply(RunState state) => state.AddRelic(_relic);

        // An unset relic silently no-ops at runtime; surfacing it in the description is what
        // makes that visible while authoring, since there is no health-check consumer up here yet.
        public override string GetDescription() =>
            _relic != null ? $"Gain relic: {_relic.RelicName}" : "Gain relic: (NONE SET)";
    }
}
