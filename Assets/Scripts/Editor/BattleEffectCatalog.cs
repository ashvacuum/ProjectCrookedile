using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Reflection-built registry of every concrete <see cref="BattleEffect"/> subclass. Auto-discovers
    /// effects (nothing to hand-author), so it stays in sync with the code. Used by the Content Audit
    /// to verify each effect is authorable ([Serializable]) and self-describing, and by the
    /// "List Battle Effects" menu as a quick reference of what the card/enemy authors can pick.
    /// </summary>
    public static class BattleEffectCatalog
    {
        public readonly struct EffectInfo
        {
            public readonly Type Type;
            public readonly string DisplayName;
            public readonly bool Serializable;
            public readonly string Description;

            public EffectInfo(Type type, string displayName, bool serializable, string description)
            {
                Type = type;
                DisplayName = displayName;
                Serializable = serializable;
                Description = description;
            }
        }

        /// <summary>All concrete BattleEffect subclasses, ordered by display name.</summary>
        public static IReadOnlyList<EffectInfo> All()
        {
            Type baseType = typeof(BattleEffect);
            var result = new List<EffectInfo>();

            foreach (Type t in baseType.Assembly.GetTypes())
            {
                if (t.IsAbstract || t == baseType || !baseType.IsAssignableFrom(t))
                    continue;

                bool serializable = t.IsSerializable;
                string description = "";
                try
                {
                    if (serializable && t.GetConstructor(Type.EmptyTypes) != null)
                    {
                        var instance = (BattleEffect)Activator.CreateInstance(t);
                        description = instance.GetDescription() ?? "";
                    }
                }
                catch
                {
                    description = "(GetDescription threw)";
                }

                result.Add(new EffectInfo(t, Prettify(t.Name), serializable, description));
            }

            return result.OrderBy(e => e.DisplayName).ToList();
        }

        /// <summary>"ApplyPressureEffect" → "Deal Damage".</summary>
        private static string Prettify(string typeName)
        {
            string s = Regex.Replace(typeName, "([a-z0-9])([A-Z])", "$1 $2");
            if (s.EndsWith(" Effect"))
                s = s.Substring(0, s.Length - " Effect".Length);
            return s;
        }

        [MenuItem("Crookedile/List Battle Effects")]
        public static void ListToConsole()
        {
            var all = All();
            var sb = new StringBuilder();
            foreach (var e in all)
            {
                string flags = e.Serializable ? "" : "  [NOT SERIALIZABLE]";
                sb.AppendLine($"  {e.DisplayName} ({e.Type.Name}){flags}  —  {e.Description}");
            }
            Debug.Log($"[BattleEffectCatalog] {all.Count} effects:\n{sb}");
        }
    }
}
