using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// ScriptableObject containing all data for a single card in the CCG.
    /// Cards can have costs, effects, origin bonuses, and can be upgraded.
    /// IDs are auto-generated as GUIDs and should never be manually edited.
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Crookedile/Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        [HorizontalGroup("ID")]
        [ReadOnly]
        [HideLabel]
        [Tooltip("Unique identifier for this card. Auto-generated GUID.")]
        [SerializeField] private string _id;

        [Tooltip("Display name of the card shown to players")]
        [SerializeField] private string _cardName;

        [Tooltip("Card type determines general behavior (Attack, Defense, Charm, Leverage, Power)")]
        [SerializeField] private CardType _cardType;

        [Tooltip("Rarity affects card acquisition chance and power level")]
        [SerializeField] private CardRarity _rarity;

        [Header("Visuals")]
        [Tooltip("Main artwork displayed on the front of the card")]
        [SerializeField] private Sprite _artwork;

        [Tooltip("Mechanical description of what the card does (e.g., 'Deal 10 damage. +1 Heat')")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;

        [Tooltip("Optional flavor text for storytelling and theme (e.g., 'A direct threat.')")]
        [TextArea(2, 3)]
        [SerializeField] private string _flavorText;

        [Header("Costs")]
        [Tooltip("List of resources required to play this card (₱, Lagay, Energy, etc.)")]
        [SerializeField] private List<CardCost> _costs = new List<CardCost>();

        [Header("Effects")]
        [Tooltip("List of effects that trigger when this card is played")]
        [SerializeField] private List<CardEffect> _effects = new List<CardEffect>();

        [Header("Card Tier")]
        [Tooltip("Card tier (Basic, Enhanced, Rare) - separate pools, each upgrades independently")]
        [SerializeField] private CardTier _tier = CardTier.Basic;

        [Tooltip("Is this the upgraded (+) version?")]
        [SerializeField] private bool _isUpgraded = false;

        [Header("Upgrade")]
        [Tooltip("Reference to the upgraded (+) version of this card (if it exists)")]
        [SerializeField] private CardData _upgradedVersion;

        [Tooltip("Upgrade cost in Funds (₱)")]
        [SerializeField] private int _upgradeCost = 50;

        [Header("Metadata")]
        [Tooltip("Tags for searching/filtering (e.g., 'violence', 'corruption', 'persuasion')")]
        [SerializeField] private List<string> _tags = new List<string>();

        [Tooltip("Is this card included in starter decks?")]
        [SerializeField] private bool _isStarterCard = false;

        [Tooltip("Must this card be unlocked through progression?")]
        [SerializeField] private bool _isUnlockable = false;

        #region Properties

        /// <summary>
        /// Unique identifier for this card. Auto-generated GUID.
        /// </summary>
        public string ID => _id;

        /// <summary>
        /// Display name of the card shown to players.
        /// </summary>
        public string CardName => _cardName;

        /// <summary>
        /// Type of card (Attack, Defense, Charm, Leverage, Power).
        /// </summary>
        public CardType CardType => _cardType;

        /// <summary>
        /// Rarity level (Common, Uncommon, Rare, Legendary).
        /// </summary>
        public CardRarity Rarity => _rarity;

        /// <summary>
        /// Main artwork displayed on the front of the card.
        /// </summary>
        public Sprite Artwork => _artwork;

        /// <summary>
        /// Mechanical description of card effects.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Flavor text for storytelling and theme.
        /// </summary>
        public string FlavorText => _flavorText;

        /// <summary>
        /// List of costs required to play this card.
        /// </summary>
        public List<CardCost> Costs => _costs;

        /// <summary>
        /// List of effects that trigger when played.
        /// </summary>
        public List<CardEffect> Effects => _effects;

        /// <summary>
        /// Card tier (Basic, Enhanced, Rare).
        /// </summary>
        public CardTier Tier => _tier;

        /// <summary>
        /// Is this the upgraded (+) version?
        /// </summary>
        public bool IsUpgraded => _isUpgraded;

        /// <summary>
        /// Reference to the upgraded (+) version of this card.
        /// Null if no upgraded version exists.
        /// </summary>
        public CardData UpgradedVersion => _upgradedVersion;

        /// <summary>
        /// Can this card be upgraded? (Has an upgraded version and is not already upgraded)
        /// </summary>
        public bool CanUpgrade => !_isUpgraded && _upgradedVersion != null;

        /// <summary>
        /// Cost in Funds (₱) to upgrade this card.
        /// </summary>
        public int UpgradeCost => _upgradeCost;

        /// <summary>
        /// Tags for searching and filtering.
        /// </summary>
        public List<string> Tags => _tags;

        /// <summary>
        /// Whether this card appears in starter decks.
        /// </summary>
        public bool IsStarterCard => _isStarterCard;

        /// <summary>
        /// Whether this card must be unlocked.
        /// </summary>
        public bool IsUnlockable => _isUnlockable;

        #endregion

        #region Public Methods

        /// <summary>
        /// Copies the card ID to the clipboard.
        /// </summary>
        [Button("Copy ID", ButtonSizes.Small)]
        [HorizontalGroup("ID", Width = 80)]
        private void CopyIDToClipboard()
        {
            GUIUtility.systemCopyBuffer = _id;
            Debug.Log($"Copied card ID to clipboard: {_id}");
        }

#if UNITY_EDITOR
        /// <summary>
        /// Duplicates this card as a new card.
        /// </summary>
        [Button("Duplicate Card", ButtonSizes.Medium)]
        [PropertySpace(SpaceBefore = 10)]
        private void DuplicateCard()
        {
            // Get the path of the current asset
            string currentPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string directory = System.IO.Path.GetDirectoryName(currentPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
            string newPath = $"{directory}/{fileName} Copy.asset";

            // Make sure the path is unique
            newPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(newPath);

            // Create a copy
            CardData duplicate = Instantiate(this);
            duplicate._id = System.Guid.NewGuid().ToString();
            duplicate._upgradedVersion = null; // New card doesn't reference upgrades
            duplicate._cardName = $"{_cardName} Copy";
            duplicate._isUpgraded = false;

            // Create the asset
            UnityEditor.AssetDatabase.CreateAsset(duplicate, newPath);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"Duplicated card to: {newPath}");
            UnityEditor.Selection.activeObject = duplicate;
        }
#endif

        /// <summary>
        /// Checks if this card has a specific tag.
        /// </summary>
        /// <param name="tag">Tag to search for</param>
        /// <returns>True if the card has this tag</returns>
        public bool HasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>
        /// Gets the display name with upgrade indicator.
        /// </summary>
        /// <returns>Card name with + suffix if upgraded</returns>
        public string GetDisplayName()
        {
            return _isUpgraded ? $"{_cardName}+" : _cardName;
        }

        /// <summary>
        /// Gets tier display text (e.g., "Basic", "Enhanced", "Rare").
        /// </summary>
        public string GetTierDisplayText()
        {
            return _tier.ToString();
        }

        /// <summary>
        /// Gets the current card to use (returns upgraded version if this is base and has upgrade).
        /// Use this for runtime card operations to automatically use upgraded stats.
        /// </summary>
        /// <returns>Upgraded version if available and not already upgraded, otherwise this card</returns>
        public CardData GetCurrentVersion()
        {
            // If we have an upgraded version referenced and we're not already upgraded, use the upgrade
            return (!_isUpgraded && _upgradedVersion != null) ? _upgradedVersion : this;
        }

        /// <summary>
        /// Gets the card name for the current version (base or upgraded).
        /// </summary>
        public string GetCardName(bool useUpgraded = true)
        {
            return useUpgraded ? GetCurrentVersion().CardName : _cardName;
        }

        /// <summary>
        /// Gets the costs for the current version (base or upgraded).
        /// </summary>
        public List<CardCost> GetCosts(bool useUpgraded = true)
        {
            return useUpgraded ? GetCurrentVersion().Costs : _costs;
        }

        /// <summary>
        /// Gets the effects for the current version (base or upgraded).
        /// </summary>
        public List<CardEffect> GetEffects(bool useUpgraded = true)
        {
            return useUpgraded ? GetCurrentVersion().Effects : _effects;
        }

        /// <summary>
        /// Gets the description for the current version (base or upgraded).
        /// </summary>
        public string GetDescription(bool useUpgraded = true)
        {
            return useUpgraded ? GetCurrentVersion().Description : _description;
        }

        /// <summary>
        /// Gets the artwork for the current version (base or upgraded).
        /// </summary>
        public Sprite GetArtwork(bool useUpgraded = true)
        {
            return useUpgraded ? GetCurrentVersion().Artwork : _artwork;
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate unique ID if empty
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void Reset()
        {
            // Generate new ID when asset is created
            _id = System.Guid.NewGuid().ToString();
        }
#endif
    }
}
