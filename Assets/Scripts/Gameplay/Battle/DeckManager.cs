using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Crookedile.Data;
using UnityEngine;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using EventBus = Crookedile.Core.EventBus;

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
        [SerializeField] private List<CardData> _deck    = new List<CardData>();
        [SerializeField] private List<CardData> _hand    = new List<CardData>();
        [SerializeField] private List<CardData> _discard = new List<CardData>();
        [SerializeField] private List<CardData> _exhaust = new List<CardData>();

        [Header("Settings")]
        [SerializeField] private int _maxHandSize = 10;

        private string _ownerName; // For logging purposes
        private bool   _isPlayer;  // True for the player's deck; false for enemies

        // Cards retained at end of turn (not discarded; cleared after each EndTurn).
        private readonly HashSet<CardData> _retainedCards = new HashSet<CardData>();

        // Per-card AP cost reductions applied this battle (individual card effects).
        // int.MaxValue signals "free" (0 AP regardless of base cost).
        private readonly Dictionary<CardData, int> _cardCostReductions = new Dictionary<CardData, int>();

        // Cached ReadOnlyCollection wrappers — live views of the underlying lists.
        // AsReadOnly() returns a wrapper that reflects the list directly, so we only
        // need to create it once and reuse the same instance on every property access.
        private ReadOnlyCollection<CardData> _handReadOnly;
        private ReadOnlyCollection<CardData> _discardReadOnly;
        private ReadOnlyCollection<CardData> _exhaustReadOnly;
        private ReadOnlyCollection<CardData> _deckReadOnly;

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
        /// Cards currently in hand (read-only live view).
        /// </summary>
        public IReadOnlyList<CardData> Hand => _handReadOnly;

        /// <summary>
        /// Is the hand full?
        /// </summary>
        public bool IsHandFull => _hand.Count >= _maxHandSize;

        /// <summary>
        /// Is the deck empty?
        /// </summary>
        public bool IsDeckEmpty => _deck.Count == 0;

        /// <summary>
        /// Cards currently in the discard pile (read-only live view). Ordered oldest-to-newest.
        /// </summary>
        public IReadOnlyList<CardData> DiscardPile => _discardReadOnly;

        /// <summary>
        /// Cards currently in the exhaust pile (read-only live view). Ordered oldest-to-newest.
        /// </summary>
        public IReadOnlyList<CardData> ExhaustPile => _exhaustReadOnly;

        /// <summary>
        /// Cards currently in the draw pile (read-only live view). Order reflects the live shuffle.
        /// </summary>
        public IReadOnlyList<CardData> DrawPile => _deckReadOnly;

        /// <summary>
        /// All cards across every zone — draw pile, hand, discard, and exhaust (snapshot).
        /// Order: draw → hand → discard → exhaust.
        /// Note: allocates a new list; use the individual zone properties for repeated access.
        /// </summary>
        public IReadOnlyList<CardData> AllCards =>
            _deck.Concat(_hand).Concat(_discard).Concat(_exhaust).ToList().AsReadOnly();

        #endregion

        #region Initialization

        /// <summary>
        /// Creates a new DeckManager with an initial deck of cards.
        /// </summary>
        public DeckManager(List<CardData> initialDeck, string ownerName = "Unknown", int maxHandSize = 10, bool isPlayer = true)
        {
            _ownerName   = ownerName;
            _maxHandSize = maxHandSize;
            _isPlayer    = isPlayer;

            // Create cached wrappers once — they track the underlying list by reference.
            _handReadOnly    = _hand.AsReadOnly();
            _discardReadOnly = _discard.AsReadOnly();
            _exhaustReadOnly = _exhaust.AsReadOnly();
            _deckReadOnly    = _deck.AsReadOnly();

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
                    cardsDrawn++;
                else
                    break; // Can't draw more cards
            }

            if (cardsDrawn > 0)
                GameLogger.LogInfo<DeckManager>($"{_ownerName} drew {cardsDrawn} card(s). Hand: {HandCount}/{_maxHandSize}");

            return cardsDrawn;
        }

        /// <summary>
        /// Draws a single card from the deck.
        /// </summary>
        /// <returns>True if a card was drawn, false if hand is full or no cards available</returns>
        public bool DrawCard()
        {
            if (IsHandFull)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot draw - hand is full ({_maxHandSize})");
                return false;
            }

            if (IsDeckEmpty)
            {
                if (_discard.Count > 0)
                    ShuffleDiscardIntoDeck();
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

            EventBus.Publish(new CardDrawnEvent { Card = drawnCard, IsPlayer = _isPlayer });
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
            int idx = _hand.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot play card - not in hand");
                return false;
            }

            _hand.RemoveAt(idx);
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
            _hand.RemoveAt(index);
            _discard.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} played card: {card.CardName}");
            return true;
        }

        #endregion

        #region Discarding Cards

        /// <summary>
        /// Discards a card from hand without playing it.
        /// </summary>
        public bool DiscardCard(CardData card)
        {
            int idx = _hand.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot discard card - not in hand");
                return false;
            }

            _hand.RemoveAt(idx);
            _discard.Add(card);

            EventBus.Publish(new CardDiscardedEvent { Card = card, IsPlayer = _isPlayer });
            GameLogger.LogInfo<DeckManager>($"{_ownerName} discarded card: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Discards the hand at end of turn, skipping retained cards.
        /// Retained cards stay in hand for the next turn; their retain flag is cleared so they
        /// discard normally next end-of-turn unless retained again.
        /// </summary>
        public List<CardData> DiscardHand()
        {
            var discarded = new List<CardData>();
            for (int i = _hand.Count - 1; i >= 0; i--)
            {
                CardData card = _hand[i];
                if (_retainedCards.Contains(card)) continue;   // skip — stays in hand
                _hand.RemoveAt(i);
                _discard.Add(card);
                discarded.Add(card);
                EventBus.Publish(new CardDiscardedEvent { Card = card, IsPlayer = _isPlayer });
            }
            _retainedCards.Clear();   // retain only lasts one turn

            GameLogger.LogInfo<DeckManager>($"{_ownerName} discarded {discarded.Count} card(s). " +
                                             $"Hand retained: {_hand.Count}");
            return discarded;
        }

        #endregion

        #region Exhausting Cards

        /// <summary>
        /// Exhausts a card (removes it from play until end of battle).
        /// </summary>
        public bool ExhaustCard(CardData card)
        {
            int idx = _hand.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot exhaust card - not in hand");
                return false;
            }

            _hand.RemoveAt(idx);
            _exhaust.Add(card);

            EventBus.Publish(new CardExhaustedEvent { Card = card, IsPlayer = _isPlayer });
            GameLogger.LogInfo<DeckManager>($"{_ownerName} exhausted card: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Exhausts a card from discard pile.
        /// </summary>
        public bool ExhaustFromDiscard(CardData card)
        {
            int idx = _discard.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot exhaust card - not in discard");
                return false;
            }

            _discard.RemoveAt(idx);
            _exhaust.Add(card);

            GameLogger.LogInfo<DeckManager>($"{_ownerName} exhausted card from discard: {card.CardName}");
            return true;
        }

        /// <summary>
        /// Moves a specific card from the discard pile directly into the hand.
        /// Used by ChooseFromDiscardToHand effects and CardSelectionPanel confirm callbacks.
        /// Returns false if the card is not in discard or the hand is full.
        /// </summary>
        public bool MoveFromDiscardToHand(CardData card)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: MoveFromDiscardToHand — card is null");
                return false;
            }
            int idx = _discard.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: MoveFromDiscardToHand — card not in discard");
                return false;
            }
            if (IsHandFull)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: MoveFromDiscardToHand — hand is full");
                return false;
            }
            _discard.RemoveAt(idx);
            _hand.Add(card);
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: Moved {card.CardName} from discard to hand");
            EventBus.Publish(new CardRecoveredEvent { Card = card, IsPlayer = _isPlayer });
            return true;
        }

        /// <summary>
        /// Moves a specific card from the discard pile to the TOP of the draw pile.
        /// Top of deck = index 0 (DrawCard pulls from _deck[0]).
        /// Returns false if the card is not in discard.
        /// </summary>
        public bool MoveFromDiscardToDeck(CardData card)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: MoveFromDiscardToDeck — card is null");
                return false;
            }
            int idx = _discard.IndexOf(card);
            if (idx < 0)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: MoveFromDiscardToDeck — card not in discard");
                return false;
            }
            _discard.RemoveAt(idx);
            _deck.Insert(0, card);   // index 0 = top (DrawCard reads _deck[0])
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: Moved {card.CardName} from discard to top of draw pile");
            return true;
        }

        /// <summary>
        /// Swaps <paramref name="oldCard"/> for <paramref name="newCard"/> in hand in-place.
        /// Used for UpgradeCardThisBattle to swap in the upgraded version.
        /// Returns false if oldCard is not currently in hand.
        /// </summary>
        public bool SwapCardInHand(CardData oldCard, CardData newCard)
        {
            int idx = _hand.IndexOf(oldCard);
            if (idx < 0 || newCard == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: SwapCardInHand — card not found in hand or newCard is null");
                return false;
            }
            _hand[idx] = newCard;
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: Upgraded {oldCard.CardName} → {newCard.CardName} in hand");
            EventBus.Publish(new CardUpgradedEvent { OldCard = oldCard, NewCard = newCard, IsPlayer = _isPlayer });
            return true;
        }

        /// <summary>
        /// Marks a card currently in hand as retained.
        /// Retained cards are not discarded at end of turn; the flag clears after EndTurn.
        /// </summary>
        public bool RetainCard(CardData card)
        {
            if (!_hand.Contains(card))
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName}: RetainCard — {card?.CardName} is not in hand");
                return false;
            }
            _retainedCards.Add(card);
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: {card.CardName} marked as retained");
            EventBus.Publish(new CardRetainedEvent { Card = card, IsPlayer = _isPlayer });
            return true;
        }

        #endregion

        #region Per-Card Cost Overrides

        /// <summary>
        /// Reduces this card's AP cost by <paramref name="reduction"/> for the rest of the battle.
        /// Reductions stack additively; the floor applied at play time is 0.
        /// </summary>
        public void ApplyCostReduction(CardData card, int reduction)
        {
            if (card == null) return;
            _cardCostReductions.TryGetValue(card, out int current);
            _cardCostReductions[card] = current + reduction;
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: {card.CardName} cost −{reduction} (total reduction: {_cardCostReductions[card]})");
        }

        /// <summary>
        /// Makes this card cost 0 AP for the rest of the battle.
        /// Uses int.MaxValue as a sentinel so <see cref="GetCardCostReduction"/> can signal "free".
        /// </summary>
        public void MakeCardFreeThisBattle(CardData card)
        {
            if (card == null) return;
            _cardCostReductions[card] = int.MaxValue;
            GameLogger.LogInfo<DeckManager>($"{_ownerName}: {card.CardName} is now free (0 AP) this battle");
        }

        /// <summary>
        /// Returns the AP cost reduction that has been applied to this specific card this battle.
        /// Returns 0 if none was applied. Returns <see cref="int.MaxValue"/> if the card was made free.
        /// </summary>
        public int GetCardCostReduction(CardData card)
        {
            if (card == null) return 0;
            return _cardCostReductions.TryGetValue(card, out int r) ? r : 0;
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
        public bool IsCardInHand(CardData card) => _hand.Contains(card);

        /// <summary>
        /// Gets all cards in hand of a specific type.
        /// </summary>
        public List<CardData> GetCardsOfType(CardType cardType) =>
            _hand.Where(c => c.CardType == cardType).ToList();

        /// <summary>
        /// Gets all cards in hand with a specific tag.
        /// </summary>
        public List<CardData> GetCardsWithTag(string tag) =>
            _hand.Where(c => c.HasTag(tag)).ToList();

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
        public void StartTurn(int cardsToDraw) => DrawCards(cardsToDraw);

        /// <summary>
        /// Called at the end of each turn - discards hand.
        /// </summary>
        public void EndTurn() => DiscardHand();

        /// <summary>
        /// Called at the end of battle - cleanup.
        /// </summary>
        public void EndBattle()
        {
            GameLogger.LogInfo<DeckManager>($"{_ownerName} battle ended. Exhausted: {_exhaust.Count} cards");
        }

        #endregion

        #region Card Generation

        /// <summary>
        /// Adds a card to the draw pile and shuffles.
        /// </summary>
        public void AddCardToDeck(CardData card)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot add null card to deck");
                return;
            }

            _deck.Add(card);
            Shuffle();
            GameLogger.LogInfo<DeckManager>($"{_ownerName} added {card.CardName} to deck");
        }

        /// <summary>
        /// Adds multiple copies of a card to the draw pile and shuffles.
        /// </summary>
        public void AddCardsToDeck(CardData card, int count)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot add null card to deck");
                return;
            }

            for (int i = 0; i < count; i++)
                _deck.Add(card);

            Shuffle();
            GameLogger.LogInfo<DeckManager>($"{_ownerName} added {count}x {card.CardName} to deck");
        }

        /// <summary>
        /// Adds a card directly to hand (if space available).
        /// </summary>
        public bool AddCardToHand(CardData card)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot add null card to hand");
                return false;
            }

            if (IsHandFull)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot add card to hand - hand is full");
                return false;
            }

            _hand.Add(card);
            GameLogger.LogInfo<DeckManager>($"{_ownerName} added {card.CardName} to hand");
            return true;
        }

        /// <summary>
        /// Adds multiple copies of a card directly to hand (up to hand limit).
        /// </summary>
        public int AddCardsToHand(CardData card, int count)
        {
            if (card == null)
            {
                GameLogger.LogWarning<DeckManager>($"{_ownerName} cannot add null card to hand");
                return 0;
            }

            int cardsAdded = 0;
            for (int i = 0; i < count && !IsHandFull; i++)
            {
                _hand.Add(card);
                cardsAdded++;
            }

            GameLogger.LogInfo<DeckManager>($"{_ownerName} added {cardsAdded}x {card.CardName} to hand");
            return cardsAdded;
        }

        #endregion

        #region Debugging

        /// <summary>
        /// Gets a summary of all card zones.
        /// </summary>
        public string GetStatusString() =>
            $"Deck: {DeckCount} | Hand: {HandCount}/{_maxHandSize} | Discard: {DiscardCount} | Exhaust: {ExhaustCount}";

        #endregion
    }
}
