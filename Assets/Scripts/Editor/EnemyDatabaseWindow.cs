using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Enemy;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Dockable dashboard window for browsing, auditing, and health-checking the enemy database.
    /// Open via menu: Crookedile → Enemy Database. Mirrors CardDatabaseWindow's layout. Per-enemy
    /// editing still happens in the Odin inspector on each EnemyData/EnemyMoveData asset (use
    /// "Inspect Asset" or "Select" to jump there).
    /// </summary>
    public class EnemyDatabaseWindow : EditorWindow
    {
        private EnemyDatabase database;
        private Vector2 scrollPosition;
        private List<EnemyData> filteredEnemies;
        private SortMode currentSortMode = SortMode.Name;
        private bool sortDescending = false;
        private string searchFilter = "";
        private EnemyMovePattern? filterByMovePattern = null;
        private bool filterWithPassivesOnly = false;

        private ViewMode viewMode = ViewMode.Statistics;

        private enum SortMode
        {
            Name,
            MaxHostility,
            MoveCount,
            PassiveCount,
        }

        private enum ViewMode
        {
            Statistics,
            EnemyBrowser,
            EnemyAudit,
        }

        [MenuItem("Crookedile/Enemy Database")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemyDatabaseWindow>("Enemy Database");
            window.minSize = new Vector2(560, 420);
            window.Show();
        }

        private void OnEnable()
        {
            LoadDatabase();
        }

        // Refresh when the window regains focus so external edits (new enemies, changed moves)
        // are reflected without reopening.
        private void OnFocus()
        {
            if (database != null)
                RefreshFilteredEnemies();
        }

        /// <summary>Finds the EnemyDatabase asset in the project (first match) and caches it.</summary>
        private void LoadDatabase()
        {
            if (database == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyDatabase");
                if (guids.Length > 0)
                    database = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(
                        AssetDatabase.GUIDToAssetPath(guids[0])
                    );
            }
            if (database != null)
                RefreshFilteredEnemies();
        }

        private void OnGUI()
        {
            if (database == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "No EnemyDatabase asset found in the project. Create one, then reopen this window.",
                    MessageType.Warning
                );
                if (GUILayout.Button("Search again", GUILayout.Height(30)))
                    LoadDatabase();
                return;
            }

            DrawHeader();
            DrawViewModeSelector();

            EditorGUILayout.Space(10);

            switch (viewMode)
            {
                case ViewMode.Statistics:
                    DrawStatisticsView();
                    break;
                case ViewMode.EnemyBrowser:
                    DrawEnemyBrowserView();
                    break;
                case ViewMode.EnemyAudit:
                    DrawEnemyAuditView();
                    break;
            }
        }

        #region Header

        private void DrawHeader()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enemy Database Manager", SirenixGUIStyles.BoldTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{database.Count} Enemies", SirenixGUIStyles.BoldLabel);
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndBoxHeader();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Refresh Database", GUILayout.Height(35)))
            {
                database.RefreshDatabase();
                RefreshFilteredEnemies();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Inspect Asset", GUILayout.Height(35)))
            {
                Selection.activeObject = database;
                EditorGUIUtility.PingObject(database);
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
                    viewMode == ViewMode.EnemyBrowser,
                    "Enemy Browser",
                    SirenixGUIStyles.Button,
                    GUILayout.Width(120),
                    GUILayout.Height(25)
                )
            )
            {
                viewMode = ViewMode.EnemyBrowser;
                RefreshFilteredEnemies();
            }

            int auditIssueCount = CountAuditIssues();
            string auditLabel = auditIssueCount > 0 ? $"Enemy Audit ({auditIssueCount})" : "Enemy Audit";
            if (
                GUILayout.Toggle(
                    viewMode == ViewMode.EnemyAudit,
                    auditLabel,
                    SirenixGUIStyles.Button,
                    GUILayout.Width(140),
                    GUILayout.Height(25)
                )
            )
                viewMode = ViewMode.EnemyAudit;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Statistics View

        private void DrawStatisticsView()
        {
            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.Title("Database Statistics", "", TextAlignment.Left, true);

            EditorGUILayout.Space(5);
            DrawStatSection(
                "Move Pattern",
                () =>
                {
                    foreach (EnemyMovePattern pattern in System.Enum.GetValues(typeof(EnemyMovePattern)))
                    {
                        int count = database.GetAll().Count(e => e.MovePattern == pattern);
                        float percentage =
                            database.Count > 0 ? (count / (float)database.Count) * 100f : 0f;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{pattern}:", GUILayout.Width(140));
                        DrawProgressBar(count, database.Count, $"{count} ({percentage:F1}%)");
                        GUILayout.EndHorizontal();
                    }
                }
            );

            EditorGUILayout.Space(10);

            DrawStatSection(
                "Roster Health",
                () =>
                {
                    var all = database.GetAll();
                    int withPassives = database.GetWithPassives().Count;
                    int summoners = database.GetSummoners().Count;
                    int issues = all.Count(e => GetEnemyIssues(e).Count > 0);

                    var healthStyle = new GUIStyle(EditorStyles.boldLabel);
                    healthStyle.normal.textColor =
                        issues > 0 ? new Color(1f, 0.65f, 0f) : new Color(0.4f, 0.85f, 0.4f);
                    GUILayout.Label(
                        issues > 0
                            ? $"⚠  {issues} enemy(ies) with configuration issues — see Enemy Audit"
                            : "✓  All enemies pass configuration checks",
                        healthStyle
                    );
                    GUILayout.Label(
                        $"With passives: {withPassives}    ·    Summoners: {summoners}",
                        SirenixGUIStyles.LeftAlignedGreyLabel
                    );
                }
            );

            EditorGUILayout.Space(10);

            DrawNotableEnemies();

            SirenixEditorGUI.EndBox();
        }

        private void DrawStatSection(string title, System.Action content)
        {
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            content();
            EditorGUI.indentLevel--;
            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawProgressBar(int current, int max, string label)
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            float fillAmount = max > 0 ? (current / (float)max) : 0f;

            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

            Rect fillRect = new Rect(rect.x, rect.y, rect.width * fillAmount, rect.height);
            EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 1f, 0.6f));

            SirenixEditorGUI.DrawBorders(rect, 1);
            GUI.Label(rect, label, SirenixGUIStyles.CenteredWhiteMiniLabel);
        }

        private void DrawNotableEnemies()
        {
            var all = database.GetAll();
            if (all.Count == 0)
                return;

            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Notable Enemies", EditorStyles.boldLabel);

            var mostHostile = all.OrderByDescending(e => e.MaxHostility).FirstOrDefault();
            if (mostHostile != null)
                DrawNotableEnemy("Highest Max Hostility", mostHostile, $"{mostHostile.MaxHostility}");

            var mostMoves = all.OrderByDescending(e => e.Moves?.Count ?? 0).FirstOrDefault();
            if (mostMoves != null && (mostMoves.Moves?.Count ?? 0) > 0)
                DrawNotableEnemy("Most Moves", mostMoves, $"{mostMoves.Moves.Count} moves");

            var mostPassives = all.OrderByDescending(e => e.Passives?.Count ?? 0).FirstOrDefault();
            if (mostPassives != null && (mostPassives.Passives?.Count ?? 0) > 0)
                DrawNotableEnemy("Most Passives", mostPassives, $"{mostPassives.Passives.Count} passives");

            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawNotableEnemy(string category, EnemyData enemy, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"{category}:", GUILayout.Width(160));

            if (GUILayout.Button(enemy.EnemyName, SirenixGUIStyles.MiniButton))
            {
                Selection.activeObject = enemy;
                EditorGUIUtility.PingObject(enemy);
            }

            GUILayout.Label(
                $"({value})",
                SirenixGUIStyles.RightAlignedGreyMiniLabel,
                GUILayout.Width(100)
            );
            GUILayout.EndHorizontal();
        }

        #endregion

        #region Enemy Browser View

        private void DrawEnemyBrowserView()
        {
            SirenixEditorGUI.BeginBox();

            DrawFilters();
            DrawSortControls();

            EditorGUILayout.Space(5);
            SirenixEditorGUI.HorizontalLineSeparator();
            EditorGUILayout.Space(5);

            DrawEnemyList();

            SirenixEditorGUI.EndBox();
        }

        private void DrawFilters()
        {
            SirenixEditorGUI.BeginVerticalList();
            GUILayout.Label("Filters", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            string newSearch = EditorGUILayout.TextField(searchFilter);
            if (newSearch != searchFilter)
            {
                searchFilter = newSearch;
                RefreshFilteredEnemies();
            }

            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
                filterByMovePattern = null;
                filterWithPassivesOnly = false;
                RefreshFilteredEnemies();
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pattern:", GUILayout.Width(60));

            if (GUILayout.Toggle(filterByMovePattern == null, "All", SirenixGUIStyles.MiniButton))
            {
                if (filterByMovePattern != null)
                {
                    filterByMovePattern = null;
                    RefreshFilteredEnemies();
                }
            }

            foreach (EnemyMovePattern pattern in System.Enum.GetValues(typeof(EnemyMovePattern)))
            {
                if (
                    GUILayout.Toggle(
                        filterByMovePattern == pattern,
                        pattern.ToString(),
                        SirenixGUIStyles.MiniButton
                    )
                )
                {
                    if (filterByMovePattern != pattern)
                    {
                        filterByMovePattern = pattern;
                        RefreshFilteredEnemies();
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Passives:", GUILayout.Width(60));
            bool newPassivesOnly = GUILayout.Toggle(
                filterWithPassivesOnly,
                "Has Passives",
                SirenixGUIStyles.MiniButton,
                GUILayout.Width(90)
            );
            if (newPassivesOnly != filterWithPassivesOnly)
            {
                filterWithPassivesOnly = newPassivesOnly;
                RefreshFilteredEnemies();
            }
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndVerticalList();
        }

        private void DrawSortControls()
        {
            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort By:", GUILayout.Width(60));

            foreach (SortMode mode in System.Enum.GetValues(typeof(SortMode)))
            {
                bool isActive = currentSortMode == mode;
                if (GUILayout.Toggle(isActive, mode.ToString(), SirenixGUIStyles.MiniButton))
                {
                    if (currentSortMode == mode)
                    {
                        sortDescending = !sortDescending;
                    }
                    else
                    {
                        currentSortMode = mode;
                        sortDescending = mode != SortMode.Name;
                    }
                    RefreshFilteredEnemies();
                }
            }

            GUILayout.FlexibleSpace();

            string orderIcon = sortDescending ? "↓" : "↑";
            if (GUILayout.Button(orderIcon, GUILayout.Width(30)))
            {
                sortDescending = !sortDescending;
                RefreshFilteredEnemies();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawEnemyList()
        {
            if (filteredEnemies == null || filteredEnemies.Count == 0)
            {
                SirenixEditorGUI.MessageBox("No enemies found matching filters.", MessageType.Info);
                return;
            }

            GUILayout.Label(
                $"Showing {filteredEnemies.Count} enemy(ies)",
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MaxHeight(400)
            );

            for (int i = 0; i < filteredEnemies.Count; i++)
            {
                var enemy = filteredEnemies[i];
                if (enemy == null)
                    continue;

                DrawEnemyEntry(enemy, i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEnemyEntry(EnemyData enemy, int index)
        {
            bool isOdd = index % 2 == 1;
            Color bgColor = isOdd
                ? new Color(0.25f, 0.25f, 0.25f, 0.3f)
                : new Color(0.2f, 0.2f, 0.2f, 0.2f);

            int moveCount = enemy.Moves?.Count ?? 0;
            int passiveCount = enemy.Passives?.Count ?? 0;

            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(24));

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(lineRect, bgColor);

            Rect contentRect = new Rect(
                lineRect.x + 5,
                lineRect.y + 2,
                lineRect.width - 10,
                lineRect.height - 4
            );

            float xOffset = contentRect.x;

            Rect nameRect = new Rect(xOffset, contentRect.y, 160, 20);
            if (
                GUI.Button(nameRect, enemy.EnemyName, SirenixGUIStyles.LeftAlignedWhiteMiniLabel)
            )
            {
                Selection.activeObject = enemy;
                EditorGUIUtility.PingObject(enemy);
            }
            xOffset += 165;

            DrawBadgeAtPosition(
                new Rect(xOffset, contentRect.y, 100, 18),
                enemy.MovePattern.ToString(),
                GetPatternColor(enemy.MovePattern)
            );
            xOffset += 105;

            GUI.Label(
                new Rect(xOffset, contentRect.y, 90, 18),
                $"Hostility {enemy.MinHostility}..{enemy.MaxHostility}",
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
            xOffset += 95;

            GUI.Label(
                new Rect(xOffset, contentRect.y, 55, 18),
                $"{moveCount} moves",
                SirenixGUIStyles.CenteredGreyMiniLabel
            );
            xOffset += 60;

            string passiveText = passiveCount > 0 ? $"{passiveCount} passives" : "";
            GUI.Label(
                new Rect(xOffset, contentRect.y, 70, 18),
                passiveText,
                SirenixGUIStyles.RightAlignedGreyMiniLabel
            );
        }

        private void DrawBadgeAtPosition(Rect rect, string label, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, color);
                SirenixEditorGUI.DrawBorders(rect, 1);
            }
            GUI.Label(rect, label, SirenixGUIStyles.CenteredWhiteMiniLabel);
        }

        private Color GetPatternColor(EnemyMovePattern pattern)
        {
            return pattern switch
            {
                EnemyMovePattern.Sequential => new Color(0.3f, 0.8f, 0.3f),
                EnemyMovePattern.Random => new Color(0.9f, 0.3f, 0.3f),
                EnemyMovePattern.RandomSequential => new Color(0.3f, 0.5f, 0.9f),
                _ => Color.grey,
            };
        }

        #endregion

        #region Filtering and Sorting

        private void RefreshFilteredEnemies()
        {
            if (database == null)
                return;

            filteredEnemies = database.GetAll();

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                filteredEnemies = filteredEnemies
                    .Where(e =>
                        e.EnemyName.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase)
                        >= 0
                    )
                    .ToList();
            }

            if (filterByMovePattern.HasValue)
            {
                filteredEnemies = filteredEnemies
                    .Where(e => e.MovePattern == filterByMovePattern.Value)
                    .ToList();
            }

            if (filterWithPassivesOnly)
            {
                filteredEnemies = filteredEnemies
                    .Where(e => e.Passives != null && e.Passives.Count > 0)
                    .ToList();
            }

            filteredEnemies = SortEnemies(filteredEnemies);
        }

        private List<EnemyData> SortEnemies(List<EnemyData> enemies)
        {
            IEnumerable<EnemyData> sorted = currentSortMode switch
            {
                SortMode.Name => enemies.OrderBy(e => e.EnemyName),
                SortMode.MaxHostility => enemies.OrderBy(e => e.MaxHostility),
                SortMode.MoveCount => enemies.OrderBy(e => e.Moves?.Count ?? 0),
                SortMode.PassiveCount => enemies.OrderBy(e => e.Passives?.Count ?? 0),
                _ => enemies.OrderBy(e => e.EnemyName),
            };

            if (sortDescending)
                sorted = sorted.Reverse();

            return sorted.ToList();
        }

        #endregion

        #region Enemy Audit View

        private void DrawEnemyAuditView()
        {
            var enemies = database.GetAll().OrderBy(e => e.EnemyName).ToList();

            var moves = AssetDatabase
                .FindAssets("t:EnemyMoveData")
                .Select(g =>
                    AssetDatabase.LoadAssetAtPath<EnemyMoveData>(AssetDatabase.GUIDToAssetPath(g))
                )
                .Where(m => m != null)
                .OrderBy(m => m.MoveName)
                .ToList();

            var enemiesWithIssues = enemies
                .Select(e => (enemy: e, issues: GetEnemyIssues(e)))
                .Where(x => x.issues.Count > 0)
                .ToList();

            var movesWithIssues = moves
                .Select(m => (move: m, issues: GetMoveIssues(m)))
                .Where(x => x.issues.Count > 0)
                .ToList();

            int totalIssues = enemiesWithIssues.Count + movesWithIssues.Count;

            SirenixEditorGUI.BeginBox();

            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.BeginHorizontal();

            if (totalIssues > 0)
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(1f, 0.65f, 0f);
                GUILayout.Label(
                    $"⚠  {enemiesWithIssues.Count} enemy issue(s)  ·  {movesWithIssues.Count} move issue(s)",
                    labelStyle
                );
            }
            else
            {
                var labelStyle = new GUIStyle(SirenixGUIStyles.BoldTitle);
                labelStyle.normal.textColor = new Color(0.4f, 0.85f, 0.4f);
                GUILayout.Label(
                    $"✓  All {enemies.Count} enemies and {moves.Count} moves are ready",
                    labelStyle
                );
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label(
                $"{enemies.Count} enemies  ·  {moves.Count} moves",
                SirenixGUIStyles.RightAlignedGreyMiniLabel
            );
            GUILayout.EndHorizontal();
            SirenixEditorGUI.EndBoxHeader();

            EditorGUILayout.Space(5);

            if (totalIssues == 0)
            {
                EditorGUILayout.Space(10);
                GUILayout.Label(
                    "All enemies and moves are ready for gameplay.",
                    SirenixGUIStyles.CenteredGreyMiniLabel
                );
                EditorGUILayout.Space(10);
                SirenixEditorGUI.EndBox();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.MaxHeight(500)
            );

            if (enemiesWithIssues.Count > 0)
            {
                GUILayout.Label("Enemies", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (var (enemy, issues) in enemiesWithIssues)
                {
                    SirenixEditorGUI.BeginVerticalList();
                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    GUILayout.Label(enemy.EnemyName, EditorStyles.boldLabel);
                    GUILayout.Label(
                        $"Moves: {enemy.Moves?.Count ?? 0}",
                        SirenixGUIStyles.LeftAlignedGreyLabel
                    );
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                    {
                        Selection.activeObject = enemy;
                        EditorGUIUtility.PingObject(enemy);
                    }

                    GUILayout.EndHorizontal();

                    foreach (var issue in issues)
                        GUILayout.Label($"• {issue}", EditorStyles.helpBox);

                    SirenixEditorGUI.EndVerticalList();
                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.Space(8);
            }

            if (movesWithIssues.Count > 0)
            {
                GUILayout.Label("Enemy Moves", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                foreach (var (move, issues) in movesWithIssues)
                {
                    SirenixEditorGUI.BeginVerticalList();
                    GUILayout.BeginHorizontal();

                    string displayName = string.IsNullOrWhiteSpace(move.MoveName)
                        ? "[Unnamed Move]"
                        : move.MoveName;

                    GUILayout.BeginVertical();
                    GUILayout.Label(displayName, EditorStyles.boldLabel);
                    GUILayout.Label($"{move.MoveType}", SirenixGUIStyles.LeftAlignedGreyLabel);
                    GUILayout.EndVertical();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(30)))
                    {
                        Selection.activeObject = move;
                        EditorGUIUtility.PingObject(move);
                    }

                    GUILayout.EndHorizontal();

                    foreach (var issue in issues)
                        GUILayout.Label($"• {issue}", EditorStyles.helpBox);

                    SirenixEditorGUI.EndVerticalList();
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
            SirenixEditorGUI.EndBox();
        }

        /// <summary>Number of enemy + move issues combined — drives the audit tab's badge count.</summary>
        private int CountAuditIssues()
        {
            int enemyIssues = database.GetAll().Sum(e => GetEnemyIssues(e).Count > 0 ? 1 : 0);
            int moveIssues = AssetDatabase
                .FindAssets("t:EnemyMoveData")
                .Select(g =>
                    AssetDatabase.LoadAssetAtPath<EnemyMoveData>(AssetDatabase.GUIDToAssetPath(g))
                )
                .Where(m => m != null)
                .Count(m => GetMoveIssues(m).Count > 0);
            return enemyIssues + moveIssues;
        }

        /// <summary>Returns validation issues for an enemy asset.</summary>
        private static List<string> GetEnemyIssues(EnemyData enemy)
        {
            var issues = new List<string>();

            if (enemy.Portrait == null)
                issues.Add("Missing portrait — battle UI will show a broken image slot");

            if (enemy.Moves == null || enemy.Moves.Count == 0)
                issues.Add("No moves defined — enemy cannot act on their turn");
            else
            {
                for (int i = 0; i < enemy.Moves.Count; i++)
                    if (enemy.Moves[i] == null)
                        issues.Add(
                            $"Move slot [{i}] is null — will cause a NullReferenceException at runtime"
                        );
            }

            return issues;
        }

        /// <summary>Returns validation issues for an enemy move asset.</summary>
        private static List<string> GetMoveIssues(EnemyMoveData move)
        {
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(move.MoveName))
                issues.Add("No move name — intent display will be blank in logs and debug UI");

            if (string.IsNullOrWhiteSpace(move.IntentDescription))
                issues.Add("No intent description — player cannot see what this move will do");

            bool hasEffects = move.Effects != null && move.Effects.Count > 0;

            if (!hasEffects && move.MoveType != EnemyMoveType.SummonMinion)
                issues.Add("No effects defined — move resolves but does nothing");

            if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                issues.Add(
                    "SummonMinion move has no MinionToSummon set — summon will silently fail"
                );

            return issues;
        }

        #endregion
    }
}
