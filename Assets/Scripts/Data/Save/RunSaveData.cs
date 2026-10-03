using System.Collections.Generic;
using System.IO;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// A run in progress, as plain data: every <see cref="RunState"/> field a resume needs,
    /// with content held by ID. Written at campaign checkpoints; a battle in progress is not
    /// saved, so resuming during one restarts that battle.
    /// </summary>
    public class RunSaveData
    {
        public const ushort SchemaVersion = 1;

        public struct CardEntry
        {
            public string Id;
            public bool Upgraded;
        }

        public long SavedUtcTicks;
        public int Origin;
        public bool IsCampaignRun;
        public int Seed;
        public uint[] RngState = new uint[4];

        public int Funds;
        public int Credibility;
        public int MaxHours;
        public int Day;
        public int MinutesRemaining;
        public int ElapsedMinutes;
        public string DistrictName;

        public List<CardEntry> Deck = new List<CardEntry>();
        public List<string> AllyIds = new List<string>();
        public List<string> VisitedLocationIds = new List<string>();
        public List<string> Flags = new List<string>();

        public int TodaysLocationsDay = -1;
        public List<string> TodaysLocationIds = new List<string>();
        public string PendingBattleId;
        public string NextEncounterId;
        public string OpenEventId;
        public int NextBattleHostility;

        public List<List<string>> BattleQueue;
        public int CurrentBattleIndex;

        /// <summary>The profile's unlocked content when the run started; the run reads only this.</summary>
        public List<string> UnlockedContent = new List<string>();

        /// <summary>This run's counters, folded into the profile when the run ends.</summary>
        public Dictionary<string, int> RunCounters = new Dictionary<string, int>();

        public byte[] ToBytes() =>
            SaveEnvelope.Wrap(SaveKind.Run, SchemaVersion, SaveBinary.ToBytes(Write));

        public static RunSaveData FromBytes(byte[] file)
        {
            byte[] payload = SaveEnvelope.Unwrap(file, SaveKind.Run, out ushort version);
            if (version > SchemaVersion)
                throw new SaveCorruptException($"Run save written by a newer game (v{version}).");
            return SaveBinary.FromBytes(payload, r => Read(r, version));
        }

        private void Write(BinaryWriter w)
        {
            w.Write(SavedUtcTicks);
            w.Write(Origin);
            w.Write(IsCampaignRun);
            w.Write(Seed);
            for (int i = 0; i < 4; i++)
                w.Write(RngState != null && RngState.Length == 4 ? RngState[i] : 0u);

            w.Write(Funds);
            w.Write(Credibility);
            w.Write(MaxHours);
            w.Write(Day);
            w.Write(MinutesRemaining);
            w.Write(ElapsedMinutes);
            w.WriteNullableString(DistrictName);

            w.Write(Deck.Count);
            foreach (var card in Deck)
            {
                w.WriteNullableString(card.Id);
                w.Write(card.Upgraded);
            }
            w.WriteStrings(AllyIds);
            w.WriteStrings(VisitedLocationIds);
            w.WriteStrings(Flags);

            w.Write(TodaysLocationsDay);
            w.WriteStrings(TodaysLocationIds);
            w.WriteNullableString(PendingBattleId);
            w.WriteNullableString(NextEncounterId);
            w.WriteNullableString(OpenEventId);
            w.Write(NextBattleHostility);

            w.Write(BattleQueue != null);
            if (BattleQueue != null)
            {
                w.Write(BattleQueue.Count);
                foreach (var round in BattleQueue)
                    w.WriteStrings(round);
            }
            w.Write(CurrentBattleIndex);

            w.WriteStrings(UnlockedContent);
            w.WriteCounters(RunCounters);
        }

        private static RunSaveData Read(BinaryReader r, ushort version)
        {
            // Version 1 is the only schema so far; later versions branch here.
            var data = new RunSaveData
            {
                SavedUtcTicks = r.ReadInt64(),
                Origin = r.ReadInt32(),
                IsCampaignRun = r.ReadBoolean(),
                Seed = r.ReadInt32(),
                RngState = new[] { r.ReadUInt32(), r.ReadUInt32(), r.ReadUInt32(), r.ReadUInt32() },
                Funds = r.ReadInt32(),
                Credibility = r.ReadInt32(),
                MaxHours = r.ReadInt32(),
                Day = r.ReadInt32(),
                MinutesRemaining = r.ReadInt32(),
                ElapsedMinutes = r.ReadInt32(),
                DistrictName = r.ReadNullableString(),
            };

            int deckCount = r.ReadCount();
            for (int i = 0; i < deckCount; i++)
                data.Deck.Add(new CardEntry { Id = r.ReadNullableString(), Upgraded = r.ReadBoolean() });
            data.AllyIds = r.ReadStringList();
            data.VisitedLocationIds = r.ReadStringList();
            data.Flags = r.ReadStringList();

            data.TodaysLocationsDay = r.ReadInt32();
            data.TodaysLocationIds = r.ReadStringList();
            data.PendingBattleId = r.ReadNullableString();
            data.NextEncounterId = r.ReadNullableString();
            data.OpenEventId = r.ReadNullableString();
            data.NextBattleHostility = r.ReadInt32();

            if (r.ReadBoolean())
            {
                int rounds = r.ReadCount();
                data.BattleQueue = new List<List<string>>(rounds);
                for (int i = 0; i < rounds; i++)
                    data.BattleQueue.Add(r.ReadStringList());
            }
            data.CurrentBattleIndex = r.ReadInt32();

            data.UnlockedContent = r.ReadStringList();
            data.RunCounters = r.ReadCounters();
            return data;
        }
    }
}
