using System;
using System.Collections.Generic;
using System.IO;
using Crookedile.Data;
using Crookedile.Data.Audio;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    public sealed class ContentAssetNaming : ContentChecks.IContentProvider
    {
        public const string CATEGORY = "Asset Names";

        public sealed class Entry
        {
            public UnityEngine.Object Asset;
            public string Path;
            public string AuthoredName;
            public string TargetPath;
            public string Error;

            public bool CanRename
            {
                get { return string.IsNullOrEmpty(Error) && Path != TargetPath; }
            }
        }

        public string Category
        {
            get { return CATEGORY; }
        }

        public IEnumerable<ContentChecks.Row> Rows()
        {
            foreach (var entry in Scan())
            {
                var issues = new List<ContentChecks.AuditIssue>();
                if (!string.IsNullOrEmpty(entry.Error))
                {
                    issues.Add(
                        new ContentChecks.AuditIssue(
                            ContentChecks.Severity.Error,
                            entry.Error
                        )
                    );
                }
                else if (entry.CanRename)
                {
                    issues.Add(
                        new ContentChecks.AuditIssue(
                            ContentChecks.Severity.Warning,
                            $"Filename differs from authored name. Expected: {entry.AuthoredName}.asset"
                        )
                    );
                }

                yield return new ContentChecks.Row(
                    System.IO.Path.GetFileName(entry.Path),
                    $"{entry.Asset.GetType().Name} | Name: {entry.AuthoredName}\n{entry.Path}",
                    entry.Asset,
                    issues
                );
            }
        }

        public static List<Entry> Scan()
        {
            var entries = new List<Entry>();
            var targets = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (
                string guid in AssetDatabase.FindAssets(
                    "t:CardData t:EnemyData t:EnemyMoveData t:EncounterData t:AllyData "
                        + "t:OriginPassive t:AudioClipData t:DistrictData t:CityZoneData",
                    new[] { "Assets" }
                )
            )
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var entry = Inspect(asset);
                if (entry == null)
                {
                    continue;
                }

                entries.Add(entry);
                if (string.IsNullOrEmpty(entry.TargetPath))
                {
                    continue;
                }

                if (targets.TryGetValue(entry.TargetPath, out var other))
                {
                    entry.Error =
                        $"Another asset in this folder has the same authored name: {other.Path}";
                    other.Error =
                        $"Another asset in this folder has the same authored name: {entry.Path}";
                }
                else
                {
                    targets.Add(entry.TargetPath, entry);
                }
            }

            entries.Sort(CompareEntries);
            return entries;
        }

        private static int CompareEntries(Entry left, Entry right)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(left.Path, right.Path);
        }

        public static Entry Inspect(UnityEngine.Object asset)
        {
            string field;
            switch (asset)
            {
                case CardData _:
                    field = "_cardName";
                    break;
                case EnemyData _:
                    field = "_enemyName";
                    break;
                case EnemyMoveData _:
                    field = "_moveName";
                    break;
                case EncounterData _:
                    field = "_displayName";
                    break;
                case AllyData _:
                    field = "_allyName";
                    break;
                case OriginPassive _:
                    field = "_passiveName";
                    break;
                case AudioClipData _:
                    field = "_clipName";
                    break;
                case DistrictData _:
                    field = "_displayName";
                    break;
                case CityZoneData _:
                    field = "_displayName";
                    break;
                default:
                    return null;
            }

            // Read the authored value, including blanks hidden by runtime display-name fallbacks.
            using var serialized = new SerializedObject(asset);
            string authoredName = serialized.FindProperty(field).stringValue;
            string path = AssetDatabase.GetAssetPath(asset);
            var entry = new Entry
            {
                Asset = asset,
                Path = path,
                AuthoredName = authoredName,
                Error = ValidateName(authoredName),
            };
            if (!string.IsNullOrEmpty(entry.Error))
            {
                return entry;
            }

            if (
                !path.StartsWith("Assets/", StringComparison.Ordinal)
                || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || !AssetDatabase.IsMainAsset(asset)
            )
            {
                entry.Error = "Only standalone .asset files under Assets can be renamed.";
                return entry;
            }

            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            entry.TargetPath = $"{folder}/{authoredName}.asset";
            foreach (string sibling in Directory.EnumerateFileSystemEntries(folder))
            {
                string normalized = sibling.Replace('\\', '/');
                if (string.Equals(normalized, path, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (
                    string.Equals(normalized, entry.TargetPath, StringComparison.OrdinalIgnoreCase)
                    || (
                        string.Equals(
                            normalized,
                            entry.TargetPath + ".meta",
                            StringComparison.OrdinalIgnoreCase
                        )
                        && !string.Equals(
                            path,
                            entry.TargetPath,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                {
                    entry.Error = $"Target filename already exists: {entry.TargetPath}";
                    break;
                }
            }

            return entry;
        }

        public static string ValidateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Missing authored name. Set the name in the Inspector.";
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "unknown enemy":
                case "new card":
                case "new enemy":
                case "new enemy move":
                case "new encounter":
                case "new event encounter":
                case "new battle encounter":
                case "new ally":
                case "new origin passive":
                case "newaudioclipdata":
                case "new district":
                case "new zone":
                    return "Placeholder name. Set an authored name in the Inspector.";
            }

            if (value != value.Trim() || value.EndsWith(".", StringComparison.Ordinal))
            {
                return "Name cannot start or end with whitespace, or end with a dot.";
            }

            foreach (char character in value)
            {
                if (char.IsControl(character) || "<>:\"/\\|?*".IndexOf(character) >= 0)
                {
                    return "Name contains characters that cannot be used in an asset filename.";
                }
            }

            string stem = value.Split('.')[0].TrimEnd().ToUpperInvariant();
            if (
                stem == "CON"
                || stem == "PRN"
                || stem == "AUX"
                || stem == "NUL"
                || (
                    stem.Length == 4
                    && (stem.StartsWith("COM") || stem.StartsWith("LPT"))
                    && "123456789¹²³".IndexOf(stem[3]) >= 0
                )
            )
            {
                return "Name is reserved by Windows. Choose another name.";
            }

            return null;
        }

        public static string Rename(UnityEngine.Object asset)
        {
            // Re-scan at click time because the Inspector or another rename may have changed the audit.
            foreach (var entry in Scan())
            {
                if (entry.Asset != asset)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(entry.Error))
                {
                    return entry.Error;
                }

                if (!entry.CanRename)
                {
                    return null;
                }

                return AssetDatabase.RenameAsset(entry.Path, entry.AuthoredName);
            }

            return "Asset is no longer available for naming.";
        }

        public static string RenameValidAssets()
        {
            int renamed = 0;
            var failures = new List<string>();
            foreach (var entry in Scan())
            {
                if (!entry.CanRename)
                {
                    continue;
                }

                var current = Inspect(entry.Asset);
                string error = current.Error;
                if (string.IsNullOrEmpty(error) && current.CanRename)
                {
                    error = AssetDatabase.RenameAsset(current.Path, current.AuthoredName);
                    if (string.IsNullOrEmpty(error))
                    {
                        renamed++;
                    }
                }

                if (!string.IsNullOrEmpty(error))
                {
                    failures.Add($"{entry.Path}: {error}");
                }
            }

            return $"Renamed {renamed} asset(s). Assets with naming errors were skipped."
                + (failures.Count == 0 ? "" : "\n\n" + string.Join("\n", failures));
        }
    }
}
