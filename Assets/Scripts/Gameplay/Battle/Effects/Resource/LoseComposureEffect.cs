using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Removes Support from the session shield by the given amount.</summary>
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
            if (ctx.BattleManager == null)
                return;
            int actual = Mathf.Min(amount, ctx.BattleManager.CurrentSupport);
            if (actual > 0)
                ctx.BattleManager.AbsorbThroughSupport(actual);
            GameLogger.LogInfo<LoseShieldEffect>($"Lost {actual} Support");
        }

        public override string GetDescription() => $"Lose {_amount} Support";
    }
}
