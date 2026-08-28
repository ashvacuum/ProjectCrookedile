using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Encounter Designer — two views over one <see cref="EncounterPoolData"/>.
    ///
    /// <para><b>Timeline</b> — a day-by-day Gantt. Each entry is a bar spanning the days it can
    /// appear on, so gaps and pile-ups are visible at a glance instead of inferred from a list of
    /// number pairs. Two things a table can't show: a day with nothing eligible (flagged red in
    /// the coverage strip — that day would hand the player an empty map), and what a seed
    /// actually generates (hit Roll and every day's real draw appears under its column, from the
    /// same call the campaign makes at runtime).</para>
    ///
    /// <para><b>Dependencies</b> — the unlock graph. Nodes are encounters, laid out left to right
    /// by how deep they sit in a chain; a solid arrow is a hard gate, a dotted one a weight
    /// boost. The Timeline can't express this: a gated entry's bar shows when it *could* appear,
    /// not whether it will, which is why those rows are tagged <c>[dep]</c> there.</para>
    ///
    /// Both views are read-only. Authoring stays in the pool asset's inspector, where Odin's
    /// type-picker already handles the polymorphic requirement lists.
    ///
    /// Menu: Crookedile → Encounter Designer.
    /// </summary>
    public class EncounterDesignerWindow : EditorWindow
    {
        [MenuItem("Crookedile/Encounter Designer")]
        public static void ShowWindow()
        {
            var win = GetWindow<EncounterDesignerWindow>("Encounter Designer");
            win.minSize = new Vector2(720, 400);
            win.Show();
        }

        #region Layout constants
        private const float LabelWidth = 190f;
        private const float RowHeight = 22f;
        private const float RowGap = 3f;
        private const float MinDayWidth = 54f;

        #endregion

        private enum Tab
        {
            Timeline,
            Dependencies,
        }

        private static readonly string[] TabNames = { "Timeline", "Dependencies" };

        private EncounterPoolData _pool;
        private Tab _tab;
        private int _seed = 12345;
        private int _perDay = 3;
        private Vector2 _scroll;

        /// <summary>Per-node drag offsets, keyed by encounter id. Deliberately not persisted.</summary>
        private readonly Dictionary<string, Vector2> _nodeDrag = new Dictionary<string, Vector2>();
        private string _draggingId;
        private Vector2 _graphScroll;

        /// <summary>Simulated draw per day, indexed day-1. Null until Roll is pressed.</summary>
        private List<EncounterData>[] _rolled;

        private void OnGUI()
        {
            DrawToolbar();

            if (_pool == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign an Encounter Pool to see its day timeline.",
                    MessageType.Info
                );
                return;
            }
            if (_pool.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This pool has no entries. Add them in the pool asset's inspector.",
                    MessageType.Warning
                );
                return;
            }

            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames, GUILayout.Height(22f));
            EditorGUILayout.Space(4f);

            if (_tab == Tab.Dependencies)
            {
                DrawDependencyTab();
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            float dayWidth = Mathf.Max(
                MinDayWidth,
                (position.width - LabelWidth - 40f) / _pool.Days
            );

            DrawDayHeader(dayWidth);
            DrawEntryRows(dayWidth);
            EditorGUILayout.Space(6f);
            DrawCoverageStrip(dayWidth);
            EditorGUILayout.LabelField(
                "w2 = weight overridden on this row    w2* = inherited from the encounter's DropWeight    "
                    + "ALWAYS = guaranteed    [dep] = gated or boosted, see the Dependencies tab",
                EditorStyles.miniLabel
            );

            if (_rolled != null)
            {
                EditorGUILayout.Space(10f);
                DrawRolledPreview(dayWidth);
            }
            EditorGUILayout.EndScrollView();
        }

        #region Toolbar
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _pool = (EncounterPoolData)
                EditorGUILayout.ObjectField(
                    _pool,
                    typeof(EncounterPoolData),
                    false,
                    GUILayout.Width(220f)
                );

            GUILayout.Space(12f);
            GUILayout.Label("Seed", EditorStyles.miniLabel, GUILayout.Width(32f));
            _seed = EditorGUILayout.IntField(
                _seed,
                EditorStyles.toolbarTextField,
                GUILayout.Width(80f)
            );

            if (GUILayout.Button("Random", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                _seed = Random.Range(1, int.MaxValue);
                Roll();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Per day", EditorStyles.miniLabel, GUILayout.Width(48f));
            _perDay = Mathf.Max(
                1,
                EditorGUILayout.IntField(
                    _perDay,
                    EditorStyles.toolbarTextField,
                    GUILayout.Width(36f)
                )
            );

            if (GUILayout.Button("Roll", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                Roll();
            if (
                _rolled != null
                && GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(48f))
            )
                _rolled = null;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Runs the real <see cref="EncounterPoolData.DrawForDay"/> for every day, carrying
        /// once-per-run exclusions forward exactly as a live run would — otherwise the preview
        /// would show day 5 re-offering something day 2 already consumed.
        /// </summary>
        private void Roll()
        {
            if (_pool == null)
                return;
            _rolled = new List<EncounterData>[_pool.Days];
            var consumed = new HashSet<string>();

            for (int day = 1; day <= _pool.Days; day++)
            {
                var picks = _pool.DrawForDay(day, _perDay, _seed, consumed);
                _rolled[day - 1] = picks;
                foreach (var pick in picks)
                    if (pick != null)
                        consumed.Add(pick.ID);
            }
        }

        #endregion

        #region Timeline
        private void DrawDayHeader(float dayWidth)
        {
            Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            for (int day = 1; day <= _pool.Days; day++)
            {
                var cell = new Rect(
                    row.x + LabelWidth + (day - 1) * dayWidth,
                    row.y,
                    dayWidth,
                    RowHeight
                );
                GUI.Label(cell, $"Day {day}", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawEntryRows(float dayWidth)
        {
            foreach (var entry in _pool.Entries)
            {
                if (entry == null)
                    continue;

                Rect row = GUILayoutUtility.GetRect(
                    0f,
                    RowHeight + RowGap,
                    GUILayout.ExpandWidth(true)
                );
                var labelRect = new Rect(row.x, row.y, LabelWidth - 6f, RowHeight);

                if (entry.Encounter == null)
                {
                    EditorGUI.LabelField(labelRect, "(no encounter set)", ErrorLabel);
                    continue;
                }

                // Click the label to ping the asset — the usual reason you're looking at this
                // row is to go edit the thing it names.
                if (GUI.Button(labelRect, entry.Encounter.name, EditorStyles.label))
                    EditorGUIUtility.PingObject(entry.Encounter);

                int last = entry.LastDay <= 0 ? _pool.Days : Mathf.Min(entry.LastDay, _pool.Days);
                if (entry.FirstDay > last)
                {
                    var warn = new Rect(row.x + LabelWidth, row.y, 300f, RowHeight);
                    EditorGUI.LabelField(
                        warn,
                        "unreachable — starts after the last day",
                        ErrorLabel
                    );
                    continue;
                }

                float x = row.x + LabelWidth + (entry.FirstDay - 1) * dayWidth;
                var bar = new Rect(
                    x + 1f,
                    row.y + 2f,
                    (last - entry.FirstDay + 1) * dayWidth - 2f,
                    RowHeight - 4f
                );
                EditorGUI.DrawRect(bar, BarColor(entry.Encounter));
                // "w2" = overridden on this row, "w2*" = inherited from the encounter's own
                // DropWeight. Worth distinguishing: it's the difference between "this pool
                // made it rare" and "it's rare everywhere".
                string weightLabel = entry.Guaranteed
                    ? "ALWAYS"
                    : $"w{entry.ResolvedWeight:0.##}{(entry.InheritsWeight ? "*" : "")}";
                // A gated entry's bar overstates its availability — the window is when it *could*
                // appear, not when it will. Flag it so the timeline isn't read as the whole truth.
                string dep = entry.HasDependencies ? "  [dep]" : "";
                GUI.Label(
                    bar,
                    $"  {weightLabel}{(entry.OncePerRun ? "  once" : "")}{dep}",
                    BarLabel
                );
            }
        }

        /// <summary>
        /// Per-day eligible count and total weight. A zero-eligible day is drawn red — it is
        /// the one authoring mistake in this asset that silently produces an empty map.
        /// </summary>
        private void DrawCoverageStrip(float dayWidth)
        {
            Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            EditorGUI.LabelField(
                new Rect(row.x, row.y, LabelWidth - 6f, RowHeight),
                "Eligible / weight",
                EditorStyles.miniBoldLabel
            );

            for (int day = 1; day <= _pool.Days; day++)
            {
                int count = 0;
                foreach (var _ in _pool.EligibleOn(day))
                    count++;
                float weight = _pool.TotalWeightOn(day);

                var cell = new Rect(
                    row.x + LabelWidth + (day - 1) * dayWidth,
                    row.y,
                    dayWidth - 2f,
                    RowHeight
                );
                EditorGUI.DrawRect(cell, count == 0 ? EmptyDay : Color.clear);
                GUI.Label(
                    cell,
                    $" {count} / {weight:0.##}",
                    count == 0 ? ErrorLabel : EditorStyles.miniLabel
                );
            }
        }

        private void DrawRolledPreview(float dayWidth)
        {
            EditorGUILayout.LabelField(
                $"Seed {_seed} — what this campaign actually generates",
                EditorStyles.boldLabel
            );

            int tallest = 1;
            foreach (var picks in _rolled)
                tallest = Mathf.Max(tallest, picks?.Count ?? 0);

            for (int slot = 0; slot < tallest; slot++)
            {
                Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
                for (int day = 1; day <= _pool.Days; day++)
                {
                    var picks = _rolled[day - 1];
                    var cell = new Rect(
                        row.x + LabelWidth + (day - 1) * dayWidth,
                        row.y,
                        dayWidth - 2f,
                        RowHeight - 2f
                    );

                    if (picks == null || slot >= picks.Count)
                    {
                        EditorGUI.DrawRect(cell, EmptyDay);
                        GUI.Label(cell, " —", ErrorLabel);
                        continue;
                    }
                    EditorGUI.DrawRect(cell, BarColor(picks[slot]));
                    GUI.Label(cell, $" {picks[slot].name}", BarLabel);
                }
            }

            EditorGUILayout.HelpBox(
                "A dash means the pool ran dry for that slot — not enough distinct eligible "
                    + "encounters remain that day once once-per-run picks are spent.",
                MessageType.None
            );
        }

        #endregion

        #region Dependency graph
        // ponytail: read-only view with auto-layout, not a node *editor*. Authoring the
        // requirement lists already works in the pool inspector via Odin's type picker;
        // rebuilding that as connect-the-dots would be a lot of code to replace something that
        // isn't broken. This exists to answer "what unlocks what", which the inspector can't show.
        private const float NodeW = 168f;
        private const float NodeH = 44f;
        private const float ColGap = 90f;
        private const float RowGapY = 18f;

        private void DrawDependencyTab()
        {
            var entries = _pool.Entries.Where(e => e?.Encounter != null).ToList();
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No entries with an encounter assigned.", MessageType.Info);
                return;
            }

            // Two encounters sharing an id would collide in every id-keyed dictionary below.
            // Report it and carry on with one of each: an exception per repaint tells the
            // designer nothing about which assets are at fault.
            var duplicates = entries
                .GroupBy(e => e.Id)
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicates.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    "Encounters sharing an ID — duplicated assets. Re-save each to reassign:\n"
                        + string.Join(
                            "\n",
                            duplicates.Select(g =>
                                $"  {g.Key}: {string.Join(", ", g.Select(e => e.Encounter.name))}"
                            )
                        ),
                    MessageType.Error
                );
                entries = entries.GroupBy(e => e.Id).Select(g => g.First()).ToList();
            }

            // Depth = longest hard-requirement chain leading here. Gives left-to-right reading
            // order for free: day-one content on the left, things it unlocks to the right.
            var depth = ComputeDepths(entries);
            var positions = LayoutNodes(entries, depth);

            int columns = depth.Values.DefaultIfEmpty(0).Max() + 1;
            float canvasW = columns * (NodeW + ColGap) + 40f;
            float canvasH = entries.Count * (NodeH + RowGapY) + 60f;

            _graphScroll = EditorGUILayout.BeginScrollView(_graphScroll);
            Rect canvas = GUILayoutUtility.GetRect(canvasW, canvasH);

            DrawEdges(entries, positions, canvas);
            DrawNodes(entries, positions, canvas);
            HandleNodeDrag(entries, positions, canvas);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(
                "Solid arrow = hard gate (target can't appear until source is visited)    "
                    + "Dotted = weight boost    Drag nodes to untangle (not saved)",
                EditorStyles.miniLabel
            );
        }

        /// <summary>
        /// Longest chain of <c>HasVisitedEncounter</c> requirements ending at each entry.
        /// Iterative relaxation rather than recursion so a cyclic authoring mistake settles
        /// instead of blowing the stack.
        /// </summary>
        private Dictionary<string, int> ComputeDepths(List<EncounterPoolEntry> entries)
        {
            var depth = entries.ToDictionary(e => e.Id, _ => 0);
            var byId = entries.ToDictionary(e => e.Id, e => e);

            for (int pass = 0; pass < entries.Count; pass++)
            {
                bool changed = false;
                foreach (var entry in entries)
                {
                    foreach (string sourceId in HardSources(entry))
                    {
                        if (!byId.ContainsKey(sourceId))
                            continue;
                        int candidate = depth[sourceId] + 1;
                        if (candidate > depth[entry.Id])
                        {
                            depth[entry.Id] = candidate;
                            changed = true;
                        }
                    }
                }
                if (!changed)
                    break;
            }
            return depth;
        }

        private Dictionary<string, Rect> LayoutNodes(
            List<EncounterPoolEntry> entries,
            Dictionary<string, int> depth
        )
        {
            var perColumn = new Dictionary<int, int>();
            var positions = new Dictionary<string, Rect>();

            foreach (var entry in entries)
            {
                int col = depth[entry.Id];
                perColumn.TryGetValue(col, out int row);
                perColumn[col] = row + 1;

                var pos = new Vector2(20f + col * (NodeW + ColGap), 20f + row * (NodeH + RowGapY));
                if (_nodeDrag.TryGetValue(entry.Id, out var offset))
                    pos += offset;
                positions[entry.Id] = new Rect(pos.x, pos.y, NodeW, NodeH);
            }
            return positions;
        }

        private void DrawEdges(
            List<EncounterPoolEntry> entries,
            Dictionary<string, Rect> positions,
            Rect canvas
        )
        {
            foreach (var entry in entries)
            {
                DrawEdgeSet(entry, HardSources(entry), positions, canvas, EdgeHard, solid: true);
                DrawEdgeSet(entry, BoostSources(entry), positions, canvas, EdgeBoost, solid: false);
            }
        }

        private void DrawEdgeSet(
            EncounterPoolEntry target,
            IEnumerable<string> sourceIds,
            Dictionary<string, Rect> positions,
            Rect canvas,
            Color color,
            bool solid
        )
        {
            if (!positions.TryGetValue(target.Id, out var toRect))
                return;

            foreach (string sourceId in sourceIds)
            {
                if (!positions.TryGetValue(sourceId, out var fromRect))
                    continue;

                Vector3 from = new Vector3(canvas.x + fromRect.xMax, canvas.y + fromRect.center.y);
                Vector3 to = new Vector3(canvas.x + toRect.x, canvas.y + toRect.center.y);
                float tangent = Mathf.Max(40f, Mathf.Abs(to.x - from.x) * 0.5f);

                Handles.DrawBezier(
                    from,
                    to,
                    from + Vector3.right * tangent,
                    to + Vector3.left * tangent,
                    color,
                    // Texture then width — a null texture draws the default solid line, and
                    // the dotted variant is what distinguishes a boost edge from a hard gate.
                    solid ? null : EditorGUIUtility.whiteTexture,
                    solid ? 2.5f : 1.5f
                );

                // Arrowhead, so direction reads without tracing the curve back.
                Handles.color = color;
                Handles.DrawSolidDisc(to, Vector3.forward, 3.5f);
            }
        }

        private void DrawNodes(
            List<EncounterPoolEntry> entries,
            Dictionary<string, Rect> positions,
            Rect canvas
        )
        {
            foreach (var entry in entries)
            {
                var r = positions[entry.Id];
                var rect = new Rect(canvas.x + r.x, canvas.y + r.y, r.width, r.height);

                EditorGUI.DrawRect(rect, BarColor(entry.Encounter));
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), Color.black * 0.35f);

                string window =
                    entry.LastDay <= 0 ? $"d{entry.FirstDay}+"
                    : entry.FirstDay == entry.LastDay ? $"d{entry.FirstDay}"
                    : $"d{entry.FirstDay}-{entry.LastDay}";
                string weight = entry.Guaranteed ? "ALWAYS" : $"w{entry.ResolvedWeight:0.##}";

                GUI.Label(
                    new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, 18f),
                    HasVisitedEncounter.Label(entry.Encounter),
                    BarLabel
                );
                GUI.Label(
                    new Rect(rect.x + 6f, rect.y + 21f, rect.width - 12f, 18f),
                    $"{window}   {weight}",
                    BarLabel
                );

                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    EditorGUIUtility.PingObject(entry.Encounter);
            }
        }

        private void HandleNodeDrag(
            List<EncounterPoolEntry> entries,
            Dictionary<string, Rect> positions,
            Rect canvas
        )
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                foreach (var entry in entries)
                {
                    var r = positions[entry.Id];
                    if (
                        new Rect(canvas.x + r.x, canvas.y + r.y, r.width, r.height).Contains(
                            e.mousePosition
                        )
                    )
                    {
                        _draggingId = entry.Id;
                        break;
                    }
                }
            }
            else if (e.type == EventType.MouseDrag && _draggingId != null)
            {
                _nodeDrag.TryGetValue(_draggingId, out var offset);
                _nodeDrag[_draggingId] = offset + e.delta;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp)
            {
                _draggingId = null;
            }
        }

        /// <summary>Encounter ids this entry is hard-gated behind.</summary>
        private static IEnumerable<string> HardSources(EncounterPoolEntry entry) =>
            VisitedIds(entry.Requirements);

        /// <summary>Encounter ids that boost this entry's weight.</summary>
        private static IEnumerable<string> BoostSources(EncounterPoolEntry entry) =>
            VisitedIds(entry.BoostIf);

        private static IEnumerable<string> VisitedIds(IReadOnlyList<RunRequirement> reqs)
        {
            foreach (var req in reqs)
                if (req is HasVisitedEncounter v && v.Encounter != null)
                    yield return v.Encounter.ID;
        }

        private static readonly Color EdgeHard = new Color(0.85f, 0.8f, 0.4f);
        private static readonly Color EdgeBoost = new Color(0.45f, 0.75f, 0.95f);

        #endregion

        #region Styling
        // Battle and event encounters get different bars so the mix across the campaign
        // reads without checking each asset's type.
        private static Color BarColor(EncounterData encounter) =>
            encounter switch
            {
                BattleEncounterData => new Color(0.62f, 0.24f, 0.24f, 0.85f),
                EventEncounterData => new Color(0.24f, 0.44f, 0.62f, 0.85f),
                _ => new Color(0.4f, 0.4f, 0.4f, 0.85f),
            };

        private static readonly Color EmptyDay = new Color(0.5f, 0.15f, 0.15f, 0.35f);

        private static GUIStyle _barLabel;
        private static GUIStyle BarLabel =>
            _barLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
            };

        private static GUIStyle _errorLabel;
        private static GUIStyle ErrorLabel =>
            _errorLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.5f, 0.5f) },
            };

        #endregion
    }
}
