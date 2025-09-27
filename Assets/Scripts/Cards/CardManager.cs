using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("Deck Management")]
    public List<Card> masterDeck = new List<Card>();
    public List<Card> playerDeck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discardPile = new List<Card>();

    [Header("Hand Settings")]
    public int maxHandSize = 5;
    public int cardsPerTurn = 2;

    [Header("Starting Cards")]
    public List<Card> starterCards = new List<Card>();

    private void Start()
    {
        InitializeDeck();
        DrawInitialHand();
    }

    private void InitializeDeck()
    {
        playerDeck.Clear();
        playerDeck.AddRange(starterCards);

        Debug.Log($"Initialized deck with {playerDeck.Count} starter cards");
    }

    public void DrawCards()
    {
        for (int i = 0; i < cardsPerTurn && hand.Count < maxHandSize; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        if (hand.Count >= maxHandSize) return;

        if (playerDeck.Count == 0)
        {
            ShuffleDiscardIntoDeck();
        }

        if (playerDeck.Count > 0)
        {
            Card drawnCard = playerDeck[0];
            playerDeck.RemoveAt(0);
            hand.Add(drawnCard);

            Debug.Log($"Drew card: {drawnCard.cardName}");
        }
    }

    private void DrawInitialHand()
    {
        for (int i = 0; i < maxHandSize; i++)
        {
            DrawCard();
        }
    }

    public void PlayCard(Card card)
    {
        if (!hand.Contains(card)) return;
        if (!card.CanPlay()) return;

        hand.Remove(card);
        discardPile.Add(card);
        card.Play();

        Debug.Log($"Played {card.cardName}, hand size: {hand.Count}");
    }

    private void ShuffleDiscardIntoDeck()
    {
        playerDeck.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();

        Debug.Log("Shuffled discard pile into deck");
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < playerDeck.Count; i++)
        {
            Card temp = playerDeck[i];
            int randomIndex = Random.Range(i, playerDeck.Count);
            playerDeck[i] = playerDeck[randomIndex];
            playerDeck[randomIndex] = temp;
        }
    }

    public void AddCardToDeck(Card card)
    {
        playerDeck.Add(card);
        Debug.Log($"Added {card.cardName} to deck");
    }

    public void AddCardToHand(Card card)
    {
        if (hand.Count < maxHandSize)
        {
            hand.Add(card);
            Debug.Log($"Added {card.cardName} to hand");
        }
    }

    public List<Card> GetPlayableCards()
    {
        return hand.Where(card => card.CanPlay()).ToList();
    }

    public List<Card> GetCardsByType(CardType cardType)
    {
        return hand.Where(card => card.cardType == cardType).ToList();
    }

    public int GetDeckSize()
    {
        return playerDeck.Count + discardPile.Count;
    }

    public void EndTurn()
    {
        Debug.Log($"Turn ended. Hand: {hand.Count}, Deck: {playerDeck.Count}, Discard: {discardPile.Count}");
    }
}