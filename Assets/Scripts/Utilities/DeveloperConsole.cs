using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Crookedile.Core;

namespace Crookedile.Utilities
{
    /// <summary>
    /// In-game developer console with autocomplete and command history.
    /// Opens with tilde (~) key. Automatically discovers cheat commands via reflection.
    /// </summary>
    [Debuggable("Console", LogLevel.Info)]
    public class DeveloperConsole : Singleton<DeveloperConsole>
    {
        [Header("Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.BackQuote; // Tilde key
        [SerializeField] private int _maxHistoryLines = 100;
        [SerializeField] private int _maxCommandHistory = 50;

        private bool _isVisible = false;
        private string _currentInput = "";
        private Vector2 _scrollPosition;
        private List<string> _outputLines = new List<string>();
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private List<string> _autoCompleteSuggestions = new List<string>();
        private int _selectedSuggestionIndex = 0;

        // Command registry
        private Dictionary<string, RegisteredCommand> _commands = new Dictionary<string, RegisteredCommand>();

        public bool IsVisible => _isVisible;

        protected override void OnAwake()
        {
            DiscoverCommands();
            AddOutput("Developer Console initialized. Type 'help' for commands.");
            GameLogger.LogInfo("Console", "Developer Console ready");
        }

        private void Update()
        {
            if (!_enabled) return;

            // Toggle console
            if (Input.GetKeyDown(_toggleKey))
            {
                ToggleConsole();
            }

            if (!_isVisible) return;

            // Command history navigation
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                NavigateHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                NavigateHistory(1);
            }

            // Autocomplete navigation
            if (_autoCompleteSuggestions.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    AcceptSuggestion();
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) && Input.GetKey(KeyCode.LeftControl))
                {
                    _selectedSuggestionIndex = (_selectedSuggestionIndex + 1) % _autoCompleteSuggestions.Count;
                }
                else if (Input.GetKeyDown(KeyCode.UpArrow) && Input.GetKey(KeyCode.LeftControl))
                {
                    _selectedSuggestionIndex--;
                    if (_selectedSuggestionIndex < 0) _selectedSuggestionIndex = _autoCompleteSuggestions.Count - 1;
                }
            }

            // Execute command
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!string.IsNullOrWhiteSpace(_currentInput))
                {
                    ExecuteCommand(_currentInput);
                    _commandHistory.Insert(0, _currentInput);
                    if (_commandHistory.Count > _maxCommandHistory)
                    {
                        _commandHistory.RemoveAt(_commandHistory.Count - 1);
                    }
                    _currentInput = "";
                    _historyIndex = -1;
                    _autoCompleteSuggestions.Clear();
                }
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            float consoleHeight = Screen.height * 0.4f;
            float consoleWidth = Screen.width * 0.9f;
            float consoleX = Screen.width * 0.05f;

            // Console background
            GUI.Box(new Rect(consoleX, 10, consoleWidth, consoleHeight), "");

            // Output area
            float outputHeight = consoleHeight - 60;
            GUILayout.BeginArea(new Rect(consoleX + 5, 15, consoleWidth - 10, outputHeight));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (string line in _outputLines)
            {
                GUILayout.Label(line);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Input area
            float inputY = 10 + outputHeight + 10;
            GUI.SetNextControlName("ConsoleInput");
            _currentInput = GUI.TextField(new Rect(consoleX + 5, inputY, consoleWidth - 10, 25), _currentInput);

            // Auto-complete suggestions
            if (_autoCompleteSuggestions.Count > 0)
            {
                float suggestionY = inputY + 30;
                float suggestionHeight = Mathf.Min(_autoCompleteSuggestions.Count * 20, 100);

                GUI.Box(new Rect(consoleX + 5, suggestionY, consoleWidth - 10, suggestionHeight), "");

                for (int i = 0; i < _autoCompleteSuggestions.Count && i < 5; i++)
                {
                    Color originalColor = GUI.color;
                    if (i == _selectedSuggestionIndex)
                    {
                        GUI.color = Color.yellow;
                    }

                    GUI.Label(new Rect(consoleX + 10, suggestionY + (i * 20), consoleWidth - 20, 20),
                        _autoCompleteSuggestions[i]);

                    GUI.color = originalColor;
                }
            }

            // Focus input when console opens
            GUI.FocusControl("ConsoleInput");

            // Update autocomplete
            UpdateAutoComplete();
        }

        private void ToggleConsole()
        {
            _isVisible = !_isVisible;
            if (_isVisible)
            {
                _currentInput = "";
                _autoCompleteSuggestions.Clear();
            }
        }

