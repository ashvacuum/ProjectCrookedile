using System;
using Crookedile.Core;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Grants Action Points to the caster at the start of their next turn.</summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class GainActionPointsNextTurnEffect : BattleEffect
    {
        [SerializeField]
        private int _amount = 1;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            ctx.Caster.GainActionPointsNextTurn(amount);
            GameLogger.LogInfo<GainActionPointsNextTurnEffect>($"Will gain {amount} AP next turn");
        }

        public override string GetDescription() => $"Gain {_amount} Action Point(s) next turn";
    }
}
