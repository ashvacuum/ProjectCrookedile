using System;
using Crookedile.Managers;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Brings every session service online before the first scene's objects wake, whichever
    /// scene the game (or the editor's Play button) starts in. Services live under one
    /// <c>[Services]</c> object kept across scene loads; scenes hold only their own managers
    /// (<see cref="Core.SceneSingleton{T}"/>) and may assume these exist.
    ///
    /// Each step runs in order and reports done or failed; a failed step is logged and the rest
    /// still run, so a missing Steam client never blocks audio. Steps that are configured assets
    /// (audio and its sound library) come from the <c>Resources/Services</c> prefab; the rest are
    /// plain components.
    ///
    /// ponytail: every step is synchronous, so this runs inside one startup hook. When a step
    /// has to wait (a Steam Cloud conflict, a slow Steam check on the Deck), move <see cref="Run"/>
    /// into a boot scene that shows progress and loads the first scene when it finishes.
    /// </summary>
    [Debuggable("Bootstrap", LogLevel.Info)]
    public static class Bootstrap
    {
        public const string ServicesPrefabPath = "Services";
        private const string Category = "Bootstrap";

        /// <summary>True once <see cref="Run"/> has finished, failed steps included.</summary>
        public static bool Done { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Run()
        {
            if (Done)
                return;

            var root = new GameObject("[Services]");
            UnityEngine.Object.DontDestroyOnLoad(root);

            int failed = 0;
            failed += Step("Steam", () => root.AddComponent<SteamManager>());
            failed += Step("Audio", () => InstantiateServicesPrefab());
            failed += Step("Scene loader", () => root.AddComponent<SceneLoader>());
#if CHEATS_ENABLED || UNITY_EDITOR || DEVELOPMENT_BUILD
            failed += Step("Cheats", () => root.AddComponent<CheatsManager>());
            failed += Step("Dev console", () => root.AddComponent<DevConsole>());
#endif

            Done = true;
            if (failed > 0)
                GameLogger.LogWarning(Category, $"Services up with {failed} failed step(s).");
            else
                GameLogger.LogInfo(Category, "Services up.");
        }

        /// <summary>Runs one step; returns 1 when it threw or produced nothing, else 0.</summary>
        private static int Step(string name, Func<UnityEngine.Object> start)
        {
            try
            {
                if (start() != null)
                    return 0;
                GameLogger.LogError(Category, $"{name}: produced nothing.");
            }
            catch (Exception e)
            {
                GameLogger.LogError(Category, $"{name}: {e.GetType().Name}: {e.Message}");
            }
            return 1;
        }

        /// <summary>The configured services (audio and its sound library), kept across loads.</summary>
        private static GameObject InstantiateServicesPrefab()
        {
            var prefab = Resources.Load<GameObject>(ServicesPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException(
                    $"no prefab at Resources/{ServicesPrefabPath}"
                );
            var services = UnityEngine.Object.Instantiate(prefab);
            services.name = prefab.name;
            UnityEngine.Object.DontDestroyOnLoad(services);
            return services;
        }
    }
}
