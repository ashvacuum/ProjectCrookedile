using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Enemy moves: the player gains Support (a receptive enemy speaking up for you). Unlike
    /// <see cref="GainSupportEffect"/>, an enemy caster never turns this into its own Denial.
    /// </summary>
    [Serializable]
    public class GrantPlayerSupportEffect : BattleEffect
    {
        [Tooltip("Support the player gains.")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null) =>
            ApplyGainSupport(amountOverride ?? _amount, ctx);

        public override string GetDescription() => $"You gain {_amount} Support";
    }
}
