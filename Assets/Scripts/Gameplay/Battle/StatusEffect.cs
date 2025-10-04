using System;
using UnityEngine;

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
        [SerializeField] private StatusEffectType _type;
        [SerializeField] private int _stacks;
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
        /// </summary>
        public bool DecrementStack()
        {
            if (_durationType == StatusDurationType.Permanent) return false;

            _stacks--;
            return _stacks <= 0;
        }

        /// <summary>
        /// Reduce stacks by specific amount. Returns true if depleted.
        /// </summary>
        public bool ReduceStacks(int amount)
        {
            _stacks -= amount;
            return _stacks <= 0;
        }
    }

    /// <summary>
    /// How status effect stacks are managed over time.
    /// </summary>
    public enum StatusDurationType
    {
        DecreasePerTurn,    // Default: Stacks reduce by 1 each turn (Slay the Spire)
        RemoveEndOfTurn,    // Removed entirely at end of turn (like Focus, Intangible)
        Permanent           // Stacks never decrease (until manually removed)
    }

    /// <summary>
    /// All possible status effects in the game.
    /// Each has specific behavior defined in StatusEffectManager.
    /// </summary>
    public enum StatusEffectType
    {
        // DEBUFFS (Negative)
        Weakened,       // Deal X less damage
        Vulnerable,     // Take X% more damage (usually 50%)
        Frail,          // Gain X% less Composure (usually 25%)
        Entangled,      // Cards cost +1 AP
        Exposed,        // Next attack deals double damage
        Scandal,        // Take X damage at end of turn (like Poison)
        Confused,       // Random card costs +1 AP each turn
        Silenced,       // Cannot play Manipulate cards

        // BUFFS (Positive)
        Strength,       // Deal X more damage
        Dexterity,      // Gain X more Composure per card
        Focus,          // Cards cost X less AP (this turn only)
        Energized,      // Draw X extra cards next turn
        Plated,         // Reduce incoming damage by X
        Regeneration,   // Heal X Resolve at end of turn
        Intangible,     // Take only 1 damage from attacks (duration-based)
        Thorns,         // Deal X damage back when attacked

        // SPECIAL
        Block,          // Temporary damage reduction (Slay the Spire style)
        Ritual,         // Gain X Composure at start of turn
        Momentum,       // Gain X damage per card played this turn
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
