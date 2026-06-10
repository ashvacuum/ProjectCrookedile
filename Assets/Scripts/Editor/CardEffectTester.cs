using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Crookedile.Editor
{
    /// <summary>
    /// Editor window for testing card effects in simulated battle conditions.
    /// Allows setting up battle stats, status effects, and deck states to test card behavior.
    /// </summary>
    public class CardEffectTester : OdinEditorWindow
    {
        [MenuItem("Crookedile/Card Effect Tester")]
        private static void OpenWindow()
        {
            GetWindow<CardEffectTester>("Card Effect Tester").Show();
        }

        #region Card to Test

        [Title("Card to Test")]
        [InfoBox("Select a card to test its effects in simulated battle conditions.")]
        [AssetSelector(Paths = "Assets/Resources/Cards")]
        [Required]
        [SerializeField]
        private CardData _cardToTest;

        #endregion

        #region Battle Setup

        [Title("Battle Simulation")]
        [FoldoutGroup("Player Stats")]
        [LabelText("Action Points")]
        [PropertyRange(0, 10)]
        [SerializeField]
        private int _playerActionPoints = 3;

        [FoldoutGroup("Player Stats")]
        [Tooltip(
            "Starting Support for the test. Session-level — set directly on BattleManager in a real battle."
        )]
        [LabelText("Starting Support")]
        [PropertyRange(0, 50)]
        [SerializeField]
        private int _playerSupport = 0;

        [FoldoutGroup("Player Stats")]
        [LabelText("Hostility")]
        [PropertyRange(0, 50)]
        [SerializeField]
        private int _playerHostility = 0;

        [FoldoutGroup("Opponent Stats")]
        [LabelText("Action Points")]
        [PropertyRange(0, 10)]
        [SerializeField]
        private int _opponentActionPoints = 3;

        [FoldoutGroup("Opponent Stats")]
        [Tooltip(
            "Starting Denial for the test. Session-level — set directly on BattleManager in a real battle."
        )]
        [LabelText("Starting Denial")]
        [PropertyRange(0, 50)]
        [SerializeField]
        private int _opponentDenial = 0;

        [FoldoutGroup("Opponent Stats")]
        [LabelText("Hostility")]
        [PropertyRange(0, 50)]
        [SerializeField]
        private int _opponentHostility = 0;

        #endregion

        #region Status Effects

        [Title("Status Effects")]
        [FoldoutGroup("Player Status Effects")]
        [InfoBox("Configure status effects active on the player.")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
        [SerializeField]
        private List<StatusEffectSetup> _playerStatusEffects = new List<StatusEffectSetup>();

        [FoldoutGroup("Opponent Status Effects")]
        [InfoBox("Configure status effects active on the opponent.")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = false)]
        [SerializeField]
        private List<StatusEffectSetup> _opponentStatusEffects = new List<StatusEffectSetup>();

        #endregion

        #region Deck Setup

        [Title("Deck Setup")]
        [FoldoutGroup("Player Deck")]
        [LabelText("Cards in Hand")]
        [PropertyRange(0, 10)]
        [SerializeField]
        private int _playerHandSize = 5;

        [FoldoutGroup("Player Deck")]
        [LabelText("Cards in Draw Pile")]
        [PropertyRange(0, 30)]
        [SerializeField]
        private int _playerDrawPileSize = 10;

        [FoldoutGroup("Player Deck")]
        [LabelText("Cards in Discard Pile")]
        [PropertyRange(0, 30)]
        [SerializeField]
        private int _playerDiscardPileSize = 2;

        [FoldoutGroup("Opponent Deck")]
        [LabelText("Cards in Hand")]
        [PropertyRange(0, 10)]
        [SerializeField]
        private int _opponentHandSize = 5;

        [FoldoutGroup("Opponent Deck")]
        [LabelText("Cards in Draw Pile")]
        [PropertyRange(0, 30)]
        [SerializeField]
        private int _opponentDrawPileSize = 10;

        [FoldoutGroup("Opponent Deck")]
        [LabelText("Cards in Discard Pile")]
        [PropertyRange(0, 30)]
        [SerializeField]
        private int _opponentDiscardPileSize = 2;

        #endregion

        #region Test Controls

        [Title("Test Controls")]
        [Button(ButtonSizes.Large, Name = "Test Card Effects")]
        [GUIColor(0.3f, 0.8f, 0.3f)]
        private void TestCard()
        {
            if (_cardToTest == null)
            {
                EditorUtility.DisplayDialog(
                    "Card Effect Tester",
                    "Please select a card to test!",
                    "OK"
                );
                return;
            }

            // Clear console for clean test output
            var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
            var clearMethod = logEntries.GetMethod(
                "Clear",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
            );
            clearMethod?.Invoke(null, null);

            Debug.Log("=== CARD EFFECT TEST START ===");
            Debug.Log($"Testing Card: <b>{_cardToTest.CardName}</b>");
            Debug.Log($"Card Type: {_cardToTest.CardType} | Rarity: {_cardToTest.Rarity}");
            Debug.Log("");

            // Setup battle stats
            BattleStats playerStats = CreateBattleStats(_playerActionPoints);
            // Support/Denial are session-level; no per-stat shield in tester.
            if (_playerHostility > 0)
                playerStats.GainHostility(_playerHostility);

            BattleStats opponentStats = CreateBattleStats(_opponentActionPoints);

            // Set hostility
            if (_opponentHostility > 0)
                opponentStats.GainHostility(_opponentHostility);

            // Create dummy decks
            DeckManager playerDeck = CreateDummyDeck(
                "Player",
                _playerHandSize,
                _playerDrawPileSize,
                _playerDiscardPileSize
            );
            DeckManager opponentDeck = CreateDummyDeck(
                "Opponent",
                _opponentHandSize,
                _opponentDrawPileSize,
                _opponentDiscardPileSize
            );

            Debug.Log("--- INITIAL STATE ---");
            LogBattleState("Player", playerStats, playerDeck);
            LogBattleState("Opponent", opponentStats, opponentDeck);
            Debug.Log("");

            // Create effect resolver
            EffectResolver resolver = new EffectResolver(playerStats, opponentStats, playerDeck);

            // Apply status effects
            ApplyStatusEffects(resolver.PlayerStatusEffects, _playerStatusEffects);
            ApplyStatusEffects(resolver.OpponentStatusEffects, _opponentStatusEffects);

            if (_playerStatusEffects.Count > 0 || _opponentStatusEffects.Count > 0)
            {
                Debug.Log("--- STATUS EFFECTS APPLIED ---");
                if (_playerStatusEffects.Count > 0)
                {
                    Debug.Log(
                        $"Player Status Effects: {string.Join(", ", _playerStatusEffects.ConvertAll(s => $"{s.Behavior?.DisplayName ?? "(none)"} x{s.Stacks}"))}"
                    );
                }
                if (_opponentStatusEffects.Count > 0)
                {
                    Debug.Log(
                        $"Opponent Status Effects: {string.Join(", ", _opponentStatusEffects.ConvertAll(s => $"{s.Behavior?.DisplayName ?? "(none)"} x{s.Stacks}"))}"
                    );
                }
                Debug.Log("");
            }

            // Test each effect
            Debug.Log("--- RESOLVING CARD EFFECTS ---");
            foreach (var effect in _cardToTest.Effects)
            {
                Debug.Log($"<color=cyan>Effect: {effect.GetDescription()}</color>");
            }
            Debug.Log("");

            resolver.ResolveCardEffects(_cardToTest, true);

            Debug.Log("");
            Debug.Log("--- FINAL STATE ---");
            LogBattleState("Player", playerStats, playerDeck);
            LogBattleState("Opponent", opponentStats, opponentDeck);

            Debug.Log("");
            Debug.Log("=== CARD EFFECT TEST COMPLETE ===");
        }

        [Button(ButtonSizes.Medium, Name = "Reset to Defaults")]
        private void ResetToDefaults()
        {
            _cardToTest = null;

            _playerActionPoints = 3;
            _playerSupport = 0;
            _playerHostility = 0;

            _opponentActionPoints = 3;
            _opponentDenial = 0;
            _opponentHostility = 0;

            _playerStatusEffects.Clear();
            _opponentStatusEffects.Clear();

            _playerHandSize = 5;
            _playerDrawPileSize = 10;
            _playerDiscardPileSize = 2;

            _opponentHandSize = 5;
            _opponentDrawPileSize = 10;
            _opponentDiscardPileSize = 2;
        }

        #endregion

        #region Helper Methods

        private BattleStats CreateBattleStats(int maxAP) => new BattleStats(maxAP);

        private DeckManager CreateDummyDeck(
            string ownerName,
            int handSize,
            int drawPileSize,
            int discardPileSize
        )
        {
            // Create a simple dummy card for testing
            var dummyCards = new List<CardData>();

            // We need at least handSize + drawPileSize + discardPileSize cards
            int totalCards = handSize + drawPileSize + discardPileSize;

            // For now, just create an empty deck manager with the right sizes
            // In a real implementation, you'd want to populate with actual cards
            DeckManager deck = new DeckManager(dummyCards, ownerName, 10);

            return deck;
        }

        private void ApplyStatusEffects(
            StatusEffectManager manager,
            List<StatusEffectSetup> effects
        )
        {
            foreach (var effect in effects)
            {
                manager.ApplyStatus(effect.Behavior, effect.Stacks, effect.Duration);
            }
        }

        private void LogBattleState(string name, BattleStats stats, DeckManager deck)
        {
            Debug.Log($"<b>{name}:</b>");
            Debug.Log($"  Action Points: {stats.CurrentActionPoints}/{stats.MaxActionPoints}");
            Debug.Log($"  Hostility: {stats.CurrentHostility}");
            Debug.Log(
                $"  Hand: {deck.HandCount} | Draw: {deck.DeckCount} | Discard: {deck.DiscardCount}"
            );
        }

        #endregion

        #region Presets

        [Title("Quick Presets")]
        [HorizontalGroup("Presets")]
        [Button("High Support Test")]
        private void PresetHighSupport()
        {
            _playerSupport = 15;
            _opponentDenial = 15;
        }

        [HorizontalGroup("Presets")]
        [Button("Status Effect Test")]
        private void PresetStatusEffects()
        {
            _playerStatusEffects.Clear();
            _playerStatusEffects.Add(
                new StatusEffectSetup
                {
                    Behavior = new StrengthStatus(),
                    Stacks = 3,
                    Duration = StatusDurationType.DecreasePerTurn,
                }
            );
            _playerStatusEffects.Add(
                new StatusEffectSetup
                {
                    Behavior = new VulnerableStatus(),
                    Stacks = 2,
                    Duration = StatusDurationType.DecreasePerTurn,
                }
            );

            _opponentStatusEffects.Clear();
            _opponentStatusEffects.Add(
                new StatusEffectSetup
                {
                    Behavior = new WeakenedStatus(),
                    Stacks = 2,
                    Duration = StatusDurationType.DecreasePerTurn,
                }
            );
        }

        [HorizontalGroup("Presets")]
        [Button("Empty Hand Test")]
        private void PresetEmptyHand()
        {
            _playerHandSize = 0;
            _playerDrawPileSize = 15;
        }

        #endregion
    }

    [System.Serializable]
    public class StatusEffectSetup
    {
        [HideLabel]
        [HorizontalGroup]
        [LabelWidth(50)]
        [SerializeReference]
        public StatusBehavior Behavior;

        [HorizontalGroup]
        [LabelText("Stacks")]
        [PropertyRange(1, 10)]
        public int Stacks = 1;

        [HorizontalGroup]
        [LabelText("Duration")]
        public StatusDurationType Duration = StatusDurationType.DecreasePerTurn;
    }
}
