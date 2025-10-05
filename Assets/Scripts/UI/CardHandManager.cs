using UnityEngine;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using MoreMountains.Feedbacks;

namespace Crookedile.UI
{
    /// <summary>
    /// Manages card hand - drawing, selecting, and discarding cards
    /// </summary>
    public class CardHandManager : MonoBehaviour
    {
        [Header("Card Setup")]
        [SerializeField] private Card3DView cardPrefab;
        [SerializeField] private Transform handTransform;
        [SerializeField] private Transform deckPosition;
        [SerializeField] private Transform discardPosition;

        [Header("Hand Layout")]
        [SerializeField] private float cardSpacing = 2f;
        [SerializeField] private float cardArcHeight = 0.5f;
        [SerializeField] private float cardRotationAngle = 5f;
        [SerializeField] private float hoverLiftHeight = 1f;

        [Header("Test Deck")]
        [SerializeField] private List<CardData> testDeck = new List<CardData>();

        [Header("Feedbacks")]
        [SerializeField] private MMFeedbacks handReorganizeFeedback;

        private List<Card3DView> cardsInHand = new List<Card3DView>();
        private Card3DView hoveredCard;
        private Card3DView selectedCard;

        public List<Card3DView> CardsInHand => cardsInHand;

        // Input handling moved to CardInputHandler for New Input System support

        public void DrawCard(CardData cardData = null)
        {
            // If no card data provided, draw from test deck
            if (cardData == null && testDeck.Count > 0)
            {
                int randomIndex = Random.Range(0, testDeck.Count);
                cardData = testDeck[randomIndex];
            }

            if (cardData == null) return;

            Card3DView newCard = Instantiate(cardPrefab, deckPosition.position, Quaternion.identity, handTransform);
            newCard.Initialize(cardData);
            newCard.OnDrawn();

            cardsInHand.Add(newCard);
            ReorganizeHand();
        }

        public void SelectCard(Card3DView card)
        {
            if (selectedCard == card) return;

            selectedCard?.OnDeselected();
            selectedCard = card;
            selectedCard.OnSelected();
        }

        public void DiscardCard(Card3DView card)
        {
            if (!cardsInHand.Contains(card)) return;

            cardsInHand.Remove(card);
            card.OnDiscarded();

            // Destroy after discard animation completes
            Destroy(card.gameObject, 1f);

            if (selectedCard == card)
                selectedCard = null;
            if (hoveredCard == card)
                hoveredCard = null;

            ReorganizeHand();
        }

        public void DiscardSelectedCard()
        {
            if (selectedCard != null)
            {
                DiscardCard(selectedCard);
            }
        }

        private void ReorganizeHand()
        {
            handReorganizeFeedback?.PlayFeedbacks();

            int cardCount = cardsInHand.Count;
            if (cardCount == 0) return;

            for (int i = 0; i < cardCount; i++)
            {
                Vector3 targetPosition = GetCardPositionInHand(i, cardCount);
                Quaternion targetRotation = GetCardRotationInHand(i, cardCount);

                // TODO: Animate to position using MMFeedbacks or tweening
                // For now, just set directly
                cardsInHand[i].transform.localPosition = targetPosition;
                cardsInHand[i].transform.localRotation = targetRotation;
            }
        }

        private Vector3 GetCardPositionInHand(int index, int totalCards)
        {
            float centerOffset = (totalCards - 1) * cardSpacing * 0.5f;
            float xPos = index * cardSpacing - centerOffset;

            // Arc calculation
            float normalizedPos = (float)index / (totalCards - 1);
            if (totalCards == 1) normalizedPos = 0.5f;

            float arcProgress = normalizedPos * 2f - 1f; // -1 to 1
            float yPos = -Mathf.Abs(arcProgress) * cardArcHeight;

            return new Vector3(xPos, yPos, 0);
        }

        private Quaternion GetCardRotationInHand(int index, int totalCards)
        {
            float centerOffset = (totalCards - 1) * 0.5f;
            float rotationMultiplier = index - centerOffset;
            float zRotation = rotationMultiplier * cardRotationAngle;

            return Quaternion.Euler(0, 0, -zRotation);
        }

        // Test functions - hook these up to UI buttons
        [ContextMenu("Draw Card")]
        public void TestDrawCard()
        {
            DrawCard();
        }

        [ContextMenu("Discard Selected")]
        public void TestDiscardSelected()
        {
            DiscardSelectedCard();
        }

        [ContextMenu("Clear Hand")]
        public void TestClearHand()
        {
            foreach (var card in cardsInHand)
            {
                Destroy(card.gameObject);
            }
            cardsInHand.Clear();
            selectedCard = null;
            hoveredCard = null;
        }

        /// <summary>
        /// Refreshes all card visuals in hand (useful when costs change due to buffs/debuffs).
        /// </summary>
        public void RefreshAllCardVisuals()
        {
            foreach (var card in cardsInHand)
            {
                card.RefreshVisuals();
            }
        }

        /// <summary>
        /// Called at the start of each turn to update turn-based cost modifiers.
        /// </summary>
        public void OnTurnStart()
        {
            foreach (var card in cardsInHand)
            {
                // Update turn counter for each cost on each card
                var costs = card.CardData.GetCosts();
                if (costs != null)
                {
                    foreach (var cost in costs)
                    {
                        cost.OnTurnInHand();
                    }
                }
            }

            // Refresh visuals to show updated costs
            RefreshAllCardVisuals();
        }
    }
}
