using Crookedile.Data;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Runtime context accumulated during a single card's effect resolution.
    /// Created once per card play and passed through every resolver sub-call so that
    /// triggered effects can read values produced by earlier base effects.
    ///
    /// Example: a lifesteal triggered effect reads <see cref="LastDamageDealt"/> to know
    /// how much Resolve to restore to the caster.
    /// </summary>
    public class EffectContext
    {
        /// <summary>Total Resolve damage dealt to any target(s) by this card's base effects.</summary>
        public int LastDamageDealt { get; set; }

        /// <summary>Total Resolve healing applied by this card's base effects.</summary>
        public int LastHealAmount { get; set; }

        /// <summary>Total Composure gained by this card's base effects.</summary>
        public int LastComposureGained { get; set; }

        /// <summary>Total Composure lost by this card's base effects.</summary>
        public int LastComposureLost { get; set; }

        /// <summary>True if any target's Resolve reached 0 during this card's resolution.</summary>
        public bool LastTargetDied { get; set; }

        /// <summary>The BattleStats of the card's caster (set at card-play time).</summary>
        public BattleStats Caster { get; set; }

        /// <summary>The BattleStats of the primary/focused target (set at card-play time).</summary>
        public BattleStats Target { get; set; }

        /// <summary>
        /// Set by <see cref="CardManipulationType.ExhaustThisCard"/> so <c>BattleManager</c>
        /// can move the card from the discard pile to the exhaust pile after all effects resolve.
        /// </summary>
        public bool ShouldExhaust { get; set; }

        /// <summary>
        /// Retrieves the integer value indicated by <paramref name="source"/>.
        /// Returns 0 for <see cref="EffectContextValue.FixedAmount"/> — the caller
        /// should use the authored amount on the <c>CardEffect</c> in that case.
        /// </summary>
        public int GetValue(EffectContextValue source)
        {
            return source switch
            {
                EffectContextValue.LastDamageDealt => LastDamageDealt,
                EffectContextValue.LastHealAmount => LastHealAmount,
                EffectContextValue.LastComposureGained => LastComposureGained,
                EffectContextValue.LastComposureLost => LastComposureLost,
                EffectContextValue.CurrentComposure => Caster?.CurrentComposure ?? 0,
                EffectContextValue.CurrentHostility => Target?.CurrentHostility ?? 0,
                _ => 0, // FixedAmount — use authored value
            };
        }
    }
}
