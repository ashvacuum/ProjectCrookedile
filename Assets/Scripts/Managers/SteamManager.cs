#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace Crookedile.Managers
{
    /// <summary>
    /// Owns the Steamworks API lifetime: init before the first scene, pump callbacks every
    /// frame, shut down on quit. Steam being absent (not running, non-Steam build) is not an
    /// error — <see cref="Initialized"/> stays false and the game runs without Steam features.
    /// </summary>
    [Debuggable("Steam", LogLevel.Info)]
    public class SteamManager : Singleton<SteamManager>
    {
        // ponytail: Valve's public test app (Spacewar). Replace with the real AppID once
        // Steamworks issues one, and update steam_appid.txt in the project root to match.
        private const uint AppId = 480;

        /// <summary>True once SteamAPI.Init succeeded; gate every Steam call on this.</summary>
        public static bool Initialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => _ = Instance;

#if !DISABLESTEAMWORKS
        protected override void OnAwake()
        {
            if (!Packsize.Test() || !DllCheck.Test())
            {
                GameLogger.LogError<SteamManager>(
                    "Steamworks.NET native binaries are missing or the wrong platform."
                );
                return;
            }

            // Launched outside Steam with no steam_appid.txt beside the exe: Steam relaunches
            // the game through its client, so this instance quits.
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
            {
                Application.Quit();
                return;
            }

            // ponytail: running without Steam is allowed for dev; a DRM-strict release would
            // quit here instead.
            Initialized = SteamAPI.Init();
            if (Initialized)
                GameLogger.LogInfo<SteamManager>(
                    $"Steam ready as {SteamFriends.GetPersonaName()} (app {AppId})."
                );
            else
                GameLogger.LogWarning<SteamManager>(
                    "SteamAPI.Init failed — is the Steam client running? Continuing without Steam."
                );
        }

        private void Update()
        {
            if (Initialized)
                SteamAPI.RunCallbacks();
        }

        protected override void OnCleanup()
        {
            if (!Initialized)
                return;
            SteamAPI.Shutdown();
            Initialized = false;
        }
#endif
    }
}
