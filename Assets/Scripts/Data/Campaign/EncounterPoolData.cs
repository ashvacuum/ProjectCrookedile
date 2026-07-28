using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// One encounter's availability window in an <see cref="EncounterPoolData"/>: which days
    /// it can appear on, and how likely it is relative to everything else eligible that day.
    /// </summary>
    [Serializable]
    public class EncounterPoolEntry
    {
        [SerializeField]
        private EncounterData _encounter;

        [Tooltip("First day this can appear, inclusive.")]
        [Min(1)]
        [SerializeField]
        private int _firstDay = 1;

        [Tooltip("Last day this can appear, inclusive. 0 means no end — available forever.")]
        [Min(0)]
        [SerializeField]
        private int _lastDay;

        [Tooltip(
            "Per-pool override for this encounter's draw weight.\n"
                + "Leave at -1 to inherit EncounterData.DropWeight — the normal case. Set a "
                + "value only when this encounter should be rarer or commoner in THIS pool "
                + "than it is by default. 0 disables it here without removing the row."
        )]
        [SerializeField]
        private float _weight = InheritWeight;

        /// <summary>Sentinel for "_weight is unset, inherit from the encounter".</summary>
        // ponytail: negative sentinel rather than a bool + ShowIf pair. One field, and the
        // tooltip carries the meaning. Swap to the toggle if designers keep typing 0 for
        // "default" and silently disabling rows.
        public const float InheritWeight = -1f;

        [Tooltip("Once drawn, never offered again this run.")]
        [SerializeField]
        private bool _oncePerRun = true;

        public EncounterData Encounter => _encounter;
        public int FirstDay => _firstDay;
        public int LastDay => _lastDay;
        public bool OncePerRun => _oncePerRun;

        /// <summary>True when this row is inheriting rather than overriding its weight.</summary>
        public bool InheritsWeight => _weight < 0f;

        /// <summary>
        /// The weight actually used when drawing: this row's override, or the encounter's own
        /// <see cref="EncounterData.DropWeight"/> when left unset. Zero when no encounter is
        /// assigned, so a blank row can never be drawn.
        /// </summary>
        public float ResolvedWeight =>
            _encounter == null ? 0f
            : InheritsWeight ? _encounter.DropWeight
            : _weight;

        /// <summary>
        /// Stable key for "already seen this" — the encounter's serialized GUID, so renaming
        /// or moving the asset doesn't reset its visited state.
        /// </summary>
        public string Id => _encounter != null ? _encounter.ID : null;

        /// <summary>True when this entry's window covers <paramref name="day"/>.</summary>
        public bool IsEligibleOn(int day) =>
            _encounter != null
            && ResolvedWeight > 0f
            && day >= _firstDay
            && (_lastDay <= 0 || day <= _lastDay);
    }

    /// <summary>
    /// The set of encounters a run can draw from, with per-entry day windows and weights.
    /// Replaces hand-placing every location: a day's offering is drawn from whatever is
    /// eligible that day.
    ///
    /// Draws are **deterministic from (seed, day)** — the same seed always produces the same
    /// campaign, which is what makes a seed shareable and a bug reproducible. Uses its own
    /// <see cref="System.Random"/> rather than <c>RandomHelper</c>/<c>UnityEngine.Random</c>
    /// on purpose: seeding the campaign must not perturb battle RNG, and vice versa.
    ///
    /// Author the entries here; visualise the day windows via Crookedile → Encounter Gantt.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → Encounter Pool
    /// </summary>
    [CreateAssetMenu(
        menuName = "Crookedile/Campaign/Encounter Pool",
        fileName = "New Encounter Pool"
    )]
    public class EncounterPoolData : ScriptableObject
    {
        [Tooltip("How many days the campaign runs. Drives the Gantt view's column count.")]
        [Min(1)]
        [SerializeField]
        private int _days = 7;

        [TableList]
        [SerializeField]
        private List<EncounterPoolEntry> _entries = new List<EncounterPoolEntry>();

        public int Days => _days;
        public IReadOnlyList<EncounterPoolEntry> Entries => _entries;

        #region Queries
        /// <summary>Every entry whose window covers <paramref name="day"/>.</summary>
        public IEnumerable<EncounterPoolEntry> EligibleOn(int day)
        {
            foreach (var entry in _entries)
                if (entry != null && entry.IsEligibleOn(day))
                    yield return entry;
        }

        /// <summary>
        /// Summed weight of everything eligible on <paramref name="day"/>. Zero means that day
        /// has nothing to offer — the failure the Gantt view exists to make visible.
        /// </summary>
        public float TotalWeightOn(int day)
        {
            float total = 0f;
            foreach (var entry in EligibleOn(day))
                total += entry.ResolvedWeight;
            return total;
        }

        #endregion

        #region Drawing
        /// <summary>
        /// Draws up to <paramref name="count"/> distinct encounters for <paramref name="day"/>.
        /// Deterministic for a given <paramref name="seed"/> and day. Returns fewer than
        /// requested (possibly none) when the eligible set is too small — the caller decides
        /// whether that is a content bug or an acceptable quiet day.
        /// </summary>
        /// <param name="exclude">
        /// Ids already consumed this run (<c>RunState.VisitedLocationIds</c>). Only filters
        /// entries marked <see cref="EncounterPoolEntry.OncePerRun"/>.
        /// </param>
        public List<EncounterData> DrawForDay(
            int day,
            int count,
            int seed,
            ICollection<string> exclude = null
        )
        {
            var drawn = new List<EncounterData>();
            // One RNG per (seed, day) rather than per draw: successive picks within a day walk
            // the same stream, so day N is reproducible without depending on day N-1 running first.
            var rng = new System.Random(unchecked(seed * 397) ^ day);
            var takenThisDay = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                var pick = DrawOne(day, rng, exclude, takenThisDay);
                if (pick == null)
                    break;
                takenThisDay.Add(pick.Id);
                drawn.Add(pick.Encounter);
            }
            return drawn;
        }

        private EncounterPoolEntry DrawOne(
            int day,
            System.Random rng,
            ICollection<string> exclude,
            HashSet<string> takenThisDay
        )
        {
            float total = 0f;
            var candidates = new List<EncounterPoolEntry>();
            foreach (var entry in EligibleOn(day))
            {
                if (takenThisDay.Contains(entry.Id))
                    continue;
                if (entry.OncePerRun && exclude != null && exclude.Contains(entry.Id))
                    continue;
                candidates.Add(entry);
                total += entry.ResolvedWeight;
            }

            if (candidates.Count == 0 || total <= 0f)
                return null;

            double roll = rng.NextDouble() * total;
            float running = 0f;
            foreach (var entry in candidates)
            {
                running += entry.ResolvedWeight;
                if (roll <= running)
                    return entry;
            }
            return candidates[candidates.Count - 1]; // float drift guard
        }

        #endregion
    }
}
