using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Faith Leader's Fanatics follow your lead (docs/core-design.md section 7). BattleManager
    /// calls these directly, in order: <see cref="ResetTurnTally"/> at player turn start,
    /// <see cref="OnCardResolved"/> after each played card resolves, and
    /// <see cref="OnPlayerTurnEnd"/> before statuses tick. A card that pushed the meter makes
    /// every Fanatic push; a card that gained Support makes every Fanatic add Support. A turn with
    /// neither, and enough conversions, ends with each Fanatic Silencing an enemy. A Silenced or
    /// Stunned Fanatic doesn't follow. A Fanatic whose conversion consumed pacify stacks past the
    /// threshold follows harder (over-stacking), and a Fanatic's Strength adds to its push.
    /// Numbers come from <see cref="FaithLeaderConfig"/>.
    /// </summary>
    [Debuggable("PacifyConversion", LogLevel.Info)]
    public class FanaticFollowers
    {
        private readonly OpinionLedger _opinion;
        private readonly IReadOnlyList<EnemyController> _enemies;

        // Pacify stacks each converted enemy's conversion consumed past the threshold.
        private readonly Dictionary<BattleStats, int> _stacksPastThreshold = new();

        public FanaticFollowers(OpinionLedger opinion, IReadOnlyList<EnemyController> enemies)
        {
            _opinion = opinion;
            _enemies = enemies;
        }

        /// <summary>Cards played this player turn that pushed the meter.</summary>
        public int AttacksThisTurn { get; private set; }

        /// <summary>Cards played this player turn that gained Support.</summary>
        public int DefendsThisTurn { get; private set; }

        public void ResetTurnTally()
        {
            AttacksThisTurn = 0;
            DefendsThisTurn = 0;
            // ponytail: a converted enemy keeps its over-stack until the turn after it stops
            // being a Fanatic, so one re-made Fanatic the same turn inherits it.
            foreach (
                var stats in _stacksPastThreshold
                    .Keys.Where(s =>
                        !_enemies.Any(e => e.Stats == s && e.StatusEffects.HasStatus<FanaticStatus>())
                    )
                    .ToList()
            )
                _stacksPastThreshold.Remove(stats);
        }

        /// <summary>A conversion consumed this many pacify stacks past its threshold.</summary>
        public void RecordConversion(BattleStats enemy, int stacksPastThreshold)
        {
            if (stacksPastThreshold > 0)
                _stacksPastThreshold[enemy] = stacksPastThreshold;
            else
                _stacksPastThreshold.Remove(enemy);
        }

        /// <summary>
        /// Fanatics copy a played card. ponytail: attack/defend is read off what the card did
        /// (pushed the meter / gained Support), not its type; the doc leaves that call open.
        /// </summary>
        public void OnCardResolved(EffectExecutionContext ctx)
        {
            if (ctx == null)
                return;
            bool attack = ctx.LastDamageDealt > 0;
            bool defend = ctx.LastSupportGained > 0;
            if (attack)
                AttacksThisTurn++;
            if (defend)
                DefendsThisTurn++;
            Follow(attack, defend);
        }

        /// <summary>
        /// Every following Fanatic pushes (<paramref name="attack"/>) and/or adds Support
        /// (<paramref name="defend"/>). Command cards call this directly; it doesn't count as you
        /// playing an attack or a defend.
        /// </summary>
        public void Follow(bool attack, bool defend)
        {
            if (!attack && !defend)
                return;

            var config = FaithLeaderConfig.Current;
            int followers = 0;
            foreach (var fanatic in Followers())
            {
                followers++;
                _stacksPastThreshold.TryGetValue(fanatic.Stats, out int extra);
                int bonus = extra * config.FollowBonusPerExtraStack;
                // The push is the Fanatic's own pressure, so its outgoing statuses (Strength,
                // Weakened) apply to it.
                if (attack)
                    _opinion.ApplyOpinionShift(
                        fanatic.StatusEffects.ModifyDamageDealt(config.FollowSwayPerFanatic + bonus),
                        toPlayer: false,
                        attackerName: "Fanatic",
                        sourceEnemyIndex: -1,
                        targetEnemyIndex: -1
                    );
                if (defend)
                    _opinion.GainSupport(config.FollowSupportPerFanatic + bonus);
            }

            if (followers > 0)
                GameLogger.LogInfo<FanaticFollowers>(
                    $"{followers} Fanatic(s) followed"
                        + $"{(attack ? " — push" : "")}{(defend ? " — Support" : "")}"
                );
        }

        /// <summary>
        /// End of your turn: with no attack and no defend played and enough conversions, each
        /// Fanatic Silences a different non-Fanatic enemy, most hostile first.
        /// </summary>
        public void OnPlayerTurnEnd(int conversionsThisTurn)
        {
            var config = FaithLeaderConfig.Current;
            if (
                AttacksThisTurn > 0
                || DefendsThisTurn > 0
                || conversionsThisTurn < config.SilenceMinConversions
            )
                return;

            var targets = _enemies
                .Where(e => !e.IsDefeated && !e.StatusEffects.HasStatus<FanaticStatus>())
                .OrderByDescending(e => e.Stats.CurrentHostility)
                .ToList();
            int silenced = 0;
            foreach (var _ in Followers())
            {
                if (silenced >= targets.Count)
                    break;
                targets[silenced++]
                    .StatusEffects.ApplyStatus(
                        StatusRegistry.Get<SilencedStatus>(),
                        config.SilenceStacks,
                        StatusDurationType.DecreasePerTurn
                    );
            }

            if (silenced > 0)
                GameLogger.LogInfo<FanaticFollowers>(
                    $"Fanatics shouted the room down — {silenced} enemy(s) Silenced"
                );
        }

        private IEnumerable<EnemyController> Followers() =>
            _enemies.Where(e =>
                !e.IsDefeated
                && e.StatusEffects.HasStatus<FanaticStatus>()
                && !e.StatusEffects.HasStatus<SilencedStatus>()
                && !e.StatusEffects.HasStatus<StunnedStatus>()
            );
    }
}
