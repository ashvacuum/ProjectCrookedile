using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Data.Database;
using Crookedile.Utilities;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// ScriptableObject database containing all card data for the game.
    /// Provides searching, filtering, and querying capabilities for cards.
    /// Auto-populates from all CardData assets using the "Refresh Database" button in the inspector.
    /// </summary>
    /// <example>
    /// // Get all Attack cards
    /// List<CardData> attacks = cardDatabase.GetByType(CardType.Attack);
    ///
    /// // Complex search query
    /// CardSearchQuery query = new CardSearchQuery();
    /// query.CardTypes.Add(CardType.Charm);
    /// query.Rarities.Add(CardRarity.Rare);
    /// List<CardData> results = cardDatabase.Search(query);
    /// </example>
    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Crookedile/Database/Card Database")]
    public class CardDatabase : GameDatabase<CardData>
    {
        /// <summary>
        /// Gets the unique ID from a CardData item.
        /// Used internally by the database system.
        /// </summary>
        protected override string GetItemID(CardData item)
        {
            return item.ID;
        }

        #region Simple Queries

        /// <summary>
        /// Gets all cards of a specific type.
        /// </summary>
        /// <param name="cardType">Type to filter by (Attack, Skill, Power)</param>
        /// <returns>List of cards matching the type</returns>
        public List<CardData> GetByType(CardType cardType)
        {
            return FindAll(card => card.CardType == cardType);
        }

        /// <summary>
        /// Gets all cards of a specific tier.
        /// </summary>
        /// <param name="tier">Tier to filter by (Basic, Enhanced, Rare)</param>
        /// <returns>List of cards matching the tier</returns>
        public List<CardData> GetByTier(CardTier tier)
        {
            return FindAll(card => card.Tier == tier);
        }

        /// <summary>
        /// Gets all cards of a specific rarity.
        /// </summary>
        /// <param name="rarity">Rarity to filter by (Common, Uncommon, Rare, Legendary)</param>
        /// <returns>List of cards matching the rarity</returns>
        public List<CardData> GetByRarity(CardRarity rarity)
        {
            return FindAll(card => card.Rarity == rarity);
        }

        /// <summary>
        /// Gets all upgraded (+) cards.
        /// </summary>
        /// <returns>List of upgraded cards</returns>
        public List<CardData> GetUpgradedCards()
        {
            return FindAll(card => card.IsUpgraded);
        }

        /// <summary>
        /// Gets all cards flagged as starter cards.
        /// </summary>
        /// <returns>List of starter cards</returns>
        public List<CardData> GetStarterCards()
        {
            return FindAll(card => card.IsStarterCard);
        }

        /// <summary>
        /// Gets all cards that must be unlocked through progression.
        /// </summary>
        /// <returns>List of unlockable cards</returns>
        public List<CardData> GetUnlockableCards()
        {
            return FindAll(card => card.IsUnlockable);
        }

        /// <summary>
        /// Gets all cards with a specific tag.
        /// </summary>
        /// <param name="tag">Tag to search for (e.g., "violence", "corruption", "persuasion")</param>
        /// <returns>List of cards with this tag</returns>
        public List<CardData> GetByTag(string tag)
        {
            return FindAll(card => card.HasTag(tag));
        }

        /// <summary>
        /// Gets all cards matching a list of tags.
        /// </summary>
        /// <param name="tags">List of tags to search for</param>
        /// <param name="requireAll">If true, cards must have ALL tags. If false, cards need ANY tag.</param>
        /// <returns>List of cards matching the tag criteria</returns>
        public List<CardData> GetByTags(List<string> tags, bool requireAll = false)
        {
            if (requireAll)
            {
                return FindAll(card => tags.All(tag => card.HasTag(tag)));
            }
            else
            {
                return FindAll(card => tags.Any(tag => card.HasTag(tag)));
            }
        }

        /// <summary>
        /// Gets all cards that can be upgraded.
        /// </summary>
        /// <returns>List of cards with available upgrades</returns>
        public List<CardData> GetUpgradableCards()
        {
            return FindAll(card => card.CanUpgrade);
        }

        #endregion

        #region Advanced Search

        /// <summary>
        /// Performs a complex search using multiple filters.
        /// All filters are AND-ed together. Empty filters are ignored.
        /// </summary>
        /// <param name="query">Search query with multiple filter criteria</param>
        /// <returns>List of cards matching all specified criteria</returns>
        /// <example>
        /// CardSearchQuery query = new CardSearchQuery();
        /// query.CardTypes.Add(CardType.Attack);
        /// query.CardTypes.Add(CardType.Power);
        /// query.Rarities.Add(CardRarity.Legendary);
        /// query.Tags.Add("violence");
        /// List<CardData> results = database.Search(query);
        /// </example>
        public List<CardData> Search(CardSearchQuery query)
        {
            List<CardData> results = GetAll();

            if (query.CardTypes != null && query.CardTypes.Count > 0)
            {
                results = results.Where(c => query.CardTypes.Contains(c.CardType)).ToList();
            }

            if (query.Rarities != null && query.Rarities.Count > 0)
            {
                results = results.Where(c => query.Rarities.Contains(c.Rarity)).ToList();
            }

            if (query.Tiers != null && query.Tiers.Count > 0)
            {
                results = results.Where(c => query.Tiers.Contains(c.Tier)).ToList();
            }

            if (query.Tags != null && query.Tags.Count > 0)
            {
                if (query.RequireAllTags)
                {
                    results = results.Where(c => query.Tags.All(tag => c.HasTag(tag))).ToList();
                }
                else
                {
                    results = results.Where(c => query.Tags.Any(tag => c.HasTag(tag))).ToList();
                }
            }

            if (query.StarterCardsOnly)
            {
                results = results.Where(c => c.IsStarterCard).ToList();
            }

            if (query.UnlockableCardsOnly)
            {
                results = results.Where(c => c.IsUnlockable).ToList();
            }

            if (query.UpgradableOnly)
            {
                results = results.Where(c => c.CanUpgrade).ToList();
            }

            if (query.UpgradedOnly)
            {
                results = results.Where(c => c.IsUpgraded).ToList();
            }

            if (!string.IsNullOrEmpty(query.NameContains))
            {
                results = results.Where(c => c.CardName.ToLower().Contains(query.NameContains.ToLower())).ToList();
            }

            return results;
        }

        #endregion

        #region Random Selection

        /// <summary>
        /// Gets a random card weighted by rarity.
        /// Common: 60% chance, Uncommon: 30%, Rare: 9%, Legendary: 1%
        /// </summary>
        /// <returns>Randomly selected card based on rarity weights</returns>
        public CardData GetRandomByRarityWeight()
        {
            List<CardData> allCards = GetAll();
            List<float> weights = new List<float>();

            foreach (var card in allCards)
            {
                float weight = card.Rarity switch
                {
                    CardRarity.Common => 60f,
                    CardRarity.Uncommon => 30f,
                    CardRarity.Rare => 9f,
                    CardRarity.Legendary => 1f,
                    _ => 1f
                };
                weights.Add(weight);
            }

            return RandomHelper.WeightedRandom(allCards, weights);
        }

        #endregion

        #region Deck Building

        /// <summary>
        /// Gets the starter deck for a specific origin.
        /// Filters starter cards by tags matching the origin.
        /// </summary>
        /// <param name="origin">Origin type to build starter deck for</param>
        /// <returns>List of cards for the starter deck</returns>
        public List<CardData> GetStarterDeck(OriginType origin)
        {
            // Get all starter cards tagged with the origin name
            string originTag = origin.ToString().ToLower();
            return FindAll(card => card.IsStarterCard &&
                                 (card.Tags.Count == 0 || card.HasTag(originTag) || card.HasTag("universal")));
        }

        #endregion
    }

    /// <summary>
    /// Query object for complex card searches.
    /// All criteria are AND-ed together. Empty lists are ignored.
    /// </summary>
    /// <example>
    /// // Find all Legendary Attack or Power cards with the "violence" tag
    /// CardSearchQuery query = new CardSearchQuery();
    /// query.CardTypes.Add(CardType.Attack);
    /// query.CardTypes.Add(CardType.Power);
    /// query.Rarities.Add(CardRarity.Legendary);
    /// query.Tags.Add("violence");
    /// List<CardData> results = database.Search(query);
    /// </example>
    [System.Serializable]
    public class CardSearchQuery
    {
        [Tooltip("Filter by card types (Attack, Skill, Power). Cards matching ANY type will be included.")]
        public List<CardType> CardTypes;

        [Tooltip("Filter by rarity. Cards matching ANY rarity will be included.")]
        public List<CardRarity> Rarities;

        [Tooltip("Filter by tier (Basic, Enhanced, Rare). Cards matching ANY tier will be included.")]
        public List<CardTier> Tiers;

        [Tooltip("Filter by tags. Use RequireAllTags to control AND vs OR logic.")]
        public List<string> Tags;

        [Tooltip("If true, cards must have ALL tags. If false, cards need ANY tag.")]
        public bool RequireAllTags = false;

        [Tooltip("Only include starter cards")]
        public bool StarterCardsOnly = false;

        [Tooltip("Only include unlockable cards")]
        public bool UnlockableCardsOnly = false;

        [Tooltip("Only include cards that can be upgraded")]
        public bool UpgradableOnly = false;

        [Tooltip("Only include upgraded (+) cards")]
        public bool UpgradedOnly = false;

        [Tooltip("Filter by card name (case-insensitive partial match)")]
        public string NameContains;

        /// <summary>
        /// Creates a new empty search query.
        /// </summary>
        public CardSearchQuery()
        {
            CardTypes = new List<CardType>();
            Rarities = new List<CardRarity>();
            Tiers = new List<CardTier>();
            Tags = new List<string>();
        }
    }
}
