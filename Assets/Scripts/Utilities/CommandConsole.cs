using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public class CommandConsole
    {
        private static Dictionary<string, Action<string[]>> _commands = new Dictionary<string, Action<string[]>>();
        private static Dictionary<string, string> _commandDescriptions = new Dictionary<string, string>();

        public static void RegisterCommand(string commandName, Action<string[]> callback, string description = "")
        {
            commandName = commandName.ToLower();
            _commands[commandName] = callback;
            _commandDescriptions[commandName] = description;
            GameLogger.LogVerbose("Core", $"Command registered: {commandName}");
        }

        public static void UnregisterCommand(string commandName)
        {
            commandName = commandName.ToLower();
            if (_commands.ContainsKey(commandName))
            {
                _commands.Remove(commandName);
                _commandDescriptions.Remove(commandName);
            }
        }

        public static void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                GameLogger.LogWarning("Core", "Empty command");
                return;
            }

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            if (_commands.ContainsKey(commandName))
            {
                try
                {
                    _commands[commandName].Invoke(args);
                    GameLogger.LogInfo("Core", $"Command executed: {commandName}");
                }
                catch (Exception e)
                {
                    GameLogger.LogError("Core", $"Command failed: {commandName} - {e.Message}");
                }
            }
            else
            {
                GameLogger.LogWarning("Core", $"Unknown command: {commandName}");
            }
        }

        public static void ListCommands()
        {
            Debug.Log("=== Available Commands ===");
            foreach (var kvp in _commandDescriptions)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }

        public static void RegisterDefaultCommands()
        {
            RegisterCommand("help", args => ListCommands(), "List all available commands");

            RegisterCommand("clear", args => Debug.ClearDeveloperConsole(), "Clear console");

            RegisterCommand("quit", args => Application.Quit(), "Quit application");

            RegisterCommand("timescale", args =>
            {
                if (args.Length > 0 && float.TryParse(args[0], out float scale))
                {
                    Time.timeScale = Mathf.Max(0f, scale);
                    Debug.Log($"Time scale set to {Time.timeScale}");
                }
                else
                {
                    Debug.Log($"Current time scale: {Time.timeScale}");
                }
            }, "Get or set time scale. Usage: timescale [value]");

            RegisterCommand("log", args =>
            {
                if (args.Length > 0)
                {
                    string category = args[0];
                    bool enabled = args.Length > 1 ? bool.Parse(args[1]) : true;
                    GameLogger.SetCategoryEnabled(category, enabled);
                    Debug.Log($"Logging for '{category}' set to {enabled}");
                }
            }, "Enable/disable logging for category. Usage: log <category> [true/false]");

            RegisterCommand("loglevel", args =>
            {
                if (args.Length > 1)
                {
                    string category = args[0];
                    if (Enum.TryParse(args[1], true, out LogLevel level))
                    {
                        GameLogger.SetCategoryLogLevel(category, level);
                        Debug.Log($"Log level for '{category}' set to {level}");
                    }
                }
            }, "Set log level for category. Usage: loglevel <category> <None|Error|Warning|Info|Verbose>");

            GameLogger.LogInfo("Core", "Default commands registered");
        }
    }
}
