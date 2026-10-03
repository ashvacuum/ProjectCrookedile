using System;
using System.IO;
using System.Text;

namespace Crookedile.Data.Save
{
    /// <summary>What a save file holds. Stored in the envelope so a file can't be read as the wrong thing.</summary>
    public enum SaveKind : byte
    {
        ProfileIndex = 1,
        Profile = 2,
        Run = 3,
    }

    /// <summary>A save file is unreadable: wrong kind, truncated, or failing its checksum.</summary>
    public class SaveCorruptException : Exception
    {
        public SaveCorruptException(string message)
            : base(message) { }
    }

    /// <summary>
    /// The wrapper every save file uses: magic, envelope version, kind, the payload's schema
    /// version, length and CRC-32, then the payload. The schema version travels with the
    /// payload so each reader can migrate older data; the CRC catches corruption.
    /// </summary>
    public static class SaveEnvelope
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CRKS");
        public const ushort EnvelopeVersion = 1;

        /// <summary>Wraps <paramref name="payload"/> in an envelope.</summary>
        public static byte[] Wrap(SaveKind kind, ushort schemaVersion, byte[] payload)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(EnvelopeVersion);
                writer.Write((byte)kind);
                writer.Write(schemaVersion);
                writer.Write(payload.Length);
                writer.Write(Crc32.Compute(payload));
                writer.Write(payload);
                writer.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Unwraps a file of the expected <paramref name="kind"/>, returning its payload and the
        /// schema version it was written with. Throws <see cref="SaveCorruptException"/> on any
        /// mismatch.
        /// </summary>
        public static byte[] Unwrap(byte[] file, SaveKind kind, out ushort schemaVersion)
        {
            const int headerSize = 4 + 2 + 1 + 2 + 4 + 4;
            if (file == null || file.Length < headerSize)
                throw new SaveCorruptException("File is shorter than a save header.");

            using (var reader = new BinaryReader(new MemoryStream(file)))
            {
                byte[] magic = reader.ReadBytes(4);
                for (int i = 0; i < Magic.Length; i++)
                    if (magic[i] != Magic[i])
                        throw new SaveCorruptException("Not a Crookedile save file.");

                ushort envelopeVersion = reader.ReadUInt16();
                if (envelopeVersion > EnvelopeVersion)
                    throw new SaveCorruptException(
                        $"Save written by a newer game (envelope v{envelopeVersion})."
                    );

                var fileKind = (SaveKind)reader.ReadByte();
                if (fileKind != kind)
                    throw new SaveCorruptException($"Expected a {kind} save, found {fileKind}.");

                schemaVersion = reader.ReadUInt16();
                int length = reader.ReadInt32();
                uint crc = reader.ReadUInt32();
                if (length < 0 || length != file.Length - headerSize)
                    throw new SaveCorruptException("Save length doesn't match its header.");

                byte[] payload = reader.ReadBytes(length);
                if (Crc32.Compute(payload) != crc)
                    throw new SaveCorruptException("Save checksum mismatch.");
                return payload;
            }
        }
    }
}
