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
                { "Hostility/Gain Hostility", ResourceEffectType.GainHostility },
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
                { "Draw Cards", CardManipulationType.DrawCards },
                { "Discard Cards", CardManipulationType.DiscardCards },
                { "Exhaust This Card", CardManipulationType.ExhaustThisCard },
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
            return _category == EffectCategory.Damage && _damageType == DamageType.FixedDamage;
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
                    _resourceType == ResourceEffectType.GainHostility ||
                    _resourceType == ResourceEffectType.ReduceHostility ||
                    _resourceType == ResourceEffectType.GainActionPoints ||
                    _resourceType == ResourceEffectType.GainActionPointsNextTurn ||
                    _resourceType == ResourceEffectType.HealResolve);
        }

        private bool ShowCardAmount()
        {
            return _category == EffectCategory.CardManipulation &&
                   (_cardManipulationType == CardManipulationType.DrawCards ||
                    _cardManipulationType == CardManipulationType.DiscardCards);
        }

        #endregion

        #region Properties

        public EffectCategory Category => _category;
        public TargetType Target => _target;
        public DamageType DamageType => _damageType;
        public int DamageAmount => _damageAmount;
        public int RandomDamageMin => _randomDamageRange.x;
        public int RandomDamageMax => _randomDamageRange.y;
        public ResourceEffectType ResourceType => _resourceType;
        public int ResourceAmount => _resourceAmount;
        public CardManipulationType CardManipulationType => _cardManipulationType;
        public int CardAmount => _cardAmount;
        public StatusEffectType StatusEffectType => _statusEffectType;
        public int StatusStacks => _statusStacks;
        public StatusDurationType StatusDuration => _statusDuration;

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
                ResourceEffectType.GainHostility => $"Gain {_resourceAmount} Hostility",
                ResourceEffectType.ReduceHostility => $"Reduce {_resourceAmount} Hostility",
                ResourceEffectType.GainActionPoints => $"Gain {_resourceAmount} AP",
                ResourceEffectType.GainActionPointsNextTurn => $"Gain {_resourceAmount} AP next turn",
                ResourceEffectType.HealResolve => $"Heal {_resourceAmount} Resolve",
                _ => "Unknown resource"
            };
        }

        private string GetCardManipulationDescription()
        {
            return _cardManipulationType switch
            {
                CardManipulationType.DrawCards => $"Draw {_cardAmount} cards",
                CardManipulationType.DiscardCards => $"Discard {_cardAmount} cards",
                CardManipulationType.ExhaustThisCard => "Exhaust",
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
        GainComposure,
        LoseComposure,
        ConsumeAllComposure,
        ComposureEqualToHostility,
        GainHostility,
        ReduceHostility,
        GainActionPoints,
        GainActionPointsNextTurn,
        HealResolve
    }

    public enum CardManipulationType
    {
        DrawCards,
        DiscardCards,
        ExhaustThisCard
    }

    #endregion
}
