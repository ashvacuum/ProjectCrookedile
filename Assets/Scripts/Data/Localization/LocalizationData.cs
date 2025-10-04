using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data.Localization
{
    [Serializable]
    public class LocalizedString
    {
        [SerializeField] private string _key;
        [SerializeField] private string _english;
        [SerializeField] private string _tagalog;

        public string Key => _key;
        public string English => _english;
        public string Tagalog => _tagalog;

        public string GetText(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.English => _english,
                SystemLanguage.Unknown => _tagalog, // Using Filipino as Unknown for now
                _ => _english
            };
        }
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Crookedile/Localization/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [SerializeField] private List<LocalizedString> _strings = new List<LocalizedString>();

        private Dictionary<string, LocalizedString> _stringDictionary;
        private bool _isInitialized = false;

        private void OnEnable()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            _stringDictionary = new Dictionary<string, LocalizedString>();
            foreach (var localizedString in _strings)
            {
                if (!string.IsNullOrEmpty(localizedString.Key))
                {
                    _stringDictionary[localizedString.Key] = localizedString;
                }
            }

            _isInitialized = true;
        }

        public string GetString(string key, SystemLanguage language)
        {
            if (!_isInitialized) Initialize();

            if (_stringDictionary.TryGetValue(key, out LocalizedString localizedString))
            {
                return localizedString.GetText(language);
            }

            return $"[MISSING: {key}]";
        }

        public bool HasKey(string key)
        {
            if (!_isInitialized) Initialize();
            return _stringDictionary.ContainsKey(key);
        }

#if UNITY_EDITOR
        public void AddString(string key, string english, string tagalog)
        {
            LocalizedString newString = new LocalizedString();
            System.Reflection.FieldInfo keyField = typeof(LocalizedString).GetField("_key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo englishField = typeof(LocalizedString).GetField("_english", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo tagalogField = typeof(LocalizedString).GetField("_tagalog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            keyField.SetValue(newString, key);
            englishField.SetValue(newString, english);
            tagalogField.SetValue(newString, tagalog);

            _strings.Add(newString);
            _isInitialized = false;
            Initialize();
        }
#endif
    }
}
