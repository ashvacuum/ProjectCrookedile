using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crookedile.Data.Campaign;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Imports the Encounters tab of <c>docs/campaign-ideation.xlsx</c> (saved as CSV) into real
    /// encounter assets and pool rows. Every column in that sheet already maps 1:1 to a
    /// serialized field, so a week ideated in the spreadsheet becomes a week you can play.
    ///
    /// Re-runnable and additive: rows are matched by Asset Name, so re-importing updates the
    /// scheduling numbers on encounters you have since hand-authored rather than replacing them.
    /// Body text, event options, battle sessions and requirement conditions are NOT imported —
    /// they are object references and polymorphic types that no column of text can resolve
    /// unambiguously. The report says which rows still need that wiring.
    ///
    /// Reached from Crookedile → Encounter Designer → Import CSV.
    /// </summary>
    public static class EncounterCsvImporter
    {
        private const string EncounterDir = "Assets/Data/Encounters";

        public static void ImportInto(EncounterPoolData pool)
        {
            if (pool == null)
                return;

            string path = EditorUtility.OpenFilePanel(
                "Import encounters CSV (the sheet's Encounters tab)",
                Application.dataPath,
                "csv"
            );
            if (string.IsNullOrEmpty(path))
                return;

            List<string[]> rows;
            try
            {
                rows = ParseCsv(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Import failed", $"Could not read the CSV:\n{e.Message}", "OK");
                return;
            }

            var header = FindHeader(rows);
            if (header == null)
            {
                EditorUtility.DisplayDialog(
                    "Import failed",
                    "No header row with an 'Asset Name' column. Export the Encounters tab, not the whole workbook.",
                    "OK"
                );
                return;
            }

            var report = new StringBuilder();
            int created = 0;
            int updated = 0;
            var needsWiring = new List<string>();

            var poolObject = new SerializedObject(pool);
            var entriesProperty = poolObject.FindProperty("_entries");

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var row in rows.Skip(header.RowIndex + 1))
                {
                    string assetName = header.Get(row, "asset name");
                    if (string.IsNullOrWhiteSpace(assetName))
                        continue;

                    bool isBattle = header
                        .Get(row, "type")
                        .Equals("battle", StringComparison.OrdinalIgnoreCase);

                    var encounter = LoadOrCreate(assetName, isBattle, ref created, ref updated);
                    if (encounter == null)
                    {
                        report.AppendLine($"  {assetName}: could not create asset");
                        continue;
                    }

                    ApplyEncounterFields(encounter, row, header, isBattle);
                    ApplyPoolRow(entriesProperty, encounter, row, header);

                    // Columns that carry an object reference or a polymorphic condition. Naming
                    // them beats a silent partial import that looks complete.
                    if (
                        !string.IsNullOrWhiteSpace(header.Get(row, "enemies"))
                        || !string.IsNullOrWhiteSpace(header.Get(row, "requirements"))
                        || !string.IsNullOrWhiteSpace(header.Get(row, "boost if"))
                    )
                        needsWiring.Add(assetName);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            poolObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(pool);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.Insert(
                0,
                $"{created} created, {updated} updated, {pool.Entries.Count} rows in '{pool.name}'.\n\n"
            );
            if (needsWiring.Count > 0)
                report.AppendLine(
                    "\nStill need hand-wiring (session, requirements or boost conditions):\n  "
                        + string.Join("\n  ", needsWiring)
                );

            Debug.Log($"[Encounter import]\n{report}", pool);
            EditorUtility.DisplayDialog("Import complete", report.ToString(), "OK");
        }

        #region Assets
        private static EncounterData LoadOrCreate(
            string assetName,
            bool isBattle,
            ref int created,
            ref int updated
        )
        {
            string guid = AssetDatabase
                .FindAssets($"{assetName} t:EncounterData", new[] { EncounterDir })
                .FirstOrDefault(g =>
                    Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == assetName
                );

            if (guid != null)
            {
                updated++;
                return AssetDatabase.LoadAssetAtPath<EncounterData>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
            }

            EncounterData asset = isBattle
                ? ScriptableObject.CreateInstance<BattleEncounterData>()
                : (EncounterData)ScriptableObject.CreateInstance<EventEncounterData>();

            Directory.CreateDirectory(EncounterDir);
            AssetDatabase.CreateAsset(asset, $"{EncounterDir}/{assetName}.asset");
            created++;
            return asset;
        }

        private static void ApplyEncounterFields(
            EncounterData encounter,
            string[] row,
            Header header,
            bool isBattle
        )
        {
            var so = new SerializedObject(encounter);
            SetString(so, "_displayName", header.Get(row, "display name"));
            SetString(so, "_blurb", header.Get(row, "blurb"));
            SetInt(so, "_hourCost", header.Get(row, "hour cost"));
            SetFloat(so, "_dropWeight", header.Get(row, "drop weight"));
            if (isBattle)
                SetBool(so, "_isBoss", header.Get(row, "is boss"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Finds this encounter's row in the pool, appending one if it has none, and writes the
        /// scheduling columns onto it.
        /// </summary>
        private static void ApplyPoolRow(
            SerializedProperty entries,
            EncounterData encounter,
            string[] row,
            Header header
        )
        {
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var candidate = entries.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("_encounter").objectReferenceValue == encounter)
                {
                    entry = candidate;
                    break;
                }
            }

            if (entry == null)
            {
                entries.arraySize++;
                entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("_encounter").objectReferenceValue = encounter;
                entry.FindPropertyRelative("_requirements").ClearArray();
                entry.FindPropertyRelative("_boostIf").ClearArray();
            }

            SetInt(entry, "_firstDay", header.Get(row, "first day"), fallback: 1);
            SetInt(entry, "_lastDay", header.Get(row, "last day"));
            SetFloat(entry, "_weight", header.Get(row, "pool weight"), fallback: -1f);
            SetBool(entry, "_oncePerRun", header.Get(row, "once per run"));
            SetBool(entry, "_guaranteed", header.Get(row, "guaranteed"));
            SetFloat(entry, "_boostMultiplier", header.Get(row, "boost x"), fallback: 2f);
        }

        #endregion

        #region Field setters — blank cells leave the existing value alone
        private static void SetString(SerializedObject so, string field, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                so.FindProperty(field).stringValue = value.Trim();
        }

        private static void SetInt(
            SerializedObject so,
            string field,
            string value,
            int fallback = 0
        )
        {
            if (int.TryParse(value, out int parsed))
                so.FindProperty(field).intValue = parsed;
            else if (!string.IsNullOrWhiteSpace(value))
                so.FindProperty(field).intValue = fallback;
        }

        private static void SetFloat(SerializedObject so, string field, string value)
        {
            if (float.TryParse(value, out float parsed))
                so.FindProperty(field).floatValue = parsed;
        }

        private static void SetBool(SerializedObject so, string field, string value)
        {
            if (TryParseBool(value, out bool parsed))
                so.FindProperty(field).boolValue = parsed;
        }

        private static void SetInt(
            SerializedProperty parent,
            string field,
            string value,
            int fallback = 0
        )
        {
            if (int.TryParse(value, out int parsed))
                parent.FindPropertyRelative(field).intValue = parsed;
            else if (string.IsNullOrWhiteSpace(value))
                parent.FindPropertyRelative(field).intValue = fallback;
        }

        private static void SetFloat(
            SerializedProperty parent,
            string field,
            string value,
            float fallback
        )
        {
            parent.FindPropertyRelative(field).floatValue = float.TryParse(value, out float parsed)
                ? parsed
                : fallback;
        }

        private static void SetBool(SerializedProperty parent, string field, string value)
        {
            if (TryParseBool(value, out bool parsed))
                parent.FindPropertyRelative(field).boolValue = parsed;
        }

        private static bool TryParseBool(string value, out bool result)
        {
            result = false;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            string v = value.Trim().ToLowerInvariant();
            result = v == "true" || v == "1" || v == "yes" || v == "y";
            return result || v == "false" || v == "0" || v == "no" || v == "n";
        }

        #endregion

        #region CSV
        /// <summary>
        /// Column lookup by a loose name: the sheet's headers carry parenthetical hints
        /// ("Last Day (0=open)"), so a match is "the header starts with this key", lowercased.
        /// </summary>
        private sealed class Header
        {
            public int RowIndex;
            private readonly Dictionary<string, int> _columns = new Dictionary<string, int>();

            public static Header From(string[] row, int rowIndex)
            {
                var header = new Header { RowIndex = rowIndex };
                for (int i = 0; i < row.Length; i++)
                {
                    string key = row[i].Trim().ToLowerInvariant();
                    if (key.Length > 0 && !header._columns.ContainsKey(key))
                        header._columns[key] = i;
                }
                return header;
            }

            public bool Has(string key) => Find(key) >= 0;

            public string Get(string[] row, string key)
            {
                int column = Find(key);
                return column >= 0 && column < row.Length ? row[column].Trim() : "";
            }

            private int Find(string key)
            {
                foreach (var kv in _columns)
                    if (kv.Key.StartsWith(key, StringComparison.Ordinal))
                        return kv.Value;
                return -1;
            }
        }

        private static Header FindHeader(List<string[]> rows)
        {
            // The sheet has title and legend rows above the table, so the header is wherever
            // "Asset Name" turns up rather than always line 1.
            for (int i = 0; i < rows.Count; i++)
            {
                var header = Header.From(rows[i], i);
                if (header.Has("asset name"))
                    return header;
            }
            return null;
        }

        /// <summary>RFC 4180 enough: quoted fields, embedded commas, doubled quotes, CRLF.</summary>
        private static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (quoted)
                {
                    if (c != '"')
                        field.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                        field.Append(text[++i]); // "" inside a quoted field is one quote
                    else
                        quoted = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        quoted = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row.ToArray());
                        row.Clear();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }
            return rows;
        }

        #endregion
    }
}
