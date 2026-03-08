using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Gameplay.Battle;

namespace Crookedile.Data
{
    /// <summary>
    /// Defines a passive ability for an origin (like Slay the Spire relics).
    /// Fully data-driven: trigger, condition, one-shot flag and effect are all
    /// configured in the Inspector — no code changes required for new passives.
    ///
    /// Create via: Assets → Create → Crookedile → Origin Passive
    /// </summary>
    [CreateAssetMenu(fileName = "New Origin Passive", menuName = "Crookedile/Origin Passive")]
    public class OriginPassive : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Name of the passive ability, e.g. 'Discipline'")]
        [SerializeField] private string _passiveName;

        [Tooltip("Which origin has this passive")]
        [SerializeField] private OriginType _origin;

        [Header("Description")]
        [TextArea(2, 4)]
        [Tooltip("Description shown to the player in the UI")]
        [SerializeField] private string _description;

        [Tooltip("Icon representing this passive")]
        [SerializeField] private Sprite _icon;

        [Header("Trigger")]
        [Tooltip("Which battle event causes this passive to attempt to fire?")]
        [SerializeField] private PassiveTrigger _trigger;

        [Header("Condition")]
        [Tooltip("Additional condition that must be true each time the trigger fires.")]
        [SerializeField] private PassiveCondition _condition;

        [Header("One Shot")]
        [Tooltip("If true, the passive fires exactly once per battle then goes silent.")]
        [SerializeField] private bool _oneShot;

        [Header("Effect (Legacy — single effect, enum-based)")]
        [Tooltip("Legacy: What happens when the passive fires? Use 'Passives (New System)' below for new content.")]
        [SerializeField] private PassiveEffectType _effectType;

        [Tooltip("Legacy: Magnitude of the effect (e.g. how many AP, how many cards to draw)")]
        [SerializeField] private int _effectAmount;

        [Title("Passives (New System)")]
        [Tooltip("Polymorphic passives using the BattlePassive + BattleEffect hierarchy.\n" +
                 "Add entries here for all new content. The legacy fields above are kept for\n" +
                 "backward compatibility (Improvise etc.) and will be migrated later.\n\n" +
                 "When this list is non-empty, PassiveResolver uses it instead of the legacy path.")]
        [SerializeReference]
        [SerializeField] private List<BattlePassive> _passives = new List<BattlePassive>();

        #region Properties

        public string            PassiveName   => _passiveName;
        public OriginType        Origin        => _origin;
        public string            Description   => _description;
        public Sprite            Icon          => _icon;
        public PassiveTrigger    Trigger       => _trigger;
        public PassiveCondition  Condition     => _condition;
        public bool              OneShot       => _oneShot;
        public PassiveEffectType EffectType    => _effectType;
        public int               EffectAmount  => _effectAmount;

        /// <summary>
        /// New-system polymorphic passives. When non-empty, PassiveResolver uses these
        /// instead of the legacy Trigger/Condition/EffectType fields.
        /// </summary>
        public IReadOnlyList<BattlePassive> Passives => _passives;

        #endregion

        /// <summary>Returns formatted passive text for UI display.</summary>
        public string GetFormattedText() => $"<b>{_passiveName}</b>\n{_description}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PassiveTrigger — which battle event fires this passive?
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Which battle moment causes the passive to attempt to fire.
    /// Pair with a <see cref="PassiveCondition"/> to add additional gating.
    /// </summary>
    public enum PassiveTrigger
    {
        BattleStart,           // Once after opening hand is dealt
        TurnStart,             // Each player turn start
        TurnEnd,               // Each player turn end
        OnCardPlayed,          // Any card played from hand
        OnPressureCardPlayed,  // A Pressure-type card played
        OnRhetoricCardPlayed,  // A Rhetoric-type card played
        OnPolicyCardPlayed,    // A Policy-type card played
        OnDamageTaken,         // Player takes resolve damage (> 0)
        OnDamageDealt,         // Player deals resolve damage to an enemy
        OnStatusApplied,       // Player applies any status effect to an enemy
        OnCardDrawn,           // Player draws a card
        OnCardDiscarded,       // Player discards a card (not exhaust)
        OnComposureLost,       // Player loses any composure stacks
        OnEnemyDefeated,       // Any enemy's resolve reaches zero
        BattleEnd,             // When the battle concludes (victory or defeat)
        Always,                // Continuous passive — no trigger event
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PassiveCondition — additional gate checked each time the trigger fires
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Types of condition that can gate a passive from firing.
    /// </summary>
    public enum PassiveConditionType
    {
        Always,            // No condition — fires every time the trigger fires
        TurnNumberEquals,  // Only when player turn number == Value   (e.g. turn 1 only)
        TurnNumberAtMost,  // Only when player turn number <= Value   (e.g. first 3 turns)
        ResolveBelow,      // Only when player resolve <= Value% of max resolve
        NthEvent,          // Only every Value-th trigger fire        (e.g. every 5th card)
    }

    /// <summary>
    /// Data-driven condition evaluated each time the parent <see cref="PassiveTrigger"/> fires.
    /// Configured entirely in the Inspector — no code changes needed for new conditions.
    /// </summary>
    [Serializable]
    public struct PassiveCondition
    {
        [HideLabel, EnumToggleButtons]
        public PassiveConditionType Type;

        [ShowIf("@Type != PassiveConditionType.Always")]
        [MinValue(1)]
        [Tooltip("Turn number, resolve%, or N — interpretation depends on condition type.")]
        public int Value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PassiveEffectType — what the passive does when it fires
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The effect executed when a passive fires.
    /// Campaign effects (GainInfluence etc.) are declared but no-op inside a battle.
    /// </summary>
    public enum PassiveEffectType
    {
        // ── Battle Effects ────────────────────────────────────────────────────
        GainResolve,
        GainComposure,
        GainActionPoints,
        DrawCards,
        DealBonusDamage,
        ReduceHostility,

        // ── Campaign Effects ──────────────────────────────────────────────────
        GainInfluence,
        GainFunds,
        ReduceHeat,

        // ── Modifiers ─────────────────────────────────────────────────────────
        ReduceCardCost,       // Next card costs less AP
        IncreaseCardEffect,   // Next card effect is stronger
        ExtraCardReward,      // Gain an extra card reward after battle
        ExtraCardDraw,        // Draw extra cards at the start of battle

        // ── Special ───────────────────────────────────────────────────────────
        Improvise,            // Actor: open the discard-and-redraw selection panel
    }
}
