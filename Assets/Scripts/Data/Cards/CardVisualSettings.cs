using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// ScriptableObject containing global card visual settings.
    /// Defines card backs, frames, and other visual elements shared across all cards.
    /// </summary>
    [CreateAssetMenu(fileName = "CardVisualSettings", menuName = "Crookedile/Cards/Card Visual Settings")]
    public class CardVisualSettings : ScriptableObject
    {
        [Header("Card Backs")]
        [Tooltip("Default card back used for all cards")]
        [SerializeField] private Sprite _defaultCardBack;

        [Tooltip("Card back for Faith Leader origin cards")]
        [SerializeField] private Sprite _faithLeaderCardBack;

        [Tooltip("Card back for Nepo Baby origin cards")]
        [SerializeField] private Sprite _nepoBabyCardBack;

        [Tooltip("Card back for Actor origin cards")]
        [SerializeField] private Sprite _actorCardBack;

        [Header("Card Frames by Type")]
        [Tooltip("Frame for Attack cards")]
        [SerializeField] private Sprite _attackFrame;

        [Tooltip("Frame for Skill cards")]
        [SerializeField] private Sprite _skillFrame;

        [Tooltip("Frame for Power cards")]
        [SerializeField] private Sprite _powerFrame;

        [Header("Card Frames by Tier")]
        [Tooltip("Frame overlay for Basic tier cards")]
        [SerializeField] private Sprite _basicTierFrame;

        [Tooltip("Frame overlay for Enhanced tier cards")]
        [SerializeField] private Sprite _enhancedTierFrame;

        [Tooltip("Frame overlay for Rare tier cards")]
        [SerializeField] private Sprite _rareTierFrame;

        [Header("Rarity Indicators")]
        [Tooltip("Visual indicator for Common rarity")]
        [SerializeField] private Sprite _commonIndicator;

        [Tooltip("Visual indicator for Uncommon rarity")]
        [SerializeField] private Sprite _uncommonIndicator;

        [Tooltip("Visual indicator for Rare rarity")]
        [SerializeField] private Sprite _rareIndicator;

        [Tooltip("Visual indicator for Legendary rarity")]
        [SerializeField] private Sprite _legendaryIndicator;

        #region Properties

        /// <summary>
        /// Default card back used for all cards.
        /// </summary>
        public Sprite DefaultCardBack => _defaultCardBack;

        /// <summary>
        /// Card back for Faith Leader origin cards.
        /// </summary>
        public Sprite FaithLeaderCardBack => _faithLeaderCardBack;

        /// <summary>
        /// Card back for Nepo Baby origin cards.
        /// </summary>
        public Sprite NepoBabyCardBack => _nepoBabyCardBack;

        /// <summary>
        /// Card back for Actor origin cards.
        /// </summary>
        public Sprite ActorCardBack => _actorCardBack;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the appropriate card back for a specific origin.
        /// Falls back to default if origin-specific back is not set.
        /// </summary>
        /// <param name="origin">Origin type to get card back for</param>
        /// <returns>Card back sprite for the origin</returns>
        public Sprite GetCardBackForOrigin(OriginType origin)
        {
            Sprite originBack = origin switch
            {
                OriginType.Actor => _actorCardBack,
                OriginType.FaithLeader => _faithLeaderCardBack,
                OriginType.NepoBaby => _nepoBabyCardBack,
                _ => null
            };

            return originBack != null ? originBack : _defaultCardBack;
        }

        /// <summary>
        /// Gets the frame sprite for a specific card type.
        /// </summary>
        /// <param name="cardType">Card type to get frame for</param>
        /// <returns>Frame sprite for the card type</returns>
        public Sprite GetFrameForType(CardType cardType)
        {
            return cardType switch
            {
                CardType.Attack => _attackFrame,
                CardType.Skill => _skillFrame,
                CardType.Power => _powerFrame,
                _ => null
            };
        }

        /// <summary>
        /// Gets the frame overlay sprite for a specific card tier.
        /// </summary>
        /// <param name="tier">Card tier to get frame for</param>
        /// <returns>Tier frame overlay sprite</returns>
        public Sprite GetFrameForTier(CardTier tier)
        {
            return tier switch
            {
                CardTier.Basic => _basicTierFrame,
                CardTier.Enhanced => _enhancedTierFrame,
                CardTier.Rare => _rareTierFrame,
                _ => null
            };
        }

        /// <summary>
        /// Gets the rarity indicator sprite for a specific rarity.
        /// </summary>
        /// <param name="rarity">Rarity to get indicator for</param>
        /// <returns>Rarity indicator sprite</returns>
        public Sprite GetRarityIndicator(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => _commonIndicator,
                CardRarity.Uncommon => _uncommonIndicator,
                CardRarity.Rare => _rareIndicator,
                CardRarity.Legendary => _legendaryIndicator,
                _ => null
            };
        }

        #endregion
    }
}
