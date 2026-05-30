#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Right-click Editor tool that generates a <c>.anim</c> <see cref="AnimationClip"/> asset
    /// for any selected sprite sheet texture (spriteMode = Multiple) or for every sprite sheet
    /// found inside a selected folder.
    ///
    /// Works by treating <c>Fireball.anim</c> as a YAML template — all binding settings
    /// (classID, component GUID, flags, loop, wrapMode, etc.) are inherited exactly from that
    /// file. Only the clip name, keyframe sprite references, stop time, and pptrCurveMapping
    /// are replaced with per-sheet values.
    ///
    /// Usage:
    ///   1. In the Project window select one or more sprite-sheet textures OR a folder
    ///      containing sprite-sheet sub-folders.
    ///   2. Right-click → Crookedile → Generate Anim from Sprite Sheet.
    ///   3. A <c>.anim</c> file appears next to each sprite sheet, named after its parent folder
    ///      (or the texture itself when the filename is not generic).
    ///   4. Drag the <c>.anim</c> into an Animator Controller state as normal.
    /// </summary>
    public static class SpriteSheetAnimationGenerator
    {
        #region Tunable constants
        /// <summary>Playback rate written into each generated clip. Matches the manual workflow (30fps keyframes, 60fps sample rate).</summary>
        private const float DEFAULT_FRAME_RATE = 30f;

        /// <summary>
        /// Sample rate inherited from the Fireball.anim template (m_SampleRate: 60).
        /// Used only to compute the clip stop time: last_keyframe + 1/SAMPLE_RATE.
        /// </summary>
        private const float SAMPLE_RATE = 60f;

        /// <summary>
        /// Multiplier applied to each sprite's native pixel dimensions when baking the
        /// RectTransform size into the generated clip.
        /// 1 = native pixel size  |  2 = double size  |  0.5 = half size, etc.
        /// Change this constant to globally adjust how large all generated VFX appear
        /// without touching the prefab or pixels-per-unit on any texture.
        /// </summary>
        private const float DISPLAY_SCALE = 1f;

        /// <summary>
        /// Asset name (no extension) of the .anim that acts as the YAML template.
        /// Must live somewhere inside the project's Assets folder.
        /// </summary>
        private const string TEMPLATE_CLIP_NAME = "Fireball";

        /// <summary>
        /// Project-relative path to the Animator Controller that every generated clip
        /// should be registered into as a state.
        /// </summary>
        private const string VFX_CONTROLLER_PATH = "Assets/Resources/BaseAnimationVFX.controller";

        /// <summary>
        /// Texture filenames treated as generic — the parent folder name is used as the
        /// animation name instead. Case-insensitive.
        /// </summary>
        private static readonly HashSet<string> GenericTextureNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "spritesheet",
            "sprites",
            "atlas",
            "sheet",
            "texture",
        };

        #endregion

        #region Menu entry
        [MenuItem("Assets/Crookedile/Generate Anim from Sprite Sheet", false, 2000)]
        private static void GenerateAnimations()
        {
            string templatePath = FindTemplatePath();
            if (templatePath == null)
            {
                EditorUtility.DisplayDialog(
                    "Template Not Found",
                    $"Could not find '{TEMPLATE_CLIP_NAME}.anim' anywhere in the project.\n"
                        + "Make sure Fireball.anim exists (e.g. Assets/Resources/VFXAnimations/Fireball.anim) "
                        + "and try again.",
                    "OK"
                );
                return;
            }

            string templateText = File.ReadAllText(templatePath);

            int generated = 0;
            int skipped = 0;

            foreach (UnityEngine.Object selected in Selection.objects)
            {
                string assetPath = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    // Batch: process every Texture2D found recursively inside the folder.
                    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetPath });
                    foreach (string guid in guids)
                    {
                        string texPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (TryGenerateAnim(texPath, templateText))
                            generated++;
                        else
                            skipped++;
                    }
                }
                else
                {
                    if (TryGenerateAnim(assetPath, templateText))
                        generated++;
                    else
                        skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Generate Anim from Sprite Sheet",
                $"Generated {generated} animation{(generated == 1 ? "" : "s")}."
                    + (
                        skipped > 0
                            ? $"\nSkipped {skipped} (not a sliced sprite sheet, or no sprites found)."
                            : ""
                    ),
                "OK"
            );
        }

        [MenuItem("Assets/Crookedile/Generate Anim from Sprite Sheet", true)]
        private static bool ValidateGenerateAnimations() =>
            Selection.objects != null && Selection.objects.Length > 0;

        #endregion

        #region Core generation
        /// <summary>
        /// Patches the template YAML and writes a <c>.anim</c> next to <paramref name="texturePath"/>.
        /// Returns <c>false</c> (and logs a warning) when the texture should be skipped.
        /// </summary>
        private static bool TryGenerateAnim(string texturePath, string templateText)
        {
            // Only process sliced sprite sheets.
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
                return false;

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogWarning(
                    $"[AnimGen] Skipped '{texturePath}' — spriteImportMode is not Multiple."
                );
                return false;
            }

            // Load and sort the sub-sprites using natural order so _2 comes before _10.
            // Lexicographic sort gives _1, _10, _11 … _2 which produces wrong frame order.
            Sprite[] sprites = AssetDatabase
                .LoadAllAssetsAtPath(texturePath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, NaturalStringComparer.Instance)
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogWarning(
                    $"[AnimGen] Skipped '{texturePath}' — no sprites found "
                        + "(the sheet may not have been sliced in the importer yet)."
                );
                return false;
            }

            // Detect line ending used by the template so the patched sections match.
            string nl = templateText.Contains("\r\n") ? "\r\n" : "\n";

            // Build the two sprite-reference blocks that replace the template's sections.
            var curveBlock = new StringBuilder();
            var mappingBlock = new StringBuilder();

            for (int i = 0; i < sprites.Length; i++)
            {
                if (
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        sprites[i],
                        out string guid,
                        out long fileId
                    )
                )
                {
                    Debug.LogWarning(
                        $"[AnimGen] Could not resolve file ID for sprite '{sprites[i].name}' — skipped."
                    );
                    continue;
                }

                string time = FloatToYaml(i / DEFAULT_FRAME_RATE);
                curveBlock.Append($"    - time: {time}{nl}");
                curveBlock.Append($"      value: {{fileID: {fileId}, guid: {guid}, type: 3}}{nl}");
                mappingBlock.Append($"    - {{fileID: {fileId}, guid: {guid}, type: 3}}{nl}");
            }

            // Stop time = last keyframe time + one sample tick, matching Unity's convention.
            // e.g. 31 sprites @ 30fps: (31-1)/30 + 1/60 = 1.0 + 0.01667 = 1.01667
            float stopTimeValue = (sprites.Length - 1) / DEFAULT_FRAME_RATE + 1f / SAMPLE_RATE;
            string stopTime = FloatToYaml(stopTimeValue);
            string animName = ResolveAnimationName(texturePath);

            string yaml = PatchTemplate(
                templateText,
                animName,
                curveBlock.ToString(),
                mappingBlock.ToString(),
                stopTime
            );

            // Output path: same folder as the texture.
            string outputDir = Path.GetDirectoryName(texturePath)?.Replace('\\', '/') ?? "Assets";
            string outputPath = $"{outputDir}/{animName}.anim";
            string fullAnimPath = Path.GetFullPath(outputPath);
            string fullMetaPath = fullAnimPath + ".meta";

            // Preserve the existing GUID when regenerating, so any Animator Controller
            // references to this clip continue to resolve without manual rewiring.
            string existingGuid = TryReadMetaGuid(fullMetaPath);
            string metaGuid = existingGuid ?? Guid.NewGuid().ToString("N");

            // Write the .anim YAML (overwrite in-place; no DeleteAsset so the old meta GUID survives).
            File.WriteAllText(
                fullAnimPath,
                yaml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );

            // Always write the .meta explicitly with mainObjectFileID: 7400000.
            // Unity sometimes generates mainObjectFileID: 0 for files written via File.WriteAllText,
            // which prevents the AnimationClip from being recognised as the main asset.
            File.WriteAllText(
                fullMetaPath,
                "fileFormatVersion: 2\n"
                    + $"guid: {metaGuid}\n"
                    + "NativeFormatImporter:\n"
                    + "  externalObjects: {}\n"
                    + "  mainObjectFileID: 7400000\n"
                    + "  userData: \n"
                    + "  assetBundleName: \n"
                    + "  assetBundleVariant: \n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            );

            // Import after writing both files so Unity picks up the .meta we wrote.
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);

            // Bake the correct RectTransform size into the clip so the VFX prefab resizes
            // automatically when the animation starts, regardless of which sprite sheet is playing.
            // Uses the first sprite's native pixel dimensions × DISPLAY_SCALE.
            // AnimationCurve.Constant produces a flat curve (same value for the whole clip length),
            // which is exactly what we need — the size is set on frame 0 and never changes.
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (clip != null)
            {
                float w = sprites[0].rect.width * DISPLAY_SCALE;
                float h = sprites[0].rect.height * DISPLAY_SCALE;
                clip.SetCurve(
                    "",
                    typeof(RectTransform),
                    "m_SizeDelta.x",
                    AnimationCurve.Constant(0f, stopTimeValue, w)
                );
                clip.SetCurve(
                    "",
                    typeof(RectTransform),
                    "m_SizeDelta.y",
                    AnimationCurve.Constant(0f, stopTimeValue, h)
                );
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
            }

            // Register the clip as a state in the shared VFX Animator Controller.
            RegisterInController(outputPath, animName);

            Debug.Log(
                $"[AnimGen] Created: {outputPath}  ({sprites.Length} frames @ {DEFAULT_FRAME_RATE} fps)"
            );
            return true;
        }

        #endregion

        #region Template patching
        /// <summary>
        /// Performs the four targeted replacements on the template YAML text.
        /// All other content (bindings, wrapMode, classID, loop settings, …) is kept
        /// verbatim from the template so the output matches it exactly.
        /// </summary>
        private static string PatchTemplate(
            string template,
            string animName,
            string curveBlock,
            string mappingBlock,
            string stopTime
        )
        {
            // 1. Clip name  →  "  m_Name: <name>"
            //    Use a MatchEvaluator lambda — never use "$1value" replacement strings
            //    when value starts with a digit, since .NET regex parses "$12" as group 12.
            template = Regex.Replace(
                template,
                @"(  m_Name: )[^\r\n]*",
                m => m.Groups[1].Value + animName
            );

            // 2. Sprite keyframes — the indented block sitting between "    curve:" and
            //    the next "    attribute:" line.  Each entry is two lines:
            //      "    - time: X"
            //      "      value: {fileID: …, guid: …, type: 3}"
            template = Regex.Replace(
                template,
                @"(    curve:\r?\n)(?:    - time: [^\r\n]*\r?\n      value: [^\r\n]*\r?\n)+",
                m => m.Groups[1].Value + curveBlock
            );

            // 3. Clip stop time inside m_AnimationClipSettings.
            template = Regex.Replace(
                template,
                @"(    m_StopTime: )[^\r\n]*",
                m => m.Groups[1].Value + stopTime
            );

            // 4. pptrCurveMapping — one "{fileID: …}" entry per sprite.
            template = Regex.Replace(
                template,
                @"(    pptrCurveMapping:\r?\n)(?:    - \{[^\r\n]*\r?\n)+",
                m => m.Groups[1].Value + mappingBlock
            );

            return template;
        }

        #endregion

        #region Helpers
        /// <summary>
        /// Searches the entire project for an <see cref="AnimationClip"/> named
        /// <see cref="TEMPLATE_CLIP_NAME"/> and returns its asset path, or <c>null</c>.
        /// </summary>
        private static string FindTemplatePath()
        {
            string[] guids = AssetDatabase.FindAssets($"{TEMPLATE_CLIP_NAME} t:AnimationClip");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (
                    Path.GetFileNameWithoutExtension(path)
                        .Equals(TEMPLATE_CLIP_NAME, StringComparison.OrdinalIgnoreCase)
                )
                    return path;
            }
            return null;
        }

        /// <summary>
        /// Returns the animation name for a texture.
        /// Falls back to the parent folder name when the texture filename is generic
        /// (e.g. "spritesheet.png" → folder name "fanfx2_wind_spell_small_yellow").
        /// </summary>
        private static string ResolveAnimationName(string texturePath)
        {
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(texturePath);
            if (GenericTextureNames.Contains(fileNameWithoutExt))
            {
                string dir = Path.GetDirectoryName(texturePath);
                if (!string.IsNullOrEmpty(dir))
                    return Path.GetFileName(dir);
            }
            return fileNameWithoutExt;
        }

        /// <summary>
        /// Adds a state for <paramref name="clipPath"/> to the base VFX Animator Controller.
        /// Skips silently if the controller is not found or a state with the same name already exists,
        /// so this is safe to call on every regeneration pass.
        /// States are positioned in the same diagonal grid used by existing states (x+35, y+65).
        /// </summary>
        private static void RegisterInController(string clipPath, string stateName)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(VFX_CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogWarning(
                    $"[AnimGen] VFX controller not found at '{VFX_CONTROLLER_PATH}' — "
                        + "state not registered. Check the VFX_CONTROLLER_PATH constant."
                );
                return;
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // Skip if a state with this name already exists (regeneration guard).
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name.Equals(stateName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[AnimGen] State '{stateName}' already in controller — skipped.");
                    return;
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning(
                    $"[AnimGen] Could not load clip at '{clipPath}' to register in controller."
                );
                return;
            }

            // Continue the diagonal layout used by existing states (+35 x, +65 y per step).
            Vector3 pos =
                sm.states.Length > 0
                    ? new Vector3(
                        sm.states[sm.states.Length - 1].position.x + 35f,
                        sm.states[sm.states.Length - 1].position.y + 65f,
                        0f
                    )
                    : new Vector3(200f, 0f, 0f);

            AnimatorState state = sm.AddState(stateName, pos);
            state.motion = clip;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // Keep the VFXAnimationState enum in sync so the new state is immediately
            // available as a dropdown in VFXEvent assets without a manual regeneration step.
            VFXAnimationEnumGenerator.Regenerate();

            Debug.Log($"[AnimGen] Registered state '{stateName}' in {VFX_CONTROLLER_PATH}");
        }

        /// <summary>
        /// Reads the <c>guid:</c> field from an existing Unity <c>.meta</c> file.
        /// Returns <c>null</c> if the file does not exist or has no guid line.
        /// Used to preserve the asset GUID when regenerating an existing clip so that
        /// Animator Controller references remain valid.
        /// </summary>
        private static string TryReadMetaGuid(string metaFullPath)
        {
            if (!File.Exists(metaFullPath))
                return null;

            const string prefix = "guid: ";
            foreach (string line in File.ReadLines(metaFullPath))
            {
                if (line.StartsWith(prefix))
                    return line.Substring(prefix.Length).Trim();
            }
            return null;
        }

        /// <summary>
        /// Formats a float value the same way Unity writes it in .anim YAML:
        /// round-trip precision, invariant culture, no trailing ".0" for whole numbers.
        /// </summary>
        private static string FloatToYaml(float value)
        {
            if (value == 0f)
                return "0";
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Compares strings so that embedded numeric runs are sorted numerically rather than
    /// lexicographically. "sprite_2" &lt; "sprite_10" (natural) vs "sprite_10" &lt; "sprite_2" (lex).
    ///
    /// Algorithm: walk both strings in parallel, chunking into alternating runs of
    /// digits and non-digits. Non-digit chunks compare case-insensitively by character;
    /// digit chunks are parsed as <see cref="long"/> and compared numerically.
    /// </summary>
    internal sealed class NaturalStringComparer : IComparer<string>
    {
        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        private NaturalStringComparer() { }

        public int Compare(string a, string b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            int i = 0,
                j = 0;

            while (i < a.Length && j < b.Length)
            {
                bool aDigit = char.IsDigit(a[i]);
                bool bDigit = char.IsDigit(b[j]);

                if (aDigit && bDigit)
                {
                    // Numeric chunk — parse and compare as long.
                    int ai = i,
                        bi = j;
                    while (i < a.Length && char.IsDigit(a[i]))
                        i++;
                    while (j < b.Length && char.IsDigit(b[j]))
                        j++;

                    long numA = long.Parse(a.Substring(ai, i - ai));
                    long numB = long.Parse(b.Substring(bi, j - bi));
                    int cmp = numA.CompareTo(numB);
                    if (cmp != 0)
                        return cmp;
                }
                else
                {
                    // Non-digit chunk — compare single character, case-insensitive.
                    int cmp = char.ToUpperInvariant(a[i]).CompareTo(char.ToUpperInvariant(b[j]));
                    if (cmp != 0)
                        return cmp;
                    i++;
                    j++;
                }
            }

            // Shorter string sorts first when one is a prefix of the other.
            return a.Length.CompareTo(b.Length);
        }
    }
}
        #endregion
#endif
