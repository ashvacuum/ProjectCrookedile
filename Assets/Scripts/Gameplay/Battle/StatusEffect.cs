using System;
using Sirenix.OdinInspector;
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
        [InfoBox("@GetEffectDescription()")]
        [SerializeField]
        private StatusEffectType _type;

        [Tooltip("Number of stacks. Most effects scale linearly with stacks.")]
        [SerializeField]
        private int _stacks;

        [Tooltip(
            "DecreasePerTurn: loses 1 stack each turn.\nRemoveEndOfTurn: gone entirely at end of turn.\nPermanent: never expires.\nRemoveAtPlayerTurnStart: removed when the player's next turn begins (e.g. Stunned)."
        )]
        [SerializeField]
        private StatusDurationType _durationType;

        public StatusEffectType Type => _type;
        public int Stacks => _stacks;
        public StatusDurationType DurationType => _durationType;

        public StatusEffect(
            StatusEffectType type,
            int stacks,
            StatusDurationType durationType = StatusDurationType.DecreasePerTurn
        )
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
            if (_durationType == StatusDurationType.Permanent)
                return false;
            if (_durationType == StatusDurationType.RemoveAtPlayerTurnStart)
                return false;

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

        #region Editor Helpers
        private string GetEffectDescription() =>
            _type switch
            {
                // Debuffs
                StatusEffectType.Weakened => "Deal X less damage.",
                StatusEffectType.Vulnerable => "Take 50% more damage.",
                StatusEffectType.Frail => "Gain 25% less Support.",
                StatusEffectType.Entangled => "All cards cost +1 AP.",
                StatusEffectType.Exposed => "Next attack against this target deals double damage.",
                StatusEffectType.Smear =>
                    "Take X opinion pressure at end of turn (reputation bleed).",
                StatusEffectType.Confused => "Effect values are randomised each turn.",
                StatusEffectType.Silenced => "Cannot play Rhetoric cards.",
                StatusEffectType.Stunned =>
                    "Skips its next action. Removed at start of player turn.",
                StatusEffectType.Rattled =>
                    "Take bonus damage equal to attacker Hostility per stack (reduced if attacker is Receptive).",
                StatusEffectType.Guilt =>
                    "Pacify status: blunts the enemy's push — it deals X less opinion pressure per stack. Counts toward conversion.",
                StatusEffectType.Shame =>
                    "Pacify status: the enemy can't defend the meter — it gains X less Denial per stack. Counts toward conversion.",
                StatusEffectType.Doubt =>
                    "Pacify status: the enemy may hold back its action (soft skip chance per stack). Counts toward conversion.",
                StatusEffectType.Jaded =>
                    "Threshold status: each stack raises this enemy's pacify cost by 1. Permanent; gained on each conversion; never consumed.",
                // Buffs
                StatusEffectType.Strength => "Deal X more damage.",
                StatusEffectType.Dexterity => "Gain X more Support per card played.",
                StatusEffectType.Focus => "Cards cost X less AP (this turn only).",
                StatusEffectType.Energized => "Cards cost X less AP this turn.",
                StatusEffectType.Plated => "Reduce incoming damage by X.",
                StatusEffectType.Regeneration => "Raise Opinion by X at the end of each turn.",
                StatusEffectType.Intangible => "Take only 1 damage from all attacks.",
                StatusEffectType.Thorns => "Reflect X to the Opinion Meter when hit.",
                // Special
                StatusEffectType.Ritual => "Gain X Support at the start of each turn.",
                StatusEffectType.Hardened => "De-escalation cards have no effect.",
                StatusEffectType.Fanatic => "Cannot be riled up; can still be won over.",
                StatusEffectType.Momentum =>
                    "Deal X damage to a random enemy per card played this turn.",
                StatusEffectType.Echo => "The next card played is resolved twice.",
                StatusEffectType.Turncoat =>
                    "Freshly betrayed: deals +X bonus pressure per stack, fading over a turn or two.",
                StatusEffectType.Devotion =>
                    "Steadfast: resists hostility gains by X per stack (protects converts from Sway/Rile).",
                _ => "",
            };
    }

    /// <summary>
    /// How status effect stacks are managed over time.
    /// </summary>
    public enum StatusDurationType
    {
        DecreasePerTurn, // Default: Stacks reduce by 1 each turn (Slay the Spire)
        RemoveEndOfTurn, // Removed entirely at end of turn (like Focus, Intangible)
        Permanent, // Stacks never decrease (until manually removed)
        RemoveAtPlayerTurnStart, // Removed when the player's turn begins (e.g. Stunned)
    }

    /// <summary>
    /// All possible status effects in the game.
    /// Each has specific behavior defined in StatusEffectManager.
    /// </summary>
    public enum StatusEffectType
    {
        // DEBUFFS (Negative)
        Weakened, // Deal X less damage
        Vulnerable, // Take 50% more damage (opinion meter)
        Frail, // Gain X% less Support (usually 25%)
        Entangled, // Cards cost +1 AP
        Exposed, // Next attack deals double damage
        Smear, // Reputation bleed — take X opinion pressure at end of turn (like Poison). Currently unused by player classes; reserved for hostile enemies.
        Confused, // Effect values are randomised each turn
        Silenced, // Cannot play Rhetoric cards
        Stunned, // Skips its next action; removed at start of player turn (non-stackable)
        Rattled, // Take bonus/reduced damage equal to attacker's Hostility per stack
        Guilt, // Pacify status — blunts enemy push: deals X less opinion pressure per stack; counts toward conversion
        Shame, // Pacify status — drops enemy shield: gains X less Denial per stack; counts toward conversion
        Doubt, // Pacify status — soft skip chance per stack; counts toward conversion
        Jaded, // Threshold status — raises pacify cost by 1 per stack; permanent, gained on conversion, never consumed

        // BUFFS (Positive)
        Strength, // Deal X more damage
        Dexterity, // Gain X more Support per card
        Focus, // Cards cost X less AP (this turn only)
        Energized, // Cards cost X less AP this turn
        Plated, // Reduce incoming damage by X
        Regeneration, // Raise Opinion by X at end of turn
        Intangible, // Take only 1 damage from attacks (duration-based)
        Thorns, // Reflect X pressure to the Opinion Meter when hit

        // SPECIAL
        Ritual, // Gain X Support at start of turn
        Hardened, // ReduceHostility is a no-op — won't listen to reason
        Fanatic, // GainHostility is a no-op — can't be riled up; can be won over
        Momentum, // Deal X damage to a random enemy per card played this turn
        Echo, // Next card is played twice
        Turncoat, // A freshly-betrayed enemy: deals +X bonus pressure per stack, fades over a turn or two
        Devotion, // Steadfast loyalty — resists hostility gains by X per stack (protects converts)
    }

    /// <summary>
    /// Defines when a status effect triggers.
    /// </summary>
    public enum StatusTriggerTiming
    {
        OnTurnStart, // Start of combatant's turn
        OnTurnEnd, // End of combatant's turn
        OnDamageDealt, // When dealing damage
        OnDamageTaken, // When taking damage
        OnCardPlayed, // When playing a card
        OnShieldGain, // When gaining Shield
        Passive, // Always active (modifier to stats)
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
        #endregion
