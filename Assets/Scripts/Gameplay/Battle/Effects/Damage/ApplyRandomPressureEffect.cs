using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Applies a random amount of pressure to the shared Opinion Meter (rolled between min and max inclusive).
    /// Direction is determined by <see cref="EffectExecutionContext.IsPlayerCard"/> — no target field needed.
    /// The Confused status effect's <c>amountOverride</c> is intentionally ignored here —
    /// Confused randomises authored fixed amounts, not pre-authored random ranges.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class ApplyRandomPressureEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Minimum pressure (inclusive).")]
        [SerializeField]
        private int _minDamage = 3;

        [MinValue(1)]
        [Tooltip("Maximum pressure (inclusive).")]
        [SerializeField]
        private int _maxDamage = 8;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            // amountOverride ignored intentionally — Confused randomises authored amounts only
            int rolled = RandomHelper.Range(_minDamage, _maxDamage + 1);
            BattleStats pressureTarget = ctx.IsPlayerCard ? ctx.Target : ctx.PlayerStats;
            ApplyPressure(pressureTarget, ctx.Caster, rolled, ctx);
        }

        public override DamagePreview? GetDamagePreview() =>
            new DamagePreview
            {
                Type = DamagePreviewType.Random,
                MinAmount = _minDamage,
                MaxAmount = _maxDamage,
            };

        public override string GetDescription() =>
            $"Apply {_minDamage}–{_maxDamage} pressure to the Opinion Meter";
    }
}
