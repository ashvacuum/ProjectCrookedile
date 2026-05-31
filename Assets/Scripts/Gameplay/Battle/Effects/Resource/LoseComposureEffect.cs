using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Removes Shield from the caster.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        "Assembly-CSharp",
        "LoseComposureEffect"
    )]
    public class LoseShieldEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            int actual = ApplyLoseShield(ctx.Caster, amount, ctx);
            GameLogger.LogInfo<LoseShieldEffect>($"Lost {actual} Shield");
        }

        public override string GetDescription() => $"Lose {_amount} Support";
    }
}
