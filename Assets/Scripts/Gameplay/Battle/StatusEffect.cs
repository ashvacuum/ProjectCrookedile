using System;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// A status effect (buff/debuff) applied to a combatant: a canonical <see cref="StatusBehavior"/>
    /// plus per-application stacks and duration. Stacks decrease by 1 each turn by default
    /// (Slay the Spire style). Can be marked as permanent (doesn't decrease) or removed at end of turn.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        private readonly StatusBehavior _behavior;

        [SerializeField]
        private int _stacks;

        [SerializeField]
        private StatusDurationType _durationType;

        /// <summary>The canonical behavior defining this status's rules.</summary>
        public StatusBehavior Behavior => _behavior;

        /// <summary>Stable id of the underlying behavior (storage / visuals / events key).</summary>
        public string Id => _behavior.Id;

        public string DisplayName => _behavior.DisplayName;

        /// <summary>
        /// Transitional: the legacy enum value for this status. Goes away with the enum.
        /// </summary>
        public StatusEffectType Type =>
            StatusBridge.TryToEnum(_behavior, out StatusEffectType t) ? t : default;

        public int Stacks => _stacks;
        public StatusDurationType DurationType => _durationType;

        /// <summary>Human-readable description of this status at its current stack count.</summary>
        public string Description => _behavior.Describe(_stacks);

        public StatusEffect(
            StatusBehavior behavior,
            int stacks,
            StatusDurationType durationType = StatusDurationType.DecreasePerTurn
        )
        {
            _behavior = behavior ?? throw new ArgumentNullException(nameof(behavior));
            _stacks = stacks;
            _durationType = durationType;
        }

        /// <summary>Transitional: construct from the legacy enum (maps via the bridge).</summary>
        public StatusEffect(
            StatusEffectType type,
            int stacks,
            StatusDurationType durationType = StatusDurationType.DecreasePerTurn
        )
            : this(StatusBridge.ToBehavior(type), stacks, durationType) { }

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
    /// LEGACY — being replaced by the polymorphic <see cref="StatusBehavior"/> registry.
    /// Still referenced by transitional bridge wrappers and a few serialized fields;
    /// deleted in the final migration step.
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
}
