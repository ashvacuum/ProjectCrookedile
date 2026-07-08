using System;
using System.Collections.Generic;
using System.Text;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// A self-contained, data-driven passive ability that fires effects in response to battle events.
    ///
    /// Composed of:
    ///   • One polymorphic <see cref="PassiveTriggerBase"/> — which event fires this passive
    ///   • Zero or more <see cref="PassiveConditionBase"/> — all must pass for the passive to fire
    ///   • One "one-shot" flag — if true, the passive fires exactly once per battle
    ///   • One or more <see cref="BattleEffect"/> — executed when all checks pass
    ///
    /// Card and origin passives both use this class. Authoring is done entirely in the Unity
    /// inspector via Odin's [SerializeReference] type picker — no code changes needed.
    ///
    /// Runtime state (<see cref="Spent"/>, <see cref="_fireCount"/>) must be reset via
    /// <see cref="ResetForBattle"/> at the start of each battle.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class BattlePassive
    {
        [Tooltip("Display name used in logs and tooltips.")]
        [SerializeField]
        private string _name = "New Passive";

        [Tooltip("Which battle event causes this passive to attempt to fire.")]
        [SerializeReference]
        private PassiveTriggerBase _trigger;

        [Tooltip("All conditions must pass. Leave empty to always fire when the trigger fires.")]
        [SerializeReference]
        private List<PassiveConditionBase> _conditions = new List<PassiveConditionBase>();

        [Tooltip("If true, this passive fires exactly once per battle then goes silent.")]
        [SerializeField]
        private bool _oneShot;

        [Tooltip("Effects executed when the passive fires. Reuses the BattleEffect hierarchy.")]
        [SerializeReference]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        #region Runtime state (not serialized)
        /// <summary>True after a one-shot passive has fired. Reset via <see cref="ResetForBattle"/>.</summary>
        public bool Spent { get; private set; }

        /// <summary>Total trigger fires this battle for this passive (used by NthEventCondition).</summary>
        private int _fireCount;

        #endregion

        #region Properties
        public string Name => _name;
        public PassiveTriggerBase Trigger => _trigger;
        public IReadOnlyList<PassiveConditionBase> Conditions => _conditions;
        public bool OneShot => _oneShot;
        public IReadOnlyList<BattleEffect> Effects => _effects;

        #endregion

        #region Core Dispatch
        /// <summary>
        /// Attempts to fire this passive against the current event and runtime state.
        /// </summary>
        /// <param name="evtCtx">The event that was dispatched.</param>
        /// <param name="evalCtx">Snapshot of runtime state for condition evaluation.</param>
        /// <param name="execCtx">Execution context used by BattleEffect.Execute.</param>
        /// <returns>True if the passive fired; false if the trigger did not match, the
        /// one-shot guard was active, or a condition failed.</returns>
        public bool TryFire(
            PassiveEventContext evtCtx,
            PassiveEvaluationContext evalCtx,
            EffectExecutionContext execCtx
        )
        {
            if (_trigger == null)
                return false;
            if (!_trigger.Matches(evtCtx))
                return false;
            if (_oneShot && Spent)
                return false;

            // Update fire count and expose it to conditions (e.g. NthEvent)
            _fireCount++;
            evalCtx.TriggerFireCount = _fireCount;

            // All conditions must pass
            foreach (var cond in _conditions)
                if (cond != null && !cond.Evaluate(evalCtx))
                    return false;

            // Commit one-shot guard before running effects to prevent re-entry
            if (_oneShot)
                Spent = true;

            foreach (var effect in _effects)
                effect?.Execute(execCtx);

            GameLogger.LogInfo<BattlePassive>(
                $"[Passive: {_name}] fired (fire #{_fireCount}{(_oneShot ? ", one-shot" : "")})"
            );

            return true;
        }

        /// <summary>
        /// Resets runtime state at the start of a battle so the passive is ready to fire.
        /// </summary>
        public void ResetForBattle()
        {
            Spent = false;
            _fireCount = 0;
        }

        /// <summary>Returns a human-readable description for UI display.</summary>
        public string GetDescription()
        {
            var sb = new StringBuilder();

            sb.Append(_trigger != null ? _trigger.TriggerLabel : "No trigger");

            if (_conditions != null && _conditions.Count > 0)
            {
                sb.Append(" if ");
                for (int i = 0; i < _conditions.Count; i++)
                {
                    if (i > 0)
                        sb.Append(" and ");
                    sb.Append(_conditions[i]?.ConditionLabel ?? "?");
                }
            }

            sb.Append(": ");

            if (_effects != null && _effects.Count > 0)
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    if (i > 0)
                        sb.Append(". ");
                    sb.Append(_effects[i]?.GetDescription() ?? "?");
                }
            }
            else
            {
                sb.Append("(no effects)");
            }

            if (_oneShot)
                sb.Append(" (once per battle)");

            return sb.ToString();
        }
        #endregion
    }
}