        private void UpdateAutoComplete()
        {
            if (string.IsNullOrWhiteSpace(_currentInput))
            {
                _autoCompleteSuggestions.Clear();
                return;
            }

            string[] parts = _currentInput.Split(' ');
            string commandPart = parts[0].ToLower();

            _autoCompleteSuggestions = _commands.Keys
                .Where(cmd => cmd.StartsWith(commandPart))
                .OrderBy(cmd => cmd)
                .Select(cmd =>
                {
                    var regCmd = _commands[cmd];
                    string paramInfo = string.Join(", ", regCmd.Parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    return $"{cmd} ({paramInfo}) - {regCmd.Description}";
                })
                .Take(5)
                .ToList();

            _selectedSuggestionIndex = Mathf.Clamp(_selectedSuggestionIndex, 0, _autoCompleteSuggestions.Count - 1);
        }

        private void AcceptSuggestion()
        {
            if (_autoCompleteSuggestions.Count == 0) return;

            string suggestion = _autoCompleteSuggestions[_selectedSuggestionIndex];
            string command = suggestion.Split(' ')[0];
            _currentInput = command + " ";
            _autoCompleteSuggestions.Clear();
        }

        private void NavigateHistory(int direction)
        {
            if (_commandHistory.Count == 0) return;

            _historyIndex += direction;
            _historyIndex = Mathf.Clamp(_historyIndex, -1, _commandHistory.Count - 1);

            if (_historyIndex >= 0)
            {
                _currentInput = _commandHistory[_historyIndex];
            }
            else
            {
                _currentInput = "";
            }
        }

        public void AddOutput(string message)
        {
            _outputLines.Add($"> {message}");

            if (_outputLines.Count > _maxHistoryLines)
            {
                _outputLines.RemoveAt(0);
            }

            _scrollPosition.y = float.MaxValue;
        }

        private void ExecuteCommand(string input)
        {
            AddOutput(input);

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();

            if (commandName == "help")
            {
                ShowHelp(parts.Length > 1 ? parts[1] : null);
                return;
            }

            if (commandName == "clear")
            {
                _outputLines.Clear();
                return;
            }

            if (!_commands.ContainsKey(commandName))
            {
                AddOutput($"<color=red>Unknown command: {commandName}</color>");
                AddOutput("Type 'help' for a list of commands");
                return;
            }

            RegisteredCommand cmd = _commands[commandName];

            try
            {
                object[] parameters = ParseParameters(parts.Skip(1).ToArray(), cmd.Parameters);
                cmd.Method.Invoke(cmd.Target, parameters);
            }
            catch (Exception e)
            {
                AddOutput($"<color=red>Error executing command: {e.InnerException?.Message ?? e.Message}</color>");
                GameLogger.LogError("Console", $"Command execution failed: {e.Message}");
            }
        }

        private object[] ParseParameters(string[] args, ParameterInfo[] paramInfo)
        {
            List<object> parameters = new List<object>();

            for (int i = 0; i < paramInfo.Length; i++)
            {
                ParameterInfo param = paramInfo[i];

                // Use default value if not provided
                if (i >= args.Length)
                {
                    if (param.HasDefaultValue)
                    {
                        parameters.Add(param.DefaultValue);
                    }
                    else
                    {
                        throw new Exception($"Missing required parameter: {param.Name}");
                    }
                    continue;
                }

                // Parse the argument
                string arg = args[i];
                Type paramType = param.ParameterType;

                try
                {
                    if (paramType == typeof(int))
                    {
                        parameters.Add(int.Parse(arg));
                    }
                    else if (paramType == typeof(float))
                    {
                        parameters.Add(float.Parse(arg));
                    }
                    else if (paramType == typeof(bool))
                    {
                        parameters.Add(bool.Parse(arg));
                    }
                    else if (paramType == typeof(string))
                    {
                        parameters.Add(arg);
                    }
                    else
                    {
                        throw new Exception($"Unsupported parameter type: {paramType.Name}");
                    }
                }
                catch
                {
                    throw new Exception($"Invalid value '{arg}' for parameter '{param.Name}' (expected {paramType.Name})");
                }
            }

            return parameters.ToArray();
        }

        private void ShowHelp(string category = null)
        {
            if (category == null)
            {
                AddOutput("<color=cyan>=== Available Commands ===</color>");

                var grouped = _commands.Values.GroupBy(cmd => cmd.Category ?? "General");

                foreach (var group in grouped.OrderBy(g => g.Key))
                {
                    AddOutput($"\n<color=yellow>{group.Key}:</color>");
                    foreach (var cmd in group.OrderBy(c => c.Command))
                    {
                        string paramInfo = string.Join(", ", cmd.Parameters.Select(p =>
                        {
                            string paramStr = $"{p.ParameterType.Name} {p.Name}";
                            if (p.HasDefaultValue)
                            {
                                paramStr += $" = {p.DefaultValue}";
                            }
                            return paramStr;
                        }));

                        AddOutput($"  {cmd.Command} ({paramInfo}) - {cmd.Description}");
                    }
                }

                AddOutput("\n<color=cyan>Built-in commands:</color>");
                AddOutput("  help [category] - Show this help");
                AddOutput("  clear - Clear console output");
            }
        }

        private void DiscoverCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                        foreach (var method in methods)
                        {
                            var attribute = method.GetCustomAttribute<CheatCommandAttribute>();
                            if (attribute != null)
                            {
                                RegisterCommand(method, attribute);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be reflected
                }
            }

            GameLogger.LogInfo("Console", $"Discovered {_commands.Count} cheat commands");
        }

        private void RegisterCommand(MethodInfo method, CheatCommandAttribute attribute)
        {
            object target = method.IsStatic ? null : FindFirstObjectByType(method.DeclaringType);

            if (!method.IsStatic && target == null)
            {
                GameLogger.LogWarning("Console", $"Could not find instance of {method.DeclaringType.Name} for command {attribute.Command}");
                return;
            }

            _commands[attribute.Command.ToLower()] = new RegisteredCommand
            {
                Command = attribute.Command,
                Description = attribute.Description,
                Category = attribute.Category,
                Method = method,
                Target = target,
                Parameters = method.GetParameters()
            };
        }

        private class RegisteredCommand
        {
            public string Command;
            public string Description;
            public string Category;
            public MethodInfo Method;
            public object Target;
            public ParameterInfo[] Parameters;
        }
    }
}
