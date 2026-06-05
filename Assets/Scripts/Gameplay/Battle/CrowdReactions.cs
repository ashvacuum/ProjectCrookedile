using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Owns how the room (the enemy crowd) reacts during a battle:
    ///   • hostility shifts from played cards (policy lean + singling someone out),
    ///   • the Echo Chamber rule (all-receptive → halved gains + decay),
    ///   • the Turncoat cascade (a receptive enemy flipping hostile).
    ///
    /// BattleManager owns the battle <i>flow</i> and decides <i>when</i> each of these happens;
    /// this class owns the <i>behavior</i>. It holds no turn/FSM logic. The enemy list is the same
    /// live reference BattleManager holds, so summoned enemies are seen automatically.
    /// </summary>
    [Debuggable("CrowdReactions", LogLevel.Info)]
    public class CrowdReactions : IDisposable
    {
        private readonly IReadOnlyList<EnemyController> _enemies;
        private OpinionLedger _opinion; // attached after construction (ledger needs IsEchoChamber)

        // Tunables, passed from BattleManager's serialized fields.
        private readonly int _echoChamberDecayPerTurn;
        private readonly int _turncoatStacks;
        private readonly int _turncoatOpinionHit;
        private readonly int _turncoatAdjacentNudge;

        private bool _echoChamberActive;
        private bool _resolvingTurncoat;

        public CrowdReactions(
            IReadOnlyList<EnemyController> enemies,
            int echoChamberDecayPerTurn,
            int turncoatStacks,
            int turncoatOpinionHit,
            int turncoatAdjacentNudge
        )
        {
            _enemies = enemies;
            _echoChamberDecayPerTurn = echoChamberDecayPerTurn;
            _turncoatStacks = turncoatStacks;
            _turncoatOpinionHit = turncoatOpinionHit;
            _turncoatAdjacentNudge = turncoatAdjacentNudge;

            EventBus.Subscribe<EnemyTurncoatEvent>(OnEnemyTurncoat);
        }

        /// <summary>
        /// Supplies the opinion ledger. Called by BattleManager after the ledger is built (the ledger
        /// is constructed with <see cref="IsEchoChamber"/> as its halving predicate, so it can't be
        /// passed into the constructor).
        /// </summary>
        public void AttachLedger(OpinionLedger opinion) => _opinion = opinion;

        public void Dispose() => EventBus.Unsubscribe<EnemyTurncoatEvent>(OnEnemyTurncoat);

        private IEnumerable<EnemyController> LivingEnemies => _enemies.Where(e => !e.IsDefeated);

        #region Echo Chamber

        /// <summary>
        /// True when the room is an echo chamber: at least one enemy is present and EVERY living
        /// enemy is receptive (hostility &lt; 0). A single neutral or hostile enemy breaks it.
        /// </summary>
        public bool IsEchoChamber()
        {
            bool anyLiving = false;
            foreach (var enemy in LivingEnemies)
            {
                anyLiving = true;
                if (!enemy.Stats.IsReceptive)
                    return false;
            }
            return anyLiving;
        }

        /// <summary>Recomputes echo-chamber state and publishes a transition event only when it changes.</summary>
        public void RefreshEchoChamberState()
        {
            bool now = IsEchoChamber();
            if (now == _echoChamberActive)
                return;
            _echoChamberActive = now;
            EventBus.Publish(new EchoChamberChangedEvent { Active = now });
            GameLogger.LogInfo<CrowdReactions>(
                now
                    ? "Echo chamber formed — opinion gains halved, meter will decay."
                    : "Echo chamber broken."
            );
        }

        /// <summary>Bleeds opinion at player turn end while the room is an echo chamber.</summary>
        public void ApplyTurnEndDecay()
        {
            if (_echoChamberDecayPerTurn > 0 && IsEchoChamber())
            {
                _opinion?.DecayOpinion(_echoChamberDecayPerTurn);
                GameLogger.LogInfo<CrowdReactions>(
                    $"Echo chamber decay: -{_echoChamberDecayPerTurn} opinion"
                );
            }
        }

        #endregion

        #region Card reactions

        /// <summary>
        /// Applies the room's reaction to a played card: policy-lean hostility shifts across the row,
        /// the single-target "singling someone out" hostility bump, and an echo-chamber refresh.
        /// </summary>
        public void OnCardPlayed(CardData card, EnemyController focusedEnemy, int focusedIndex)
        {
            ApplyPolicyHostilityShifts(card);
            ApplySingleTargetHostilityRaise(card, focusedEnemy, focusedIndex);
            RefreshEchoChamberState();
        }

        /// <summary>
        /// If the played card is a Policy card, shifts EVERY living enemy's hostility based on how
        /// their DemographicValues aligns with the card's PolicyLean (agreement −1, disagreement +1).
        /// </summary>
        private void ApplyPolicyHostilityShifts(CardData card)
        {
            if (card.CardType != CardType.Policy)
                return;

            foreach (var enemy in LivingEnemies)
            {
                int shift = GetPolicyHostilityShift(card.PolicyLean, enemy.EnemyData.DemographicValues);
                if (shift == 0)
                    continue;

                // BattleStats publishes the indexed HostilityChangedEvent itself.
                if (shift > 0)
                    enemy.Stats.GainHostility(shift);
                else
                    enemy.Stats.ReduceHostility(-shift);
            }
        }

        /// <summary>
        /// If the played card has any effect targeting a single enemy (<see cref="TargetType.Opponent"/>),
        /// raises the focused enemy's Hostility by 1 — the escalation of singling someone out.
        /// Does not apply to AoE or self-targeting cards.
        /// </summary>
        private void ApplySingleTargetHostilityRaise(
            CardData card,
            EnemyController focusedEnemy,
            int focusedIndex
        )
        {
            bool isSingleTarget = false;
            if (card.Effects != null)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Target == TargetType.Opponent)
                    {
                        isSingleTarget = true;
                        break;
                    }
                }
            }

            if (!isSingleTarget)
                return;
            if (focusedEnemy == null || focusedEnemy.IsDefeated)
                return;

            // BattleStats publishes the indexed HostilityChangedEvent itself.
            int old = focusedEnemy.Stats.CurrentHostility;
            focusedEnemy.Stats.GainHostility(1);
            focusedEnemy.CheckBecameHostile();

            if (focusedEnemy.Stats.CurrentHostility != old)
                GameLogger.LogInfo<CrowdReactions>(
                    $"Single-target card '{card.CardName}' raised hostility on [{focusedIndex}] "
                        + $"{focusedEnemy.EnemyData.EnemyName}: {old} → {focusedEnemy.Stats.CurrentHostility}"
                );
        }

        private static int GetPolicyHostilityShift(PolicyLean lean, DemographicValues values)
        {
            return (lean, values) switch
            {
                (PolicyLean.Left, DemographicValues.Progressive) => -1,
                (PolicyLean.Left, DemographicValues.Traditional) => +1,
                (PolicyLean.Right, DemographicValues.Traditional) => -1,
                (PolicyLean.Right, DemographicValues.Progressive) => +1,
                (PolicyLean.Center, DemographicValues.Moderate) => -1,
                _ => 0,
            };
        }

        #endregion

        #region Turncoat

        /// <summary>
        /// Reacts to a receptive enemy flipping hostile: applies the Turncoat status, takes a small
        /// opinion hit (the crowd noticed), nudges immediate neighbours toward hostility (contagion),
        /// and forces the betrayer's next intent aggressive.
        /// </summary>
        private void OnEnemyTurncoat(EnemyTurncoatEvent evt)
        {
            if (_resolvingTurncoat)
                return;

            int idx = evt.EnemyIndex;
            if (idx < 0 || idx >= _enemies.Count)
                return;
            var enemy = _enemies[idx];
            if (enemy.IsDefeated)
                return;

            _resolvingTurncoat = true;
            try
            {
                // 1. Turncoat status — hits harder than a natural hostile for a turn or two.
                if (_turncoatStacks > 0)
                {
                    enemy.StatusEffects.ApplyStatusEffect(
                        StatusEffectType.Turncoat,
                        _turncoatStacks,
                        StatusDurationType.DecreasePerTurn
                    );
                    EventBus.Publish(
                        new StatusEffectAppliedEvent
                        {
                            StatusType = StatusEffectType.Turncoat,
                            Stacks = _turncoatStacks,
                            IsToPlayer = false,
                            EnemyIndex = idx,
                        }
                    );
                }

                // 2. The crowd noticed the betrayal — small direct opinion hit (bypasses Support).
                if (_turncoatOpinionHit > 0)
                    _opinion?.DecayOpinion(_turncoatOpinionHit);

                // 3. Betrayal is contagious — nudge immediate neighbours' hostility.
                if (_turncoatAdjacentNudge > 0)
                {
                    NudgeNeighbourHostility(idx - 1);
                    NudgeNeighbourHostility(idx + 1);
                }

                // 4. Lash out — force an aggressive next intent and re-declare it for the UI.
                enemy.FlagForcedAggressiveIntent();
                var intent = enemy.SelectNextMove(_enemies);
                if (intent != null)
                    EventBus.Publish(new EnemyIntentDeclaredEvent { Move = intent, EnemyIndex = idx });

                GameLogger.LogInfo<CrowdReactions>(
                    $"Turncoat! [{idx}] {enemy.EnemyData.EnemyName} turned on you."
                );
            }
            finally
            {
                _resolvingTurncoat = false;
            }

            // Contagion may have changed the room's echo-chamber status.
            RefreshEchoChamberState();
        }

        private void NudgeNeighbourHostility(int index)
        {
            if (index < 0 || index >= _enemies.Count)
                return;
            var neighbour = _enemies[index];
            if (!neighbour.IsDefeated)
                neighbour.Stats.GainHostility(_turncoatAdjacentNudge);
        }

        #endregion
    }
}
