using System;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Lowers the player's OWN opinion — the self-cost half of risk/reward cards
    /// (gambles, desperate plays, Scandal fallout). Player cards only; an enemy
    /// authoring this is a no-op (enemies lower opinion via normal pressure).
    ///
    /// Routing: by default the loss goes through Support like an attack (shields can
    /// soak the cost); enable Bypass Support for a true unavoidable cost.
    /// </summary>
    [Serializable]
    public class LowerOwnOpinionEffect : BattleEffect
    {
        [Tooltip("Base opinion to lose. Ignored when Amount Source is not Fixed.")]
        [ShowIf("@_amountSource == EffectContextValue.FixedAmount")]
        [MinValue(1)]
        [SerializeField]
        private int _amount = 3;

        [Tooltip("Where to read the amount from at runtime.")]
        [SerializeField]
        private EffectContextValue _amountSource = EffectContextValue.FixedAmount;

        [Tooltip(
            "Optional scaling: multiply the amount by this context value. None = no scaling."
        )]
        [SerializeField]
        private EffectContextValue _perXSource = EffectContextValue.None;

        [Tooltip("Optional flat multiplier applied last. Values <= 0 are treated as 1.")]
        [SerializeField]
        private float _multiplier = 1f;

        [Tooltip(
            "If true the loss skips the Support shield entirely (unavoidable cost). "
                + "If false it routes through Support like an enemy attack."
        )]
        [SerializeField]
        private bool _bypassSupport = false;

        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            if (!ctx.IsPlayerCard)
            {
                GameLogger.LogWarning<LowerOwnOpinionEffect>(
                    "Authored on an enemy move — no-op (use ApplyPressureEffect instead)"
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
            if (amount <= 0)
                return;

            var ledger = ctx.BattleManager?.Opinion;
            if (ledger == null)
                return; // test harness — no meter to move

            if (_bypassSupport)
                ledger.DecayOpinion(amount);
            else
                ledger.ApplyPressure(
                    amount,
                    toPlayer: true,
                    attackerName: ctx.AttackerName ?? "Self",
                    sourceEnemyIndex: -1,
                    targetEnemyIndex: -1
                );

            GameLogger.LogInfo<LowerOwnOpinionEffect>(
                $"Self opinion loss: {amount} (bypassSupport={_bypassSupport})"
            );
        }

        public override string GetDescription()
        {
            string amountStr = DescribeScaledAmount(_amount, _amountSource, _perXSource, _multiplier);
            string suffix = _bypassSupport ? " (cannot be absorbed)" : "";
            return $"Lose {amountStr} Opinion{suffix}";
        }
    }
}
