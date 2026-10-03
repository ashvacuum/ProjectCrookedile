using System;
using System.Collections.Generic;
using System.IO;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Reader/writer helpers for save payloads. Every collection is length-prefixed and every
    /// string nullable, so a payload reads back exactly as written.
    /// </summary>
    public static class SaveBinary
    {
        /// <summary>Serializes with <paramref name="write"/> into a fresh byte array.</summary>
        public static byte[] ToBytes(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                write(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>Deserializes <paramref name="payload"/> with <paramref name="read"/>.</summary>
        public static T FromBytes<T>(byte[] payload, Func<BinaryReader, T> read)
        {
            try
            {
                using (var reader = new BinaryReader(new MemoryStream(payload)))
                    return read(reader);
            }
            catch (EndOfStreamException)
            {
                throw new SaveCorruptException("Save payload ended early.");
            }
        }

        public static void WriteNullableString(this BinaryWriter w, string value)
        {
            w.Write(value != null);
            if (value != null)
                w.Write(value);
        }

        public static string ReadNullableString(this BinaryReader r) =>
            r.ReadBoolean() ? r.ReadString() : null;

        public static void WriteStrings(this BinaryWriter w, ICollection<string> values)
        {
            w.Write(values?.Count ?? 0);
            if (values != null)
                foreach (var v in values)
                    w.WriteNullableString(v);
        }

        public static List<string> ReadStringList(this BinaryReader r)
        {
            int count = ReadCount(r);
            var list = new List<string>(count);
            for (int i = 0; i < count; i++)
                list.Add(r.ReadNullableString());
            return list;
        }

        public static HashSet<string> ReadStringSet(this BinaryReader r) =>
            new HashSet<string>(r.ReadStringList());

        public static void WriteCounters(this BinaryWriter w, IDictionary<string, int> counters)
        {
            w.Write(counters?.Count ?? 0);
            if (counters != null)
                foreach (var pair in counters)
                {
                    w.Write(pair.Key);
                    w.Write(pair.Value);
                }
        }

        public static Dictionary<string, int> ReadCounters(this BinaryReader r)
        {
            int count = ReadCount(r);
            var map = new Dictionary<string, int>(count);
            for (int i = 0; i < count; i++)
                map[r.ReadString()] = r.ReadInt32();
            return map;
        }

        /// <summary>Reads a collection length, rejecting values a corrupt file could produce.</summary>
        public static int ReadCount(this BinaryReader r)
        {
            int count = r.ReadInt32();
            if (count < 0 || count > 1_000_000)
                throw new SaveCorruptException($"Implausible collection size {count}.");
            return count;
        }
    }
}
