using System;
using System.Collections.Generic;
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
        private SaveContent _content;
        private RunSaveData _savedRun;
        private RunSaveData _liveRun;
        private bool _usedBackup;
        private bool _liveView;
        private bool _advanced;
        private bool _profileExpanded = true;
        private bool _snapshotsExpanded;
        private bool _dumpExpanded;
        private double _nextLiveRefresh;
        private readonly HashSet<string> _expanded = new HashSet<string>();

        [MenuItem("Crookedile/Save Debugger")]
        private static void Open() => GetWindow<SaveDebuggerWindow>("Save Debugger");

        private void OnEnable()
        {
            if (_pool == null)
            {
                string guid = AssetDatabase.FindAssets("t:EncounterPoolData").FirstOrDefault();
                if (guid != null)
                    _pool = AssetDatabase.LoadAssetAtPath<EncounterPoolData>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    );
            }
            RefreshDump();
        }

        private void RefreshDump()
        {
            SaveSystem.Reload();
            _dump = SaveDebug.Describe(_pool);
            _content = SaveContent.Load(_pool);
            _savedRun = null;
            _usedBackup = false;
            if (SaveSystem.Profiles.Count > 0)
                SaveSystem.TryReadRunSave(
                    SaveSystem.ActiveProfile.Id,
                    out _savedRun,
                    out _usedBackup
                );
            CaptureLiveRun();
        }

        private void CaptureLiveRun()
        {
            _liveRun = Application.isPlaying ? RunState.Current?.ToSaveData() : null;
        }

        private void OnInspectorUpdate()
        {
            if (!_liveView || EditorApplication.timeSinceStartup < _nextLiveRefresh)
                return;

            _nextLiveRefresh = EditorApplication.timeSinceStartup + 0.5;
            CaptureLiveRun();
            Repaint();
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
                _profileExpanded = EditorGUILayout.Foldout(
                    _profileExpanded,
                    "Profile details",
                    true
                );
                if (_profileExpanded)
                    DrawActiveProfile();
            }
            EditorGUILayout.Space();
            DrawRun();
            EditorGUILayout.Space();
            _snapshotsExpanded = EditorGUILayout.Foldout(_snapshotsExpanded, "Snapshots", true);
            if (_snapshotsExpanded)
                DrawSnapshots();
            EditorGUILayout.Space();
            _dumpExpanded = EditorGUILayout.Foldout(_dumpExpanded, "Readable disk dump", true);
            if (_dumpExpanded)
                DrawDump();
            EditorGUILayout.Space();
            _advanced = EditorGUILayout.Foldout(_advanced, "Advanced", true);
            if (_advanced)
                DrawAdvanced();

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
            EditorGUI.BeginChangeCheck();
            _pool = (EncounterPoolData)
                EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Encounter pool",
                        "Resolves encounter IDs to names in the dump."
                    ),
                    _pool,
                    typeof(EncounterPoolData),
                    false
                );
            if (EditorGUI.EndChangeCheck())
                RefreshDump();
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
                    EditorGUILayout.LabelField(
                        (isActive ? "▶ " : "   ") + entry.DisplayName,
                        entry.Id
                    );
                    using (new EditorGUI.DisabledScope(isActive))
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            SaveSystem.SelectProfile(entry.Id);
                            RefreshDump();
                        }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _newProfileName = EditorGUILayout.TextField("New profile", _newProfileName);
                using (
                    new EditorGUI.DisabledScope(SaveSystem.Profiles.Count >= SaveSystem.MaxProfiles)
                )
                    if (GUILayout.Button("Create", GUILayout.Width(60)))
                    {
                        SaveSystem.CreateProfile(_newProfileName);
                        RefreshDump();
                    }
            }
        }

        private void DrawActiveProfile()
        {
            var profile = SaveSystem.ActiveProfile;
            EditorGUILayout.LabelField(
                $"Active profile: {profile.DisplayName}",
                EditorStyles.boldLabel
            );
            EditorGUILayout.LabelField("Created", FormatTime(profile.CreatedUtcTicks));
            EditorGUILayout.LabelField("Last played", FormatTime(profile.LastPlayedUtcTicks));

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

            EditorGUILayout.LabelField(
                "Unlockable cards (toggle = explicit grant)",
                EditorStyles.miniBoldLabel
            );
            _unlockFilter = EditorGUILayout.TextField("Filter", _unlockFilter);
            foreach (var status in SaveSystem.GetUnlocks())
            {
                if (
                    !string.IsNullOrEmpty(_unlockFilter)
                    && status.Card.CardName.IndexOf(
                        _unlockFilter,
                        System.StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool granted = profile.GrantedUnlocks.Contains(status.Card.ID);
                    bool wantGranted = EditorGUILayout.ToggleLeft(
                        $"{(status.Unlocked ? "[unlocked]" : "[locked]")} {status.Card.CardName}",
                        granted,
                        GUILayout.Width(280)
                    );
                    if (GUILayout.Button("Inspect", GUILayout.Width(60)))
                        Selection.activeObject = status.Card;
                    EditorGUILayout.LabelField(
                        status.HowToUnlock + (status.Seen ? "  (seen)" : "")
                    );
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
            EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);
            bool live =
                GUILayout.Toolbar(_liveView ? 1 : 0, new[] { "Saved checkpoint", "Live run" }) == 1;
            if (live != _liveView)
            {
                _liveView = live;
                CaptureLiveRun();
            }
            var run = _liveView ? _liveRun : _savedRun;
            EditorGUILayout.LabelField(
                "Last checkpoint",
                _savedRun == null ? "None" : FormatTime(_savedRun.SavedUtcTicks)
            );
            if (_liveView)
                EditorGUILayout.HelpBox(
                    "Live campaign state refreshes twice a second. Combat turns are not saved. The live run may differ from the selected profile's checkpoint.",
                    MessageType.Info
                );
            else if (_usedBackup)
                EditorGUILayout.HelpBox(
                    "Showing the backup because the primary run save could not be read.",
                    MessageType.Warning
                );

            using (
                new EditorGUI.DisabledScope(
                    !Application.isPlaying
                        || RunState.Current == null
                        || !RunState.Current.IsCampaignRun
                )
            )
                if (
                    GUILayout.Button(
                        new GUIContent("Checkpoint now", "Save the current campaign run to disk.")
                    )
                )
                {
                    SaveSystem.Checkpoint();
                    RefreshDump();
                }
            if (run == null)
            {
                EditorGUILayout.HelpBox(
                    _liveView
                        ? "No live run. Enter Play mode and start or continue a run."
                        : "No readable saved run for this profile.",
                    MessageType.Info
                );
                return;
            }
            EditorGUILayout.LabelField(
                _liveView ? "Captured" : "Saved",
                FormatTime(run.SavedUtcTicks)
            );
            EditorGUILayout.LabelField("Origin", ((OriginType)run.Origin).ToString());
            EditorGUILayout.LabelField("Campaign run", run.IsCampaignRun ? "Yes" : "No (test run)");
            EditorGUILayout.LabelField("Seed", run.Seed.ToString());
            EditorGUILayout.LabelField("Day", run.Day.ToString());
            EditorGUILayout.LabelField(
                "Time remaining",
                $"{run.MinutesRemaining / 60}h {run.MinutesRemaining % 60}m"
            );
            EditorGUILayout.LabelField(
                "Elapsed / maximum hours",
                $"{run.ElapsedMinutes} minutes / {run.MaxHours} hours"
            );
            EditorGUILayout.LabelField("District", run.DistrictName ?? "None");
            EditorGUILayout.LabelField("Funds / credibility", $"{run.Funds} / {run.Credibility}");
            EditorGUILayout.LabelField("Next-battle hostility", run.NextBattleHostility.ToString());

            if (Expanded("Deck", run.Deck.Count))
                foreach (var group in run.Deck.GroupBy(card => (card.Id, card.Upgraded)))
                    DrawReference(
                        $"{group.Count()}x{(group.Key.Upgraded ? " upgraded" : "")}",
                        group.Key.Id,
                        _content.Card(group.Key.Id)
                    );
            DrawReferences("Allies", run.AllyIds, id => _content.Ally(id));
            if (Expanded("Flags", run.Flags.Count))
                foreach (string flag in run.Flags)
                    EditorGUILayout.SelectableLabel(
                        flag,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)
                    );
            DrawReferences(
                "Visited encounters",
                run.VisitedLocationIds,
                id => _content.Encounter(id)
            );
            DrawReferences(
                $"Today's locations (day {run.TodaysLocationsDay})",
                run.TodaysLocationIds,
                id => _content.Encounter(id)
            );
            DrawReference(
                "Pending battle",
                run.PendingBattleId,
                _content.Encounter(run.PendingBattleId)
            );
            DrawReference(
                "Next encounter",
                run.NextEncounterId,
                _content.Encounter(run.NextEncounterId)
            );
            if (_liveView)
                EditorGUILayout.LabelField("Open event", "Available in saved checkpoint only");
            else
                DrawReference("Open event", run.OpenEventId, _content.Encounter(run.OpenEventId));
            EditorGUILayout.LabelField(
                "Battle index (zero-based)",
                run.CurrentBattleIndex.ToString()
            );
            if (Expanded("Battle queue", run.BattleQueue?.Count ?? 0) && run.BattleQueue != null)
                for (int i = 0; i < run.BattleQueue.Count; i++)
                    DrawReferences($"Round {i + 1}", run.BattleQueue[i], id => _content.Enemy(id));
            DrawReferences("Run unlocks", run.UnlockedContent, id => _content.Card(id));
            if (Expanded("Run counters", run.RunCounters.Count))
                foreach (var counter in run.RunCounters.OrderBy(pair => pair.Key))
                    EditorGUILayout.LabelField(counter.Key, counter.Value.ToString());
            if (Expanded("RNG state", run.RngState?.Length ?? 0))
                EditorGUILayout.SelectableLabel(
                    string.Join(" ", run.RngState ?? new uint[0]),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight)
                );
        }

        private bool Expanded(string label, int count)
        {
            bool expanded = EditorGUILayout.Foldout(
                _expanded.Contains(label),
                $"{label} ({count})",
                true
            );
            if (expanded)
                _expanded.Add(label);
            else
                _expanded.Remove(label);
            return expanded;
        }

        private void DrawReferences(
            string label,
            List<string> ids,
            Func<string, UnityEngine.Object> resolve
        )
        {
            if (!Expanded(label, ids?.Count ?? 0) || ids == null)
                return;
            foreach (string id in ids)
                DrawReference("", id, resolve(id));
        }

        private static void DrawReference(string label, string id, UnityEngine.Object asset)
        {
            if (asset != null)
                EditorGUILayout.ObjectField(
                    new GUIContent(label, id),
                    asset,
                    asset.GetType(),
                    false
                );
            else
                EditorGUILayout.LabelField(
                    label,
                    string.IsNullOrEmpty(id) ? "None" : $"Unresolved: {id}"
                );
        }

        private static string FormatTime(long ticks) =>
            ticks <= 0
                ? "Never"
                : new DateTime(ticks, DateTimeKind.Utc)
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss");

        private void DrawAdvanced()
        {
            EditorGUILayout.HelpBox(
                "These actions modify or delete save files. Snapshots can be restored from the section above.",
                MessageType.Warning
            );
            if (SaveSystem.Profiles.Count > 0)
            {
                if (
                    GUILayout.Button("Delete active profile")
                    && EditorUtility.DisplayDialog(
                        "Delete profile",
                        "Delete this profile, its progress and its run?",
                        "Delete",
                        "Cancel"
                    )
                )
                {
                    SaveSystem.DeleteProfile(SaveSystem.ActiveProfile.Id);
                    RefreshDump();
                    return;
                }
                if (
                    GUILayout.Button("Abandon saved run")
                    && EditorUtility.DisplayDialog(
                        "Abandon run",
                        "Delete the saved run without counting it?",
                        "Abandon",
                        "Cancel"
                    )
                )
                {
                    SaveSystem.AbandonRun();
                    RefreshDump();
                }
                EditorGUILayout.LabelField("Corruption tests", EditorStyles.miniBoldLabel);
                foreach (SaveDebug.SaveFile file in Enum.GetValues(typeof(SaveDebug.SaveFile)))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawCorruptButton(file, false);
                        DrawCorruptButton(file, true);
                    }
            }
            if (
                GUILayout.Button("Wipe all profiles and runs")
                && EditorUtility.DisplayDialog(
                    "Wipe saves",
                    "Delete every profile and run? Snapshots stay.",
                    "Wipe",
                    "Cancel"
                )
            )
            {
                SaveDebug.WipeAll();
                RefreshDump();
            }
        }

        private void DrawCorruptButton(SaveDebug.SaveFile file, bool backupToo)
        {
            string label = file + (backupToo ? " + backup" : "");
            if (
                GUILayout.Button(label)
                && EditorUtility.DisplayDialog(
                    "Corrupt save",
                    $"Overwrite {label} with invalid data to test recovery?",
                    "Corrupt",
                    "Cancel"
                )
            )
            {
                SaveDebug.Corrupt(file, backupToo);
                RefreshDump();
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
                    if (
                        GUILayout.Button("Restore", GUILayout.Width(70))
                        && EditorUtility.DisplayDialog(
                            "Restore snapshot",
                            $"Replace every save file with '{name}'?",
                            "Restore",
                            "Cancel"
                        )
                    )
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
