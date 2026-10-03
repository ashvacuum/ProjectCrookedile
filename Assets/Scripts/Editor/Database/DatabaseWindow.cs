using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// One window for browsing, auditing and editing authored content, one tab per asset type.
    /// The shared browser is <see cref="ContentTab{T}"/>; each tab only describes its type.
    /// To add a type: write a tab subclass and list it in <see cref="CreateTabs"/>.
    /// </summary>
    public class DatabaseWindow : EditorWindow
    {
        private List<ContentTab> _tabs;
        private int _current;
        private int _next = -1;

        // Tabs whose assets may have changed since they last loaded. Only the visible tab
        // reloads; the others reload when shown, so a change doesn't rescan every tab (the
        // Checks tab walks every prefab).
        private readonly HashSet<ContentTab> _stale = new HashSet<ContentTab>();

        [MenuItem("Crookedile/Database", priority = 0)]
        private static void Open()
        {
            var window = GetWindow<DatabaseWindow>("Database");
            window.minSize = new Vector2(820, 480);
        }

        private static List<ContentTab> CreateTabs() =>
            new List<ContentTab>
            {
                new CardsTab(),
                new EnemiesTab(),
                new StatusesTab(),
                new AlliesTab(),
                new OriginsTab(),
                new EncountersTab(),
                new BattleSessionsTab(),
                new BuildingBlocksTab(),
                new ChecksTab(),
            };

        private void OnEnable()
        {
            _tabs = CreateTabs();
            foreach (var tab in _tabs)
            {
                tab.Repaint = Repaint;
                _stale.Add(tab);
            }
            EditorApplication.projectChanged += MarkDirty;
            Undo.undoRedoPerformed += MarkDirty;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= MarkDirty;
            Undo.undoRedoPerformed -= MarkDirty;
            foreach (var tab in _tabs)
                tab.OnDisable();
        }

        // Assets changed on disk (imports, renames, edits elsewhere): reload on the next draw
        // rather than mid-event.
        private void MarkDirty()
        {
            _stale.UnionWith(_tabs);
            Repaint();
        }

        private void OnGUI()
        {
            // Anything that changes what gets drawn waits for a layout pass, so the layout and
            // repaint passes of one frame always draw the same controls.
            if (Event.current.type == EventType.Layout)
            {
                if (_next >= 0)
                {
                    _tabs[_current].OnDisable();
                    _current = _next;
                    _next = -1;
                }
                if (_stale.Remove(_tabs[_current]))
                    _tabs[_current].Reload();
            }

            string[] labels = _tabs
                .Select(t => !_stale.Contains(t) && t.ProblemCount > 0 ? $"{t.Title} ({t.ProblemCount})" : t.Title)
                .ToArray();
            const float tabHeight = 24f;
            int picked = GUI.Toolbar(new Rect(4, 2, position.width - 8, tabHeight), _current, labels);
            if (picked != _current)
            {
                _next = picked;
                Repaint();
            }

            _tabs[_current].OnGUI(new Rect(0, tabHeight + 4, position.width, position.height - tabHeight - 4));
        }
    }
}
