using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Core;
using UnityEngine;

namespace Crookedile.Utilities
{
    /// <summary>
    /// In-game developer console. Press tilde (~) to open.
    /// Tabs: Logs (filtered game log stream) | Cheats (command list with toggles).
    /// Command input at the bottom runs cheat commands in either tab.
    /// </summary>
    [Debuggable("Console", LogLevel.Info)]
    public class DeveloperConsole : Singleton<DeveloperConsole>
    {
        [Header("Settings")]
        [SerializeField]
        private bool _enabled = true;

        [SerializeField]
        private KeyCode _toggleKey = KeyCode.BackQuote;

        [SerializeField]
        private int _maxEntries = 200;

        [SerializeField]
        private int _maxCmdHistory = 50;

        #region State
        private bool _isVisible;
        private string _currentInput = "";

        private enum Tab
        {
            Logs,
            Cheats,
        }

        private Tab _activeTab = Tab.Logs;

        // Log entries
        private readonly List<ConsoleEntry> _entries = new List<ConsoleEntry>();
        private Vector2 _logScroll;

        // Level filter — which levels are currently shown in Logs tab
        private readonly HashSet<LogLevel> _shownLevels = new HashSet<LogLevel>
        {
            LogLevel.Error,
            LogLevel.Warning,
            LogLevel.Info,
            LogLevel.Verbose,
        };

        // Category filter — null means show all
        private string _categoryFilter = "";

        // Cheats tab
        private Vector2 _cheatsScroll;

        // Command input
        private readonly List<string> _cmdHistory = new List<string>();
        private int _historyIndex = -1;
        private List<string> _suggestions = new List<string>();
        private int _suggestionIdx;

        // Command registry
        private readonly Dictionary<string, RegisteredCommand> _commands = new Dictionary<
            string,
            RegisteredCommand
        >(StringComparer.OrdinalIgnoreCase);

        public bool IsVisible => _isVisible;

        #endregion

        #region Lifecycle
        protected override void OnAwake()
        {
            DiscoverCommands();
            GameLogger.OnLogAdded += OnGameLogAdded;
            AddSystem("Developer Console ready  •  type 'help' for commands");
            GameLogger.LogInfo("Console", "Developer Console initialised");
        }

        private void OnDestroy() => GameLogger.OnLogAdded -= OnGameLogAdded;

        private void OnGameLogAdded(LogEntry entry)
        {
            string color = entry.Level switch
            {
                LogLevel.Error => "red",
                LogLevel.Warning => "yellow",
                LogLevel.Verbose => "grey",
                _ => "white",
            };
            AddEntry(
                $"<color={color}>[{entry.Category}] {entry.Message}</color>",
                entry.Level,
                entry.Category
            );
        }

        #endregion

        #region Input
        private void Update()
        {
            if (!_enabled)
                return;

            if (Input.GetKeyDown(_toggleKey))
                ToggleConsole();
            if (!_isVisible)
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow))
                NavigateHistory(-1);
            if (Input.GetKeyDown(KeyCode.DownArrow))
                NavigateHistory(1);

