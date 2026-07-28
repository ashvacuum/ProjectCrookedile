using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crookedile.UI.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// One-shot setup for the campaign loop: builds <c>Assets/Scenes/campaign.unity</c> with a
    /// camera and a <see cref="CampaignFlow"/>, then registers both <c>main</c> and
    /// <c>campaign</c> in Build Settings.
    ///
    /// The Build Settings half matters more than it looks: <c>SceneManager.LoadScene(name)</c>
    /// only resolves scenes listed there, and the project currently ships a single *disabled*
    /// entry pointing at a <c>Bootstrap.unity</c> that doesn't exist. Without this, the first
    /// campaign → battle transition fails at runtime even though everything else is correct.
    ///
    /// Menu: Crookedile → Campaign → Create Campaign Scene.
    /// </summary>
    public static class CampaignSceneCreator
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string CampaignPath = ScenesDir + "/campaign.unity";
        private const string MainPath = ScenesDir + "/main.unity";

        [MenuItem("Crookedile/Campaign/Create Campaign Scene")]
        public static void CreateCampaignScene()
        {
            if (File.Exists(CampaignPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Campaign scene exists",
                    $"{CampaignPath} already exists. Overwrite it?",
                    "Overwrite",
                    "Cancel"
                );
                if (!overwrite)
                    return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // A camera isn't needed to draw IMGUI, but a scene without one logs a warning every
            // frame and makes the Game view look broken.
            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.14f);
            cameraGo.tag = "MainCamera";

            new GameObject("CampaignFlow").AddComponent<CampaignFlow>();

            Directory.CreateDirectory(ScenesDir);
            EditorSceneManager.SaveScene(scene, CampaignPath);
            RegisterScenes();

            AssetDatabase.Refresh();
            Debug.Log(
                $"[CampaignSceneCreator] Created {CampaignPath} and registered it in Build Settings.\n"
                    + "Next: select the CampaignFlow object and assign an Encounter Pool, then press Play."
            );
        }

        /// <summary>
        /// Ensures both scenes are present and enabled in Build Settings, preserving any other
        /// entries and dropping ones whose files no longer exist.
        /// </summary>
        [MenuItem("Crookedile/Campaign/Fix Build Settings Scenes")]
        public static void RegisterScenes()
        {
            var entries = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToList();

            foreach (string path in new[] { MainPath, CampaignPath })
            {
                if (!File.Exists(path))
                    continue;

                var existing = entries.FirstOrDefault(s => s.path == path);
                if (existing != null)
                    existing.enabled = true;
                else
                    entries.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = entries.ToArray();
            Debug.Log(
                "[CampaignSceneCreator] Build Settings scenes: "
                    + string.Join(
                        ", ",
                        entries.Select(s => Path.GetFileNameWithoutExtension(s.path))
                    )
            );
        }
    }
}
