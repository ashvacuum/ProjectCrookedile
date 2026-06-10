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

}
