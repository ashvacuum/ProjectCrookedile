using System;
using System.Collections.Generic;
using Crookedile.Gameplay.Battle;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// A named reactive effect that fires automatically after a card's base effects resolve,
    /// when a specific trigger event occurred AND an optional condition is met.
    ///
    /// Both the trigger and the condition must be true for the response to execute.
    /// Triggered effects share the same <see cref="Effects.EffectExecutionContext"/> as the
    /// card's new-path base effects, so responses can reference runtime values such as the
    /// damage just dealt (via <c>AmountSource = LastDamageDealt</c>).
    ///
    /// <example>
    /// Lifesteal — "Heal for the damage you dealt":
    ///   Name:      "Lifesteal"
    ///   Trigger:   OnDamageDealt
    ///   Condition: Always
    ///   Response:  HealResolveEffect (AmountSource = LastDamageDealt)
    ///
    /// Kill-draw — "Draw 2 cards if you defeat an enemy":
    ///   Name:      "Kill Reward"
    ///   Trigger:   OnKill
    ///   Condition: IfTargetDied
    ///   Response:  DrawCardsEffect (Amount = 2)
    ///
    /// Pile-on — "Apply Weakened only if target already has a debuff":
    ///   Name:      "Pile On"
    ///   Trigger:   OnDamageDealt
    ///   Condition: IfTargetHasDebuff
    ///   Response:  ApplyStatusEffect (Weakened, 2)
    /// </example>
    /// </summary>
    [Serializable]
    public class TriggeredEffect
    {
        [Tooltip("Human-readable name for this triggered effect (shown in logs and tooltips).")]
        [SerializeField] private string _name = "Triggered Effect";

        [Title("Trigger")]
        [Tooltip("The in-resolution event that can activate this effect.")]
        [EnumToggleButtons]
        [SerializeField] private EffectTrigger _trigger = EffectTrigger.OnDamageDealt;

        [Title("Condition")]
        [Tooltip("Optional extra condition that must also be true. 'Always' means no restriction.")]
        [EnumToggleButtons]
        [SerializeField] private EffectCondition _condition = EffectCondition.Always;

        [ShowIf("_condition", EffectCondition.IfAmountAboveThreshold)]
        [Tooltip("The relevant context amount must be strictly greater than this value.")]
        [SerializeField] private int _conditionThreshold = 0;

        [Title("Response Effects")]
        [Tooltip("One or more BattleEffects to execute when both trigger and condition are satisfied.\n\n" +
                 "Use the + button to add effects; pick any BattleEffect subclass from the type menu.\n" +
                 "Example — Lifesteal: add HealResolveEffect and set AmountSource = LastDamageDealt.")]
        [SerializeReference]
        [SerializeField] private List<BattleEffect> _effects = new List<BattleEffect>();

        // ─── Properties ───────────────────────────────────────────────────────────

        /// <summary>Human-readable label for this triggered effect.</summary>
        public string Name => _name;

        /// <summary>The event that must occur during resolution to activate this effect.</summary>
        public EffectTrigger Trigger => _trigger;

        /// <summary>Extra condition that must be true (in addition to the trigger).</summary>
        public EffectCondition Condition => _condition;

        /// <summary>Used when Condition == IfAmountAboveThreshold.</summary>
        public int ConditionThreshold => _conditionThreshold;

        /// <summary>The effects to resolve when this trigger fires.</summary>
        public IReadOnlyList<BattleEffect> Effects => _effects;
    }
}
