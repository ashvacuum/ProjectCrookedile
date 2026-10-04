using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Audio;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using Crookedile.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Floating Text")]
        [Tooltip("Hostility rising / an enemy turning Hostile or Turncoat.")]
        [SerializeField]
        private Color _hostileColor = new Color(0.95f, 0.3f, 0.2f);

        [Tooltip("Hostility falling / an enemy turning Receptive.")]
        [SerializeField]
        private Color _receptiveColor = new Color(0.3f, 0.85f, 0.45f);

        [Tooltip("An enemy settling back into Neutral.")]
        [SerializeField]
        private Color _neutralColor = new Color(0.8f, 0.8f, 0.85f);

        [Tooltip("Color of the 'Blocked' text when Support/Denial fully absorbs a hit.")]
        [SerializeField]
        private Color _blockedColor = new Color(0.7f, 0.7f, 0.75f);

        [Tooltip("Color of Support/Denial gain/loss numbers shown on the meter.")]
        [FormerlySerializedAs("_shieldColor")]
        [SerializeField]
        private Color _supportColor = new Color(0.4f, 0.6f, 0.9f);

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
                _battleManager = FindFirstObjectByType<BattleManager>();
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
            Sub<OpinionChangedEvent>(OnOpinionChanged);
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
                ? (
                    evt.IsBossSource
                        ? _battleUI?.BossTransform
                        : _battleUI?.GetEnemySlotTransform(evt.SourceEnemyIndex)
                )
                : _battleUI?.PlayerSlotTransform;
            Play(trigger, vfxSource);

            // No damage number: nothing has HP. The meter's own motion (OnOpinionChanged) is the
            // readout; a player-targeted enemy just gets a light "you addressed me" tell.
            if (!evt.IsToPlayer)
                ReactOnEnemy(evt.TargetEnemyIndex);

            // The one case the meter can't show: Support/Denial ate the whole shift.
            if (evt.Applied == 0 && evt.Absorbed > 0)
                FloatingTextManager.Instance?.Show(
                    "Blocked",
                    _battleUI?.MeterTransform,
                    _blockedColor
                );
        }

        /// <summary>
        /// Every Opinion move, whatever caused it (cards, enemy moves, echo decay, conversion
        /// bursts): meter impact plus the OpinionRaised/Lowered cue sparking at the fill's edge.
        /// </summary>
        private void OnOpinionChanged(OpinionChangedEvent evt)
        {
            var meter = _battleUI?.OpinionMeter;
            if (meter == null || evt.NewValue == evt.OldValue)
                return;
            meter.Kick(evt.OldValue, evt.NewValue, evt.MaxValue);
            PlayAtWorld(
                evt.NewValue > evt.OldValue
                    ? BattleAudioTrigger.OpinionRaised
                    : BattleAudioTrigger.OpinionLowered,
                meter.EdgeWorldPosition(evt.NewValue, evt.MaxValue)
            );
        }

        /// <summary>
        /// Light "you addressed me" tell on a player-targeted enemy — a small scale punch. The
        /// enemy isn't being depleted (the meter is), so this stays subtle, not a damage hit.
        /// </summary>
        private void ReactOnEnemy(int enemyIndex)
        {
            var slot = _battleUI?.GetEnemySlotTransform(enemyIndex);
            if (slot == null)
                return;
            slot.DOComplete();
            slot.DOPunchScale(Vector3.one * 0.08f, 0.2f, vibrato: 6, elasticity: 0.5f)
                .SetLink(slot.gameObject);
        }

        private void OnHealApplied(HealingAppliedEvent evt)
        {
            var target = evt.IsToPlayer ? _battleUI?.PlayerSlotTransform : null;
            Play(BattleAudioTrigger.HealApplied, target);
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
            // Non-blocking: the move resolves on the same frame.
            var vfx = evt.Move?.MoveVFX;
            if (vfx == null)
                return;
            foreach (var anchor in AnchorsFor(vfx, _battleUI?.PlayerSlotTransform))
                VFXManager.Instance?.Play(vfx, anchor);
        }

        /// <summary>
        /// Resolves a card/move VFX's <see cref="VFXAnchor"/> to the spots it plays at.
        /// A null entry plays at the VFX canvas root (screen center).
        /// </summary>
        private List<RectTransform> AnchorsFor(VFXEvent vfx, RectTransform target)
        {
            switch (vfx.Anchor)
            {
                case VFXAnchor.OpinionMeter:
                    return new List<RectTransform> { _battleUI?.MeterTransform };
                case VFXAnchor.Player:
                    return new List<RectTransform> { _battleUI?.PlayerSlotTransform };
                case VFXAnchor.ScreenCenter:
                    return new List<RectTransform> { null };
                case VFXAnchor.EveryEnemy:
                    var slots = new List<RectTransform>();
                    var enemies = _battleManager?.Enemies;
                    for (int i = 0; enemies != null && i < enemies.Count; i++)
                    {
                        var slot = enemies[i].IsDefeated
                            ? null
                            : _battleUI?.GetEnemySlotTransform(i);
                        if (slot != null)
                            slots.Add(slot);
                    }
                    return slots.Count > 0 ? slots : new List<RectTransform> { target };
                default:
                    return new List<RectTransform> { target };
            }
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
            // Target anchor = the last-targeted enemy slot, else the card's origin rect.
            var anchors = AnchorsFor(
                card.CardVFX,
                EnemySlotUI.LastTargetedRect ?? CardButton.LastPlayedRect
            );

            // The first copy owns hit timing; any others (EveryEnemy) are cosmetic.
            var completion = new Cysharp.Threading.Tasks.UniTaskCompletionSource();
            var vfx = VFXManager.Instance?.PlayAndSetInstance(
                card.CardVFX,
                anchors[0],
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
            else
                for (int i = 1; i < anchors.Count; i++)
                    VFXManager.Instance.Play(card.CardVFX, anchors[i]);

            return completion.Task;
        }

        private void OnEnemyIntentDeclared(EnemyIntentDeclaredEvent evt) =>
            Play(
                BattleAudioTrigger.EnemyIntentDeclared,
                _battleUI?.GetEnemySlotTransform(evt.EnemyIndex)
            );

        // Support and Denial both live on the meter (rendered as bar segments), so their
        // gain/loss feedback anchors to the meter — not a player/enemy slot — just like the
        // Opinion numbers that move it.
        private void OnSupportChanged(SupportChangedEvent evt) =>
            SupportFeedback(evt.OldValue, evt.NewValue, evt.IsDecay);

        private void OnDenialChanged(DenialChangedEvent evt) =>
            SupportFeedback(evt.OldValue, evt.NewValue, evt.IsDecay);

        private void SupportFeedback(int oldValue, int newValue, bool isDecay)
        {
            // Ambient turn-start expiry is not an attack — no sting, no number.
            if (isDecay)
                return;
            var meter = _battleUI?.MeterTransform;
            Play(
                newValue > oldValue
                    ? BattleAudioTrigger.SupportGained
                    : BattleAudioTrigger.SupportLost,
                meter
            );
            int delta = newValue - oldValue;
            if (delta != 0)
                FloatingTextManager.Instance?.Show(
                    (delta > 0 ? "+" : "") + delta,
                    meter,
                    _supportColor
                );
        }

        private void OnHostilityChanged(HostilityChangedEvent evt)
        {
            // Player hostility (index -1) has no enemy slot to anchor the cue to.
            int delta = evt.NewValue - evt.OldValue;
            var enemies = _battleManager?.Enemies;
            if (evt.EnemyIndex < 0 || delta == 0 || enemies == null || evt.EnemyIndex >= enemies.Count)
                return;

            var slot = _battleUI?.GetEnemySlotTransform(evt.EnemyIndex);
            int zone = enemies[evt.EnemyIndex].Stats.NeutralZone;
            int was = Stance(evt.OldValue, zone);
            int now = Stance(evt.NewValue, zone);
            string amount = delta.ToString("+0;-0");

            if (was == now)
            {
                Play(BattleAudioTrigger.EnemyHostilityChanged, slot);
                FloatingTextManager.Instance?.Show(
                    amount,
                    slot,
                    delta > 0 ? _hostileColor : _receptiveColor
                );
                return;
            }

            // Stance flip — the moment the room changes. One label (word + amount) so a
            // Turncoat doesn't stack a second "Hostile!" on top of itself.
            var (trigger, word, color) =
                now > 0
                    ? was < 0
                        ? (BattleAudioTrigger.EnemyTurncoat, "Turncoat!", _hostileColor)
                        : (BattleAudioTrigger.EnemyBecameHostile, "Hostile!", _hostileColor)
                : now < 0 ? (BattleAudioTrigger.EnemyBecameReceptive, "Receptive!", _receptiveColor)
                : (BattleAudioTrigger.EnemyBecameNeutral, "Neutral", _neutralColor);
            Play(trigger, slot);
            FloatingTextManager.Instance?.Show($"{word} {amount}", slot, color);
            ShakeEnemy(slot, trigger == BattleAudioTrigger.EnemyTurncoat ? 1f : 0.6f);
        }

        /// <summary>Same rule as <c>BattleStats.IsHostile/IsReceptive</c>: 1 / -1 / 0 (neutral).</summary>
        private static int Stance(int hostility, int neutralZone) =>
            hostility > neutralZone ? 1
            : hostility < -neutralZone ? -1
            : 0;

        /// <summary>Rotation shake + scale punch — rotation/scale, because the enemy row's
        /// layout group owns slot positions.</summary>
        private static void ShakeEnemy(RectTransform slot, float strength)
        {
            if (slot == null)
                return;
            slot.DOComplete();
            slot.DOShakeRotation(0.45f, new Vector3(0f, 0f, 14f * strength), vibrato: 14)
                .SetLink(slot.gameObject);
            slot.DOPunchScale(Vector3.one * 0.18f * strength, 0.45f, vibrato: 8)
                .SetLink(slot.gameObject);
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

        /// <summary><see cref="Play"/> at a world point instead of a UI element.</summary>
        private void PlayAtWorld(BattleAudioTrigger trigger, Vector3 worldPos)
        {
            if (_soundMap == null || !_soundMap.TryGet(trigger, out var entry))
                return;
            entry.Sound?.Play();
            if (entry.Visual != null)
                VFXManager.Instance?.PlayAtWorld(entry.Visual, worldPos);
        }

        #endregion
    }
}
