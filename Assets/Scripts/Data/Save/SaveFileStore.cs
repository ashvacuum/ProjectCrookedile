using System;
using System.IO;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Reads and writes save files under one root folder, crash-safely: a write goes to a
    /// temporary file, is read back and checked, then replaces the real file, which is kept as
    /// <c>.bak</c>. A read that fails falls back to that backup. Knows nothing about Unity or
    /// the payloads, so it runs anywhere.
    /// </summary>
    public class SaveFileStore
    {
        public string Root { get; }

        public SaveFileStore(string root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public string PathOf(string relativePath) => Path.Combine(Root, relativePath);

        public bool Exists(string relativePath) =>
            File.Exists(PathOf(relativePath)) || File.Exists(PathOf(relativePath) + ".bak");

        /// <summary>Writes <paramref name="bytes"/> atomically, keeping the previous file as a backup.</summary>
        public void Write(string relativePath, byte[] bytes)
        {
            string path = PathOf(relativePath);
            string tmp = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllBytes(tmp, bytes);
            byte[] check = File.ReadAllBytes(tmp);
            if (check.Length != bytes.Length)
                throw new IOException($"Save verification failed for {relativePath}.");
            for (int i = 0; i < check.Length; i++)
                if (check[i] != bytes[i])
                    throw new IOException($"Save verification failed for {relativePath}.");

            if (File.Exists(path))
                File.Copy(path, path + ".bak", overwrite: true);
            File.Copy(tmp, path, overwrite: true);
            File.Delete(tmp);
        }

        /// <summary>
        /// Reads and decodes a file with <paramref name="decode"/>, falling back to the backup
        /// when the main file is missing or fails to decode. Returns false when neither works;
        /// <paramref name="usedBackup"/> says whether the backup was the one read.
        /// </summary>
        public bool TryRead<T>(
            string relativePath,
            Func<byte[], T> decode,
            out T value,
            out bool usedBackup
        )
        {
            string path = PathOf(relativePath);
            usedBackup = false;
            if (TryDecode(path, decode, out value))
                return true;
            usedBackup = true;
            return TryDecode(path + ".bak", decode, out value);
        }

        private static bool TryDecode<T>(string path, Func<byte[], T> decode, out T value)
        {
            value = default;
            if (!File.Exists(path))
                return false;
            try
            {
                value = decode(File.ReadAllBytes(path));
                return true;
            }
            catch (SaveCorruptException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>Deletes a file with its backup and any leftover temporary file.</summary>
        public void Delete(string relativePath)
        {
            string path = PathOf(relativePath);
            foreach (var p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p))
                    File.Delete(p);
        }

        /// <summary>Deletes a folder under the root and everything in it.</summary>
        public void DeleteFolder(string relativePath)
        {
            string path = PathOf(relativePath);
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
