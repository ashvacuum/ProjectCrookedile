using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Crookedile.Data;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Creates or syncs the <see cref="OriginDatabase"/> asset, seeding an entry for every
    /// <see cref="OriginType"/> with display name, description, unique resource and starter tag, and
    /// auto-linking the matching <see cref="OriginPassive"/> asset (by its Origin). Existing entries
    /// are preserved on re-run (only missing origins are added).
    ///
    /// Menu: Crookedile → Generate → Origin Database. Then fill in colors/icons.
    /// </summary>
    public static class OriginDatabaseGenerator
    {
        private const string AssetPath = "Assets/Resources/Databases/OriginDatabase.asset";

        [MenuItem("Crookedile/Generate/Origin Database")]
        public static void Generate()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.Refresh();

            var db = AssetDatabase.LoadAssetAtPath<OriginDatabase>(AssetPath);
            bool isNew = db == null;
            if (isNew)
                db = ScriptableObject.CreateInstance<OriginDatabase>();

            var entries = new List<OriginDatabase.Entry>(db.Entries ?? Array.Empty<OriginDatabase.Entry>());
            var present = new HashSet<OriginType>();
            foreach (var e in entries)
                present.Add(e.Type);

            var passives = FindPassivesByOrigin();

            int added = 0;
            foreach (OriginType origin in Enum.GetValues(typeof(OriginType)))
            {
                if (present.Contains(origin))
                    continue;

                var seed = Seed(origin);
                seed.MaxActionPoints = 3;
                seed.Color = Color.white;
                seed.StarterTag = origin.ToString().ToLowerInvariant();
                passives.TryGetValue(origin, out var passive);
                seed.Passive = passive;

                entries.Add(seed);
                added++;
            }

            SetEntries(db, entries.ToArray());

            if (isNew)
                AssetDatabase.CreateAsset(db, AssetPath);
            else
                EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[OriginDatabaseGenerator] {(isNew ? "Created" : "Synced")} {AssetPath} — "
                    + $"{added} new entries, {entries.Count} total.",
                db
            );
        }

        private static OriginDatabase.Entry Seed(OriginType origin)
        {
            switch (origin)
            {
                case OriginType.FaithLeader:
                    return new OriginDatabase.Entry
                    {
                        Type = origin,
                        DisplayName = "Faith Leader",
                        Description =
                            "The converter. Stack pacify statuses (Guilt/Shame/Doubt) to convert enemies "
                            + "into one-turn meter-pumping Fanatics.",
                        Resource = ArchetypeResource.None,
                    };
                case OriginType.NepoBaby:
                    return new OriginDatabase.Entry
                    {
                        Type = origin,
                        DisplayName = "Nepo Baby",
                        Description =
                            "The schemer. Sacrifice cards to bank Patronage, then summon bodies into the room.",
                        Resource = ArchetypeResource.Patronage,
                    };
                case OriginType.Actor:
                    return new OriginDatabase.Entry
                    {
                        Type = origin,
                        DisplayName = "Celebrity",
                        Description =
                            "The open canvas. Drafts into Attention, Scandal or Drama King; first card "
                            + "each battle is played upgraded.",
                        Resource = ArchetypeResource.Attention,
                    };
                default:
                    return new OriginDatabase.Entry { Type = origin, DisplayName = origin.ToString() };
            }
        }

        private static Dictionary<OriginType, OriginPassive> FindPassivesByOrigin()
        {
            var map = new Dictionary<OriginType, OriginPassive>();
            foreach (string guid in AssetDatabase.FindAssets("t:OriginPassive"))
            {
                var p = AssetDatabase.LoadAssetAtPath<OriginPassive>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (p != null)
                    map[p.Origin] = p;
            }
            return map;
        }

        private static void SetEntries(OriginDatabase db, OriginDatabase.Entry[] entries)
        {
            FieldInfo f = typeof(OriginDatabase).GetField(
                "_entries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            f.SetValue(db, entries);
        }
    }
}
