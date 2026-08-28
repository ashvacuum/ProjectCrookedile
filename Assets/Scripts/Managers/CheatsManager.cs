using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Managers
{
    /// <summary>
    /// Development cheats and debug tools manager.
    /// Only active when CHEATS_ENABLED compile flag is set.
    /// Use menu: Crookedile > Toggle Cheats Build (Ctrl+Shift+C) to enable/disable.
    /// Provides tools for testing, debugging, and speeding up development.
    /// </summary>
    [Debuggable("Cheats", LogLevel.Info)]
    public class CheatsManager : Singleton<CheatsManager>
    {
        [Header("Current State")]
        [ReadOnly]
        [SerializeField]
        private bool _godModeActive = false;

        [ReadOnly]
        [SerializeField]
        private bool _unlimitedResourcesActive = false;

        [ReadOnly]
        [SerializeField]
        private float _timeScale = 1f;

        public bool GodModeActive => _godModeActive;
        public bool UnlimitedResourcesActive => _unlimitedResourcesActive;

        protected override void OnAwake()
        {
#if CHEATS_ENABLED
            GameLogger.LogInfo("Cheats", "Cheats Manager initialized");
            RegisterCommands();
#else
            GameLogger.LogWarning("Cheats", "Cheats disabled - CHEATS_ENABLED flag not set");
#endif
        }

        #region Cheat Commands

        [FoldoutGroup("General Cheats")]
        [Button("Toggle God Mode", ButtonSizes.Large)]
        [GUIColor(0.3f, 1f, 0.3f)]
        [CheatCommand("god", "Toggle invincibility", Category = "General")]
        public void ToggleGodMode()
        {
            _godModeActive = !_godModeActive;
            GameLogger.LogInfo("Cheats", $"God Mode: {(_godModeActive ? "ON" : "OFF")}");

            EventBus.Publish(new GodModeToggleEvent { Enabled = _godModeActive });
        }

        [FoldoutGroup("General Cheats")]
        [Button("Toggle Unlimited Resources", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.3f, 1f)]
        [CheatCommand("unlimited", "Toggle unlimited resources", Category = "General")]
        public void ToggleUnlimitedResources()
        {
            _unlimitedResourcesActive = !_unlimitedResourcesActive;
            GameLogger.LogInfo(
                "Cheats",
                $"Unlimited Resources: {(_unlimitedResourcesActive ? "ON" : "OFF")}"
            );

            EventBus.Publish(
                new UnlimitedResourcesToggleEvent { Enabled = _unlimitedResourcesActive }
            );
        }

        #endregion

        #region Resource Cheats

        [FoldoutGroup("Resource Cheats")]
        [Button("Give Max Resources", ButtonSizes.Large)]
        [GUIColor(1f, 0.8f, 0.3f)]
        [CheatCommand("maxres", "Give maximum resources", Category = "Resources")]
        public void GiveMaxResources()
        {
            GiveResources(99999, 100, 100, 0, 30000);
            GameLogger.LogInfo("Cheats", "Gave maximum resources");
        }

        [FoldoutGroup("Resource Cheats")]
        [Button("Give Resources")]
        [CheatCommand(
            "give",
            "Give resources (money, lagay, utang, heat, support)",
            Category = "Resources"
        )]
        public void GiveResources(
            [LabelText("₱")] int campaignFunds = 1000,
            [LabelText("L")] int lagay = 10,
            [LabelText("U")] int utangNaLoob = 10,
            [LabelText("H")] int heat = 0,
            [LabelText("Support")] int support = 5000
        )
        {
            var resourceEvent = new CheatGiveResourcesEvent
            {
                CampaignFunds = campaignFunds,
                Lagay = lagay,
                UtangNaLoob = utangNaLoob,
                Heat = heat,
                Support = support,
            };

            EventBus.Publish(resourceEvent);
            GameLogger.LogInfo(
                "Cheats",
                $"Gave resources: ₱{campaignFunds}, {lagay}L, {utangNaLoob}U, {heat}H, {support} Support"
            );
        }

        [FoldoutGroup("Resource Cheats")]
        [Button("Clear Heat")]
        [CheatCommand("clearheat", "Remove all Heat", Category = "Resources")]
        public void ClearHeat()
        {
            EventBus.Publish(new CheatClearHeatEvent());
            GameLogger.LogInfo("Cheats", "Cleared all Heat");
        }

        #endregion

        #region Card Cheats

        [FoldoutGroup("Card Cheats")]
        [Button("Draw Cards", ButtonSizes.Medium)]
        [CheatCommand("draw", "Draw cards", Category = "Cards")]
        public void DrawCards([LabelText("Amount")] int amount = 5)
        {
            EventBus.Publish(new CheatDrawCardsEvent { Amount = amount });
            GameLogger.LogInfo("Cheats", $"Drew {amount} cards");
        }

        [FoldoutGroup("Card Cheats")]
        [Button("Refresh Hand", ButtonSizes.Medium)]
        [CheatCommand("refreshhand", "Discard and redraw hand", Category = "Cards")]
        public void RefreshHand()
        {
            EventBus.Publish(new CheatRefreshHandEvent());
            GameLogger.LogInfo("Cheats", "Refreshed hand");
        }

        [FoldoutGroup("Card Cheats")]
        [Button("Unlock All Cards", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 1f)]
        [CheatCommand("unlockall", "Unlock all cards in the game", Category = "Cards")]
        public void UnlockAllCards()
        {
            EventBus.Publish(new CheatUnlockAllCardsEvent());
            GameLogger.LogInfo("Cheats", "Unlocked all cards");
        }

        #endregion

        #region Time Cheats

        [FoldoutGroup("Time Cheats")]
        [Button("Set Time Scale")]
        public void SetTimeScale(float scale = 1f)
        {
            _timeScale = scale;
            Time.timeScale = scale;
            GameLogger.LogInfo("Cheats", $"Time scale set to {scale}x");
        }

        [FoldoutGroup("Time Cheats")]
        [Button("Adjust Time Scale")]
        public void AdjustTimeScale(float multiplier)
        {
            _timeScale = Mathf.Clamp(_timeScale * multiplier, 0.1f, 10f);
            Time.timeScale = _timeScale;
            GameLogger.LogInfo("Cheats", $"Time scale adjusted to {_timeScale}x");
        }

        [FoldoutGroup("Time Cheats")]
        [Button("Skip Day", ButtonSizes.Medium)]
        public void SkipDay()
        {
            EventBus.Publish(new CheatSkipDayEvent());
            GameLogger.LogInfo("Cheats", "Skipped to next day");
        }

        [FoldoutGroup("Time Cheats")]
        [Button("Jump to Day")]
        public void JumpToDay(int day = 1)
        {
            EventBus.Publish(new CheatJumpToDayEvent { Day = day });
            GameLogger.LogInfo("Cheats", $"Jumped to day {day}");
        }

        #endregion

        #region Battle Cheats

        [FoldoutGroup("Battle Cheats")]
        [Button("Win Current Battle", ButtonSizes.Large)]
        [GUIColor(0.3f, 1f, 0.3f)]
        [CheatCommand("win", "Instantly win the current battle", Category = "Battle")]
        public void WinBattle()
        {
            EventBus.Publish(new CheatWinBattleEvent());
            GameLogger.LogInfo("Cheats", "Force won current battle");
        }

        [FoldoutGroup("Battle Cheats")]
        [Button("Set Opponent Confidence")]
        [CheatCommand("setconfidence", "Set opponent's confidence (0-100)", Category = "Battle")]
        public void SetOpponentConfidence(int confidence = 1)
        {
            confidence = Mathf.Clamp(confidence, 0, 100);
            EventBus.Publish(new CheatSetOpponentConfidenceEvent { Confidence = confidence });
            GameLogger.LogInfo("Cheats", $"Set opponent confidence to {confidence}");
        }

        #endregion

        #region Manager Integration Examples

        /// <summary>
        /// Example: Call AudioManager from cheats
        /// </summary>
        [CheatCommand("mute_music", "Mute/unmute music", Category = "Audio")]
        public void ToggleMusic()
        {
            if (AudioManager.Instance == null)
                return;
            // AudioManager.Instance.SetMusicVolume(0f); // Example
            GameLogger.LogInfo("Cheats", "Toggled music");
        }

        /// <summary>
        /// Example: Call SceneLoader from cheats
        /// </summary>
        [CheatCommand("loadscene", "Load a scene by name", Category = "Scene")]
        public void LoadScene(string sceneName)
        {
            if (SceneLoader.Instance == null)
                return;
            SceneLoader.Instance.LoadScene(sceneName);
            GameLogger.LogInfo("Cheats", $"Loading scene: {sceneName}");
        }

        /// <summary>
        /// Example: Call LocalizationManager from cheats
        /// </summary>
        [CheatCommand("setlang", "Set language (english/tagalog)", Category = "Localization")]
        public void SetLanguage(string language)
        {
            if (LocalizationManager.Instance == null)
                return;

            UnityEngine.SystemLanguage lang =
                language.ToLower() == "tagalog"
                    ? UnityEngine.SystemLanguage.Unknown // Using Unknown for Filipino
                    : UnityEngine.SystemLanguage.English;

            LocalizationManager.Instance.SetLanguage(lang);
            GameLogger.LogInfo("Cheats", $"Language set to: {language}");
        }

        #endregion

        #region Utility

        private void RegisterCommands()
        {
            // Commands are now auto-discovered via CheatCommandAttribute
            GameLogger.LogInfo("Cheats", "Cheat commands registered via attributes");
        }

        protected override void OnCleanup()
        {
            // Reset time scale on cleanup
            Time.timeScale = 1f;
        }

        #endregion
    }

    #region Cheat Events

    public class GodModeToggleEvent : IGameEvent
    {
        public bool Enabled;
    }

    public class UnlimitedResourcesToggleEvent : IGameEvent
    {
        public bool Enabled;
    }

    public class CheatGiveResourcesEvent : IGameEvent
    {
        public int CampaignFunds;
        public int Lagay;
        public int UtangNaLoob;
        public int Heat;
        public int Support;
    }

    public class CheatClearHeatEvent : IGameEvent { }

    public class CheatDrawCardsEvent : IGameEvent
    {
        public int Amount;
    }

    public class CheatRefreshHandEvent : IGameEvent { }

    public class CheatUnlockAllCardsEvent : IGameEvent { }

    public class CheatSkipDayEvent : IGameEvent { }

    public class CheatJumpToDayEvent : IGameEvent
    {
        public int Day;
    }

    public class CheatWinBattleEvent : IGameEvent { }

    public class CheatSetOpponentConfidenceEvent : IGameEvent
    {
        public int Confidence;
    }

    #endregion
}
