using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Data.Cards;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Panel that floats above the existing hand and shows cards queued for discard.
    ///
    /// The actual hand (CardHandLayout) remains visible below and is the source.
    /// BattleUI calls <see cref="AddToDiscard"/> when a hand card is clicked in improvise mode.
    /// Clicking a card in the discard zone fires <see cref="OnCardReturnedToHand"/> so
    /// BattleUI can rebuild the hand to show the card again.
    ///
    /// Usage:
    ///   panel.Open("Improvise", OnImproviseConfirmed);
    ///   // Then wire hand card callbacks to: panel.AddToDiscard(card)
    ///
    /// Attach to a Panel anchored above the hand area. Starts inactive (hidden).
    /// discardContainer should use HorizontalLayoutGroup.
    /// </summary>
    public class CardSelectionPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text   titleText;
        [SerializeField] private TMP_Text   selectionCountText;  // e.g. "2 to discard"
        [SerializeField] private Transform  discardContainer;    // cards queued for discard
        [SerializeField] private Button     discardButton;       // executes the discard

        [Header("Fallback Prefabs (used only when BattlePoolManager is not injected)")]
        [SerializeField] private CardButton _pressurePrefab;
        [SerializeField] private CardButton _rhetoricPrefab;
        [SerializeField] private CardButton _policyPrefab;

        // ─── Events ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when the player clicks a card in the discard zone to return it to hand.
        /// BattleUI should respond by calling RefreshImproviseHand() to show the card again.
        /// </summary>
        public event Action<CardData> OnCardReturnedToHand;

        // ─── Per-session state ────────────────────────────────────────────────────

        private readonly List<CardData>   _selectedCards  = new List<CardData>();
        private readonly List<CardButton> _spawnedButtons = new List<CardButton>();

        private Action<List<CardData>> _onDiscard;
        private BattlePoolManager      _pool;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            discardButton?.onClick.AddListener(OnDiscardClicked);
            gameObject.SetActive(false);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Sets the shared pool. Call from BattleUI.Initialize() before the first Open.</summary>
        public void SetPool(BattlePoolManager pool) => _pool = pool;

        /// <summary>
        /// Opens the panel with an empty discard zone.
        /// BattleUI is responsible for rebuilding the hand with AddToDiscard callbacks.
        /// </summary>
        /// <param name="title">Panel header shown above the discard zone (e.g. "Improvise").</param>
        /// <param name="onDiscard">Called with the queued card list when the player presses Discard. Empty list = skip.</param>
        public void Open(string title, Action<List<CardData>> onDiscard)
        {
            _onDiscard = onDiscard;

            if (titleText != null) titleText.text = title;

            ClearDiscardZone();
            RefreshUI();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called by BattleUI when a hand card is clicked in improvise mode.
        /// Spawns a CardButton in the discard zone; clicking it fires OnCardReturnedToHand.
        /// </summary>
        public void AddToDiscard(CardData card)
        {
            if (card == null) return;

            _selectedCards.Add(card);

            CardButton button = _pool != null
                ? _pool.RentCard(card.CardType, discardContainer)
                : InstantiateFallback(card.CardType);
            if (button == null) { _selectedCards.Remove(card); return; }
            // int.MaxValue AP → card displays as affordable (no grey tint)
            // Callback → return this card to hand when clicked
            int baseCost = card.Costs != null && card.Costs.Count > 0 ? card.Costs[0].BaseAmount : 0;
            button.Initialize(card, _spawnedButtons.Count, int.MaxValue, baseCost,
                onClick: () => ReturnToHand(button, card));

            _spawnedButtons.Add(button);

            // Force the layout group to position this button immediately so
            // SetBasePosition() receives the correct slot position (not 0,0).
            // This is cheap — called once per card added, not per frame.
            Canvas.ForceUpdateCanvases();
            button.SetBasePosition(button.transform.localPosition);

            RefreshUI();
        }

        /// <summary>Cards currently queued for discard. Read by BattleUI to exclude them from the hand display.</summary>
        public IReadOnlyList<CardData> SelectedForDiscard => _selectedCards;

        public void Close()
        {
            ClearDiscardZone();
            gameObject.SetActive(false);
        }

        // ─── Internal ─────────────────────────────────────────────────────────────

        private void ReturnToHand(CardButton button, CardData card)
        {
            _selectedCards.Remove(card);
            _spawnedButtons.Remove(button);
            if (_pool != null) _pool.ReturnCard(button);
            else Destroy(button.gameObject);
            OnCardReturnedToHand?.Invoke(card);
            RefreshUI();
        }

        private void OnDiscardClicked()
        {
            var toDiscard = new List<CardData>(_selectedCards);
            var callback  = _onDiscard;
            Close();
            callback?.Invoke(toDiscard);
        }

        private void RefreshUI()
        {
            if (selectionCountText != null)
                selectionCountText.text = $"{_selectedCards.Count} to discard";
        }

        private void ClearDiscardZone()
        {
            foreach (var button in _spawnedButtons)
            {
                if (button == null) continue;
                if (_pool != null) _pool.ReturnCard(button);
                else Destroy(button.gameObject);
            }

            _spawnedButtons.Clear();
            _selectedCards.Clear();
        }

        private CardButton InstantiateFallback(CardType cardType)
        {
            CardButton prefab = cardType switch
            {
                CardType.Rhetoric => _rhetoricPrefab,
                CardType.Policy   => _policyPrefab,
                _                 => _pressurePrefab,
            };
            if (prefab == null) return null;
            return Instantiate(prefab, discardContainer);
        }
    }
}
