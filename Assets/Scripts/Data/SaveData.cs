using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Save data for the game. Keeps it simple - just card IDs, upgrade status, and resources.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        [Header("Player Deck")]
        [Tooltip("List of cards in the player's deck")]
        public List<CardInstance> deck = new List<CardInstance>();

        [Header("Campaign Resources")]
        [Tooltip("Current campaign funds (₱)")]
        public int funds = 0;

        [Tooltip("Current heat level (0-100)")]
        public int heat = 0;

        [Tooltip("Current influence")]
        public int influence = 0;

        [Header("Campaign Progress")]
        [Tooltip("Current day (1-45)")]
        public int currentDay = 1;

        [Tooltip("Player's chosen origin")]
        public OriginType origin = OriginType.FaithLeader;

        [Header("Unlocks")]
        [Tooltip("Unlocked card IDs (available for acquisition)")]
        public List<string> unlockedCardIDs = new List<string>();

        [Tooltip("Unlocked location IDs")]
        public List<string> unlockedLocationIDs = new List<string>();

        [Header("Metadata")]
        [Tooltip("Last save timestamp in sortable format (yyyy-MM-dd HH:mm:ss)")]
        public string lastSaveTime;

        [Tooltip("Total playtime in seconds")]
        public float totalPlaytime = 0f;

        private const string DATE_FORMAT = "o"; // ISO 8601 format (UTC)

        // Dirty flag tracking (not serialized)
        [NonSerialized] private bool _isDirty = false;

        /// <summary>
        /// Marks this save data as modified (needs saving).
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Clears the dirty flag after successful save.
        /// </summary>
        public void ClearDirty()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Checks if this save data has been modified since last save.
        /// </summary>
        public bool IsDirty()
        {
            return _isDirty;
        }

        /// <summary>
        /// Creates a new save with default starting values.
        /// </summary>
        public SaveData()
        {
            deck = new List<CardInstance>();
            funds = 100;
            heat = 0;
            influence = 0;
            currentDay = 1;
            origin = OriginType.FaithLeader;
            unlockedCardIDs = new List<string>();
            unlockedLocationIDs = new List<string>();
            lastSaveTime = DateTime.UtcNow.ToString(DATE_FORMAT);
            totalPlaytime = 0f;
        }

        /// <summary>
        /// Creates a new save with specific origin and starting resources.
        /// </summary>
        public SaveData(OriginType selectedOrigin, int startingFunds = 100, int startingInfluence = 0)
        {
            deck = new List<CardInstance>();
            funds = startingFunds;
            heat = 0;
            influence = startingInfluence;
            currentDay = 1;
            origin = selectedOrigin;
            unlockedCardIDs = new List<string>();
            unlockedLocationIDs = new List<string>();
            lastSaveTime = DateTime.UtcNow.ToString(DATE_FORMAT);
            totalPlaytime = 0f;
        }

        /// <summary>
        /// Adds a card to the deck.
        /// </summary>
        public void AddCard(string cardID, bool isUpgraded = false)
        {
            deck.Add(new CardInstance(cardID, isUpgraded));
            MarkDirty();
        }

        /// <summary>
        /// Removes a card from the deck by ID.
        /// </summary>
        public bool RemoveCard(string cardID)
        {
            var card = deck.Find(c => c.cardID == cardID);
            if (card != null)
            {
                deck.Remove(card);
                MarkDirty();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Upgrades a card in the deck.
        /// </summary>
        public bool UpgradeCard(string cardID)
        {
            var card = deck.Find(c => c.cardID == cardID && !c.isUpgraded);
            if (card != null)
            {
                card.isUpgraded = true;
                MarkDirty();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a card is in the deck.
        /// </summary>
        public bool HasCard(string cardID)
        {
            return deck.Exists(c => c.cardID == cardID);
        }

        /// <summary>
        /// Gets the count of a specific card in the deck.
        /// </summary>
        public int GetCardCount(string cardID)
        {
            return deck.FindAll(c => c.cardID == cardID).Count;
        }

        /// <summary>
        /// Updates the last save timestamp.
        /// </summary>
        public void UpdateSaveTime()
        {
            lastSaveTime = DateTime.UtcNow.ToString(DATE_FORMAT);
        }

        /// <summary>
        /// Gets the last save time as a DateTime object (UTC).
        /// </summary>
        public DateTime? GetSaveDateTime()
        {
            if (DateTime.TryParse(lastSaveTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime result))
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Sets the origin from a string name.
        /// </summary>
        public bool SetOriginFromString(string originName)
        {
            if (Enum.TryParse<OriginType>(originName, ignoreCase: true, out OriginType result))
            {
                origin = result;
                return true;
            }
            Debug.LogWarning($"Failed to parse origin: {originName}");
            return false;
        }

        /// <summary>
        /// Gets the origin as a string.
        /// </summary>
        public string GetOriginString()
        {
            return origin.ToString();
        }
    }

    /// <summary>
    /// Represents a single card instance in the player's deck.
    /// Just stores the card ID and whether it's been upgraded.
    /// </summary>
    [Serializable]
    public class CardInstance
    {
        [Tooltip("Card ID reference (links to CardData asset)")]
        public string cardID;

        [Tooltip("Has this card been upgraded to (+) version?")]
        public bool isUpgraded;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public CardInstance() { }

        /// <summary>
        /// Creates a new card instance.
        /// </summary>
        public CardInstance(string id, bool upgraded = false)
        {
            cardID = id;
            isUpgraded = upgraded;
        }

        /// <summary>
        /// Gets a debug-friendly string representation.
        /// </summary>
        public override string ToString()
        {
            return isUpgraded ? $"{cardID}+" : cardID;
        }
    }
}
