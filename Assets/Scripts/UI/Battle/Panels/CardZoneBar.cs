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
    public class CardZoneBar : MonoBehaviour
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

        private BattleManager _bm;

        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        /// <summary>Supplies the battle context. Called by BattleUI.Initialize.</summary>
        public void Bind(BattleManager bm) => _bm = bm;

        #region Lifecycle / events

        private void Awake()
        {
            discardZoneButton?.onClick.AddListener(ShowDiscardZone);
            exhaustZoneButton?.onClick.AddListener(ShowExhaustZone);
            deckZoneButton?.onClick.AddListener(ShowDeckZone);
        }

        private void OnEnable()
        {
            Sub<CardGrantedEvent>(OnCardGranted);
            Sub<CardExhaustedEvent>(OnCardExhausted);
        }

        private void OnDisable()
        {
            foreach (var unsub in _eventUnsubscribers)
                unsub();
            _eventUnsubscribers.Clear();
        }

        private void Sub<T>(System.Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _eventUnsubscribers.Add(() => EventBus.Unsubscribe(handler));
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

        #endregion

        #region Public API

        /// <summary>
        /// Repaints the three zone counters. Called from BattleUI's coalesced stats
        /// refresh and after grant/exhaust events land.
        /// </summary>
        public void RefreshCounts()
        {
            DeckManager deck = _bm?.PlayerDeck;
            if (deck == null)
                return;
            if (discardCountText != null)
                discardCountText.text = deck.DiscardCount.ToString();
            if (exhaustCountText != null)
                exhaustCountText.text = deck.ExhaustCount.ToString();
            if (deckCountText != null)
                deckCountText.text = deck.DeckCount.ToString();
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

            int ap = _bm?.PlayerStats.CurrentActionPoints ?? 0;
            int cost = _bm?.GetEffectiveCardCost(card) ?? 1;
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
            if (cardZonePanel == null || _bm?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Discard Pile", _bm.PlayerDeck.DiscardPile);
        }

        private void ShowExhaustZone()
        {
            if (cardZonePanel == null || _bm?.PlayerDeck == null)
                return;
            cardZonePanel.Open("Exhaust Pile", _bm.PlayerDeck.ExhaustPile);
        }

        private void ShowDeckZone()
        {
            if (cardZonePanel == null || _bm?.PlayerDeck == null)
                return;

            // Shuffle display copy — don't reveal the real draw order.
            var display = new List<CardData>(_bm.PlayerDeck.DrawPile);
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
