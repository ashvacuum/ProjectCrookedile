using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Removes Support from the session shield by the given amount.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class LoseBufferEffect : BattleEffect
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
            GameLogger.LogInfo<LoseBufferEffect>($"Lost {actual} Support");
        }

        public override string GetDescription() => $"Lose {_amount} Support";
    }
}
