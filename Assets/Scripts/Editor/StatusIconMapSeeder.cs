using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Data.Battle;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Seeds the existing <see cref="StatusEffectIconMapSO"/> (the status visual DB already wired to
    /// the battle status badges) with an entry for every <see cref="StatusEffectType"/> that doesn't
    /// have one yet — pre-filling the display name and description from StatusEffect.GetEffectDescription.
    /// Existing entries (icons/colors you've set) are preserved; only missing statuses are added.
    ///
    /// Menu: Crookedile → Generate → Seed Status Icon Map. Then fill in icons/colors.
    /// </summary>
    public static class StatusIconMapSeeder
    {
        [MenuItem("Crookedile/Generate/Seed Status Icon Map")]
        public static void Seed()
        {
            string[] guids = AssetDatabase.FindAssets("t:StatusEffectIconMapSO");
            if (guids.Length == 0)
            {
                Debug.LogWarning(
                    "[StatusIconMapSeeder] No StatusEffectIconMap asset found. Create one via "
                        + "Create → Crookedile → Battle → Status Effect Icon Map, then re-run."
                );
                return;
            }

            var map = AssetDatabase.LoadAssetAtPath<StatusEffectIconMapSO>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );

            var entries = GetEntries(map);
            var present = new HashSet<StatusEffectType>(entries.Select(e => e.type));

            int added = 0;
            foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
            {
                if (present.Contains(type))
                    continue;
                entries.Add(
                    new StatusEffectIconMapSO.Entry
                    {
                        type = type,
                        effectName = type.ToString(),
                        description = new StatusEffect(type, 1).Description,
                        color = Color.white,
                    }
                );
                added++;
            }

            SetEntries(map, entries);
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[StatusIconMapSeeder] Seeded {map.name}: {added} new entries, {entries.Count} total. "
                    + "Fill in icons/colors.",
                map
            );
        }

        private static List<StatusEffectIconMapSO.Entry> GetEntries(StatusEffectIconMapSO map)
        {
            FieldInfo f = typeof(StatusEffectIconMapSO).GetField(
                "_entries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            var list = f.GetValue(map) as List<StatusEffectIconMapSO.Entry>;
            return list != null
                ? new List<StatusEffectIconMapSO.Entry>(list)
                : new List<StatusEffectIconMapSO.Entry>();
        }

        private static void SetEntries(StatusEffectIconMapSO map, List<StatusEffectIconMapSO.Entry> entries)
        {
            FieldInfo f = typeof(StatusEffectIconMapSO).GetField(
                "_entries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            f.SetValue(map, entries);
        }
    }
}
