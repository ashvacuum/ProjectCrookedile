using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Player-side signed Opinion change — the converged replacement for
    /// RaiseOpinionEffect (gain crowd) and LowerOwnOpinionEffect (self-cost).
    ///
    /// Positive = win the crowd over (raise the meter, routed like the player's pressure).
    /// Negative = lose your own standing (gambles, Scandal fallout); routes through Support
    ///            unless Bypass Support is set (true unavoidable cost).
    ///
    /// Player cards only. Enemies change opinion via the caster-directional ApplyOpinionEffect,
    /// which is intentionally NOT sign-based (its direction comes from who casts it).
    /// </summary>
    [Serializable]
    public class ChangeOpinionEffect : BattleEffect
    {
        [Tooltip(
            "Signed Opinion change. Positive = gain crowd; negative = lose your own standing. "
                + "Ignored when Amount Source is not Fixed."
        )]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [SerializeField]
        private int _amount = 5;

        [Tooltip("Where to read the amount from at runtime.")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip("Optional scaling: multiply the amount by this context value. None = no scaling.")]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        [Tooltip(
            "Losses only: if true the loss skips the Support shield (unavoidable cost). "
                + "Ignored for gains."
        )]
        [SerializeField]
        private bool _bypassSupport = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard)
            {
                GameLogger.LogWarning<ChangeOpinionEffect>(
                    "Authored on an enemy move — no-op (enemies use ApplyOpinionEffect)"
                );
                return;
            }

            int amount = ResolveScaledAmount(
                ctx,
                amountOverride,
                _amount,
                _amountSource,
                _perXSource,
                _multiplier
            );
            if (amount == 0)
                return;

            if (amount > 0)
            {
                // Win the crowd — same routing as the old RaiseOpinionEffect.
                ApplyOpinion(ctx.Target, ctx.Caster, amount, ctx);
                GameLogger.LogInfo<ChangeOpinionEffect>($"Raised Opinion by {amount}");
                return;
            }

            // Self-cost — same routing as the old LowerOwnOpinionEffect.
            var ledger = ctx.BattleManager?.Opinion;
            if (ledger == null)
                return; // test harness — no meter to move
            int loss = -amount;
            if (_bypassSupport)
                ledger.DecayOpinion(loss);
            else
                ledger.ApplyOpinionShift(
                    loss,
                    toPlayer: true,
                    attackerName: ctx.AttackerName ?? "Self",
                    sourceEnemyIndex: -1,
                    targetEnemyIndex: -1
                );
            GameLogger.LogInfo<ChangeOpinionEffect>(
                $"Self opinion loss: {loss} (bypassSupport={_bypassSupport})"
            );
        }

        public override string GetDescription()
        {
            string amountStr = DescribeScaledAmount(
                Mathf.Abs(_amount),
                _amountSource,
                _perXSource,
                _multiplier
            );
            if (_amount < 0)
                return $"Lose {amountStr} Opinion{(_bypassSupport ? " (cannot be absorbed)" : "")}";
            return $"Raise {amountStr} Opinion";
        }
    }
}
