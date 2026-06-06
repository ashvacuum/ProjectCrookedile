using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// ScriptableObject containing global card visual settings.
    /// Defines card backs, frames, and other visual elements shared across all cards.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CardVisualSettings",
        menuName = "Crookedile/Cards/Card Visual Settings"
    )]
    public class CardVisualSettings : ScriptableObject
    {
        [Header("Card Backs")]
        [Tooltip("Default card back used for all cards")]
        [SerializeField]
        private Sprite _defaultCardBack;

        [Tooltip("Card back for Faith Leader origin cards")]
        [SerializeField]
        private Sprite _faithLeaderCardBack;

        [Tooltip("Card back for Nepo Baby origin cards")]
        [SerializeField]
        private Sprite _nepoBabyCardBack;

        [Tooltip("Card back for Actor origin cards")]
        [SerializeField]
        private Sprite _actorCardBack;

        [Header("Card Frames by Type")]
        [Tooltip("Frame for Pressure cards (Green - persuasion, de-escalation)")]
        [SerializeField]
        private Sprite _pressureFrame;

        [Tooltip("Frame for Rhetoric cards (Red - aggressive framing, pressure tactics)")]
        [SerializeField]
        private Sprite _rhetoricFrame;

        [Tooltip("Frame for Policy cards (Blue - policy positions with lean)")]
        [SerializeField]
        private Sprite _policyFrame;

        [Tooltip("Frame for Status cards (Purple - temporary effect cards)")]
        [SerializeField]
        private Sprite _statusFrame;

        [Tooltip("Frame for Scandal cards (Dark crimson - manufactured controversy, unplayable, clogs the hand)")]
        [SerializeField]
        private Sprite _curseFrame;

        [Header("Card Frames by Rarity")]
        [Tooltip("Frame overlay for Basic rarity cards")]
        [SerializeField]
        private Sprite _basicFrame;

        [Tooltip("Frame overlay for Enhanced rarity cards")]
        [SerializeField]
        private Sprite _enhancedFrame;

        [Tooltip("Frame overlay for Rare rarity cards")]
        [SerializeField]
        private Sprite _rareFrame;

        [Header("Upgrade Visuals")]
        [Tooltip("Suffix appended to a card's name when it is in its upgraded state (e.g. \"+\")")]
        [SerializeField]
        private string _upgradedNameSuffix = "+";

        [Tooltip(
            "Optional tint color applied to the card name text when the card is upgraded. "
                + "Set alpha to 0 to leave the name color unchanged."
        )]
        [SerializeField]
        private Color _upgradedNameColor = Color.white;

        [Header("Cost Display")]
        [Tooltip(
            "Color applied to the cost text when the card's AP cost has been temporarily "
                + "reduced or made free by a battle effect (e.g. MakeCardFree, MakeAllCardsFreeNextPlay). "
                + "Set alpha to 0 to disable the tint."
        )]
        [SerializeField]
        private Color _discountedCostColor = new Color(0.3f, 1f, 0.3f, 1f);

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

        /// <summary>
        /// Suffix appended to a card's display name when it is in its upgraded state.
        /// </summary>
        public string UpgradedNameSuffix => _upgradedNameSuffix;

        /// <summary>
        /// Tint color applied to the card name text when the card is upgraded.
        /// </summary>
        public Color UpgradedNameColor => _upgradedNameColor;

        /// <summary>
        /// Color applied to the cost text when the card's cost has been temporarily reduced
        /// or made free by a battle effect. Alpha 0 disables the tint.
        /// </summary>
        public Color DiscountedCostColor => _discountedCostColor;

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
                _ => null,
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
                CardType.Pressure => _pressureFrame,
                CardType.Rhetoric => _rhetoricFrame,
                CardType.Policy => _policyFrame,
                CardType.Status => _statusFrame,
                CardType.Scandal => _curseFrame,
                _ => null,
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
                CardRarity.Basic => _basicFrame,
                CardRarity.Enhanced => _enhancedFrame,
                CardRarity.Rare => _rareFrame,
                _ => null,
            };
        }

        #endregion
    }
}
