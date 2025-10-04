using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using UnityEngine;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Manages card zones (Deck, Hand, Discard) for a single combatant in battle.
    /// Handles drawing, discarding, shuffling, and card zone queries.
    /// </summary>
    [Serializable]
    public class DeckManager
    {
        [Header("Card Zones")]
        [SerializeField] private List<CardData> _deck = new List<CardData>();
        [SerializeField] private List<CardData> _hand = new List<CardData>();
        [SerializeField] private List<CardData> _discard = new List<CardData>();
        [SerializeField] private List<CardData> _exhaust = new List<CardData>();

        [Header("Settings")]
        [SerializeField] private int _maxHandSize = 10;

        private string _ownerName; // For logging purposes

        #region Properties

        /// <summary>
        /// Number of cards remaining in the deck.
        /// </summary>
        public int DeckCount => _deck.Count;

        /// <summary>
        /// Number of cards currently in hand.
        /// </summary>
        public int HandCount => _hand.Count;

        /// <summary>
        /// Number of cards in the discard pile.
        /// </summary>
        public int DiscardCount => _discard.Count;

        /// <summary>
        /// Number of cards in the exhaust pile.
        /// </summary>
        public int ExhaustCount => _exhaust.Count;

        /// <summary>
        /// Cards currently in hand (read-only).
        /// </summary>
        public IReadOnlyList<CardData> Hand => _hand.AsReadOnly();

        /// <summary>
        /// Is the hand full?
        /// </summary>
        public bool IsHandFull => _hand.Count >= _maxHandSize;

        /// <summary>
        /// Is the deck empty?
        /// </summary>
        public bool IsDeckEmpty => _deck.Count == 0;

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new DeckManager with an initial deck of cards.
        /// </summary>
        public DeckManager(List<CardData> initialDeck, string ownerName = "Unknown", int maxHandSize = 10)
        {
            _ownerName = ownerName;
            _maxHandSize = maxHandSize;
            InitializeDeck(initialDeck);
        }

        /// <summary>
        /// Initializes the deck with cards and shuffles.
        /// </summary>
        public void InitializeDeck(List<CardData> cards)
        {
            _deck.Clear();
            _hand.Clear();
            _discard.Clear();
            _exhaust.Clear();

            _deck.AddRange(cards);
            Shuffle();

            GameLogger.LogInfo<DeckManager>($"{_ownerName} deck initialized with {_deck.Count} cards");
        }

        #endregion

        #region Drawing Cards

        /// <summary>
        /// Draws a specified number of cards from the deck to hand.
        /// Automatically shuffles discard pile into deck if needed.
        /// </summary>
        /// <param name="count">Number of cards to draw</param>
        /// <returns>Number of cards actually drawn</returns>
        public int DrawCards(int count)
        {
            int cardsDrawn = 0;

            for (int i = 0; i < count; i++)
            {
                if (DrawCard())
                {
                    cardsDrawn++;
                }
                else
                {
                    break; // Can't draw more cards
                }
            }

            if (cardsDrawn > 0)
            {
                GameLogger.LogInfo<DeckManager>($"{_ownerName} drew {cardsDrawn} card(s). Hand: {HandCount}/{_maxHandSize}");
            }

            return cardsDrawn;
        }

        /// <summary>
        /// Draws a single card from the deck.
        /// </summary>
        /// <returns>True if a card was drawn, false if hand is full or no cards available</returns>
        public bool DrawCard()
        {
            // Check if hand is full
            if (IsHandFull)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot draw - hand is full ({_maxHandSize})");
                return false;
            }

            // If deck is empty, try to shuffle discard pile
            if (IsDeckEmpty)
            {
                if (_discard.Count > 0)
                {
                    ShuffleDiscardIntoDeck();
                }
                else
                {
                    GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot draw - deck and discard are empty");
                    return false;
                }
            }

            // Draw from top of deck
            CardData drawnCard = _deck[0];
            _deck.RemoveAt(0);
            _hand.Add(drawnCard);

            return true;
        }

        #endregion

        #region Playing Cards

        /// <summary>
        /// Plays a card from hand (moves it to discard pile).
        /// </summary>
        /// <param name="card">The card to play</param>
        /// <returns>True if card was successfully played</returns>
        public bool PlayCard(CardData card)
        {
            if (!_hand.Contains(card))
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot play card - not in hand");
                return false;
            }

            _hand.Remove(card);
            _discard.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} played card: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Plays a card by index in hand.
        /// </summary>
        public bool PlayCardAtIndex(int index)
        {
            if (index < 0 || index >= _hand.Count)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} invalid card index: {index}");
                return false;
            }

            CardData card = _hand[index];
            return PlayCard(card);
        }

        #endregion

        #region Discarding Cards

        /// <summary>
        /// Discards a card from hand without playing it.
        /// </summary>
        public bool DiscardCard(CardData card)
        {
            if (!_hand.Contains(card))
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot discard card - not in hand");
                return false;
            }

            _hand.Remove(card);
            _discard.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} discarded card: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Discards entire hand.
        /// </summary>
        public int DiscardHand()
        {
            int count = _hand.Count;
            _discard.AddRange(_hand);
            _hand.Clear();

            GameLogger.LogInfo<DeckManager>($"{_ownerName} discarded entire hand ({count} cards)");
            return count;
        }

        #endregion

        #region Exhausting Cards

        /// <summary>
        /// Exhausts a card (removes it from play until end of battle).
        /// </summary>
        public bool ExhaustCard(CardData card)
        {
            if (!_hand.Contains(card))
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot exhaust card - not in hand");
                return false;
            }

            _hand.Remove(card);
            _exhaust.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} exhausted card: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Exhausts a card from discard pile.
        /// </summary>
        public bool ExhaustFromDiscard(CardData card)
        {
            if (!_discard.Contains(card))
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot exhaust card - not in discard");
                return false;
            }

            _discard.Remove(card);
            _exhaust.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} exhausted card from discard: {card.CardName}");
            return true;
        }

        #endregion

        #region Shuffling

        /// <summary>
        /// Shuffles the deck.
        /// </summary>
        public void Shuffle()
        {
            if (_deck.Count <= 1)
                return;

            // Fisher-Yates shuffle
            for (int i = _deck.Count - 1; i > 0; i--)
            {
                int j = RandomHelper.Range(0, i + 1);
                CardData temp = _deck[i];
                _deck[i] = _deck[j];
                _deck[j] = temp;
            }

            GameLogger.LogInfo<DeckManager>($"{_ownerName} shuffled deck ({_deck.Count} cards)");
        }

        /// <summary>
        /// Moves all cards from discard pile to deck and shuffles.
        /// </summary>
        public void ShuffleDiscardIntoDeck()
        {
            if (_discard.Count == 0)
                return;

            int count = _discard.Count;
            _deck.AddRange(_discard);
            _discard.Clear();
            Shuffle();

            GameLogger.LogInfo<DeckManager>($"{_ownerName} shuffled {count} cards from discard into deck");
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets a card from hand by index.
        /// </summary>
        public CardData GetCardInHand(int index)
        {
            if (index < 0 || index >= _hand.Count)
                return null;

            return _hand[index];
        }

        /// <summary>
        /// Checks if a specific card is in hand.
        /// </summary>
        public bool IsCardInHand(CardData card)
        {
            return _hand.Contains(card);
        }

        /// <summary>
        /// Gets all cards in hand of a specific type.
        /// </summary>
        public List<CardData> GetCardsOfType(CardType cardType)
        {
            return _hand.Where(c => c.CardType == cardType).ToList();
        }

        /// <summary>
        /// Gets all cards in hand with a specific tag.
        /// </summary>
        public List<CardData> GetCardsWithTag(string tag)
        {
            return _hand.Where(c => c.HasTag(tag)).ToList();
        }

        #endregion

        #region Battle Lifecycle

        /// <summary>
        /// Called at the start of battle - draws initial hand.
        /// </summary>
        public void StartBattle(int initialHandSize)
        {
            DrawCards(initialHandSize);
            GameLogger.LogInfo<DeckManager>($"{_ownerName} drew initial hand of {initialHandSize} cards");
        }

        /// <summary>
        /// Called at the start of each turn.
        /// </summary>
        public void StartTurn(int cardsToDraw)
        {
            DrawCards(cardsToDraw);
        }

        /// <summary>
        /// Called at the end of each turn - discards hand.
        /// </summary>
        public void EndTurn()
        {
            DiscardHand();
        }

        /// <summary>
        /// Called at the end of battle - cleanup.
        /// </summary>
        public void EndBattle()
        {
            GameLogger.LogInfo<DeckManager>($"{_ownerName} battle ended. Exhausted: {_exhaust.Count} cards");
        }

        #endregion

        #region Debugging

        /// <summary>
        /// Gets a summary of all card zones.
        /// </summary>
        public string GetStatusString()
        {
            return $"Deck: {DeckCount} | Hand: {HandCount}/{_maxHandSize} | Discard: {DiscardCount} | Exhaust: {ExhaustCount}";
        }

        #endregion
    }
}
