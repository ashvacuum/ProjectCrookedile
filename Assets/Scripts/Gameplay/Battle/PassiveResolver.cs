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
        private readonly IReadOnlyList<BattlePassive> _runPassives;
        private readonly BattleStats _playerStats;
        private DeckManager _deck;
        private Func<float> _getOpinionPercentage;

        private int _playerTurnNumber = 0;

        #region BattlePassive collection
        private readonly List<BattlePassive> _allPassives;

        // Passives bucketed by the System.Type of the event their trigger listens for. Lets
        // DispatchEvent<T> visit only passives that can possibly match typeof(T) — and skip all
        // context allocation when no passive listens for the event. Rebuilt by RegisterCardPassives.
        private readonly Dictionary<Type, List<BattlePassive>> _passivesByEvent =
            new Dictionary<Type, List<BattlePassive>>();

        // Owning card per card-sourced passive (default AND activated) — lets passive effects
        // act on the card itself (MoveOwnerCardEffect). Origin/relic passives have no owner.
        private readonly Dictionary<BattlePassive, CardData> _ownerByPassive =
            new Dictionary<BattlePassive, CardData>();

        private readonly EffectResolver _effectResolver;
        private IReadOnlyList<EnemyController> _enemies;
        private StatusEffectManager _playerStatusEffects;

        #endregion

        // Records an unsubscribe action per EventBus subscription, so Dispose() can tear them all
        // down without a parallel set of hand-maintained delegate fields (which were easy to
        // desync — a subscribe with no matching unsubscribe leaks the listener).
        private readonly List<Action> _unsubscribers = new List<Action>();

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
            BattleManager battleManager = null,
            IReadOnlyList<BattlePassive> runPassives = null
        )
        {
            _passive = passive;
            _playerStats = playerStats;
            _effectResolver = effectResolver;
            _enemies = enemies ?? Array.Empty<EnemyController>();
            _playerStatusEffects = playerStatusEffects;
            _getOpinionPercentage = getOpinionPercentage ?? (() => 0f);
            _battleManager = battleManager;
            _runPassives = runPassives;
            _allPassives = new List<BattlePassive>();

            SubscribeToAllEventsForNewSystem();
        }

        #endregion

        #region EventBus wiring

        /// <summary>
        /// Subscribes a single <c>e =&gt; DispatchEvent(e)</c> handler for event type
        /// <typeparamref name="T"/> and records the matching unsubscribe so Dispose can undo it.
        /// </summary>
        private void Subscribe<T>()
            where T : struct, IGameEvent
        {
            Action<T> handler = e => DispatchEvent(e);
            EventBus.Subscribe(handler);
            _unsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void SubscribeToAllEventsForNewSystem()
        {
            // Subscribe unconditionally to every known event type; each BattlePassive's trigger
            // does its own filtering. BattleStartedEvent is omitted — handled via FireBattleStart().
            Subscribe<TurnStartedEvent>();
            Subscribe<TurnEndedEvent>();
            Subscribe<BattleEndedEvent>();

            Subscribe<CardPlayedEvent>();
            Subscribe<CardDrawnEvent>();
            Subscribe<CardDiscardedEvent>();
            Subscribe<CardExhaustedEvent>();
            Subscribe<CardRetainedEvent>();
            Subscribe<CardRecoveredEvent>();
            Subscribe<CardUpgradedEvent>();

            Subscribe<DamageDealtEvent>();
            Subscribe<HealingAppliedEvent>();
            Subscribe<StatusEffectAppliedEvent>();
            Subscribe<SupportChangedEvent>();
            Subscribe<DenialChangedEvent>();

            Subscribe<EnemyDefeatedEvent>();
            Subscribe<EnemySummonedEvent>();
            Subscribe<EnemyActingEvent>();
        }

        /// <summary>Unsubscribes all EventBus listeners. Call when the battle ends.</summary>
        public void Dispose()
        {
            foreach (var unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();
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
            _ownerByPassive.Clear();

            // Origin passive — new-system entries (when present)
            if (_passive?.Passives != null)
            {
                foreach (var bp in _passive.Passives)
                    if (bp != null)
                        _allPassives.Add(bp);
            }

            // Run-level passives (relics) — persist across battles on RunState, re-registered
            // fresh each battle so their one-shot/fire-count state resets like any other passive.
            if (_runPassives != null)
            {
                foreach (var bp in _runPassives)
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
                        // Activated-passive (Policy) cards are excluded: their passives switch on
                        // only when the card is played (see ActivateCardPassives). Everything else
                        // is a DEFAULT passive — ambient from battle start, owner card recorded so
                        // effects can act on the card itself (e.g. MoveOwnerCardEffect).
                        if (card == null || card.IsActivatedPassive)
                            continue;
                        var cardPassives = card.GetPassives();
                        if (cardPassives == null)
                            continue;
                        foreach (var bp in cardPassives)
                        {
                            if (bp == null)
                                continue;
                            _allPassives.Add(bp);
                            _ownerByPassive[bp] = card;
                        }
                    }
                }
            }

            // Reset all passives so they're fresh for this battle, and bucket them by the event
            // type their trigger listens for so DispatchEvent can skip non-matching passives.
            _passivesByEvent.Clear();
            foreach (var bp in _allPassives)
                BucketPassive(bp);

            GameLogger.LogInfo<PassiveResolver>(
                $"Registered {_allPassives.Count} BattlePassive(s) for this battle."
            );
        }

        /// <summary>Resets a passive for this battle and files it under its trigger's event type.</summary>
        private void BucketPassive(BattlePassive bp)
        {
            bp.ResetForBattle();

            var eventType = bp.Trigger?.EventType;
            if (eventType == null)
                return; // a passive with no trigger can never fire — leave it unbucketed

            if (!_passivesByEvent.TryGetValue(eventType, out var bucket))
            {
                bucket = new List<BattlePassive>();
                _passivesByEvent[eventType] = bucket;
            }
            bucket.Add(bp);
        }

        /// <summary>
        /// Activates a Power card's passives at runtime (when the card is played), making them live
        /// for the rest of the battle. Call from BattleManager when an <see cref="CardData.IsPower"/>
        /// card resolves.
        /// </summary>
        public void ActivateCardPassives(CardData card)
        {
            var passives = card?.GetPassives();
            if (passives == null)
                return;

            int added = 0;
            foreach (var bp in passives)
            {
                if (bp == null)
                    continue;
                _allPassives.Add(bp);
                _ownerByPassive[bp] = card;
                BucketPassive(bp);
                added++;
            }

            if (added > 0)
                GameLogger.LogInfo<PassiveResolver>(
                    $"Activated {added} passive(s) from '{card.CardName}'."
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
            if (_effectResolver == null)
                return;

            // Only passives whose trigger listens for this exact event type can fire. If none do,
            // bail before allocating any context — most events have no listening passive.
            if (
                !_passivesByEvent.TryGetValue(typeof(T), out var bucket)
                || bucket == null
                || bucket.Count == 0
            )
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

            foreach (var passive in bucket)
            {
                // Fresh execution context per passive prevents state bleed between passives.
                // Enrich it with values from the triggering event so passive effects can use
                // AmountSource (e.g. LastDamageDealt for lifesteal-style passives).
                var execCtx = _effectResolver.CreateContext(isPlayerCard: true);
                EnrichContextFromEvent(evtCtx, execCtx);
                // Card-sourced passives know their owning card (null for origin/relic passives).
                _ownerByPassive.TryGetValue(passive, out var ownerCard);
                execCtx.OwnerCard = ownerCard;
                passive.TryFire(evtCtx, evalCtx, execCtx);
            }
        }

        /// <summary>
        /// Populates the accumulated result fields on <paramref name="execCtx"/> from the
        /// data carried by the triggering event. This allows passive <see cref="BattleEffect"/>
        /// entries to use <see cref="EffectContextValue"/> sources such as
        /// <c>LastDamageDealt</c>, <c>LastHealAmount</c>, <c>LastSupportGained</c>, and
        /// <c>LastSupportLost</c> — mirroring the values card effects accumulate during
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
            else if (evtCtx.Is<HostilityChangedEvent>())
            {
                var e = evtCtx.As<HostilityChangedEvent>();
                int delta = e.NewValue - e.OldValue;
                if (delta > 0)
                    execCtx.LastHostilityGained = delta;
                else if (delta < 0)
                    execCtx.LastHostilityLost = -delta;
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
        #endregion
        #endregion
    }
}
