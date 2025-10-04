using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Editor utility to toggle CHEATS_ENABLED scripting define symbol.
    /// Access via menu: Crookedile > Toggle Cheats Build
    /// </summary>
    public static class CheatsBuildToggle
    {
        private const string CHEATS_DEFINE = "CHEATS_ENABLED";

        [MenuItem("Crookedile/Toggle Cheats Build %#C")] // Ctrl+Shift+C
        public static void ToggleCheatsEnabled()
        {
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            bool isEnabled = defines.Contains(CHEATS_DEFINE);

            if (isEnabled)
            {
                // Remove the define
                defines = defines.Replace(CHEATS_DEFINE, "").Replace(";;", ";").Trim(';');
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                Debug.Log($"<color=red>Cheats DISABLED</color> for {buildTarget}");
            }
            else
            {
                // Add the define
                if (!string.IsNullOrEmpty(defines))
                {
                    defines += ";";
                }
                defines += CHEATS_DEFINE;
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines);
                Debug.Log($"<color=green>Cheats ENABLED</color> for {buildTarget}");
            }

            // Refresh to recompile
            AssetDatabase.Refresh();
        }

        [MenuItem("Crookedile/Toggle Cheats Build %#C", true)]
        public static bool ToggleCheatsEnabledValidate()
        {
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            bool isEnabled = defines.Contains(CHEATS_DEFINE);

            Menu.SetChecked("Crookedile/Toggle Cheats Build", isEnabled);
            return true;
        }

        [MenuItem("Crookedile/Enable Cheats for All Platforms")]
        public static void EnableCheatsForAllPlatforms()
        {
            foreach (BuildTarget target in System.Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                if (group == BuildTargetGroup.Unknown) continue;

                try
                {
                    NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                    string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                    if (!defines.Contains(CHEATS_DEFINE))
                    {
                        if (!string.IsNullOrEmpty(defines))
                        {
                            defines += ";";
                        }
                        defines += CHEATS_DEFINE;
                        PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                    }
                }
                catch
                {
                    // Skip unsupported platforms
                }
            }

            Debug.Log("<color=green>Cheats ENABLED for all platforms</color>");
            AssetDatabase.Refresh();
        }

        [MenuItem("Crookedile/Disable Cheats for All Platforms")]
        public static void DisableCheatsForAllPlatforms()
        {
            foreach (BuildTarget target in System.Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
                if (group == BuildTargetGroup.Unknown) continue;

                try
                {
                    NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                    string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                    if (defines.Contains(CHEATS_DEFINE))
                    {
                        defines = defines.Replace(CHEATS_DEFINE, "").Replace(";;", ";").Trim(';');
                        PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                    }
                }
                catch
                {
                    // Skip unsupported platforms
                }
            }

            Debug.Log("<color=red>Cheats DISABLED for all platforms</color>");
            AssetDatabase.Refresh();
        }
    }
}
