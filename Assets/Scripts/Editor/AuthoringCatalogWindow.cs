using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Authoring Catalog — a browsable reference of every polymorphic <c>[SerializeReference]</c>
    /// building block the inspector lets you pick: BattleEffects, passive Triggers, passive
    /// Conditions, and StatusBehaviors. For each it shows the human description and the serialized
    /// fields (with their tooltips) — the same info the type-picker dropdown hides behind a click.
    /// Reflection-built, so it can never drift from the code.
    ///
    /// Menu: Crookedile → Authoring Catalog.
    /// </summary>
    public class AuthoringCatalogWindow : OdinMenuEditorWindow
    {
        [MenuItem("Crookedile/Authoring Catalog")]
        public static void ShowWindow()
        {
            var win = GetWindow<AuthoringCatalogWindow>("Authoring Catalog");
            win.minSize = new Vector2(640, 480);
            win.Show();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(false);
            tree.Config.DrawSearchToolbar = true;

            var usage = BuildUsageIndex();

            AddGroup<BattleEffect>(tree, "Effects", e => Safe(() => e.GetDescription()), usage);
            AddGroup<PassiveTriggerBase>(tree, "Triggers", t => Safe(() => t.TriggerLabel), usage);
            AddGroup<PassiveConditionBase>(
                tree,
                "Conditions",
                c => Safe(() => c.ConditionLabel),
                usage
            );

            // StatusBehaviors come from the registry's canonical instances (no reflection needed).
            var iconMap = Resources.Load<Crookedile.Data.Battle.StatusEffectIconMapSO>(
                "StatusEffectIconMap"
            );
            foreach (var b in StatusRegistry.All.OrderBy(b => b.DisplayName))
            {
                Sprite icon = null;
                bool mapped =
                    iconMap != null && iconMap.TryGet(b.Id, out icon, out _) && icon != null;

                var usages = UsagesFor(usage, b.GetType());
                var entry = new Entry
                {
                    Title = b.DisplayName,
                    TypeName = b.GetType().Name,
                    Description = Safe(() => b.Describe(1)),
                    Extra = new List<(string, string)>
                    {
                        ("Id", b.Id),
                        ("IsDebuff", b.IsDebuff.ToString()),
                        ("Category", b.Category.ToString()),
                        ("CountsTowardPacify", b.CountsTowardPacify.ToString()),
                    },
                    Fields = SerializedFields(b.GetType()),
                    Usages = usages,
                    Icon = icon,
                    IconMap = iconMap,
                    IsStatus = true,
                };
                tree.Add(
                    $"Statuses/{b.DisplayName} ({usages.Count}){(mapped ? "" : "  ⚠ no icon")}",
                    entry
                );
            }

            return tree;
        }

        #region CSV export

        /// <summary>
        /// Writes the whole catalog (Effects, Triggers, Conditions, Statuses) to
        /// docs/authoring-bible.csv — the spreadsheet twin of this window.
        /// </summary>
        [MenuItem("Crookedile/Export/Authoring Bible CSV")]
        public static void ExportCsv()
        {
            var rows = new List<string[]>();
            var usage = BuildUsageIndex();

            CollectGroup<BattleEffect>(rows, "Effect", e => Safe(() => e.GetDescription()), usage);
            CollectGroup<PassiveTriggerBase>(
                rows,
                "Trigger",
                t => Safe(() => t.TriggerLabel),
                usage
            );
            CollectGroup<PassiveConditionBase>(
                rows,
                "Condition",
                c => Safe(() => c.ConditionLabel),
                usage
            );

            foreach (var b in StatusRegistry.All.OrderBy(b => b.DisplayName))
            {
                var usages = UsagesFor(usage, b.GetType());
                rows.Add(
                    new[]
                    {
                        "Status",
                        b.DisplayName,
                        b.GetType().Name,
                        Safe(() => b.Describe(1)),
                        FlattenFields(SerializedFields(b.GetType())),
                        $"Id={b.Id}; IsDebuff={b.IsDebuff}; Category={b.Category}; "
                            + $"Pacify={b.CountsTowardPacify}",
                        usages.Count.ToString(),
                        JoinUsages(usages),
                    }
                );
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Kind,Name,TypeName,Description,SerializedFields,Extra,UsageCount,UsedBy");
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                        sb.Append(',');
                    sb.Append(EscapeCsv(row[i]));
                }
                sb.AppendLine();
            }

            string fullPath = System.IO.Path.Combine(
                System.IO.Directory.GetParent(Application.dataPath).FullName,
                "docs/authoring-bible.csv"
            );
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath));
            System.IO.File.WriteAllText(fullPath, sb.ToString(), System.Text.Encoding.UTF8);

            Debug.Log($"[AuthoringCatalog] Exported {rows.Count} entries → {fullPath}");
            EditorUtility.RevealInFinder(fullPath);
        }

        // Mirror of AddGroup, collecting CSV rows instead of tree entries.
        private static void CollectGroup<T>(
            List<string[]> rows,
            string kind,
            Func<T, string> describe,
            Dictionary<Type, List<UnityEngine.Object>> usage
        )
            where T : class
        {
            Type baseType = typeof(T);
            var group = new List<string[]>();

            foreach (Type t in baseType.Assembly.GetTypes())
            {
                if (t.IsAbstract || t == baseType || !baseType.IsAssignableFrom(t))
                    continue;

                string desc =
                    t.GetConstructor(Type.EmptyTypes) != null
                        ? Safe(() => describe((T)Activator.CreateInstance(t)) ?? "")
                        : "(no parameterless constructor)";

                var usages = UsagesFor(usage, t);
                group.Add(
                    new[]
                    {
                        kind,
                        Prettify(t.Name),
                        t.Name,
                        desc,
                        FlattenFields(SerializedFields(t)),
                        t.IsSerializable ? "" : "NOT [Serializable] — hidden from pickers",
                        usages.Count.ToString(),
                        JoinUsages(usages),
                    }
                );
            }

            group.Sort((a, b) => string.CompareOrdinal(a[1], b[1]));
            rows.AddRange(group);
        }

        private static string JoinUsages(List<UnityEngine.Object> usages)
        {
            if (usages == null || usages.Count == 0)
                return "";
            var names = new List<string>();
            foreach (var a in usages)
                if (a != null)
                    names.Add(a.name);
            return string.Join("; ", names);
        }

        private static string FlattenFields(List<FieldRow> fields)
        {
            var parts = new List<string>();
            foreach (var f in fields)
                parts.Add(
                    string.IsNullOrEmpty(f.Tooltip)
                        ? $"{f.Name} ({f.Type})"
                        : $"{f.Name} ({f.Type}): {f.Tooltip.Replace("\n", " ")}"
                );
            return string.Join(" | ", parts);
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }

        #endregion

        // Reflects every concrete subclass of T, builds a catalog entry from a default instance.
        private static void AddGroup<T>(
            OdinMenuTree tree,
            string group,
            Func<T, string> describe,
            Dictionary<Type, List<UnityEngine.Object>> usage
        )
            where T : class
        {
            Type baseType = typeof(T);
            var entries = new List<Entry>();

            foreach (Type t in baseType.Assembly.GetTypes())
            {
                if (t.IsAbstract || t == baseType || !baseType.IsAssignableFrom(t))
                    continue;

                string desc = "";
                if (t.GetConstructor(Type.EmptyTypes) != null)
                {
                    try
                    {
                        desc = describe((T)Activator.CreateInstance(t)) ?? "";
                    }
                    catch
                    {
                        desc = "(description threw)";
                    }
                }
                else
                {
                    desc = "(no parameterless constructor — can't preview)";
                }

                entries.Add(
                    new Entry
                    {
                        Title = Prettify(t.Name),
                        TypeName = t.Name,
                        Description = desc,
                        Serializable = t.IsSerializable,
                        Fields = SerializedFields(t),
                        Usages = UsagesFor(usage, t),
                    }
                );
            }

            foreach (var e in entries.OrderBy(e => e.Title))
                tree.Add($"{group}/{e.Title} ({e.Usages.Count})", e);
        }

        #region Usage index

        private static readonly Type[] ScannedAssetTypes =
        {
            typeof(Crookedile.Data.Cards.CardData),
            typeof(Crookedile.Data.Enemy.EnemyMoveData),
            typeof(Crookedile.Data.Enemy.EnemyData),
            typeof(Crookedile.Data.OriginPassive),
            typeof(Crookedile.Data.RelicData),
        };

        /// <summary>
        /// Scans every content asset (cards, enemy moves, enemies, origin passives, relics) and
        /// records which concrete effect/trigger/condition/status types each one contains,
        /// including nested references (a StatusBehavior inside an ApplyStatusBehaviorEffect
        /// inside a passive). Nested asset references are NOT followed — an EnemyData that uses
        /// an EnemyMoveData attributes the move's contents to the move, not the enemy.
        /// </summary>
        private static Dictionary<Type, List<UnityEngine.Object>> BuildUsageIndex()
        {
            var index = new Dictionary<Type, HashSet<UnityEngine.Object>>();

            foreach (Type assetType in ScannedAssetTypes)
            {
                foreach (string guid in AssetDatabase.FindAssets($"t:{assetType.Name}"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    );
                    if (asset == null || !assetType.IsInstanceOfType(asset))
                        continue;
                    Walk(asset, asset, index, new HashSet<object>());
                }
            }

            var result = new Dictionary<Type, List<UnityEngine.Object>>();
            foreach (var kvp in index)
                result[kvp.Key] = kvp.Value.OrderBy(a => a.name).ToList();
            return result;
        }

        private static void Walk(
            object node,
            UnityEngine.Object asset,
            Dictionary<Type, HashSet<UnityEngine.Object>> index,
            HashSet<object> seen
        )
        {
            if (node == null)
                return;
            Type t = node.GetType();
            if (t.IsPrimitive || t.IsEnum || node is string)
                return;
            // Don't follow references INTO other assets — they get scanned on their own.
            if (node is UnityEngine.Object && !ReferenceEquals(node, asset))
                return;
            if (!seen.Add(node))
                return;

            if (
                node is BattleEffect
                || node is StatusBehavior
                || node is PassiveTriggerBase
                || node is PassiveConditionBase
            )
            {
                if (!index.TryGetValue(t, out var set))
                    index[t] = set = new HashSet<UnityEngine.Object>();
                set.Add(asset);
            }

            foreach (
                var f in t.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
            )
            {
                if (f.IsStatic || f.Name.Contains("<"))
                    continue;
                bool serialized = f.IsPublic
                    ? f.GetCustomAttribute<NonSerializedAttribute>() == null
                    : f.GetCustomAttribute<SerializeField>() != null
                        || f.GetCustomAttribute<SerializeReference>() != null;
                if (!serialized)
                    continue;
                if (f.FieldType.IsPrimitive || f.FieldType.IsEnum || f.FieldType == typeof(string))
                    continue;

                object value;
                try
                {
                    value = f.GetValue(node);
                }
                catch
                {
                    continue;
                }

                if (value is System.Collections.IEnumerable list && !(value is string))
                {
                    foreach (var item in list)
                        Walk(item, asset, index, seen);
                }
                else
                {
                    Walk(value, asset, index, seen);
                }
            }
        }

        private static List<UnityEngine.Object> UsagesFor(
            Dictionary<Type, List<UnityEngine.Object>> usage,
            Type t
        ) => usage.TryGetValue(t, out var list) ? list : new List<UnityEngine.Object>();

        #endregion

        /// <summary>Public instance fields + private ones marked [SerializeField], with tooltips.</summary>
        private static List<FieldRow> SerializedFields(Type t)
        {
            var rows = new List<FieldRow>();
            foreach (
                var f in t.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                )
            )
            {
                if (f.IsStatic || f.Name.Contains("<") || f.Name.Contains("k__BackingField"))
                    continue;
                bool serialized = f.IsPublic
                    ? f.GetCustomAttribute<NonSerializedAttribute>() == null
                    : f.GetCustomAttribute<SerializeField>() != null;
                if (!serialized)
                    continue;

                rows.Add(
                    new FieldRow
                    {
                        Name = f.Name.TrimStart('_'),
                        Type = Prettify(f.FieldType.Name),
                        Tooltip = f.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "",
                    }
                );
            }
            return rows;
        }

        private static string Safe(Func<string> f)
        {
            try
            {
                return f() ?? "";
            }
            catch
            {
                return "(threw)";
            }
        }

        private static string Prettify(string typeName)
        {
            string s = Regex.Replace(typeName, "([a-z0-9])([A-Z])", "$1 $2");
            if (s.EndsWith(" Effect"))
                s = s.Substring(0, s.Length - " Effect".Length);
            return s;
        }

        private struct FieldRow
        {
            public string Name;
            public string Type;
            public string Tooltip;
        }

        // One catalog page, drawn by Odin via [OnInspectorGUI].
        private sealed class Entry
        {
            public string Title;
            public string TypeName;
            public string Description;
            public bool Serializable = true;
            public List<FieldRow> Fields;
            public List<(string label, string value)> Extra;
            public List<UnityEngine.Object> Usages;
            public Sprite Icon;
            public UnityEngine.Object IconMap;
            public bool IsStatus;

            [Sirenix.OdinInspector.OnInspectorGUI]
            private void Draw()
            {
                SirenixEditorGUI.Title(Title, TypeName, TextAlignment.Left, true);

                if (IsStatus)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (Icon != null)
                    {
                        GUILayout.Label(
                            AssetPreview.GetAssetPreview(Icon) ?? Icon.texture,
                            GUILayout.Width(48),
                            GUILayout.Height(48)
                        );
                    }
                    else
                    {
                        SirenixEditorGUI.MessageBox(
                            "No icon in StatusEffectIconMap — run the seeder, then assign a sprite.",
                            MessageType.Warning
                        );
                    }
                    if (IconMap != null && GUILayout.Button("Open Icon Map", GUILayout.Width(110)))
                    {
                        Selection.activeObject = IconMap;
                        EditorGUIUtility.PingObject(IconMap);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (!Serializable)
                    SirenixEditorGUI.MessageBox(
                        "Not [Serializable] — won't appear in the inspector type-picker.",
                        MessageType.Warning
                    );

                SirenixEditorGUI.MessageBox(
                    string.IsNullOrWhiteSpace(Description) ? "(no description)" : Description,
                    MessageType.Info
                );

                if (Extra != null && Extra.Count > 0)
                {
                    SirenixEditorGUI.BeginBox();
                    SirenixEditorGUI.Title("Properties", "", TextAlignment.Left, false);
                    foreach (var (label, value) in Extra)
                        Field(label, value);
                    SirenixEditorGUI.EndBox();
                }

                SirenixEditorGUI.BeginBox();
                SirenixEditorGUI.Title("Serialized fields", "", TextAlignment.Left, false);
                if (Fields == null || Fields.Count == 0)
                    EditorGUILayout.LabelField("(no authorable fields)", EditorStyles.miniLabel);
                foreach (var f in Fields)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"{f.Name}  :  {f.Type}",
                        EditorStyles.boldLabel,
                        GUILayout.Width(220)
                    );
                    EditorGUILayout.LabelField(
                        f.Tooltip,
                        EditorStyles.wordWrappedMiniLabel
                    );
                    EditorGUILayout.EndHorizontal();
                }
                SirenixEditorGUI.EndBox();

                SirenixEditorGUI.BeginBox();
                SirenixEditorGUI.Title(
                    $"Used by ({Usages?.Count ?? 0})",
                    "",
                    TextAlignment.Left,
                    false
                );
                if (Usages == null || Usages.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        "UNUSED — no card, enemy move, enemy, origin passive, or relic references this.",
                        EditorStyles.wordWrappedMiniLabel
                    );
                }
                else
                {
                    foreach (var asset in Usages)
                    {
                        if (asset == null)
                            continue;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(
                            $"{asset.name}  ({asset.GetType().Name})",
                            GUILayout.MinWidth(200)
                        );
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                SirenixEditorGUI.EndBox();
            }

            private static void Field(string label, string value)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.Width(180));
                EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
