using System;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>How a status is grouped for presentation and rules-of-thumb.</summary>
    public enum StatusCategory
    {
        Debuff,
        Buff,
        Pacify, // Faith Leader: counts toward conversion (Guilt/Shame/Doubt)
        Threshold, // Faith Leader: Jaded
        HostilityFlag, // Hardened/Fanatic/Turncoat/Devotion
        Special,
    }

    /// <summary>
    /// Polymorphic status effect.
    /// Each concrete status is one [Serializable] subclass that owns its own rules (description,
    /// debuff/buff, the pressure/support/cost modify hooks, hostility flags, pacify flag). Statuses are
    /// stacked/looked-up by concrete <see cref="System.Type"/> at runtime; <see cref="Id"/> is the
    /// stable key for serialization, the visual registry, and events.
    ///
    /// Default hooks are pass-through, so a behavior only overrides what it actually changes.
    /// Stateful/consume-on-trigger statuses (Exposed, Intangible) and turn-tick statuses
    /// (Ritual, Smear, Regeneration) are read by type in the manager/BattleManager, not via these hooks.
    /// </summary>
    [Serializable]
    public abstract class StatusBehavior
    {
        /// <summary>Stable, lowercase, never-reused id (serialization / visuals / events).</summary>
        public abstract string Id { get; }

        public virtual string DisplayName => Id;

        public abstract bool IsDebuff { get; }

        public virtual StatusCategory Category => IsDebuff ? StatusCategory.Debuff : StatusCategory.Buff;

        /// <summary>True if this status counts toward the Faith Leader pacify threshold (Guilt/Shame/Doubt).</summary>
        public virtual bool CountsTowardPacify => false;

        /// <summary>Human-readable description for the given stack count.</summary>
        public virtual string Describe(int stacks) => DisplayName;

        #region Opinion / resource pipeline (pure pass-through by default)

        /// <summary>Adjusts the Opinion shift this combatant DEALS to the meter (Strength/Weakened/Guilt/Turncoat).</summary>
        public virtual float ModifyOutgoingOpinion(float amount, int stacks) => amount;

        /// <summary>Adjusts the Opinion shift this combatant TAKES (Vulnerable/Plated/Rattled).</summary>
        public virtual float ModifyIncomingOpinion(float amount, int stacks, int attackerHostility) =>
            amount;

        /// <summary>Adjusts Support gained by the player (Dexterity/Frail).</summary>
        public virtual int ModifySupportGained(int support, int stacks) => support;

        /// <summary>Adjusts Denial gained by an enemy (Shame drops the enemy's Denial).</summary>
        public virtual int ModifyDenialGained(int denial, int stacks) => denial;

        /// <summary>Adjusts card AP cost (Focus/Energized/Entangled).</summary>
        public virtual int ModifyCardCost(int cost, int stacks) => cost;

        /// <summary>
        /// True for statuses consumed after a single incoming hit (Exposed doubles then drops;
        /// Intangible caps at 1 then drops). The manager removes a stack after applying incoming mods.
        /// </summary>
        public virtual bool ConsumedOnIncomingHit => false;

        /// <summary>
        /// When true the manager applies this status's incoming modifier LAST (after all others) —
        /// for hard overrides like Intangible (take exactly 1).
        /// </summary>
        public virtual bool IncomingOverride => false;

        #endregion

        #region Hostility flags

        /// <summary>Fanatic — can't be riled (hostility gains are no-ops).</summary>
        public virtual bool BlocksHostilityGain => false;

        /// <summary>Hardened — won't listen to reason (hostility reductions are no-ops).</summary>
        public virtual bool BlocksHostilityReduction => false;

        /// <summary>Devotion — reduces incoming hostility gains by this much per stack.</summary>
        public virtual int HostilityResistPerStack => 0;

        /// <summary>Called when this status's stacks hit zero and it falls off its owner (Fanatic snaps hostility back up).</summary>
        public virtual void OnDepleted(BattleStats owner) { }

        #endregion
    }
}
