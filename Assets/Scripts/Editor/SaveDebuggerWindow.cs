using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Save;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Inspect and edit save files without playing: profiles, the active profile's counters
    /// and unlocks, the run in progress (readable), snapshots, and corruption tests. Works in
    /// edit and play mode against the same folder the game uses.
    /// </summary>
    public class SaveDebuggerWindow : EditorWindow
    {
        private EncounterPoolData _pool;
        private Vector2 _scroll;
        private Vector2 _dumpScroll;
        private string _dump;
        private string _newProfileName = "Player";
        private string _snapshotName = "snapshot";
        private string _counterKey = ProfileCounters.RunsWon;
        private int _counterValue = 1;
        private string _unlockFilter = "";

        [MenuItem("Crookedile/Save Debugger")]
        private static void Open() => GetWindow<SaveDebuggerWindow>("Save Debugger");

        private void OnEnable()
        {
            if (_pool == null)
            {
                string guid = AssetDatabase.FindAssets("t:EncounterPoolData").FirstOrDefault();
                if (guid != null)
                    _pool = AssetDatabase.LoadAssetAtPath<EncounterPoolData>(AssetDatabase.GUIDToAssetPath(guid));
            }
            RefreshDump();
        }

        private void RefreshDump()
        {
            SaveSystem.Reload();
            _dump = SaveDebug.Describe(_pool);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            EditorGUILayout.Space();
            DrawProfiles();
            if (SaveSystem.Profiles.Count > 0)
            {
                EditorGUILayout.Space();
                DrawActiveProfile();
                EditorGUILayout.Space();
                DrawRun();
            }
            EditorGUILayout.Space();
            DrawSnapshots();
            EditorGUILayout.Space();
            DrawDump();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Save folder", SaveSystem.Root);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open folder"))
                {
                    System.IO.Directory.CreateDirectory(SaveSystem.Root);
                    EditorUtility.RevealInFinder(SaveSystem.Root);
                }
                if (GUILayout.Button("Reload from disk"))
                    RefreshDump();
            }
            _pool = (EncounterPoolData)EditorGUILayout.ObjectField(
                new GUIContent("Encounter pool", "Resolves encounter IDs to names in the dump."),
                _pool,
                typeof(EncounterPoolData),
                false
            );
        }

        private void DrawProfiles()
        {
            EditorGUILayout.LabelField("Profiles", EditorStyles.boldLabel);
            string activeId = SaveSystem.Profiles.Count > 0 ? SaveSystem.ActiveProfile.Id : null;
            foreach (var entry in SaveSystem.Profiles.ToList())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isActive = entry.Id == activeId;
                    EditorGUILayout.LabelField((isActive ? "▶ " : "   ") + entry.DisplayName, entry.Id);
                    using (new EditorGUI.DisabledScope(isActive))
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SaveSystem.SelectProfile(entry.Id);
                            RefreshDump();
                        }
                    if (GUILayout.Button("Delete", GUILayout.Width(60))
                        && EditorUtility.DisplayDialog("Delete profile", $"Delete '{entry.DisplayName}', its progress and its run?", "Delete", "Cancel"))
                    {
                        SaveSystem.DeleteProfile(entry.Id);
                        RefreshDump();
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _newProfileName = EditorGUILayout.TextField("New profile", _newProfileName);
                using (new EditorGUI.DisabledScope(SaveSystem.Profiles.Count >= SaveSystem.MaxProfiles))
                    if (GUILayout.Button("Create", GUILayout.Width(60)))
                    {
                        SaveSystem.CreateProfile(_newProfileName);
                        RefreshDump();
                    }
            }
            if (GUILayout.Button("Wipe all profiles and runs")
                && EditorUtility.DisplayDialog("Wipe saves", "Delete every profile and run? Snapshots stay.", "Wipe", "Cancel"))
            {
                SaveDebug.WipeAll();
                RefreshDump();
            }
        }

        private void DrawActiveProfile()
        {
            var profile = SaveSystem.ActiveProfile;
            EditorGUILayout.LabelField($"Active profile: {profile.DisplayName}", EditorStyles.boldLabel);

            bool unlockAll = EditorGUILayout.Toggle("Unlock-all override", profile.UnlockAll);
            if (unlockAll != profile.UnlockAll)
            {
                SaveSystem.SetUnlockAll(unlockAll);
                RefreshDump();
            }

            EditorGUILayout.LabelField("Counters", EditorStyles.miniBoldLabel);
            foreach (var pair in profile.Counters.OrderBy(p => p.Key).ToList())
            {
                int value = EditorGUILayout.DelayedIntField(pair.Key, pair.Value);
                if (value != pair.Value)
                {
                    SaveDebug.SetCounter(pair.Key, value);
                    RefreshDump();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                var keys = ProfileCounters.All().ToArray();
                int index = Mathf.Max(0, System.Array.IndexOf(keys, _counterKey));
                _counterKey = keys[EditorGUILayout.Popup(index, keys)];
                _counterValue = EditorGUILayout.IntField(_counterValue, GUILayout.Width(60));
                if (GUILayout.Button("Set", GUILayout.Width(60)))
                {
                    SaveDebug.SetCounter(_counterKey, _counterValue);
                    RefreshDump();
                }
            }

            EditorGUILayout.LabelField("Unlockable cards (toggle = explicit grant)", EditorStyles.miniBoldLabel);
            _unlockFilter = EditorGUILayout.TextField("Filter", _unlockFilter);
            foreach (var status in SaveSystem.GetUnlocks())
            {
                if (!string.IsNullOrEmpty(_unlockFilter)
                    && status.Card.CardName.IndexOf(_unlockFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool granted = profile.GrantedUnlocks.Contains(status.Card.ID);
                    bool wantGranted = EditorGUILayout.ToggleLeft(
                        $"{(status.Unlocked ? "[unlocked]" : "[locked]")} {status.Card.CardName}",
                        granted,
                        GUILayout.Width(280)
                    );
                    EditorGUILayout.LabelField(status.HowToUnlock + (status.Seen ? "  (seen)" : ""));
                    if (wantGranted != granted)
                    {
                        if (wantGranted)
                            SaveSystem.GrantUnlock(status.Card.ID);
                        else
                            SaveDebug.RevokeUnlock(status.Card.ID);
                        RefreshDump();
                    }
                }
            }
            if (GUILayout.Button("Forget which unlocks were shown"))
            {
                SaveDebug.ClearSeenUnlocks();
                RefreshDump();
            }
        }

        private void DrawRun()
        {
            EditorGUILayout.LabelField("Run in progress", EditorStyles.boldLabel);
            bool hasRun = SaveSystem.HasRunInProgress;
            EditorGUILayout.LabelField(hasRun ? "Saved run on disk (details in the dump below)." : "No saved run.");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!Application.isPlaying || RunState.Current == null))
                    if (GUILayout.Button(new GUIContent("Checkpoint now", "Play mode: save the run in memory now.")))
                    {
                        SaveSystem.Checkpoint();
                        RefreshDump();
                    }
                using (new EditorGUI.DisabledScope(!hasRun))
                    if (GUILayout.Button("Abandon run")
                        && EditorUtility.DisplayDialog("Abandon run", "Delete the saved run without counting it?", "Abandon", "Cancel"))
                    {
                        SaveSystem.AbandonRun();
                        RefreshDump();
                    }
            }

            EditorGUILayout.LabelField("Corrupt a file (tests the backup fallback)", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (SaveDebug.SaveFile file in System.Enum.GetValues(typeof(SaveDebug.SaveFile)))
                {
                    if (GUILayout.Button(file.ToString()))
                    {
                        SaveDebug.Corrupt(file);
                        RefreshDump();
                    }
                    if (GUILayout.Button(file + " + backup"))
                    {
                        SaveDebug.Corrupt(file, backupToo: true);
                        RefreshDump();
                    }
                }
            }
        }

        private void DrawSnapshots()
        {
            EditorGUILayout.LabelField("Snapshots", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _snapshotName = EditorGUILayout.TextField("Name", _snapshotName);
                if (GUILayout.Button("Save snapshot", GUILayout.Width(110)))
                {
                    SaveDebug.SaveSnapshot(_snapshotName);
                    RefreshDump();
                }
            }
            foreach (var name in SaveDebug.ListSnapshots())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(name);
                    if (GUILayout.Button("Restore", GUILayout.Width(70))
                        && EditorUtility.DisplayDialog("Restore snapshot", $"Replace every save file with '{name}'?", "Restore", "Cancel"))
                    {
                        SaveDebug.RestoreSnapshot(name);
                        RefreshDump();
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        SaveDebug.DeleteSnapshot(name);
                        RefreshDump();
                    }
                }
            }
        }

        private void DrawDump()
        {
            EditorGUILayout.LabelField("Readable dump", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh"))
                    RefreshDump();
                if (GUILayout.Button("Copy"))
                    EditorGUIUtility.systemCopyBuffer = _dump;
                if (GUILayout.Button("Export to file"))
                    EditorUtility.RevealInFinder(SaveDebug.ExportDump(_pool));
            }
            _dumpScroll = EditorGUILayout.BeginScrollView(_dumpScroll, GUILayout.MinHeight(300));
            EditorGUILayout.TextArea(_dump ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }
}
