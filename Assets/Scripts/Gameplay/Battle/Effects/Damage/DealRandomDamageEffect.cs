using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Deals a random amount of Resolve damage (rolled between min and max inclusive).
    /// The Confused status effect's <c>amountOverride</c> is intentionally ignored here —
    /// Confused randomises authored fixed amounts, not pre-authored random ranges.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class DealRandomDamageEffect : BattleEffect
    {
        [Tooltip("Who receives the damage.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        [MinValue(1)]
        [Tooltip("Minimum damage (inclusive).")]
        [SerializeField]
        private int _minDamage = 3;

        [MinValue(1)]
        [Tooltip("Maximum damage (inclusive).")]
        [SerializeField]
        private int _maxDamage = 8;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            // amountOverride ignored intentionally — Confused randomises authored amounts only
            int rolled = RandomHelper.Range(_minDamage, _maxDamage + 1);
            foreach (var (target, _) in ctx.GetTargets(_target))
                ApplyResolveDamage(target, ctx.Caster, rolled, ctx);
        }

        public override string GetDescription() =>
            $"Raise Opinion by {_minDamage}–{_maxDamage} (through {_target}'s composure)";
    }
}
