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
        [Tooltip("Frame for Diplomacy cards (Green - peaceful persuasion)")]
        [SerializeField] private Sprite _diplomacyFrame;

        [Tooltip("Frame for Hostility cards (Red - aggressive tactics)")]
        [SerializeField] private Sprite _hostilityFrame;

        [Tooltip("Frame for Manipulate cards (Purple - utility/resources)")]
        [SerializeField] private Sprite _manipulateFrame;

        [Header("Card Frames by Rarity")]
        [Tooltip("Frame overlay for Common rarity cards")]
        [SerializeField] private Sprite _commonFrame;

        [Tooltip("Frame overlay for Uncommon rarity cards")]
        [SerializeField] private Sprite _uncommonFrame;

        [Tooltip("Frame overlay for Rare rarity cards")]
        [SerializeField] private Sprite _rareFrame;

        [Tooltip("Frame overlay for Legendary rarity cards")]
        [SerializeField] private Sprite _legendaryFrame;

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
                CardType.Diplomacy => _diplomacyFrame,
                CardType.Hostility => _hostilityFrame,
                CardType.Manipulate => _manipulateFrame,
                _ => null
            };
        }

        /// <summary>
        /// Gets the frame overlay sprite for a specific rarity.
        /// </summary>
        /// <param name="rarity">Rarity to get frame for</param>
        /// <returns>Rarity frame overlay sprite</returns>
        public Sprite GetFrameForRarity(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Basic => _commonFrame,
                CardRarity.Enhanced => _uncommonFrame,
                CardRarity.Rare => _rareFrame,
                CardRarity.Legendary => _legendaryFrame,
                _ => null
            };
        }

        #endregion
    }
}
