using UnityEngine;
using Crookedile.Utilities;

namespace Crookedile.Examples
{
    /// <summary>
    /// Example of how to use the Debug Logger system
    /// Mark your class with [Debuggable] attribute to make it appear in the Debug Manager window
    /// </summary>
    [Debuggable("Gameplay", LogLevel.Info)]
    public class Example_DebugUsage : MonoBehaviour
    {
        private void Start()
        {
            // Simple logging using GameLogger static class
            GameLogger.LogInfo("Gameplay", "Game started!");

            // Different log levels
            GameLogger.LogVerbose("Gameplay", "This is verbose info");
            GameLogger.LogInfo("Gameplay", "This is standard info");
            GameLogger.LogWarning("Gameplay", "This is a warning");
            GameLogger.LogError("Gameplay", "This is an error");

            // With context (click the log to highlight the object in hierarchy)
            GameLogger.LogInfo("Gameplay", "This log points to this GameObject", this);

            // To use the system:
            // 1. Add [Debuggable("CategoryName")] attribute to your class
            // 2. In Unity, go to menu: Crookedile > Debug Manager
            // 3. Click "Refresh Categories" to scan all scripts
            // 4. Toggle categories on/off to filter console output
            // 5. Change log levels per category (None, Error, Warning, Info, Verbose)
        }

        private void Update()
        {
            // Example: Only log verbose info when enabled
            GameLogger.LogVerbose("Gameplay", $"Update called at {Time.time}");
        }
    }

    // Another example with different category
    [Debuggable("Card", LogLevel.Verbose)]
    public class Example_CardDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            GameLogger.LogInfo("Card", "Card enabled");
        }

        private void OnDisable()
        {
            GameLogger.LogInfo("Card", "Card disabled");
        }
    }
}
