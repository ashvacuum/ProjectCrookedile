using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Data.Cards;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Reusable multi-select card picker panel with a two-zone reparenting UX.
    ///
    /// Cards start in the <b>Keep</b> zone. Clicking a card moves it to the <b>Discard</b> zone;
    /// clicking it again moves it back. Physical position is the selection state — no outline needed.
    ///
    /// Usage:
    ///   cardSelectionPanel.Open(title, instruction, cards, minSelect, maxSelect, onConfirm);
    ///
    /// No cancel button — the panel closes only via Confirm. Pass minSelect: 0 to allow
    /// confirming with no cards selected (treated as a skip by the caller).
    ///
    /// Attach to a full-screen Panel in the Canvas. Starts inactive (hidden).
    /// Both handContainer and discardContainer should use HorizontalLayoutGroup.
    /// </summary>
    public class CardSelectionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text   titleText;
        [SerializeField] private TMP_Text   instructionText;
        [SerializeField] private TMP_Text   selectionCountText;  // e.g. "2 to discard"
        [SerializeField] private Transform  handContainer;       // cards to keep — all cards spawn here
        [SerializeField] private Transform  discardContainer;    // cards to discard — starts empty
        [SerializeField] private CardButton cardPrefab;
        [SerializeField] private Button     confirmButton;

        // Optional cosmetic zone labels — wire to header TMP_Text objects in Inspector
        [SerializeField] private TMP_Text   handLabel;           // e.g. "Keep"
        [SerializeField] private TMP_Text   discardLabel;        // e.g. "Discard"

        // ─── Per-session state ────────────────────────────────────────────────────

        private readonly List<CardData>                   _selectedCards  = new List<CardData>();
        private readonly List<CardButton>                 _spawnedButtons = new List<CardButton>();
        private readonly Dictionary<CardButton, CardData> _cardButtonMap  = new Dictionary<CardButton, CardData>();

        private int                    _minSelect;
        private int                    _maxSelect;
        private Action<List<CardData>> _onConfirm;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            gameObject.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the panel and populates the Keep zone with the given cards.
        /// </summary>
        /// <param name="title">Panel header (e.g. "Improvise").</param>
        /// <param name="instruction">Subtitle instruction shown under the title.</param>
        /// <param name="cards">Cards to display — all start in the Keep zone.</param>
        /// <param name="minSelect">Minimum cards that must be in the Discard zone to confirm. 0 = always confirmable (skip).</param>
        /// <param name="maxSelect">Maximum cards that can be moved to the Discard zone at once.</param>
        /// <param name="onConfirm">Called with the discard-zone card list when confirmed. Empty list = player skipped.</param>
        public void Open(
            string                  title,
            string                  instruction,
            IReadOnlyList<CardData> cards,
            int                     minSelect,
            int                     maxSelect,
            Action<List<CardData>>  onConfirm)
        {
            _minSelect = minSelect;
            _maxSelect = maxSelect;
            _onConfirm = onConfirm;

            if (titleText       != null) titleText.text       = title;
            if (instructionText != null) instructionText.text = instruction;

            ClearSpawnedCards();

            // All cards spawn into handContainer (Keep zone)
            for (int i = 0; i < cards.Count; i++)
            {
                CardData   card   = cards[i];
                CardButton button = Instantiate(cardPrefab, handContainer);

                // int.MaxValue AP → all cards display as affordable (no grey tint)
                // Callback → move card between zones on click
                int capturedIndex = i;
                button.Initialize(card, capturedIndex, int.MaxValue, () => ToggleCard(button, card));

                _spawnedButtons.Add(button);
                _cardButtonMap[button] = card;
            }

            RefreshUI();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            ClearSpawnedCards();
            gameObject.SetActive(false);
        }

        // ─── Selection Logic ──────────────────────────────────────────────────────

        private void ToggleCard(CardButton button, CardData card)
        {
            if (_selectedCards.Contains(card))
            {
                // Card is in discardContainer — move it back to handContainer (Keep)
                _selectedCards.Remove(card);
                button.transform.SetParent(handContainer, false);
            }
            else if (_selectedCards.Count < _maxSelect)
            {
                // Card is in handContainer — move it to discardContainer (Discard)
                _selectedCards.Add(card);
                button.transform.SetParent(discardContainer, false);
            }
            // If already at maxSelect, clicking an unselected card does nothing

            // worldPositionStays: false — both containers have layout groups,
            // so Unity reflows immediately. 'true' would fight the layout and cause jitter.

            RefreshUI();
        }

        private void RefreshUI()
        {
            if (selectionCountText != null)
                selectionCountText.text = $"{_selectedCards.Count} to discard";

            if (confirmButton != null)
                confirmButton.interactable = _selectedCards.Count >= _minSelect;
        }

        // ─── Button Handlers ──────────────────────────────────────────────────────

        private void OnConfirmClicked()
        {
            if (_selectedCards.Count < _minSelect) return;

            var confirmed = new List<CardData>(_selectedCards);
            var callback  = _onConfirm;
            Close();
            callback?.Invoke(confirmed);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void ClearSpawnedCards()
        {
            foreach (var button in _spawnedButtons)
                if (button != null) Destroy(button.gameObject);

            _spawnedButtons.Clear();
            _cardButtonMap.Clear();
            _selectedCards.Clear();

            // Defensive pass: destroy any children that survived edge cases
            // (e.g. panel reused mid-battle while cards were mid-drag)
            if (handContainer    != null) foreach (Transform child in handContainer)    Destroy(child.gameObject);
            if (discardContainer != null) foreach (Transform child in discardContainer) Destroy(child.gameObject);
        }
    }
}