            if (_suggestions.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                    AcceptSuggestion();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!string.IsNullOrWhiteSpace(_currentInput))
                {
                    ExecuteCommand(_currentInput);
                    _cmdHistory.Insert(0, _currentInput);
                    if (_cmdHistory.Count > _maxCmdHistory)
                        _cmdHistory.RemoveAt(_cmdHistory.Count - 1);
                    _currentInput = "";
                    _historyIndex = -1;
                    _suggestions.Clear();
                }
            }
        }

        #endregion

        #region GUI
        private void OnGUI()
        {
            if (!_isVisible)
                return;

            float w = Screen.width * 0.9f;
            float h = Screen.height * 0.5f;
            float x = Screen.width * 0.05f;
            float y = 10f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none);

            float innerX = x + 6f;
            float innerW = w - 12f;
            float curY = y + 6f;

        #endregion

            #region Tab bar
            curY = DrawTabBar(innerX, curY, innerW);
            curY += 4f;

            #region Content area (leaves room for input + suggestions at bottom)
            const float inputH = 26f;
            const float suggestH = 80f;
            float contentH = h - (curY - y) - inputH - 10f;

            if (_activeTab == Tab.Logs)
                curY = DrawLogsTab(innerX, curY, innerW, contentH);
            else
                curY = DrawCheatsTab(innerX, curY, innerW, contentH);

            #endregion

            #region Command input
            curY = y + h - inputH - 4f;
            DrawCommandInput(innerX, curY, innerW, inputH, suggestH);
        }

        private float DrawTabBar(float x, float y, float w)
        {
            float btnW = 80f;
            float gap = 4f;

            if (DrawTabButton(x, y, btnW, "Logs", _activeTab == Tab.Logs))
                _activeTab = Tab.Logs;
            if (DrawTabButton(x + btnW + gap, y, btnW, "Cheats", _activeTab == Tab.Cheats))
                _activeTab = Tab.Cheats;

            return y + 22f;
        }

        private bool DrawTabButton(float x, float y, float w, string label, bool active)
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = active ? Color.yellow : Color.white;
            return GUI.Button(new Rect(x, y, w, 20f), label, style);
        }

            #endregion

        #region Logs tab
        private float DrawLogsTab(float x, float y, float w, float contentH)
        {
            // Level filter buttons
            y = DrawLevelFilters(x, y, w);
            y += 2f;

            // Category filter field
            GUI.Label(new Rect(x, y, 60f, 18f), "Category:");
            _categoryFilter = GUI.TextField(
                new Rect(x + 64f, y, w - 64f, 18f),
                _categoryFilter ?? ""
            );
            y += 22f;

            // Log output
            var visible = _entries
                .Where(e =>
                    e.Level == LogLevel.None
                    || // system/command messages always shown
                    (
                        _shownLevels.Contains(e.Level)
                        && (
                            string.IsNullOrEmpty(_categoryFilter)
                            || e.Category.IndexOf(
                                _categoryFilter,
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                    )
                )
                .ToList();

            float scrollH = contentH - (y - (y - contentH)); // remaining height
            // Recalculate based on what we've consumed
            float used = y;
            float logH =
                contentH - (used - (Screen.height * 0.05f + 10f + 6f + 22f + 4f + 22f + 2f + 22f));

            // Simpler: just use what's left
            logH = Mathf.Max(40f, contentH - 46f);

            _logScroll = GUI.BeginScrollView(
                new Rect(x, y, w, logH),
                _logScroll,
                new Rect(0, 0, w - 16f, Mathf.Max(logH, visible.Count * 16f))
            );

            float lineY = 0f;
            foreach (var entry in visible)
            {
                GUI.Label(new Rect(4f, lineY, w - 20f, 16f), entry.Text);
                lineY += 16f;
            }

            GUI.EndScrollView();
            return y + logH;
        }

        private float DrawLevelFilters(float x, float y, float w)
        {
            float btnW = 68f;
            float gap = 3f;
            var levels = new[]
            {
                LogLevel.Error,
                LogLevel.Warning,
                LogLevel.Info,
                LogLevel.Verbose,
            };
            var colors = new[] { Color.red, Color.yellow, Color.cyan, Color.grey };

            for (int i = 0; i < levels.Length; i++)
            {
                bool active = _shownLevels.Contains(levels[i]);
                var style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = active ? colors[i] : new Color(0.4f, 0.4f, 0.4f);
                bool clicked = GUI.Button(
                    new Rect(x + i * (btnW + gap), y, btnW, 20f),
                    levels[i].ToString(),
                    style
                );
                if (clicked)
                {
                    if (active)
                        _shownLevels.Remove(levels[i]);
                    else
                        _shownLevels.Add(levels[i]);
                }
            }

            return y + 22f;
        }

        #endregion

        #region Cheats tab
        private float DrawCheatsTab(float x, float y, float w, float contentH)
        {
            GUI.Label(
                new Rect(x, y, w, 18f),
                "<b>Toggle cheats on/off. Disabled commands will not execute.</b>"
            );
            y += 20f;

            _cheatsScroll = GUI.BeginScrollView(
                new Rect(x, y, w, contentH - 22f),
                _cheatsScroll,
                new Rect(0, 0, w - 16f, _commands.Count * 44f)
            );

            float rowY = 0f;
            foreach (var kvp in _commands.OrderBy(c => c.Value.Category).ThenBy(c => c.Key))
            {
                var cmd = kvp.Value;

                // Toggle button
                string label = cmd.Enabled ? "ON" : "OFF";
                Color btnCol = cmd.Enabled ? Color.green : Color.red;
                var style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = btnCol;

                if (GUI.Button(new Rect(4f, rowY + 4f, 36f, 20f), label, style))
                    cmd.Enabled = !cmd.Enabled;

                // Command name + category
                string cat = string.IsNullOrEmpty(cmd.Category)
                    ? ""
                    : $" <color=grey>[{cmd.Category}]</color>";
                GUI.Label(new Rect(46f, rowY, w - 60f, 20f), $"<b>{cmd.Command}</b>{cat}");

                // Description + parameters
                string parms = string.Join(
                    ", ",
                    cmd.Parameters.Select(p =>
                        p.HasDefaultValue
                            ? $"{p.ParameterType.Name} {p.Name}={p.DefaultValue}"
                            : $"{p.ParameterType.Name} {p.Name}"
                    )
                );
                string desc = string.IsNullOrEmpty(cmd.Description)
                    ? ""
                    : $"  —  {cmd.Description}";
                GUI.Label(
                    new Rect(46f, rowY + 20f, w - 60f, 18f),
                    $"<color=grey>({parms}){desc}</color>"
                );

                rowY += 44f;
            }

            GUI.EndScrollView();
            return y + contentH - 22f;
        }

        #endregion

        #region Command input
        private void DrawCommandInput(float x, float y, float w, float inputH, float maxSuggestH)
        {
            // Autocomplete popup (above input)
            if (_suggestions.Count > 0)
            {
                float sh = Mathf.Min(_suggestions.Count * 20f, maxSuggestH);
                GUI.Box(new Rect(x, y - sh - 2f, w, sh), GUIContent.none);
                for (int i = 0; i < _suggestions.Count; i++)
                {
                    var style = new GUIStyle(GUI.skin.label);
                    style.normal.textColor = i == _suggestionIdx ? Color.yellow : Color.white;
                    GUI.Label(
                        new Rect(x + 4f, y - sh - 2f + i * 20f, w - 8f, 20f),
                        _suggestions[i],
                        style
                    );
                }
            }

            GUI.SetNextControlName("ConsoleInput");
            _currentInput = GUI.TextField(new Rect(x, y, w, inputH), _currentInput);
            GUI.FocusControl("ConsoleInput");

            UpdateAutoComplete();
        }

        #endregion

        #region Toggle
        private void ToggleConsole()
        {
            _isVisible = !_isVisible;
            if (!_isVisible)
                return;

            _currentInput = "";
            _suggestions.Clear();

            // Backfill history on first open
            if (_entries.Count <= 1)
            {
                foreach (var e in GameLogger.Entries)
                    OnGameLogAdded(e);
            }

            _logScroll.y = float.MaxValue;
        }

        #endregion

        #region Command execution
        private void ExecuteCommand(string input)
        {
            AddSystem($"> {input}");

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            string name = parts[0].ToLower();

            if (name == "help")
            {
                ShowHelp();
                return;
            }
            if (name == "clear")
            {
                _entries.Clear();
                return;
            }

            if (!_commands.TryGetValue(name, out var cmd))
            {
                AddSystem($"<color=red>Unknown command: {name}. Type 'help'.</color>");
                return;
            }

            if (!cmd.Enabled)
            {
                AddSystem($"<color=grey>Command '{name}' is disabled.</color>");
                return;
            }

            try
            {
                object[] parms = ParseParameters(parts.Skip(1).ToArray(), cmd.Parameters);
                cmd.Method.Invoke(cmd.Target, parms);
                GameLogger.LogInfo("Console", $"Executed: {name}");
            }
            catch (Exception e)
            {
                AddSystem($"<color=red>Error: {e.InnerException?.Message ?? e.Message}</color>");
                GameLogger.LogError("Console", $"Command failed: {name} — {e.Message}");
            }
        }

        private void ShowHelp()
        {
            AddSystem("<color=cyan>=== Commands ===</color>");
            foreach (
                var group in _commands
                    .Values.GroupBy(c => c.Category ?? "General")
                    .OrderBy(g => g.Key)
            )
            {
                AddSystem($"<color=yellow>{group.Key}</color>");
                foreach (var c in group.OrderBy(x => x.Command))
                {
                    string state = c.Enabled ? "" : " <color=red>[disabled]</color>";
                    AddSystem($"  {c.Command}{state} — {c.Description}");
                }
            }
        }

        #endregion

        #region Autocomplete
        private void UpdateAutoComplete()
        {
            if (string.IsNullOrWhiteSpace(_currentInput))
            {
                _suggestions.Clear();
                return;
            }

            string prefix = _currentInput.Split(' ')[0].ToLower();
            _suggestions = _commands
                .Keys.Where(k => k.StartsWith(prefix))
                .OrderBy(k => k)
                .Select(k =>
                {
                    var c = _commands[k];
                    string p = string.Join(
                        ", ",
                        c.Parameters.Select(x => $"{x.ParameterType.Name} {x.Name}")
                    );
                    return $"{k} ({p})";
                })
                .Take(5)
                .ToList();

            _suggestionIdx = Mathf.Clamp(_suggestionIdx, 0, Mathf.Max(0, _suggestions.Count - 1));
        }

        private void AcceptSuggestion()
        {
            if (_suggestions.Count == 0)
                return;
            _currentInput = _suggestions[_suggestionIdx].Split(' ')[0] + " ";
            _suggestions.Clear();
        }

        private void NavigateHistory(int dir)
        {
            if (_cmdHistory.Count == 0)
                return;
            _historyIndex = Mathf.Clamp(_historyIndex + dir, -1, _cmdHistory.Count - 1);
            _currentInput = _historyIndex >= 0 ? _cmdHistory[_historyIndex] : "";
        }

        #endregion

        #region Entry helpers
        private void AddSystem(string text) => AddEntry(text, LogLevel.None, "");

        private void AddEntry(string text, LogLevel level, string category)
        {
            if (_entries.Count >= _maxEntries)
                _entries.RemoveAt(0);
            _entries.Add(
                new ConsoleEntry
                {
                    Text = text,
                    Level = level,
                    Category = category,
                }
            );
            _logScroll.y = float.MaxValue;
        }

        #endregion

        #region Command discovery
        private void DiscoverCommands()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    foreach (
                        var method in type.GetMethods(
                            BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.Instance
                                | BindingFlags.Static
                        )
                    )
                    {
                        var attr = method.GetCustomAttribute<CheatCommandAttribute>();
                        if (attr != null)
                            TryRegisterCommand(method, attr);
                    }
                }
                catch
                { /* skip unloadable assemblies */
                }
            }

            GameLogger.LogInfo("Console", $"Discovered {_commands.Count} cheat commands");
        }

        private void TryRegisterCommand(MethodInfo method, CheatCommandAttribute attr)
        {
            object target = method.IsStatic ? null : FindFirstObjectByType(method.DeclaringType);
            if (!method.IsStatic && target == null)
            {
                GameLogger.LogVerbose(
                    "Console",
                    $"No instance of {method.DeclaringType?.Name} for command '{attr.Command}'"
                );
                return;
            }

            _commands[attr.Command.ToLower()] = new RegisteredCommand
            {
                Command = attr.Command,
                Description = attr.Description,
                Category = attr.Category,
                Method = method,
                Target = target,
                Parameters = method.GetParameters(),
                Enabled = true,
            };
        }

        #endregion

        #region Parameter parsing
        private static object[] ParseParameters(string[] args, ParameterInfo[] pInfo)
        {
            var result = new List<object>();
            for (int i = 0; i < pInfo.Length; i++)
            {
                if (i >= args.Length)
                {
                    if (pInfo[i].HasDefaultValue)
                    {
                        result.Add(pInfo[i].DefaultValue);
                        continue;
                    }
                    throw new Exception($"Missing required parameter: {pInfo[i].Name}");
                }

                string arg = args[i];
                Type type = pInfo[i].ParameterType;

                if (type == typeof(int))
                    result.Add(int.Parse(arg));
                else if (type == typeof(float))
                    result.Add(float.Parse(arg));
                else if (type == typeof(bool))
                    result.Add(bool.Parse(arg));
                else if (type == typeof(string))
                    result.Add(arg);
                else
                    throw new Exception($"Unsupported param type: {type.Name}");
            }
            return result.ToArray();
        }

        #endregion

        #region Types
        private struct ConsoleEntry
        {
            public string Text;
            public LogLevel Level;
            public string Category;
        }

        private class RegisteredCommand
        {
            public string Command;
            public string Description;
            public string Category;
            public MethodInfo Method;
            public object Target;
            public ParameterInfo[] Parameters;
            public bool Enabled;

        #endregion
        }

            #endregion
    }
}
