using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Utilities;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Faith Leader conversion engine, extracted from BattleManager. Conversion is
    /// CARD-DRIVEN: <see cref="ConvertPacifiedEffect"/> calls <see cref="TryConvert"/> when an
    /// explicit conversion card is played (the old auto-convert on pacify-status application
    /// was removed by design). When the target's total pacify stacks reach
    /// <c>3 + its Jaded stacks</c>, the pacify statuses are consumed and the enemy converts:
    /// <list type="bullet">
    ///   <item>Hardened enemy — can't be converted; silenced instead (no burst, no Jaded).</item>
    ///   <item>Any other enemy — a one-turn Fanatic burst pumps the meter, then it reverts to
    ///   neutral and gains a permanent Jaded stack (raising its next conversion cost).</item>
    /// </list>
    /// </summary>
    [Debuggable("PacifyConversion", LogLevel.Info)]
    public class PacifyConversionEngine
    {
        // Base pacify threshold before Jaded; each Jaded stack on the enemy raises it by 1.
        private const int BaseThreshold = 3;

        // Opinion pumped into the meter per pacify stack consumed on conversion (generous by
        // design; over-stacking past the threshold yields a proportionally bigger burst).
        private const int BurstPerStack = 3;

        private readonly OpinionLedger _opinion;
        private readonly BattleStats _playerStats;

        public PacifyConversionEngine(OpinionLedger opinion, BattleStats playerStats)
        {
            _opinion = opinion;
            _playerStats = playerStats;
        }

        /// <summary>
        /// Pacify conversions made during the current player turn — read by Sermon-style
        /// harvest cards via <c>EffectContextValue.ConversionsThisTurn</c>.
        /// </summary>
        public int ConversionsThisTurn { get; private set; }

        /// <summary>Resets the per-turn conversion tally. Call at the start of each player turn.</summary>
        public void ResetTurnTally() => ConversionsThisTurn = 0;

        /// <summary>The pacify statuses that count toward (and are consumed by) a conversion.</summary>
        public static bool IsPacifyStatus(StatusBehavior behavior) =>
            behavior != null && behavior.CountsTowardPacify;

        /// <summary>The default conversion fuel: the Guilt/Shame/Doubt trio.</summary>
        private static readonly StatusBehavior[] DefaultPacifyStatuses =
        {
            StatusRegistry.Get<GuiltStatus>(),
            StatusRegistry.Get<ShameStatus>(),
            StatusRegistry.Get<DoubtStatus>(),
        };

        /// <summary>
        /// Runs the conversion check. No-op when the threshold isn't met. Player target is ignored.
        /// </summary>
        /// <param name="statuses">
        /// Which statuses count toward (and are consumed by) this conversion. Null/empty =
        /// the default pacify trio (Guilt/Shame/Doubt).
        /// </param>
        /// <summary>What a conversion attempt did — the caller applies awards on Converted.</summary>
        public enum Outcome
        {
            NotReady = 0, // threshold unmet — nothing happened
            Silenced = 1, // Hardened target: fuel consumed, silenced, no awards
            Converted = 2, // full conversion: burst fired, reverted to neutral
        }

        /// <param name="bufferStatus">
        /// The escalator status: the threshold is 3 + the target's current stacks of it.
        /// Null = the default Jaded. The CALLER is responsible for awarding it on success —
        /// the engine only reads it.
        /// </param>
        /// <param name="consumed">Fuel stacks actually consumed (0 on NotReady).</param>
        public Outcome TryConvert(
            BattleStats enemyStats,
            StatusEffectManager mgr,
            IReadOnlyList<StatusBehavior> statuses,
            StatusBehavior bufferStatus,
            out int consumed
        )
        {
            consumed = 0;
            if (enemyStats == null || mgr == null || enemyStats == _playerStats)
                return Outcome.NotReady;

            if (statuses == null || statuses.Count == 0)
                statuses = DefaultPacifyStatuses;
            if (bufferStatus == null)
                bufferStatus = StatusRegistry.Get<JadedStatus>();

            int total = 0;
            foreach (var status in statuses)
                if (status != null)
                    total += mgr.GetStacks(status);

            // The buffer status is the escalator: its stacks raise this conversion's cost.
            int threshold = BaseThreshold + mgr.GetStacks(bufferStatus);
            if (total < threshold)
                return Outcome.NotReady;

            int idx = enemyStats.OwnerEnemyIndex;

            // Consume ALL selected fuel (authored order) — spent whether we convert or
            // (Hardened) silence.
            foreach (var status in statuses)
            {
                if (status == null)
                    continue;
                int stacks = mgr.GetStacks(status);
                if (stacks <= 0)
                    continue;
                mgr.RemoveStacksNotify(status, stacks);
                consumed += stacks;
            }

            // A true non-believer can't be converted — shut them up instead.
            if (enemyStats.IsHardened)
            {
                mgr.ApplyStatus(
                    StatusRegistry.Get<SilencedStatus>(),
                    1,
                    StatusDurationType.DecreasePerTurn
                );
                GameLogger.LogInfo<PacifyConversionEngine>(
                    $"Enemy [{idx}] is Hardened — pacify failed, silenced instead"
                );
                EventBus.Publish(
                    new EnemyConvertedEvent
                    {
                        EnemyIndex = idx,
                        OpinionBurst = 0,
                        WasSilenced = true,
                    }
                );
                return Outcome.Silenced;
            }

            // Convert: one-turn Fanatic burst pumping the meter (generous, scales with stacks eaten).
            int burst = consumed * BurstPerStack;
            _opinion.RaiseDirect(burst);

            // Revert to neutral. Awards (buffer/Jaded, Fanatic, ...) are the CALLER's job —
            // ConvertPacifiedEffect applies its authored award list on Outcome.Converted.
            enemyStats.SetHostility(0);

            ConversionsThisTurn++;

            GameLogger.LogInfo<PacifyConversionEngine>(
                $"Enemy [{idx}] converted — {consumed} pacify stacks → {burst} opinion burst"
            );
            EventBus.Publish(
                new EnemyConvertedEvent
                {
                    EnemyIndex = idx,
                    OpinionBurst = burst,
                    WasSilenced = false,
                }
            );
            return Outcome.Converted;
        }
    }
}
