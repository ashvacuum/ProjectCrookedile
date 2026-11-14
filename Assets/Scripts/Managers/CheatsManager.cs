using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Managers
{
    /// <summary>
    /// The game's cheat commands. Each <c>[CheatCommand]</c> is a dev-console command and, where it
    /// has a <c>[Button]</c>, an Odin button on this component. Every command acts directly on the
    /// run, the battle or the save system; none goes through the EventBus.
    ///
    /// Compiled in when <c>CHEATS_ENABLED</c> is set (menu: Crookedile > Toggle Cheats Build,
    /// Ctrl+Shift+C) and runnable while <see cref="Cheats.Enabled"/> is on. A command not marked
    /// <c>Safe</c> flags the run as cheated when the console runs it.
    /// </summary>
    [Debuggable("Cheats", LogLevel.Info)]
    public class CheatsManager : Singleton<CheatsManager>
    {
        [Header("Current State")]
        [ReadOnly]
        [SerializeField]
        private float _timeScale = 1f;

        private static Gameplay.Battle.BattleManager Battle => Gameplay.Battle.BattleManager.Current;

        #region Run Cheats

        [FoldoutGroup("Run Cheats")]
        [Button("Adjust Funds")]
        [CheatCommand("funds", "Add Funds to the run, or remove with a negative (funds -50)", Category = "Run")]
        public void AdjustFunds(int amount = 100)
        {
            if (Data.RunState.Current == null)
            {
                GameLogger.LogWarning("Cheats", "funds: no run in progress");
                return;
            }
            Data.RunState.Current.AdjustFunds(amount);
            GameLogger.LogInfo("Cheats", $"Funds now {Data.RunState.Current.Funds}");
        }

        [FoldoutGroup("Run Cheats")]
        [Button("Adjust Credibility")]
        [CheatCommand("credibility", "Add Credibility to the run, or remove with a negative", Category = "Run")]
        public void AdjustCredibility(int amount = 10)
        {
            if (Data.RunState.Current == null)
            {
                GameLogger.LogWarning("Cheats", "credibility: no run in progress");
                return;
            }
            Data.RunState.Current.AdjustCredibility(amount);
            GameLogger.LogInfo("Cheats", $"Credibility now {Data.RunState.Current.Credibility}");
        }

        #endregion

        #region Battle Cheats

        [FoldoutGroup("Battle Cheats")]
        [Button("Raise Opinion")]
        [CheatCommand("opinion", "Raise the Opinion Meter directly (opinion 20)", Category = "Battle")]
        public void RaiseOpinion(int amount = 10)
        {
            if (Battle == null)
            {
                GameLogger.LogWarning("Cheats", "opinion: no battle in progress");
                return;
            }
            Battle.RaiseOpinion(amount);
            GameLogger.LogInfo("Cheats", $"Opinion now {Battle.CurrentOpinion}");
        }

        #endregion

        #region Card Cheats

        [FoldoutGroup("Card Cheats")]
        [Button("Draw Cards", ButtonSizes.Medium)]
        [CheatCommand("draw", "Draw cards mid-battle", Category = "Cards")]
        public void DrawCards([LabelText("Amount")] int amount = 5)
        {
            if (Battle?.PlayerDeck == null)
            {
                GameLogger.LogWarning("Cheats", "draw: no battle in progress");
                return;
            }
            GameLogger.LogInfo("Cheats", $"Drew {Battle.PlayerDeck.DrawCards(amount)} card(s)");
        }

        [FoldoutGroup("Card Cheats")]
        [Button("Add Card To Hand", ButtonSizes.Medium)]
        [CheatCommand(
            "addcard",
            "Add a card to your hand mid-battle by name, spaces optional (addcard blowtheallowance)",
            Category = "Cards"
        )]
        public void AddCardToHand([LabelText("Card name")] string cardName)
        {
            var battle = Battle;
            var database = Data.Cards.CardDatabase.Shared;
            if (battle == null || battle.PlayerDeck == null || database == null)
            {
                GameLogger.LogWarning("Cheats", "addcard: no battle in progress");
                return;
            }

            string wanted = Normalize(cardName);
            Data.Cards.CardData match = null;
            int partialMatches = 0;
            foreach (var card in database.GetAll())
            {
                if (card == null || card.IsUpgraded)
                    continue;
                string name = Normalize(card.CardName);
                if (name == wanted)
                {
                    match = card;
                    partialMatches = 1;
                    break;
                }
                if (name.Contains(wanted))
                {
                    match = card;
                    partialMatches++;
                }
            }

            if (match == null || partialMatches > 1)
            {
                GameLogger.LogWarning(
                    "Cheats",
                    partialMatches > 1
                        ? $"addcard: '{cardName}' matches {partialMatches} cards; be more specific"
                        : $"addcard: no card named '{cardName}'"
                );
                return;
            }
            battle.PlayerDeck.AddCardToHand(match);
            GameLogger.LogInfo("Cheats", $"Added {match.CardName} to hand");
        }

        private static string Normalize(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in text ?? "")
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        [FoldoutGroup("Card Cheats")]
        [Button("Unlock All Cards", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 1f)]
        [CheatCommand("unlockall", "Unlock all cards in the game", Category = "Cards")]
        public void UnlockAllCards()
        {
            Data.Save.SaveSystem.SetUnlockAll(true);
            GameLogger.LogInfo("Cheats", "Unlocked all cards for this profile, from the next run");
        }

        [FoldoutGroup("Card Cheats")]
        [Button("Lock All Cards", ButtonSizes.Medium)]
        [CheatCommand("lockall", "Turn the unlock-all override off again", Category = "Cards")]
        public void LockAllCards()
        {
            Data.Save.SaveSystem.SetUnlockAll(false);
            GameLogger.LogInfo("Cheats", "Unlock-all override off, from the next run");
        }

        [FoldoutGroup("Card Cheats")]
        [CheatCommand("locks", "Turn unlock locks on (locks true) or off (locks false) for this session; off by default, so everything is unlocked", Category = "Cards")]
        public void SetLocksEnabled(bool enabled)
        {
            Data.Unlocks.UnlockRules.LocksEnabled = enabled;
            GameLogger.LogInfo("Cheats", $"Unlock locks {(enabled ? "on" : "off")}, from the next run");
        }

        [FoldoutGroup("Card Cheats")]
        [Button("List Unlocks", ButtonSizes.Medium)]
        [CheatCommand("unlocks", "List every unlockable card and whether this profile has it", Category = "Cards", Safe = true)]
        public void ListUnlocks()
        {
            foreach (var status in Data.Save.SaveSystem.GetUnlocks())
                GameLogger.LogInfo(
                    "Cheats",
                    $"{(status.Unlocked ? "[x]" : "[ ]")} {status.Card.CardName} — {status.HowToUnlock}"
                );
        }

        #endregion

        #region Save Debug

        [FoldoutGroup("Save Debug")]
        [Button("Describe Saves", ButtonSizes.Medium)]
        [CheatCommand("saveinfo", "Print every profile and run save, readable", Category = "Save", Safe = true)]
        public void SaveInfo() => GameLogger.LogInfo("Cheats", Data.Save.SaveDebug.Describe());

        [FoldoutGroup("Save Debug")]
        [Button("Export Dump", ButtonSizes.Medium)]
        [CheatCommand("savedump", "Write the readable save dump to save-dump.txt in the save folder", Category = "Save", Safe = true)]
        public void SaveDump() => Data.Save.SaveDebug.ExportDump();

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savepath", "Print the save folder", Category = "Save", Safe = true)]
        public void SavePath() => GameLogger.LogInfo("Cheats", Data.Save.SaveSystem.Root);

        [FoldoutGroup("Save Debug")]
        [Button("Checkpoint Now", ButtonSizes.Medium)]
        [CheatCommand("savenow", "Save the current run immediately", Category = "Save")]
        public void SaveNow()
        {
            Data.Save.SaveSystem.Checkpoint();
            GameLogger.LogInfo("Cheats", "Run checkpoint written");
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savesnap", "Snapshot every save file under a name (savesnap beforeboss)", Category = "Save")]
        public void SaveSnapshot(string name) => Data.Save.SaveDebug.SaveSnapshot(name);

        [FoldoutGroup("Save Debug")]
        [CheatCommand("saveload", "Restore a snapshot, then re-enter the campaign to continue it", Category = "Save")]
        public void LoadSnapshot(string name) => Data.Save.SaveDebug.RestoreSnapshot(name);

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savesnaps", "List save snapshots", Category = "Save", Safe = true)]
        public void ListSnapshots()
        {
            var names = Data.Save.SaveDebug.ListSnapshots();
            GameLogger.LogInfo("Cheats", names.Count == 0 ? "No snapshots" : string.Join(", ", names));
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("saveabandon", "Throw away the run in progress without counting it", Category = "Save")]
        public void AbandonRun()
        {
            Data.Save.SaveSystem.AbandonRun();
            GameLogger.LogInfo("Cheats", "Run abandoned");
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savewipe", "Delete every profile and run (snapshots stay)", Category = "Save")]
        public void WipeSaves() => Data.Save.SaveDebug.WipeAll();

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savegrant", "Grant a card unlock to this profile by name (savegrant trustfund)", Category = "Save")]
        public void GrantUnlock(string cardName)
        {
            var card = Data.Save.SaveDebug.FindCard(cardName, out string error);
            if (card == null)
            {
                GameLogger.LogWarning("Cheats", error);
                return;
            }
            Data.Save.SaveSystem.GrantUnlock(card.ID);
            GameLogger.LogInfo("Cheats", $"Granted {card.CardName}, from the next run");
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("saverevoke", "Take back a granted card unlock by name", Category = "Save")]
        public void RevokeUnlock(string cardName)
        {
            var card = Data.Save.SaveDebug.FindCard(cardName, out string error);
            if (card == null)
            {
                GameLogger.LogWarning("Cheats", error);
                return;
            }
            Data.Save.SaveDebug.RevokeUnlock(card.ID);
            GameLogger.LogInfo("Cheats", $"Revoked the grant on {card.CardName}");
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savecounter", "Set a profile counter (savecounter runs_won_nepobaby 1); 0 clears it", Category = "Save")]
        public void SetCounter(string key, int value)
        {
            Data.Save.SaveDebug.SetCounter(key, value);
            GameLogger.LogInfo("Cheats", $"{key} = {value}");
        }

        [FoldoutGroup("Save Debug")]
        [CheatCommand("savecorrupt", "Corrupt a save file to test recovery: run, profile or profileindex; add true to kill its backup too", Category = "Save")]
        public void CorruptSave(Data.Save.SaveDebug.SaveFile file, bool backupToo = false) =>
            Data.Save.SaveDebug.Corrupt(file, backupToo);

        #endregion

        #region Time Cheats

        [FoldoutGroup("Time Cheats")]
        [Button("Set Time Scale")]
        [CheatCommand("timescale", "Set the game speed (timescale 3); 1 is normal", Category = "Time", Safe = true)]
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

        #endregion

        #region Scene and language

        [CheatCommand("loadscene", "Load a scene by name", Category = "Scene")]
        public void LoadScene(string sceneName)
        {
            if (SceneLoader.Instance == null)
                return;
            SceneLoader.Instance.LoadScene(sceneName);
            GameLogger.LogInfo("Cheats", $"Loading scene: {sceneName}");
        }

        [CheatCommand("setlang", "Set language (english/tagalog)", Category = "Localization", Safe = true)]
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

        protected override void OnCleanup()
        {
            Time.timeScale = 1f;
        }
    }
}
