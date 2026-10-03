using System;
using System.Collections.Generic;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Unlocks;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Profiles, run saves and unlocks. The one place that knows where saves live and when a
    /// run starts and ends. Files sit under <c>Application.persistentDataPath</c>:
    /// <code>
    ///   profiles.idx                       profile list + last used
    ///   profiles/&lt;id&gt;/profile.sav   lifetime progress
    ///   profiles/&lt;id&gt;/run.sav       the run in progress, if any
    /// </code>
    /// A profile is created on first use, so callers never have to make one first. Only
    /// campaign runs save; test runs (<see cref="RunState.IsCampaignRun"/> false) never touch
    /// the disk.
    /// </summary>
    [Debuggable("Save", LogLevel.Info)]
    public static class SaveSystem
    {
        public const int MaxProfiles = 3;
        private const string IndexFile = "profiles.idx";
        private const string ProfileFile = "profile.sav";
        private const string RunFile = "run.sav";

        private static SaveFileStore _store;
        private static ProfileIndex _index;
        private static ProfileData _active;

        #region Setup

        private static SaveFileStore Store =>
            _store ??= new SaveFileStore(Application.persistentDataPath);

        /// <summary>
        /// Points the save system at another folder and forgets everything loaded (tests use a
        /// temporary folder). Null goes back to <c>Application.persistentDataPath</c>.
        /// </summary>
        public static void UseRoot(string root)
        {
            _store = root != null ? new SaveFileStore(root) : null;
            _index = null;
            _active = null;
        }

        /// <summary>The folder saves are written to.</summary>
        public static string Root => Store.Root;

        /// <summary>
        /// Forgets every cached profile and index so the next access re-reads the disk (after a
        /// debug tool rewrote the files). Keeps the current folder.
        /// </summary>
        public static void Reload() => UseRoot(Store.Root);

        #endregion

        #region Profiles

        private static ProfileIndex Index
        {
            get
            {
                if (_index != null)
                    return _index;
                if (!Store.TryRead(IndexFile, ProfileIndex.FromBytes, out _index, out bool backup))
                    _index = new ProfileIndex();
                else if (backup)
                    GameLogger.LogWarning("Save", "Profile index was unreadable; restored its backup.");
                return _index;
            }
        }

        /// <summary>Every profile on this machine.</summary>
        public static IReadOnlyList<ProfileIndex.Entry> Profiles => Index.Profiles;

        /// <summary>The profile in use, created as "Player 1" if there is none.</summary>
        public static ProfileData ActiveProfile
        {
            get
            {
                if (_active != null)
                    return _active;
                string id = Index.ActiveProfileId;
                if (id == null && Index.Profiles.Count > 0)
                    id = Index.Profiles[0].Id;
                _active = id != null ? LoadProfile(id) : null;
                if (_active == null)
                    _active = CreateProfile("Player 1") ?? new ProfileData { Id = "fallback" };
                return _active;
            }
        }

        /// <summary>Creates a profile and makes it active. Null when all slots are taken.</summary>
        public static ProfileData CreateProfile(string displayName)
        {
            if (Index.Profiles.Count >= MaxProfiles)
            {
                GameLogger.LogWarning("Save", $"Can't create a profile: all {MaxProfiles} slots are used.");
                return null;
            }
            long now = DateTime.UtcNow.Ticks;
            var profile = new ProfileData
            {
                Id = Guid.NewGuid().ToString("N"),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim(),
                CreatedUtcTicks = now,
                LastPlayedUtcTicks = now,
            };
            Index.Profiles.Add(new ProfileIndex.Entry { Id = profile.Id, DisplayName = profile.DisplayName });
            Index.ActiveProfileId = profile.Id;
            _active = profile;
            SaveProfile();
            GameLogger.LogInfo("Save", $"Created profile '{profile.DisplayName}'.");
            return profile;
        }

        /// <summary>Switches to another profile. Returns false if it can't be loaded.</summary>
        public static bool SelectProfile(string profileId)
        {
            var profile = LoadProfile(profileId);
            if (profile == null)
                return false;
            _active = profile;
            Index.ActiveProfileId = profileId;
            WriteIndex();
            return true;
        }

        /// <summary>Deletes a profile, its progress and its run. The next access picks another profile.</summary>
        public static void DeleteProfile(string profileId)
        {
            Index.Profiles.RemoveAll(p => p.Id == profileId);
            if (Index.ActiveProfileId == profileId)
                Index.ActiveProfileId = Index.Profiles.Count > 0 ? Index.Profiles[0].Id : null;
            if (_active != null && _active.Id == profileId)
                _active = null;
            Store.DeleteFolder(ProfileFolder(profileId));
            WriteIndex();
        }

        /// <summary>Writes the active profile (and the index) to disk.</summary>
        public static void SaveProfile()
        {
            var profile = _active;
            if (profile == null)
                return;
            profile.LastPlayedUtcTicks = DateTime.UtcNow.Ticks;
            Store.Write(ProfilePath(profile.Id), profile.ToBytes());
            WriteIndex();
        }

        private static ProfileData LoadProfile(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
                return null;
            if (!Store.TryRead(ProfilePath(profileId), ProfileData.FromBytes, out var profile, out bool backup))
                return null;
            if (backup)
                GameLogger.LogWarning("Save", $"Profile {profileId} was unreadable; restored its backup.");
            return profile;
        }

        private static void WriteIndex() => Store.Write(IndexFile, Index.ToBytes());

        internal static string ProfileFolder(string profileId) => System.IO.Path.Combine("profiles", profileId);

        internal static string ProfilePath(string profileId) =>
            System.IO.Path.Combine(ProfileFolder(profileId), ProfileFile);

        internal static string RunPath(string profileId) =>
            System.IO.Path.Combine(ProfileFolder(profileId), RunFile);

        internal static string IndexPath => IndexFile;

        internal static SaveFileStore FileStore => Store;

        /// <summary>
        /// Reads a profile's run save without starting it. False when there is none or it is
        /// unreadable (file and backup).
        /// </summary>
        public static bool TryReadRunSave(string profileId, out RunSaveData run, out bool usedBackup) =>
            Store.TryRead(RunPath(profileId), RunSaveData.FromBytes, out run, out usedBackup);

        /// <summary>Reads any profile on this machine without making it active. Null if unreadable.</summary>
        public static ProfileData PeekProfile(string profileId) => LoadProfile(profileId);

        #endregion

        #region Runs

        /// <summary>True when the active profile has a run to continue.</summary>
        public static bool HasRunInProgress => Store.Exists(RunPath(ActiveProfile.Id));

        /// <summary>
        /// Starts a fresh campaign run for the active profile, replacing any run in progress:
        /// the origin's starter deck, the profile's current unlocks, and a first save.
        /// </summary>
        public static RunState StartNewRun(
            OriginType origin,
            int seed = 0,
            int maxHours = RunState.DEFAULT_MAX_HOURS
        )
        {
            var profile = ActiveProfile;
            var cards = Resources.Load<CardDatabase>("Databases/CardDatabase");
            var deck = cards != null ? cards.GetStarterDeck(origin) : new List<CardData>();
            if (deck.Count == 0)
                GameLogger.LogWarning("Save", $"Starter deck for {origin} is empty.");

            Store.Delete(RunPath(profile.Id));
            var run = RunState.Create(origin, deck, battleQueue: null, isCampaignRun: true, maxHours: maxHours, seed: seed);
            run.SetUnlockedContent(UnlockedIds(profile, cards));

            profile.AddToCounter(ProfileCounters.RunsStarted, 1);
            SaveProfile();
            Checkpoint();
            GameLogger.LogInfo("Save", $"New {origin} run, seed {run.Seed}, {run.UnlockedContent.Count} unlock(s).");
            return run;
        }

        /// <summary>
        /// Loads the active profile's run in progress and makes it <see cref="RunState.Current"/>.
        /// Encounters resolve through <paramref name="pool"/>. Null when there is no readable run.
        /// <paramref name="openEvent"/> is an event that was open, unanswered, when the run was
        /// saved. A pending battle stays on <see cref="RunState.PendingBattle"/> to restart.
        /// </summary>
        public static RunState ContinueRun(EncounterPoolData pool, out EventEncounterData openEvent)
        {
            openEvent = null;
            string path = RunPath(ActiveProfile.Id);
            if (!Store.TryRead(path, RunSaveData.FromBytes, out var data, out bool backup))
            {
                if (Store.Exists(path))
                    GameLogger.LogError("Save", "The run in progress is unreadable (file and backup).");
                return null;
            }
            if (backup)
                GameLogger.LogWarning("Save", "Run save was unreadable; continued from its backup.");

            var run = RunState.Restore(data, SaveContent.Load(pool), out openEvent, out var missing);
            if (missing.Count > 0)
                GameLogger.LogWarning("Save", $"Dropped content that no longer exists: {string.Join(", ", missing)}");
            GameLogger.LogInfo("Save", $"Continued a {run.Origin} run on day {run.Day}.");
            return run;
        }

        /// <summary>
        /// Saves the current run if it's a campaign run. Call whenever the campaign settles:
        /// back on the map, after a choice, before a battle. <paramref name="openEventId"/> is an
        /// event on screen that hasn't been answered yet.
        /// </summary>
        public static void Checkpoint(string openEventId = null)
        {
            var run = RunState.Current;
            if (run == null || !run.IsCampaignRun)
                return;
            try
            {
                Store.Write(RunPath(ActiveProfile.Id), run.ToSaveData(openEventId).ToBytes());
            }
            catch (Exception e)
            {
                GameLogger.LogError("Save", $"Run checkpoint failed: {e.Message}");
            }
        }

        /// <summary>
        /// Ends the current campaign run: folds its results into the profile's counters, saves
        /// the profile, deletes the run save and clears <see cref="RunState.Current"/>. Returns
        /// the cards this run unlocked (empty if none).
        /// </summary>
        public static List<CardData> EndRun(bool victory)
        {
            var run = RunState.Current;
            var unlocked = new List<CardData>();
            if (run == null)
                return unlocked;
            if (!run.IsCampaignRun)
            {
                RunState.Clear();
                return unlocked;
            }

            var profile = ActiveProfile;
            var cards = Resources.Load<CardDatabase>("Databases/CardDatabase");
            var before = new HashSet<string>(UnlockedIds(profile, cards));

            if (victory)
            {
                profile.AddToCounter(ProfileCounters.RunsWon, 1);
                profile.AddToCounter(ProfileCounters.RunsWonAs(run.Origin), 1);
            }
            else
                profile.AddToCounter(ProfileCounters.RunsLost, 1);
            foreach (var pair in run.RunCounters)
                profile.AddToCounter(pair.Key, pair.Value);
            profile.RaiseCounterTo(ProfileCounters.HighestDay, run.Day);

            foreach (var id in UnlockedIds(profile, cards))
                if (!before.Contains(id) && cards.GetByID(id) is CardData card)
                    unlocked.Add(card);

            SaveProfile();
            Store.Delete(RunPath(profile.Id));
            RunState.Clear();
            GameLogger.LogInfo(
                "Save",
                $"Run ended ({(victory ? "won" : "lost")}). Unlocked: {(unlocked.Count == 0 ? "nothing" : string.Join(", ", unlocked.ConvertAll(c => c.CardName)))}."
            );
            return unlocked;
        }

        /// <summary>Throws away the run in progress without counting it.</summary>
        public static void AbandonRun()
        {
            Store.Delete(RunPath(ActiveProfile.Id));
            RunState.Clear();
        }

        #endregion

        #region Unlocks

        /// <summary>One locked-by-default card and where the active profile stands on it.</summary>
        public struct UnlockStatus
        {
            public CardData Card;
            public bool Unlocked;
            public bool Seen;
            public string HowToUnlock;
        }

        /// <summary>Every unlockable card, with whether the active profile has it and how to get it.</summary>
        public static List<UnlockStatus> GetUnlocks()
        {
            var profile = ActiveProfile;
            var list = new List<UnlockStatus>();
            var cards = Resources.Load<CardDatabase>("Databases/CardDatabase");
            if (cards == null)
                return list;
            foreach (var card in cards.GetUnlockableCards())
                list.Add(
                    new UnlockStatus
                    {
                        Card = card,
                        Unlocked = UnlockRules.IsUnlocked(card, profile),
                        Seen = profile.SeenUnlocks.Contains(card.ID),
                        HowToUnlock = UnlockRules.HowToUnlock(card),
                    }
                );
            return list;
        }

        /// <summary>Unlocks content for the active profile outright (a campaign event's reward).</summary>
        public static void GrantUnlock(string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
                return;
            if (ActiveProfile.GrantedUnlocks.Add(contentId))
                SaveProfile();
        }

        /// <summary>Records that the player has been shown these unlocks.</summary>
        public static void MarkUnlocksSeen(IEnumerable<string> contentIds)
        {
            bool changed = false;
            foreach (var id in contentIds)
                changed |= ActiveProfile.SeenUnlocks.Add(id);
            if (changed)
                SaveProfile();
        }

        /// <summary>Dev override: every unlockable counts as unlocked (or not) from the next run.</summary>
        public static void SetUnlockAll(bool unlockAll)
        {
            ActiveProfile.UnlockAll = unlockAll;
            SaveProfile();
        }

        private static IEnumerable<string> UnlockedIds(ProfileData profile, CardDatabase cards)
        {
            if (cards == null)
                yield break;
            foreach (var card in cards.GetUnlockableCards())
                if (UnlockRules.IsUnlocked(card, profile))
                    yield return card.ID;
        }

        #endregion
    }
}
