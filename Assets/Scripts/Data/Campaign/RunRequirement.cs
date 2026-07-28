using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// A condition tested against the run — "has the player done X / got Y / reached Z".
    /// The primitive behind encounter dependencies: gating one encounter behind another,
    /// weighting one up once something has happened, and (later) greying out event options
    /// the player can't afford.
    ///
    /// Stored via <c>[SerializeReference]</c> so Odin renders a type-picker. To add a new
    /// condition: create a <c>[Serializable]</c> class inheriting this and implement
    /// <see cref="Check"/> and <see cref="GetDescription"/>.
    /// </summary>
    [Serializable]
    [InfoBox(
        "@$value == null ? \"(no condition chosen)\" : $value.EditorSafeDescription()",
        InfoMessageType.None
    )]
    public abstract class RunRequirement
    {
        [Tooltip("Invert this condition — met when it would otherwise fail.")]
        [SerializeField]
        private bool _negate;

        /// <summary>
        /// True when this condition holds for <paramref name="state"/>, honouring
        /// <c>_negate</c>. A null state means "no run to test against" and is treated as met,
        /// so edit-time tooling can preview content without a live run.
        /// </summary>
        public bool IsMet(RunState state) => state == null || Check(state) != _negate;

        /// <summary>The raw test, before negation. Implement this, not <see cref="IsMet"/>.</summary>
        protected abstract bool Check(RunState state);

        /// <summary>Human-readable summary, before negation.</summary>
        protected abstract string Describe();

        /// <summary>Human-readable summary including negation.</summary>
        public string GetDescription() => _negate ? $"NOT ({Describe()})" : Describe();

        /// <summary>Guarded wrapper for the inspector InfoBox — see <c>RunOutcome</c>.</summary>
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

    /// <summary>
    /// The direct dependency: true once <see cref="_encounter"/> has been resolved this run.
    /// This is "the vigil only shows up after you've met the Fixer" — and, negated, "these two
    /// events are mutually exclusive".
    /// </summary>
    [Serializable]
    public class HasVisitedEncounter : RunRequirement
    {
        [SerializeField]
        private EncounterData _encounter;

        /// <summary>The gating encounter. Exposed so the dependency graph can draw the edge.</summary>
        public EncounterData Encounter => _encounter;

        protected override bool Check(RunState state) =>
            _encounter != null && state.IsVisited(_encounter.ID);

        protected override string Describe() =>
            _encounter != null ? $"visited {Label(_encounter)}" : "visited (NONE SET)";

        /// <summary>
        /// Display label for an encounter: its DisplayName, or the asset name while that's still
        /// blank. Public because the editor assembly draws it and can't see internals.
        /// </summary>
        public static string Label(EncounterData e) =>
            e == null ? "(none)"
            : string.IsNullOrEmpty(e.DisplayName) ? e.name
            : e.DisplayName;
    }

    /// <summary>True when the run holds at least <see cref="_amount"/> Funds.</summary>
    [Serializable]
    public class FundsAtLeast : RunRequirement
    {
        [SerializeField]
        private int _amount = 50;

        protected override bool Check(RunState state) => state.Funds >= _amount;

        protected override string Describe() => $"Funds ≥ {_amount}";
    }

    /// <summary>True when the run holds at least <see cref="_amount"/> Credibility.</summary>
    [Serializable]
    public class CredibilityAtLeast : RunRequirement
    {
        [SerializeField]
        private int _amount = 25;

        protected override bool Check(RunState state) => state.Credibility >= _amount;

        protected override string Describe() => $"Credibility ≥ {_amount}";
    }

    /// <summary>True when the run has acquired <see cref="_relic"/>.</summary>
    [Serializable]
    public class HasRelic : RunRequirement
    {
        [SerializeField]
        private RelicData _relic;

        protected override bool Check(RunState state) =>
            _relic != null && state.Relics.Contains(_relic);

        protected override string Describe() =>
            _relic != null ? $"has {_relic.RelicName}" : "has relic (NONE SET)";
    }

    /// <summary>
    /// True from <see cref="_day"/> onwards. Overlaps with a pool entry's FirstDay, but works
    /// anywhere a requirement does — including as a boost condition or an option gate.
    /// </summary>
    [Serializable]
    public class DayAtLeast : RunRequirement
    {
        [Min(1)]
        [SerializeField]
        private int _day = 3;

        protected override bool Check(RunState state) => state.Day >= _day;

        protected override string Describe() => $"day ≥ {_day}";
    }
}
