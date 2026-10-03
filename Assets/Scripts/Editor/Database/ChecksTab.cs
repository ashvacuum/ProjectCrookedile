using System.Collections.Generic;
using System.Linq;
using Crookedile.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Project-wide audits that aren't one asset type's tab: readiness, asset names, shared art,
    /// card visuals, intents, audio and VFX, localization, rewards, UI references, enemy moves,
    /// pools and origin passives. One row per finding source, from
    /// <see cref="ContentChecks.CheckProviders"/>.
    /// </summary>
    public sealed class ChecksTab : ContentTab<ChecksTab.Check>
    {
        public sealed class Check
        {
            public string Category;
            public ContentChecks.Row Row;
        }

        private List<string> _categories = new List<string>();
        private int _category; // 0 = all

        public override string Title => "Checks";

        protected override string DisplayName(Check check) => check.Row.Label ?? "(unnamed)";

        protected override string SearchText(Check check) => check.Category + " " + check.Row.Detail;

        protected override Object AssetOf(Check check) => check.Row.Context;

        protected override IEnumerable<Check> Find()
        {
            foreach (var provider in ContentChecks.CheckProviders())
            {
                List<ContentChecks.Row> rows;
                try
                {
                    rows = provider.Rows().ToList();
                }
                catch (System.Exception e)
                {
                    rows = new List<ContentChecks.Row>
                    {
                        new ContentChecks.Row("(check failed)", e.Message, null, new List<ContentChecks.AuditIssue>
                        {
                            new ContentChecks.AuditIssue(ContentChecks.Severity.Error, e.Message),
                        }),
                    };
                }
                foreach (var row in rows)
                    yield return new Check { Category = provider.Category, Row = row };
            }
        }

        protected override void OnReloaded(IReadOnlyList<Check> all) =>
            _categories = new[] { "All" }.Concat(all.Select(c => c.Category).Distinct()).ToList();

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Item", 180, null, c => DisplayName(c));
            yield return new Column("Check", 110, c => c.Category, c => c.Category);
            yield return new Column("Detail", 220, c => c.Row.Detail, c => c.Row.Detail ?? "");
        }

        protected override bool DrawFilters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Check", EditorStyles.miniLabel, GUILayout.Width(62));
                int picked = EditorGUILayout.Popup(_category, _categories.ToArray(), GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                if (picked == _category)
                    return false;
                _category = picked;
                return true;
            }
        }

        protected override bool PassesFilters(Check check) =>
            _category <= 0 || _category >= _categories.Count || check.Category == _categories[_category];

        protected override IEnumerable<Issue> Audit(Check check) => check.Row.Issues.Select(ProviderAudit.ToIssue);

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Rename valid asset names", () =>
                EditorUtility.DisplayDialog("Asset names", ContentAssetNaming.RenameValidAssets(), "OK"));
        }

        protected override void DrawPreview(Check check)
        {
            EditorGUILayout.LabelField(check.Category, EditorStyles.miniLabel);
            if (!string.IsNullOrEmpty(check.Row.Detail))
                EditorGUILayout.HelpBox(check.Row.Detail, MessageType.None);
            if (check.Row.Thumbnail != null)
                Picture(check.Row.Thumbnail, 48);
            if (check.Category == ContentAssetNaming.CATEGORY && check.Row.Context != null
                && check.Row.Issues.Any(i => i.Severity == ContentChecks.Severity.Warning)
                && GUILayout.Button("Rename to its authored name", GUILayout.Width(200)))
            {
                var asset = check.Row.Context;
                Later(() =>
                {
                    string error = ContentAssetNaming.Rename(asset);
                    if (!string.IsNullOrEmpty(error))
                        EditorUtility.DisplayDialog("Could not rename asset", error, "OK");
                    Reload();
                });
            }
        }

        protected override void DrawSummary(IReadOnlyList<Check> all)
        {
            EditorGUILayout.LabelField("Project-wide checks. Pick one in the filter, or use Problems only.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            foreach (var group in all.GroupBy(c => c.Category))
            {
                int problems = group.Count(c => c.Row.Issues.Any(i => i.Severity >= ContentChecks.Severity.Warning));
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(group.Key, GUILayout.Width(160));
                    GUILayout.Label($"{group.Count()} rows", EditorStyles.miniLabel, GUILayout.Width(70));
                    GUILayout.Label(problems == 0 ? "OK" : $"{problems} with problems", problems == 0 ? EditorStyles.miniLabel : EditorStyles.boldLabel);
                }
            }
        }
    }
}
