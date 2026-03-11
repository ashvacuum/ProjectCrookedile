using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Data.Database;
using Crookedile.Utilities;
using Crookedile.Data;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// ScriptableObject database containing all card data for the game.
    /// Provides searching, filtering, and querying capabilities for cards.
    /// Auto-populates from all CardData assets using the "Refresh Database" button in the inspector.
    /// </summary>
    /// <example>
    /// // Get all Pressure cards
    /// List<CardData> pressure = cardDatabase.GetByType(CardType.Pressure);
    ///
    /// // Complex search query
    /// CardSearchQuery query = new CardSearchQuery();
    /// query.CardTypes.Add(CardType.Rhetoric);
    /// query.Rarities.Add(CardRarity.Enhanced);
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
        /// <param name="cardType">Type to filter by (Pressure, Rhetoric, Policy)</param>
        /// <returns>List of cards matching the type</returns>
        public List<CardData> GetByType(CardType cardType)
        {
            return FindAll(card => card.CardType == cardType);
        }

        /// <summary>
        /// Gets all cards of a specific rarity.
        /// </summary>
        /// <param name="rarity">Rarity to filter by (Basic, Enhanced, Rare)</param>
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
        /// query.CardTypes.Add(CardType.Pressure);
        /// query.CardTypes.Add(CardType.Rhetoric);
        /// query.Rarities.Add(CardRarity.Rare);
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
        /// Basic: 70% chance, Enhanced: 25%, Rare: 5%
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
                    CardRarity.Basic => 70f,
                    CardRarity.Enhanced => 25f,
                    CardRarity.Rare => 5f,
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

        #region Reward Offers

        /// <summary>
        /// Generates a randomised reward card offer for the post-battle card pick screen.
        ///
        /// Rules:
        ///   - Policy cards are universal — no origin filter applied.
        ///   - Pressure / Rhetoric cards must have the origin's tag or "universal".
        ///   - Starter cards (<see cref="CardData.IsStarterCard"/>) are always excluded.
        ///   - Upgraded cards are excluded so players receive base versions only.
        ///   - <paramref name="typeFilter"/> restricts all picks to one <see cref="CardType"/>;
        ///     pass <c>null</c> to draw from the full eligible pool.
        ///   - The same card is never returned twice in one offer.
        ///   - Weighted rarity draw: Basic 70 % / Enhanced 25 % / Rare 5 %.
        ///     If the chosen rarity bucket is empty the method falls back to any non-empty bucket.
        /// </summary>
        /// <param name="origin">Player's current run origin (used to filter Pressure/Rhetoric cards).</param>
        /// <param name="count">Number of cards to offer (default 3).</param>
        /// <param name="typeFilter">Restrict to a single <see cref="CardType"/>; <c>null</c> = any type.</param>
        /// <returns>
        /// List of unique <see cref="CardData"/> offers. May be shorter than <paramref name="count"/>
        /// if the eligible pool is exhausted.
        /// </returns>
        public List<CardData> GenerateRewardOffer(OriginType origin, int count = 3, CardType? typeFilter = null)
        {
            string originTag = origin.ToString().ToLower();

            // Build the candidate pool ─────────────────────────────────────────────────
            List<CardData> candidates = new List<CardData>();

            bool includePolicy   = typeFilter == null || typeFilter == CardType.Policy;
            bool includePressure = typeFilter == null || typeFilter == CardType.Pressure;
            bool includeRhetoric = typeFilter == null || typeFilter == CardType.Rhetoric;

            foreach (CardData card in GetAll())
            {
                if (card == null)          continue;
                if (card.IsStarterCard)    continue;
                if (card.IsUpgraded)       continue;   // offer base version only
                if (card.IsInDevelopment)  continue;   // no artwork — not ready for play

                switch (card.CardType)
                {
                    case CardType.Policy:
                        if (includePolicy)
                            candidates.Add(card);
                        break;

                    case CardType.Pressure:
                        if (includePressure && (card.HasTag(originTag) || card.HasTag("universal")))
                            candidates.Add(card);
                        break;

                    case CardType.Rhetoric:
                        if (includeRhetoric && (card.HasTag(originTag) || card.HasTag("universal")))
                            candidates.Add(card);
                        break;
                }
            }

            if (candidates.Count == 0) return new List<CardData>();

            // Split into rarity buckets ────────────────────────────────────────────────
            var basicBucket    = candidates.Where(c => c.Rarity == CardRarity.Basic).ToList();
            var enhancedBucket = candidates.Where(c => c.Rarity == CardRarity.Enhanced).ToList();
            var rareBucket     = candidates.Where(c => c.Rarity == CardRarity.Rare).ToList();

            // Rarity weights: Basic 70 / Enhanced 25 / Rare 5
            const float weightBasic    = 70f;
            const float weightEnhanced = 25f;
            const float weightRare     =  5f;

            var result = new List<CardData>(count);

            for (int i = 0; i < count; i++)
            {
                if (basicBucket.Count == 0 && enhancedBucket.Count == 0 && rareBucket.Count == 0)
                    break;   // pool exhausted

                // Weighted pick among non-empty buckets
                List<CardData> bucket = PickWeightedBucket(
                    basicBucket,    weightBasic,
                    enhancedBucket, weightEnhanced,
                    rareBucket,     weightRare);

                if (bucket == null || bucket.Count == 0) break;

                int idx  = UnityEngine.Random.Range(0, bucket.Count);
                var pick = bucket[idx];
                bucket.RemoveAt(idx);   // prevent duplicates

                result.Add(pick);
            }

            return result;
        }

        /// <summary>
        /// Returns one of the three rarity buckets using weighted random selection.
        /// Buckets with zero remaining cards are excluded from selection.
        /// Returns <c>null</c> if all buckets are empty.
        /// </summary>
        private static List<CardData> PickWeightedBucket(
            List<CardData> basic,    float wBasic,
            List<CardData> enhanced, float wEnhanced,
            List<CardData> rare,     float wRare)
        {
            float total = 0f;
            if (basic.Count    > 0) total += wBasic;
            if (enhanced.Count > 0) total += wEnhanced;
            if (rare.Count     > 0) total += wRare;
            if (total <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, total);
            float cursor = 0f;

            if (basic.Count > 0)
            {
                cursor += wBasic;
                if (roll < cursor) return basic;
            }
            if (enhanced.Count > 0)
            {
                cursor += wEnhanced;
                if (roll < cursor) return enhanced;
            }
            return rare.Count > 0 ? rare : null;
        }

        #endregion
    }

    /// <summary>
    /// Query object for complex card searches.
    /// All criteria are AND-ed together. Empty lists are ignored.
    /// </summary>
    /// <example>
    /// // Find all Rare Pressure or Rhetoric cards with the "violence" tag
    /// CardSearchQuery query = new CardSearchQuery();
    /// query.CardTypes.Add(CardType.Pressure);
    /// query.CardTypes.Add(CardType.Rhetoric);
    /// query.Rarities.Add(CardRarity.Rare);
    /// query.Tags.Add("violence");
    /// List<CardData> results = database.Search(query);
    /// </example>
    [System.Serializable]
    public class CardSearchQuery
    {
        [Tooltip("Filter by card types (Pressure, Rhetoric, Policy). Cards matching ANY type will be included.")]
        public List<CardType> CardTypes;

        [Tooltip("Filter by rarity. Cards matching ANY rarity will be included.")]
        public List<CardRarity> Rarities;

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
            Tags = new List<string>();
        }
    }
}
