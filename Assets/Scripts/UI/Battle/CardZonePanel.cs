using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Data.Cards;

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
        [SerializeField] private TMP_Text   titleText;
        [SerializeField] private TMP_Text   countText;
        [SerializeField] private TMP_Text   emptyLabel;
        [SerializeField] private Transform  cardContainer;  // Child Content with GridLayoutGroup
        [SerializeField] private CardButton cardPrefab;     // Assign CardPrefab in Inspector
        [SerializeField] private Button     closeButton;

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
            if (titleText != null) titleText.text = title;
            if (countText != null) countText.text = $"({cards.Count})";

            // Clear previous cards
            foreach (Transform child in cardContainer)
                Destroy(child.gameObject);

            bool isEmpty = cards.Count == 0;
            if (emptyLabel != null) emptyLabel.gameObject.SetActive(isEmpty);

            // Newest-first: iterate backwards
            for (int i = cards.Count - 1; i >= 0; i--)
            {
                CardButton card = Instantiate(cardPrefab, cardContainer);

                // int.MaxValue AP → all cards show as "affordable" (no grey tint)
                // null callback  → nothing fires if interaction somehow reaches the card
                card.Initialize(cards[i], i, int.MaxValue, null);

                // Kill all interaction — hover scale, drag, click
                CanvasGroup cg = card.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.interactable   = false;
                    cg.blocksRaycasts = false;
                }
            }

            gameObject.SetActive(true);
        }

        public void Close() => gameObject.SetActive(false);
    }
}
