using System;
using System.Collections.Generic;
using System.IO;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Everything a player profile keeps across runs: lifetime counters, explicitly granted
    /// unlocks, the unlocks already revealed, and the dev unlock-all override. Plain data with
    /// its own binary format; <see cref="SaveSystem"/> owns where it lives.
    /// </summary>
    public class ProfileData
    {
        /// <summary>Schema version written with every profile. Bump it and add a migration on change.</summary>
        public const ushort SchemaVersion = 1;

        public string Id;
        public string DisplayName;
        public long CreatedUtcTicks;
        public long LastPlayedUtcTicks;

        /// <summary>Lifetime counters ("runs_won", "runs_won_nepobaby", …). Keys are <see cref="ProfileCounters"/> constants.</summary>
        public Dictionary<string, int> Counters = new Dictionary<string, int>();

        /// <summary>Content unlocked by an explicit grant (a campaign event), by content ID.</summary>
        public HashSet<string> GrantedUnlocks = new HashSet<string>();

        /// <summary>Unlocked content the player has already been shown, so a reveal happens once.</summary>
        public HashSet<string> SeenUnlocks = new HashSet<string>();

        /// <summary>Dev override: everything counts as unlocked.</summary>
        public bool UnlockAll;

        public int GetCounter(string key) =>
            key != null && Counters.TryGetValue(key, out int value) ? value : 0;

        public void AddToCounter(string key, int amount)
        {
            if (string.IsNullOrEmpty(key) || amount == 0)
                return;
            Counters[key] = GetCounter(key) + amount;
        }

        /// <summary>Raises a best-ever counter to <paramref name="value"/> if it's higher.</summary>
        public void RaiseCounterTo(string key, int value)
        {
            if (!string.IsNullOrEmpty(key) && value > GetCounter(key))
                Counters[key] = value;
        }

        public byte[] ToBytes() =>
            SaveEnvelope.Wrap(SaveKind.Profile, SchemaVersion, SaveBinary.ToBytes(Write));

        public static ProfileData FromBytes(byte[] file)
        {
            byte[] payload = SaveEnvelope.Unwrap(file, SaveKind.Profile, out ushort version);
            if (version > SchemaVersion)
                throw new SaveCorruptException($"Profile written by a newer game (v{version}).");
            return SaveBinary.FromBytes(payload, r => Read(r, version));
        }

        private void Write(BinaryWriter w)
        {
            w.WriteNullableString(Id);
            w.WriteNullableString(DisplayName);
            w.Write(CreatedUtcTicks);
            w.Write(LastPlayedUtcTicks);
            w.WriteCounters(Counters);
            w.WriteStrings(GrantedUnlocks);
            w.WriteStrings(SeenUnlocks);
            w.Write(UnlockAll);
        }

        private static ProfileData Read(BinaryReader r, ushort version)
        {
            // Version 1 is the only schema so far; later versions branch here.
            return new ProfileData
            {
                Id = r.ReadNullableString(),
                DisplayName = r.ReadNullableString(),
                CreatedUtcTicks = r.ReadInt64(),
                LastPlayedUtcTicks = r.ReadInt64(),
                Counters = r.ReadCounters(),
                GrantedUnlocks = r.ReadStringSet(),
                SeenUnlocks = r.ReadStringSet(),
                UnlockAll = r.ReadBoolean(),
            };
        }
    }

    /// <summary>The list of profiles on this machine and which one was used last.</summary>
    public class ProfileIndex
    {
        public const ushort SchemaVersion = 1;

        public struct Entry
        {
            public string Id;
            public string DisplayName;
        }

        public List<Entry> Profiles = new List<Entry>();
        public string ActiveProfileId;

        public byte[] ToBytes() =>
            SaveEnvelope.Wrap(
                SaveKind.ProfileIndex,
                SchemaVersion,
                SaveBinary.ToBytes(w =>
                {
                    w.Write(Profiles.Count);
                    foreach (var p in Profiles)
                    {
                        w.WriteNullableString(p.Id);
                        w.WriteNullableString(p.DisplayName);
                    }
                    w.WriteNullableString(ActiveProfileId);
                })
            );

        public static ProfileIndex FromBytes(byte[] file)
        {
            byte[] payload = SaveEnvelope.Unwrap(file, SaveKind.ProfileIndex, out ushort version);
            if (version > SchemaVersion)
                throw new SaveCorruptException($"Profile index written by a newer game (v{version}).");
            return SaveBinary.FromBytes(
                payload,
                r =>
                {
                    var index = new ProfileIndex();
                    int count = r.ReadCount();
                    for (int i = 0; i < count; i++)
                        index.Profiles.Add(
                            new Entry
                            {
                                Id = r.ReadNullableString(),
                                DisplayName = r.ReadNullableString(),
                            }
                        );
                    index.ActiveProfileId = r.ReadNullableString();
                    return index;
                }
            );
        }
    }

    /// <summary>Profile counter keys. Code increments them; unlock conditions read them.</summary>
    public static class ProfileCounters
    {
        public const string RunsStarted = "runs_started";
        public const string RunsWon = "runs_won";
        public const string RunsLost = "runs_lost";
        public const string BattlesWon = "battles_won";
        public const string HighestDay = "highest_day";

        /// <summary>Runs won as a specific origin, e.g. "runs_won_nepobaby".</summary>
        public static string RunsWonAs(OriginType origin) => RunsWon + "_" + origin.ToString().ToLowerInvariant();

        /// <summary>Every key a condition can pick, for inspector dropdowns.</summary>
        public static IEnumerable<string> All()
        {
            yield return RunsStarted;
            yield return RunsWon;
            yield return RunsLost;
            yield return BattlesWon;
            yield return HighestDay;
            foreach (OriginType origin in Enum.GetValues(typeof(OriginType)))
                yield return RunsWonAs(origin);
        }
    }
}
