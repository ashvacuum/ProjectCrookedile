using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Raises Hostility on the chosen target(s), making them deal more damage.
    /// With <see cref="TargetType.RandomReceptive"/> (and enough amount to cross 0) this is a Sway —
    /// converting a receptive enemy to hostile, which can trigger the Turncoat cascade.
    /// </summary>
    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
    public class RaiseTargetHostilityEffect : BattleEffect
    {
        [MinValue(1)]
        [SerializeField]
        private int _amount = 2;

        [Tooltip(
            "Which enemies to rile. Opponent = the focused enemy; Adjacent / AllReceptive / "
                + "RandomReceptive (Sway) / AllOpponents address groups."
        )]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            int amount = amountOverride ?? _amount;
            foreach (var (stats, _) in ctx.GetTargets(_target))
                stats.GainHostility(amount);
            GameLogger.LogInfo<RaiseTargetHostilityEffect>(
                $"Raised Hostility by {amount} ({_target})"
            );
        }

        public override string GetDescription() =>
            _target == TargetType.Opponent
                ? $"Raise target's Hostility by {_amount}"
                : $"Raise Hostility by {_amount} ({_target})";
    }
}
