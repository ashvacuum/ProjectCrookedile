using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Save;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor
{
    /// <summary>
    /// Inspect and edit save files without playing: profiles, the active profile's counters
    /// and unlocks, the run in progress (readable), snapshots, and corruption tests. Works in
    /// edit and play mode against the same folder the game uses. Odin draws it: the save folder
    /// and encounter pool on top, then a tab per area. Every view reads the rows
    /// <see cref="RefreshDump"/> builds; every action calls <see cref="SaveSystem"/> or
    /// <see cref="SaveDebug"/> and then refreshes.
    /// </summary>
    /// <remarks>
    /// Odin draws fields before properties before methods unless told otherwise, so members
    /// carry <c>PropertyOrder</c> and groups an order, and each tab's first field is declared in
    /// tab order.
    /// </remarks>
    public class SaveDebuggerWindow : OdinEditorWindow
    {
        private const string Tabs = "Tabs";
        private const string ProfilesTab = Tabs + "/Profiles";
        private const string ActiveBox = ProfilesTab + "/Active profile";
        private const string SnapshotsTab = Tabs + "/Snapshots";
        private const string DumpTab = Tabs + "/Dump";
        private const string DangerTab = Tabs + "/Danger zone";

        private enum RunSource
        {
            [LabelText("Saved checkpoint")]
            SavedCheckpoint,

            [LabelText("Live run")]
            LiveRun,
        }

        private SaveContent _content;
        private ProfileData _profile;
        private RunSaveData _savedRun;
        private RunSaveData _liveRun;
        private bool _usedBackup;
        private double _nextLiveRefresh;
        private Vector2 _dumpScroll;

        [MenuItem("Crookedile/Save Debugger")]
        private static void Open() => GetWindow<SaveDebuggerWindow>("Save Debugger");

        #region Header

        [ShowInInspector, DisplayAsString, LabelText("Save folder"), PropertyOrder(-30)]
        private string SaveFolder => SaveSystem.Root;

        [Tooltip("Resolves encounter IDs to names in the Run tab and the dump.")]
        [SerializeField, LabelText("Encounter pool"), PropertyOrder(-10)]
        [OnValueChanged(nameof(RefreshDump))]
        private EncounterPoolData _pool;

        [ButtonGroup("Folder", -20), Button("Open folder")]
        private void OpenFolder()
        {
            System.IO.Directory.CreateDirectory(SaveSystem.Root);
            EditorUtility.RevealInFinder(SaveSystem.Root);
        }

        [ButtonGroup("Folder"), Button("Reload from disk")]
        private void ReloadFromDisk() => RefreshDump();

        #endregion

        #region Profiles tab

        [TabGroup(Tabs, "Profiles"), PropertyOrder(1)]
        [InfoBox("No profiles on disk. Create one below.", VisibleIf = "@!HasProfile")]
        [
            ShowInInspector,
            TableList(IsReadOnly = true, AlwaysExpanded = true),
            LabelText("Profiles")
        ]
        private List<ProfileRow> _profiles = new List<ProfileRow>();

        [Tooltip("Display name for the profile Create makes.")]
        [HorizontalGroup(ProfilesTab + "/New", order: 2), SerializeField, LabelText("New profile")]
        private string _newProfileName = "Player";

        [ShowInInspector, PropertyOrder(4), ShowIf(nameof(HasProfile))]
        [BoxGroup(ActiveBox), TableList(IsReadOnly = true, AlwaysExpanded = true)]
        [
            LabelText("Counters"),
            PropertyTooltip("Edit a value and press Enter to save it. 0 removes the counter.")
        ]
        private List<CounterRow> _counters = new List<CounterRow>();

        [Tooltip("Counter to set on the active profile.")]
        [HorizontalGroup(ActiveBox + "/Set", order: 5), ShowIf(nameof(HasProfile))]
        [SerializeField, ValueDropdown(nameof(CounterKeys)), LabelText("Set counter")]
        private string _counterKey = ProfileCounters.RunsWon;

        [Tooltip("Value to set. 0 removes the counter.")]
        [HorizontalGroup(ActiveBox + "/Set", 80), ShowIf(nameof(HasProfile))]
        [SerializeField, HideLabel]
        private int _counterValue = 1;

        private bool HasProfile => _profile != null;
        private bool CanCreateProfile => SaveSystem.Profiles.Count < SaveSystem.MaxProfiles;
        private static IEnumerable<string> CounterKeys => ProfileCounters.All();

        [BoxGroup(ActiveBox, order: 3), ShowIf(nameof(HasProfile)), PropertyOrder(0)]
        [ShowInInspector, DisplayAsString, LabelText("Name")]
        private string ProfileName => _profile?.DisplayName;

        [BoxGroup(ActiveBox), ShowIf(nameof(HasProfile)), PropertyOrder(1)]
        [ShowInInspector, DisplayAsString]
        private string Created => FormatTime(_profile?.CreatedUtcTicks ?? 0);

        [BoxGroup(ActiveBox), ShowIf(nameof(HasProfile)), PropertyOrder(2)]
        [ShowInInspector, DisplayAsString, LabelText("Last played")]
        private string LastPlayed => FormatTime(_profile?.LastPlayedUtcTicks ?? 0);

        [BoxGroup(ActiveBox), ShowIf(nameof(HasProfile)), PropertyOrder(3)]
        [ShowInInspector, LabelText("Unlock-all override")]
        [PropertyTooltip("Every unlockable counts as unlocked from the next run.")]
        private bool UnlockAll
        {
            get => _profile != null && _profile.UnlockAll;
            set
            {
                SaveSystem.SetUnlockAll(value);
                RefreshDump();
            }
        }

        [
            HorizontalGroup(ProfilesTab + "/New", 80),
            Button("Create"),
            EnableIf(nameof(CanCreateProfile))
        ]
        private void CreateProfile()
        {
            SaveSystem.CreateProfile(_newProfileName);
            RefreshDump();
        }

        [HorizontalGroup(ActiveBox + "/Set", 60), ShowIf(nameof(HasProfile)), Button("Set")]
        private void SetCounterFromRow()
        {
            SaveDebug.SetCounter(_counterKey, _counterValue);
            RefreshDump();
        }

        private void SelectProfile(string id)
        {
            SaveSystem.SelectProfile(id);
            RefreshDump();
        }

        private void SetCounter(string key, int value)
        {
            SaveDebug.SetCounter(key, value);
            RefreshDump();
        }

        #endregion

        #region Unlocks tab

        [Tooltip("Shows only cards whose name contains this text.")]
        [TabGroup(Tabs, "Unlocks"), PropertyOrder(100), ShowIf(nameof(HasProfile))]
        [SerializeField, LabelText("Filter"), OnValueChanged(nameof(BuildUnlocks))]
        [InfoBox(
            "Granted is the explicit grant: tick to grant, untick to revoke it and its seen mark. "
                + "A card unlocked by its condition stays unlocked until the counters behind it change."
        )]
        private string _unlockFilter = "";

        [TabGroup(Tabs, "Unlocks"), PropertyOrder(101), ShowIf(nameof(HasProfile))]
        [
            ShowInInspector,
            TableList(IsReadOnly = true, AlwaysExpanded = true),
            LabelText("Unlockable cards")
        ]
        private List<UnlockRow> _unlocks = new List<UnlockRow>();

        [TabGroup(Tabs, "Unlocks"), PropertyOrder(102), ShowIf(nameof(HasProfile))]
        [Button("Forget which unlocks were shown")]
        private void ClearSeenUnlocks()
        {
            SaveDebug.ClearSeenUnlocks();
            RefreshDump();
        }

        [TabGroup(Tabs, "Unlocks"), PropertyOrder(103), HideIf(nameof(HasProfile))]
        [ShowInInspector, DisplayAsString, HideLabel]
        private string NoProfileForUnlocks => "No profile. Create one in the Profiles tab.";

        private void BuildUnlocks()
        {
            if (_profile == null)
            {
                _unlocks = new List<UnlockRow>();
                return;
            }
            _unlocks = SaveSystem
                .GetUnlocks()
                .Where(status =>
                    string.IsNullOrEmpty(_unlockFilter)
                    || status.Card.CardName.IndexOf(
                        _unlockFilter,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                .Select(status => new UnlockRow(
                    status,
                    _profile.GrantedUnlocks.Contains(status.Card.ID),
                    SetGranted
                ))
                .ToList();
        }

        private void SetGranted(CardData card, bool granted)
        {
            if (granted)
                SaveSystem.GrantUnlock(card.ID);
            else
                SaveDebug.RevokeUnlock(card.ID);
            RefreshDump();
        }

        #endregion

        #region Run tab

        [Tooltip(
            "Saved checkpoint reads the run file on disk; Live run reads the run in memory in Play mode."
        )]
        [TabGroup(Tabs, "Run"), PropertyOrder(200)]
        [SerializeField, EnumToggleButtons, HideLabel, OnValueChanged(nameof(CaptureLiveRun))]
        private RunSource _runSource = RunSource.SavedCheckpoint;

        [TabGroup(Tabs, "Run"), PropertyOrder(203)]
        [ShowInInspector, HideLabel, InlineProperty, HideReferenceObjectPicker]
        [ShowIf("@_runReadout != null")]
        private RunReadout _runReadout;

        private bool Live => _runSource == RunSource.LiveRun;

        private bool CanCheckpoint =>
            Application.isPlaying && RunState.Current != null && RunState.Current.IsCampaignRun;

        [TabGroup(Tabs, "Run"), PropertyOrder(201), Button("Checkpoint now")]
        [EnableIf(nameof(CanCheckpoint)), PropertyTooltip("Save the current campaign run to disk.")]
        private void CheckpointNow()
        {
            SaveSystem.Checkpoint();
            RefreshDump();
        }

        [TabGroup(Tabs, "Run"), PropertyOrder(202)]
        [ShowInInspector, DisplayAsString, LabelText("Last checkpoint")]
        [InfoBox(
            "Live campaign state refreshes twice a second. Combat turns are not saved. "
                + "The live run may differ from the selected profile's checkpoint.",
            VisibleIf = nameof(Live)
        )]
        [InfoBox(
            "Showing the backup because the primary run save could not be read.",
            InfoMessageType.Warning,
            VisibleIf = "@!Live && _usedBackup"
        )]
        [InfoBox(
            "No live run. Enter Play mode and start or continue a run.",
            VisibleIf = "@Live && _runReadout == null"
        )]
        [InfoBox(
            "No readable saved run for this profile.",
            VisibleIf = "@!Live && _runReadout == null"
        )]
        private string LastCheckpoint =>
            _savedRun == null ? "None" : FormatTime(_savedRun.SavedUtcTicks);

        private void CaptureLiveRun()
        {
            _liveRun = Application.isPlaying ? RunState.Current?.ToSaveData() : null;
            var run = Live ? _liveRun : _savedRun;
            _runReadout = run == null ? null : new RunReadout(run, _content, Live);
        }

        private void OnInspectorUpdate()
        {
            if (!Live || EditorApplication.timeSinceStartup < _nextLiveRefresh)
                return;

            _nextLiveRefresh = EditorApplication.timeSinceStartup + 0.5;
            CaptureLiveRun();
            Repaint();
        }

        #endregion

        #region Snapshots tab

        [Tooltip("Folder name for the snapshot. Saving over an existing name replaces it.")]
        [HorizontalGroup(SnapshotsTab + "/Save", order: 300), SerializeField, LabelText("Name")]
        private string _snapshotName = "snapshot";

        [TabGroup(Tabs, "Snapshots"), PropertyOrder(301)]
        [InfoBox("No snapshots yet.", VisibleIf = "@_snapshots.Count == 0")]
        [
            ShowInInspector,
            TableList(IsReadOnly = true, AlwaysExpanded = true),
            LabelText("Snapshots (oldest first)")
        ]
        private List<SnapshotRow> _snapshots = new List<SnapshotRow>();

        [HorizontalGroup(SnapshotsTab + "/Save", 110), Button("Save snapshot")]
        [PropertyTooltip("Checkpoint the run, then copy every save file into the named snapshot.")]
        private void SaveSnapshot()
        {
            SaveDebug.SaveSnapshot(_snapshotName);
            RefreshDump();
        }

        private void RestoreSnapshot(string name)
        {
            if (!Confirm("Restore snapshot", $"Replace every save file with '{name}'?", "Restore"))
                return;
            SaveDebug.RestoreSnapshot(name);
            RefreshDump();
        }

        private void DeleteSnapshot(string name)
        {
            if (!Confirm("Delete snapshot", $"Delete the snapshot '{name}'?", "Delete"))
                return;
            SaveDebug.DeleteSnapshot(name);
            RefreshDump();
        }

        #endregion

        #region Dump tab

        [TabGroup(Tabs, "Dump"), PropertyOrder(401)]
        [ShowInInspector, HideLabel, CustomValueDrawer(nameof(DrawDump))]
        private string _dump;

        [ButtonGroup(DumpTab + "/Actions", 400), Button("Refresh")]
        private void RefreshFromDump() => RefreshDump();

        [ButtonGroup(DumpTab + "/Actions"), Button("Copy")]
        private void CopyDump() => EditorGUIUtility.systemCopyBuffer = _dump;

        [ButtonGroup(DumpTab + "/Actions"), Button("Export to file")]
        private void ExportDump() => EditorUtility.RevealInFinder(SaveDebug.ExportDump(_pool));

        // Editable only so the text can be selected; edits vanish on the next refresh.
        private string DrawDump(string value, GUIContent label)
        {
            _dumpScroll = EditorGUILayout.BeginScrollView(_dumpScroll, GUILayout.MinHeight(300));
            EditorGUILayout.TextArea(value ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            return value;
        }

        #endregion

        #region Danger zone tab

        [TabGroup(Tabs, "Danger zone"), PropertyOrder(520), ShowIf(nameof(HasProfile))]
        [
            ShowInInspector,
            TableList(IsReadOnly = true, AlwaysExpanded = true),
            LabelText("Corruption tests")
        ]
        [PropertyTooltip(
            "Overwrites a file of the active profile with invalid data to exercise the backup fallback. "
                + "With backup also breaks the .bak, so the file is truly lost."
        )]
        private List<CorruptRow> _corruptRows = new List<CorruptRow>();

        [TabGroup(Tabs, "Danger zone"), PropertyOrder(500)]
        [InfoBox(
            "These actions modify or delete save files. Save a snapshot first to undo them.",
            InfoMessageType.Warning
        )]
        [ShowInInspector, DisplayAsString, LabelText("Active profile")]
        private string DangerTarget => _profile?.DisplayName ?? "None";

        [TabGroup(Tabs, "Danger zone"), PropertyOrder(510), ShowIf(nameof(HasProfile))]
        [Button("Abandon saved run", ButtonSizes.Medium), GUIColor(1f, 0.75f, 0.6f)]
        private void AbandonRun()
        {
            if (!Confirm("Abandon run", "Delete the saved run without counting it?", "Abandon"))
                return;
            SaveSystem.AbandonRun();
            RefreshDump();
        }

        [TabGroup(Tabs, "Danger zone"), PropertyOrder(530), ShowIf(nameof(HasProfile))]
        [Button("Delete active profile", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        private void DeleteActiveProfile()
        {
            if (
                !Confirm(
                    "Delete profile",
                    "Delete this profile, its progress and its run?",
                    "Delete"
                )
            )
                return;
            SaveSystem.DeleteProfile(_profile.Id);
            RefreshDump();
        }

        [TabGroup(Tabs, "Danger zone"), PropertyOrder(540)]
        [Button("Wipe all profiles and runs", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        private void WipeAll()
        {
            if (!Confirm("Wipe saves", "Delete every profile and run? Snapshots stay.", "Wipe"))
                return;
            SaveDebug.WipeAll();
            RefreshDump();
        }

        private void Corrupt(SaveDebug.SaveFile file, bool backupToo)
        {
            string label = file + (backupToo ? " + backup" : "");
            if (
                !Confirm(
                    "Corrupt save",
                    $"Overwrite {label} with invalid data to test recovery?",
                    "Corrupt"
                )
            )
                return;
            SaveDebug.Corrupt(file, backupToo);
            RefreshDump();
        }

        #endregion

        #region Refresh

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_pool == null)
            {
                string guid = AssetDatabase.FindAssets("t:EncounterPoolData").FirstOrDefault();
                if (guid != null)
                    _pool = AssetDatabase.LoadAssetAtPath<EncounterPoolData>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    );
            }
            _corruptRows = Enum.GetValues(typeof(SaveDebug.SaveFile))
                .Cast<SaveDebug.SaveFile>()
                .Select(file => new CorruptRow(file, Corrupt))
                .ToList();
            RefreshDump();
        }

        private void RefreshDump()
        {
            SaveSystem.Reload();
            _dump = SaveDebug.Describe(_pool);
            _content = SaveContent.Load(_pool);
            // Only touch ActiveProfile when a profile exists: it creates one on first access.
            _profile = SaveSystem.Profiles.Count > 0 ? SaveSystem.ActiveProfile : null;
            _savedRun = null;
            _usedBackup = false;
            if (_profile != null)
                SaveSystem.TryReadRunSave(_profile.Id, out _savedRun, out _usedBackup);

            _profiles = SaveSystem
                .Profiles.Select(entry => new ProfileRow(
                    entry,
                    entry.Id == _profile?.Id,
                    SelectProfile
                ))
                .ToList();
            _counters =
                _profile == null
                    ? new List<CounterRow>()
                    : _profile
                        .Counters.OrderBy(pair => pair.Key)
                        .Select(pair => new CounterRow(pair.Key, pair.Value, SetCounter))
                        .ToList();
            BuildUnlocks();
            _snapshots = SaveDebug
                .ListSnapshots()
                .Select(name => new SnapshotRow(name, RestoreSnapshot, DeleteSnapshot))
                .ToList();
            CaptureLiveRun();
        }

        private static bool Confirm(string title, string message, string ok) =>
            EditorUtility.DisplayDialog(title, message, ok, "Cancel");

        private static string FormatTime(long ticks) =>
            ticks <= 0
                ? "Never"
                : new DateTime(ticks, DateTimeKind.Utc)
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss");

        #endregion

        #region Rows

        private class ProfileRow
        {
            private readonly string _id;
            private readonly Action<string> _select;

            public ProfileRow(ProfileIndex.Entry entry, bool active, Action<string> select)
            {
                _id = entry.Id;
                Name = entry.DisplayName;
                Active = active;
                _select = select;
            }

            [ShowInInspector, DisplayAsString]
            public string Name { get; }

            [ShowInInspector, DisplayAsString]
            public string Id => _id;

            [ShowInInspector, TableColumnWidth(50, false)]
            public bool Active { get; }

            [Button, TableColumnWidth(70, false), DisableIf(nameof(Active))]
            private void Select() => _select(_id);
        }

        private class CounterRow
        {
            private readonly int _value;
            private readonly Action<string, int> _set;

            public CounterRow(string key, int value, Action<string, int> set)
            {
                Key = key;
                _value = value;
                _set = set;
            }

            [ShowInInspector, DisplayAsString]
            public string Key { get; }

            [ShowInInspector, DelayedProperty]
            public int Value
            {
                get => _value;
                set => _set(Key, value);
            }
        }

        private class UnlockRow
        {
            private readonly SaveSystem.UnlockStatus _status;
            private readonly bool _granted;
            private readonly Action<CardData, bool> _setGranted;

            public UnlockRow(
                SaveSystem.UnlockStatus status,
                bool granted,
                Action<CardData, bool> setGranted
            )
            {
                _status = status;
                _granted = granted;
                _setGranted = setGranted;
            }

            [ShowInInspector, TableColumnWidth(60, false)]
            public bool Granted
            {
                get => _granted;
                set => _setGranted(_status.Card, value);
            }

            [ShowInInspector, DisplayAsString, TableColumnWidth(70, false)]
            public string State => _status.Unlocked ? "Unlocked" : "Locked";

            [ShowInInspector]
            public CardData Card => _status.Card;

            [ShowInInspector, DisplayAsString(false), LabelText("How to unlock")]
            public string HowToUnlock => _status.HowToUnlock;

            [ShowInInspector, TableColumnWidth(45, false)]
            public bool Seen => _status.Seen;

            [Button, TableColumnWidth(70, false)]
            private void Inspect() => Selection.activeObject = _status.Card;
        }

        private class SnapshotRow
        {
            private readonly Action<string> _restore;
            private readonly Action<string> _delete;

            public SnapshotRow(string name, Action<string> restore, Action<string> delete)
            {
                Name = name;
                _restore = restore;
                _delete = delete;
            }

            [ShowInInspector, DisplayAsString]
            public string Name { get; }

            [Button, TableColumnWidth(70, false)]
            private void Restore() => _restore(Name);

            [Button, TableColumnWidth(70, false)]
            private void Delete() => _delete(Name);
        }

        private class CorruptRow
        {
            private readonly Action<SaveDebug.SaveFile, bool> _corrupt;

            public CorruptRow(SaveDebug.SaveFile file, Action<SaveDebug.SaveFile, bool> corrupt)
            {
                File = file;
                _corrupt = corrupt;
            }

            [ShowInInspector, DisplayAsString]
            public SaveDebug.SaveFile File { get; }

            [Button("Corrupt"), TableColumnWidth(90, false)]
            private void CorruptFile() => _corrupt(File, false);

            [Button("With backup"), TableColumnWidth(100, false)]
            private void CorruptWithBackup() => _corrupt(File, true);
        }

        /// <summary>A content reference from a save: the asset when it resolves, and its ID.</summary>
        private class RefRow
        {
            public RefRow(string id, Object asset, string text = null)
            {
                Asset = asset;
                Id =
                    text
                    ?? (
                        string.IsNullOrEmpty(id) ? "None"
                        : asset == null ? $"Unresolved: {id}"
                        : id
                    );
            }

            [ShowInInspector]
            public Object Asset { get; }

            [ShowInInspector, DisplayAsString]
            public string Id { get; }
        }

        private class NotedRef : RefRow
        {
            public NotedRef(string note, string id, Object asset, string text = null)
                : base(id, asset, text) => Note = note;

            [ShowInInspector, DisplayAsString, PropertyOrder(-1), TableColumnWidth(120, false)]
            public string Note { get; }
        }

        /// <summary>One run save, read-only and readable, with content IDs resolved through the window's content.</summary>
        private class RunReadout
        {
            private readonly RunSaveData _run;
            private readonly bool _live;

            public RunReadout(RunSaveData run, SaveContent content, bool live)
            {
                _run = run;
                _live = live;
                Deck = run
                    .Deck.GroupBy(card => (card.Id, card.Upgraded))
                    .Select(group => new NotedRef(
                        $"{group.Count()}x{(group.Key.Upgraded ? " upgraded" : "")}",
                        group.Key.Id,
                        content.Card(group.Key.Id)
                    ))
                    .ToList();
                Allies = Refs(run.AllyIds, content.Ally);
                Flags = new List<string>(run.Flags);
                Encounters = new List<NotedRef>
                {
                    new NotedRef(
                        "Pending battle",
                        run.PendingBattleId,
                        content.Encounter(run.PendingBattleId)
                    ),
                    new NotedRef(
                        "Next encounter",
                        run.NextEncounterId,
                        content.Encounter(run.NextEncounterId)
                    ),
                    live
                        ? new NotedRef(
                            "Open event",
                            null,
                            null,
                            "Available in saved checkpoint only"
                        )
                        : new NotedRef(
                            "Open event",
                            run.OpenEventId,
                            content.Encounter(run.OpenEventId)
                        ),
                };
                Visited = Refs(run.VisitedLocationIds, content.Encounter);
                TodaysLocations = Refs(run.TodaysLocationIds, content.Encounter);
                // ponytail: flattened to one row per enemy, so an empty round has no row.
                BattleQueue = (run.BattleQueue ?? new List<List<string>>())
                    .SelectMany(
                        (round, i) =>
                            (round ?? new List<string>()).Select(id => new NotedRef(
                                $"Round {i + 1}",
                                id,
                                content.Enemy(id)
                            ))
                    )
                    .ToList();
                RunUnlocks = Refs(run.UnlockedContent, content.Card);
                RunCounters = run
                    .RunCounters.OrderBy(pair => pair.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            private static List<RefRow> Refs(List<string> ids, Func<string, Object> resolve) =>
                (ids ?? new List<string>()).Select(id => new RefRow(id, resolve(id))).ToList();

            private string SavedLabel => _live ? "Captured" : "Saved";
            private string TodaysLabel => $"Today's locations (day {_run.TodaysLocationsDay})";

            [ShowInInspector, DisplayAsString, LabelText("$SavedLabel")]
            public string Saved => FormatTime(_run.SavedUtcTicks);

            [ShowInInspector, DisplayAsString]
            public string Origin => ((OriginType)_run.Origin).ToString();

            [ShowInInspector, DisplayAsString, LabelText("Campaign run")]
            public string CampaignRun => _run.IsCampaignRun ? "Yes" : "No (test run)";

            [ShowInInspector, DisplayAsString]
            public int Seed => _run.Seed;

            [ShowInInspector, DisplayAsString]
            public int Day => _run.Day;

            [ShowInInspector, DisplayAsString, LabelText("Time remaining")]
            public string TimeRemaining =>
                $"{_run.MinutesRemaining / 60}h {_run.MinutesRemaining % 60}m";

            [ShowInInspector, DisplayAsString, LabelText("Elapsed / maximum")]
            public string Elapsed => $"{_run.ElapsedMinutes} minutes / {_run.MaxHours} hours";

            [ShowInInspector, DisplayAsString]
            public string District => _run.DistrictName ?? "None";

            [ShowInInspector, DisplayAsString]
            public int Funds => _run.Funds;

            [ShowInInspector, DisplayAsString]
            public string Credibility =>
                $"{_run.Credibility} (max bonus {_run.MaxCredibilityBonus:+0;-0;+0})";

            [ShowInInspector, DisplayAsString, LabelText("Next-battle hostility")]
            public int NextBattleHostility => _run.NextBattleHostility;

            [ShowInInspector, DisplayAsString, LabelText("Cards since rare")]
            public int CardsSinceRare => _run.CardsSinceRare;

            [ShowInInspector, DisplayAsString, LabelText("Battle round")]
            public string BattleRound =>
                $"{_run.CurrentBattleIndex + 1} of {_run.BattleQueue?.Count ?? 0} (index {_run.CurrentBattleIndex})";

            [ShowInInspector, TableList(IsReadOnly = true)]
            public List<NotedRef> Deck { get; }

            [ShowInInspector, TableList(IsReadOnly = true)]
            public List<RefRow> Allies { get; }

            // List elements stay editable so a flag can be selected and copied; edits touch this copy only.
            [ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
            public List<string> Flags { get; }

            [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true)]
            public List<NotedRef> Encounters { get; }

            [ShowInInspector, TableList(IsReadOnly = true), LabelText("Visited encounters")]
            public List<RefRow> Visited { get; }

            [ShowInInspector, TableList(IsReadOnly = true), LabelText("$TodaysLabel")]
            public List<RefRow> TodaysLocations { get; }

            [ShowInInspector, TableList(IsReadOnly = true), LabelText("Battle queue")]
            public List<NotedRef> BattleQueue { get; }

            [ShowInInspector, TableList(IsReadOnly = true), LabelText("Run unlocks")]
            public List<RefRow> RunUnlocks { get; }

            [ShowInInspector, ReadOnly, LabelText("Run counters")]
            [DictionaryDrawerSettings(
                IsReadOnly = true,
                KeyLabel = "Counter",
                ValueLabel = "Value"
            )]
            public Dictionary<string, int> RunCounters { get; }

            [ShowInInspector, DisplayAsString(false), LabelText("RNG state")]
            [InlineButton(nameof(CopyRng), "Copy")]
            public string RngState => string.Join(" ", _run.RngState ?? new uint[0]);

            private void CopyRng() => EditorGUIUtility.systemCopyBuffer = RngState;
        }

        #endregion
    }
}
