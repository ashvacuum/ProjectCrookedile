using System.Collections.Generic;
using System.IO;
using System.Text;
using Crookedile.Data.Cards;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Exports every CardData asset to a CSV for spreadsheet auditing
    /// (Crookedile → Export → Cards CSV). One row per card; the Class column comes from the
    /// asset's folder under Resources/Cards (FaithLeader/Celebrity/NepoBaby/Curses/...), so
    /// filtering by class is a one-click spreadsheet filter.
    /// </summary>
    public static class CardCsvExporter
    {
        private const string OutputPath = "docs/card-audit.csv";

        [MenuItem("Crookedile/Export/Cards CSV")]
        public static void Export()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData");
            var rows = new List<string[]>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null)
                    continue;

                rows.Add(
                    new[]
                    {
                        ClassFromPath(path),
                        card.CardName,
                        card.CardType.ToString(),
                        card.Rarity.ToString(),
                        DescribeCosts(card),
                        card.IsStarterCard ? "yes" : "",
                        card.IsUpgraded ? "is upgrade"
                        : card.CanUpgrade ? "yes"
                        : "MISSING",
                        card.IsActivatedPassive ? "yes" : "",
                        card.IsUnplayable ? "yes" : "",
                        card.IsGeneratedOnly ? "yes" : "",
                        card.GetInnateRetain(useUpgraded: false) ? "yes" : "",
                        (card.Effects?.Count ?? 0).ToString(),
                        (card.Passives?.Count ?? 0).ToString(),
                        string.Join(" ", card.Tags ?? new List<string>()),
                        Flatten(card.Description),
                        Flatten(card.ConfigurationNotes),
                        path,
                    }
                );
            }

            // Stable order: class, then name — diffs stay readable between exports.
            rows.Sort(
                (a, b) =>
                {
                    int c = string.CompareOrdinal(a[0], b[0]);
                    return c != 0 ? c : string.CompareOrdinal(a[1], b[1]);
                }
            );

            var sb = new StringBuilder();
            sb.AppendLine(
                "Class,Name,Type,Rarity,Cost,Starter,Upgrade,ActivatedPassive,Unplayable,"
                    + "GeneratedOnly,InnateRetain,Effects,Passives,Tags,Description,ConfigNotes,AssetPath"
            );
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(Escape(row[i]));
                }
                sb.AppendLine();
            }

            string fullPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                OutputPath
            );
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[CardCsvExporter] Exported {rows.Count} cards → {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        /// <summary>The folder segment after "Cards/" — the card's class/pool (e.g. FaithLeader).</summary>
        private static string ClassFromPath(string assetPath)
        {
            string[] parts = assetPath.Replace('\\', '/').Split('/');
            for (int i = 0; i < parts.Length - 1; i++)
                if (parts[i] == "Cards")
                    return parts[i + 1].EndsWith(".asset") ? "(root)" : parts[i + 1];
            return "(outside Cards/)";
        }

        private static string DescribeCosts(CardData card)
        {
            var costs = card.GetCosts(useUpgraded: false);
            if (costs == null || costs.Count == 0)
                return "Free";
            var parts = new List<string>();
            foreach (var cost in costs)
                if (cost != null)
                    parts.Add($"{cost.BaseAmount} {cost.CostType}");
            return string.Join(" + ", parts);
        }

        private static string Flatten(string text) =>
            string.IsNullOrEmpty(text) ? "" : text.Replace("\r", "").Replace("\n", " | ");

        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }
    }
}
