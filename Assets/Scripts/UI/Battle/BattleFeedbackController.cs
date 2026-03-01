using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using Crookedile.Data.Audio;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Subscribes to all battle <see cref="EventBus"/> events and fires the matching
    /// audio + VFX pair from a <see cref="BattleSoundMap"/> ScriptableObject.
    ///
    /// Attach to a dedicated "BattleFeedbackController" GameObject in the battle scene.
    /// Wire <see cref="_soundMap"/> and <see cref="_battleUI"/> in the Inspector.
    ///
    /// All entries in the sound map are optional — a missing entry or null AudioEvent/VFXEvent
    /// is silently ignored, so the game runs without sound until assets are assigned.
    /// </summary>
    public class BattleFeedbackController : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("ScriptableObject that maps each BattleAudioTrigger to an AudioEvent + VFXEvent pair.")]
        [SerializeField] private BattleSoundMap _soundMap;

        [Header("Scene References")]
        [Tooltip("Needed to resolve VFX target positions (player stats panel, enemy slots).")]
        [SerializeField] private BattleUI       _battleUI;

        // ─── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Subscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Subscribe<CardExhaustedEvent>(OnCardExhausted);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealingAppliedEvent>(OnHealApplied);
            EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Subscribe<ComposureChangedEvent>(OnComposureChanged);
            EventBus.Subscribe<EnemyHostilityChangedEvent>(OnHostilityChanged);
            EventBus.Subscribe<ActionPointsChangedEvent>(OnAPChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<CardDrawnEvent>(OnCardDrawn);
            EventBus.Unsubscribe<CardDiscardedEvent>(OnCardDiscarded);
            EventBus.Unsubscribe<CardExhaustedEvent>(OnCardExhausted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealingAppliedEvent>(OnHealApplied);
            EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusApplied);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            EventBus.Unsubscribe<ComposureChangedEvent>(OnComposureChanged);
            EventBus.Unsubscribe<EnemyHostilityChangedEvent>(OnHostilityChanged);
            EventBus.Unsubscribe<ActionPointsChangedEvent>(OnAPChanged);
        }

        // ─── Event Handlers ───────────────────────────────────────────────────

        private void OnBattleStarted(BattleStartedEvent _)
            => Play(BattleAudioTrigger.BattleStart);

        private void OnBattleEnded(BattleEndedEvent evt)
            => Play(evt.Result.isVictory ? BattleAudioTrigger.BattleVictory
                                         : BattleAudioTrigger.BattleDefeat);

        private void OnTurnStarted(TurnStartedEvent evt)
            => Play(evt.IsPlayerTurn ? BattleAudioTrigger.PlayerTurnStart
                                     : BattleAudioTrigger.OpponentTurnStart);

        private void OnTurnEnded(TurnEndedEvent _)
        {
            // No dedicated trigger for turn-end yet — add an entry to BattleAudioTrigger if needed.
        }

        private void OnCardPlayed(CardPlayedEvent _)
            => Play(BattleAudioTrigger.CardPlayed);

        private void OnCardDrawn(CardDrawnEvent _)
            => Play(BattleAudioTrigger.CardDrawn);

        private void OnCardDiscarded(CardDiscardedEvent _)
            => Play(BattleAudioTrigger.CardDiscarded);

        private void OnCardExhausted(CardExhaustedEvent _)
            => Play(BattleAudioTrigger.CardExhausted);

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            var trigger = evt.IsToPlayer ? BattleAudioTrigger.DamageDealtToPlayer
                                         : BattleAudioTrigger.DamageDealtToEnemy;

            // Enemy → player: show VFX at the attacking enemy's slot so the player
            //   sees which enemy hit them.
            // Player → enemy: show VFX at the player stats panel (source of the attack).
            var target = evt.IsToPlayer
                ? _battleUI?.GetEnemySlotTransform(evt.SourceEnemyIndex)
                : _battleUI?.PlayerStatsPanel;

            Play(trigger, target);
        }

        private void OnHealApplied(HealingAppliedEvent evt)
        {
            var target = evt.IsToPlayer ? _battleUI?.PlayerStatsPanel : null;
            Play(BattleAudioTrigger.HealApplied, target);
        }

        private void OnStatusApplied(StatusEffectAppliedEvent evt)
        {
            var target = evt.IsToPlayer ? _battleUI?.PlayerStatsPanel : null;
            Play(BattleAudioTrigger.StatusEffectApplied, target);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
            => Play(BattleAudioTrigger.EnemyDefeated, _battleUI?.GetEnemySlotTransform(evt.EnemyIndex));

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt)
            => Play(BattleAudioTrigger.EnemyIntentDeclared, _battleUI?.GetEnemySlotTransform(evt.EnemyIndex));

        private void OnComposureChanged(ComposureChangedEvent evt)
            => Play(evt.NewValue > evt.OldValue ? BattleAudioTrigger.ComposureGained
                                                : BattleAudioTrigger.ComposureLost);

        private void OnHostilityChanged(EnemyHostilityChangedEvent evt)
            => Play(BattleAudioTrigger.EnemyHostilityChanged,
                    _battleUI?.GetEnemySlotTransform(evt.EnemyIndex));

        private void OnAPChanged(ActionPointsChangedEvent evt)
        {
            // Only fire for player AP changes (enemies have 0 AP; filter noise).
            if (!evt.IsPlayer) return;
            Play(evt.NewValue > evt.OldValue ? BattleAudioTrigger.APGained
                                             : BattleAudioTrigger.APSpent);
        }

        // ─── Core Play Helper ─────────────────────────────────────────────────

        /// <summary>
        /// Looks up the trigger in the sound map and fires audio + VFX.
        /// All null checks are internal — safe to call when map or entries are unassigned.
        /// </summary>
        private void Play(BattleAudioTrigger trigger, RectTransform target = null)
        {
            if (_soundMap == null) return;
            if (!_soundMap.TryGet(trigger, out var entry)) return;

            entry.Sound?.Play();

            if (entry.Visual != null)
            {
                if (target != null) entry.Visual.Play(target);
                else                entry.Visual.Play();
            }
        }
    }
}
