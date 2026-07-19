using System;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Shifts the shared Opinion Meter by a random amount (rolled between min and max inclusive).
    /// Direction is determined by <see cref="EffectExecutionContext.IsPlayerCard"/> — no target field needed.
    /// The Confused status effect's <c>amountOverride</c> is intentionally ignored here —
    /// Confused randomises authored fixed amounts, not pre-authored random ranges.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        null,
        "ApplyRandomPressureEffect"
    )]
    public class ApplyRandomOpinionEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Minimum Opinion shift (inclusive).")]
        [SerializeField]
        private int _minDamage = 3;

        [MinValue(1)]
        [Tooltip("Maximum Opinion shift (inclusive).")]
        [SerializeField]
        private int _maxDamage = 8;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            // amountOverride ignored intentionally — Confused randomises authored amounts only
            int rolled = RandomHelper.Range(_minDamage, _maxDamage + 1);
            BattleStats opinionTarget = ctx.IsPlayerCard ? ctx.Target : ctx.PlayerStats;
            ApplyOpinion(opinionTarget, ctx.Caster, rolled, ctx);
        }

        public override DamagePreview? GetDamagePreview() =>
            new DamagePreview
            {
                Type = DamagePreviewType.Random,
                MinAmount = _minDamage,
                MaxAmount = _maxDamage,
            };

        public override string GetDescription() =>
            $"Shift Opinion by {_minDamage}–{_maxDamage}";
    }
}
