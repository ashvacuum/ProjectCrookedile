using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Crookedile.Data.Campaign;
using Crookedile.Data.Unlocks;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Every <c>[SerializeReference]</c> building block the inspector's type pickers offer:
    /// effects, passive triggers and conditions, overworld passives, run outcomes and
    /// requirements, unlock conditions. Its description, its authorable fields with tooltips,
    /// and which assets use it. Reflection-built, so it can't drift from the code. Statuses
    /// have their own tab.
    /// </summary>
    public sealed class BuildingBlocksTab : ContentTab<BuildingBlocksTab.Block>
    {
        public sealed class Block
        {
            public Type Type;
            public string Kind;
            public string Title;
            public string Description;
            public List<ContentUsage.FieldInfoRow> Fields;
            public IReadOnlyList<Object> UsedBy;
        }

        private static readonly (Type type, string kind, Func<object, string> describe)[] Kinds =
        {
            (typeof(BattleEffect), "Effect", o => ((BattleEffect)o).GetDescription()),
            (typeof(PassiveTriggerBase), "Trigger", o => ((PassiveTriggerBase)o).TriggerLabel),
            (typeof(PassiveConditionBase), "Condition", o => ((PassiveConditionBase)o).ConditionLabel),
            (typeof(OverworldPassive), "Overworld passive", o => ((OverworldPassive)o).EditorSafeDescription()),
            (typeof(RunOutcome), "Run outcome", o => ((RunOutcome)o).EditorSafeDescription()),
            (typeof(RunRequirement), "Run requirement", o => ((RunRequirement)o).EditorSafeDescription()),
            (typeof(UnlockCondition), "Unlock condition", o => ((UnlockCondition)o).Describe()),
        };

        private string _kind;

        public override string Title => "Building blocks";

        protected override string DisplayName(Block block) => block.Title;

        protected override string SearchText(Block block) => block.Type.Name + " " + block.Description;

        protected override Object AssetOf(Block block) => null;

        protected override IEnumerable<Block> Find()
        {
            var usage = ContentUsage.Build();
            foreach (var (baseType, kind, describe) in Kinds)
                foreach (Type t in baseType.Assembly.GetTypes())
                {
                    if (t.IsAbstract || t == baseType || !baseType.IsAssignableFrom(t))
                        continue;
                    string description = t.GetConstructor(Type.EmptyTypes) != null
                        ? ContentUsage.Safe(() => describe(Activator.CreateInstance(t)))
                        : "(no parameterless constructor, can't preview)";
                    yield return new Block
                    {
                        Type = t,
                        Kind = kind,
                        Title = ContentUsage.Prettify(t.Name),
                        Description = description,
                        Fields = ContentUsage.SerializedFields(t),
                        UsedBy = usage.Of(t),
                    };
                }
        }

        protected override IEnumerable<Column> BuildColumns()
        {
            yield return new Column("Block", 180, null, b => b.Title);
            yield return new Column("Kind", 110, b => b.Kind, b => b.Kind);
            yield return new Column("Fields", 48, b => b.Fields.Count.ToString(), b => b.Fields.Count);
            yield return new Column("Used by", 60, b => b.UsedBy.Count == 0 ? "unused" : b.UsedBy.Count.ToString(), b => b.UsedBy.Count);
        }

        protected override bool DrawFilters()
        {
            bool changed = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Kind", EditorStyles.miniLabel, GUILayout.Width(62));
                if (GUILayout.Toggle(_kind == null, "All", EditorStyles.miniButtonLeft) && _kind != null)
                {
                    _kind = null;
                    changed = true;
                }
                foreach (var (_, kind, _) in Kinds)
                    if (GUILayout.Toggle(_kind == kind, kind, EditorStyles.miniButtonMid) && _kind != kind)
                    {
                        _kind = kind;
                        changed = true;
                    }
                GUILayout.FlexibleSpace();
            }
            return changed;
        }

        protected override bool PassesFilters(Block block) => _kind == null || block.Kind == _kind;

        protected override IEnumerable<Issue> Audit(Block block)
        {
            if (!block.Type.IsSerializable)
                yield return Issue.Warning("Not [Serializable], so the inspector's type picker never offers it.");
            if (string.IsNullOrWhiteSpace(block.Description))
                yield return Issue.Info("Empty description: the inspector shows nothing for it.");
            if (block.UsedBy.Count == 0)
                yield return Issue.Info("Unused: no content asset picks it.");
        }

        protected override void DrawPreview(Block block)
        {
            EditorGUILayout.LabelField($"{block.Kind} · {block.Type.Name}", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(block.Description) ? "(no description)" : block.Description, MessageType.None);

            EditorGUILayout.LabelField("Authorable fields", EditorStyles.boldLabel);
            if (block.Fields.Count == 0)
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
            foreach (var field in block.Fields)
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{field.Name} : {field.Type}", EditorStyles.boldLabel, GUILayout.Width(220));
                    EditorGUILayout.LabelField(field.Tooltip, EditorStyles.wordWrappedMiniLabel);
                }

            UsedByList(block.UsedBy);
        }

        protected override void DrawSummary(IReadOnlyList<Block> all)
        {
            EditorGUILayout.LabelField($"{all.Count} building blocks. Select one for its fields and users.", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            foreach (var group in all.GroupBy(b => b.Kind))
                Bar(group.Key, group.Count(), all.Count);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Unused: {all.Count(b => b.UsedBy.Count == 0)}");
        }

        protected override IEnumerable<TabAction> Actions()
        {
            yield return new TabAction("Export CSV", ExportCsv);
        }

        /// <summary>Lists the assets using something, each a link to the asset.</summary>
        internal static void UsedByList(IReadOnlyList<Object> usedBy)
        {
            EditorGUILayout.LabelField($"Used by ({usedBy.Count})", EditorStyles.boldLabel);
            if (usedBy.Count == 0)
                EditorGUILayout.LabelField("No card, enemy, move, origin passive, ally, encounter or pool uses it.", EditorStyles.wordWrappedMiniLabel);
            foreach (var asset in usedBy)
                if (asset != null && GUILayout.Button($"{asset.name}  ({asset.GetType().Name})", EditorStyles.linkLabel))
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
        }

        /// <summary>Writes every block (and status) to Exports/authoring-bible.csv, the spreadsheet twin of this tab.</summary>
        private void ExportCsv()
        {
            var sb = new StringBuilder("Kind,Name,TypeName,Description,SerializedFields,UsageCount,UsedBy\n");
            void Row(params string[] cells) => sb.AppendLine(string.Join(",", cells.Select(Escape)));
            foreach (var block in Find().OrderBy(b => b.Kind).ThenBy(b => b.Title))
                Row(block.Kind, block.Title, block.Type.Name, block.Description, Fields(block.Fields), block.UsedBy.Count.ToString(), string.Join("; ", block.UsedBy.Select(a => a.name)));
            var usage = ContentUsage.Build();
            foreach (var status in StatusRegistry.All.OrderBy(s => s.DisplayName))
            {
                var users = usage.Of(status.GetType());
                Row("Status", status.DisplayName, status.GetType().Name, ContentUsage.Safe(() => status.Describe(1)), Fields(ContentUsage.SerializedFields(status.GetType())), users.Count.ToString(), string.Join("; ", users.Select(a => a.name)));
            }

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Exports/authoring-bible.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            EditorUtility.RevealInFinder(path);
        }

        private static string Fields(List<ContentUsage.FieldInfoRow> fields) =>
            string.Join(" | ", fields.Select(f => string.IsNullOrEmpty(f.Tooltip) ? $"{f.Name} ({f.Type})" : $"{f.Name} ({f.Type}): {f.Tooltip.Replace("\n", " ")}"));

        private static string Escape(string cell) =>
            string.IsNullOrEmpty(cell) ? ""
            : cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n") ? "\"" + cell.Replace("\"", "\"\"") + "\""
            : cell;
    }
}
