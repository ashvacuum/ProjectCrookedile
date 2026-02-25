using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Simplified card effect system for battle effects only.
    /// Uses Odin Inspector for clean, contextual dropdowns.
    /// </summary>
    [Serializable]
    public class CardEffect
    {
        [Title("Name (optional)")]
        [Tooltip("Human-readable label for this effect. Shown in triggered-effect inspector entries.")]
        [SerializeField] private string _name = "";

        [Title("Effect Type")]
        [EnumToggleButtons]
        [SerializeField] private EffectCategory _category;

        [Title("Target")]
        [ShowIf("ShowTarget")]
        [EnumToggleButtons]
        [SerializeField] private TargetType _target = TargetType.Opponent;

        [Title("Damage")]
        [ShowIf("_category", EffectCategory.Damage)]
        [ValueDropdown("GetDamageTypes")]
        [SerializeField] private DamageType _damageType;

        [ShowIf("ShowFixedDamage")]
        [LabelText("Damage Amount")]
        [MinValue(1)]
        [SerializeField] private int _damageAmount = 3;

        [ShowIf("ShowRandomDamage")]
        [LabelText("Random Damage Range")]
        [MinMaxSlider(1, 15, true)]
        [SerializeField] private Vector2Int _randomDamageRange = new Vector2Int(3, 9);

        [Title("Resource")]
        [ShowIf("_category", EffectCategory.Resource)]
        [ValueDropdown("GetResourceTypes")]
        [SerializeField] private ResourceEffectType _resourceType;

        [ShowIf("ShowResourceAmount")]
        [LabelText("Amount")]
        [MinValue(1)]
        [SerializeField] private int _resourceAmount = 3;

        [Title("Card Manipulation")]
        [ShowIf("_category", EffectCategory.CardManipulation)]
        [ValueDropdown("GetCardManipulationTypes")]
        [SerializeField] private CardManipulationType _cardManipulationType;

        [ShowIf("ShowCardAmount")]
        [LabelText("Number of Cards")]
        [MinValue(1)]
        [SerializeField] private int _cardAmount = 2;

        [ShowIf("ShowCardToAdd")]
        [LabelText("Card to Add")]
        [SerializeField] private CardData _cardToAdd;

        [ShowIf("ShowCostReduction")]
        [LabelText("Cost Reduction")]
        [MinValue(1)]
        [SerializeField] private int _costReduction = 1;

        [Title("Status Effect")]
        [ShowIf("_category", EffectCategory.StatusEffect)]
        [ValueDropdown("GetStatusEffectTypes")]
        [SerializeField] private StatusEffectType _statusEffectType;

        [ShowIf("_category", EffectCategory.StatusEffect)]
        [LabelText("Stacks")]
        [MinValue(1)]
        [SerializeField] private int _statusStacks = 2;

        [ShowIf("_category", EffectCategory.StatusEffect)]
        [SerializeField] private StatusDurationType _statusDuration = StatusDurationType.DecreasePerTurn;

        [ShowIf("ShowAmountSource")]
        [Title("Amount Source")]
        [Tooltip("Where to read the numeric amount at runtime.\n" +
                 "FixedAmount = use the authored value above (default).\n" +
                 "None = always 0 (hides the fixed amount field).\n" +
                 "Other values = read from EffectContext at resolve time (e.g. LastDamageDealt for lifesteal).")]
        [SerializeField] private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        #region Odin Dropdowns

        private static ValueDropdownList<DamageType> GetDamageTypes()
        {
            return new ValueDropdownList<DamageType>
            {
                { "Fixed Damage", DamageType.FixedDamage },
                { "Random Damage (Actor)", DamageType.RandomDamage },
                { "Damage = Composure (Faith Leader)", DamageType.DamageEqualToComposure },
            };
        }

        private static ValueDropdownList<ResourceEffectType> GetResourceTypes()
        {
            return new ValueDropdownList<ResourceEffectType>
            {
                { "Composure/Gain Composure", ResourceEffectType.GainComposure },
                { "Composure/Lose Composure", ResourceEffectType.LoseComposure },
                { "Composure/Consume All Composure", ResourceEffectType.ConsumeAllComposure },
                { "Composure/Composure = Hostility (Actor)", ResourceEffectType.ComposureEqualToHostility },
                { "Hostility/Reduce Hostility", ResourceEffectType.ReduceHostility },
                { "Action Points/Gain AP (This Turn)", ResourceEffectType.GainActionPoints },
                { "Action Points/Gain AP (Next Turn)", ResourceEffectType.GainActionPointsNextTurn },
                { "Resolve/Heal Resolve", ResourceEffectType.HealResolve },
            };
        }

        private static ValueDropdownList<CardManipulationType> GetCardManipulationTypes()
        {
            return new ValueDropdownList<CardManipulationType>
            {
                { "Draw/Draw Cards", CardManipulationType.DrawCards },
                { "Draw/Choose from Discard to Hand", CardManipulationType.ChooseFromDiscardToHand },
                { "Draw/Choose from Discard to Deck", CardManipulationType.ChooseFromDiscardToDeck },

                { "Discard/Discard Cards", CardManipulationType.DiscardCards },
                { "Discard/Discard Hand", CardManipulationType.DiscardHand },
                { "Discard/Exhaust This Card", CardManipulationType.ExhaustThisCard },

                { "Create/Add Card to Deck", CardManipulationType.AddCardToDeck },
                { "Create/Add Card to Hand", CardManipulationType.AddCardToHand },

                { "Upgrade/Upgrade Card This Battle", CardManipulationType.UpgradeCardThisBattle },
                { "Upgrade/Upgrade All Cards in Hand", CardManipulationType.UpgradeAllCardsInHand },

                { "Retain/Make Card Permanent (Retain)", CardManipulationType.MakeCardRetain },
                { "Retain/Make All Cards Retain", CardManipulationType.MakeAllCardsRetain },

                { "Cost/Reduce Card Cost This Battle", CardManipulationType.ReduceCardCost },
                { "Cost/Make Card Cost 0 This Turn", CardManipulationType.MakeCardFree },
            };
        }

        private static ValueDropdownList<StatusEffectType> GetStatusEffectTypes()
        {
            return new ValueDropdownList<StatusEffectType>
            {
                { "Debuffs/Weakened (Deal X less damage)", StatusEffectType.Weakened },
                { "Debuffs/Vulnerable (Take 50% more damage)", StatusEffectType.Vulnerable },
                { "Debuffs/Frail (Gain 25% less Composure)", StatusEffectType.Frail },
                { "Debuffs/Entangled (Cards cost +1 AP)", StatusEffectType.Entangled },
                { "Debuffs/Exposed (Next attack double damage)", StatusEffectType.Exposed },
                { "Debuffs/Scandal (Take X damage per turn)", StatusEffectType.Scandal },
                { "Debuffs/Confused (Random card +1 AP)", StatusEffectType.Confused },
                { "Debuffs/Silenced (Can't play Manipulate)", StatusEffectType.Silenced },

                { "Buffs/Strength (Deal X more damage)", StatusEffectType.Strength },
                { "Buffs/Dexterity (Gain X more Composure)", StatusEffectType.Dexterity },
                { "Buffs/Focus (Cards cost X less AP)", StatusEffectType.Focus },
                { "Buffs/Energized (Draw X cards next turn)", StatusEffectType.Energized },
                { "Buffs/Plated (Reduce damage by X)", StatusEffectType.Plated },
                { "Buffs/Regeneration (Heal X per turn)", StatusEffectType.Regeneration },
                { "Buffs/Intangible (Take 1 damage only)", StatusEffectType.Intangible },
                { "Buffs/Thorns (Deal X back when hit)", StatusEffectType.Thorns },

                { "Special/Block (Temporary damage reduction)", StatusEffectType.Block },
                { "Special/Ritual (Gain X Composure per turn)", StatusEffectType.Ritual },
                { "Special/Momentum (X damage per card)", StatusEffectType.Momentum },
                { "Special/Echo (Next card plays twice)", StatusEffectType.Echo },
            };
        }

        #endregion

        #region Odin Conditionals

        private bool ShowTarget()
        {
            return _category == EffectCategory.Damage || _category == EffectCategory.StatusEffect;
        }

        private bool ShowFixedDamage()
        {
            return _category == EffectCategory.Damage
                   && _damageType == DamageType.FixedDamage
                   && _amountSource == EffectContextValue.FixedAmount;
        }

        private bool ShowRandomDamage()
        {
            return _category == EffectCategory.Damage && _damageType == DamageType.RandomDamage;
        }

        private bool ShowResourceAmount()
        {
            return _category == EffectCategory.Resource &&
                   (_resourceType == ResourceEffectType.GainComposure ||
                    _resourceType == ResourceEffectType.LoseComposure ||
                    _resourceType == ResourceEffectType.ReduceHostility ||
                    _resourceType == ResourceEffectType.GainActionPoints ||
                    _resourceType == ResourceEffectType.GainActionPointsNextTurn ||
                    _resourceType == ResourceEffectType.HealResolve) &&
                   _amountSource == EffectContextValue.FixedAmount;
        }

        /// <summary>
        /// AmountSource is only meaningful for effect types that have a single authored numeric amount.
        /// Hides for RandomDamage (min/max ranges), DamageEqualToComposure, ConsumeAllComposure,
        /// ComposureEqualToHostility, and all CardManipulation / StatusEffect types.
        /// </summary>
        private bool ShowAmountSource()
        {
            if (_category == EffectCategory.Damage)
                return _damageType == DamageType.FixedDamage;
            if (_category == EffectCategory.Resource)
            {
                return _resourceType == ResourceEffectType.GainComposure   ||
                       _resourceType == ResourceEffectType.LoseComposure   ||
                       _resourceType == ResourceEffectType.ReduceHostility ||
                       _resourceType == ResourceEffectType.GainActionPoints ||
                       _resourceType == ResourceEffectType.GainActionPointsNextTurn ||
                       _resourceType == ResourceEffectType.HealResolve;
            }
            return false;
        }

        private bool ShowCardAmount()
        {
            return _category == EffectCategory.CardManipulation &&
                   (_cardManipulationType == CardManipulationType.DrawCards ||
                    _cardManipulationType == CardManipulationType.DiscardCards ||
                    _cardManipulationType == CardManipulationType.ChooseFromDiscardToHand ||
                    _cardManipulationType == CardManipulationType.ChooseFromDiscardToDeck ||
                    _cardManipulationType == CardManipulationType.AddCardToDeck ||
                    _cardManipulationType == CardManipulationType.AddCardToHand);
        }

        private bool ShowCardToAdd()
        {
            return _category == EffectCategory.CardManipulation &&
                   (_cardManipulationType == CardManipulationType.AddCardToDeck ||
                    _cardManipulationType == CardManipulationType.AddCardToHand);
        }

        private bool ShowCostReduction()
        {
            return _category == EffectCategory.CardManipulation &&
                   _cardManipulationType == CardManipulationType.ReduceCardCost;
        }

        #endregion

        #region Properties

        public string             EffectName   => _name;
        public EffectCategory     Category     => _category;
        public TargetType Target => _target;
        public DamageType DamageType => _damageType;
        public int DamageAmount => _damageAmount;
        public int RandomDamageMin => _randomDamageRange.x;
        public int RandomDamageMax => _randomDamageRange.y;
        public ResourceEffectType ResourceType => _resourceType;
        public int ResourceAmount => _resourceAmount;
        public CardManipulationType CardManipulationType => _cardManipulationType;
        public int CardAmount => _cardAmount;
        public CardData CardToAdd => _cardToAdd;
        public int CostReduction => _costReduction;
        public StatusEffectType   StatusEffectType => _statusEffectType;
        public int                StatusStacks     => _statusStacks;
        public StatusDurationType StatusDuration   => _statusDuration;
        public EffectContextValue AmountSource     => _amountSource;

        #endregion

        #region Amount Resolution

        /// <summary>
        /// Returns the numeric amount to use for this effect at runtime.
        /// If <see cref="AmountSource"/> is <see cref="EffectContextValue.FixedAmount"/> (the default),
        /// returns the authored inspector value. Otherwise reads the live value from
        /// <paramref name="ctx"/>, enabling triggered effects like lifesteal.
        /// </summary>
        /// <param name="ctx">The EffectContext accumulated during the current card resolution.
        /// Pass null to always use the authored value (safe for base effects).</param>
        public int GetEffectiveAmount(EffectContext ctx)
        {
            if (_amountSource == EffectContextValue.FixedAmount)
                return GetBaseAmount();
            if (_amountSource == EffectContextValue.None || ctx == null)
                return 0;
            return ctx.GetValue(_amountSource);
        }

        /// <summary>
        /// Returns the authored (inspector-set) numeric amount for this effect,
        /// selecting the appropriate field based on effect category.
        /// </summary>
        private int GetBaseAmount()
        {
            return _category switch
            {
                EffectCategory.Damage           => _damageAmount,
                EffectCategory.Resource         => _resourceAmount,
                EffectCategory.StatusEffect     => _statusStacks,
                EffectCategory.CardManipulation => _cardAmount,
                _                               => 0
            };
        }

        #endregion

        #region Description

        [Title("Preview")]
        [ShowInInspector, DisplayAsString, HideLabel]
        private string Preview => GetDescription();

        /// <summary>
        /// Gets a human-readable description of this effect.
        /// </summary>
        public string GetDescription()
        {
            switch (_category)
            {
                case EffectCategory.Damage:
                    return GetDamageDescription();

                case EffectCategory.Resource:
                    return GetResourceDescription();

                case EffectCategory.CardManipulation:
                    return GetCardManipulationDescription();

                case EffectCategory.StatusEffect:
                    return GetStatusEffectDescription();

                default:
                    return "Unknown effect";
            }
        }

        private string GetDamageDescription()
        {
            string targetStr = _target != TargetType.Self ? $" to {_target}" : "";

            return _damageType switch
            {
                DamageType.FixedDamage => $"Deal {_damageAmount} Resolve damage{targetStr}",
                DamageType.RandomDamage => $"Deal {_randomDamageRange.x}-{_randomDamageRange.y} random damage{targetStr}",
                DamageType.DamageEqualToComposure => $"Deal damage = Composure{targetStr}",
                _ => "Unknown damage"
            };
        }

        private string GetResourceDescription()
        {
            return _resourceType switch
            {
                ResourceEffectType.GainComposure => $"Gain {_resourceAmount} Composure",
                ResourceEffectType.LoseComposure => $"Lose {_resourceAmount} Composure",
                ResourceEffectType.ConsumeAllComposure => "Consume all Composure",
                ResourceEffectType.ComposureEqualToHostility => "Gain Composure = Hostility",
                ResourceEffectType.ReduceHostility => $"Reduce {_resourceAmount} Hostility",
                ResourceEffectType.GainActionPoints => $"Gain {_resourceAmount} AP",
                ResourceEffectType.GainActionPointsNextTurn => $"Gain {_resourceAmount} AP next turn",
                ResourceEffectType.HealResolve => $"Heal {_resourceAmount} Resolve",
                _ => "Unknown resource"
            };
        }

        private string GetCardManipulationDescription()
        {
            string cardName = _cardToAdd != null ? _cardToAdd.CardName : "[No Card]";

            return _cardManipulationType switch
            {
                CardManipulationType.DrawCards => $"Draw {_cardAmount} card(s)",
                CardManipulationType.ChooseFromDiscardToHand => $"Choose {_cardAmount} card(s) from discard pile to hand",
                CardManipulationType.ChooseFromDiscardToDeck => $"Choose {_cardAmount} card(s) from discard pile to deck",

                CardManipulationType.DiscardCards => $"Discard {_cardAmount} card(s)",
                CardManipulationType.DiscardHand => "Discard your entire hand",
                CardManipulationType.ExhaustThisCard => "Exhaust this card",

                CardManipulationType.AddCardToDeck => $"Add {_cardAmount} {cardName} to deck",
                CardManipulationType.AddCardToHand => $"Add {_cardAmount} {cardName} to hand",

                CardManipulationType.UpgradeCardThisBattle => "Upgrade a card this battle",
                CardManipulationType.UpgradeAllCardsInHand => "Upgrade all cards in hand",

                CardManipulationType.MakeCardRetain => "Make a card retain (permanent)",
                CardManipulationType.MakeAllCardsRetain => "Make all cards retain",

                CardManipulationType.ReduceCardCost => $"Reduce a card's cost by {_costReduction} this battle",
                CardManipulationType.MakeCardFree => "Make a card cost 0 this turn",

                _ => "Unknown card manipulation"
            };
        }

        private string GetStatusEffectDescription()
        {
            string targetStr = _target != TargetType.Self ? $" to {_target}" : "";
            string durationStr = _statusDuration == StatusDurationType.RemoveEndOfTurn ? " (this turn)" :
                                _statusDuration == StatusDurationType.Permanent ? " (permanent)" : "";

            return $"Apply {_statusStacks} {_statusEffectType}{durationStr}{targetStr}";
        }

        #endregion
    }

    #region Enums

    public enum EffectCategory
    {
        [LabelText("💥 Damage")]
        Damage,

        [LabelText("⚡ Resource")]
        Resource,

        [LabelText("🎴 Card Manipulation")]
        CardManipulation,

        [LabelText("✨ Status Effect")]
        StatusEffect
    }

    public enum DamageType
    {
        FixedDamage,
        RandomDamage,
        DamageEqualToComposure
    }

    public enum ResourceEffectType
    {
        GainComposure             = 0,
        LoseComposure             = 1,
        ConsumeAllComposure       = 2,
        ComposureEqualToHostility = 3,
        // 4 intentionally skipped — GainHostility was here; removed to preserve .asset serialization
        ReduceHostility           = 5,
        GainActionPoints          = 6,
        GainActionPointsNextTurn  = 7,
        HealResolve               = 8,
    }

    public enum CardManipulationType
    {
        // Draw effects
        DrawCards,
        ChooseFromDiscardToHand,
        ChooseFromDiscardToDeck,

        // Discard effects
        DiscardCards,
        DiscardHand,
        ExhaustThisCard,

        // Card creation effects
        AddCardToDeck,
        AddCardToHand,

        // Upgrade effects
        UpgradeCardThisBattle,
        UpgradeAllCardsInHand,

        // Retain effects (cards don't discard at end of turn)
        MakeCardRetain,
        MakeAllCardsRetain,

        // Cost modification effects
        ReduceCardCost,
        MakeCardFree
    }

    #endregion
}
