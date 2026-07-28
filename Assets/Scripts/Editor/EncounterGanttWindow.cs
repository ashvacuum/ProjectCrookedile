using System.Collections.Generic;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Encounter Gantt — a day-by-day timeline of an <see cref="EncounterPoolData"/>.
    /// Each entry is a bar spanning the days it can appear on, so gaps and pile-ups are
    /// visible at a glance instead of inferred from a list of number pairs.
    ///
    /// Two things a table can't show and this can:
    ///   • A day with nothing eligible — flagged red in the coverage strip. That day would
    ///     hand the player an empty map.
    ///   • What a seed actually generates. Enter a seed, hit Roll, and every day's real draw
    ///     appears under its column — the same call the campaign will make at runtime.
    ///
    /// Editing lives in the pool asset's own inspector (Odin TableList); this window is a
    /// read-only view plus the seed simulator.
    ///
    /// Menu: Crookedile → Encounter Gantt.
    /// </summary>
    public class EncounterGanttWindow : EditorWindow
    {
        [MenuItem("Crookedile/Encounter Gantt")]
        public static void ShowWindow()
        {
            var win = GetWindow<EncounterGanttWindow>("Encounter Gantt");
            win.minSize = new Vector2(720, 400);
            win.Show();
        }

        #region Layout constants
        private const float LabelWidth = 190f;
        private const float RowHeight = 22f;
        private const float RowGap = 3f;
        private const float MinDayWidth = 54f;

        #endregion

        private EncounterPoolData _pool;
        private int _seed = 12345;
        private int _perDay = 3;
        private Vector2 _scroll;

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
                "w2 = weight overridden on this row    w2* = inherited from the encounter's DropWeight",
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
            _seed = EditorGUILayout.IntField(_seed, EditorStyles.toolbarTextField, GUILayout.Width(80f));

            if (GUILayout.Button("Random", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                _seed = Random.Range(1, int.MaxValue);
                Roll();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Per day", EditorStyles.miniLabel, GUILayout.Width(48f));
            _perDay = Mathf.Max(
                1,
                EditorGUILayout.IntField(_perDay, EditorStyles.toolbarTextField, GUILayout.Width(36f))
            );

            if (GUILayout.Button("Roll", EditorStyles.toolbarButton, GUILayout.Width(48f)))
                Roll();
            if (_rolled != null && GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(48f)))
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
                var cell = new Rect(row.x + LabelWidth + (day - 1) * dayWidth, row.y, dayWidth, RowHeight);
                GUI.Label(cell, $"Day {day}", EditorStyles.miniBoldLabel);
            }
        }

        private void DrawEntryRows(float dayWidth)
        {
            foreach (var entry in _pool.Entries)
            {
                if (entry == null)
                    continue;

                Rect row = GUILayoutUtility.GetRect(0f, RowHeight + RowGap, GUILayout.ExpandWidth(true));
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
                    EditorGUI.LabelField(warn, "unreachable — starts after the last day", ErrorLabel);
                    continue;
                }

                float x = row.x + LabelWidth + (entry.FirstDay - 1) * dayWidth;
                var bar = new Rect(x + 1f, row.y + 2f, (last - entry.FirstDay + 1) * dayWidth - 2f, RowHeight - 4f);
                EditorGUI.DrawRect(bar, BarColor(entry.Encounter));
                // "w2" = overridden on this row, "w2*" = inherited from the encounter's own
                // DropWeight. Worth distinguishing: it's the difference between "this pool
                // made it rare" and "it's rare everywhere".
                string weightLabel = $"w{entry.ResolvedWeight:0.##}{(entry.InheritsWeight ? "*" : "")}";
                GUI.Label(bar, $"  {weightLabel}{(entry.OncePerRun ? "  once" : "")}", BarLabel);
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

                var cell = new Rect(row.x + LabelWidth + (day - 1) * dayWidth, row.y, dayWidth - 2f, RowHeight);
                EditorGUI.DrawRect(cell, count == 0 ? EmptyDay : Color.clear);
                GUI.Label(cell, $" {count} / {weight:0.##}", count == 0 ? ErrorLabel : EditorStyles.miniLabel);
            }
        }

        private void DrawRolledPreview(float dayWidth)
        {
            EditorGUILayout.LabelField($"Seed {_seed} — what this campaign actually generates", EditorStyles.boldLabel);

            int tallest = 1;
            foreach (var picks in _rolled)
                tallest = Mathf.Max(tallest, picks?.Count ?? 0);

            for (int slot = 0; slot < tallest; slot++)
            {
                Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
                for (int day = 1; day <= _pool.Days; day++)
                {
                    var picks = _rolled[day - 1];
                    var cell = new Rect(row.x + LabelWidth + (day - 1) * dayWidth, row.y, dayWidth - 2f, RowHeight - 2f);

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
