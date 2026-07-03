using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Removes Action Points from the caster this turn (the mirror of
    /// <see cref="GainActionPointsEffect"/>). Used by AP-drain junk like Emptiness.
    /// BattleStats clamps at 0, so over-draining just empties the pool.
    /// </summary>
    [Serializable]
    public class LoseActionPointsEffect : BattleEffect
    {
        [Tooltip("AP to remove. Clamped at 0 by BattleStats — can't go negative.")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            if (amount <= 0)
                return;
            ctx.Caster.GainActionPoints(-amount);
            GameLogger.LogInfo<LoseActionPointsEffect>($"Lost {amount} Action Point(s)");
        }

        public override string GetDescription() => $"Lose {_amount} Action Point(s)";
    }
}
