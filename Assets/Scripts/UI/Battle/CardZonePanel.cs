using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Shared overlay panel for viewing a card zone (Discard, Exhaust, or Draw pile).
    /// Spawns fully rendered CardButton instances in display-only (non-interactive) mode.
    /// Attach to a full-screen Panel in the Canvas; starts disabled.
    /// The cardContainer should have a GridLayoutGroup for card sizing and wrapping.
    /// </summary>
    public class CardZonePanel : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text countText;

        [SerializeField]
        private TMP_Text emptyLabel;

        [SerializeField]
        private Transform cardContainer; // Child Content with GridLayoutGroup

        [SerializeField]
        private Button closeButton;

        [Header("Fallback Prefabs (used only when BattlePoolManager singleton is absent)")]
        [SerializeField]
        private CardButton _pressurePrefab;

        [SerializeField]
        private CardButton _rhetoricPrefab;

        [SerializeField]
        private CardButton _policyPrefab;

        #region Runtime
        private readonly List<CardButton> _spawnedButtons = new List<CardButton>();

        #endregion

        #region Lifecycle
        private void Awake()
        {
            closeButton?.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Populates and shows the panel for the given card list.
        /// Pass cards in oldest-first order; the panel displays them newest-first (reversed).
        /// For the draw pile pass a pre-shuffled display copy so real draw order stays hidden.
        /// </summary>
        public void Open(string title, IReadOnlyList<CardData> cards)
        {
            if (titleText != null)
                titleText.text = title;
            if (countText != null)
                countText.text = $"({cards.Count})";

            // Return any previously displayed cards before spawning new ones
            ClearCards();

            bool isEmpty = cards.Count == 0;
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(isEmpty);

            // Newest-first: iterate backwards
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CardData cardData = cards[i];
                CardButton card =
                    BattlePoolManager.Instance != null
                        ? BattlePoolManager.Instance.RentCard(cardData.CardType, cardContainer)
                        : InstantiateFallback(cardData.CardType);

                if (card == null)
                    continue;

                // int.MaxValue AP → all cards show as "affordable" (no grey tint)
                // null callback  → nothing fires if interaction somehow reaches the card
                int baseCost =
                    cardData.Costs != null && cardData.Costs.Count > 0
                        ? cardData.Costs[0].BaseAmount
                        : 0;
                card.Initialize(cardData, i, int.MaxValue, baseCost);

                // Kill all interaction — hover scale, drag, click
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }

                _spawnedButtons.Add(card);
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            ClearCards();
            gameObject.SetActive(false);
        }

        #endregion

        #region Internal
        private void ClearCards()
        {
            foreach (var btn in _spawnedButtons)
            {
                if (btn == null)
                    continue;
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnCard(btn);
                else
                    Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();
        }

        private CardButton InstantiateFallback(CardType cardType)
        {
            CardButton prefab = cardType switch
            {
                CardType.Rhetoric => _rhetoricPrefab,
                CardType.Policy => _policyPrefab,
                _ => _pressurePrefab,
            };
            if (prefab == null)
                return null;
            return Instantiate(prefab, cardContainer);
        }
    }
}
        #endregion
