using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Data;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;

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

        [Tooltip("Card type determines general behavior (Pressure, Rhetoric, Policy)")]
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
        [Tooltip("New polymorphic effect list — use this for all newly authored cards. " +
                 "Add effects via the + button; each subclass shows only its own fields.")]
        [SerializeReference]
        [SerializeField] private List<BattleEffect> _newEffects = new List<BattleEffect>();

        [Tooltip("Legacy effect list — kept for backwards compatibility during migration. " +
                 "Run Crookedile / Tools / Migrate Effects to convert. Do not author new effects here.")]
        [FoldoutGroup("Legacy Effects (Migration)")]
        [SerializeField] private List<CardEffect> _effects = new List<CardEffect>();

        [Header("Upgrade")]
        [Tooltip("Is this card currently in its upgraded state?")]
        [SerializeField] private bool _isUpgraded = false;

        [Tooltip("Overridden costs when this card is upgraded. If empty, base costs are used.")]
        [SerializeField] private List<CardCost> _upgradedCosts = new List<CardCost>();

        [Tooltip("Overridden effects when this card is upgraded. If empty, base effects are used.")]
        [SerializeReference]
        [SerializeField] private List<BattleEffect> _upgradedEffects = new List<BattleEffect>();

        [Header("Metadata")]
        [Tooltip("Tags for searching/filtering (e.g., 'violence', 'corruption', 'persuasion')")]
        [SerializeField] private List<string> _tags = new List<string>();

        [Tooltip("Is this card included in starter decks?")]
        [SerializeField] private bool _isStarterCard = false;

        [Tooltip("Must this card be unlocked through progression?")]
        [SerializeField] private bool _isUnlockable = false;

        [ShowIf("_cardType", CardType.Status)]
        [Tooltip("If true, this Status card is shown in the hand but cannot be played. " +
                 "All Curses are always unplayable regardless of this flag.")]
        [SerializeField] private bool _isUnplayable = false;

        [Header("Policy")]
        [ShowIf("_cardType", CardType.Policy)]
        [Tooltip("The political lean of this Policy card.\n" +
                 "Left: Progressives −1 hostility, Traditionals +1 hostility\n" +
                 "Center: Moderates −1 hostility\n" +
                 "Right: Traditionals −1 hostility, Progressives +1 hostility\n" +
                 "None: No demographic hostility shift when played")]
        [SerializeField] private PolicyLean _policyLean = PolicyLean.None;

        [Header("Card Passives")]
        [Tooltip("Battle-scoped passives that fire on broad battle events (turn start, damage dealt, etc.) " +
                 "for the entire battle regardless of card location in the deck.\n\n" +
                 "Each entry has its own polymorphic trigger, optional conditions, and a list of " +
                 "BattleEffects to execute. Add entries via the + button and use the type picker " +
                 "to choose trigger and condition classes.")]
        [SerializeReference]
        [SerializeField] private List<BattlePassive> _passives = new List<BattlePassive>();

        [Header("Triggered Effects")]
        [Tooltip("Named effects that fire automatically after this card's base effects resolve, " +
                 "conditioned on runtime events (damage dealt, kills, status applied, etc.).\n\n" +
                 "Example — Lifesteal: Trigger=OnDamageDealt, Condition=Always, " +
                 "Response=HealResolve with AmountSource=LastDamageDealt.")]
        [SerializeField] private List<TriggeredEffect> _triggeredEffects = new List<TriggeredEffect>();

        [Header("VFX")]
        [Tooltip("VFX played when this card is used. Leave null for no card VFX.\n" +
                 "Add an 'ApplyEffects' AnimationEvent at the hit frame to resolve damage in sync with the animation.")]
        [SerializeField] private VFXEvent _cardVFX;

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
        /// Type of card (Pressure, Rhetoric, Policy).
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
        /// Polymorphic effect list for this card.
        /// Returns <c>_newEffects</c> when populated (new system); falls back to the legacy
        /// <c>_effects</c> list during the migration window. After migration is complete and
        /// <c>_effects</c> is cleared, this will always return <c>_newEffects</c>.
        /// </summary>
        public List<BattleEffect> NewEffects => _newEffects;

        /// <summary>Legacy effect list — read by the migration tool and the old EffectResolver path.</summary>
        public List<CardEffect> Effects => _effects;

        /// <summary>
        /// Is this card currently in its upgraded state?
        /// </summary>
        public bool IsUpgraded => _isUpgraded;

        /// <summary>
        /// Overridden costs used when this card is upgraded. Empty means base costs apply.
        /// </summary>
        public List<CardCost> UpgradedCosts => _upgradedCosts;

        /// <summary>
        /// Overridden effects used when this card is upgraded. Empty means base effects apply.
        /// </summary>
        public List<BattleEffect> UpgradedEffects => _upgradedEffects;

        /// <summary>
        /// Can this card be upgraded? True if it is not already upgraded and has at least one
        /// upgraded cost or upgraded effect defined.
        /// </summary>
        public bool CanUpgrade => !_isUpgraded && (_upgradedCosts.Count > 0 || _upgradedEffects.Count > 0);

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

        /// <summary>
        /// True if this card can never be played: all Curses, and Status cards flagged as unplayable.
        /// The hand displays these cards at half alpha; dragging is blocked.
        /// </summary>
        public bool IsUnplayable => _cardType == CardType.Curse ||
                                    (_cardType == CardType.Status && _isUnplayable);

        /// <summary>
        /// Political lean of this card. Only relevant for CardType.Policy.
        /// Determines which demographics become more or less hostile when played.
        /// </summary>
        public PolicyLean PolicyLean => _policyLean;

        /// <summary>
        /// Battle-scoped passives that fire on broad battle events for the entire battle.
        /// Registered by PassiveResolver at the start of each battle.
        /// </summary>
        public IReadOnlyList<BattlePassive> Passives => _passives;

        /// <summary>
        /// Named reactive effects that fire after this card's base effects resolve,
        /// conditioned on runtime events such as damage dealt, kills, or status applied.
        /// </summary>
        public IReadOnlyList<TriggeredEffect> TriggeredEffects => _triggeredEffects;

        /// <summary>
        /// VFX played when this card is used. Null means effects resolve immediately (no regression).
        /// When set, card effects are deferred to the 'ApplyEffects' Animation Event in the clip,
        /// or to <see cref="Crookedile.UI.VFXAnimatedImage.OnAnimationComplete"/> as a safety net.
        /// </summary>
        public VFXEvent CardVFX => _cardVFX;

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
            duplicate._cardName = $"{_cardName} Copy";
            duplicate._isUpgraded = false;
            duplicate._upgradedCosts = new List<CardCost>();
            duplicate._upgradedEffects = new List<BattleEffect>();

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
        /// <param name="upgradedSuffix">Suffix appended when the card is upgraded (default "+").</param>
        /// <returns>Card name with suffix if upgraded</returns>
        public string GetDisplayName(string upgradedSuffix = "+")
        {
            return _isUpgraded ? $"{_cardName}{upgradedSuffix}" : _cardName;
        }

        /// <summary>
        /// Creates a runtime instance of this card in its upgraded state.
        /// The returned object is a new <see cref="ScriptableObject"/> instance; caller is
        /// responsible for managing its lifetime (e.g. destroying it when the battle ends).
        /// </summary>
        public CardData CreateUpgradedInstance()
        {
            var copy = Instantiate(this);
            copy._isUpgraded = true;
            return copy;
        }

        /// <summary>
        /// Gets the costs to use, respecting upgrade state.
        /// Returns <see cref="_upgradedCosts"/> when upgraded and the list is non-empty;
        /// falls back to base <see cref="_costs"/> otherwise.
        /// </summary>
        public List<CardCost> GetCosts(bool useUpgraded = true)
        {
            if (useUpgraded && _isUpgraded && _upgradedCosts.Count > 0)
                return _upgradedCosts;
            return _costs;
        }

        /// <summary>
        /// Gets the new polymorphic effects to use, respecting upgrade state.
        /// Returns <see cref="_upgradedEffects"/> when upgraded and the list is non-empty;
        /// falls back to base <see cref="_newEffects"/> otherwise.
        /// </summary>
        public List<BattleEffect> GetNewEffects(bool useUpgraded = true)
        {
            if (useUpgraded && _isUpgraded && _upgradedEffects.Count > 0)
                return _upgradedEffects;
            return _newEffects;
        }

        /// <summary>
        /// Gets the legacy effects for this card.
        /// </summary>
        public List<CardEffect> GetEffects(bool useUpgraded = true)
        {
            return _effects;
        }

        /// <summary>
        /// Gets the description for this card.
        /// </summary>
        public string GetDescription(bool useUpgraded = true)
        {
            return _description;
        }

        /// <summary>
        /// Gets the artwork for this card.
        /// </summary>
        public Sprite GetArtwork(bool useUpgraded = true)
        {
            return _artwork;
        }

        /// <summary>
        /// Gets the card name (without any upgrade suffix).
        /// </summary>
        public string GetCardName(bool useUpgraded = true)
        {
            return _cardName;
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
