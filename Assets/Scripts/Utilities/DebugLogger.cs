using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;

namespace Crookedile.Utilities
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4
    }

    public class DebugLogger : Singleton<DebugLogger>
    {
        [SerializeField] private DebugSettings _settings;

        private Dictionary<string, bool> _categoryEnabled = new Dictionary<string, bool>();
        private Dictionary<string, LogLevel> _categoryLogLevels = new Dictionary<string, LogLevel>();

        private bool _globalLoggingEnabled = true;
        private LogLevel _globalLogLevel = LogLevel.Info;
        private bool _showTimestamp = true;
        private bool _showStackTrace = false;

        protected override void OnAwake()
        {
            if (_settings != null)
            {
                _settings.ApplySettings();
            }
            else
            {
                // Default categories if no settings provided
                RegisterCategory("Core", true, LogLevel.Info);
                RegisterCategory("Gameplay", true, LogLevel.Info);
                RegisterCategory("UI", true, LogLevel.Info);
                RegisterCategory("Audio", true, LogLevel.Warning);
                RegisterCategory("Card", true, LogLevel.Info);
                RegisterCategory("Battle", true, LogLevel.Info);
                RegisterCategory("Resources", true, LogLevel.Info);
                RegisterCategory("Events", true, LogLevel.Info);
                RegisterCategory("Save", true, LogLevel.Warning);
            }
        }

        public void RegisterCategory(string category, bool enabled = true, LogLevel logLevel = LogLevel.Info)
        {
            if (!_categoryEnabled.ContainsKey(category))
            {
                _categoryEnabled[category] = enabled;
                _categoryLogLevels[category] = logLevel;
            }
        }

        public void SetCategoryEnabled(string category, bool enabled)
        {
            if (_categoryEnabled.ContainsKey(category))
            {
                _categoryEnabled[category] = enabled;
            }
            else
            {
                RegisterCategory(category, enabled);
            }
        }

        public void SetCategoryLogLevel(string category, LogLevel logLevel)
        {
            if (_categoryLogLevels.ContainsKey(category))
            {
                _categoryLogLevels[category] = logLevel;
            }
            else
            {
                RegisterCategory(category, true, logLevel);
            }
        }

        public void SetGlobalLogLevel(LogLevel logLevel)
        {
            _globalLogLevel = logLevel;
        }

        public void SetGlobalLoggingEnabled(bool enabled)
        {
            _globalLoggingEnabled = enabled;
        }

        public void SetShowTimestamp(bool show)
        {
            _showTimestamp = show;
        }

        public void SetShowStackTrace(bool show)
        {
            _showStackTrace = show;
        }

        private bool ShouldLog(string category, LogLevel logLevel)
        {
            if (!_globalLoggingEnabled) return false;

            if (!_categoryEnabled.ContainsKey(category)) return false;
            if (!_categoryEnabled[category]) return false;

            LogLevel categoryLevel = _categoryLogLevels.ContainsKey(category)
                ? _categoryLogLevels[category]
                : _globalLogLevel;

            return logLevel <= categoryLevel;
        }

        private string FormatMessage(string category, string message)
        {
            string timestamp = _showTimestamp ? $"[{Time.time:F2}] " : "";
            return $"{timestamp}[{category}] {message}";
        }

        public void Log(string category, string message, LogLevel logLevel = LogLevel.Info, Object context = null)
        {
            if (!ShouldLog(category, logLevel)) return;

            string formattedMessage = FormatMessage(category, message);

            switch (logLevel)
            {
                case LogLevel.Error:
                    Debug.LogError($"<color=red>{formattedMessage}</color>", context);
                    if (_showStackTrace) Debug.LogError(System.Environment.StackTrace);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning($"<color=yellow>{formattedMessage}</color>", context);
                    break;
                case LogLevel.Info:
                    Debug.Log($"<color=cyan>{formattedMessage}</color>", context);
                    break;
                case LogLevel.Verbose:
                    Debug.Log($"<color=gray>{formattedMessage}</color>", context);
                    break;
            }
        }

        public void LogError(string category, string message, Object context = null)
        {
            Log(category, message, LogLevel.Error, context);
        }

        public void LogWarning(string category, string message, Object context = null)
        {
            Log(category, message, LogLevel.Warning, context);
        }

        public void LogInfo(string category, string message, Object context = null)
        {
            Log(category, message, LogLevel.Info, context);
        }

        public void LogVerbose(string category, string message, Object context = null)
        {
            Log(category, message, LogLevel.Verbose, context);
        }

        public void PrintAllCategories()
        {
            Debug.Log("=== Debug Logger Categories ===");
            foreach (var kvp in _categoryEnabled)
            {
                string category = kvp.Key;
                bool enabled = kvp.Value;
                LogLevel level = _categoryLogLevels.ContainsKey(category) ? _categoryLogLevels[category] : _globalLogLevel;
                Debug.Log($"{category}: {(enabled ? "Enabled" : "Disabled")} (Level: {level})");
            }
        }
    }

    // Static helper class for easier access
    public static class GameLogger
    {
        public static void Log(string category, string message, LogLevel logLevel = LogLevel.Info, Object context = null)
        {
            DebugLogger.Instance?.Log(category, message, logLevel, context);
        }

        public static void LogError(string category, string message, Object context = null)
        {
            DebugLogger.Instance?.LogError(category, message, context);
        }

        public static void LogWarning(string category, string message, Object context = null)
        {
            DebugLogger.Instance?.LogWarning(category, message, context);
        }

        public static void LogInfo(string category, string message, Object context = null)
        {
            DebugLogger.Instance?.LogInfo(category, message, context);
        }

        public static void LogVerbose(string category, string message, Object context = null)
        {
            DebugLogger.Instance?.LogVerbose(category, message, context);
        }

        public static void RegisterCategory(string category, bool enabled = true, LogLevel logLevel = LogLevel.Info)
        {
            DebugLogger.Instance?.RegisterCategory(category, enabled, logLevel);
        }

        public static void SetCategoryEnabled(string category, bool enabled)
        {
            DebugLogger.Instance?.SetCategoryEnabled(category, enabled);
        }

        public static void SetCategoryLogLevel(string category, LogLevel logLevel)
        {
            DebugLogger.Instance?.SetCategoryLogLevel(category, logLevel);
        }
    }
}
