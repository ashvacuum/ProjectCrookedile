using UnityEngine;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using Crookedile.Data.Cards;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Bridges the 3D CardHandManager with the BattleManager's logical DeckManager.
    /// Syncs visual card display with actual battle state.
    /// </summary>
    public class BattleCardHandBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CardHandManager cardHandManager;
        [SerializeField] private BattleManager battleManager;

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
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        }

        /// <summary>
        /// Initialize with BattleManager reference.
        /// </summary>
        public void Initialize(BattleManager manager)
        {
            battleManager = manager;
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            // Draw initial hand
            SyncHandWithDeckManager();
        }

        private void OnTurnStarted(TurnStartedEvent evt)
        {
            // Only show cards during player's turn
            if (evt.IsPlayerTurn)
            {
                SyncHandWithDeckManager();
            }
            else
            {
                ClearHand();
            }
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            if (evt.IsPlayer)
            {
                // Remove the card from visual hand
                RemoveCardFromHand(evt.Card);
            }
        }

        /// <summary>
        /// Syncs the 3D card hand display with BattleManager's DeckManager.
        /// </summary>
        private void SyncHandWithDeckManager()
        {
            if (battleManager == null || cardHandManager == null) return;

            // Clear current visual hand
            ClearHand();

            // Get cards from DeckManager
            var hand = battleManager.PlayerDeck.Hand;

            // Create 3D card views for each card in hand
            foreach (CardData card in hand)
            {
                cardHandManager.DrawCard(card);
            }
        }

        /// <summary>
        /// Removes a specific card from the visual hand.
        /// </summary>
        private void RemoveCardFromHand(CardData cardData)
        {
            if (cardHandManager == null) return;

            // Find the card in the hand
            var cardsInHand = cardHandManager.CardsInHand;
            for (int i = cardsInHand.Count - 1; i >= 0; i--)
            {
                if (cardsInHand[i].CardData == cardData)
                {
                    cardHandManager.DiscardCard(cardsInHand[i]);
                    break;
                }
            }
        }

        /// <summary>
        /// Clears all cards from visual hand (opponent's turn).
        /// </summary>
        private void ClearHand()
        {
            if (cardHandManager == null) return;

            // Remove all cards from visual display
            var cardsInHand = cardHandManager.CardsInHand;
            for (int i = cardsInHand.Count - 1; i >= 0; i--)
            {
                cardHandManager.DiscardCard(cardsInHand[i]);
            }
        }
    }
}
