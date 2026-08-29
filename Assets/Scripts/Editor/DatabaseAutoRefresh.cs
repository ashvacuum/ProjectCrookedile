using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data.Database;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Keeps every <see cref="GameDatabase{T}"/> asset in step with the assets it collects.
    ///
    /// The databases populated from a manual "Refresh Database" button, which meant a card could
    /// exist, look correct in its inspector, and still be invisible to <c>GetByID</c> because
    /// nobody pressed it. Silent, and indistinguishable from the card being broken. This closes
    /// that by refreshing on import instead of on discipline.
    ///
    /// Only the database whose item type actually changed is touched: adding a card does not
    /// rescan enemies. Deletions can't be type-checked — the asset is already gone — so a delete
    /// refreshes any database left holding a null, which is exactly the set that lost something.
    ///
    /// Refreshed databases are left dirty rather than saved here: writing assets from inside an
    /// import callback invites reentrancy, and Unity saves them with the next project save.
    /// </summary>
    public class DatabaseAutoRefresh : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            // Most imports are scripts, textures and scenes. Nothing here can matter unless a
            // ScriptableObject moved, so pay the reflection cost only when one did.
            var touched = importedAssets
                .Concat(movedAssets)
                .Where(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                .ToList();
            bool anyDeleted = deletedAssets
                .Concat(movedFromAssetPaths)
                .Any(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase));

            if (touched.Count == 0 && !anyDeleted)
                return;

            var changedTypes = touched
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(a => a != null)
                .Select(a => a.GetType())
                .Distinct()
                .ToList();

            foreach (var database in LoadAllDatabases())
            {
                Type itemType = ItemTypeOf(database.GetType());
                if (itemType == null)
                    continue;

                // A database of CardData must also refresh when a subclass of CardData lands.
                bool gained = changedTypes.Any(t => itemType.IsAssignableFrom(t));
                bool lost = anyDeleted && HasMissingItem(database);
                if (!gained && !lost)
                    continue;

                Refresh(database);
            }
        }

        [MenuItem("Crookedile/Refresh All Databases")]
        private static void RefreshAll()
        {
            foreach (var database in LoadAllDatabases())
                Refresh(database);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Calls the database's editor-only RefreshDatabase. Reflection because the method lives
        /// on the generic base and there is no non-generic handle to cast to — so it complains
        /// loudly if the name ever drifts, rather than silently not refreshing, which is the
        /// exact failure this class exists to prevent.
        /// </summary>
        private static void Refresh(ScriptableObject database)
        {
            var method = database.GetType().GetMethod("RefreshDatabase");
            if (method == null)
            {
                Debug.LogWarning(
                    $"{database.GetType().Name} has no RefreshDatabase() — auto-refresh skipped it.",
                    database
                );
                return;
            }
            method.Invoke(database, null);
        }

        /// <summary>Every database asset in the project, whatever concrete type it is.</summary>
        private static IEnumerable<ScriptableObject> LoadAllDatabases()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.IsAbstract || ItemTypeOf(type) == null)
                    continue;

                foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        AssetDatabase.GUIDToAssetPath(guid)
                    );
                    if (asset != null && asset.GetType() == type)
                        yield return asset;
                }
            }
        }

        /// <summary>The T of a <c>GameDatabase&lt;T&gt;</c>, or null if the type isn't one.</summary>
        private static Type ItemTypeOf(Type databaseType)
        {
            for (Type t = databaseType; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(GameDatabase<>))
                    return t.GetGenericArguments()[0];
            return null;
        }

        /// <summary>
        /// True when the database's serialized list has a hole in it — what a deleted asset
        /// leaves behind, and a cheaper test than working out what the deleted path used to be.
        /// </summary>
        private static bool HasMissingItem(ScriptableObject database)
        {
            var items = new SerializedObject(database).FindProperty("_items");
            if (items == null || !items.isArray)
                return false;

            for (int i = 0; i < items.arraySize; i++)
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    return true;
            return false;
        }
    }
}
