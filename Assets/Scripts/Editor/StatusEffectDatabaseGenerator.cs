using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Creates or syncs the <see cref="StatusEffectDatabase"/> asset, seeding an entry for every
    /// <see cref="StatusEffectType"/> with its display name, category, and the existing description
    /// (from StatusEffect.GetEffectDescription). Existing entries are preserved (icons/colors/SFX you
    /// set are kept) — re-running only ADDS entries for statuses not yet present.
    ///
    /// Menu: Crookedile → Generate → Status Effect Database. Then fill in icons/colors/SFX/VFX.
    /// </summary>
    public static class StatusEffectDatabaseGenerator
    {
        private const string AssetPath = "Assets/Resources/Databases/StatusEffectDatabase.asset";

        [MenuItem("Crookedile/Generate/Status Effect Database")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.Refresh();

            var db = AssetDatabase.LoadAssetAtPath<StatusEffectDatabase>(AssetPath);
            bool isNew = db == null;
            if (isNew)
                db = ScriptableObject.CreateInstance<StatusEffectDatabase>();

            var existing = new List<StatusEffectDatabase.Entry>(db.Entries ?? Array.Empty<StatusEffectDatabase.Entry>());
            var present = new HashSet<StatusEffectType>();
            foreach (var e in existing)
                present.Add(e.Type);

            int added = 0;
            foreach (StatusEffectType type in Enum.GetValues(typeof(StatusEffectType)))
            {
                if (present.Contains(type))
                    continue;
                existing.Add(
                    new StatusEffectDatabase.Entry
                    {
                        Type = type,
                        DisplayName = type.ToString(),
                        Description = new StatusEffect(type, 1).Description,
                        Category = Categorize(type),
                        Color = Color.white,
                    }
                );
                added++;
            }

            SetEntries(db, existing.ToArray());

            if (isNew)
                AssetDatabase.CreateAsset(db, AssetPath);
            else
                EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[StatusEffectDatabaseGenerator] {(isNew ? "Created" : "Synced")} {AssetPath} — "
                    + $"{added} new entries, {existing.Count} total. Fill in icons/colors/SFX/VFX.",
                db
            );
        }

        private static StatusCategory Categorize(StatusEffectType t)
        {
            switch (t)
            {
                case StatusEffectType.Guilt:
                case StatusEffectType.Shame:
                case StatusEffectType.Doubt:
                    return StatusCategory.Pacify;
                case StatusEffectType.Jaded:
                    return StatusCategory.Threshold;
                case StatusEffectType.Hardened:
                case StatusEffectType.Fanatic:
                case StatusEffectType.Turncoat:
                case StatusEffectType.Devotion:
                    return StatusCategory.HostilityFlag;
                case StatusEffectType.Strength:
                case StatusEffectType.Dexterity:
                case StatusEffectType.Focus:
                case StatusEffectType.Energized:
                case StatusEffectType.Plated:
                case StatusEffectType.Regeneration:
                case StatusEffectType.Intangible:
                case StatusEffectType.Thorns:
                case StatusEffectType.Ritual:
                case StatusEffectType.Momentum:
                case StatusEffectType.Echo:
                    return StatusCategory.Buff;
                case StatusEffectType.Weakened:
                case StatusEffectType.Vulnerable:
                case StatusEffectType.Frail:
                case StatusEffectType.Entangled:
                case StatusEffectType.Exposed:
                case StatusEffectType.Smear:
                case StatusEffectType.Confused:
                case StatusEffectType.Silenced:
                case StatusEffectType.Stunned:
                case StatusEffectType.Rattled:
                    return StatusCategory.Debuff;
                default:
                    return StatusCategory.Special;
            }
        }

        private static void SetEntries(StatusEffectDatabase db, StatusEffectDatabase.Entry[] entries)
        {
            FieldInfo f = typeof(StatusEffectDatabase).GetField(
                "_entries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            f.SetValue(db, entries);
        }
    }
}
