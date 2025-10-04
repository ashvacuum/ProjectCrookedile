using UnityEngine;
using Crookedile.Core;
using Crookedile.Data.Localization;
using Crookedile.Utilities;

namespace Crookedile.Managers
{
    [Debuggable("Localization", LogLevel.Info)]
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [SerializeField] private LocalizationData _localizationData;
        [SerializeField] private SystemLanguage _currentLanguage = SystemLanguage.English;

        public SystemLanguage CurrentLanguage => _currentLanguage;

        protected override void OnAwake()
        {
            if (_localizationData == null)
            {
                GameLogger.LogError("Localization", "LocalizationData not assigned!");
            }

            // Load saved language preference
            string savedLanguage = PlayerPrefs.GetString("Language", SystemLanguage.English.ToString());
            if (System.Enum.TryParse(savedLanguage, out SystemLanguage language))
            {
                _currentLanguage = language;
            }

            GameLogger.LogInfo("Localization", $"Language set to: {_currentLanguage}");
        }

        public string GetString(string key)
        {
            if (_localizationData == null)
            {
                GameLogger.LogError("Localization", "LocalizationData is null!");
                return $"[ERROR: {key}]";
            }

            return _localizationData.GetString(key, _currentLanguage);
        }

        public string GetString(string key, params object[] args)
        {
            string baseString = GetString(key);
            try
            {
                return string.Format(baseString, args);
            }
            catch (System.Exception e)
            {
                GameLogger.LogError("Localization", $"Failed to format string '{key}': {e.Message}");
                return baseString;
            }
        }

        public void SetLanguage(SystemLanguage language)
        {
            _currentLanguage = language;
            PlayerPrefs.SetString("Language", language.ToString());
            PlayerPrefs.Save();

            GameLogger.LogInfo("Localization", $"Language changed to: {_currentLanguage}");

            // Trigger language change event
            EventBus.Publish(new LanguageChangedEvent { NewLanguage = language });
        }

        public bool HasKey(string key)
        {
            if (_localizationData == null) return false;
            return _localizationData.HasKey(key);
        }
    }

    // Event for language changes
    public class LanguageChangedEvent : IGameEvent
    {
        public SystemLanguage NewLanguage;
    }

    // Static helper for easy access
    public static class Localization
    {
        public static string Get(string key)
        {
            return LocalizationManager.Instance?.GetString(key) ?? $"[NO MANAGER: {key}]";
        }

        public static string Get(string key, params object[] args)
        {
            return LocalizationManager.Instance?.GetString(key, args) ?? $"[NO MANAGER: {key}]";
        }
    }
}
