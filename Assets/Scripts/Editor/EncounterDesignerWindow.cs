using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Encounter Designer — several views over one <see cref="EncounterPoolData"/>.
    ///
    /// <para><b>Timeline</b> — a day-by-day Gantt. Each entry is a bar spanning the days it can
    /// appear on, so gaps and pile-ups are visible at a glance instead of inferred from a list of
    /// number pairs. Two things a table can't show: a day with nothing eligible (flagged red in
    /// the coverage strip — that day would hand the player an empty map), and what a seed
    /// actually generates (hit Roll and every day's real draw appears under its column, from the
    /// same call the campaign makes at runtime).</para>
    ///
    /// <para><b>Dependencies</b> — the unlock graph. Nodes are encounters, laid out left to right
    /// by how deep they sit in a chain; a solid arrow is a hard gate, a dotted one a nudge. Edges
    /// are derived, never drawn by hand: visited-gates, weight boosts, <c>GoToEncounter</c>
    /// chains, and flag writes matched to flag reads by name. The Timeline can't express this: a
    /// gated entry's bar shows when it *could* appear, not whether it will, which is why those
    /// rows are tagged <c>[dep]</c> there.</para>
    ///
    /// <para><b>Flags</b> — the same string-matching, listed instead of drawn, because a flag
    /// only half-exists: a misspelt name on either side is a gate that never opens and nothing
    /// anywhere complains. Every writer and reader of every flag in the project, with the
    /// unpaired ones called out.</para>
    ///
    /// The Authoring tab embeds Odin inspectors; the Table tab also supports bulk scheduling edits.
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
            Table,
            Dependencies,
            Flags,
            Simulate,
            Travel,
            Authoring,
        }

        private static readonly string[] TabNames =
        {
            "Timeline",
            "Table",
            "Dependencies",
            "Flags",
            "Simulate",
            "Travel",
            "Authoring",
        };

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
            var previousTab = _tab;
            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames, GUILayout.Height(22f));
            if (previousTab == Tab.Authoring && _tab != previousTab)
            {
                InvalidatePreviews();
            }

            EditorGUILayout.Space(4f);

            if (_tab == Tab.Authoring)
            {
                DrawAuthoringTab();
                return;
            }

            if (_pool.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This pool has no entries. Use Authoring to create events or battles.",
                    MessageType.Warning
                );
                return;
            }

            if (_tab == Tab.Table)
            {
                DrawTableTab();
                return;
            }
            if (_tab == Tab.Dependencies)
            {
                DrawDependencyTab();
                return;
            }
            if (_tab == Tab.Flags)
            {
                DrawFlagsTab();
                return;
            }
            if (_tab == Tab.Simulate)
            {
                DrawSimulateTab();
                return;
            }

            if (_tab == Tab.Travel)
            {
                DrawTravelTab();
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

        #region Odin authoring
        private UnityEngine.Object _authoringTarget;
        private UnityEditor.Editor _authoringEditor;
        private Vector2 _authoringScroll;

        private void OnEnable()
        {
            Undo.undoRedoPerformed += InvalidatePreviews;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= InvalidatePreviews;
            if (_authoringEditor != null)
            {
                DestroyImmediate(_authoringEditor);
            }
        }

        private void InvalidatePreviews()
        {
            _rolled = null;
            _sim = null;
            _flagUses = null;
            Repaint();
        }

        private void EditAsset(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            _authoringTarget = asset;
            _authoringScroll = Vector2.zero;
            _tab = Tab.Authoring;
            Repaint();
        }

        private void DrawAuthoringTab()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Pool", EditorStyles.toolbarButton))
            {
                EditAsset(_pool);
            }

            using (new EditorGUI.DisabledScope(_pool.Travel == null))
            {
                if (GUILayout.Button("Travel network", EditorStyles.toolbarButton))
                {
                    EditAsset(_pool.Travel);
                }
            }

            if (GUILayout.Button("New district", EditorStyles.toolbarButton))
            {
                var district = Crookedile.Utilities.AuthoringAssets.CreateBeside<DistrictData>(_pool, "New District");
                Undo.RegisterCreatedObjectUndo(district, "Create district");
                EditAsset(district);
            }

            if (GUILayout.Button("New ally", EditorStyles.toolbarButton))
            {
                var ally = Crookedile.Utilities.AuthoringAssets.CreateBeside<AllyData>(_pool, "New Ally");
                Undo.RegisterCreatedObjectUndo(ally, "Create ally");
                EditAsset(ally);
            }

            if (GUILayout.Button("Save assets", EditorStyles.toolbarButton))
            {
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();
            if (_authoringTarget == null)
            {
                _authoringTarget = _pool;
            }

            var selected = EditorGUILayout.ObjectField("Editing asset", _authoringTarget, typeof(ScriptableObject), false);
            if (selected != null && selected != _authoringTarget)
            {
                EditAsset(selected);
            }

            EditorGUILayout.HelpBox("Edit the original asset here with Odin. Expand district and network references to edit them inline. "
                + "Choose an encounter from Table, Timeline, or Travel to open it here.", MessageType.Info);
            UnityEditor.Editor.CreateCachedEditor(_authoringTarget, typeof(OdinEditor), ref _authoringEditor);
            _authoringScroll = EditorGUILayout.BeginScrollView(_authoringScroll);
            var previousSelection = Selection.activeObject;
            EditorGUI.BeginChangeCheck();
            _authoringEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                InvalidatePreviews();
            }

            EditorGUILayout.EndScrollView();
            if (Selection.activeObject != previousSelection && Selection.activeObject is EncounterData encounter)
            {
                EditAsset(encounter);
            }
        }
        #endregion

        #region Toolbar
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var previousPool = _pool;
            _pool = (EncounterPoolData)
                EditorGUILayout.ObjectField(
                    _pool,
                    typeof(EncounterPoolData),
                    false,
                    GUILayout.Width(220f)
                );
            if (_pool != previousPool)
            {
                _authoringTarget = _pool;
                InvalidatePreviews();
            }

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

            GUILayout.Space(12f);
            if (GUILayout.Button("Import CSV", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                EncounterCsvImporter.ImportInto(_pool);

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

                // Click the label to edit the asset — the usual reason you're looking at this
                // row is to go edit the thing it names.
                if (GUI.Button(labelRect, entry.Encounter.name, EditorStyles.label))
                    EditAsset(entry.Encounter);

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

            // Everything below is keyed by encounter id, so repeats need collapsing. One
            // encounter in several rows is deliberate; two assets sharing an id is a real fault.
            var shared = entries.GroupBy(e => e.Id).Where(g => g.Count() > 1).ToList();

            var collisions = shared
                .Where(g => g.Select(e => e.Encounter).Distinct().Count() > 1)
                .ToList();
            if (collisions.Count > 0)
                EditorGUILayout.HelpBox(
                    "Different encounters sharing an ID — one of them is unreachable by lookup. "
                        + "Re-save each asset to reassign:\n"
                        + string.Join(
                            "\n",
                            collisions.Select(g =>
                                $"  {g.Key}: {string.Join(", ", g.Select(e => e.Encounter.name).Distinct())}"
                            )
                        ),
                    MessageType.Error
                );

            var repeated = shared.Except(collisions).ToList();
            if (repeated.Count > 0)
                EditorGUILayout.HelpBox(
                    "Listed in more than one row — drawn at most once per day, and Once Per Run "
                        + "applies to the encounter, not the row, so visiting it retires every "
                        + "row at once. The graph shows one node:\n"
                        + string.Join(
                            "\n",
                            repeated.Select(g => $"  {g.First().Encounter.name} × {g.Count()} rows")
                        ),
                    MessageType.Info
                );

            if (shared.Count > 0)
                entries = entries.GroupBy(e => e.Id).Select(g => g.First()).ToList();

            // Depth = longest hard-requirement chain leading here. Gives left-to-right reading
            // order for free: day-one content on the left, things it unlocks to the right.
            var edges = BuildEdges(entries);
            var depth = ComputeDepths(entries, edges);
            var positions = LayoutNodes(entries, depth);

            int columns = depth.Values.DefaultIfEmpty(0).Max() + 1;
            float canvasW = columns * (NodeW + ColGap) + 40f;
            float canvasH = entries.Count * (NodeH + RowGapY) + 60f;

            _graphScroll = EditorGUILayout.BeginScrollView(_graphScroll);
            Rect canvas = GUILayoutUtility.GetRect(canvasW, canvasH);

            DrawEdges(edges, positions, canvas);
            DrawNodes(entries, positions, canvas);
            HandleNodeDrag(entries, positions, canvas);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField(
                "Yellow = visited gate    Blue = weight boost    Green = leads-to chain    "
                    + "Pink = flag, named on the curve",
                EditorStyles.miniLabel
            );
            EditorGUILayout.LabelField(
                "Solid = hard gate    Dotted = a nudge, a negated check, or a branch inside one "
                    + "option    Drag nodes to untangle (not saved)",
                EditorStyles.miniLabel
            );
        }

        /// <summary>
        /// Longest chain of hard edges ending at each entry. Iterative relaxation rather than
        /// recursion so a cyclic authoring mistake settles instead of blowing the stack.
        /// </summary>
        private Dictionary<string, int> ComputeDepths(
            List<EncounterPoolEntry> entries,
            List<Edge> edges
        )
        {
            var depth = entries.ToDictionary(e => e.Id, _ => 0);
            var hard = edges.Where(e => e.Solid).ToList();

            for (int pass = 0; pass < entries.Count; pass++)
            {
                bool changed = false;
                foreach (var edge in hard)
                {
                    if (!depth.ContainsKey(edge.From) || !depth.ContainsKey(edge.To))
                        continue;
                    int candidate = depth[edge.From] + 1;
                    if (candidate > depth[edge.To])
                    {
                        depth[edge.To] = candidate;
                        changed = true;
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

        private void DrawEdges(List<Edge> edges, Dictionary<string, Rect> positions, Rect canvas)
        {
            foreach (var edge in edges)
            {
                if (
                    !positions.TryGetValue(edge.From, out var fromRect)
                    || !positions.TryGetValue(edge.To, out var toRect)
                )
                    continue;

                Vector3 from = new Vector3(canvas.x + fromRect.xMax, canvas.y + fromRect.center.y);
                Vector3 to = new Vector3(canvas.x + toRect.x, canvas.y + toRect.center.y);
                float tangent = Mathf.Max(40f, Mathf.Abs(to.x - from.x) * 0.5f);

                Handles.DrawBezier(
                    from,
                    to,
                    from + Vector3.right * tangent,
                    to + Vector3.left * tangent,
                    edge.Color,
                    // Texture then width — a null texture draws the default solid line, and
                    // the dotted variant is what distinguishes a soft nudge from a hard gate.
                    edge.Solid ? null : EditorGUIUtility.whiteTexture,
                    edge.Solid ? 2.5f : 1.5f
                );

                // Arrowhead, so direction reads without tracing the curve back.
                Handles.color = edge.Color;
                Handles.DrawSolidDisc(to, Vector3.forward, 3.5f);

                if (string.IsNullOrEmpty(edge.Label))
                    continue;

                // Flag names are the whole point of the edge — without the name on the curve
                // you can see that two encounters are linked but not by what.
                Vector3 mid = (from + to) * 0.5f + Vector3.up * -8f;
                var labelRect = new Rect(mid.x - 55f, mid.y - 8f, 110f, 16f);
                EditorGUI.DrawRect(labelRect, new Color(0.15f, 0.15f, 0.15f, 0.85f));
                GUI.Label(labelRect, edge.Label, EdgeLabelStyle(edge.Color));
            }
        }

        private static GUIStyle _edgeLabel;

        private static GUIStyle EdgeLabelStyle(Color color)
        {
            _edgeLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };
            _edgeLabel.normal.textColor = color;
            return _edgeLabel;
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
                    EditAsset(entry.Encounter);
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

        /// <summary>One arrow: <c>From</c> has to happen for <c>To</c> to be reachable, or likelier.</summary>
        private readonly struct Edge
        {
            public readonly string From;
            public readonly string To;
            public readonly Color Color;

            /// <summary>Solid = hard gate. Dotted = a nudge, or a branch inside one option.</summary>
            public readonly bool Solid;

            /// <summary>Drawn at the curve's midpoint. Empty for edges the legend already explains.</summary>
            public readonly string Label;

            public Edge(string from, string to, Color color, bool solid, string label = "")
            {
                From = from;
                To = to;
                Color = color;
                Solid = solid;
                Label = label;
            }
        }

        /// <summary>
        /// Every ordering constraint the pool encodes, in one pass: visited-gates, weight
        /// boosts, <c>GoToEncounter</c> chains, and — the invisible ones — flag writes paired
        /// to flag reads by name. A flag edge is a guess by string match, which is exactly what
        /// the runtime does too; the Flags tab is where the unmatched names get named.
        /// </summary>
        private List<Edge> BuildEdges(List<EncounterPoolEntry> entries)
        {
            var edges = new List<Edge>();
            var known = entries.Select(e => e.Id).ToHashSet();

            // Flag name → ids of pool entries whose options set it. Cleared flags are writes
            // too, but a clear can't unlock anything, so they don't produce edges.
            var writers = new Dictionary<string, List<string>>();
            foreach (var entry in entries)
                foreach (var outcome in OptionOutcomes(entry))
                    if (outcome is SetFlagOutcome s && !s.Clears && !string.IsNullOrWhiteSpace(s.Flag))
                    {
                        if (!writers.TryGetValue(s.Flag.Trim(), out var list))
                            writers[s.Flag.Trim()] = list = new List<string>();
                        list.Add(entry.Id);
                    }

            foreach (var entry in entries)
            {
                AddRequirementEdges(entry.Requirements, entry.Id, solid: true, "");
                AddRequirementEdges(entry.BoostIf, entry.Id, solid: false, "");

                if (!(entry.Encounter is EventEncounterData ev))
                    continue;

                foreach (var option in ev.Options)
                {
                    if (option == null)
                        continue;

                    // An option's own requirements gate that branch, not the encounter's
                    // appearance — dotted, so it never reads as a hard unlock.
                    AddRequirementEdges(option.Requirements, entry.Id, solid: false, "opt");

                    foreach (var outcome in option.Outcomes)
                        if (
                            outcome is GoToEncounterOutcome go
                            && go.Target != null
                            && known.Contains(go.Target.ID)
                        )
                            edges.Add(new Edge(entry.Id, go.Target.ID, EdgeChain, solid: true));
                }
            }

            return edges;

            void AddRequirementEdges(
                IReadOnlyList<RunRequirement> reqs,
                string targetId,
                bool solid,
                string prefix
            )
            {
                foreach (var req in reqs)
                {
                    if (req is HasVisitedEncounter v && v.Encounter != null)
                    {
                        if (known.Contains(v.Encounter.ID))
                            edges.Add(
                                new Edge(
                                    v.Encounter.ID,
                                    targetId,
                                    solid ? EdgeHard : EdgeBoost,
                                    solid && !req.Negated,
                                    Join(prefix, req.Negated ? "not visited" : "")
                                )
                            );
                    }
                    else if (req is HasFlag f && !string.IsNullOrWhiteSpace(f.Flag))
                    {
                        if (!writers.TryGetValue(f.Flag.Trim(), out var sources))
                            continue;
                        foreach (string sourceId in sources)
                        {
                            if (sourceId == targetId)
                                continue;
                            edges.Add(
                                new Edge(
                                    sourceId,
                                    targetId,
                                    EdgeFlag,
                                    solid && !req.Negated,
                                    Join(prefix, (req.Negated ? "not " : "") + f.Flag.Trim())
                                )
                            );
                        }
                    }
                }
            }

            static string Join(string prefix, string label) =>
                string.IsNullOrEmpty(prefix) ? label
                : string.IsNullOrEmpty(label) ? prefix
                : $"{prefix}: {label}";
        }

        private static IEnumerable<RunOutcome> OptionOutcomes(EncounterPoolEntry entry)
        {
            if (!(entry.Encounter is EventEncounterData ev))
                yield break;
            foreach (var option in ev.Options)
            {
                if (option == null)
                    continue;
                foreach (var outcome in option.Outcomes)
                    yield return outcome;
            }
        }

        private static readonly Color EdgeHard = new Color(0.85f, 0.8f, 0.4f);
        private static readonly Color EdgeBoost = new Color(0.45f, 0.75f, 0.95f);
        private static readonly Color EdgeChain = new Color(0.45f, 0.85f, 0.5f);
        private static readonly Color EdgeFlag = new Color(0.9f, 0.5f, 0.9f);

        #endregion

        #region Table
        // Column widths, left to right. One array so the header and the rows can't drift apart —
        // they did, twice, while this was two sets of literals.
        private const float ColName = 168f;
        private const float ColType = 52f;
        private const float ColDay = 40f;
        private const float ColWeight = 52f;
        private const float ColResolved = 44f;
        private const float ColFlag = 30f;
        private const float ColDelete = 22f;
        private const float CellGap = 4f;

        private Vector2 _tableScroll;

        /// <summary>Live view of <see cref="_pool"/>. Rebuilt when the assigned pool changes.</summary>
        private SerializedObject _poolSo;
        private EncounterPoolData _soTarget;

        /// <summary>
        /// The pool as an editable grid — the numbers a designer retunes in bulk (day window,
        /// weight, guaranteed) on one line each, instead of one expanded inspector row at a time.
        ///
        /// Edits go through <see cref="SerializedObject"/> rather than the entry's own fields,
        /// which is what buys Undo, the dirty flag and multi-object semantics for free. The
        /// requirement lists deliberately aren't editable here: they're polymorphic
        /// <c>[SerializeReference]</c> lists that need Odin's type picker, so this shows the
        /// count and opens the asset in the embedded Odin Authoring tab.
        /// </summary>
        private void DrawTableTab()
        {
            if (_poolSo == null || _soTarget != _pool)
            {
                _poolSo = new SerializedObject(_pool);
                _soTarget = _pool;
            }
            _poolSo.Update();

            var entries = _poolSo.FindProperty("_entries");
            if (entries == null)
            {
                EditorGUILayout.HelpBox(
                    "Couldn't find the pool's _entries field — the field was renamed.",
                    MessageType.Error
                );
                return;
            }

            DrawTableHeader();

            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll);
            int deleteAt = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (DrawTableRow(entries.GetArrayElementAtIndex(i), i))
                    deleteAt = i;
            EditorGUILayout.EndScrollView();

            // Deferred: mutating the array mid-loop invalidates every property handle after it.
            if (deleteAt >= 0)
                entries.DeleteArrayElementAtIndex(deleteAt);

            if (_poolSo.ApplyModifiedProperties())
            {
                InvalidatePreviews();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Last 0 = no end day    Weight -1 = inherit the encounter's DropWeight (grey "
                    + "number is what it resolves to)    G = guaranteed    1× = once per run",
                EditorStyles.miniLabel
            );
        }

        private void DrawTableHeader()
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(18f));
            EditorGUI.DrawRect(r, new Color(0.16f, 0.16f, 0.16f, 0.6f));

            float x = r.x + 5f;
            Header(ref x, ColName, "Encounter");
            Header(ref x, ColType, "Type");
            Header(ref x, ColDay, "First");
            Header(ref x, ColDay, "Last");
            Header(ref x, ColWeight, "Weight");
            Header(ref x, ColResolved, "= w");
            Header(ref x, ColFlag, "G");
            Header(ref x, ColFlag, "1×");
            Header(ref x, 120f, "Dependencies");

            void Header(ref float cursor, float width, string label)
            {
                GUI.Label(new Rect(cursor, r.y, width, 16f), label, EditorStyles.miniBoldLabel);
                cursor += width + CellGap;
            }
        }

        /// <summary>Draws one row. Returns true when its delete button was pressed.</summary>
        private bool DrawTableRow(SerializedProperty entry, int index)
        {
            var encounter = entry.FindPropertyRelative("_encounter");
            var firstDay = entry.FindPropertyRelative("_firstDay");
            var lastDay = entry.FindPropertyRelative("_lastDay");
            var weight = entry.FindPropertyRelative("_weight");
            var guaranteed = entry.FindPropertyRelative("_guaranteed");
            var oncePerRun = entry.FindPropertyRelative("_oncePerRun");

            Rect row = EditorGUILayout.GetControlRect(GUILayout.Height(22f));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(
                    row,
                    index % 2 == 1
                        ? new Color(0.25f, 0.25f, 0.25f, 0.3f)
                        : new Color(0.2f, 0.2f, 0.2f, 0.2f)
                );

            var asset = encounter.objectReferenceValue as EncounterData;
            float x = row.x + 5f;
            float y = row.y + 2f;

            // Name doubles as the way in: the fields this table can't edit live on that asset.
            var nameRect = new Rect(x, y, ColName, 18f);
            if (asset == null)
            {
                EditorGUI.PropertyField(nameRect, encounter, GUIContent.none);
            }
            else if (GUI.Button(nameRect, HasVisitedEncounter.Label(asset), BarLabel))
            {
                EditAsset(asset);
            }
            x += ColName + CellGap;

            DrawBadge(
                new Rect(x, y, ColType, 18f),
                asset is BattleEncounterData ? "Battle"
                    : asset is EventEncounterData ? "Event"
                    : "—",
                BarColor(asset)
            );
            x += ColType + CellGap;

            EditorGUI.PropertyField(new Rect(x, y, ColDay, 18f), firstDay, GUIContent.none);
            x += ColDay + CellGap;
            EditorGUI.PropertyField(new Rect(x, y, ColDay, 18f), lastDay, GUIContent.none);
            x += ColDay + CellGap;

            EditorGUI.PropertyField(new Rect(x, y, ColWeight, 18f), weight, GUIContent.none);
            x += ColWeight + CellGap;

            // What the row actually draws at, since -1 means "ask the encounter". Without this
            // the weight column reads as -1 and tells you nothing about the odds.
            float resolved =
                asset == null ? 0f
                : weight.floatValue < 0f ? asset.DropWeight
                : weight.floatValue;
            GUI.Label(
                new Rect(x, y, ColResolved, 18f),
                resolved <= 0f ? "—" : $"{resolved:0.##}",
                EditorStyles.miniLabel
            );
            x += ColResolved + CellGap;

            EditorGUI.PropertyField(new Rect(x, y, ColFlag, 18f), guaranteed, GUIContent.none);
            x += ColFlag + CellGap;
            EditorGUI.PropertyField(new Rect(x, y, ColFlag, 18f), oncePerRun, GUIContent.none);
            x += ColFlag + CellGap;

            // Read-only on purpose — see the tab summary.
            var poolEntry = index < _pool.Entries.Count ? _pool.Entries[index] : null;
            string deps =
                poolEntry == null || !poolEntry.HasDependencies
                    ? ""
                    : poolEntry.DescribeDependencies();
            GUI.Label(
                new Rect(x, y, row.xMax - x - ColDelete - CellGap - 5f, 18f),
                deps,
                EditorStyles.miniLabel
            );

            var deleteRect = new Rect(row.xMax - ColDelete - 5f, y, ColDelete, 18f);
            return GUI.Button(deleteRect, "−", EditorStyles.miniButton);
        }

        private static void DrawBadge(Rect rect, string label, Color color)
        {
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, color);
            GUI.Label(rect, label, BadgeLabel);
        }

        private static GUIStyle _badgeLabel;
        private static GUIStyle BadgeLabel =>
            _badgeLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
            };

        #endregion

        #region Flags
        // The index walks every encounter asset, so it is built once and refreshed by hand
        // rather than on each repaint. Rescan after editing an option's outcomes.
        private List<FlagUse> _flagUses;
        private Vector2 _flagScroll;

        /// <summary>
        /// A flag has no asset of its own — it exists only as matching strings in two lists,
        /// and a mismatch silently makes content unreachable for the whole run. This pairs the
        /// writers and readers by name and calls out the halves that found no partner.
        /// </summary>
        private void DrawFlagsTab()
        {
            _flagUses ??= FlagIndex.Build(_pool);

            EditorGUILayout.BeginHorizontal();
            var groups = _flagUses.GroupBy(u => u.Flag).OrderBy(g => g.Key).ToList();
            EditorGUILayout.LabelField(
                $"{groups.Count} flag{(groups.Count == 1 ? "" : "s")} across every encounter asset",
                EditorStyles.boldLabel
            );
            if (GUILayout.Button("Rescan", EditorStyles.miniButton, GUILayout.Width(70f)))
                _flagUses = FlagIndex.Build(_pool);
            EditorGUILayout.EndHorizontal();

            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No flags authored yet. A SetFlagOutcome on an event option writes one; a "
                        + "HasFlag requirement on a later option or pool row reads it.",
                    MessageType.Info
                );
                return;
            }

            var setNames = groups
                .Where(g => g.Any(u => u.Kind == FlagUseKind.Set))
                .Select(g => g.Key)
                .ToList();

            _flagScroll = EditorGUILayout.BeginScrollView(_flagScroll);
            foreach (var group in groups)
            {
                bool isSet = group.Any(u => u.Kind == FlagUseKind.Set);
                bool isRead = group.Any(u => !u.IsWrite);

                EditorGUILayout.Space(6f);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel, GUILayout.Width(220f));

                if (group.Key == FlagIndex.Blank)
                    EditorGUILayout.LabelField("blank name — does nothing at runtime", ErrorLabel);
                else if (!isSet)
                {
                    string near = FlagIndex.ClosestMatch(group.Key, setNames);
                    EditorGUILayout.LabelField(
                        near == null
                            ? "read but never set — this gate can never open"
                            : $"read but never set — did you mean \"{near}\"?",
                        ErrorLabel
                    );
                }
                else if (!isRead)
                    EditorGUILayout.LabelField("set but never read — nothing gates on it yet");
                EditorGUILayout.EndHorizontal();

                foreach (var use in group.OrderBy(u => u.IsWrite ? 0 : 1))
                {
                    string verb = use.Kind switch
                    {
                        FlagUseKind.Set => "SET",
                        FlagUseKind.Clear => "CLEAR",
                        FlagUseKind.ReadNot => "READ NOT",
                        _ => "READ",
                    };
                    string owner = use.Owner == null ? "(missing)" : use.Owner.name;
                    if (
                        GUILayout.Button(
                            $"    {verb,-9}{owner} — {use.Where}",
                            EditorStyles.miniLabel
                        )
                    )
                        EditAsset(use.Owner);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Simulate
        // A single Roll answers "what does seed 12345 give me". These answer the questions a
        // week of hand-playing can't: is any day starved, is any encounter never seen, and how
        // much of the pool one player actually gets through.
        private int _runs = 500;
        private int _hoursPerDay = 3;
        private SimResult _sim;

        private sealed class SimResult
        {
            public int Runs;
            public int PerDay;
            public int Hours;
            public int PoolSize;
            public float[] OfferedByDay; // mean encounters offered, day-1 indexed
            public float[] AffordableByDay; // mean of those the hour budget allows
            public float[] ThinShareByDay; // share of runs offering fewer than PerDay
            public readonly Dictionary<string, int> RunsSeenIn = new Dictionary<string, int>();
            public float MeanUniquePerRun;
            public float MeanOverlap; // Jaccard between consecutive seeds
        }

        private void DrawSimulateTab()
        {
            DrawPreviewAllies();
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Runs", EditorStyles.miniLabel, GUILayout.Width(34f));
            _runs = Mathf.Clamp(
                EditorGUILayout.IntField(_runs, EditorStyles.toolbarTextField, GUILayout.Width(56f)),
                1,
                20000
            );
            GUILayout.Label("Hours/day", EditorStyles.miniLabel, GUILayout.Width(62f));
            _hoursPerDay = Mathf.Max(
                0,
                EditorGUILayout.IntField(
                    _hoursPerDay,
                    EditorStyles.toolbarTextField,
                    GUILayout.Width(36f)
                )
            );
            if (GUILayout.Button("Simulate", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                _sim = Simulate();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Draws are made with no RunState, so hard requirements pass and weight boosts "
                    + "never fire — gated content shows up here as if it were always available. "
                    + "Affordability visits the earliest-finishing reachable event next, including travel and waiting. "
                    + "It does not apply event outcomes or the mandatory boss fallback.",
                MessageType.Info
            );

            if (_sim == null)
            {
                EditorGUILayout.LabelField(
                    "Press Simulate to run the real draw across many seeds.",
                    EditorStyles.miniLabel
                );
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSimPerDay();
            EditorGUILayout.Space(8f);
            DrawSimCoverage();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Plays <see cref="_runs"/> whole campaigns through the real
        /// <see cref="EncounterPoolData.DrawForDay"/>, carrying once-per-run exclusions forward
        /// exactly as <see cref="Roll"/> does for a single seed.
        /// </summary>
        private SimResult Simulate()
        {
            var entries = _pool.Entries.Where(e => e?.Encounter != null).ToList();
            int days = _pool.Days;

            var result = new SimResult
            {
                Runs = _runs,
                PerDay = _perDay,
                Hours = _hoursPerDay,
                PoolSize = entries.Select(e => e.Id).Distinct().Count(),
                OfferedByDay = new float[days],
                AffordableByDay = new float[days],
                ThinShareByDay = new float[days],
            };

            long uniqueTotal = 0;
            double overlapTotal = 0;
            HashSet<string> previousSeen = null;

            for (int run = 0; run < _runs; run++)
            {
                var consumed = new HashSet<string>();
                var seen = new HashSet<string>();
                int seed = unchecked(_seed + run * 7919); // stride by a prime to decorrelate runs

                for (int day = 1; day <= days; day++)
                {
                    var picks = _pool.DrawForDay(day, _perDay, seed, consumed);

                    result.OfferedByDay[day - 1] += picks.Count;
                    result.AffordableByDay[day - 1] += Affordable(picks, _hoursPerDay, consumed);
                    if (picks.Count < _perDay)
                        result.ThinShareByDay[day - 1] += 1f;

                    foreach (var pick in picks)
                    {
                        if (pick == null)
                            continue;
                        seen.Add(pick.ID);
                    }
                }

                uniqueTotal += seen.Count;
                foreach (string id in seen)
                    result.RunsSeenIn[id] = result.RunsSeenIn.TryGetValue(id, out int n) ? n + 1 : 1;

                if (previousSeen != null)
                {
                    int union = previousSeen.Union(seen).Count();
                    overlapTotal += union == 0 ? 0 : previousSeen.Intersect(seen).Count() / (double)union;
                }
                previousSeen = seen;
            }

            for (int d = 0; d < days; d++)
            {
                result.OfferedByDay[d] /= _runs;
                result.AffordableByDay[d] /= _runs;
                result.ThinShareByDay[d] /= _runs;
            }
            result.MeanUniquePerRun = uniqueTotal / (float)_runs;
            result.MeanOverlap = _runs < 2 ? 0f : (float)(overlapTotal / (_runs - 1));
            return result;
        }

        /// <summary>Greedily visits the earliest-finishing reachable encounter using the runtime travel calculation.</summary>
        private int Affordable(List<EncounterData> picks, int hours, HashSet<string> consumed)
        {
            var remaining = new List<EncounterData>(picks);
            var network = _pool.Travel;
            var district = network != null ? network.Headquarters : null;
            int clock = network != null ? network.DayStartMinute : CampaignTravelData.DEFAULT_START_MINUTE;
            int end = Mathf.Min(clock + hours * 60, CampaignTravelData.MINUTES_PER_DAY);
            int taken = 0;
            while (remaining.Count > 0)
            {
                EncounterData next = null;
                int finish = int.MaxValue;
                foreach (var pick in remaining)
                {
                    var plan = CampaignVisitPlan.Calculate(pick, network, district, clock, end - clock, _previewAllies);
                    if (plan.CanEnter && plan.FinishMinute < finish)
                    {
                        next = pick;
                        finish = plan.FinishMinute;
                    }
                }

                if (next == null)
                {
                    break;
                }

                clock = finish;
                if (next.District != null)
                {
                    district = next.District;
                }

                consumed.Add(next.ID);
                remaining.Remove(next);
                taken++;
            }

            return taken;
        }

        #region Travel preview
        private DistrictData _previewDistrict;
        private int _previewMinute = CampaignTravelData.DEFAULT_START_MINUTE;
        private int _previewBudget = 180;
        private readonly List<AllyData> _previewAllies = new List<AllyData>();

        private void DrawPreviewAllies()
        {
            EditorGUILayout.LabelField("Recruited allies for Travel and Simulate", EditorStyles.boldLabel);
            for (int i = 0; i < _previewAllies.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                _previewAllies[i] = (AllyData)EditorGUILayout.ObjectField(_previewAllies[i], typeof(AllyData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    InvalidatePreviews();
                }

                using (new EditorGUI.DisabledScope(_previewAllies[i] == null))
                {
                    if (GUILayout.Button("Edit ally", GUILayout.Width(65f)))
                    {
                        EditAsset(_previewAllies[i]);
                    }
                }

                bool remove = GUILayout.Button("Remove", GUILayout.Width(65f));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    _previewAllies.RemoveAt(i);
                    InvalidatePreviews();
                    break;
                }
            }

            if (GUILayout.Button("Add preview ally"))
            {
                _previewAllies.Add(null);
            }
        }

        private void DrawTravelTab()
        {
            DrawPreviewAllies();
            var network = _pool.Travel;
            if (GUILayout.Button(network != null ? $"Edit travel network: {network.name}" : "Author travel network on pool"))
            {
                EditAsset(network != null ? (UnityEngine.Object)network : _pool);
            }
            if (network == null)
            {
                EditorGUILayout.HelpBox("Create a Campaign/Travel Network asset and assign it on the pool. "
                    + "Use Authoring to create districts and edit roads inline with Odin.",
                    MessageType.Info);
            }
            else if (network.Headquarters == null)
            {
                EditorGUILayout.HelpBox("Assign the network's Headquarters district so the first trip has a starting point.", MessageType.Error);
            }

            _previewDistrict = (DistrictData)EditorGUILayout.ObjectField("From (blank = HQ)",
                _previewDistrict, typeof(DistrictData), false);
            _previewMinute = EditorGUILayout.IntSlider("Departure minute", _previewMinute, 0, 1439);
            _previewBudget = EditorGUILayout.IntSlider("Minutes remaining", _previewBudget, 0, 1440);
            var from = _previewDistrict != null ? _previewDistrict : network != null ? network.Headquarters : null;
            EditorGUILayout.LabelField($"Depart {CampaignTravelData.FormatTime(_previewMinute)} from "
                + $"{(from != null ? from.DisplayName : "Local")}");
            EditorGUILayout.HelpBox("Preview uses the runtime route and time-window calculation. "
                + "Entry is allowed at opening and blocked at closing; an event may finish after closing. "
                + "Click an encounter or the network button to edit it here in Authoring.", MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var entry in _pool.Entries)
            {
                var encounter = entry?.Encounter;
                if (encounter == null)
                {
                    continue;
                }

                if (GUILayout.Button($"Edit {HasVisitedEncounter.Label(encounter)}", EditorStyles.miniButton))
                {
                    EditAsset(encounter);
                }
                var plan = CampaignVisitPlan.Calculate(encounter, network, from, _previewMinute, _previewBudget, _previewAllies);
                string destination = encounter.District != null ? encounter.District.DisplayName : "Local";
                EditorGUILayout.LabelField($"{destination} | Entry {CampaignTravelData.FormatTime(encounter.OpeningMinute)} - "
                    + $"{CampaignTravelData.FormatTime(encounter.ClosingMinute)} | Base duration {encounter.DurationMinutes}m");
                if (plan.CanEnter)
                {
                    EditorGUILayout.LabelField($"Travel {plan.TravelMinutes}m ({plan.TrafficMultiplier:0.##}x traffic), "
                        + $"wait {plan.WaitMinutes}m | Arrive {CampaignTravelData.FormatTime(plan.ArrivalMinute)}, "
                        + $"finish {CampaignTravelData.FormatTime(plan.FinishMinute)}");
                    EditorGUILayout.LabelField($"Encounter {plan.EncounterMinutes}m | Allies save "
                        + $"{plan.TravelMinutesSaved}m travel + {plan.EncounterMinutesSaved}m encounter time; waiting is not discounted.");
                }
                else
                {
                    EditorGUILayout.HelpBox(plan.BlockedReason, MessageType.Warning);
                }

                EditorGUILayout.Space(6f);
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion

        private void DrawSimPerDay()
        {
            EditorGUILayout.LabelField(
                $"{_sim.Runs} runs — offered vs. what {_sim.Hours} hours buys",
                EditorStyles.boldLabel
            );

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(70f));
            for (int day = 1; day <= _pool.Days; day++)
                GUILayout.Label($"D{day}", EditorStyles.miniBoldLabel, GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            DrawSimRow("offered", _sim.OfferedByDay, v => v < _sim.PerDay - 0.01f);
            DrawSimRow("affordable", _sim.AffordableByDay, v => v < 2f);
            DrawSimRow("thin runs", _sim.ThinShareByDay, v => v > 0.05f, percent: true);

            float squeeze = _sim.OfferedByDay.Sum() - _sim.AffordableByDay.Sum();
            EditorGUILayout.LabelField(
                squeeze < 0.5f
                    ? "No time pressure: the hour budget covers everything offered, so the player never chooses."
                    : $"Time pressure: {squeeze:0.#} encounters per run are offered but unaffordable — that is the choice.",
                EditorStyles.miniLabel
            );
        }

        private void DrawSimRow(
            string label,
            float[] values,
            System.Func<float, bool> warn,
            bool percent = false
        )
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(70f));
            foreach (float v in values)
            {
                var style = new GUIStyle(EditorStyles.miniLabel);
                if (warn(v))
                    style.normal.textColor = new Color(1f, 0.55f, 0.35f);
                GUILayout.Label(
                    percent ? $"{v * 100f:0}%" : $"{v:0.0}",
                    style,
                    GUILayout.Width(52f)
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSimCoverage()
        {
            float burn = _sim.PoolSize == 0 ? 0f : _sim.MeanUniquePerRun / _sim.PoolSize;
            EditorGUILayout.LabelField(
                $"A run sees {_sim.MeanUniquePerRun:0.0} of {_sim.PoolSize} encounters ({burn * 100f:0}% of the pool). "
                    + $"Two runs share {_sim.MeanOverlap * 100f:0}% of their content.",
                EditorStyles.boldLabel
            );

            var byId = _pool
                .Entries.Where(e => e?.Encounter != null)
                .GroupBy(e => e.Id)
                .ToDictionary(g => g.Key, g => g.First().Encounter);

            var never = byId.Where(kv => !_sim.RunsSeenIn.ContainsKey(kv.Key)).ToList();
            if (never.Count > 0)
                EditorGUILayout.HelpBox(
                    "Never drawn in any run — unreachable content:\n  "
                        + string.Join(", ", never.Select(kv => kv.Value.name)),
                    MessageType.Error
                );

            var always = _sim
                .RunsSeenIn.Where(kv => kv.Value >= _sim.Runs * 0.8f && byId.ContainsKey(kv.Key))
                .OrderByDescending(kv => kv.Value)
                .ToList();
            if (always.Count > 0)
                EditorGUILayout.HelpBox(
                    "In 80%+ of runs — these define the campaign's texture, so they had better be good:\n  "
                        + string.Join(
                            ", ",
                            always.Select(kv =>
                                $"{byId[kv.Key].name} {kv.Value * 100f / _sim.Runs:0}%"
                            )
                        ),
                    MessageType.Info
                );
        }

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
