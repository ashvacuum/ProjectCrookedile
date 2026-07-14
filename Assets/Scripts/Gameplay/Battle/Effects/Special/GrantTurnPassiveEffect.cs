using System;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Grants a fully authored <see cref="BattlePassive"/> until the end of THIS player turn —
    /// "Whenever an enemy becomes Receptive this turn, gain 3 Support" style cards. The nested
    /// passive uses the whole toolbox (any trigger, conditions, TriggeringEnemy targeting,
    /// amount sources); it simply expires at turn end instead of lasting the battle.
    /// </summary>
    [Serializable]
    public class GrantTurnPassiveEffect : BattleEffect
    {
        [Tooltip("The passive to run until end of turn: trigger + conditions + effects.")]
        [SerializeField]
        private BattlePassive _passive = new BattlePassive();

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (_passive == null || _passive.Trigger == null)
            {
                GameLogger.LogWarning<GrantTurnPassiveEffect>(
                    "No passive/trigger authored — no-op"
                );
                return;
            }
            if (ctx.BattleManager?.Passives == null)
                return;

            ctx.BattleManager.Passives.ActivateTemporaryPassive(_passive, ctx.OwnerCard);
        }

        public override string GetDescription() =>
            _passive != null && _passive.Trigger != null
                ? $"This turn: {_passive.GetDescription()}"
                : "This turn: (no passive authored)";
    }
}
