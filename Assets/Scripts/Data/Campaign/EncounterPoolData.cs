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
        [Required("Blank rows can never be drawn — pick an encounter or delete the row.")]
        [AssetsOnly]
        [TableColumnWidth(190)]
        [SerializeField]
        private EncounterData _encounter;

        [Tooltip("First day this can appear, inclusive.")]
        [Min(1)]
        [TableColumnWidth(70, Resizable = false)]
        [SerializeField]
        private int _firstDay = 1;

        [Tooltip("Last day this can appear, inclusive. 0 means no end — available forever.")]
        [Min(0)]
        [TableColumnWidth(70, Resizable = false)]
        [SerializeField]
        private int _lastDay;

        [Tooltip(
            "Per-pool override for this encounter's draw weight.\n"
                + "Leave at -1 to inherit EncounterData.DropWeight — the normal case. Set a "
                + "value only when this encounter should be rarer or commoner in THIS pool "
                + "than it is by default. 0 disables it here without removing the row."
        )]
        [ValidateInput(
            "@_weight != 0f || _guaranteed",
            "Weight 0 and not guaranteed — this row can never be drawn.",
            InfoMessageType.Warning
        )]
        [TableColumnWidth(80, Resizable = false)]
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

        [Tooltip(
            "Always appears on every day in its window, ahead of the random picks.\n"
                + "This is how a day-7 boss or a day-1 opener is made certain — a weight alone "
                + "only makes it likely, and 'likely' is not a structure you can design around."
        )]
        [SerializeField]
        private bool _guaranteed;

        [TableColumnWidth(200)]
        [Header("Dependencies")]
        [Tooltip(
            "Hard gate: ALL must hold or this encounter can't appear at all.\n"
                + "Use for \"B only exists once you've done A\"."
        )]
        [SerializeReference]
        [SerializeField]
        private List<RunRequirement> _requirements = new List<RunRequirement>();

        [Tooltip(
            "Soft nudge: when ALL of these hold, the draw weight is multiplied by Boost "
                + "Multiplier. The encounter stays available either way.\n"
                + "Use for \"A makes B more likely\"."
        )]
        [SerializeReference]
        [SerializeField]
        private List<RunRequirement> _boostIf = new List<RunRequirement>();

        [Tooltip("Weight multiplier applied when every Boost If condition holds.")]
        [Min(0f)]
        [SerializeField]
        private float _boostMultiplier = 2f;

        public EncounterData Encounter => _encounter;

#if UNITY_EDITOR
        /// <summary>Editor-only wiring for the pool's "New Event/Battle" buttons.</summary>
        internal void EditorSetEncounter(EncounterData encounter) => _encounter = encounter;
#endif

        public int FirstDay => _firstDay;
        public int LastDay => _lastDay;
        public bool OncePerRun => _oncePerRun;
        public bool Guaranteed => _guaranteed;

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

        /// <summary>True when every hard requirement holds. A null state skips the check.</summary>
        public bool RequirementsMet(RunState state)
        {
            if (state == null)
                return true;
            foreach (var req in _requirements)
                if (req != null && !req.IsMet(state))
                    return false;
            return true;
        }

        /// <summary>True when every boost condition holds — and there is at least one.</summary>
        public bool BoostActive(RunState state)
        {
            if (state == null || _boostIf.Count == 0)
                return false;
            foreach (var req in _boostIf)
                if (req != null && !req.IsMet(state))
                    return false;
            return true;
        }

        /// <summary>
        /// Draw weight for this run's current state: <see cref="ResolvedWeight"/>, multiplied
        /// when the boost conditions hold.
        /// </summary>
        public float WeightFor(RunState state) =>
            BoostActive(state) ? ResolvedWeight * _boostMultiplier : ResolvedWeight;

        /// <summary>
        /// True when this entry's window covers <paramref name="day"/> and its requirements hold.
        /// A guaranteed entry ignores weight — it isn't competing for a slot, so authoring one
        /// with weight 0 (a reasonable thing to assume) must not silently remove it.
        /// </summary>
        /// <param name="state">
        /// Null means "no run to test against", which skips requirement checks so edit-time
        /// tooling can show every authored entry rather than an empty board.
        /// </param>
        public bool IsEligibleOn(int day, RunState state = null) =>
            _encounter != null
            && (_guaranteed || ResolvedWeight > 0f)
            && day >= _firstDay
            && (_lastDay <= 0 || day <= _lastDay)
            && RequirementsMet(state);

        /// <summary>True when this entry gates or boosts on anything — used by tooling.</summary>
        public bool HasDependencies => _requirements.Count > 0 || _boostIf.Count > 0;

        public IReadOnlyList<RunRequirement> Requirements => _requirements;
        public IReadOnlyList<RunRequirement> BoostIf => _boostIf;
        public float BoostMultiplier => _boostMultiplier;

        /// <summary>Human-readable dependency summary for tooling.</summary>
        public string DescribeDependencies()
        {
            var parts = new List<string>();
            foreach (var r in _requirements)
                if (r != null)
                    parts.Add($"needs {r.GetDescription()}");
            foreach (var r in _boostIf)
                if (r != null)
                    parts.Add($"×{_boostMultiplier:0.##} if {r.GetDescription()}");
            return string.Join(", ", parts);
        }
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
    /// Author the entries here; visualise the day windows via Crookedile → Encounter Designer.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → Encounter Pool
    /// </summary>
    [CreateAssetMenu(
        menuName = "Crookedile/Campaign/Encounter Pool",
        fileName = "New Encounter Pool"
    )]
    // Coverage is the failure this asset is prone to and the one you cannot see by reading the
    // rows: a day with nothing eligible hands the player an empty map. Surfaced here rather
    // than only in Content Hub, because here is where the windows get typed.
    [InfoBox(
        "@CoverageWarning()",
        InfoMessageType.Error,
        VisibleIf = "@!string.IsNullOrEmpty(CoverageWarning())"
    )]
    public class EncounterPoolData : ScriptableObject
    {
        [Tooltip("How many days the campaign runs. Drives the Gantt view's column count.")]
        [PropertyRange(1, 30)]
        [SerializeField]
        private int _days = 7;

        [TableList]
        [SerializeField]
        private List<EncounterPoolEntry> _entries = new List<EncounterPoolEntry>();

        public int Days => _days;
        public IReadOnlyList<EncounterPoolEntry> Entries => _entries;

        /// <summary>
        /// Days with nothing eligible, as an inspector warning. Empty string when the week is
        /// covered. Requirements are skipped (no run to test against), so this is the optimistic
        /// reading — a day named here is empty even before gates narrow it.
        /// </summary>
        private string CoverageWarning()
        {
            var empty = new List<int>();
            for (int day = 1; day <= _days; day++)
            {
                bool any = false;
                foreach (var entry in _entries)
                    if (entry != null && entry.IsEligibleOn(day))
                    {
                        any = true;
                        break;
                    }
                if (!any)
                    empty.Add(day);
            }

            return empty.Count == 0
                ? ""
                : $"Nothing is eligible on day{(empty.Count > 1 ? "s" : "")} "
                    + $"{string.Join(", ", empty)} — the player gets an empty map. Widen a "
                    + "Last Day, or add an entry with no end day.";
        }

#if UNITY_EDITOR
        #region Authoring
        // Creating an encounter used to be: right-click Create in the right folder, name it,
        // come back here, add a row, drag it in. Four steps and a trip through the Project
        // window to add one encounter to a week. These do all of it in one click, and leave the
        // new asset selected so it can be renamed and filled in immediately.

        [ButtonGroup("New")]
        [Button("New Event", ButtonSizes.Medium)]
        private void CreateEventEncounter() => CreateEncounter<EventEncounterData>("New Event");

        [ButtonGroup("New")]
        [Button("New Battle", ButtonSizes.Medium)]
        private void CreateBattleEncounter() => CreateEncounter<BattleEncounterData>("New Battle");

        private void CreateEncounter<T>(string baseName)
            where T : EncounterData
        {
            // Beside the pool, so a pool and its content stay together without asking where.
            string folder = System.IO.Path.GetDirectoryName(
                UnityEditor.AssetDatabase.GetAssetPath(this)
            );
            if (string.IsNullOrEmpty(folder))
                folder = "Assets";

            var encounter = CreateInstance<T>();
            string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{baseName}.asset"
            );
            UnityEditor.AssetDatabase.CreateAsset(encounter, path);

            var entry = new EncounterPoolEntry();
            entry.EditorSetEncounter(encounter);
            _entries.Add(entry);

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Selection.activeObject = encounter;
        }

        #endregion
