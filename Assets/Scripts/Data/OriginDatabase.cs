using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// The unique in-battle resource an archetype runs on. Faith Leader has none (it runs on statuses).
    /// </summary>
    public enum ArchetypeResource
    {
        None, // Faith Leader and Nepo Baby — no banked resource (statuses / the deck itself)
        // 1 is retired (was Patronage); never reuse it — ordinals are serialized.
        Attention = 2, // Celebrity (Actor) — court attention, spend as a meter hit
    }

    /// <summary>
    /// Central registry of per-archetype (<see cref="OriginType"/>) configuration that is otherwise
    /// scattered across passive assets, tag conventions and code: display name, description, color,
    /// icon, unique resource, starter-deck tag, and the origin's <see cref="OriginPassive"/>.
    /// The single source of truth for per-origin battle data (AP, portrait, passive, resource);
    /// replaced the old OriginStats asset. The Content Audit validates it.
    ///
    /// Create via: Assets → Create → Crookedile → Database → Origin Database
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Database/Origin Database", fileName = "OriginDatabase")]
    public class OriginDatabase : ScriptableObject
    {
        /// <summary>One line of a starter deck: a card and how many copies.</summary>
        [Serializable]
        public class StarterCard
        {
            [Tooltip("The card.")]
            public Cards.CardData Card;

            [Tooltip("Copies in the starting deck.")]
            [MinValue(1)]
            public int Count = 1;
        }

        /// <summary>
        /// The authored starter deck for <paramref name="origin"/>, copies expanded, or an empty
        /// list when none is authored (callers fall back to tag-collected starters).
        /// </summary>
        public List<Cards.CardData> BuildStarterDeck(OriginType origin)
        {
            var deck = new List<Cards.CardData>();
            if (!TryGet(origin, out var entry) || entry.StarterDeck == null)
                return deck;
            foreach (var line in entry.StarterDeck)
                if (line?.Card != null)
                    for (int i = 0; i < Math.Max(1, line.Count); i++)
                        deck.Add(line.Card);
            return deck;
        }

        [Serializable]
        public struct Entry
        {
            // Art on the left, everything else beside it: an origin is judged as a whole, and
            // scrolling between its portrait and its numbers made that harder than it needed to be.
            [HorizontalGroup("Origin", 76)]
            [VerticalGroup("Origin/Art")]
            [PreviewField(72, ObjectFieldAlignment.Left)]
            [HideLabel]
            [Tooltip("Character portrait shown in the player slot during battle.")]
            public Sprite Portrait;

            [VerticalGroup("Origin/Art")]
            [PreviewField(72, ObjectFieldAlignment.Left)]
            [HideLabel]
            [Tooltip("Small UI icon (menus, labels).")]
            public Sprite Icon;

            [BoxGroup("Origin/Right/Identity", LabelText = "Identity")]
            [HorizontalGroup("Origin/Right", LabelWidth = 90)]
            [EnumToggleButtons]
            public OriginType Type;

            [BoxGroup("Origin/Right/Identity")]
            public string DisplayName;

            [BoxGroup("Origin/Right/Identity")]
            [TextArea(2, 4)]
            public string Description;

            [BoxGroup("Origin/Right/Identity")]
            public Color Color;

            [BoxGroup("Origin/Right/Battle", LabelText = "Battle")]
            [EnumToggleButtons]
            public ArchetypeResource Resource;

            [BoxGroup("Origin/Right/Battle")]
            [Required("Without a passive this origin plays as a plain deck with no identity.")]
            [Tooltip("The origin's starter passive asset.")]
            public OriginPassive Passive;

            [BoxGroup("Origin/Right/Battle")]
            [Tooltip(
                "Tag used to collect this origin's starter cards (CardDatabase.GetStarterDeck)."
            )]
            [ValidateInput(
                "@!string.IsNullOrWhiteSpace(StarterTag)",
                "No starter tag — GetStarterDeck returns nothing and this origin begins with an "
                    + "empty deck.",
                InfoMessageType.Error
            )]
            public string StarterTag;

            [BoxGroup("Origin/Right/Battle")]
            [Tooltip(
                "The deck this origin starts every run with, card by card with copies. The one "
                    + "source for the campaign and the battle test starter. Empty falls back to "
                    + "one copy of each card tagged with the Starter Tag."
            )]
            [TableList(AlwaysExpanded = true)]
            public List<StarterCard> StarterDeck;

            [BoxGroup("Origin/Right/Battle")]
            [PropertyRange(1, 10)]
            [Tooltip("Max Action Points per turn.")]
            public int MaxActionPoints;

            [BoxGroup("Origin/Right/Campaign", LabelText = "Campaign start")]
            [Tooltip(
                "Funds this origin starts a run with. A real design lever, not flavour — "
                    + "Nepo Baby starting rich and Faith Leader starting broke changes which "
                    + "event options are even reachable on day one."
            )]
            [ValidateInput(
                "@StartingFunds > 0",
                "0 Funds — every FundsAtLeast gate fails all run, so those options are dead.",
                InfoMessageType.Warning
            )]
            public int StartingFunds;

            [BoxGroup("Origin/Right/Campaign")]
            [Tooltip("Credibility this origin starts a run with.")]
            [ValidateInput(
                "@StartingCredibility > 0",
                "0 Credibility — CredibilityAtLeast gates can never pass, and percentage-based "
                    + "Credibility outcomes scale off this, so they do nothing.",
                InfoMessageType.Warning
            )]
            public int StartingCredibility;

            [BoxGroup("Origin/Right/Campaign")]
            [Tooltip("Hours per campaign day. 0 falls back to the run’s default.")]
            [LabelText("@MaxHours == 0 ? \"Hours/day (default 8)\" : \"Hours/day\"")]
            public int MaxHours;

            /// <summary>Row label for the entries list, so origins read without expanding.</summary>
            private string Summary =>
                $"{(string.IsNullOrEmpty(DisplayName) ? Type.ToString() : DisplayName)}"
                + $"  —  {StartingFunds}F / {StartingCredibility}C / {(MaxHours == 0 ? RunState.DEFAULT_MAX_HOURS : MaxHours)}h";
        }

        // BuildMap assigns by Type, so a second row for the same origin silently replaces the
        // first — everything looks authored and half of it is never read.
        [InfoBox(
            "@DuplicateTypeWarning()",
            InfoMessageType.Error,
            VisibleIf = "@!string.IsNullOrEmpty(DuplicateTypeWarning())"
        )]
        [InfoBox(
            "@MissingTypeWarning()",
            InfoMessageType.Warning,
            VisibleIf = "@!string.IsNullOrEmpty(MissingTypeWarning())"
        )]
        [ListDrawerSettings(ListElementLabelName = "Summary", ShowFoldout = true)]
        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        /// <summary>Origins listed more than once — the later row silently wins.</summary>
        private string DuplicateTypeWarning()
        {
            if (_entries == null)
                return "";
            var seen = new HashSet<OriginType>();
            var dupes = new List<OriginType>();
            foreach (var e in _entries)
                if (!seen.Add(e.Type) && !dupes.Contains(e.Type))
                    dupes.Add(e.Type);

            return dupes.Count == 0
                ? ""
                : $"Listed twice: {string.Join(", ", dupes)}. The last row wins and the earlier "
                    + "one is never read — delete the duplicate.";
        }

        /// <summary>Origins the enum defines but this database has no row for.</summary>
        private string MissingTypeWarning()
        {
            var missing = new List<OriginType>();
            foreach (OriginType type in Enum.GetValues(typeof(OriginType)))
                if (_entries == null || Array.FindIndex(_entries, e => e.Type == type) < 0)
                    missing.Add(type);

            return missing.Count == 0
                ? ""
                : $"No row for {string.Join(", ", missing)} — a run started as one of those gets "
                    + "zero Funds, zero Credibility and no passive.";
        }

        public IReadOnlyList<Entry> Entries => _entries;

        private Dictionary<OriginType, Entry> _map;

        private void OnEnable() => BuildMap();

        private void BuildMap()
        {
            _map = new Dictionary<OriginType, Entry>();
            if (_entries == null)
                return;
            foreach (var e in _entries)
                _map[e.Type] = e;
        }

        public bool TryGet(OriginType type, out Entry entry)
        {
            if (_map == null)
                BuildMap();
            return _map.TryGetValue(type, out entry);
        }

        public ArchetypeResource GetResource(OriginType type) =>
            TryGet(type, out var e) ? e.Resource : ArchetypeResource.None;

        public OriginPassive GetPassive(OriginType type) =>
            TryGet(type, out var e) ? e.Passive : null;

        #region Campaign start
        private const string ResourcePath = "Databases/OriginDatabase";
        private static OriginDatabase _cached;

        /// <summary>
        /// The shared instance, resolved by path. Lets <see cref="RunState.Create"/> apply an
        /// origin's starting values without every caller having to look them up and remember to
        /// pass them along.
        /// </summary>
        public static OriginDatabase Shared
        {
            get
            {
                // Re-resolves on null rather than caching the miss — a domain reload clears this.
                if (_cached == null)
                    _cached = Resources.Load<OriginDatabase>(ResourcePath);
                return _cached;
            }
        }

        /// <summary>
        /// Starting campaign values for <paramref name="type"/>. Returns all zeros when the
        /// origin has no entry, which is the same as "starts with nothing" — a missing entry
        /// should never block a run from being created.
        /// </summary>
        public (int funds, int credibility, int maxHours) GetCampaignStart(OriginType type) =>
            TryGet(type, out var e)
                ? (e.StartingFunds, e.StartingCredibility, e.MaxHours)
                : (0, 0, 0);

        #endregion
    }
}
