using UnityEditor;
using UnityEngine;
using Crookedile.Utilities;

namespace Crookedile.Editor
{
    [CustomEditor(typeof(DebugSettings))]
    public class DebugSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DebugSettings settings = (DebugSettings)target;

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Refresh Categories", GUILayout.Height(30)))
            {
                settings.RefreshCategories();
                EditorUtility.SetDirty(settings);
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Apply Settings at Runtime", GUILayout.Height(25)))
            {
                if (Application.isPlaying)
                {
                    settings.ApplySettings();
                    Debug.Log("Debug settings applied!");
                }
                else
                {
                    Debug.LogWarning("Settings can only be applied during Play mode");
                }
            }

            EditorGUILayout.Space(10);

            DrawDefaultInspector();
        }
    }

    public class DebugManagerWindow : EditorWindow
    {
        private DebugSettings _settings;
        private Vector2 _scrollPosition;
        private bool _showGlobalSettings = true;

        [MenuItem("Crookedile/Debug Manager")]
        public static void ShowWindow()
        {
            DebugManagerWindow window = GetWindow<DebugManagerWindow>("Debug Manager");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            LoadOrCreateSettings();
        }

        private void LoadOrCreateSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:DebugSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<DebugSettings>(path);
            }
            else
            {
                _settings = CreateInstance<DebugSettings>();
                AssetDatabase.CreateAsset(_settings, "Assets/DebugSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("No DebugSettings found. Creating new one...", MessageType.Info);
                if (GUILayout.Button("Create Debug Settings"))
                {
                    LoadOrCreateSettings();
                }
                return;
            }

            EditorGUILayout.Space(10);

            // Action Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Categories", GUILayout.Height(30)))
            {
                _settings.RefreshCategories();
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
            if (GUILayout.Button("Apply Settings", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    _settings.ApplySettings();
                    Debug.Log("Debug settings applied at runtime!");
                }
                else
                {
                    Debug.LogWarning("Enter Play mode to apply settings");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Global Settings
            _showGlobalSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showGlobalSettings, "Global Settings");
            if (_showGlobalSettings)
            {
                EditorGUI.indentLevel++;
                _settings.globalLoggingEnabled = EditorGUILayout.Toggle("Global Logging Enabled", _settings.globalLoggingEnabled);
                _settings.globalLogLevel = (LogLevel)EditorGUILayout.EnumPopup("Global Log Level", _settings.globalLogLevel);
                _settings.showTimestamp = EditorGUILayout.Toggle("Show Timestamp", _settings.showTimestamp);
                _settings.showStackTrace = EditorGUILayout.Toggle("Show Stack Trace", _settings.showStackTrace);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(10);

            // Categories
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _settings.categories.Count; i++)
            {
                var category = _settings.categories[i];

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                category.enabled = EditorGUILayout.Toggle(category.enabled, GUILayout.Width(20));
                EditorGUILayout.LabelField(category.categoryName, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                category.logLevel = (LogLevel)EditorGUILayout.EnumPopup("Log Level", category.logLevel);

                if (category.scripts.Count > 0)
                {
                    EditorGUILayout.LabelField($"Scripts ({category.scripts.Count}):", EditorStyles.miniLabel);
                    EditorGUI.indentLevel++;
                    foreach (string script in category.scripts)
                    {
                        EditorGUILayout.LabelField("• " + script, EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
