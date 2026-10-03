using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Unlocks;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Development tools for the save files, which are binary and can't be read by hand:
    /// readable dumps with IDs resolved to names, named snapshots of the whole save folder,
    /// direct edits to the active profile, and deliberate corruption to exercise the backup
    /// fallback. Used by the Save Debugger window and the dev console's <c>save.*</c> commands.
    /// Nothing here runs in normal play.
    /// </summary>
    [Debuggable("Save", LogLevel.Info)]
    public static class SaveDebug
    {
        private const string SnapshotFolder = "debug-snapshots";

        /// <summary>Which save file a tool acts on.</summary>
        public enum SaveFile
        {
            Run,
            Profile,
            ProfileIndex,
        }

        #region Dumps

        /// <summary>
        /// Everything on disk, readable: the save folder, every profile (active one marked) and
        /// each profile's run. Encounters resolve through <paramref name="pool"/> when given.
        /// </summary>
        public static string Describe(EncounterPoolData pool = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Save folder: {SaveSystem.Root}");
            // Only touch ActiveProfile when a profile exists: it creates one on first access.
            var active = SaveSystem.Profiles.Count > 0 ? SaveSystem.ActiveProfile : null;
            var content = SaveContent.Load(pool != null ? pool : LoadedPool());

            if (SaveSystem.Profiles.Count == 0)
                sb.AppendLine("No profiles.");
            foreach (var entry in SaveSystem.Profiles)
            {
                sb.AppendLine();
                bool isActive = active != null && entry.Id == active.Id;
                sb.AppendLine($"=== Profile '{entry.DisplayName}'{(isActive ? "  (active)" : "")} ===");
                var profile = isActive ? active : SaveSystem.PeekProfile(entry.Id);
                if (profile == null)
                {
                    sb.AppendLine("  (unreadable: file and backup both fail)");
                    continue;
                }
                sb.Append(DescribeProfile(profile, content));

                if (SaveSystem.TryReadRunSave(entry.Id, out var run, out bool backup))
                {
                    sb.AppendLine(backup ? "  --- Run in progress (read from BACKUP) ---" : "  --- Run in progress ---");
                    sb.Append(DescribeRun(run, content));
                }
                else
                    sb.AppendLine("  No run in progress.");
            }

            var snapshots = ListSnapshots();
            sb.AppendLine();
            sb.AppendLine(snapshots.Count == 0 ? "No snapshots." : $"Snapshots: {string.Join(", ", snapshots)}");
            return sb.ToString();
        }

        /// <summary>One profile, readable. Card IDs show as names where <paramref name="content"/> knows them.</summary>
        public static string DescribeProfile(ProfileData profile, SaveContent content = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  Id: {profile.Id}");
            sb.AppendLine($"  Created: {Time(profile.CreatedUtcTicks)}   Last played: {Time(profile.LastPlayedUtcTicks)}");
            sb.AppendLine($"  Unlock-all override: {(profile.UnlockAll ? "ON" : "off")}");
            sb.AppendLine("  Counters:");
            if (profile.Counters.Count == 0)
                sb.AppendLine("    (none)");
            foreach (var pair in profile.Counters.OrderBy(p => p.Key))
                sb.AppendLine($"    {pair.Key} = {pair.Value}");
            sb.AppendLine($"  Granted unlocks: {Names(profile.GrantedUnlocks, content)}");
            sb.AppendLine($"  Seen unlocks: {Names(profile.SeenUnlocks, content)}");

            var cards = content?.Cards;
            if (cards != null)
            {
                sb.AppendLine("  Unlockable cards:");
                foreach (var card in cards.GetUnlockableCards())
                    sb.AppendLine(
                        $"    [{(UnlockRules.IsUnlocked(card, profile) ? "x" : " ")}] {card.CardName} — {UnlockRules.HowToUnlock(card)}"
                    );
            }
            return sb.ToString();
        }

        /// <summary>One run save, readable, with content IDs resolved to names where possible.</summary>
        public static string DescribeRun(RunSaveData run, SaveContent content = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"    Saved: {Time(run.SavedUtcTicks)}");
            sb.AppendLine($"    Origin: {(OriginType)run.Origin}   Campaign run: {run.IsCampaignRun}   Seed: {run.Seed}");
            sb.AppendLine($"    RNG state: {string.Join(" ", run.RngState ?? new uint[0])}");
            sb.AppendLine(
                $"    Day {run.Day}   Time left {run.MinutesRemaining / 60}h {run.MinutesRemaining % 60}m   "
                    + $"Elapsed {run.ElapsedMinutes}m   Max hours {run.MaxHours}   District: {run.DistrictName ?? "(none)"}"
            );
            sb.AppendLine($"    Funds {run.Funds}   Credibility {run.Credibility}   Next-battle Hostility +{run.NextBattleHostility}");

            sb.AppendLine($"    Deck ({run.Deck.Count}):");
            foreach (var group in run.Deck.GroupBy(c => (c.Id, c.Upgraded)).OrderBy(g => CardName(g.Key.Id, content)))
                sb.AppendLine(
                    $"      {group.Count()}x {CardName(group.Key.Id, content)}{(group.Key.Upgraded ? "+" : "")}"
                );

            sb.AppendLine($"    Allies: {List(run.AllyIds, id => content?.Ally(id)?.AllyName)}");
            sb.AppendLine($"    Flags: {(run.Flags.Count == 0 ? "(none)" : string.Join(", ", run.Flags))}");
            sb.AppendLine($"    Visited: {List(run.VisitedLocationIds, id => EncounterName(id, content))}");
            sb.AppendLine(
                $"    Today's locations (day {run.TodaysLocationsDay}): {List(run.TodaysLocationIds, id => EncounterName(id, content))}"
            );
            sb.AppendLine($"    Pending battle: {OneEncounter(run.PendingBattleId, content)}");
            sb.AppendLine($"    Next encounter (chain): {OneEncounter(run.NextEncounterId, content)}");
            sb.AppendLine($"    Open event: {OneEncounter(run.OpenEventId, content)}");
            if (run.BattleQueue != null)
            {
                sb.AppendLine($"    Battle queue (round {run.CurrentBattleIndex + 1} of {run.BattleQueue.Count}):");
                for (int i = 0; i < run.BattleQueue.Count; i++)
                    sb.AppendLine($"      {i + 1}. {List(run.BattleQueue[i], id => content?.Enemy(id)?.EnemyName)}");
            }
            sb.AppendLine($"    Unlocks this run: {Names(run.UnlockedContent, content)}");
            sb.AppendLine(
                $"    Run counters: {(run.RunCounters.Count == 0 ? "(none)" : string.Join(", ", run.RunCounters.Select(p => $"{p.Key}={p.Value}")))}"
            );
            return sb.ToString();
        }

        /// <summary>
        /// An encounter pool already in memory (the campaign scene's), so dumps can name
        /// encounters without being handed one. Null when none is loaded.
        /// </summary>
        public static EncounterPoolData LoadedPool()
        {
            var pools = Resources.FindObjectsOfTypeAll<EncounterPoolData>();
            return pools.Length > 0 ? pools[0] : null;
        }

        /// <summary>Writes <see cref="Describe"/> to <c>save-dump.txt</c> in the save folder and returns its path.</summary>
        public static string ExportDump(EncounterPoolData pool = null)
        {
            string path = Path.Combine(SaveSystem.Root, "save-dump.txt");
            Directory.CreateDirectory(SaveSystem.Root);
            File.WriteAllText(path, Describe(pool));
            GameLogger.LogInfo("Save", $"Save dump written to {path}");
            return path;
        }

        private static string Time(long ticks) =>
            ticks <= 0 ? "(never)" : new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        private static string CardName(string id, SaveContent content) =>
            content?.Card(id)?.CardName ?? $"<missing card {id}>";

        private static string EncounterName(string id, SaveContent content)
        {
            var encounter = content?.Encounter(id);
            if (encounter == null)
                return null;
            return string.IsNullOrEmpty(encounter.DisplayName) ? encounter.name : encounter.DisplayName;
        }

        private static string OneEncounter(string id, SaveContent content) =>
            string.IsNullOrEmpty(id) ? "(none)" : EncounterName(id, content) ?? $"<unresolved {id}>";

        private static string Names(IEnumerable<string> ids, SaveContent content) =>
            List(ids, id => content?.Card(id)?.CardName);

        private static string List(IEnumerable<string> ids, Func<string, string> name)
        {
            var parts = ids?.Select(id => name(id) ?? $"<{id}>").ToList() ?? new List<string>();
            return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
        }

        #endregion

        #region Snapshots

        private static string SnapshotsRoot => Path.Combine(SaveSystem.Root, SnapshotFolder);

        /// <summary>Snapshot names, oldest first.</summary>
        public static List<string> ListSnapshots()
        {
            if (!Directory.Exists(SnapshotsRoot))
                return new List<string>();
            return new DirectoryInfo(SnapshotsRoot)
                .GetDirectories()
                .OrderBy(d => d.CreationTimeUtc)
                .Select(d => d.Name)
                .ToList();
        }

        /// <summary>
        /// Copies every save file (all profiles, their runs, the index) into a named snapshot,
        /// replacing one with the same name. Saves the current run first so the snapshot is
        /// up to date.
        /// </summary>
        public static bool SaveSnapshot(string name)
        {
            if (!ValidName(name))
                return false;
            SaveSystem.Checkpoint();
            string target = Path.Combine(SnapshotsRoot, name);
            if (Directory.Exists(target))
                Directory.Delete(target, recursive: true);
            CopySaves(SaveSystem.Root, target);
            GameLogger.LogInfo("Save", $"Snapshot '{name}' saved.");
            return true;
        }

        /// <summary>
        /// Replaces every save file with a snapshot's, then reloads. The run in memory is
        /// dropped: continue it again to play from the snapshot.
        /// </summary>
        public static bool RestoreSnapshot(string name)
        {
            string source = Path.Combine(SnapshotsRoot, name ?? "");
            if (!ValidName(name) || !Directory.Exists(source))
            {
                GameLogger.LogWarning("Save", $"No snapshot named '{name}'.");
                return false;
            }
            DeleteSaves(SaveSystem.Root);
            CopySaves(source, SaveSystem.Root);
            RunState.Clear();
            SaveSystem.Reload();
            GameLogger.LogInfo("Save", $"Snapshot '{name}' restored. Re-enter the campaign to continue its run.");
            return true;
        }

        public static bool DeleteSnapshot(string name)
        {
            string path = Path.Combine(SnapshotsRoot, name ?? "");
            if (!ValidName(name) || !Directory.Exists(path))
                return false;
            Directory.Delete(path, recursive: true);
            return true;
        }

        private static bool ValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(".."))
            {
                GameLogger.LogWarning("Save", $"'{name}' isn't a usable snapshot name.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// True for paths the save system owns: the profile index and the profiles folder.
        /// Nothing else under the root is touched, so other data in persistentDataPath is safe.
        /// </summary>
        private static bool IsSaveFile(string relativePath)
        {
            string first = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            return first == "profiles" || first.StartsWith(SaveSystem.IndexPath);
        }

        /// <summary>Copies the save files under <paramref name="from"/> to <paramref name="to"/>.</summary>
        private static void CopySaves(string from, string to)
        {
            Directory.CreateDirectory(to);
            if (!Directory.Exists(from))
                return;
            foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(from.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!IsSaveFile(relative))
                    continue;
                string dest = Path.Combine(to, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(file, dest, overwrite: true);
            }
        }

        /// <summary>Deletes the save files under <paramref name="root"/> and nothing else.</summary>
        private static void DeleteSaves(string root)
        {
            string profiles = Path.Combine(root, "profiles");
            if (Directory.Exists(profiles))
                Directory.Delete(profiles, recursive: true);
            if (!Directory.Exists(root))
                return;
            foreach (var file in Directory.GetFiles(root))
                if (IsSaveFile(Path.GetFileName(file)))
                    File.Delete(file);
        }

        #endregion

        #region Edits

        /// <summary>Deletes every profile and run (snapshots stay). The next access creates a fresh profile.</summary>
        public static void WipeAll()
        {
            DeleteSaves(SaveSystem.Root);
            RunState.Clear();
            SaveSystem.Reload();
            GameLogger.LogWarning("Save", "All profiles and runs wiped.");
        }

        /// <summary>Sets one of the active profile's counters. 0 removes it.</summary>
        public static void SetCounter(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            var profile = SaveSystem.ActiveProfile;
            if (value == 0)
                profile.Counters.Remove(key);
            else
                profile.Counters[key] = value;
            SaveSystem.SaveProfile();
        }

        /// <summary>
        /// Takes back an explicit grant and its "seen" mark. A card unlocked by its condition
        /// stays unlocked until the counters behind it change.
        /// </summary>
        public static void RevokeUnlock(string contentId)
        {
            var profile = SaveSystem.ActiveProfile;
            bool changed = profile.GrantedUnlocks.Remove(contentId) | profile.SeenUnlocks.Remove(contentId);
            if (changed)
                SaveSystem.SaveProfile();
        }

        /// <summary>Forgets which unlocks were shown, so every reveal plays again.</summary>
        public static void ClearSeenUnlocks()
        {
            SaveSystem.ActiveProfile.SeenUnlocks.Clear();
            SaveSystem.SaveProfile();
        }

        /// <summary>
        /// Overwrites a save file of the active profile with garbage, to exercise the backup
        /// fallback. With <paramref name="backupToo"/> the backup goes as well, so the file is
        /// truly lost.
        /// </summary>
        public static void Corrupt(SaveFile which, bool backupToo = false)
        {
            string relative = which switch
            {
                SaveFile.Run => SaveSystem.RunPath(SaveSystem.ActiveProfile.Id),
                SaveFile.Profile => SaveSystem.ProfilePath(SaveSystem.ActiveProfile.Id),
                _ => SaveSystem.IndexPath,
            };
            string path = SaveSystem.FileStore.PathOf(relative);
            var garbage = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            if (File.Exists(path))
                File.WriteAllBytes(path, garbage);
            if (backupToo && File.Exists(path + ".bak"))
                File.WriteAllBytes(path + ".bak", garbage);
            SaveSystem.Reload();
            GameLogger.LogWarning("Save", $"Corrupted {which}{(backupToo ? " and its backup" : "")}: {path}");
        }

        #endregion

        /// <summary>
        /// Finds a card by name, ignoring case, spaces and punctuation; a unique partial match
        /// also counts. Null with <paramref name="error"/> set otherwise.
        /// </summary>
        public static CardData FindCard(string query, out string error)
        {
            error = null;
            var cards = CardDatabase.Shared;
            if (cards == null)
            {
                error = "CardDatabase not found.";
                return null;
            }
            string wanted = Normalize(query);
            var matches = cards.GetAll().Where(c => c != null && !c.IsUpgraded && Normalize(c.CardName).Contains(wanted)).ToList();
            var exact = matches.FirstOrDefault(c => Normalize(c.CardName) == wanted);
            if (exact != null)
                return exact;
            if (matches.Count == 1)
                return matches[0];
            error = matches.Count == 0 ? $"No card named '{query}'." : $"'{query}' matches {matches.Count} cards.";
            return null;
        }

        private static string Normalize(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text ?? "")
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }
    }
}
