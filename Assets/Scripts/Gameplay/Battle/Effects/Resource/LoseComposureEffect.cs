using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Removes Composure from the caster.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class LoseComposureEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            int actual = ApplyLoseComposure(ctx.Caster, amount, ctx);
            GameLogger.LogInfo<LoseComposureEffect>($"Lost {actual} Composure");
        }

        public override string GetDescription() => $"Lose {_amount} Composure";
    }
}
