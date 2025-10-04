using System;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines a passive ability for an origin (like Slay the Spire relics).
    /// Each origin has unique passive abilities that activate during gameplay.
    /// Examples:
    /// - Strongman: "Whenever you play an Attack, gain 1 Influence"
    /// - Celebrity: "Start each battle with +2 Confidence"
    /// - Religious Leader: "Restore 3 Confidence at the end of your turn"
    /// </summary>
    [CreateAssetMenu(fileName = "New Origin Passive", menuName = "Crookedile/Origin Passive")]
    public class OriginPassive : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of the passive ability")]
        [SerializeField] private string _passiveName;

        [Tooltip("Which origin has this passive")]
        [SerializeField] private OriginType _origin;

        [Header("Description")]
        [Tooltip("Description of what the passive does")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Tooltip("Icon representing this passive")]
        [SerializeField] private Sprite _icon;

        [Header("Trigger Conditions")]
        [Tooltip("When does this passive trigger?")]
        [SerializeField] private PassiveTrigger _trigger;

        [Tooltip("Optional: Specific card type that triggers this (if trigger is OnCardPlayed)")]
        [SerializeField] private CardType _triggerCardType;

        [Header("Effects")]
        [Tooltip("What happens when the passive triggers? (can be battle or campaign effect)")]
        [SerializeField] private PassiveEffectType _effectType;

        [Tooltip("Magnitude of the effect")]
        [SerializeField] private int _effectAmount;

        #region Properties

        public string PassiveName => _passiveName;
        public OriginType Origin => _origin;
        public string Description => _description;
        public Sprite Icon => _icon;
        public PassiveTrigger Trigger => _trigger;
        public CardType TriggerCardType => _triggerCardType;
        public PassiveEffectType EffectType => _effectType;
        public int EffectAmount => _effectAmount;

        #endregion

        /// <summary>
        /// Gets formatted passive text for UI display.
        /// </summary>
        public string GetFormattedText()
        {
            return $"<b>{_passiveName}</b>\n{_description}";
        }
    }

    /// <summary>
    /// When does a passive ability trigger?
    /// </summary>
    public enum PassiveTrigger
    {
        BattleStart,        // At the start of every battle
        TurnStart,          // At the start of your turn
        TurnEnd,            // At the end of your turn
        OnCardPlayed,       // When you play a card (can filter by type)
        OnDamageTaken,      // When you take damage
        OnDamageDealt,      // When you deal damage
        OnConfidenceLost,   // When you lose Confidence
        OnEgoLost,          // When you lose Ego
        OnCardDrawn,        // When you draw a card
        BattleEnd,          // At the end of battle (before rewards)
        Always              // Passive effect (no trigger, always active)
    }

    /// <summary>
    /// What effect does the passive have?
    /// </summary>
    public enum PassiveEffectType
    {
        // Battle Effects
        GainConfidence,
        GainEgo,
        GainActionPoints,
        GainBlock,
        DrawCards,
        DealBonusDamage,

        // Campaign Effects
        GainInfluence,
        GainFunds,
        ReduceHeat,

        // Modifiers
        ReduceCardCost,         // Next card costs less
        IncreaseCardEffect,     // Next card effect is stronger
        ExtraCardReward,        // Gain extra card after battle

        // Special
        Custom                  // Requires custom logic implementation
    }
}
