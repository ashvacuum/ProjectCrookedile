using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crookedile.Data.Campaign;
using Crookedile.Data.Unlocks;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.Editor.Database
{
    /// <summary>
    /// Which content assets use which <c>[SerializeReference]</c> building block (an effect, a
    /// trigger, a status, a run outcome, ...), found by walking every content asset's serialized
    /// graph. References into other assets are not followed: an enemy that uses a move attributes
    /// the move's contents to the move, not the enemy. Built on demand; cheap enough to rebuild
    /// on every reload.
    /// </summary>
    public sealed class ContentUsage
    {
        private static readonly Type[] ScannedAssetTypes =
        {
            typeof(Data.Cards.CardData),
            typeof(Data.Enemy.EnemyMoveData),
            typeof(Data.Enemy.EnemyData),
            typeof(Data.OriginPassive),
            typeof(Data.AllyData),
            typeof(EncounterData),
            typeof(EncounterPoolData),
        };

        /// <summary>The building-block base types the scan records.</summary>
        public static readonly Type[] BlockTypes =
        {
            typeof(BattleEffect),
            typeof(PassiveTriggerBase),
            typeof(PassiveConditionBase),
            typeof(StatusBehavior),
            typeof(OverworldPassive),
            typeof(RunOutcome),
            typeof(RunRequirement),
            typeof(UnlockCondition),
        };

        private readonly Dictionary<Type, List<Object>> _byType;

        private ContentUsage(Dictionary<Type, List<Object>> byType) => _byType = byType;

        /// <summary>Assets that contain at least one instance of <paramref name="type"/>, by name.</summary>
        public IReadOnlyList<Object> Of(Type type) =>
            _byType.TryGetValue(type, out var list) ? list : (IReadOnlyList<Object>)Array.Empty<Object>();

        public static ContentUsage Build()
        {
            var index = new Dictionary<Type, HashSet<Object>>();
            foreach (Type assetType in ScannedAssetTypes)
                foreach (string guid in AssetDatabase.FindAssets("t:" + assetType.Name))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset != null && assetType.IsInstanceOfType(asset))
                        Walk(asset, asset, index, new HashSet<object>());
                }
            return new ContentUsage(index.ToDictionary(p => p.Key, p => p.Value.OrderBy(a => a.name).ToList()));
        }

        private static void Walk(object node, Object asset, Dictionary<Type, HashSet<Object>> index, HashSet<object> seen)
        {
            if (node == null)
                return;
            Type t = node.GetType();
            if (t.IsPrimitive || t.IsEnum || node is string)
                return;
            if (node is Object && !ReferenceEquals(node, asset))
                return;
            if (!seen.Add(node))
                return;

            if (BlockTypes.Any(b => b.IsInstanceOfType(node)))
            {
                if (!index.TryGetValue(t, out var set))
                    index[t] = set = new HashSet<Object>();
                set.Add(asset);
            }

            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.IsStatic || f.Name.Contains("<"))
                    continue;
                bool serialized = f.IsPublic
                    ? f.GetCustomAttribute<NonSerializedAttribute>() == null
                    : f.GetCustomAttribute<SerializeField>() != null || f.GetCustomAttribute<SerializeReference>() != null;
                if (!serialized || f.FieldType.IsPrimitive || f.FieldType.IsEnum || f.FieldType == typeof(string))
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
                    foreach (var item in list)
                        Walk(item, asset, index, seen);
                else
                    Walk(value, asset, index, seen);
            }
        }

        // ---- Reflection helpers shared by the building-block tabs --------------------------

        public readonly struct FieldInfoRow
        {
            public readonly string Name;
            public readonly string Type;
            public readonly string Tooltip;

            public FieldInfoRow(string name, string type, string tooltip)
            {
                Name = name;
                Type = type;
                Tooltip = tooltip;
            }
        }

        /// <summary>Public instance fields and private ones marked [SerializeField], with tooltips.</summary>
        public static List<FieldInfoRow> SerializedFields(Type t) =>
            t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsStatic && !f.Name.Contains("<"))
                .Where(f => f.IsPublic ? f.GetCustomAttribute<NonSerializedAttribute>() == null : f.GetCustomAttribute<SerializeField>() != null)
                .Select(f => new FieldInfoRow(f.Name.TrimStart('_'), Prettify(f.FieldType.Name), f.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? ""))
                .ToList();

        /// <summary>"ApplyOpinionEffect" → "Apply Opinion".</summary>
        public static string Prettify(string typeName)
        {
            string s = Regex.Replace(typeName, "([a-z0-9])([A-Z])", "$1 $2");
            return s.EndsWith(" Effect") ? s.Substring(0, s.Length - " Effect".Length) : s;
        }

        public static string Safe(Func<string> describe)
        {
            try
            {
                return describe() ?? "";
            }
            catch
            {
                return "(description threw)";
            }
        }
    }
}
