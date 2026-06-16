#if UNITY_EDITOR
using System.Collections.Generic;
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
    /// Generates <c>VFXAnimationState.cs</c> — a C# enum that mirrors every state in the
    /// shared VFX Animator Controller (<c>BaseAnimationVFX.controller</c>).
    ///
    /// Triggers automatically:
    ///   • Whenever <c>BaseAnimationVFX.controller</c> is imported (catches manual state additions).
    ///   • At the end of <see cref="SpriteSheetAnimationGenerator.RegisterInController"/> (after
    ///     auto-generating a new clip, the enum is immediately refreshed in the same operation).
    ///
    /// Manual trigger:
    ///   Tools → Crookedile → Regenerate VFX Animation Enum
    /// </summary>
    public class VFXAnimationEnumGenerator : AssetPostprocessor
    {
        internal const string ControllerPath = "Assets/Resources/BaseAnimationVFX.controller";
        private const string OutputPath = "Assets/Scripts/Data/VFX/VFXAnimationState.cs";

        #region Auto-trigger
        /// <summary>
        /// Called by Unity after assets are imported. Regenerates the enum whenever the
        /// VFX controller is saved so manually-added states appear in the dropdown automatically.
        /// </summary>
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths
        )
        {
            if (importedAssets.Contains(ControllerPath) || movedAssets.Contains(ControllerPath))
                Regenerate();
        }

        #endregion

        #region Manual trigger
        [MenuItem("Tools/Crookedile/Regenerate VFX Animation Enum")]
        public static void Regenerate()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError(
                    $"[VFXEnum] AnimatorController not found at '{ControllerPath}'. "
                        + "Check the ControllerPath constant in VFXAnimationEnumGenerator."
                );
                return;
            }

            // Collect all state names across all layers (and any nested sub-state machines).
            var stateNames = new List<string>();
            foreach (var layer in controller.layers)
                CollectStates(layer.stateMachine, stateNames);

            // Sort alphabetically and deduplicate so the file is stable across regenerations.
            stateNames = stateNames.Distinct().OrderBy(s => s).ToList();

            // Read the first-sprite dimensions for each state so NativeSizes can be baked in.
            var nativeSizes = new Dictionary<string, Vector2>();
            foreach (var name in stateNames)
            {
                var clip = FindClipForState(controller, name);
                var size = ReadFirstSpriteSize(clip);
                if (size.HasValue)
                    nativeSizes[name] = size.Value;
            }

            WriteFile(stateNames, nativeSizes);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[VFXEnum] Regenerated {stateNames.Count} state(s): "
                    + string.Join(", ", stateNames)
            );
        }

        #endregion

        #region Helpers
        private static void CollectStates(AnimatorStateMachine sm, List<string> names)
        {
            foreach (var child in sm.states)
                names.Add(child.state.name);
            // Recurse into sub-state machines (if any are added in the future).
            foreach (var sub in sm.stateMachines)
                CollectStates(sub.stateMachine, names);
        }

        /// <summary>
        /// Reads the pixel size of the first sprite keyframe in <paramref name="clip"/>.
        /// Returns null when the clip has no object-reference curves (e.g. no sprite keys).
        /// </summary>
        private static Vector2? ReadFirstSpriteSize(AnimationClip clip)
        {
            if (clip == null)
                return null;
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                if (keys.Length > 0 && keys[0].value is Sprite sprite)
                    return new Vector2(sprite.rect.width, sprite.rect.height);
            }
            return null;
        }

        /// <summary>
        /// Searches every layer (and sub-state machine) of <paramref name="controller"/> for a state
        /// whose name matches <paramref name="stateName"/> and returns its motion clip, or null.
        /// </summary>
        private static AnimationClip FindClipForState(
            AnimatorController controller,
            string stateName
        )
        {
            foreach (var layer in controller.layers)
            {
                var clip = FindClipInStateMachine(layer.stateMachine, stateName);
                if (clip != null)
                    return clip;
            }
            return null;
        }

        private static AnimationClip FindClipInStateMachine(
            AnimatorStateMachine sm,
            string stateName
        )
        {
            foreach (var child in sm.states)
                if (child.state.name == stateName && child.state.motion is AnimationClip c)
                    return c;
            foreach (var sub in sm.stateMachines)
            {
                var c = FindClipInStateMachine(sub.stateMachine, stateName);
                if (c != null)
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Converts an Animator state name to a valid PascalCase C# identifier.
        /// Segments are split on any run of non-alphanumeric characters, then joined with
        /// the first letter of each segment uppercased.
        ///
        /// Examples:
        ///   "Fireball"               → Fireball
        ///   "fanfx2_absorb_large_red" → Fanfx2AbsorbLargeRed
        ///   "Fireball 1"             → Fireball1
        /// </summary>
        internal static string ToEnumName(string stateName)
        {
            var parts = Regex.Split(stateName, @"[^a-zA-Z0-9]+").Where(p => p.Length > 0);
            var sb = new StringBuilder();
            foreach (string part in parts)
            {
                sb.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                    sb.Append(part, 1, part.Length - 1);
            }

            string result = sb.ToString();

            // Prefix underscore when the identifier would start with a digit.
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "_Unknown" : result;
        }

        private static void WriteFile(
            List<string> stateNames,
            Dictionary<string, Vector2> nativeSizes
        )
        {
            var sb = new StringBuilder();

        #endregion

            #region Header
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// Generated by VFXAnimationEnumGenerator.cs — do NOT edit manually.");
            sb.AppendLine("// Source: Assets/Resources/BaseAnimationVFX.controller");
            sb.AppendLine("// Regenerate via:");
            sb.AppendLine("//   • Tools → Crookedile → Regenerate VFX Animation Enum");
            sb.AppendLine(
                "//   • Right-click sprite sheet → Crookedile → Generate Anim from Sprite Sheet"
            );
            sb.AppendLine("//   • Save BaseAnimationVFX.controller (auto)");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace Crookedile.Data.VFX");
            sb.AppendLine("{");

            #endregion

            #region Enum
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated enum of all states in <c>BaseAnimationVFX</c>.");
            sb.AppendLine("    /// Use <see cref=\"VFXAnimationStateExtensions.ToStateName\"/> to");
            sb.AppendLine("    /// get the exact state name string for <c>Animator.Play()</c>.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public enum VFXAnimationState");
            sb.AppendLine("    {");
            sb.AppendLine("        None = 0,");
            foreach (var name in stateNames)
                sb.AppendLine($"        {ToEnumName(name)},");
            sb.AppendLine("    }");
            sb.AppendLine();

            #endregion

            #region Extension
            sb.AppendLine(
                "    /// <summary>Extension methods for <see cref=\"VFXAnimationState\"/>.</summary>"
            );
            sb.AppendLine("    public static class VFXAnimationStateExtensions");
            sb.AppendLine("    {");
            sb.AppendLine(
                "        private static readonly Dictionary<VFXAnimationState, string> StateNames ="
            );
            sb.AppendLine("            new Dictionary<VFXAnimationState, string>");
            sb.AppendLine("        {");
            sb.AppendLine("            { VFXAnimationState.None, string.Empty },");
            foreach (var name in stateNames)
                sb.AppendLine(
                    $"            {{ VFXAnimationState.{ToEnumName(name)}, \"{name}\" }},"
                );
            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(
                "        /// Native pixel dimensions of the first sprite in each animation clip."
            );
            sb.AppendLine(
                "        /// Auto-populated at code-gen time by reading the clip's object-reference curves."
            );
            sb.AppendLine(
                "        /// States with no sprite keys (e.g. the Empty state) are omitted."
            );
            sb.AppendLine(
                "        /// Used by <see cref=\"Crookedile.UI.VFXAnimatedImage.PlayAnimation\"/> to set the"
            );
            sb.AppendLine(
                "        /// RectTransform size on activation so each animation displays at its correct dimensions."
            );
            sb.AppendLine("        /// </summary>");
            sb.AppendLine(
                "        public static readonly Dictionary<string, Vector2> NativeSizes ="
            );
            sb.AppendLine("            new Dictionary<string, Vector2>");
            sb.AppendLine("        {");
            foreach (var kvp in nativeSizes)
                sb.AppendLine(
                    $"            {{ \"{kvp.Key}\", new Vector2({kvp.Value.x}f, {kvp.Value.y}f) }},"
                );
            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine(
                "        /// Returns the exact Animator state name string for <c>Animator.Play()</c>."
            );
            sb.AppendLine(
                "        /// Returns <see cref=\"string.Empty\"/> for <c>VFXAnimationState.None</c>."
            );
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static string ToStateName(this VFXAnimationState state)");
            sb.AppendLine(
                "            => StateNames.TryGetValue(state, out var name) ? name : string.Empty;"
            );
            sb.AppendLine("    }");
            sb.AppendLine("}");

            string dir = Path.GetDirectoryName(OutputPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);
        }
    }
}
            #endregion
#endif
