using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>Every <see cref="AllyData"/>, the game's relics: what it does in battle and on the map.</summary>
    public sealed class AlliesTab : ContentTab<AllyData>
    {
        private CardRarity? _rarity;
        private Dictionary<Object, List<Issue>> _issues;

        public override string Title => "Allies";

        protected override string DisplayName(AllyData ally) =>
            string.IsNullOrWhiteSpace(ally.AllyName) ? $"({ally.name})" : ally.AllyName;

        protected override string SearchText(AllyData ally) => ContentUsage.Safe(() => ally.AutoDescription);

        protected override void OnReloaded(IReadOnlyList<AllyData> all) =>
            _issues = ProviderAudit.ByAsset(new ContentChecks.AlliesProvider());

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Ally", 160, null, a => DisplayName(a));
            yield return new Column("Rarity", 70, a => a.Rarity.ToString(), a => a.Rarity);
            yield return new Column("Battle", 50, a => Blank(a.Passives.Count), a => a.Passives.Count);
            yield return new Column("Map", 44, a => Blank(a.OverworldPassives.Count), a => a.OverworldPassives.Count);
            yield return new Column("Icon", 40, a => a.Icon != null ? "✓" : "—", a => a.Icon != null);
        }

        protected override bool DrawFilters() => EnumFilter("Rarity", ref _rarity);

        protected override bool PassesFilters(AllyData ally) => !_rarity.HasValue || ally.Rarity == _rarity.Value;

        protected override IEnumerable<Issue> Audit(AllyData ally) => ProviderAudit.For(_issues, ally);

        protected override void DrawPreview(AllyData ally)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Picture(ally.Icon, 64);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"{DisplayName(ally)}   ·   {ally.Rarity}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(ContentUsage.Safe(() => ally.AutoDescription), EditorStyles.wordWrappedLabel);
                }
            }
        }

        protected override void DrawSummary(IReadOnlyList<AllyData> all)
        {
            EditorGUILayout.LabelField($"{all.Count} allies.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            foreach (var group in all.GroupBy(a => a.Rarity).OrderBy(g => g.Key))
                Bar(group.Key.ToString(), group.Count(), all.Count);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Battle passives: {all.Count(a => a.Passives.Count > 0)}   ·   Map passives: {all.Count(a => a.OverworldPassives.Count > 0)}");
            EditorGUILayout.LabelField($"No icon: {all.Count(a => a.Icon == null)}");
        }

        private static string Blank(int value) => value == 0 ? "" : value.ToString();
    }
}
