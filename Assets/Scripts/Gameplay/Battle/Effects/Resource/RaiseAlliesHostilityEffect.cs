using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises the Hostility of every other living enemy in the room (excludes the caster by default).
    /// Intended for "RileOthers" enemy moves that agitate allies without affecting themselves.
    ///
    /// Uses <see cref="TargetType.AllAllies"/> which, for an enemy caster, resolves to all living
    /// enemies. The caster is then optionally filtered out via <see cref="_includeSelf"/>.
    /// No-op when used on a player card (players have no allies).
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseAlliesHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [Tooltip("Amount of Hostility to add to each ally.")]
        [SerializeField]
        private int _amount = 2;

        [Tooltip("If true, the casting enemy also rouses itself in addition to its allies.")]
        [SerializeField]
        private bool _includeSelf = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            int count = 0;

            foreach (var (stats, _) in ctx.GetTargets(TargetType.AllAllies))
            {
                if (!_includeSelf && stats == ctx.Caster)
                    continue;
                ctx.LastHostilityGained += stats.GainHostility(amount);
                count++;
            }

            GameLogger.LogInfo<RaiseAlliesHostilityEffect>(
                $"Riled {count} ally(ies) by {amount} Hostility"
            );
        }

        public override string GetDescription() =>
            _includeSelf
                ? $"Raise all enemies' Hostility by {_amount}"
                : $"Rile other enemies' Hostility by {_amount}";
    }
}
