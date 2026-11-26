using UnityEngine;

namespace Crookedile.Utilities
{
    /// <summary>Quits the game, or leaves play mode in the editor.</summary>
    public static class AppQuit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
