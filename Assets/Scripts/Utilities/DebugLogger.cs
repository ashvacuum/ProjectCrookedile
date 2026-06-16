using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Utilities
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Verbose = 4,
    }

    /// <summary>
    /// Represents a single captured log entry. Read via <see cref="GameLogger.Entries"/>
    /// or subscribe to <see cref="GameLogger.OnLogAdded"/> for live GUI updates.
    /// </summary>
    public readonly struct LogEntry
    {
        public readonly float Time;
        public readonly string Category;
        public readonly string Message;
        public readonly LogLevel Level;

        public LogEntry(float time, string category, string message, LogLevel level)
        {
            Time = time;
            Category = category;
            Message = message;
            Level = level;
        }
    }

    /// <summary>
    /// Centralised logging service. Filters by category and level, writes to Unity's console,
    /// and maintains a circular buffer that GUI panels can read or subscribe to.
    ///
    /// Call via the static API:
    ///   GameLogger.LogInfo&lt;BattleManager&gt;("message")        — type name becomes category
    ///   GameLogger.LogInfo("Battle", "message")              — explicit category
    ///
    /// Configure per-category levels at startup:
    ///   GameLogger.Configure(debugSettingsAsset)
    ///
    /// Read logs in a GUI panel:
    ///   GameLogger.OnLogAdded += entry => myList.Add(entry);
    ///   var snapshot = GameLogger.Entries;
    /// </summary>
    public static class GameLogger
    {
        #region Configuration
        private static bool _globalEnabled = true;
        private static LogLevel _globalLevel = LogLevel.Info;
        private static bool _showTimestamp = true;

        private static readonly Dictionary<string, LogLevel> _categoryLevels = new Dictionary<
            string,
            LogLevel
        >(StringComparer.Ordinal);

        private static readonly Dictionary<string, bool> _categoryEnabled = new Dictionary<
            string,
            bool
        >(StringComparer.Ordinal);

        // Focus mode: when non-null, ONLY this category logs (all levels), bypassing the global
        // level and per-category filters. Lets you isolate one category without disabling the
        // others one by one. Toggle via SoloCategory / ClearSolo (or the "logsolo" console cmd).
        private static string _soloCategory;

        #endregion

        #region Log Buffer
        private const int BufferCapacity = 200;

        private static readonly Queue<LogEntry> _buffer = new Queue<LogEntry>(BufferCapacity);

        /// <summary>Fired on the Unity main thread whenever a new entry passes the filter.</summary>
        public static event Action<LogEntry> OnLogAdded;

        /// <summary>Snapshot of all buffered log entries (oldest first, max 200).</summary>
        public static IEnumerable<LogEntry> Entries => _buffer;

        #endregion

        #region Configuration API
        public static void Configure(DebugSettings settings)
        {
            if (settings == null)
                return;
            _globalEnabled = settings.globalLoggingEnabled;
            _globalLevel = settings.globalLogLevel;
            _showTimestamp = settings.showTimestamp;
            foreach (var cat in settings.categories)
                SetCategory(cat.categoryName, cat.enabled, cat.logLevel);
        }

        public static void SetCategory(
            string category,
            bool enabled,
            LogLevel level = LogLevel.Info
        )
        {
            _categoryEnabled[category] = enabled;
            _categoryLevels[category] = level;
        }

        public static void SetGlobalLevel(LogLevel level) => _globalLevel = level;

        public static void SetGlobalEnabled(bool enabled) => _globalEnabled = enabled;

        /// <summary>
        /// Focus the console on a single category: only <paramref name="category"/> logs
        /// (all its levels), everything else is muted, until <see cref="ClearSolo"/>.
        /// Pass null/empty to clear.
        /// </summary>
        public static void SoloCategory(string category) =>
            _soloCategory = string.IsNullOrEmpty(category) ? null : category;

        /// <summary>Clears focus mode — all categories log per their normal levels again.</summary>
        public static void ClearSolo() => _soloCategory = null;

        /// <summary>The category currently soloed, or null when focus mode is off.</summary>
        public static string CurrentSolo => _soloCategory;

        #endregion

        #region Console commands

        [CheatCommand("logsolo", "Mute all logs except one category (e.g. logsolo Card)")]
        private static string CmdLogSolo(string category)
        {
            SoloCategory(category);
            return $"Log focus → '{category}'. Everything else muted. Use 'logall' to restore.";
        }

        [CheatCommand("logall", "Clear log focus — all categories log again")]
        private static string CmdLogAll()
        {
            ClearSolo();
            return "Log focus cleared — all categories log per their levels.";
        }

        #region Logging API — generic (type name as category)
        public static void LogError<T>(string message, UnityEngine.Object context = null) =>
            Write(typeof(T).Name, message, LogLevel.Error, context);

        public static void LogWarning<T>(string message, UnityEngine.Object context = null) =>
            Write(typeof(T).Name, message, LogLevel.Warning, context);

        public static void LogInfo<T>(string message, UnityEngine.Object context = null) =>
            Write(typeof(T).Name, message, LogLevel.Info, context);

        public static void LogVerbose<T>(string message, UnityEngine.Object context = null) =>
            Write(typeof(T).Name, message, LogLevel.Verbose, context);

        #endregion

        #region Logging API — string category
        public static void LogError(
            string category,
            string message,
            UnityEngine.Object context = null
        ) => Write(category, message, LogLevel.Error, context);

        public static void LogWarning(
            string category,
            string message,
            UnityEngine.Object context = null
        ) => Write(category, message, LogLevel.Warning, context);

        public static void LogInfo(
            string category,
            string message,
            UnityEngine.Object context = null
        ) => Write(category, message, LogLevel.Info, context);

        public static void LogVerbose(
            string category,
            string message,
            UnityEngine.Object context = null
        ) => Write(category, message, LogLevel.Verbose, context);

        #endregion

        #region Internal
        private static void Write(
            string category,
            string message,
            LogLevel level,
            UnityEngine.Object context
        )
        {
            if (!_globalEnabled)
                return;

            // Focus mode wins over everything: only the soloed category logs (all levels).
            if (_soloCategory != null)
            {
                if (!string.Equals(category, _soloCategory, StringComparison.Ordinal))
                    return;
            }
            else
            {
                if (level > _globalLevel)
                    return;
                if (_categoryEnabled.TryGetValue(category, out bool catEnabled) && !catEnabled)
                    return;
                if (_categoryLevels.TryGetValue(category, out LogLevel catLevel) && level > catLevel)
                    return;
            }

            var entry = new LogEntry(Time.time, category, message, level);

            if (_buffer.Count >= BufferCapacity)
                _buffer.Dequeue();
            _buffer.Enqueue(entry);

            OnLogAdded?.Invoke(entry);

            string prefix = _showTimestamp ? $"[{Time.time:F2}] " : "";
            string formatted = $"{prefix}[{category}] {message}";
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formatted, context);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted, context);
                    break;
                default:
                    Debug.Log(formatted, context);
                    break;
            }
        }

        #endregion

        #endregion
    }
}
