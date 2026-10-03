using System;
using Crookedile.Data.Enemy;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Brings a body into the row: spawns one or more copies of an <see cref="EnemyData"/> at a
    /// chosen starting mood (negative hostility = receptive, positive = hostile). No player card
    /// uses it — player-side summoning is cut — and it is kept for summon-style content to come.
    /// </summary>
    [Serializable]
    public class SummonBodyEffect : BattleEffect
    {
        [Required]
        [Tooltip("Which body to summon into the row.")]
        [SerializeField]
        private EnemyData _bodyToSummon;

        [MinValue(1)]
        [Tooltip("How many copies to summon (capped by the 5-enemy row limit).")]
        [SerializeField]
        private int _count = 1;

        [Tooltip(
            "Starting hostility for the summoned body. Negative = a receptive ally; "
                + "positive = a hostile Plant; 0 = neutral."
        )]
        [SerializeField]
        private int _initialHostility = -3;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_bodyToSummon == null)
            {
                GameLogger.LogWarning<SummonBodyEffect>("No body assigned — nothing summoned");
                return;
            }

            ctx.BattleManager?.SummonMinions(_bodyToSummon, _count, _initialHostility);
        }

        public override string GetDescription()
        {
            string mood =
                _initialHostility < 0 ? "receptive ally"
                : _initialHostility > 0 ? "hostile body"
                : "neutral body";
            string body = _bodyToSummon != null ? _bodyToSummon.EnemyName : "a body";
            return _count == 1
                ? $"Summon {body} as a {mood}"
                : $"Summon {_count}x {body} as {mood}s";
        }
    }
}
