using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Faith Leader conversion engine, extracted from BattleManager. Call
    /// <see cref="TryConvert"/> after a pacify status (Guilt/Shame/Doubt) lands on an enemy.
    /// When the enemy's total pacify stacks reach <c>3 + its Jaded stacks</c>, the pacify
    /// statuses are consumed and the enemy converts:
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

        /// <summary>
        /// Runs the conversion check. No-op when the threshold isn't met. Player target is ignored.
        /// </summary>
        public void TryConvert(BattleStats enemyStats, StatusEffectManager mgr)
        {
            if (enemyStats == null || mgr == null || enemyStats == _playerStats)
                return;

            int guilt = mgr.GetStacks<GuiltStatus>();
            int shame = mgr.GetStacks<ShameStatus>();
            int doubt = mgr.GetStacks<DoubtStatus>();
            int total = guilt + shame + doubt;

            int threshold = BaseThreshold + mgr.GetStacks<JadedStatus>();
            if (total < threshold)
                return;

            int idx = enemyStats.OwnerEnemyIndex;

            // Consume the pacify statuses — spent whether we convert or (Hardened) silence.
            mgr.RemoveStatusNotify(StatusRegistry.Get<GuiltStatus>());
            mgr.RemoveStatusNotify(StatusRegistry.Get<ShameStatus>());
            mgr.RemoveStatusNotify(StatusRegistry.Get<DoubtStatus>());

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
                return;
            }

            // Convert: one-turn Fanatic burst pumping the meter (generous, scales with stacks eaten).
            int burst = total * BurstPerStack;
            _opinion.RaiseDirect(burst);

            // Revert to neutral and gain a permanent Jaded stack (raises the next conversion cost).
            enemyStats.SetHostility(0);
            mgr.ApplyStatus(StatusRegistry.Get<JadedStatus>(), 1, StatusDurationType.Permanent);

            ConversionsThisTurn++;

            GameLogger.LogInfo<PacifyConversionEngine>(
                $"Enemy [{idx}] converted — {total} pacify stacks → {burst} opinion burst "
                    + $"(now Jaded {mgr.GetStacks<JadedStatus>()})"
            );
            EventBus.Publish(
                new EnemyConvertedEvent
                {
                    EnemyIndex = idx,
                    OpinionBurst = burst,
                    WasSilenced = false,
                }
            );
        }
    }
}
