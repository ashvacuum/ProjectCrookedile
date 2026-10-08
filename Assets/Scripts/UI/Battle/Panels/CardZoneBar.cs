using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns the card-zone bar: the deck/discard/exhaust buttons and counts, the
    /// card-granted flight animation with its count punch, and the zone-viewer popups.
    /// Extracted from BattleUI.
    ///
    /// Self-subscribes to CardGranted/CardExhausted; BattleUI drives the coalesced
    /// <see cref="RefreshCounts"/> from its stats refresh.
    /// </summary>
    public class CardZoneBar : BattlePanel
    {
        [Header("Zone Buttons")]
        [SerializeField]
        private Button discardZoneButton;

        [SerializeField]
        private Button exhaustZoneButton;

        [SerializeField]
        private Button deckZoneButton;

        [Header("Zone Counts")]
        [SerializeField]
        private TMP_Text discardCountText;

        [SerializeField]
        private TMP_Text exhaustCountText;

        [SerializeField]
        private TMP_Text deckCountText;

        [Header("Zone Viewer")]
        [SerializeField]
        private CardZonePanel cardZonePanel;

        [Header("Card Grant Animation")]
        [Tooltip("Seconds for the zone count text to scale up on card grant arrival.")]
        [SerializeField]
        private float _countPunchDuration = 0.25f;

        [Tooltip("Scale multiplier applied to the count text at the peak of the punch.")]
        [SerializeField]
        private float _countPunchScale = 1.4f;

        /// <summary>
        /// Cards the model has drawn that haven't left the deck on screen yet. The deck counter
        /// shows them as still in the pile, so it ticks down as each card flies out.
        /// </summary>
        private int _drawsAwaitingLaunch;

        #region Lifecycle / events

        private void Awake()
        {
            discardZoneButton?.onClick.AddListener(ShowDiscardZone);
            exhaustZoneButton?.onClick.AddListener(ShowExhaustZone);
            deckZoneButton?.onClick.AddListener(ShowDeckZone);
        }

        private void Start()
        {
            // Start, not Awake: every singleton has registered by now.
            if (CardFlyAnimator.Instance != null && deckZoneButton != null)
                CardFlyAnimator.Instance.DeckTransform = deckZoneButton.transform;
        }

        private void OnEnable()
        {
            On<CardGrantedEvent>(OnCardGranted);
            On<CardExhaustedEvent>(OnCardExhausted);
            On<CardDrawnEvent>(OnCardDrawn);
            On<DrawnCardLaunchedEvent>(OnDrawnCardLaunched);
            On<DeckReshuffledEvent>(OnDeckReshuffled);
        }

        private void OnCardGranted(CardGrantedEvent evt)
        {
            if (!evt.IsPlayer)
                return;
            Transform target = evt.ToDiscard
                ? discardZoneButton.transform
                : deckZoneButton.transform;
            TMP_Text counter = evt.ToDiscard ? discardCountText : deckCountText;
            CardGrantedAnimationSequence(evt.Card, target, counter).Forget();
        }

        private void OnCardExhausted(CardExhaustedEvent evt)
        {
            // Ensure exhaust count is always up-to-date regardless of trigger source
            // (ExhaustFromDiscard does not pass through CardPlayedEvent → stats refresh).
            if (!evt.IsPlayer)
                return;
            RefreshCounts();
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (evt.IsPlayer)
                _drawsAwaitingLaunch++;
        }

        private void OnDrawnCardLaunched(DrawnCardLaunchedEvent evt)
        {
            _drawsAwaitingLaunch = Mathf.Max(0, _drawsAwaitingLaunch - 1);
            RefreshCounts();
            PunchCountText(deckCountText);
        }

        private void OnDeckReshuffled(DeckReshuffledEvent evt)
        {
            if (!evt.IsPlayer || Battle == null)
                return;
            RefreshCounts();
            PunchCountText(discardCountText);
            if (CardFlyAnimator.Instance == null)
            {
                PunchCountText(deckCountText);
                return;
            }
            CardFlyAnimator.Instance.AnimateReshuffle(
                discardZoneButton != null ? discardZoneButton.transform : null,
                deckZoneButton != null ? deckZoneButton.transform : null,
                evt.Count,
                Battle.PlayerOrigin,
                () => PunchCountText(deckCountText)
            );
        }

        #endregion

        #region Public API

        /// <summary>
        /// Repaints the three zone counters. Called from BattleUI's coalesced stats
        /// refresh and after grant/exhaust events land.
        /// </summary>
        public void RefreshCounts()
        {
            DeckManager deck = Battle?.PlayerDeck;
            if (deck == null)
                return;
            if (discardCountText != null)
                discardCountText.text = deck.DiscardCount.ToString();
            if (exhaustCountText != null)
                exhaustCountText.text = deck.ExhaustCount.ToString();

            // ponytail: self-heals rather than tracking every exit path. Off-turn the hand is
            // discarded or cleared, so nothing is waiting to launch; and you can never wait on more
            // cards than are in hand. Exact per-card bookkeeping only if the counter visibly drifts.
            if (!Battle.IsPlayerTurn)
                _drawsAwaitingLaunch = 0;
            _drawsAwaitingLaunch = Mathf.Min(_drawsAwaitingLaunch, deck.HandCount);
            if (deckCountText != null)
                deckCountText.text = (deck.DeckCount + _drawsAwaitingLaunch).ToString();
        }

        #endregion

        #region Grant animation

        /// <summary>
        /// Rents a card button, initialises it display-only, then asks CardFlyAnimator to
        /// show it at screen centre and fly it to the target zone. On arrival the count
        /// text receives a scale-punch and the button is returned to the pool.
        /// </summary>
        private async UniTaskVoid CardGrantedAnimationSequence(
            CardData card,
            Transform targetZone,
            TMP_Text countText
        )
        {
            var btn = BattlePoolManager.Instance?.RentCard(card.CardType, transform);
            if (btn == null)
            {
                RefreshCounts();
                return;
            }

            int ap = Battle?.PlayerStats.CurrentActionPoints ?? 0;
            int cost = Battle?.GetEffectiveCardCost(card) ?? 1;
            btn.Initialize(card, 0, ap, cost, forceUnplayable: true);

            if (CardFlyAnimator.Instance == null)
            {
                // No animator — skip the flight, count the card and return the button.
                RefreshCounts();
                BattlePoolManager.Instance?.ReturnCard(btn);
                return;
            }

            var arrived = new UniTaskCompletionSource();
            CardFlyAnimator.Instance.AnimateCardGranted(
                btn,
                targetZone,
                () =>
                {
                    RefreshCounts();
                    PunchCountText(countText);
                    BattlePoolManager.Instance?.ReturnCard(btn);
                    arrived.TrySetResult();
                }
            );

            await arrived.Task.AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
        }

        private void PunchCountText(TMP_Text text)
        {
            if (text == null)
                return;
            text.transform.DOKill();
            text.transform.DOScale(Vector3.one * _countPunchScale, _countPunchDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    text
                        .transform.DOScale(Vector3.one, _countPunchDuration * 0.5f)
                        .SetEase(Ease.InQuad)
                )
                .SetLink(gameObject);
        }

        #endregion

        #region Zone viewers

        private void ShowDiscardZone()
        {
            if (cardZonePanel == null || Battle?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Discard Pile", Battle.PlayerDeck.DiscardPile);
        }

        private void ShowExhaustZone()
        {
            if (cardZonePanel == null || Battle?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Exhaust Pile", Battle.PlayerDeck.ExhaustPile);
        }

        private void ShowDeckZone()
        {
            if (cardZonePanel == null || Battle?.PlayerDeck == null)
                return;

            // Shuffle display copy — don't reveal the real draw order.
            var display = new List<CardData>(Battle.PlayerDeck.DrawPile);
            for (int i = display.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (display[i], display[j]) = (display[j], display[i]);
            }

            cardZonePanel.Open("Draw Pile", display);
        }

        #endregion
    }
}
