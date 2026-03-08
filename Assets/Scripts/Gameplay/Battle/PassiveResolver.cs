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
    /// Supports two parallel passive systems:
    ///
    ///   1. LEGACY — single-passive per OriginPassive ScriptableObject (switch-statement dispatch).
    ///      Kept for backward compatibility — notably required by the Actor's Improvise mechanic,
    ///      which is interactive and doesn't fit the fire-and-forget BattlePassive model.
    ///
    ///   2. NEW (SOLID) — any number of <see cref="BattlePassive"/> entries registered from:
    ///      • OriginPassive._passives (when non-empty, preferred over legacy for that origin)
    ///      • CardData._passives (every card in the player's starting deck contributes its passives)
    ///
    ///      New-system passives use fully polymorphic triggers (<see cref="PassiveTriggerBase"/>),
    ///      conditions (<see cref="PassiveConditionBase"/>), and effects (<see cref="BattleEffect"/>).
    ///      No switch statement — adding new triggers / conditions / effects = adding new files only.
    ///
    /// Call <see cref="Dispose"/> when the battle ends to unsubscribe legacy EventBus listeners.
    /// </summary>
    public class PassiveResolver : IDisposable
    {
        // ── Legacy single-passive path ────────────────────────────────────────

        private readonly OriginPassive _passive;          // null if origin has no legacy passive
        private readonly BattleStats   _playerStats;
        private DeckManager            _deck;

        private int  _playerTurnNumber  = 0;
        private int  _triggerEventCount = 0;   // for NthEvent condition (legacy)
        private bool _fired             = false; // one-shot guard (legacy)

        // ── Improvise state (Actor passive — legacy path only) ────────────────

        private bool _improviseAvailable = false;
        private bool _improviseUsed      = false;

        /// <summary>Fired when the Actor's Improvise window opens. BattleUI subscribes.</summary>
        public event Action OnImproviseAvailable;

        /// <summary>True when Improvise is available this turn and hasn't been used yet.</summary>
        public bool ImproviseAvailable => _improviseAvailable && !_improviseUsed;

        // ── New-system BattlePassive collection ───────────────────────────────

        private readonly List<BattlePassive>           _allPassives;
        private readonly EffectResolver                _effectResolver;
        private          IReadOnlyList<EnemyController> _enemies;
        private          StatusEffectManager            _playerStatusEffects;

        // ── Lambda references for unconditional new-system subscriptions ──────
        // Stored as fields so that Dispose() can unsubscribe them correctly.

        private readonly Action<TurnStartedEvent>          _onTurnStarted;
        private readonly Action<TurnEndedEvent>            _onTurnEnded;
        private readonly Action<BattleEndedEvent>          _onBattleEnded;
        private readonly Action<CardPlayedEvent>           _onCardPlayed;
        private readonly Action<CardDrawnEvent>            _onCardDrawn;
        private readonly Action<CardDiscardedEvent>        _onCardDiscarded;
        private readonly Action<CardExhaustedEvent>        _onCardExhausted;
        private readonly Action<CardRetainedEvent>         _onCardRetained;
        private readonly Action<CardRecoveredEvent>        _onCardRecovered;
        private readonly Action<CardUpgradedEvent>         _onCardUpgraded;
        private readonly Action<DamageDealtEvent>          _onDamageDealt;
        private readonly Action<HealingAppliedEvent>       _onHealingApplied;
        private readonly Action<StatusEffectAppliedEvent>  _onStatusEffectApplied;
        private readonly Action<ComposureChangedEvent>     _onComposureChanged;
        private readonly Action<EnemyDefeatedEvent>        _onEnemyDefeated;
        private readonly Action<EnemySummonedEvent>        _onEnemySummoned;
        private readonly Action<EnemyActingEvent>          _onEnemyActing;

        // ─── Constructor ──────────────────────────────────────────────────────

        /// <summary>
        /// Creates a PassiveResolver for the given origin passive and player state.
        /// </summary>
        /// <param name="passive">The origin's passive ScriptableObject (may be null).</param>
        /// <param name="playerStats">Player's battle statistics.</param>
        /// <param name="effectResolver">Creates <see cref="EffectExecutionContext"/> for BattleEffect execution.</param>
        /// <param name="enemies">All active enemies (for condition evaluation).</param>
        /// <param name="playerStatusEffects">Player's status manager (for condition evaluation).</param>
        public PassiveResolver(
            OriginPassive                  passive,
            BattleStats                    playerStats,
            EffectResolver                 effectResolver,
            IReadOnlyList<EnemyController> enemies            = null,
            StatusEffectManager            playerStatusEffects = null)
        {
            _passive             = passive;
            _playerStats         = playerStats;
            _effectResolver      = effectResolver;
            _enemies             = enemies     ?? Array.Empty<EnemyController>();
            _playerStatusEffects = playerStatusEffects;
            _allPassives         = new List<BattlePassive>();

            // Initialise stored lambdas (required so Dispose can unsubscribe exact same delegate)
            _onTurnStarted         = e => DispatchEvent(e);
            _onTurnEnded           = e => DispatchEvent(e);
            _onBattleEnded         = e => DispatchEvent(e);
            _onCardPlayed          = e => DispatchEvent(e);
            _onCardDrawn           = e => DispatchEvent(e);
            _onCardDiscarded       = e => DispatchEvent(e);
            _onCardExhausted       = e => DispatchEvent(e);
            _onCardRetained        = e => DispatchEvent(e);
            _onCardRecovered       = e => DispatchEvent(e);
            _onCardUpgraded        = e => DispatchEvent(e);
            _onDamageDealt         = e => DispatchEvent(e);
            _onHealingApplied      = e => DispatchEvent(e);
            _onStatusEffectApplied = e => DispatchEvent(e);
            _onComposureChanged    = e => DispatchEvent(e);
            _onEnemyDefeated       = e => DispatchEvent(e);
            _onEnemySummoned       = e => DispatchEvent(e);
            _onEnemyActing         = e => DispatchEvent(e);

            SubscribeToLegacyEvents();
            SubscribeToAllEventsForNewSystem();
        }

        // ─── EventBus wiring — LEGACY ─────────────────────────────────────────

        private void SubscribeToLegacyEvents()
        {
            if (_passive == null) return;

            switch (_passive.Trigger)
            {
                case PassiveTrigger.TurnEnd:
                    EventBus.Subscribe<TurnEndedEvent>(OnLegacyTurnEnded);
                    break;

                case PassiveTrigger.OnCardPlayed:
                case PassiveTrigger.OnPressureCardPlayed:
                case PassiveTrigger.OnRhetoricCardPlayed:
                case PassiveTrigger.OnPolicyCardPlayed:
                    EventBus.Subscribe<CardPlayedEvent>(OnLegacyCardPlayed);
                    break;

                case PassiveTrigger.OnDamageTaken:
                case PassiveTrigger.OnDamageDealt:
                    EventBus.Subscribe<DamageDealtEvent>(OnLegacyDamageDealt);
                    break;

                case PassiveTrigger.OnStatusApplied:
                    EventBus.Subscribe<StatusEffectAppliedEvent>(OnLegacyStatusApplied);
                    break;

                case PassiveTrigger.OnCardDrawn:
                    EventBus.Subscribe<CardDrawnEvent>(OnLegacyCardDrawn);
                    break;

                case PassiveTrigger.OnCardDiscarded:
                    EventBus.Subscribe<CardDiscardedEvent>(OnLegacyCardDiscarded);
                    break;

                case PassiveTrigger.OnComposureLost:
                    EventBus.Subscribe<ComposureChangedEvent>(OnLegacyComposureChanged);
                    break;

                case PassiveTrigger.OnEnemyDefeated:
                    EventBus.Subscribe<EnemyDefeatedEvent>(OnLegacyEnemyDefeated);
                    break;

                case PassiveTrigger.BattleEnd:
                    EventBus.Subscribe<BattleEndedEvent>(OnLegacyBattleEnded);
                    break;
            }
        }

        private void UnsubscribeFromLegacyEvents()
        {
            if (_passive == null) return;

            switch (_passive.Trigger)
            {
                case PassiveTrigger.TurnEnd:
                    EventBus.Unsubscribe<TurnEndedEvent>(OnLegacyTurnEnded);
                    break;

                case PassiveTrigger.OnCardPlayed:
                case PassiveTrigger.OnPressureCardPlayed:
                case PassiveTrigger.OnRhetoricCardPlayed:
                case PassiveTrigger.OnPolicyCardPlayed:
                    EventBus.Unsubscribe<CardPlayedEvent>(OnLegacyCardPlayed);
                    break;

                case PassiveTrigger.OnDamageTaken:
                case PassiveTrigger.OnDamageDealt:
                    EventBus.Unsubscribe<DamageDealtEvent>(OnLegacyDamageDealt);
                    break;

                case PassiveTrigger.OnStatusApplied:
                    EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnLegacyStatusApplied);
                    break;

                case PassiveTrigger.OnCardDrawn:
                    EventBus.Unsubscribe<CardDrawnEvent>(OnLegacyCardDrawn);
                    break;

                case PassiveTrigger.OnCardDiscarded:
                    EventBus.Unsubscribe<CardDiscardedEvent>(OnLegacyCardDiscarded);
                    break;

                case PassiveTrigger.OnComposureLost:
                    EventBus.Unsubscribe<ComposureChangedEvent>(OnLegacyComposureChanged);
                    break;

                case PassiveTrigger.OnEnemyDefeated:
                    EventBus.Unsubscribe<EnemyDefeatedEvent>(OnLegacyEnemyDefeated);
                    break;

                case PassiveTrigger.BattleEnd:
                    EventBus.Unsubscribe<BattleEndedEvent>(OnLegacyBattleEnded);
                    break;
            }
        }

        // ─── EventBus wiring — NEW SYSTEM (unconditional, all events) ─────────

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
            EventBus.Subscribe(_onComposureChanged);

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
            EventBus.Unsubscribe(_onComposureChanged);

            EventBus.Unsubscribe(_onEnemyDefeated);
            EventBus.Unsubscribe(_onEnemySummoned);
            EventBus.Unsubscribe(_onEnemyActing);
        }

        /// <summary>Unsubscribes all EventBus listeners. Call when the battle ends.</summary>
        public void Dispose()
        {
            UnsubscribeFromLegacyEvents();
            UnsubscribeFromAllEventsForNewSystem();
        }

        // ─── New-system: registration ─────────────────────────────────────────

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
                    if (bp != null) _allPassives.Add(bp);
            }

            // Card passives — all cards in the full deck (draw + hand + discard)
            if (deck != null)
            {
                foreach (var card in deck.Hand)
                {
                    if (card?.Passives == null) continue;
                    foreach (var bp in card.Passives)
                        if (bp != null) _allPassives.Add(bp);
                }
            }

            // Reset all passives so they're fresh for this battle
            foreach (var bp in _allPassives)
                bp.ResetForBattle();

            GameLogger.LogInfo<PassiveResolver>(
                $"Registered {_allPassives.Count} BattlePassive(s) for this battle.");
        }

        // ─── New-system: dispatch ─────────────────────────────────────────────

        /// <summary>
        /// Boxes <paramref name="evt"/> and dispatches to all registered BattlePassives.
        /// Each passive handles its own trigger matching, condition evaluation, and effect execution.
        /// </summary>
        private void DispatchEvent<T>(T evt) where T : struct, IGameEvent
        {
            if (_allPassives.Count == 0 || _effectResolver == null) return;

            var evtCtx  = new PassiveEventContext(evt);
            var evalCtx = new PassiveEvaluationContext(
                _playerStats,
                _deck,
                _enemies,
                _playerStatusEffects,
                _playerTurnNumber,
                evtCtx);

            foreach (var passive in _allPassives)
            {
                // Fresh execution context per passive prevents state bleed between passives
                var execCtx = _effectResolver.CreateContext(isPlayerCard: true);
                passive.TryFire(evtCtx, evalCtx, execCtx);
            }
        }

        // ─── Direct-call hooks (invoked by BattleManager) ─────────────────────

        /// <summary>
        /// Called once after the opening hand is dealt.
        /// Registers card passives, fires BattleStart legacy passive, then dispatches
        /// a synthetic BattleStartedEvent so new-system passives with BattleStartTrigger fire.
        /// </summary>
        public void FireBattleStart(DeckManager deck)
        {
            _deck = deck;
            RegisterCardPassives(deck);

            // Legacy path
            if (_passive?.Trigger == PassiveTrigger.BattleStart)
                LegacyTryFire();

            // New system: synthetic event (after opening hand, not at BattleStartedEvent time)
            DispatchEvent(default(BattleStartedEvent));
        }

        /// <summary>
        /// Called at the start of each player turn by BattleManager.
        /// Updates the turn counter (needed by TurnNumber conditions), resets Improvise,
        /// and fires the legacy TurnStart passive if applicable.
        /// New-system TurnStart passives fire via the TurnStartedEvent subscription.
        /// </summary>
        public void FireTurnStart(int playerTurnNumber)
        {
            _playerTurnNumber = playerTurnNumber;
            _improviseUsed    = false;

            // Legacy path only
            if (_passive?.Trigger == PassiveTrigger.TurnStart)
                LegacyTryFire();

            // New system fires via EventBus.Subscribe<TurnStartedEvent> (published after this call)
        }

        // ─── Legacy EventBus handlers ──────────────────────────────────────────

        private void OnLegacyTurnEnded(TurnEndedEvent evt)
        {
            if (!evt.WasPlayerTurn) return;
            LegacyTryFire();
        }

        private void OnLegacyCardPlayed(CardPlayedEvent evt)
        {
            if (!evt.IsPlayer) return;
            bool typeMatch = _passive.Trigger switch
            {
                PassiveTrigger.OnPressureCardPlayed => evt.Card.CardType == CardType.Pressure,
                PassiveTrigger.OnRhetoricCardPlayed => evt.Card.CardType == CardType.Rhetoric,
                PassiveTrigger.OnPolicyCardPlayed   => evt.Card.CardType == CardType.Policy,
                _                                   => true,
            };
            if (typeMatch) LegacyTryFire();
        }

        private void OnLegacyDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Amount <= 0) return;
            bool match = _passive.Trigger == PassiveTrigger.OnDamageTaken ?  evt.IsToPlayer
                       : _passive.Trigger == PassiveTrigger.OnDamageDealt ? !evt.IsToPlayer
                       : false;
            if (match) LegacyTryFire();
        }

        private void OnLegacyStatusApplied(StatusEffectAppliedEvent evt)
        {
            if (!evt.IsToPlayer) LegacyTryFire();
        }

        private void OnLegacyCardDrawn(CardDrawnEvent evt)
        {
            if (!evt.IsPlayer) return;
            LegacyTryFire();
        }

        private void OnLegacyCardDiscarded(CardDiscardedEvent evt)
        {
            if (!evt.IsPlayer) return;
            LegacyTryFire();
        }

        private void OnLegacyComposureChanged(ComposureChangedEvent evt)
        {
            if (!evt.IsPlayer || evt.NewValue >= evt.OldValue) return;
            LegacyTryFire();
        }

        private void OnLegacyEnemyDefeated(EnemyDefeatedEvent evt) => LegacyTryFire();
        private void OnLegacyBattleEnded(BattleEndedEvent evt)     => LegacyTryFire();

        // ─── Legacy core ───────────────────────────────────────────────────────

        private void LegacyTryFire()
        {
            if (_passive == null) return;
            if (_passive.OneShot && _fired) return;

            _triggerEventCount++;
            if (!LegacyEvaluateCondition()) return;

            _fired = true;
            LegacyResolveEffect();
        }

        private bool LegacyEvaluateCondition()
        {
            var c = _passive.Condition;
            return c.Type switch
            {
                PassiveConditionType.Always           => true,
                PassiveConditionType.TurnNumberEquals => _playerTurnNumber == c.Value,
                PassiveConditionType.TurnNumberAtMost => _playerTurnNumber <= c.Value,
                PassiveConditionType.ResolveBelow     =>
                    _playerStats.CurrentResolve * 100 <= _playerStats.MaxResolve * c.Value,
                PassiveConditionType.NthEvent         => _triggerEventCount % c.Value == 0,
                _                                     => true,
            };
        }

        private void LegacyResolveEffect()
        {
            if (_passive == null) return;

            switch (_passive.EffectType)
            {
                case PassiveEffectType.GainActionPoints:
                    _playerStats.GainActionPoints(_passive.EffectAmount);
                    break;

                case PassiveEffectType.GainComposure:
                    _playerStats.GainComposure(_passive.EffectAmount);
                    break;

                case PassiveEffectType.GainResolve:
                    _playerStats.RestoreResolve(_passive.EffectAmount);
                    break;

                case PassiveEffectType.DrawCards:
                    _deck?.DrawCards(_passive.EffectAmount);
                    break;

                case PassiveEffectType.ReduceHostility:
                    GameLogger.LogWarning<PassiveResolver>(
                        $"[{_passive.PassiveName}] ReduceHostility is not supported in the legacy passive path.");
                    break;

                case PassiveEffectType.Improvise:
                    _improviseAvailable = true;
                    OnImproviseAvailable?.Invoke();
                    break;

                default:
                    break;
            }

            GameLogger.LogInfo<PassiveResolver>(
                $"[Legacy: {_passive.PassiveName}] fired — {_passive.EffectType} ×{_passive.EffectAmount}" +
                (_passive.OneShot ? " (one-shot)" : ""));
        }

        // ─── Improvise API (Actor passive — legacy path) ───────────────────────

        /// <summary>
        /// Called by the UI when the player confirms an Improvise selection.
        /// Discards the chosen cards and draws the same number back.
        /// </summary>
        public bool TryImprovise(DeckManager deck, List<CardData> cardsToDiscard)
        {
            if (!ImproviseAvailable)
            {
                GameLogger.LogWarning<PassiveResolver>("TryImprovise called but Improvise is not available");
                return false;
            }

            if (cardsToDiscard == null || cardsToDiscard.Count == 0)
            {
                _improviseUsed      = true;
                _improviseAvailable = false;
                GameLogger.LogInfo<PassiveResolver>("[Improvise] Skipped — no cards discarded");
                return false;
            }

            int count = cardsToDiscard.Count;
            foreach (var card in cardsToDiscard)
                deck.DiscardCard(card);

            int drawn = deck.DrawCards(count);
            _improviseUsed      = true;
            _improviseAvailable = false;

            GameLogger.LogInfo<PassiveResolver>($"[Improvise] Discarded {count} card(s), drew {drawn} back");
            return true;
        }
    }
}
