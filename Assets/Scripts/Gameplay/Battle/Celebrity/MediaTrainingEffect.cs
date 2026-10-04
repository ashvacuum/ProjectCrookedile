using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    [Serializable]
    public sealed class MediaTrainingEffect : BattleEffect
    {
        [Tooltip("Glamour spent to draw at the start of a turn.")]
        [MinValue(1)]
        [SerializeField]
        private int _glamourCost = 2;

        [Tooltip("Cards drawn after paying the Glamour cost.")]
        [MinValue(1)]
        [SerializeField]
        private int _drawCount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard || ctx.BattleManager == null)
                return;

            ctx.BattleManager.Celebrity.MediaTrainingCost = _glamourCost;
            ctx.BattleManager.Celebrity.MediaTrainingDraw = _drawCount;
        }

        public override string GetDescription() =>
            $"At the start of your turn, you may spend {_glamourCost} Glamour to draw {_drawCount} card(s)";

#if UNITY_EDITOR
        public override IEnumerable<string> GetConfigurationIssues()
        {
            if (_glamourCost <= 0 || _drawCount <= 0)
                yield return "Effect amounts must be positive.";
        }
#endif
    }
}
