using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// One-shot: builds the Archivo Black TMP font asset (if it isn't there yet) and points the
    /// whole project at it — TMP's default font plus every <see cref="TMP_Text"/> already sitting
    /// in a prefab or scene. Re-running it is safe; it only touches objects on a different font.
    ///
    /// ponytail: brute-force walk of every prefab and scene. Fine at this project's size; if it
    /// ever crawls, filter the asset search to the UI folders.
    /// </summary>
    public static class ProjectFontSwapper
    {
        const string SourceTtf = "Assets/Art/Fonts/ArchivoBlack-Regular.ttf";
        const string FontAssetPath = "Assets/Art/Fonts/ArchivoBlack-Regular SDF.asset";

        [MenuItem("Crookedile/Fonts/Apply Archivo Black Everywhere")]
        public static void Apply()
        {
            var font = GetOrCreateFontAsset();
            if (font == null)
                return;

            SetAsTmpDefault(font);

            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                if (Retarget(root, font))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/"))
                    continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirty = scene
                    .GetRootGameObjects()
                    .Aggregate(false, (acc, go) => Retarget(go, font) || acc);
                if (dirty)
                {
                    EditorSceneManager.SaveScene(scene);
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Archivo Black applied. {changed} prefab(s)/scene(s) updated.");
        }

        static bool Retarget(GameObject root, TMP_FontAsset font)
        {
            bool dirty = false;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == font)
                    continue;
                text.font = font;
                text.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(text);
                dirty = true;
            }
            return dirty;
        }

        static void SetAsTmpDefault(TMP_FontAsset font)
        {
            var settings = TMP_Settings.instance;
            if (settings == null)
                return;
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedProperties();
        }

        static TMP_FontAsset GetOrCreateFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
                return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceTtf);
            if (source == null)
            {
                Debug.LogError($"No font at {SourceTtf}.");
                return null;
            }

            // Dynamic atlas: glyphs render on demand, so no character-set picking up front.
            var font = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic
            );
            font.name = Path.GetFileNameWithoutExtension(FontAssetPath);

            AssetDatabase.CreateAsset(font, FontAssetPath);
            font.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontAssetPath);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }
    }
}
