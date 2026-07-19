using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Removes Support by the given amount.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(
        true,
        "Crookedile.Gameplay.Battle",
        null,
        "LoseBufferEffect"
    )]
    public class LoseSupportEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            if (ctx.BattleManager == null)
                return;
            int actual = ctx.BattleManager.SpendSupport(amount);
            GameLogger.LogInfo<LoseSupportEffect>($"Lost {actual} Support");
        }

        public override string GetDescription() => $"Lose {_amount} Support";
    }
}
