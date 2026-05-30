using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Grants Action Points to the caster immediately this turn.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class GainActionPointsEffect : BattleEffect
    {
        [SerializeField] private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            ctx.Caster.GainActionPoints(amount);
            GameLogger.LogInfo<GainActionPointsEffect>($"Gained {amount} Action Points");
        }

        public override string GetDescription() => $"Gain {_amount} Action Point(s)";
    }
}
