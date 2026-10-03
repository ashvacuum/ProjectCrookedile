using System;
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

        // Tabs whose assets may have changed since they last loaded. Only the visible tab
        // reloads; the others reload when shown, so a change doesn't rescan every tab (the
        // Checks tab walks every prefab).
        private readonly HashSet<ContentTab> _stale = new HashSet<ContentTab>();

        // Tabs that threw while loading or drawing, with the message shown in their place.
        private readonly Dictionary<ContentTab, string> _errors = new Dictionary<ContentTab, string>();

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
            var tab = _tabs[_current];
            if (Event.current.type == EventType.Layout && _stale.Remove(tab))
                TryReload(tab);

            string[] labels = _tabs
                .Select(t => !_stale.Contains(t) && !_errors.ContainsKey(t) && t.ProblemCount > 0 ? $"{t.Title} ({t.ProblemCount})" : t.Title)
                .ToArray();
            const float tabHeight = 24f;
            int picked = GUI.Toolbar(new Rect(4, 2, position.width - 8, tabHeight), _current, labels);
            if (picked != _current)
            {
                // Switch now and end this event, so the next event is a fresh layout pass that
                // draws only the new tab.
                tab.OnDisable();
                _current = picked;
                if (_stale.Remove(_tabs[_current]))
                    TryReload(_tabs[_current]);
                Repaint();
                GUIUtility.ExitGUI();
            }

            var area = new Rect(0, tabHeight + 4, position.width, position.height - tabHeight - 4);
            if (_errors.TryGetValue(tab, out string error))
            {
                GUILayout.BeginArea(area);
                EditorGUILayout.HelpBox($"The {tab.Title} tab failed:\n{error}", MessageType.Error);
                if (GUILayout.Button("Retry", GUILayout.Width(80)))
                {
                    _errors.Remove(tab);
                    _stale.Add(tab);
                    Repaint();
                }
                GUILayout.EndArea();
                return;
            }

            try
            {
                tab.OnGUI(area);
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                // A broken tab shows its error instead of taking the window down with it.
                _errors[tab] = e.Message;
                Debug.LogException(e);
                Repaint();
                GUIUtility.ExitGUI();
            }
        }

        private void TryReload(ContentTab tab)
        {
            try
            {
                tab.Reload();
                _errors.Remove(tab);
            }
            catch (Exception e)
            {
                _errors[tab] = "Loading failed: " + e.Message;
                Debug.LogException(e);
            }
        }
    }
}
