using System;
using System.Collections.Generic;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Queues its nested effects to resolve at the start of a FUTURE player turn — the generic
    /// "takes effect next turn" wrapper for any card type (the activated-passive trick covers
    /// Policies; this covers Rhetoric/Pressure and composition inside other effects).
    ///
    /// The nested effects resolve with a fresh context at fire time: single-target entries hit
    /// whoever is focused THEN, group targets resolve against the live board.
    /// </summary>
    [Serializable]
    public class DelayedEffect : BattleEffect
    {
        [Min(1)]
        [Tooltip("How many of your turn starts to wait. 1 = the start of your next turn.")]
        [SerializeField]
        private int _turnsDelay = 1;

        [SerializeReference]
        [Tooltip("Effects to execute when the delay elapses.")]
        [ListDrawerSettings(ShowFoldout = true)]
        [SerializeField]
        private List<BattleEffect> _effects = new List<BattleEffect>();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_effects == null || _effects.Count == 0)
                return;
            if (ctx.BattleManager == null)
            {
                GameLogger.LogWarning<DelayedEffect>("No BattleManager in context — no-op");
                return;
            }

            ctx.BattleManager.QueueDelayedEffects(_effects, _turnsDelay);
        }

        public override string GetDescription()
        {
            var parts = new List<string>();
            if (_effects != null)
                foreach (var e in _effects)
                    if (e != null)
                        parts.Add(e.GetDescription());
            string when = _turnsDelay <= 1 ? "Next turn" : $"In {_turnsDelay} turns";
            return parts.Count > 0 ? $"{when}: {string.Join(". ", parts)}" : $"{when}: (no effects)";
        }
    }
}
