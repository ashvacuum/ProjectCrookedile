using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class OverpromiseEffect : BattleEffect
    {
        [Tooltip(
            "Sway multiplier for the next card this turn that deals Sway; cards without Sway don't spend it."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _swayMultiplier = 3;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            ctx.BattleManager.Celebrity.NextSwayMultiplier = _swayMultiplier;
        }

        public override string GetDescription() =>
            $"Your next Sway card this turn deals {_swayMultiplier} times its Sway";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_swayMultiplier <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
