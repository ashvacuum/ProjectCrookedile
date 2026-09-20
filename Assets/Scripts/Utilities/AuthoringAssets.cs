#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Crookedile.Utilities
{
    /// <summary>
    /// Editor-only helpers behind the "New X" buttons on authoring assets — the ones that spare
    /// a trip through the Project window when filling in a list.
    /// </summary>
    public static class AuthoringAssets
    {
        /// <summary>
        /// Creates a <typeparamref name="T"/> asset in the same folder as <paramref name="owner"/>
        /// and returns it. Beside the owner, not inside it, so other content can reference the
        /// result too — use <c>AddObjectToAsset</c> directly when a child asset is what you want.
        /// </summary>
        public static T CreateBeside<T>(Object owner, string baseName)
            where T : ScriptableObject
        {
            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(owner));
            if (string.IsNullOrEmpty(folder))
                folder = "Assets";

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(
                asset,
                AssetDatabase.GenerateUniqueAssetPath($"{folder}/{baseName}.asset")
            );
            EditorUtility.SetDirty(owner);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif
