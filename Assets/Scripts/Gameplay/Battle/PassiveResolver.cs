using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Evaluates and fires origin passive abilities at the correct battle moments.
    ///
    /// All trigger/condition/effect configuration lives in <see cref="OriginPassive"/> ScriptableObject
    /// assets — no code changes are needed when adding new passives.
    ///
    /// Most triggers are wired via EventBus subscriptions, keeping BattleManager
    /// nearly passive-agnostic. The two exceptions (BattleStart, TurnStart) are
    /// still direct-called because they carry data (deck reference, turn number)
    /// that isn't in the published events.
    ///
    /// Call <see cref="Dispose"/> when the battle ends to unsubscribe all events.
    /// </summary>
    public class PassiveResolver : IDisposable
    {
        private readonly OriginPassive _passive;
        private readonly BattleStats   _playerStats;
        private DeckManager            _deck;

        // ── Condition tracking ────────────────────────────────────────────────
        private int  _playerTurnNumber  = 0;
        private int  _triggerEventCount = 0;   // total trigger fires (used by NthEvent)
        private bool _fired             = false; // one-shot guard

        // ── Improvise state (Actor passive) ───────────────────────────────────
        private bool _improviseAvailable = false;
        private bool _improviseUsed      = false;

        /// <summary>
        /// Fired when the Actor's Improvise window opens.
        /// BattleUI subscribes to show/hide the Improvise button.
        /// </summary>
        public event Action OnImproviseAvailable;

        /// <summary>True when Improvise is available this turn and hasn't been used yet.</summary>
        public bool ImproviseAvailable => _improviseAvailable && !_improviseUsed;

        // ─── Constructor ──────────────────────────────────────────────────────

        public PassiveResolver(OriginPassive passive, BattleStats playerStats)
        {
            _passive     = passive;
            _playerStats = playerStats;
            SubscribeToEvents();
        }

        // ─── EventBus wiring ──────────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            if (_passive == null) return;

            switch (_passive.Trigger)
            {
                case PassiveTrigger.TurnEnd:
                    EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
                    break;

                case PassiveTrigger.OnCardPlayed:
                case PassiveTrigger.OnPressureCardPlayed:
                case PassiveTrigger.OnRhetoricCardPlayed:
                case PassiveTrigger.OnPolicyCardPlayed:
                    EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
                    break;

                case PassiveTrigger.OnDamageTaken:
                case PassiveTrigger.OnDamageDealt:
                    EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
                    break;

                case PassiveTrigger.OnStatusApplied:
                    EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied);
                    break;

                case PassiveTrigger.OnCardDrawn:
                    EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
                    break;

                case PassiveTrigger.OnCardDiscarded:
                    EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
                    break;

                case PassiveTrigger.OnComposureLost:
                    EventBus.Subscribe<ComposureChangedEvent>(OnComposureChanged);
                    break;

                case PassiveTrigger.OnEnemyDefeated:
                    EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                    break;

                case PassiveTrigger.BattleEnd:
                    EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
                    break;

                // BattleStart, TurnStart, Always — handled via direct Fire* calls
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_passive == null) return;

            switch (_passive.Trigger)
            {
                case PassiveTrigger.TurnEnd:
                    EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
                    break;

                case PassiveTrigger.OnCardPlayed:
                case PassiveTrigger.OnPressureCardPlayed:
                case PassiveTrigger.OnRhetoricCardPlayed:
                case PassiveTrigger.OnPolicyCardPlayed:
                    EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
                    break;

                case PassiveTrigger.OnDamageTaken:
                case PassiveTrigger.OnDamageDealt:
                    EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
                    break;

                case PassiveTrigger.OnStatusApplied:
                    EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusApplied);
                    break;

                case PassiveTrigger.OnCardDrawn:
                    EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
                    break;

                case PassiveTrigger.OnCardDiscarded:
                    EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
                    break;

                case PassiveTrigger.OnComposureLost:
                    EventBus.Unsubscribe<ComposureChangedEvent>(OnComposureChanged);
                    break;

                case PassiveTrigger.OnEnemyDefeated:
                    EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
                    break;

                case PassiveTrigger.BattleEnd:
                    EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
                    break;
            }
        }

        /// <summary>Call when the battle ends to release all EventBus subscriptions.</summary>
        public void Dispose() => UnsubscribeFromEvents();

        // ─── Direct-call hooks (still invoked by BattleManager) ───────────────

        /// <summary>
        /// Called once after the opening hand is dealt.
        /// Also stores the deck reference needed by DrawCards effect type.
        /// </summary>
        public void FireBattleStart(DeckManager deck)
        {
            _deck = deck;
            if (_passive?.Trigger == PassiveTrigger.BattleStart)
                TryFire();
        }

        /// <summary>
        /// Called at the start of each player turn (not enemy turns).
        /// Tracks the cumulative player turn number for condition evaluation
        /// and resets the Improvise window each turn.
        /// </summary>
        public void FireTurnStart(int playerTurnNumber)
        {
            _playerTurnNumber = playerTurnNumber;
            _improviseUsed    = false;

            if (_passive?.Trigger == PassiveTrigger.TurnStart)
                TryFire();
        }

        // ─── EventBus handlers ────────────────────────────────────────────────

        private void OnTurnEnded(TurnEndedEvent evt)
        {
            if (!evt.WasPlayerTurn) return;
            TryFire();
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            if (!evt.IsPlayer) return;

            // For typed triggers, check the card's CardType
            bool typeMatch = _passive.Trigger switch
            {
                PassiveTrigger.OnPressureCardPlayed => evt.Card.CardType == CardType.Pressure,
                PassiveTrigger.OnRhetoricCardPlayed => evt.Card.CardType == CardType.Rhetoric,
                PassiveTrigger.OnPolicyCardPlayed   => evt.Card.CardType == CardType.Policy,
                _                                   => true,  // OnCardPlayed — any type
            };

            if (typeMatch) TryFire();
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Amount <= 0) return;

            bool match = _passive.Trigger == PassiveTrigger.OnDamageTaken ?  evt.IsToPlayer
                       : _passive.Trigger == PassiveTrigger.OnDamageDealt ? !evt.IsToPlayer
                       : false;

            if (match) TryFire();
        }

        private void OnStatusApplied(StatusEffectAppliedEvent evt)
        {
            // "OnStatusApplied" means the player applied a status to an enemy
            if (!evt.IsToPlayer) TryFire();
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (!evt.IsPlayer) return;
            TryFire();
        }

        private void OnCardDiscarded(CardDiscardedEvent evt)
        {
            if (!evt.IsPlayer) return;
            TryFire();
        }

        private void OnComposureChanged(ComposureChangedEvent evt)
        {
            // Only fire when the player actually lost composure (not gained)
            if (!evt.IsPlayer || evt.NewValue >= evt.OldValue) return;
            TryFire();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt) => TryFire();
        private void OnBattleEnded(BattleEndedEvent evt)     => TryFire();

        // ─── Core dispatch ────────────────────────────────────────────────────

        /// <summary>
        /// Increments the event counter, checks the one-shot guard and condition,
        /// then calls <see cref="ResolveEffect"/> if everything passes.
        /// </summary>
        private void TryFire()
        {
            if (_passive == null) return;

            // One-shot: stop after the first successful fire
            if (_passive.OneShot && _fired) return;

            _triggerEventCount++;

            if (!EvaluateCondition()) return;

            _fired = true;
            ResolveEffect();
        }

        private bool EvaluateCondition()
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

        private void ResolveEffect()
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
                    // Requires an enemy reference not available here; log and skip.
                    GameLogger.LogWarning<PassiveResolver>(
                        $"[{_passive.PassiveName}] ReduceHostility requires an enemy reference — not supported as a passive effect.");
                    break;

                case PassiveEffectType.Improvise:
                    _improviseAvailable = true;
                    OnImproviseAvailable?.Invoke();
                    break;

                // Campaign effects (GainInfluence, GainFunds, ReduceHeat) and
                // modifier effects (ReduceCardCost, IncreaseCardEffect, etc.)
                // are no-op inside a battle — handled by the campaign layer.
                default:
                    break;
            }

            GameLogger.LogInfo<PassiveResolver>(
                $"[{_passive.PassiveName}] fired — {_passive.EffectType} ×{_passive.EffectAmount}" +
                (_passive.OneShot ? " (one-shot)" : ""));
        }

        // ─── Improvise API (Actor passive) ────────────────────────────────────

        /// <summary>
        /// Called by the UI when the player confirms an Improvise selection.
        /// Discards the chosen cards and draws the same number back.
        /// </summary>
        /// <param name="deck">Player's deck manager.</param>
        /// <param name="cardsToDiscard">Cards to discard. Empty/null = skip.</param>
        /// <returns>True if Improvise executed; false if unavailable or skipped.</returns>
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
