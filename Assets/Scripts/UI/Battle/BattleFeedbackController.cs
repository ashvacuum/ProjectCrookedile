using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Audio;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using Crookedile.Managers;
using UnityEngine;

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
    public class BattleFeedbackController : MonoBehaviour, ICardPlayFeedback
    {
        [Header("Data")]
        [Tooltip(
            "ScriptableObject that maps each BattleAudioTrigger to an AudioEvent + VFXEvent pair."
        )]
        [SerializeField]
        private BattleSoundMap _soundMap;

        [Header("Scene References")]
        [Tooltip("Needed to resolve VFX target positions (player slot, enemy slots).")]
        [SerializeField]
        private BattleUI _battleUI;

        [Tooltip(
            "BattleManager to register card-play VFX callbacks with. "
                + "Found automatically if left unassigned."
        )]
        [SerializeField]
        private BattleManager _battleManager;

        [Header("Floating Numbers")]
        [Tooltip("Color of damage number text spawned by FloatingTextManager.")]
        [SerializeField]
        private Color _damageColor = new Color(0.9f, 0.2f, 0.2f);

        [Tooltip("Color of heal number text spawned by FloatingTextManager.")]
        [SerializeField]
        private Color _healColor = new Color(0.2f, 0.9f, 0.2f);

        #region Lifecycle
        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        /// <summary>
        /// Subscribes <paramref name="handler"/> and records the matching unsubscribe so
        /// <see cref="OnDisable"/> can't drift out of sync with the subscribe list.
        /// </summary>
        private void Sub<T>(System.Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _eventUnsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void OnEnable()
        {
            // Register as the card-play VFX implementation — a direct callback handshake,
            // not bus events (game flow must never block on a missed message).
            if (_battleManager == null)
                _battleManager = FindObjectOfType<BattleManager>();
            if (_battleManager != null)
                _battleManager.CardPlayFeedback = this;

            Sub<BattleStartedEvent>(OnBattleStarted);
            Sub<BattleEndedEvent>(OnBattleEnded);
            Sub<TurnStartedEvent>(OnTurnStarted);
            Sub<TurnEndedEvent>(OnTurnEnded);
            Sub<CardPlayedEvent>(OnCardPlayed);
            Sub<CardDrawnEvent>(OnCardDrawn);
            Sub<CardDiscardedEvent>(OnCardDiscarded);
            Sub<CardExhaustedEvent>(OnCardExhausted);
            Sub<DamageDealtEvent>(OnDamageDealt);
            Sub<HealingAppliedEvent>(OnHealApplied);
            Sub<StatusEffectAppliedEvent>(OnStatusApplied);
            Sub<EnemyDefeatedEvent>(OnEnemyDefeated);
            Sub<EnemyActingEvent>(OnEnemyActing);
            Sub<EnemyIntentDeclaredEvent>(OnEnemyIntentDeclared);
            Sub<SupportChangedEvent>(OnSupportChanged);
            Sub<DenialChangedEvent>(OnDenialChanged);
            Sub<HostilityChangedEvent>(OnHostilityChanged);
            Sub<ActionPointsChangedEvent>(OnAPChanged);
        }

        private void OnDisable()
        {
            if (_battleManager != null && ReferenceEquals(_battleManager.CardPlayFeedback, this))
                _battleManager.CardPlayFeedback = null;

            foreach (var unsub in _eventUnsubscribers)
                unsub();
            _eventUnsubscribers.Clear();
        }

        #endregion

        #region Event Handlers
        private void OnBattleStarted(BattleStartedEvent _) => Play(BattleAudioTrigger.BattleStart);

        private void OnBattleEnded(BattleEndedEvent evt) =>
            Play(
                evt.Result.isVictory
                    ? BattleAudioTrigger.BattleVictory
                    : BattleAudioTrigger.BattleDefeat
            );

        private void OnTurnStarted(TurnStartedEvent evt) =>
            Play(
                evt.IsPlayerTurn
                    ? BattleAudioTrigger.PlayerTurnStart
                    : BattleAudioTrigger.OpponentTurnStart
            );

        private void OnTurnEnded(TurnEndedEvent _)
        {
            // No dedicated trigger for turn-end yet — add an entry to BattleAudioTrigger if needed.
        }

        private void OnCardPlayed(CardPlayedEvent _) => Play(BattleAudioTrigger.CardPlayed);

        private void OnCardDrawn(CardDrawnEvent _) => Play(BattleAudioTrigger.CardDrawn);

        private void OnCardDiscarded(CardDiscardedEvent _) =>
            Play(BattleAudioTrigger.CardDiscarded);

        private void OnCardExhausted(CardExhaustedEvent _) =>
            Play(BattleAudioTrigger.CardExhausted);

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            var trigger = evt.IsToPlayer
                ? BattleAudioTrigger.DamageDealtToPlayer
                : BattleAudioTrigger.DamageDealtToEnemy;

            // VFX source: attacker's position.
            // Enemy → player: VFX at the attacking enemy's slot.
            // Player → enemy: VFX at the player slot (source of the attack).
            var vfxSource = evt.IsToPlayer
                ? _battleUI?.GetEnemySlotTransform(evt.SourceEnemyIndex)
                : _battleUI?.PlayerSlotTransform;
            Play(trigger, vfxSource);

            // Floating damage number: appears at the target that took the hit.
            // Player took damage: number appears at the player slot.
            // Enemy took damage: number appears at the targeted enemy slot.
            var dmgTarget = evt.IsToPlayer
                ? _battleUI?.PlayerSlotTransform
                : _battleUI?.GetEnemySlotTransform(evt.TargetEnemyIndex);
            FloatingTextManager.Instance?.Show(evt.Amount.ToString(), dmgTarget, _damageColor);
        }

        private void OnHealApplied(HealingAppliedEvent evt)
        {
            var target = evt.IsToPlayer ? _battleUI?.PlayerSlotTransform : null;
            Play(BattleAudioTrigger.HealApplied, target);
            if (evt.IsToPlayer)
                FloatingTextManager.Instance?.Show($"+{evt.Amount}", target, _healColor);
        }

        private void OnStatusApplied(StatusEffectAppliedEvent evt)
        {
            var target = evt.IsToPlayer ? _battleUI?.PlayerSlotTransform : null;
            Play(BattleAudioTrigger.StatusEffectApplied, target);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt) =>
            Play(
                BattleAudioTrigger.EnemyDefeated,
                _battleUI?.GetEnemySlotTransform(evt.EnemyIndex)
            );

        private void OnEnemyActing(EnemyActingEvent evt)
        {
            // Play the move's VFX on the player slot if one is configured.
            // Non-blocking: damage resolves simultaneously on the same frame.
            if (evt.Move?.MoveVFX != null)
                VFXManager.Instance?.Play(evt.Move.MoveVFX, _battleUI?.PlayerSlotTransform);
        }

        /// <summary>
        /// <see cref="ICardPlayFeedback"/> implementation — called directly by
        /// <c>BattleManager.PlayCard</c>. Spawns the card's VFX, fires the hit-frame
        /// callback through the animation, and completes the returned task when the
        /// animation ends. If the VFX fails to spawn, the callback fires and the task
        /// completes immediately so the battle is never left blocked.
        /// </summary>
        public Cysharp.Threading.Tasks.UniTask PlayCardVFX(
            Crookedile.Data.Cards.CardData card,
            System.Action onApplyEffects
        )
        {
            // Resolve VFX spawn target: prefer the last-targeted enemy slot, fall back to the card's origin rect.
            var vfxTarget = EnemySlotUI.LastTargetedRect ?? CardButton.LastPlayedRect;

            var completion = new Cysharp.Threading.Tasks.UniTaskCompletionSource();
            var vfx = VFXManager.Instance?.PlayAndSetInstance(
                card.CardVFX,
                vfxTarget,
                new BattleVFXContext
                {
                    OnApplyEffects = onApplyEffects,
                    OnComplete = () => completion.TrySetResult(),
                }
            );

            if (vfx == null)
            {
                onApplyEffects?.Invoke();
                completion.TrySetResult();
            }

            return completion.Task;
        }

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt) =>
            Play(
                BattleAudioTrigger.EnemyIntentDeclared,
                _battleUI?.GetEnemySlotTransform(evt.EnemyIndex)
            );

        private void OnSupportChanged(SupportChangedEvent evt) =>
            Play(
                evt.NewValue > evt.OldValue
                    ? BattleAudioTrigger.ShieldGained
                    : BattleAudioTrigger.ShieldLost
            );

        private void OnDenialChanged(DenialChangedEvent evt) =>
            Play(
                evt.NewValue > evt.OldValue
                    ? BattleAudioTrigger.ShieldGained
                    : BattleAudioTrigger.ShieldLost
            );

        private void OnHostilityChanged(HostilityChangedEvent evt)
        {
            // Player hostility (index -1) has no enemy slot to anchor the cue to.
            if (evt.EnemyIndex < 0)
                return;
            Play(
                BattleAudioTrigger.EnemyHostilityChanged,
                _battleUI?.GetEnemySlotTransform(evt.EnemyIndex)
            );
        }

        private void OnAPChanged(ActionPointsChangedEvent evt)
        {
            // Only fire for player AP changes (enemies have 0 AP; filter noise).
            if (!evt.IsPlayer)
                return;
            Play(
                evt.NewValue > evt.OldValue
                    ? BattleAudioTrigger.APGained
                    : BattleAudioTrigger.APSpent
            );
        }

        #endregion

        #region Core Play Helper
        /// <summary>
        /// Looks up the trigger in the sound map and fires audio + VFX.
        /// All null checks are internal — safe to call when map or entries are unassigned.
        /// </summary>
        private void Play(BattleAudioTrigger trigger, RectTransform target = null)
        {
            if (_soundMap == null)
                return;
            if (!_soundMap.TryGet(trigger, out var entry))
                return;

            entry.Sound?.Play();

            if (entry.Visual != null)
            {
                if (target != null)
                    VFXManager.Instance?.Play(entry.Visual, target);
                else
                    VFXManager.Instance?.Play(entry.Visual, (RectTransform)null);
            }
        }

        #endregion
    }
}
