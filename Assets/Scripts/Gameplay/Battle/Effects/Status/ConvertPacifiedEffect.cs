using System;
using Crookedile.Data;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Explicitly attempts the Faith Leader pacify conversion on the chosen targets —
    /// the authored replacement for the old hidden auto-convert that fired whenever a
    /// pacify status landed. Author this on a card (e.g. a Policy "Cast Out"): if the
    /// target's Guilt+Shame+Doubt stacks meet the threshold (3 + its Jaded stacks), the
    /// stacks are consumed and the enemy converts (Fanatic burst → reverts to neutral,
    /// permanent Jaded +1; Hardened enemies are Silenced instead). Below the threshold
    /// the play is a no-op on that target.
    /// </summary>
    [Serializable]
    public class ConvertPacifiedEffect : BattleEffect
    {
        [Tooltip("Which enemies to attempt the conversion on.")]
        [SerializeField]
        private TargetType _target = TargetType.Opponent;

        public override TargetType Target => _target;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (ctx.BattleManager == null)
            {
                GameLogger.LogWarning<ConvertPacifiedEffect>("No BattleManager in context — no-op");
                return;
            }

            foreach (var (targetStats, statusMgr) in ctx.GetTargets(_target))
            {
                if (targetStats == null || statusMgr == null || targetStats == ctx.PlayerStats)
                    continue;

                ctx.BattleManager.TryPacifyConvert(targetStats, statusMgr);
            }
        }

        public override string GetDescription()
        {
            string targetStr = _target == TargetType.Opponent ? "an enemy" : $"{_target}";
            return $"Convert {targetStr} with enough Guilt/Shame/Doubt (3 + Jaded) — "
                + "consume the stacks for an opinion burst.";
        }
    }
}
