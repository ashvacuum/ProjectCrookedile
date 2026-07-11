using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using Sirenix.OdinInspector;
using UnityEngine;

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
        [SerializeField]
        private string _id;

        [Tooltip("Display name of the card shown to players")]
        [SerializeField]
        private string _cardName;

        [Tooltip("Card type determines general behavior (Pressure, Rhetoric, Policy)")]
        [SerializeField]
        private CardType _cardType;

        [Tooltip("Rarity affects card acquisition chance and power level")]
        [SerializeField]
        private CardRarity _rarity;

        [Header("Visuals")]
        [Tooltip("Main artwork displayed on the front of the card")]
        [SerializeField]
        [PreviewField]
        private Sprite _artwork;

        [Button("Assign Unused Character Art")]
        [Tooltip("Picks a random Character_ sprite not already used by any other card.")]
        private void AssignUnusedCharacterArt()
        {
#if UNITY_EDITOR
            const string charDir = "Assets/Art/CCG Fantasy Game UI/Sprites/Characters";

            // Sprites already in use by every OTHER card.
            var used = new HashSet<string>();
            foreach (var cardGuid in UnityEditor.AssetDatabase.FindAssets("t:CardData"))
            {
                var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(cardGuid)
                );
                if (card == null || card == this || card._artwork == null)
                    continue;
                var sp = UnityEditor.AssetDatabase.GetAssetPath(card._artwork);
                if (!string.IsNullOrEmpty(sp))
                    used.Add(UnityEditor.AssetDatabase.AssetPathToGUID(sp));
            }

            // Character_ sprites not used by any other card.
            var candidates = new List<Sprite>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { charDir }))
            {
                if (used.Contains(guid))
                    continue;
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!System.IO.Path.GetFileName(path).StartsWith("Character_"))
                    continue;
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    candidates.Add(sprite);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[{name}] No unused Character_ sprites left in {charDir}.");
                return;
            }

            _artwork = candidates[Random.Range(0, candidates.Count)];
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[{name}] Assigned unused art: {_artwork.name}");
#endif
        }

        [FoldoutGroup("Description Override")]
        [ShowInInspector]
        [ReadOnly]
        [MultiLineProperty(4)]
        [PropertyTooltip("Live preview of the auto-generated description (from the effect list). Fill the override below to replace it with something shorter.")]
        private string AutoDescriptionPreview =>
            _effects == null || _effects.Count == 0 ? "(no effects)" : BuildAutoDescription();

        [FoldoutGroup("Description Override")]
        [InfoBox(
            "Leave blank — description is auto-generated from card effects at runtime "
                + "(preview above). Only fill this when the auto-generated text is too long or unclear.",
            InfoMessageType.Info
        )]
        [Tooltip("Optional explicit override. Leave blank to auto-generate from card effects.")]
        [TextArea(2, 5)]
        [SerializeField]
        private string _description;

        [Tooltip("Optional flavor text for storytelling and theme (e.g., 'A direct threat.')")]
        [TextArea(2, 3)]
        [SerializeField]
        private string _flavorText;

        [Header("Costs")]
        [Tooltip("List of resources required to play this card (₱, Lagay, Energy, etc.)")]
        [SerializeField]
        private List<CardCost> _costs = new List<CardCost>();

        [Header("Effects")]
        [Tooltip("Polymorphic effect list. Add effects via the + button.")]
        [SerializeReference]
        [SerializeField]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        [Header("Card Passives")]
        [Tooltip(
            "Battle-scoped passives that fire on broad battle events (turn start, damage dealt, etc.) "
                + "for the entire battle regardless of card location in the deck.\n\n"
                + "Each entry has its own polymorphic trigger, optional conditions, and a list of "
                + "BattleEffects to execute. Add entries via the + button and use the type picker "
                + "to choose trigger and condition classes."
        )]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        #region Upgrade
        [FoldoutGroup("Upgrade")]
        [Tooltip("Is this card currently in its upgraded state?")]
        [SerializeField]
        private bool _isUpgraded = false;

        [FoldoutGroup("Upgrade")]
        [Tooltip("Overridden costs when this card is upgraded. If empty, base costs are used.")]
        [SerializeField]
        private List<CardCost> _upgradedCosts = new List<CardCost>();

        [FoldoutGroup("Upgrade")]
        [Tooltip("Overridden effects when this card is upgraded. If empty, base effects are used.")]
        [SerializeReference]
        [SerializeField]
        private List<BattleEffect> _upgradedEffects = new List<BattleEffect>();

        [FoldoutGroup("Upgrade")]
        [Tooltip(
            "Overridden battle-scoped passives when this card is upgraded. If empty, base passives are used."
        )]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _upgradedPassives = new List<BattlePassive>();

        #endregion

        #region Metadata
        [Header("Metadata")]
        [Tooltip("Tags for searching/filtering (e.g., 'violence', 'corruption', 'persuasion')")]
        [SerializeField]
        private List<string> _tags = new List<string>();

        [Tooltip("Is this card included in starter decks?")]
        [SerializeField]
        private bool _isStarterCard = false;

        [Tooltip("Must this card be unlocked through progression?")]
        [SerializeField]
        private bool _isUnlockable = false;

        [ShowIf("_cardType", CardType.Heckle)]
        [Tooltip(
            "If true, this Status card is shown in the hand but cannot be played. "
                + "All Scandals are always unplayable regardless of this flag."
        )]
        [SerializeField]
        private bool _isUnplayable = false;

        [Tooltip(
            "If true, this card is never discarded at end of turn — it stays in hand until "
                + "played. A card property (like Unplayable), not an effect."
        )]
        [SerializeField]
        private bool _innateRetain = false;

        [Header("VFX")]
        [Tooltip(
            "VFX played when this card is used. Leave null for no card VFX.\n"
                + "Add an 'ApplyEffects' AnimationEvent at the hit frame to resolve damage in sync with the animation."
        )]
        [SerializeField]
        private VFXEvent _cardVFX;

        #endregion

        #region Configuration Tracking
        [FoldoutGroup("Configuration")]
        [InfoBox(
            "Outstanding setup steps — see notes below. Clear this field when done.",
            InfoMessageType.Warning,
            "NeedsConfiguration"
        )]
        [Tooltip(
            "Designer notes for effects/passives still needing manual Inspector setup. "
                + "Populated by the card generator. Clear this field when all steps are complete."
        )]
        [TextArea(2, 6)]
        [SerializeField]
        private string _configurationNotes;

        #region Properties

        /// <summary>Unique identifier for this card. Auto-generated GUID.</summary>
        public string ID => _id;

        /// <summary>Display name of the card shown to players.</summary>
        public string CardName => _cardName;

        /// <summary>Type of card (Pressure, Rhetoric, Policy).</summary>
        public CardType CardType => _cardType;

        /// <summary>Rarity level (Common, Uncommon, Rare, Legendary).</summary>
        public CardRarity Rarity => _rarity;

        /// <summary>Main artwork displayed on the front of the card.</summary>
        public Sprite Artwork => _artwork;

        /// <summary>
        /// Mechanical description of card effects.
        /// Returns the manual override if set, otherwise auto-generates from the card's effects.
        /// </summary>
        public string Description =>
            !string.IsNullOrEmpty(_description) ? _description : BuildAutoDescription();

        /// <summary>Flavor text for storytelling and theme.</summary>
        public string FlavorText => _flavorText;

        /// <summary>List of costs required to play this card.</summary>
        public List<CardCost> Costs => _costs;

        /// <summary>Polymorphic effect list for this card.</summary>
        public List<BattleEffect> Effects => _effects;

        /// <summary>Is this card currently in its upgraded state?</summary>
        public bool IsUpgraded => _isUpgraded;

        /// <summary>Overridden costs used when this card is upgraded. Empty means base costs apply.</summary>
        public List<CardCost> UpgradedCosts => _upgradedCosts;

        /// <summary>Overridden effects used when this card is upgraded. Empty means base effects apply.</summary>
        public List<BattleEffect> UpgradedEffects => _upgradedEffects;

        /// <summary>Overridden passives used when this card is upgraded. Empty means base passives apply.</summary>
        public IReadOnlyList<BattlePassive> UpgradedPassives => _upgradedPassives;

        /// <summary>
        /// Can this card be upgraded? True if it is not already upgraded and has at least one
        /// upgraded field defined (costs, effects, or passives).
        /// </summary>
        public bool CanUpgrade =>
            _cardType != CardType.Scandal
            && _cardType != CardType.Heckle
            && !_isUpgraded
            && (
                _upgradedCosts.Count > 0
                || _upgradedEffects.Count > 0
                || _upgradedPassives.Count > 0
            );

        /// <summary>Tags for searching and filtering.</summary>
        public List<string> Tags => _tags;

        /// <summary>Whether this card appears in starter decks.</summary>
        public bool IsStarterCard => _isStarterCard;

        /// <summary>Whether this card must be unlocked.</summary>
        public bool IsUnlockable => _isUnlockable;

        /// <summary>
        /// True if this card always stays in hand at end of turn (never discarded until played).
        /// Checked by <c>DeckManager.DiscardHand</c> alongside per-turn granted retains.
        /// </summary>
        public bool InnateRetain => _innateRetain;

        /// <summary>
        /// True if this card can never be played: all Scandals, and Status cards flagged as unplayable.
        /// The hand displays these cards at half alpha; dragging is blocked.
        /// </summary>
        public bool IsUnplayable =>
            _cardType == CardType.Scandal || (_cardType == CardType.Heckle && _isUnplayable);

        /// <summary>
        /// Battle-scoped passives that fire on broad battle events.
        /// DEFAULT passives (every non-Policy card): active from battle start while the card is
        /// anywhere in the deck — the card never needs to be played.
        /// ACTIVATED passives (Policy cards, <see cref="IsActivatedPassive"/>): switch on only
        /// when the card is played, then the card exhausts.
        /// </summary>
        public IReadOnlyList<BattlePassive> Passives => _passives;

        /// <summary>
        /// True for Policy cards: their <see cref="Passives"/> activate on play (not at battle
        /// start) and the card is exhausted afterwards. Every other card type carries DEFAULT
        /// passives — ambient from battle start. See PassiveResolver / CardPlayController.
        /// </summary>
        public bool IsActivatedPassive => _cardType == CardType.Policy;

        /// <summary>
        /// VFX played when this card is used. Null means effects resolve immediately (no regression).
        /// When set, card effects are deferred to the 'ApplyEffects' Animation Event in the clip,
        /// or to <see cref="Crookedile.UI.VFXAnimatedImage.OnAnimationComplete"/> as a safety net.
        /// </summary>
        public VFXEvent CardVFX => _cardVFX;

        /// <summary>
        /// True when this card asset still has manual Inspector setup steps outstanding.
        /// Populated by the card generator; designer clears the notes when complete.
        /// </summary>
        public bool NeedsConfiguration => !string.IsNullOrEmpty(_configurationNotes);

        /// <summary>
        /// Human-readable description of what still needs to be configured in the Unity Inspector.
        /// </summary>
        public string ConfigurationNotes => _configurationNotes;

        /// <summary>
        /// True if this card has no artwork assigned and is therefore not yet ready for gameplay.
        /// Cards in development are excluded from reward pools and card-choice panels.
        /// </summary>
        public bool IsInDevelopment => _artwork == null;

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
            string currentPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string directory = System.IO.Path.GetDirectoryName(currentPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(currentPath);
            string newPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                $"{directory}/{fileName} Copy.asset"
            );

            CardData duplicate = Instantiate(this);
            duplicate._id = System.Guid.NewGuid().ToString();
            duplicate._cardName = $"{_cardName} Copy";
            duplicate._isUpgraded = false;
            duplicate._upgradedCosts = new List<CardCost>();
            duplicate._upgradedEffects = new List<BattleEffect>();
            duplicate._upgradedPassives = new List<BattlePassive>();

            UnityEditor.AssetDatabase.CreateAsset(duplicate, newPath);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"Duplicated card to: {newPath}");
            UnityEditor.Selection.activeObject = duplicate;
        }
