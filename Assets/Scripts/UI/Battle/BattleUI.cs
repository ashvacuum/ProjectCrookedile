using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Main UI controller for battle screen.
    /// Displays player/opponent stats, hand, and battle controls.
    /// </summary>
    public class BattleUI : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private TMP_Text playerResolveText;
        [SerializeField] private TMP_Text playerComposureText;
        [SerializeField] private TMP_Text playerHostilityText;
        [SerializeField] private TMP_Text playerAPText;

        [Header("Enemy Slots")]
        [Tooltip("Parent transform that enemy slot panels are spawned into")]
        [SerializeField] private Transform  enemySlotContainer;
        [Tooltip("Prefab with an EnemySlotUI component — instantiated once per enemy")]
        [SerializeField] private GameObject enemySlotPrefab;

        // Runtime list of spawned enemy slot UIs (one per enemy in the battle)
        private List<EnemySlotUI> _enemySlots = new List<EnemySlotUI>();

        [Header("Battle Info")]
        [SerializeField] private TMP_Text turnNumberText;
        [SerializeField] private TMP_Text phaseText;

        [Header("Hand Display")]
        [SerializeField] private Transform cardButtonContainer;
        [SerializeField] private GameObject cardButtonPrefab;

        [Header("Controls")]
        [SerializeField] private Button endTurnButton;

        [Header("Battle Log")]
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private ScrollRect battleLogScrollRect;
        [SerializeField] private int maxLogLines = 20;

        [Header("Battle Result")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private BattleManager battleManager;
        private List<CardButton> activeCardButtons = new List<CardButton>();
        private ObjectPool<CardButton> _cardPool;
        private List<string> battleLogLines = new List<string>();

        #region Initialization

        private void Awake()
        {
            // Setup button listeners
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            // Hide result panels
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void Start()
        {
            if (cardButtonPrefab != null)
            {
                var prefabComponent = cardButtonPrefab.GetComponent<CardButton>();
                if (prefabComponent != null)
                    _cardPool = new ObjectPool<CardButton>(prefabComponent, initialSize: 7, parent: cardButtonContainer);
            }
        }

        private void OnDestroy()
        {
            _cardPool?.Clear();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Subscribe<EnemyHostilityChangedEvent>(OnEnemyHostilityChanged);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Unsubscribe<EnemyHostilityChangedEvent>(OnEnemyHostilityChanged);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        /// <summary>
        /// Initialize with BattleManager reference.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
            RefreshUI();
        }

        #endregion

        #region Event Handlers

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            AddLogEntry("=== Battle Started ===");
            BuildEnemySlots();
            RefreshUI();
        }

        private void BuildEnemySlots()
        {
            // Destroy any slots from a previous battle
            foreach (var slot in _enemySlots)
                if (slot != null) Destroy(slot.gameObject);
            _enemySlots.Clear();

            if (enemySlotContainer == null || enemySlotPrefab == null || battleManager == null) return;

            for (int i = 0; i < battleManager.Enemies.Count; i++)
            {
                var go   = Instantiate(enemySlotPrefab, enemySlotContainer);
                var slot = go.GetComponent<EnemySlotUI>();
                if (slot != null)
                {
                    slot.Initialize(i, battleManager, battleManager.PlayerOrigin);
                    _enemySlots.Add(slot);
                }
            }
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            string turnOwner = evt.IsPlayerTurn ? "Player" : "Opponent";
            AddLogEntry($"--- Turn {evt.TurnNumber}: {turnOwner} ---");
            RefreshUI();
        }

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            RefreshUI();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            string player = evt.IsPlayer ? "Player" : "Opponent";
            AddLogEntry($"{player} played: {evt.Card.CardName}");
            RefreshUI();
        }

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt)
        {
            if (evt.Move != null)
                AddLogEntry($"Enemy [{evt.EnemyIndex}] intends: {evt.Move.IntentDescription}");
            if (evt.EnemyIndex < _enemySlots.Count)
                _enemySlots[evt.EnemyIndex]?.UpdateIntent(evt.Move);
        }

        private void OnEnemyHostilityChanged(EnemyHostilityChangedEvent evt)
        {
            if (evt.EnemyIndex < _enemySlots.Count)
            {
                _enemySlots[evt.EnemyIndex]?.Refresh();
                _enemySlots[evt.EnemyIndex]?.PulseHostility();
            }
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            AddLogEntry($"{evt.EnemyName} defeated!");
            if (evt.EnemyIndex < _enemySlots.Count)
                _enemySlots[evt.EnemyIndex]?.MarkDefeated();
        }

        private void OnBattleEnded(BattleEndedEvent evt)
        {
            if (evt.Result.isVictory)
            {
                AddLogEntry("=== VICTORY ===");
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                AddLogEntry("=== DEFEAT ===");
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }

            // Disable controls
            if (endTurnButton != null) endTurnButton.interactable = false;
            ClearCardButtons();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Refreshes all UI elements to match current battle state.
        /// </summary>
        public void RefreshUI()
        {
            if (battleManager == null) return;

            UpdateStatsDisplay();
            UpdateBattleInfo();
            UpdateHandDisplay();
            UpdateStatusEffects();
        }

        private void UpdateStatsDisplay()
        {
            // Player stats
            var playerStats = battleManager.PlayerStats;
            if (playerStats == null) return; // Battle not yet initialized — wait for BattleStartedEvent
            if (playerResolveText != null)
                playerResolveText.text = $"Resolve: {playerStats.CurrentResolve}/{playerStats.MaxResolve}";
            if (playerComposureText != null)
                playerComposureText.text = $"Composure: {playerStats.CurrentComposure}";
            // playerHostilityText intentionally not updated — player hostility stays 0; enemy owns the number line
            if (playerAPText != null)
                playerAPText.text = $"AP: {playerStats.CurrentActionPoints}/{playerStats.MaxActionPoints}";

            // Enemy slots — refresh each and update focus highlight
            for (int i = 0; i < _enemySlots.Count; i++)
            {
                _enemySlots[i]?.Refresh();
                _enemySlots[i]?.SetSelected(i == battleManager.FocusedEnemyIndex);
            }
        }

        private void UpdateBattleInfo()
        {
            if (turnNumberText != null)
                turnNumberText.text = $"Turn: {battleManager.CurrentTurn}";

            if (phaseText != null)
            {
                string phase = battleManager.CurrentState.ToString();
                string turnOwner = battleManager.IsPlayerTurn ? "Player" : "Opponent";
                phaseText.text = $"{phase} ({turnOwner})";
            }

            // Enable/disable end turn button
            if (endTurnButton != null)
            {
                bool canEndTurn = battleManager.IsPlayerTurn &&
                                  battleManager.CurrentState == BattleState.PlayerTurn;
                endTurnButton.interactable = canEndTurn;
            }
        }

        private void UpdateHandDisplay()
        {
            if (cardButtonContainer == null || cardButtonPrefab == null || battleManager.PlayerStats == null) return;

            // Clear existing buttons
            ClearCardButtons();

            // Only show hand during player's turn
            if (!battleManager.IsPlayerTurn) return;

            int currentAP = battleManager.PlayerStats.CurrentActionPoints;
            var hand = battleManager.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                CardButton cardButton = _cardPool != null
                    ? _cardPool.Get()
                    : Instantiate(cardButtonPrefab, cardButtonContainer).GetComponent<CardButton>();

                if (cardButton != null)
                {
                    int handIndex = i; // Capture for closure
                    cardButton.Initialize(card, handIndex, currentAP, () => OnCardButtonClicked(card, handIndex));
                    cardButton.PlayDrawAnimation();
                    activeCardButtons.Add(cardButton);
                }
            }

            // Apply arc fan layout if CardHandLayout is on the container.
            // Remove the Horizontal Layout Group from the container if using this.
            cardButtonContainer.GetComponent<CardHandLayout>()?.ArrangeCards(activeCardButtons);
        }

        /// <summary>
        /// Refreshes card affordability dimming when AP changes mid-turn without rebuilding the hand.
        /// </summary>
        private void RefreshCardAffordability()
        {
            if (!battleManager.IsPlayerTurn) return;
            int currentAP = battleManager.PlayerStats.CurrentActionPoints;
            foreach (var cardButton in activeCardButtons)
            {
                if (cardButton != null)
                    cardButton.RefreshVisuals(currentAP);
            }
        }

        private void UpdateStatusEffects()
        {
            // TODO: Display active status effects
            // For now, this is placeholder - will implement when we add status effect icons
        }

        private void ClearCardButtons()
        {
            foreach (var button in activeCardButtons)
            {
                if (button == null) continue;
                if (_cardPool != null)
                    _cardPool.Return(button);
                else
                    Destroy(button.gameObject);
            }
            activeCardButtons.Clear();
        }

        /// <summary>
        /// Briefly scales a text element up then back to normal to signal a change.
        /// </summary>
        private IEnumerator PulseText(TMP_Text text)
        {
            Vector3 original = text.transform.localScale;
            text.transform.localScale = original * 1.2f;
            yield return new WaitForSeconds(0.15f);
            text.transform.localScale = original;
        }

        #endregion

        #region Battle Log

        private void AddLogEntry(string message)
        {
            battleLogLines.Add(message);

            // Trim log if too long
            if (battleLogLines.Count > maxLogLines)
            {
                battleLogLines.RemoveAt(0);
            }

            UpdateBattleLog();
        }

        private void UpdateBattleLog()
        {
            if (battleLogText == null) return;

            battleLogText.text = string.Join("\n", battleLogLines);

            // Auto-scroll to bottom
            if (battleLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                battleLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion

        #region Input Handlers

        private void OnEndTurnClicked()
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new EndTurnRequestedEvent());
                AddLogEntry("Player ended turn");
            }
        }

        private void OnCardButtonClicked(CardData card, int handIndex)
        {
            if (battleManager != null && battleManager.IsPlayerTurn)
            {
                EventBus.Publish(new PlayCardRequestedEvent
                {
                    Card = card,
                    HandIndex = handIndex
                });
            }
        }

        #endregion
    }
}
