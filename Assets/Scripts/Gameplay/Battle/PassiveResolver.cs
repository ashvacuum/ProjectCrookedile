using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Evaluates and fires passive abilities at the correct battle moments.
    ///
    /// Supports any number of <see cref="BattlePassive"/> entries registered from:
    ///   • OriginPassive._passives (populated per origin)
    ///   • CardData._passives (every card in the player's starting deck contributes its passives)
    ///
    /// Passives use fully polymorphic triggers (<see cref="PassiveTriggerBase"/>),
    /// conditions (<see cref="PassiveConditionBase"/>), and effects (<see cref="BattleEffect"/>).
    /// No switch statement — adding new triggers / conditions / effects = adding new files only.
    ///
    /// Call <see cref="Dispose"/> when the battle ends to unsubscribe EventBus listeners.
    /// </summary>
    public class PassiveResolver : IDisposable
    {
        private readonly OriginPassive _passive;
        private readonly BattleStats _playerStats;
        private DeckManager _deck;
        private Func<float> _getOpinionPercentage;

        private int _playerTurnNumber = 0;

        #region BattlePassive collection
        private readonly List<BattlePassive> _allPassives;
        private readonly EffectResolver _effectResolver;
        private IReadOnlyList<EnemyController> _enemies;
        private StatusEffectManager _playerStatusEffects;

        #endregion

        #region Lambda references for unconditional new-system subscriptions
        // Stored as fields so that Dispose() can unsubscribe them correctly.

        private readonly Action<TurnStartedEvent> _onTurnStarted;
        private readonly Action<TurnEndedEvent> _onTurnEnded;
        private readonly Action<BattleEndedEvent> _onBattleEnded;
        private readonly Action<CardPlayedEvent> _onCardPlayed;
        private readonly Action<CardDrawnEvent> _onCardDrawn;
        private readonly Action<CardDiscardedEvent> _onCardDiscarded;
        private readonly Action<CardExhaustedEvent> _onCardExhausted;
        private readonly Action<CardRetainedEvent> _onCardRetained;
        private readonly Action<CardRecoveredEvent> _onCardRecovered;
        private readonly Action<CardUpgradedEvent> _onCardUpgraded;
        private readonly Action<DamageDealtEvent> _onDamageDealt;
        private readonly Action<HealingAppliedEvent> _onHealingApplied;
        private readonly Action<StatusEffectAppliedEvent> _onStatusEffectApplied;
        private readonly Action<SupportChangedEvent> _onSupportChanged;
        private readonly Action<DenialChangedEvent> _onDenialChanged;
        private readonly Action<EnemyDefeatedEvent> _onEnemyDefeated;
        private readonly Action<EnemySummonedEvent> _onEnemySummoned;
        private readonly Action<EnemyActingEvent> _onEnemyActing;

        #endregion

        #region Constructor
        /// <summary>
        /// Creates a PassiveResolver for the given origin passive and player state.
        /// </summary>
        /// <param name="passive">The origin's passive ScriptableObject (may be null).</param>
        /// <param name="playerStats">Player's battle statistics.</param>
        /// <param name="effectResolver">Creates <see cref="EffectExecutionContext"/> for BattleEffect execution.</param>
        /// <param name="enemies">All active enemies (for condition evaluation).</param>
        /// <param name="playerStatusEffects">Player's status manager (for condition evaluation).</param>
        private BattleManager _battleManager;

        public PassiveResolver(
            OriginPassive passive,
            BattleStats playerStats,
            EffectResolver effectResolver,
            IReadOnlyList<EnemyController> enemies = null,
            StatusEffectManager playerStatusEffects = null,
            Func<float> getOpinionPercentage = null,
            BattleManager battleManager = null
        )
        {
            _passive = passive;
            _playerStats = playerStats;
            _effectResolver = effectResolver;
            _enemies = enemies ?? Array.Empty<EnemyController>();
            _playerStatusEffects = playerStatusEffects;
            _getOpinionPercentage = getOpinionPercentage ?? (() => 0f);
            _battleManager = battleManager;
            _allPassives = new List<BattlePassive>();

            // Initialise stored lambdas (required so Dispose can unsubscribe exact same delegate)
            _onTurnStarted = e => DispatchEvent(e);
            _onTurnEnded = e => DispatchEvent(e);
            _onBattleEnded = e => DispatchEvent(e);
            _onCardPlayed = e => DispatchEvent(e);
            _onCardDrawn = e => DispatchEvent(e);
            _onCardDiscarded = e => DispatchEvent(e);
            _onCardExhausted = e => DispatchEvent(e);
            _onCardRetained = e => DispatchEvent(e);
            _onCardRecovered = e => DispatchEvent(e);
            _onCardUpgraded = e => DispatchEvent(e);
            _onDamageDealt = e => DispatchEvent(e);
            _onHealingApplied = e => DispatchEvent(e);
            _onStatusEffectApplied = e => DispatchEvent(e);
            _onSupportChanged = e => DispatchEvent(e);
            _onDenialChanged = e => DispatchEvent(e);
            _onEnemyDefeated = e => DispatchEvent(e);
            _onEnemySummoned = e => DispatchEvent(e);
            _onEnemyActing = e => DispatchEvent(e);

            SubscribeToAllEventsForNewSystem();
        }

        #endregion

        #region EventBus wiring
        private void SubscribeToAllEventsForNewSystem()
        {
            // Subscribe unconditionally to every known event type.
            // Each BattlePassive's PassiveTriggerBase.Matches() handles filtering.
            // BattleStartedEvent is omitted — timing handled via FireBattleStart() instead.

            EventBus.Subscribe(_onTurnStarted);
            EventBus.Subscribe(_onTurnEnded);
            EventBus.Subscribe(_onBattleEnded);

            EventBus.Subscribe(_onCardPlayed);
            EventBus.Subscribe(_onCardDrawn);
            EventBus.Subscribe(_onCardDiscarded);
            EventBus.Subscribe(_onCardExhausted);
            EventBus.Subscribe(_onCardRetained);
            EventBus.Subscribe(_onCardRecovered);
            EventBus.Subscribe(_onCardUpgraded);

            EventBus.Subscribe(_onDamageDealt);
            EventBus.Subscribe(_onHealingApplied);
            EventBus.Subscribe(_onStatusEffectApplied);
            EventBus.Subscribe(_onSupportChanged);
            EventBus.Subscribe(_onDenialChanged);

            EventBus.Subscribe(_onEnemyDefeated);
            EventBus.Subscribe(_onEnemySummoned);
            EventBus.Subscribe(_onEnemyActing);
        }

        private void UnsubscribeFromAllEventsForNewSystem()
        {
            EventBus.Unsubscribe(_onTurnStarted);
            EventBus.Unsubscribe(_onTurnEnded);
            EventBus.Unsubscribe(_onBattleEnded);

            EventBus.Unsubscribe(_onCardPlayed);
            EventBus.Unsubscribe(_onCardDrawn);
            EventBus.Unsubscribe(_onCardDiscarded);
            EventBus.Unsubscribe(_onCardExhausted);
            EventBus.Unsubscribe(_onCardRetained);
            EventBus.Unsubscribe(_onCardRecovered);
            EventBus.Unsubscribe(_onCardUpgraded);

            EventBus.Unsubscribe(_onDamageDealt);
            EventBus.Unsubscribe(_onHealingApplied);
            EventBus.Unsubscribe(_onStatusEffectApplied);
            EventBus.Unsubscribe(_onSupportChanged);
            EventBus.Unsubscribe(_onDenialChanged);

            EventBus.Unsubscribe(_onEnemyDefeated);
            EventBus.Unsubscribe(_onEnemySummoned);
            EventBus.Unsubscribe(_onEnemyActing);
        }

        /// <summary>Unsubscribes all EventBus listeners. Call when the battle ends.</summary>
        public void Dispose()
        {
            UnsubscribeFromAllEventsForNewSystem();
        }

        #region New-system: registration
        /// <summary>
        /// Collects all <see cref="BattlePassive"/> entries from the origin passive and
        /// every card in the player's deck, resets their runtime state, and fires BattleStart
        /// triggers on the new system.
        /// </summary>
        /// <remarks>Must be called after the opening hand is dealt.</remarks>
        private void RegisterCardPassives(DeckManager deck)
        {
            _allPassives.Clear();

            // Origin passive — new-system entries (when present)
            if (_passive?.Passives != null)
            {
                foreach (var bp in _passive.Passives)
                    if (bp != null)
                        _allPassives.Add(bp);
            }

            // Card passives — all cards in the full deck (draw pile + hand + discard).
            // Exhaust pile is excluded: exhausted cards leave active play for the battle duration.
            if (deck != null)
            {
                var zones = new IReadOnlyList<CardData>[]
                {
                    deck.DrawPile,
                    deck.Hand,
                    deck.DiscardPile,
                };
                foreach (var zone in zones)
                {
                    foreach (var card in zone)
                    {
                        var cardPassives = card?.GetPassives();
                        if (cardPassives == null)
                            continue;
                        foreach (var bp in cardPassives)
                            if (bp != null)
                                _allPassives.Add(bp);
                    }
                }
            }

            // Reset all passives so they're fresh for this battle
            foreach (var bp in _allPassives)
                bp.ResetForBattle();

            GameLogger.LogInfo<PassiveResolver>(
                $"Registered {_allPassives.Count} BattlePassive(s) for this battle."
            );
        }

        #endregion

        #region New-system: dispatch
        /// <summary>
        /// Boxes <paramref name="evt"/> and dispatches to all registered BattlePassives.
        /// Each passive handles its own trigger matching, condition evaluation, and effect execution.
        /// </summary>
        private void DispatchEvent<T>(T evt)
            where T : struct, IGameEvent
        {
            if (_allPassives.Count == 0 || _effectResolver == null)
                return;

            var evtCtx = new PassiveEventContext(evt);
            var evalCtx = new PassiveEvaluationContext(
                _playerStats,
                _deck,
                _enemies,
                _playerStatusEffects,
                _playerTurnNumber,
                evtCtx,
                _getOpinionPercentage(),
                _battleManager
            );

            foreach (var passive in _allPassives)
            {
                // Fresh execution context per passive prevents state bleed between passives.
                // Enrich it with values from the triggering event so passive effects can use
                // AmountSource (e.g. LastDamageDealt for lifesteal-style passives).
                var execCtx = _effectResolver.CreateContext(isPlayerCard: true);
                EnrichContextFromEvent(evtCtx, execCtx);
                passive.TryFire(evtCtx, evalCtx, execCtx);
            }
        }

        /// <summary>
        /// Populates the accumulated result fields on <paramref name="execCtx"/> from the
        /// data carried by the triggering event. This allows passive <see cref="BattleEffect"/>
        /// entries to use <see cref="EffectContextValue"/> sources such as
        /// <c>LastDamageDealt</c>, <c>LastHealAmount</c>, <c>LastShieldGained</c>, and
        /// <c>LastShieldLost</c> — mirroring the values card effects accumulate during
        /// in-resolution execution.
        /// </summary>
        private static void EnrichContextFromEvent(
            PassiveEventContext evtCtx,
            EffectExecutionContext execCtx
        )
        {
            if (evtCtx.Is<DamageDealtEvent>())
            {
                var e = evtCtx.As<DamageDealtEvent>();
                if (!e.IsToPlayer)
                    execCtx.LastDamageDealt = e.Amount;
            }
            else if (evtCtx.Is<HealingAppliedEvent>())
            {
                var e = evtCtx.As<HealingAppliedEvent>();
                if (e.IsToPlayer)
                    execCtx.LastHealAmount = e.Amount;
            }
            else if (evtCtx.Is<SupportChangedEvent>())
            {
                var e = evtCtx.As<SupportChangedEvent>();
                int delta = e.NewValue - e.OldValue;
                if (delta > 0)
                    execCtx.LastSupportGained = delta;
                else if (delta < 0)
                    execCtx.LastSupportLost = -delta;
            }
            else if (evtCtx.Is<EnemyDefeatedEvent>())
            {
                execCtx.LastTargetDied = true;
            }
        }

        #endregion

        #region Direct-call hooks (invoked by BattleManager)
        /// <summary>
        /// Called once after the opening hand is dealt.
        /// Registers card passives, then dispatches a synthetic BattleStartedEvent so
        /// passives with BattleStartTrigger fire at the correct moment (after hand is dealt,
        /// not at the real BattleStartedEvent time).
        /// </summary>
        public void FireBattleStart(DeckManager deck)
        {
            _deck = deck;
            RegisterCardPassives(deck);

            DispatchEvent(default(BattleStartedEvent));
        }

        /// <summary>
        /// Called at the start of each player turn by BattleManager.
        /// Updates the turn counter (needed by TurnNumber conditions).
        /// New-system TurnStart passives fire via the TurnStartedEvent subscription.
        /// </summary>
        public void FireTurnStart(int playerTurnNumber)
        {
            _playerTurnNumber = playerTurnNumber;
            // New system fires via EventBus.Subscribe<TurnStartedEvent> (published after this call)
        }
    }
}
        #endregion
        #endregion
