#if UNITY_EDITOR
using Crookedile.Data.Enemy;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// One-click backfill: stamps every existing EnemyData asset with a unique ID.
    /// EnemyData.OnValidate already auto-generates one for newly created/opened assets;
    /// this just forces it for assets that predate the ID field and saves the result.
    /// Safe to re-run — only touches assets with a blank ID.
    /// </summary>
    public static class EnemyDataFixer
    {
        [MenuItem("Tools/Crookedile/Backfill Enemy IDs")]
        public static void BackfillIDs()
        {
            int fixedCount = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:EnemyData"))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );
                if (enemy == null || !string.IsNullOrEmpty(enemy.ID))
                    continue;

                var so = new SerializedObject(enemy);
                so.FindProperty("_id").stringValue = System.Guid.NewGuid().ToString();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(enemy);
                fixedCount++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyDataFixer] Backfilled IDs on {fixedCount} enemy asset(s).");
        }
    }
}
#endif
