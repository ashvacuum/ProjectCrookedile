using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    public enum IssueLevel
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>One audit finding on one asset.</summary>
    public readonly struct Issue
    {
        public readonly IssueLevel Level;
        public readonly string Text;

        public Issue(IssueLevel level, string text)
        {
            Level = level;
            Text = text;
        }

        public static Issue Error(string text) => new Issue(IssueLevel.Error, text);

        public static Issue Warning(string text) => new Issue(IssueLevel.Warning, text);

        public static Issue Info(string text) => new Issue(IssueLevel.Info, text);
    }

    /// <summary>
    /// One tab of the <see cref="DatabaseWindow"/>. The window only talks to this
    /// non-generic face; everything shared lives in <see cref="ContentTab{T}"/>.
    /// </summary>
    public abstract class ContentTab
    {
        public abstract string Title { get; }

        /// <summary>Assets in this tab with at least one warning or error.</summary>
        public abstract int ProblemCount { get; }

        public abstract void Reload();

        public abstract void OnGUI(Rect area);

        public abstract void OnDisable();

        /// <summary>Set by the window so deferred work can ask for a redraw.</summary>
        public Action Repaint;

        /// <summary>Runs <paramref name="work"/> after the current GUI pass, then repaints.</summary>
        protected void Later(Action work) =>
            EditorApplication.delayCall += () =>
            {
                work();
                Repaint?.Invoke();
            };
    }

    /// <summary>
    /// The shared browser for one kind of content: finding every item, search, type-specific
    /// filters, sortable columns, a per-item audit, and a detail pane with a preview plus the
    /// backing asset's own inspector (Odin draws it when installed). Items are usually assets;
    /// a tab over something else (a code type, a registry entry, an audit finding) overrides
    /// <see cref="Find"/> and <see cref="AssetOf"/>. A tab subclass only describes its content:
    /// columns, filters, audit rules, preview and actions.
    /// </summary>
    public abstract class ContentTab<T> : ContentTab
        where T : class
    {
        /// <summary>One column of the list. A null <see cref="SortKey"/> makes it unsortable.</summary>
        protected sealed class Column
        {
            public readonly string Header;
            public readonly float Width;
            public readonly Func<T, string> Text;
            public readonly Func<T, IComparable> SortKey;
            public readonly Func<T, Color?> Badge;

            public Column(
                string header,
                float width,
                Func<T, string> text,
                Func<T, IComparable> sortKey = null,
                Func<T, Color?> badge = null
            )
            {
                Header = header;
                Width = width;
                Text = text;
                SortKey = sortKey;
                Badge = badge;
            }
        }

        /// <summary>A button in the tab's toolbar.</summary>
        protected readonly struct TabAction
        {
            public readonly string Label;
            public readonly Action Run;

            public TabAction(string label, Action run)
            {
                Label = label;
                Run = run;
            }
        }

        private const float RowHeight = 20f;
        private const float StatusWidth = 22f;

        private List<T> _all = new List<T>();
        private List<T> _shown = new List<T>();
        private readonly Dictionary<T, List<Issue>> _issues = new Dictionary<T, List<Issue>>();
        private List<Column> _columns;
        private string _search = "";
        private bool _problemsOnly;
        private int _sortColumn;
        private bool _sortDescending;
        private T _selected;
        private UnityEditor.Editor _inspector;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private float _split = 0.55f;
        private Rect _body;
        private bool _hasPending;
        private T _pending;

        // ---- What a tab describes -------------------------------------------------------

        /// <summary>Name shown in the list and searched first.</summary>
        protected virtual string DisplayName(T item) => item is Object asset ? asset.name : item.ToString();

        /// <summary>The asset behind an item, for the inspector and Ping / Duplicate / Delete. Null for none.</summary>
        protected virtual Object AssetOf(T item) => item as Object;

        /// <summary>Extra text the search box matches (tags, descriptions).</summary>
        protected virtual string SearchText(T item) => "";

        protected abstract IEnumerable<Column> BuildColumns();

        /// <summary>Audit findings for one asset. Errors and warnings count as problems.</summary>
        protected abstract IEnumerable<Issue> Audit(T item);

        /// <summary>Draws the tab's own filters. Return true when one changed.</summary>
        protected virtual bool DrawFilters() => false;

        protected virtual bool PassesFilters(T item) => true;

        /// <summary>How the selected asset looks, above its inspector.</summary>
        protected virtual void DrawPreview(T item) { }

        /// <summary>Shown in the detail pane when nothing is selected: counts and breakdowns.</summary>
        protected virtual void DrawSummary(IReadOnlyList<T> all) { }

        protected virtual IEnumerable<TabAction> Actions() => Enumerable.Empty<TabAction>();

        /// <summary>Called after the assets are found and before they are audited.</summary>
        protected virtual void OnReloaded(IReadOnlyList<T> all) { }

        /// <summary>Finds the items. Default: every asset of type <typeparamref name="T"/>.</summary>
        protected virtual IEnumerable<T> Find() =>
            typeof(Object).IsAssignableFrom(typeof(T))
                ? FindAssets<T>()
                : Enumerable.Empty<T>();

        /// <summary>Every asset of type <typeparamref name="TAsset"/> in the project.</summary>
        protected static IEnumerable<TAsset> FindAssets<TAsset>()
            where TAsset : class =>
            AssetDatabase
                .FindAssets("t:" + typeof(TAsset).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g), typeof(TAsset)) as TAsset)
                .Where(a => a != null)
                .Distinct();

        /// <summary>Issues found for <paramref name="item"/> in the last reload.</summary>
        protected IReadOnlyList<Issue> IssuesOf(T item) =>
            item != null && _issues.TryGetValue(item, out var list) ? list : (IReadOnlyList<Issue>)Array.Empty<Issue>();

        /// <summary>Re-applies search, filters and sort without reloading assets.</summary>
        protected void Refilter()
        {
            IEnumerable<T> rows = _all.Where(PassesFilters);
            if (!string.IsNullOrWhiteSpace(_search))
                rows = rows.Where(i =>
                    Contains(DisplayName(i), _search) || Contains(SearchText(i), _search)
                );
            if (_problemsOnly)
                rows = rows.Where(i => IsProblem(IssuesOf(i)));

            var column = _columns[Mathf.Clamp(_sortColumn, 0, _columns.Count - 1)];
            if (column.SortKey != null)
            {
                rows = rows.OrderBy(column.SortKey).ThenBy(DisplayName);
                if (_sortDescending)
                    rows = rows.Reverse();
            }
            _shown = rows.ToList();
        }

        // ---- ContentTab ---------------------------------------------------------------

        public override int ProblemCount => _all.Count(i => IsProblem(IssuesOf(i)));

        public override void Reload()
        {
            _columns ??= BuildColumns().ToList();
            _all = Find().OrderBy(DisplayName).ToList();
            OnReloaded(_all);
            _issues.Clear();
            foreach (var item in _all)
                _issues[item] = RunAudit(item);
            if (_selected != null && !_all.Contains(_selected))
                Select(null);
            Refilter();
        }

        public override void OnDisable() => Select(null);

        public override void OnGUI(Rect area)
        {
            if (_columns == null)
                return; // not loaded yet; the window reloads it on the next layout pass
            if (_hasPending && Event.current.type == EventType.Layout)
            {
                _hasPending = false;
                Select(_pending);
            }
            GUILayout.BeginArea(area);
            DrawToolbar();
            if (DrawFilters())
                Refilter();

            // The layout pass only hands back a placeholder rect, and the detail pane lays itself
            // out inside this one, so keep the last real rect and draw with it in every event.
            Rect body = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            if (Event.current.type != EventType.Layout)
                _body = body;
            body = _body;
            var listRect = new Rect(body.x, body.y, body.width * _split, body.height);
            var detailRect = new Rect(listRect.xMax + 4, body.y, body.width - listRect.width - 4, body.height);
            DrawList(listRect);
            EditorGUI.DrawRect(new Rect(listRect.xMax + 1, body.y, 1, body.height), new Color(0, 0, 0, 0.3f));
            DrawDetail(detailRect);
            GUILayout.EndArea();
        }

        // ---- Drawing ------------------------------------------------------------------

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(160));
                if (search != _search)
                {
                    _search = search;
                    Refilter();
                }
                bool problems = GUILayout.Toggle(
                    _problemsOnly,
                    $"Problems only ({ProblemCount})",
                    EditorStyles.toolbarButton
                );
                if (problems != _problemsOnly)
                {
                    _problemsOnly = problems;
                    Refilter();
                }
                GUILayout.Label($"{_shown.Count} / {_all.Count}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                foreach (var action in Actions())
                {
                    var run = action.Run;
                    if (GUILayout.Button(action.Label, EditorStyles.toolbarButton))
                        Later(() =>
                        {
                            run();
                            Reload();
                        });
                }
                if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
                    Later(Reload);
            }
        }

        private void DrawList(Rect rect)
        {
            float x = rect.x + StatusWidth;
            var header = new Rect(rect.x, rect.y, rect.width, RowHeight);
            EditorGUI.DrawRect(header, new Color(0, 0, 0, 0.15f));
            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                var cell = new Rect(x, rect.y, ColumnWidth(c, rect.width), RowHeight);
                string label = col.Header + (c == _sortColumn && col.SortKey != null ? (_sortDescending ? " ▼" : " ▲") : "");
                if (GUI.Button(cell, label, EditorStyles.miniBoldLabel) && col.SortKey != null)
                {
                    _sortDescending = c == _sortColumn && !_sortDescending;
                    _sortColumn = c;
                    Refilter();
                }
                x += cell.width;
            }

            var view = new Rect(rect.x, rect.y + RowHeight, rect.width, rect.height - RowHeight);
            var content = new Rect(0, 0, view.width - 16, _shown.Count * RowHeight);
            _listScroll = GUI.BeginScrollView(view, _listScroll, content);
            int first = Mathf.Max(0, (int)(_listScroll.y / RowHeight));
            int last = Mathf.Min(_shown.Count, first + (int)(view.height / RowHeight) + 2);
            for (int i = first; i < last; i++)
                DrawRow(new Rect(0, i * RowHeight, content.width, RowHeight), _shown[i], i);
            GUI.EndScrollView();
        }

        private void DrawRow(Rect rect, T item, int index)
        {
            bool selected = item == _selected;
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(
                    rect,
                    selected ? new Color(0.24f, 0.48f, 0.9f, 0.45f)
                    : index % 2 == 0 ? new Color(0, 0, 0, 0.06f)
                    : Color.clear
                );

            var issues = IssuesOf(item);
            var worst = issues.Count == 0 ? (IssueLevel?)null : issues.Max(i => i.Level);
            if (worst.HasValue)
                GUI.Label(
                    new Rect(rect.x + 2, rect.y + 1, StatusWidth, RowHeight),
                    new GUIContent(LevelIcon(worst.Value), string.Join("\n", issues.Select(i => i.Text)))
                );

            float x = rect.x + StatusWidth;
            for (int c = 0; c < _columns.Count; c++)
            {
                var col = _columns[c];
                var cell = new Rect(x + 1, rect.y + 1, ColumnWidth(c, rect.width + 16) - 2, RowHeight - 2);
                var badge = col.Badge?.Invoke(item);
                if (badge.HasValue && Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(cell, badge.Value);
                GUI.Label(cell, c == 0 ? DisplayName(item) : col.Text(item) ?? "", badge.HasValue ? WhiteMini : EditorStyles.miniLabel);
                x += ColumnWidth(c, rect.width + 16);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectLater(item);
                if (Event.current.clickCount == 2 && AssetOf(item) != null)
                    EditorGUIUtility.PingObject(AssetOf(item));
                Event.current.Use();
            }
        }

        private void DrawDetail(Rect rect)
        {
            GUILayout.BeginArea(rect);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            if (_selected == null)
            {
                EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);
                DrawSummary(_all);
            }
            else
            {
                var asset = AssetOf(_selected);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(DisplayName(_selected), EditorStyles.boldLabel);
                    if (asset != null && GUILayout.Button("Ping", EditorStyles.miniButtonLeft, GUILayout.Width(44)))
                        EditorGUIUtility.PingObject(asset);
                    // Duplicate and delete only make sense when the item IS the asset.
                    if (_selected is Object own && AssetDatabase.Contains(own))
                    {
                        if (GUILayout.Button("Duplicate", EditorStyles.miniButtonMid, GUILayout.Width(66)))
                            Later(() => Duplicate(own));
                        if (GUILayout.Button("Delete", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                            Later(() => Delete(own));
                    }
                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                        SelectLater(null);
                }
                if (_selected != null)
                {
                    foreach (var issue in IssuesOf(_selected).OrderByDescending(i => i.Level))
                        EditorGUILayout.HelpBox(issue.Text, issue.Level switch
                        {
                            IssueLevel.Error => MessageType.Error,
                            IssueLevel.Warning => MessageType.Warning,
                            _ => MessageType.Info,
                        });
                    DrawPreview(_selected);
                    EditorGUILayout.Space();
                    if (asset != null)
                    {
                        UnityEditor.Editor.CreateCachedEditor(asset, null, ref _inspector);
                        EditorGUI.BeginChangeCheck();
                        _inspector.OnInspectorGUI();
                        if (EditorGUI.EndChangeCheck())
                            _issues[_selected] = RunAudit(_selected);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ---- Helpers ------------------------------------------------------------------

        /// <summary>Selects on the next layout pass, so one frame never draws two layouts.</summary>
        protected void SelectLater(T item)
        {
            _pending = item;
            _hasPending = true;
            Repaint?.Invoke();
        }

        private void Select(T item)
        {
            _selected = item;
            if (_inspector != null)
            {
                Object.DestroyImmediate(_inspector);
                _inspector = null;
            }
            GUI.FocusControl(null);
        }

        private void Duplicate(Object item)
        {
            string path = AssetDatabase.GetAssetPath(item);
            string copy = AssetDatabase.GenerateUniqueAssetPath(path);
            if (AssetDatabase.CopyAsset(path, copy))
            {
                Reload();
                Select(AssetDatabase.LoadAssetAtPath(copy, item.GetType()) as T);
            }
        }

        private void Delete(Object item)
        {
            string path = AssetDatabase.GetAssetPath(item);
            if (!EditorUtility.DisplayDialog("Delete asset", $"Move '{Path.GetFileName(path)}' to the trash?", "Delete", "Cancel"))
                return;
            Select(null);
            AssetDatabase.MoveAssetToTrash(path);
            Reload();
        }

        private float ColumnWidth(int index, float total)
        {
            // The first column (the name) takes whatever the fixed-width columns leave.
            if (index != 0)
                return _columns[index].Width;
            float fixedWidth = _columns.Skip(1).Sum(c => c.Width) + StatusWidth + 16;
            return Mathf.Max(_columns[0].Width, total - fixedWidth);
        }

        private List<Issue> RunAudit(T item)
        {
            try
            {
                return Audit(item).ToList();
            }
            catch (Exception e)
            {
                return new List<Issue> { Issue.Error($"Audit threw: {e.Message}") };
            }
        }

        private static bool IsProblem(IReadOnlyList<Issue> issues) =>
            issues.Any(i => i.Level != IssueLevel.Info);

        private static bool Contains(string text, string part) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        private static Texture LevelIcon(IssueLevel level) =>
            EditorGUIUtility.IconContent(
                level switch
                {
                    IssueLevel.Error => "console.erroricon.sml",
                    IssueLevel.Warning => "console.warnicon.sml",
                    _ => "console.infoicon.sml",
                }
            ).image;

        private static GUIStyle _whiteMini;

        private static GUIStyle WhiteMini =>
            _whiteMini ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };

        // ---- Shared drawing helpers for tabs --------------------------------------------

        /// <summary>A labelled row of toggle buttons for an optional enum filter. True when changed.</summary>
        protected static bool EnumFilter<TEnum>(string label, ref TEnum? value, Func<TEnum, string> name = null)
            where TEnum : struct, Enum
        {
            bool changed = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(62));
                if (GUILayout.Toggle(!value.HasValue, "All", EditorStyles.miniButtonLeft) && value.HasValue)
                {
                    value = null;
                    changed = true;
                }
                foreach (TEnum option in Enum.GetValues(typeof(TEnum)))
                {
                    bool on = value.HasValue && value.Value.Equals(option);
                    if (GUILayout.Toggle(on, name != null ? name(option) : option.ToString(), EditorStyles.miniButtonMid) && !on)
                    {
                        value = option;
                        changed = true;
                    }
                }
                GUILayout.FlexibleSpace();
            }
            return changed;
        }

        /// <summary>A bar for one share of a total, for summaries.</summary>
        protected static void Bar(string label, int count, int total)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(110));
                Rect r = GUILayoutUtility.GetRect(100, 16, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.2f));
                EditorGUI.DrawRect(new Rect(r.x, r.y, total > 0 ? r.width * count / total : 0, r.height), new Color(0.3f, 0.6f, 1f, 0.6f));
                GUI.Label(r, count.ToString(), WhiteMini);
            }
        }

        /// <summary>A sprite drawn at a fixed height, keeping its aspect.</summary>
        protected static void Picture(Sprite sprite, float height)
        {
            if (sprite == null)
            {
                EditorGUILayout.HelpBox("No art.", MessageType.None);
                return;
            }
            var tex = AssetPreview.GetAssetPreview(sprite) ?? sprite.texture;
            float aspect = sprite.rect.height > 0 ? sprite.rect.width / sprite.rect.height : 1f;
            Rect r = GUILayoutUtility.GetRect(height * aspect, height, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
        }
    }
}
