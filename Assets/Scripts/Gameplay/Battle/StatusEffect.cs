using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Represents a status effect (buff/debuff) applied to a combatant.
    /// Stacks decrease by 1 each turn by default (Slay the Spire style).
    /// Can be marked as permanent (doesn't decrease) or removed at end of turn.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        [InfoBox("@GetEffectDescription()")]
        [SerializeField] private StatusEffectType _type;

        [Tooltip("Number of stacks. Most effects scale linearly with stacks.")]
        [SerializeField] private int _stacks;

        [Tooltip("DecreasePerTurn: loses 1 stack each turn.\nRemoveEndOfTurn: gone entirely at end of turn.\nPermanent: never expires.\nRemoveAtPlayerTurnStart: removed when the player's next turn begins (e.g. Stunned).")]
        [SerializeField] private StatusDurationType _durationType;

        public StatusEffectType Type => _type;
        public int Stacks => _stacks;
        public StatusDurationType DurationType => _durationType;

        public StatusEffect(StatusEffectType type, int stacks, StatusDurationType durationType = StatusDurationType.DecreasePerTurn)
        {
            _type = type;
            _stacks = stacks;
            _durationType = durationType;
        }

        /// <summary>
        /// Add stacks to this status effect.
        /// </summary>
        public void AddStacks(int amount)
        {
            _stacks += amount;
        }

        /// <summary>
        /// Reduce stacks by 1 (called each turn). Returns true if depleted.
        /// Moves toward 0 in both directions so negative stacks (e.g. -3 Strength)
        /// fade correctly: -3 → -2 → -1 → 0 (removed).
        /// </summary>
        public bool DecrementStack()
        {
            if (_durationType == StatusDurationType.Permanent) return false;
            if (_durationType == StatusDurationType.RemoveAtPlayerTurnStart) return false;

            // Always step toward 0 so both positive and negative stacks expire correctly.
            if (_stacks > 0)
                _stacks--;
            else
                _stacks++;

            return _stacks == 0;
        }

        /// <summary>
        /// Reduce stacks by specific amount. Returns true if depleted.
        /// </summary>
        public bool ReduceStacks(int amount)
        {
            _stacks -= amount;
            return _stacks <= 0;
        }

        // ─── Editor Helpers ───────────────────────────────────────────────────────

        private string GetEffectDescription() => _type switch
        {
            // Debuffs
            StatusEffectType.Weakened       => "Deal X less damage.",
            StatusEffectType.Vulnerable     => "Take 50% more damage.",
            StatusEffectType.Frail          => "Gain 25% less Composure.",
            StatusEffectType.Entangled      => "All cards cost +1 AP.",
            StatusEffectType.Exposed        => "Next attack against this target deals double damage.",
            StatusEffectType.Scandal        => "Take X damage at end of turn (like Poison).",
            StatusEffectType.Confused       => "Effect values are randomised each turn.",
            StatusEffectType.Silenced       => "Cannot play Rhetoric cards.",
            StatusEffectType.Stunned        => "Skips its next action. Removed at start of player turn.",
            StatusEffectType.Rattled        => "Take bonus damage equal to attacker Hostility per stack (reduced if attacker is Receptive).",
            // Buffs
            StatusEffectType.Strength       => "Deal X more damage.",
            StatusEffectType.Dexterity      => "Gain X more Composure per card played.",
            StatusEffectType.Focus          => "Cards cost X less AP (this turn only).",
            StatusEffectType.Energized      => "Cards cost X less AP this turn.",
            StatusEffectType.Plated         => "Reduce incoming damage by X.",
            StatusEffectType.Regeneration   => "Heal X Resolve at end of turn.",
            StatusEffectType.Intangible     => "Take only 1 damage from all attacks.",
            StatusEffectType.Thorns         => "Reflect X to the Opinion Meter when hit.",
            // Special
            StatusEffectType.Ritual         => "Gain X Composure at the start of each turn.",
            StatusEffectType.Momentum       => "Deal X damage to a random enemy per card played this turn.",
            StatusEffectType.Echo           => "The next card played is resolved twice.",
            _                               => ""
        };
    }

    /// <summary>
    /// How status effect stacks are managed over time.
    /// </summary>
    public enum StatusDurationType
    {
        DecreasePerTurn,            // Default: Stacks reduce by 1 each turn (Slay the Spire)
        RemoveEndOfTurn,            // Removed entirely at end of turn (like Focus, Intangible)
        Permanent,                  // Stacks never decrease (until manually removed)
        RemoveAtPlayerTurnStart     // Removed when the player's turn begins (e.g. Stunned)
    }

    /// <summary>
    /// All possible status effects in the game.
    /// Each has specific behavior defined in StatusEffectManager.
    /// </summary>
    public enum StatusEffectType
    {
        // DEBUFFS (Negative)
        Weakened,       // Deal X less damage
        Vulnerable,     // Take 50% more damage (opinion meter)
        Frail,          // Gain X% less Composure (usually 25%)
        Entangled,      // Cards cost +1 AP
        Exposed,        // Next attack deals double damage
        Scandal,        // Take X damage at end of turn (like Poison)
        Confused,       // Effect values are randomised each turn
        Silenced,       // Cannot play Rhetoric cards
        Stunned,        // Skips its next action; removed at start of player turn (non-stackable)
        Rattled,        // Take bonus/reduced damage equal to attacker's Hostility per stack

        // BUFFS (Positive)
        Strength,       // Deal X more damage
        Dexterity,      // Gain X more Composure per card
        Focus,          // Cards cost X less AP (this turn only)
        Energized,      // Cards cost X less AP this turn
        Plated,         // Reduce incoming damage by X
        Regeneration,   // Heal X Resolve at end of turn
        Intangible,     // Take only 1 damage from attacks (duration-based)
        Thorns,         // Reflect X to Opinion Meter when hit (no Resolve damage)

        // SPECIAL
        Ritual,         // Gain X Composure at start of turn
        Momentum,       // Deal X damage to a random enemy per card played this turn
        Echo,           // Next card is played twice
    }

    /// <summary>
    /// Defines when a status effect triggers.
    /// </summary>
    public enum StatusTriggerTiming
    {
        OnTurnStart,    // Start of combatant's turn
        OnTurnEnd,      // End of combatant's turn
        OnDamageDealt,  // When dealing damage
        OnDamageTaken,  // When taking damage
        OnCardPlayed,   // When playing a card
        OnComposureGain,// When gaining Composure
        Passive         // Always active (modifier to stats)
    }

    /// <summary>
    /// Metadata for each status effect type.
    /// Defines behavior, trigger timing, and description.
    /// </summary>
    [Serializable]
    public class StatusEffectData
    {
        public StatusEffectType type;
        public string displayName;
        public string description;
        public StatusTriggerTiming triggerTiming;
        public bool isDebuff;
        public bool stacksReducePerTurn; // If true, reduces by 1 stack per turn
        public bool durationReducesPerTurn; // If true, duration reduces by 1 per turn

        // Icons/visuals
        public Sprite icon;
        public Color color;
    }
}
