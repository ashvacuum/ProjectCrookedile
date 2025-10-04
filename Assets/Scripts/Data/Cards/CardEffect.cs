using System;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Represents a single effect that occurs when a card is played.
    /// Effects can be battle-specific (damage Ego/Confidence, draw cards) or
    /// campaign-specific (gain resources, modify Heat, unlock content).
    /// </summary>
    [Serializable]
    public class CardEffect
    {
        [Tooltip("Type of battle effect (ConfidenceDamage, EgoDamage, DrawCards, etc.)")]
        [SerializeField] private BattleEffectType _battleEffectType;

        [Tooltip("Type of campaign effect (GainCampaignFunds, ModifyHeat, AddCardToDeck, etc.)")]
        [SerializeField] private CampaignEffectType _campaignEffectType;

        [Tooltip("Is this a battle effect or campaign effect?")]
        [SerializeField] private EffectContext _effectContext;

        [Tooltip("Who is affected by this effect (Self, Opponent, All, Random)")]
        [SerializeField] private TargetType _targetType;

        [Tooltip("Numerical value of the effect (e.g., 10 damage, 3 cards drawn, 200₱ gained)")]
        [SerializeField] private int _amount;

        [Tooltip("Which campaign resource is affected (only for campaign effects)")]
        [SerializeField] private CampaignResourceType _resourceType;

        /// <summary>
        /// Battle effect type (ConfidenceDamage, EgoDamage, etc.).
        /// </summary>
        public BattleEffectType BattleEffectType => _battleEffectType;

        /// <summary>
        /// Campaign effect type (GainCampaignFunds, ModifyHeat, etc.).
        /// </summary>
        public CampaignEffectType CampaignEffectType => _campaignEffectType;

        /// <summary>
        /// Is this a battle or campaign effect?
        /// </summary>
        public EffectContext EffectContext => _effectContext;

        /// <summary>
        /// Who this effect targets (Self, Opponent, All, Random).
        /// </summary>
        public TargetType TargetType => _targetType;

        /// <summary>
        /// Numerical value of the effect.
        /// </summary>
        public int Amount => _amount;

        /// <summary>
        /// Which campaign resource is affected (for campaign effects).
        /// </summary>
        public CampaignResourceType ResourceType => _resourceType;

        /// <summary>
        /// Default constructor for serialization.
        /// </summary>
        public CardEffect() { }

        /// <summary>
        /// Creates a new battle effect.
        /// </summary>
        public CardEffect(BattleEffectType battleEffectType, TargetType targetType, int amount)
        {
            _effectContext = EffectContext.Battle;
            _battleEffectType = battleEffectType;
            _targetType = targetType;
            _amount = amount;
        }

        /// <summary>
        /// Creates a new campaign effect.
        /// </summary>
        public CardEffect(CampaignEffectType campaignEffectType, int amount, CampaignResourceType resourceType = CampaignResourceType.Funds)
        {
            _effectContext = EffectContext.Campaign;
            _campaignEffectType = campaignEffectType;
            _targetType = TargetType.Self; // Campaign effects always affect the player
            _amount = amount;
            _resourceType = resourceType;
        }

        /// <summary>
        /// Gets a human-readable description of this effect.
        /// </summary>
        /// <returns>String like "Deal 10 Confidence damage to Opponent" or "Gain 200₱"</returns>
        public string GetDescription()
        {
            if (_effectContext == EffectContext.Battle)
            {
                return GetBattleEffectDescription();
            }
            else
            {
                return GetCampaignEffectDescription();
            }
        }

        private string GetBattleEffectDescription()
        {
            string action = _battleEffectType switch
            {
                BattleEffectType.ConfidenceDamage => "Deal",
                BattleEffectType.EgoDamage => "Deal",
                BattleEffectType.ConfidenceRestore => "Restore",
                BattleEffectType.EgoRestore => "Restore",
                BattleEffectType.DrawCards => "Draw",
                BattleEffectType.DiscardCards => "Discard",
                BattleEffectType.GainActionPoints => "Gain",
                BattleEffectType.GainBlock => "Gain",
                BattleEffectType.ApplyBuff => "Apply",
                BattleEffectType.ApplyDebuff => "Apply",
                BattleEffectType.DestroyCard => "Destroy",
                BattleEffectType.ExhaustCard => "Exhaust",
                _ => "Unknown"
            };

            string effectName = _battleEffectType switch
            {
                BattleEffectType.ConfidenceDamage => "Confidence damage",
                BattleEffectType.EgoDamage => "Ego damage",
                BattleEffectType.ConfidenceRestore => "Confidence",
                BattleEffectType.EgoRestore => "Ego",
                BattleEffectType.DrawCards => "cards",
                BattleEffectType.DiscardCards => "cards",
                BattleEffectType.GainActionPoints => "Action Points",
                BattleEffectType.GainBlock => "Block",
                BattleEffectType.ApplyBuff => "buff",
                BattleEffectType.ApplyDebuff => "debuff",
                BattleEffectType.DestroyCard => "card",
                BattleEffectType.ExhaustCard => "card",
                _ => ""
            };

            string target = _targetType != TargetType.Self ? $" to {_targetType}" : "";
            return $"{action} {_amount} {effectName}{target}";
        }

        private string GetCampaignEffectDescription()
        {
            string action = _campaignEffectType switch
            {
                CampaignEffectType.GainFunds => "Gain",
                CampaignEffectType.LoseFunds => "Lose",
                CampaignEffectType.GainHeat => "Gain",
                CampaignEffectType.LoseHeat => "Reduce",
                CampaignEffectType.GainInfluence => "Gain",
                CampaignEffectType.LoseInfluence => "Lose",
                CampaignEffectType.AddCardToDeck => "Add card to deck",
                CampaignEffectType.RemoveCardFromDeck => "Remove card from deck",
                CampaignEffectType.UpgradeCard => "Upgrade card",
                CampaignEffectType.TransformCard => "Transform card",
                CampaignEffectType.UnlockLocation => "Unlock location",
                CampaignEffectType.UnlockCard => "Unlock card",
                CampaignEffectType.TriggerEvent => "Trigger event",
                CampaignEffectType.AdvanceDay => "Advance day",
                _ => "Unknown"
            };

            string resourceSymbol = GetCampaignResourceSymbol();

            // Special cases that don't use amount
            if (_campaignEffectType == CampaignEffectType.AddCardToDeck ||
                _campaignEffectType == CampaignEffectType.RemoveCardFromDeck ||
                _campaignEffectType == CampaignEffectType.UpgradeCard ||
                _campaignEffectType == CampaignEffectType.TransformCard ||
                _campaignEffectType == CampaignEffectType.UnlockLocation ||
                _campaignEffectType == CampaignEffectType.UnlockCard ||
                _campaignEffectType == CampaignEffectType.TriggerEvent)
            {
                return action;
            }

            return $"{action} {_amount}{resourceSymbol}";
        }

        /// <summary>
        /// Gets the symbol for campaign resources.
        /// </summary>
        /// <returns>Resource symbol (₱, H, I) or empty string</returns>
        private string GetCampaignResourceSymbol()
        {
            return _resourceType switch
            {
                CampaignResourceType.Funds => "₱",
                CampaignResourceType.Heat => "H",
                CampaignResourceType.Influence => " Influence",
                _ => ""
            };
        }

        /// <summary>
        /// Creates a copy of this effect with modified values.
        /// Useful for applying origin bonuses or other modifiers.
        /// </summary>
        /// <param name="amountModifier">Amount to add to the effect value</param>
        /// <returns>New CardEffect instance with modified amount</returns>
        public CardEffect WithModifiedAmount(int amountModifier)
        {
            if (_effectContext == EffectContext.Battle)
            {
                return new CardEffect(_battleEffectType, _targetType, _amount + amountModifier);
            }
            else
            {
                return new CardEffect(_campaignEffectType, _amount + amountModifier, _resourceType);
            }
        }
    }

    /// <summary>
    /// Defines whether an effect applies during battle or to the campaign.
    /// </summary>
    public enum EffectContext
    {
        Battle,     // Effect applies during card battle (damage Ego, draw cards, etc.)
        Campaign    // Effect applies to campaign state (gain resources, modify Heat, etc.)
    }
}
