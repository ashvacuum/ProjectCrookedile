using System.Collections.Generic;
using System.IO;

namespace Crookedile.Data.Save
{
    /// <summary>What kind of player decision a <see cref="BattleInput"/> records.</summary>
    public enum BattleInputKind : byte
    {
        PlayCard = 1,
        EndTurn = 2,
        CardChoice = 3,
        MediaTraining = 4,
    }

    /// <summary>
    /// One player decision in a battle. A battle is deterministic from its seed, so replaying
    /// its decisions in order rebuilds it exactly.
    /// </summary>
    public struct BattleInput
    {
        public BattleInputKind Kind;

        /// <summary>PlayCard: index in hand.</summary>
        public int HandIndex;

        /// <summary>PlayCard: the focused enemy when it was played.</summary>
        public int TargetIndex;

        /// <summary>CardChoice: indices into the request's choices; empty = cancelled.</summary>
        public List<int> Picks;
    }

    /// <summary>
    /// The decisions made so far in the battle round the run save has pending, so quitting
    /// mid-battle resumes into the same spot. Only valid for the round it names.
    /// </summary>
    public class BattleLogData
    {
        public const ushort SchemaVersion = 1;

        public string EncounterId;
        public int BattleIndex;
        public List<BattleInput> Inputs = new List<BattleInput>();

        public byte[] ToBytes() =>
            SaveEnvelope.Wrap(SaveKind.BattleLog, SchemaVersion, SaveBinary.ToBytes(Write));

        public static BattleLogData FromBytes(byte[] file)
        {
            byte[] payload = SaveEnvelope.Unwrap(file, SaveKind.BattleLog, out ushort version);
            if (version > SchemaVersion)
                throw new SaveCorruptException($"Battle log written by a newer game (v{version}).");
            return SaveBinary.FromBytes(payload, Read);
        }

        private void Write(BinaryWriter w)
        {
            w.WriteNullableString(EncounterId);
            w.Write(BattleIndex);
            w.Write(Inputs.Count);
            foreach (var input in Inputs)
            {
                w.Write((byte)input.Kind);
                w.Write(input.HandIndex);
                w.Write(input.TargetIndex);
                w.Write(input.Picks?.Count ?? 0);
                if (input.Picks != null)
                    foreach (int pick in input.Picks)
                        w.Write(pick);
            }
        }

        private static BattleLogData Read(BinaryReader r)
        {
            var data = new BattleLogData
            {
                EncounterId = r.ReadNullableString(),
                BattleIndex = r.ReadInt32(),
            };
            int count = r.ReadCount();
            for (int i = 0; i < count; i++)
            {
                var input = new BattleInput
                {
                    Kind = (BattleInputKind)r.ReadByte(),
                    HandIndex = r.ReadInt32(),
                    TargetIndex = r.ReadInt32(),
                };
                int picks = r.ReadCount();
                input.Picks = new List<int>(picks);
                for (int p = 0; p < picks; p++)
                    input.Picks.Add(r.ReadInt32());
                data.Inputs.Add(input);
            }
            return data;
        }
    }
}