#endif

        /// <summary>
        /// Checks if this card has a specific tag.
        /// </summary>
        public bool HasTag(string tag) => _tags.Contains(tag);

        /// <summary>
        /// Gets the display name with upgrade indicator.
        /// </summary>
        /// <param name="upgradedSuffix">Suffix appended when the card is upgraded (default "+").</param>
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
        /// Gets the polymorphic effects to use, respecting upgrade state.
        /// Returns <see cref="_upgradedEffects"/> when upgraded and the list is non-empty;
        /// falls back to base <see cref="_effects"/> otherwise.
        /// </summary>
        public List<BattleEffect> GetNewEffects(bool useUpgraded = true)
        {
            if (useUpgraded && _isUpgraded && _upgradedEffects.Count > 0)
                return _upgradedEffects;
            return _effects;
        }

        /// <summary>
        /// Gets the battle-scoped passives to use, respecting upgrade state.
        /// Returns <see cref="_upgradedPassives"/> when upgraded and the list is non-empty;
        /// falls back to base <see cref="_passives"/> otherwise.
        /// </summary>
        public IReadOnlyList<BattlePassive> GetPassives(bool useUpgraded = true)
        {
            if (useUpgraded && _isUpgraded && _upgradedPassives.Count > 0)
                return _upgradedPassives;
            return _passives;
        }

        /// <summary>
        /// Gets the description for this card.
        /// Returns the manual override if set, otherwise auto-generates from the card's effects.
        /// </summary>
        public string GetDescription(bool useUpgraded = true) =>
            !string.IsNullOrEmpty(_description) ? _description : BuildAutoDescription();

        /// <summary>
        /// Builds a description string by concatenating <see cref="BattleEffect.GetDescription"/>
        /// from all effects. Uses the new BattleEffect system first, falls back to legacy
        /// effect descriptions. Returns an empty string if no effects are present.
        /// </summary>
        private string BuildAutoDescription()
        {
            if (_effects == null || _effects.Count == 0)
                return string.Empty;

            var parts = new System.Collections.Generic.List<string>(_effects.Count);
            foreach (var e in _effects)
            {
                if (e == null)
                    continue;
                string d = e.GetDescription();
                if (!string.IsNullOrEmpty(d))
                    parts.Add(d);
            }
            return parts.Count > 0 ? string.Join(". ", parts) : string.Empty;
        }

        /// <summary>Gets the artwork for this card.</summary>
        public Sprite GetArtwork(bool useUpgraded = true) => _artwork;

        /// <summary>Gets the card name (without any upgrade suffix).</summary>
        public string GetCardName(bool useUpgraded = true) => _cardName;

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        private void Reset()
        {
            _id = System.Guid.NewGuid().ToString();
        }
#endif
        #endregion
    }
}
