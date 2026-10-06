using System;
using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Hostility by an amount that grows on the Fibonacci scale (1, 1, 2, 3, 5…) each
    /// time this battle that the same escalation is used (Dynasty's third plays). Escalations
    /// sharing a key share one counter.
    /// </summary>
    [Serializable]
    public class EscalatingHostilityEffect : BattleEffect
    {
        [Tooltip("Counter name: every effect with the same key escalates together this battle.")]
        [SerializeField]
        private string _escalationKey = "dynasty";

        [MinValue(1)]
        [Tooltip("Multiplies the scale: Hostility = Base × Fib(uses this battle).")]
        [SerializeField]
        private int _baseAmount = 1;

        [Tooltip("Whose Hostility rises.")]
        [SerializeField]
        private TargetType _target = TargetType.AllOpponents;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            var state = ctx.BattleManager?.NepoBaby;
            int uses = state != null ? state.NextEscalation(_escalationKey) : 1;
            int amount = _baseAmount * NepoBabyRules.Fibonacci(uses);
            foreach (var (stats, _) in ctx.GetTargets(_target))
                ctx.LastHostilityGained += stats.GainHostility(amount);
        }

        public override string GetDescription()
        {
            string who = _target == TargetType.AllOpponents ? "all enemies" : _target.ToString();
            return $"Aggravate {who}, more each time this battle (1, 1, 2, 3, 5…)";
        }
    }
}
