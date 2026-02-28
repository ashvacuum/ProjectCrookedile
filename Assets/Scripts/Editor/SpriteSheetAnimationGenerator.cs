#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.Editor
{
    /// <summary>
    /// Right-click Editor tool that generates a <c>.anim</c> <see cref="AnimationClip"/> asset
    /// for any selected sprite sheet texture (spriteMode = Multiple) or for every sprite sheet
    /// found inside a selected folder.
    ///
    /// Usage:
    ///   1. In the Project window select one or more sprite-sheet textures OR a folder
    ///      containing sprite-sheet sub-folders.
    ///   2. Right-click → Crookedile → Generate Anim from Sprite Sheet.
    ///   3. A <c>.anim</c> file appears next to each sprite sheet, named after its parent folder
    ///      (or the texture itself when the filename is not generic).
    ///
    /// The generated clip targets <see cref="UnityEngine.UI.Image.sprite"/> on the root GameObject
    /// (matching the Fireball.anim format — VFX prefabs are canvas Image-based) and loops by default.
    /// </summary>
    public static class SpriteSheetAnimationGenerator
    {
        // ─── Tunable defaults ─────────────────────────────────────────────────────

        private const float DEFAULT_FRAME_RATE = 12f;   // frames per second
        private const bool  DEFAULT_LOOP       = true;

        /// <summary>
        /// Texture filenames considered "generic" — the parent folder name is used instead.
        /// Case-insensitive comparison.
        /// </summary>
        private static readonly HashSet<string> GenericTextureNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "spritesheet",
            "sprites",
            "atlas",
            "sheet",
            "texture",
        };

        // ─── Menu entry ───────────────────────────────────────────────────────────

        [MenuItem("Assets/Crookedile/Generate Anim from Sprite Sheet", false, 2000)]
        private static void GenerateAnimations()
        {
            int generated = 0;
            int skipped   = 0;

            foreach (UnityEngine.Object selected in Selection.objects)
            {
                string assetPath = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(assetPath)) continue;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    // Batch: find all sliced textures under this folder (recursive).
                    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetPath });
                    foreach (string guid in guids)
                    {
                        string texPath = AssetDatabase.GUIDToAssetPath(guid);
                        bool ok = TryGenerateAnim(texPath);
                        if (ok) generated++;
                        else    skipped++;
                    }
                }
                else
                {
                    // Single texture selected directly.
                    bool ok = TryGenerateAnim(assetPath);
                    if (ok) generated++;
                    else    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Generate Anim from Sprite Sheet",
                $"Generated {generated} animation{(generated == 1 ? "" : "s")}." +
                (skipped > 0 ? $"\nSkipped {skipped} (not a sliced sprite sheet or no sprites found)." : ""),
                "OK");
        }

        // Validate: only show the menu item when at least one asset is selected.
        [MenuItem("Assets/Crookedile/Generate Anim from Sprite Sheet", true)]
        private static bool ValidateGenerateAnimations()
        {
            return Selection.objects != null && Selection.objects.Length > 0;
        }

        // ─── Core logic ───────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to generate a <c>.anim</c> clip for the texture at <paramref name="texturePath"/>.
        /// Returns <c>true</c> on success, <c>false</c> if the texture was skipped.
        /// </summary>
        private static bool TryGenerateAnim(string texturePath)
        {
            // Load the texture importer so we can inspect spriteMode.
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null) return false;

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning($"[AnimGen] Skipped '{texturePath}' — spriteImportMode is not Multiple.");
                return false;
            }

            // Load all sub-assets and filter to Sprite type only.
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            Sprite[] sprites = allAssets
                .OfType<Sprite>()
                .OrderBy(s => s.name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogWarning(
                    $"[AnimGen] Skipped '{texturePath}' — no sprites found (sheet may not be sliced yet).");
                return false;
            }

            // Determine the output animation name and path.
            string animName = ResolveAnimationName(texturePath);
            string outputDir = Path.GetDirectoryName(texturePath)?.Replace('\\', '/') ?? "Assets";
            string outputPath = $"{outputDir}/{animName}.anim";

            // Overwrite an existing clip at this path.
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath) != null)
                AssetDatabase.DeleteAsset(outputPath);

            // Build the AnimationClip.
            AnimationClip clip = new AnimationClip
            {
                frameRate = DEFAULT_FRAME_RATE,
            };

            // One keyframe per sprite.
            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / DEFAULT_FRAME_RATE,
                    value = sprites[i],
                };
            }

            // Bind to Image.sprite on the root GameObject (path = "").
            // Matches Fireball.anim — VFX prefabs use a UI Image, not a SpriteRenderer.
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                "",
                typeof(Image),
                "m_Sprite");

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            // Apply loop setting.
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = DEFAULT_LOOP;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // Save the asset.
            AssetDatabase.CreateAsset(clip, outputPath);

            Debug.Log($"[AnimGen] Created: {outputPath} ({sprites.Length} frames @ {DEFAULT_FRAME_RATE} fps)");
            return true;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the animation clip name for a given texture path.
        /// Uses the parent folder name when the texture filename is generic (e.g. "spritesheet.png").
        /// Otherwise uses the texture filename without extension.
        /// </summary>
        private static string ResolveAnimationName(string texturePath)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(texturePath);

            if (GenericTextureNames.Contains(fileNameWithoutExt))
            {
                // Use the parent folder name.
                string dir = Path.GetDirectoryName(texturePath);
                if (!string.IsNullOrEmpty(dir))
                    return Path.GetFileName(dir);
            }

            return fileNameWithoutExt;
        }
    }
}
#endif
