using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Cards;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// The playable classes, one row per <see cref="OriginType"/>: their entry in
    /// <see cref="OriginDatabase"/> (portrait, passive, starter deck, campaign start) and its audit.
    /// Editing happens in the database asset's inspector below the preview.
    /// </summary>
    public sealed class OriginsTab : ContentTab<OriginsTab.Origin>
    {
        public sealed class Origin
        {
            public OriginType Type;
            public bool HasEntry;
            public OriginDatabase.Entry Entry;
        }

        private OriginDatabase _db;

        public override string Title => "Origins";

        protected override string DisplayName(Origin origin) =>
            origin.HasEntry && !string.IsNullOrWhiteSpace(origin.Entry.DisplayName) ? origin.Entry.DisplayName : origin.Type.ToString();

        protected override Object AssetOf(Origin origin) => _db;

        protected override IEnumerable<Origin> Find()
        {
            _db = FindAssets<OriginDatabase>().FirstOrDefault();
            foreach (OriginType type in System.Enum.GetValues(typeof(OriginType)))
            {
                OriginDatabase.Entry entry = default;
                bool has = _db != null && _db.TryGet(type, out entry);
                yield return new Origin { Type = type, HasEntry = has, Entry = entry };
            }
        }

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Origin", 120, null, o => DisplayName(o));
            yield return new Column("Passive", 120, o => PassiveName(o) ?? "—", o => PassiveName(o) ?? "");
            yield return new Column("Deck", 44, o => DeckSize(o).ToString(), o => DeckSize(o));
            yield return new Column("AP", 32, o => o.HasEntry ? o.Entry.MaxActionPoints.ToString() : "", o => o.Entry.MaxActionPoints);
            yield return new Column("Funds", 46, o => o.HasEntry ? o.Entry.StartingFunds.ToString() : "", o => o.Entry.StartingFunds);
            yield return new Column("Cred.", 42, o => o.HasEntry ? o.Entry.StartingCredibility.ToString() : "", o => o.Entry.StartingCredibility);
        }

        protected override IEnumerable<Issue> Audit(Origin origin)
        {
            if (_db == null)
            {
                yield return Issue.Error("No OriginDatabase asset.");
                yield break;
            }
            if (!origin.HasEntry)
            {
                yield return Issue.Error("No entry in OriginDatabase.");
                yield break;
            }
            var e = origin.Entry;
            if (string.IsNullOrWhiteSpace(e.DisplayName))
                yield return Issue.Warning("No display name.");
            if (string.IsNullOrWhiteSpace(e.Description))
                yield return Issue.Info("No description.");
            if (e.Passive == null)
                yield return Issue.Warning("No class passive linked.");
            if (e.Portrait == null)
                yield return Issue.Info("No portrait.");
            if (e.StarterDeck == null || e.StarterDeck.Count == 0)
                yield return Issue.Warning("No authored starter deck: runs fall back to one of each starter-tagged card.");
            else if (e.StarterDeck.Any(s => s == null || s.Card == null || s.Count <= 0))
                yield return Issue.Error("A starter-deck row has no card or a count of 0.");
        }

        protected override void DrawPreview(Origin origin)
        {
            if (!origin.HasEntry)
                return;
            var e = origin.Entry;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Picture(e.Portrait, 90);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(DisplayName(origin), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(e.Description ?? "", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(
                        $"Passive: {(e.Passive != null ? e.Passive.PassiveName : "none")} · {e.MaxActionPoints} AP · starts with {e.StartingFunds} Funds, {e.StartingCredibility} Credibility",
                        EditorStyles.wordWrappedMiniLabel
                    );
                }
            }
            EditorGUILayout.LabelField($"Starter deck ({DeckSize(origin)})", EditorStyles.boldLabel);
            if (e.StarterDeck != null)
                foreach (var row in e.StarterDeck.Where(r => r?.Card != null))
                    if (GUILayout.Button($"{row.Count} × {row.Card.CardName}", EditorStyles.linkLabel))
                        EditorGUIUtility.PingObject(row.Card);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("OriginDatabase (this class's entry is in the list below)", EditorStyles.boldLabel);
        }

        private static int DeckSize(Origin origin) =>
            origin.HasEntry && origin.Entry.StarterDeck != null
                ? origin.Entry.StarterDeck.Where(r => r?.Card != null).Sum(r => r.Count)
                : 0;

        private static string PassiveName(Origin origin) =>
            origin.HasEntry && origin.Entry.Passive != null ? origin.Entry.Passive.PassiveName : null;
    }
}