#endif

        #region Queries
        /// <summary>
        /// Every entry whose window covers <paramref name="day"/> and whose requirements hold.
        /// Pass null for <paramref name="state"/> to ignore requirements (edit-time preview).
        /// </summary>
        public IEnumerable<EncounterPoolEntry> EligibleOn(int day, RunState state = null)
        {
            foreach (var entry in _entries)
                if (entry != null && entry.IsEligibleOn(day, state))
                    yield return entry;
        }

        /// <summary>
        /// Summed weight of everything eligible on <paramref name="day"/>. Zero means that day
        /// has nothing to offer — the failure the Gantt view exists to make visible.
        /// </summary>
        public float TotalWeightOn(int day, RunState state = null)
        {
            float total = 0f;
            foreach (var entry in EligibleOn(day, state))
                total += entry.WeightFor(state);
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
        /// <param name="state">
        /// The live run, used to evaluate dependency requirements and weight boosts. Null skips
        /// both, which is what edit-time previews want.
        /// </param>
        public List<EncounterData> DrawForDay(
            int day,
            int count,
            int seed,
            ICollection<string> exclude = null,
            RunState state = null
        )
        {
            var drawn = new List<EncounterData>();
            // One RNG per (seed, day) rather than per draw: successive picks within a day walk
            // the same stream, so day N is reproducible without depending on day N-1 running first.
            var rng = new System.Random(unchecked(seed * 397) ^ day);
            var takenThisDay = new HashSet<string>();

            // Guaranteed entries come first and ignore `count` — a day-7 boss that got crowded
            // out by a full slate of random events would silently break the run's structure.
            foreach (var entry in EligibleOn(day, state))
            {
                if (!entry.Guaranteed)
                    continue;
                if (entry.OncePerRun && exclude != null && exclude.Contains(entry.Id))
                    continue;
                if (!takenThisDay.Add(entry.Id))
                    continue;
                drawn.Add(entry.Encounter);
            }

            for (int i = drawn.Count; i < count; i++)
            {
                var pick = DrawOne(day, rng, exclude, takenThisDay, state);
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
            HashSet<string> takenThisDay,
            RunState state
        )
        {
            float total = 0f;
            var candidates = new List<EncounterPoolEntry>();
            foreach (var entry in EligibleOn(day, state))
            {
                if (takenThisDay.Contains(entry.Id))
                    continue;
                if (entry.OncePerRun && exclude != null && exclude.Contains(entry.Id))
                    continue;
                candidates.Add(entry);
                total += entry.WeightFor(state);
            }

            if (candidates.Count == 0 || total <= 0f)
                return null;

            double roll = rng.NextDouble() * total;
            float running = 0f;
            foreach (var entry in candidates)
            {
                running += entry.WeightFor(state);
                if (roll <= running)
                    return entry;
            }
            return candidates[candidates.Count - 1]; // float drift guard
        }

        #endregion
    }
}
