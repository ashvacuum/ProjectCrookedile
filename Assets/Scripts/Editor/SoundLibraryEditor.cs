using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Audio;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    [CustomEditor(typeof(SoundLibrary))]
    public class SoundLibraryEditor : OdinEditor
    {
        private SoundLibrary database;
        private Vector2 scrollPosition;
        private List<AudioClipData> filteredClips;
        private string searchFilter = "";
        private string filterByCategory = null;
        private SortMode currentSortMode = SortMode.Name;
        private bool sortDescending = false;
        private ViewMode viewMode = ViewMode.Statistics;

        private enum ViewMode
        {
            Statistics,
            SoundBrowser,
        }

        private enum SortMode
        {
            Name,
            ID,
            Category,
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            database = (SoundLibrary)target;
            RefreshFilteredClips();
        }

        public override void OnInspectorGUI()
        {
            if (database == null)
                return;
            DrawHeader();
            DrawViewModeSelector();
            EditorGUILayout.Space(10);

            switch (viewMode)
            {
                case ViewMode.Statistics:
                    DrawStatisticsView();
                    break;
                case ViewMode.SoundBrowser:
                    DrawSoundBrowserView();
                    break;
            }

            EditorGUILayout.Space(10);
            DrawDefaultInspector();
        }

        #region Header
        private void DrawHeader()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sound Library Manager", SirenixGUIStyles.BoldTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{database.Count} Clips", SirenixGUIStyles.BoldLabel);
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndBoxHeader();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Database", GUILayout.Height(35)))
            {
                database.RefreshDatabase();
                RefreshFilteredClips();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Validate Library", GUILayout.Height(35)))
            {
                ValidateSoundLibrary(database);
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBox();
        }

        private void DrawViewModeSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (
                GUILayout.Toggle(
                    viewMode == ViewMode.Statistics,
                    "Statistics",
                    SirenixGUIStyles.Button,
                    GUILayout.Width(120),
                    GUILayout.Height(25)
                )
            )
                viewMode = ViewMode.Statistics;

            if (
                GUILayout.Toggle(
                    viewMode == ViewMode.SoundBrowser,
                    "Sound Browser",
                    SirenixGUIStyles.Button,
                    GUILayout.Width(120),
                    GUILayout.Height(25)
                )
            )
            {
                viewMode = ViewMode.SoundBrowser;
                RefreshFilteredClips();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Statistics View
        private void DrawStatisticsView()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title("Library Statistics", "", TextAlignment.Left, true);

            var allClips = database.GetAll();

            // Category breakdown
            EditorGUILayout.Space(5);
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Categories", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var categories = allClips
                .Select(c => string.IsNullOrEmpty(c.Category) ? "(uncategorised)" : c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var cat in categories)
            {
                string lookup = cat == "(uncategorised)" ? "" : cat;
                int count = allClips.Count(c => (c.Category ?? "") == lookup);
                float pct = allClips.Count > 0 ? count / (float)allClips.Count * 100f : 0f;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{cat}:", GUILayout.Width(130));
                DrawProgressBar(count, allClips.Count, $"{count} ({pct:F1}%)");
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            SirenixEditorGUI.EndVerticalList();

            // Quick health check
            EditorGUILayout.Space(10);
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Quick Health Check", EditorStyles.boldLabel);

            int nullClips = allClips.Count(c => c.Clip == null);
            int emptyIds = allClips.Count(c => string.IsNullOrEmpty(c.ID));
            int dupIds = allClips.GroupBy(c => c.ID).Count(g => g.Count() > 1);
            int emptyNames = allClips.Count(c => string.IsNullOrEmpty(c.ClipName));
            int dupNames = allClips
                .Where(c => !string.IsNullOrEmpty(c.ClipName))
                .GroupBy(c => c.ClipName, System.StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Count() > 1);

            DrawHealthRow("Null Clips", nullClips);
            DrawHealthRow("Empty IDs", emptyIds);
            DrawHealthRow("Duplicate IDs", dupIds);
            DrawHealthRow("Empty Names", emptyNames);
            DrawHealthRow("Duplicate Names", dupNames);

            SirenixEditorGUI.EndVerticalList();
            SirenixEditorGUI.EndBox();
        }

        private void DrawHealthRow(string label, int issueCount)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"{label}:", GUILayout.Width(140));
            Color prev = GUI.color;
            GUI.color = issueCount == 0 ? Color.green : Color.red;
            GUILayout.Label(
                issueCount == 0 ? "✓ OK" : $"✗ {issueCount}",
                SirenixGUIStyles.BoldLabel
            );
            GUI.color = prev;
            GUILayout.EndHorizontal();
        }

        private void DrawProgressBar(int current, int max, string label)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            float fill = max > 0 ? current / (float)max : 0f;

            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, rect.width * fill, rect.height),
                new Color(0.3f, 0.7f, 1f, 0.6f)
            );
            SirenixEditorGUI.DrawBorders(rect, 1);
            GUI.Label(rect, label, SirenixGUIStyles.CenteredWhiteMiniLabel);
        }

        #endregion

        #region Sound Browser View
        private void DrawSoundBrowserView()
        {
            SirenixEditorGUI.BeginBox();

            // Filters
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Filters", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            string newSearch = EditorGUILayout.TextField(searchFilter);
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                RefreshFilteredClips();
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
                filterByCategory = null;
                RefreshFilteredClips();
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            // Category filter buttons
            var cats = database
                .GetAll()
                .Select(c => string.IsNullOrEmpty(c.Category) ? "(uncategorised)" : c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            if (cats.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Category:", GUILayout.Width(60));
                if (GUILayout.Toggle(filterByCategory == null, "All", SirenixGUIStyles.MiniButton))
                {
                    if (filterByCategory != null)
                    {
                        filterByCategory = null;
                        RefreshFilteredClips();
                    }
                }
                foreach (var cat in cats)
                {
                    if (GUILayout.Toggle(filterByCategory == cat, cat, SirenixGUIStyles.MiniButton))
                    {
                        if (filterByCategory != cat)
                        {
                            filterByCategory = cat;
                            RefreshFilteredClips();
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }

            SirenixEditorGUI.EndVerticalList();

            // Sort controls
            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort By:", GUILayout.Width(60));
            foreach (SortMode mode in System.Enum.GetValues(typeof(SortMode)))
            {
                if (
                    GUILayout.Toggle(
                        currentSortMode == mode,
                        mode.ToString(),
                        SirenixGUIStyles.MiniButton
                    )
                )
                {
                    if (currentSortMode == mode)
                        sortDescending = !sortDescending;
                    else
                    {
                        currentSortMode = mode;
                        sortDescending = false;
                    }
                    RefreshFilteredClips();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(sortDescending ? "↓" : "↑", GUILayout.Width(30)))
            {
                sortDescending = !sortDescending;
                RefreshFilteredClips();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            SirenixEditorGUI.HorizontalLineSeparator();
            EditorGUILayout.Space(5);

            // Clip list
            if (filteredClips == null || filteredClips.Count == 0)
            {
                SirenixEditorGUI.MessageBox("No clips matching filters.", MessageType.Info);
            }
            else
            {
                GUILayout.Label(
                    $"Showing {filteredClips.Count} clip(s)",
                    SirenixGUIStyles.CenteredGreyMiniLabel
                );
                EditorGUILayout.Space(5);

                scrollPosition = EditorGUILayout.BeginScrollView(
                    scrollPosition,
                    GUILayout.MaxHeight(400)
                );
                for (int i = 0; i < filteredClips.Count; i++)
                    DrawClipEntry(filteredClips[i], i);
                EditorGUILayout.EndScrollView();
            }

            SirenixEditorGUI.EndBox();
        }

        private void DrawClipEntry(AudioClipData clip, int index)
        {
            bool isOdd = index % 2 == 1;
            Color bg = isOdd
                ? new Color(0.25f, 0.25f, 0.25f, 0.3f)
                : new Color(0.2f, 0.2f, 0.2f, 0.2f);

            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(24));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(lineRect, bg);

            Rect content = new Rect(
                lineRect.x + 5,
                lineRect.y + 2,
                lineRect.width - 10,
                lineRect.height - 4
            );
            float x = content.x;

            // ClipName (primary) — click to select asset
            string displayName = string.IsNullOrEmpty(clip.ClipName)
                ? "⚠ (no name)"
                : clip.ClipName;
            if (
                GUI.Button(
                    new Rect(x, content.y, 160, 20),
                    displayName,
                    SirenixGUIStyles.LeftAlignedWhiteMiniLabel
                )
            )
            {
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
            }
            x += 165;

            // ID truncated (first 8 chars) — secondary reference
            string shortId = string.IsNullOrEmpty(clip.ID)
                ? "(no id)"
                : clip.ID.Substring(0, Mathf.Min(8, clip.ID.Length)) + "…";
            GUI.Label(
                new Rect(x, content.y, 80, 18),
                shortId,
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
            x += 85;

            // Category badge
            string catLabel = string.IsNullOrEmpty(clip.Category) ? "(none)" : clip.Category;
            Rect catRect = new Rect(x, content.y, 85, 18);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(catRect, new Color(0.3f, 0.5f, 0.9f, 0.6f));
            GUI.Label(catRect, catLabel, SirenixGUIStyles.CenteredWhiteMiniLabel);
            x += 90;

            // AudioClip asset name
            string clipName = clip.Clip != null ? clip.Clip.name : "⚠ null";
            GUI.Label(
                new Rect(x, content.y, 130, 18),
                clipName,
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
            x += 135;

            // Volume + pitch
            GUI.Label(
                new Rect(x, content.y, 105, 18),
                $"Vol:{clip.Volume:F2}  P:{clip.Pitch:F2}",
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
        }

        #endregion

        #region Filtering / Sorting
        private void RefreshFilteredClips()
        {
            if (database == null)
                return;
            filteredClips = database.GetAll();

            if (!string.IsNullOrWhiteSpace(searchFilter))
                filteredClips = filteredClips
                    .Where(c =>
                        (
                            !string.IsNullOrEmpty(c.ClipName)
                            && c.ClipName.IndexOf(
                                searchFilter,
                                System.StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                        || (
                            !string.IsNullOrEmpty(c.ID)
                            && c.ID.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase)
                                >= 0
                        )
                        || (
                            c.Clip != null
                            && c.Clip.name.IndexOf(
                                searchFilter,
                                System.StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                    )
                    .ToList();

            if (filterByCategory != null)
            {
                string lookup = filterByCategory == "(uncategorised)" ? "" : filterByCategory;
                filteredClips = filteredClips.Where(c => (c.Category ?? "") == lookup).ToList();
            }

            filteredClips = currentSortMode switch
            {
                SortMode.Name => sortDescending
                    ? filteredClips.OrderByDescending(c => c.ClipName).ToList()
                    : filteredClips.OrderBy(c => c.ClipName).ToList(),
                SortMode.ID => sortDescending
                    ? filteredClips.OrderByDescending(c => c.ID).ToList()
                    : filteredClips.OrderBy(c => c.ID).ToList(),
                SortMode.Category => sortDescending
                    ? filteredClips.OrderByDescending(c => c.Category).ToList()
                    : filteredClips.OrderBy(c => c.Category).ToList(),
                _ => filteredClips,
            };
        }

        #endregion

        #region Validation
        private void ValidateSoundLibrary(SoundLibrary db)
        {
            var allClips = db.GetAll();
            int issueCount = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("Sound Library Validation Report");
            report.AppendLine("================================\n");

            // Duplicate IDs
            var dupIdGroups = allClips.GroupBy(c => c.ID).Where(g => g.Count() > 1).ToList();
            foreach (var grp in dupIdGroups)
            {
                issueCount++;
                report.AppendLine(
                    $"Duplicate ID \"{grp.Key}\": {grp.Count()} assets share this ID."
                );
                foreach (var c in grp)
                {
                    report.AppendLine($"  - {c.name}");
                    Debug.LogWarning($"[SoundLibrary] Duplicate ID \"{grp.Key}\" on: {c.name}", c);
                }
                report.AppendLine();
            }

            // Duplicate Names
            var dupNameGroups = allClips
                .Where(c => !string.IsNullOrEmpty(c.ClipName))
                .GroupBy(c => c.ClipName, System.StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();
            foreach (var grp in dupNameGroups)
            {
                issueCount++;
                report.AppendLine(
                    $"Duplicate Name \"{grp.Key}\": {grp.Count()} assets share this name."
                );
                foreach (var c in grp)
                {
                    report.AppendLine($"  - {c.name}");
                    Debug.LogWarning(
                        $"[SoundLibrary] Duplicate Name \"{grp.Key}\" on: {c.name}",
                        c
                    );
                }
                report.AppendLine();
            }

            // Per-clip issues
            foreach (var clip in allClips)
            {
                var issues = new System.Text.StringBuilder();
                if (string.IsNullOrEmpty(clip.ID))
                    issues.AppendLine("  - ID is empty");
                if (string.IsNullOrEmpty(clip.ClipName))
                    issues.AppendLine("  - ClipName is empty");
                if (clip.Clip == null)
                    issues.AppendLine("  - AudioClip is null");

                if (issues.Length > 0)
                {
                    issueCount++;
                    report.AppendLine($"Asset: {clip.name}");
                    report.Append(issues);
                    report.AppendLine();
                    Debug.LogWarning($"[SoundLibrary] Issues on \"{clip.name}\":\n{issues}", clip);
                }
            }

            if (issueCount == 0)
            {
                Debug.Log(
                    report.AppendLine($"All {allClips.Count} clips passed validation!").ToString()
                );
                EditorUtility.DisplayDialog(
                    "Sound Library",
                    $"All {allClips.Count} clips passed validation!",
                    "OK"
                );
            }
            else
            {
                Debug.LogWarning(report.ToString());
                EditorUtility.DisplayDialog(
                    "Sound Library",
                    $"Found {issueCount} issue(s).\nCheck the Console for details.",
                    "OK"
                );
            }
        }
        #endregion
    }
}
