using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Handles card draw, discard, and grant fly animations.
    ///
    /// Draw:  <see cref="AnimateDrawIn"/> hides every new button, then reveals them one by
    ///        one with a scale-in pop and re-runs the arc layout after each reveal, creating
    ///        a "deal from deck" effect. The first card launches immediately; the stagger
    ///        delay applies between cards.
    ///
    /// Discard: <see cref="AnimateDiscardOut"/> flies the card from its current position to
    ///          <see cref="_discardTransform"/> while shrinking to zero. World-space
    ///          <c>transform.position</c> is used — no anchor arithmetic, no re-parenting.
    ///
    /// Grant:  <see cref="AnimateCardGranted"/> pops the card in at screen centre, holds it
    ///         so the player can read it, then flies it to the target zone. Requests are
    ///         queued so simultaneous grants play one after another instead of overlapping.
    ///
    /// Setup:
    ///   1. Add this component to a scene GameObject (e.g. a child of BattleUI).
    ///   2. Assign <see cref="_rootCanvas"/> to the root battle Canvas (grant centring).
    ///   3. Assign <see cref="_discardTransform"/> to the discard pile button's RectTransform.
    ///   4. Tune the draw/discard parameters in the Inspector.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class CardFlyAnimator : Singleton<CardFlyAnimator>
    {
        #region Inspector
        [Header("Transforms")]
        [Tooltip("Root battle canvas. Granted cards are centred on it during the hold phase.")]
        [SerializeField]
        private Canvas _rootCanvas;

        [Tooltip("Discard pile button RectTransform — fly target for discard animations.")]
        [SerializeField]
        private RectTransform _discardTransform;

        [Header("Draw Settings")]
        [Tooltip(
            "Seconds between successive card launches in a draw batch.\n"
                + "Set to 0 to launch all cards simultaneously."
        )]
        [SerializeField]
        private float _drawStaggerDelay = 0.1f;

        [Header("Discard Settings")]
        [Tooltip("Total duration (seconds) of the fly-to-discard animation.")]
        [SerializeField]
        private float _discardDuration = 0.28f;

        [Header("Card Grant Settings")]
        [Tooltip("Seconds to scale the card in from zero at the start of the grant animation.")]
        [SerializeField]
        private float _grantScaleInDuration = 0.15f;

        [Tooltip("Seconds the granted card is held at full size before flying to the zone.")]
        [SerializeField]
        private float _grantHoldDuration = 0.7f;

        [Tooltip("Seconds for the card to fly from screen center to the target zone.")]
        [SerializeField]
        private float _grantFlyDuration = 0.35f;

        #endregion

        #region Runtime
        private readonly Queue<(CardButton btn, Transform zone, Action onArrival)> _grantQueue =
            new Queue<(CardButton, Transform, Action)>();
        private bool _grantRunning;

        #endregion

        #region Draw API
        /// <summary>
        /// Reveals each button in <paramref name="buttons"/> with a staggered scale-in pop,
        /// re-running the arc layout after each reveal.
        /// Call this immediately after <c>CardHandLayout.ArrangeCards()</c> so the arc targets
        /// are already established.
        /// </summary>
        public void AnimateDrawIn(List<CardButton> buttons, Transform handContainer)
        {
            if (handContainer == null)
                return;
            StaggeredDraw(buttons, handContainer).Forget();
        }

        #endregion

        #region Discard API
        /// <summary>
        /// Flies <paramref name="btn"/> from its current position to the discard pile,
        /// shrinking it to zero.  <paramref name="onComplete"/> is invoked when finished
        /// (use it to return the button to <see cref="BattlePoolManager"/>).
        /// Shrinks in place if <see cref="_discardTransform"/> is unassigned.
        /// </summary>
        public void AnimateDiscardOut(CardButton btn, Action onComplete)
        {
            btn.enabled = false;
            if (btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = false;

            btn.transform.DOKill();

            var seq = DOTween.Sequence().SetLink(btn.gameObject);
            if (_discardTransform != null)
                seq.Join(
                    btn.transform.DOMove(_discardTransform.position, _discardDuration)
                        .SetEase(Ease.InQuad)
                );
            seq.Join(btn.transform.DOScale(0f, _discardDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                GameLogger.LogVerbose(
                    "Card",
                    $"Discard animation complete for '{btn.CardData?.CardName}'",
                    this
                );
                btn.transform.localScale = Vector3.one;
                btn.enabled = true;
                if (cg != null)
                    cg.blocksRaycasts = true;
                onComplete?.Invoke();
            });
        }

        #endregion

        #region Card Grant API
        /// <summary>
        /// Shows <paramref name="btn"/> at the center of the battle canvas with a pop scale-in,
        /// holds it so the player can read it, then flies it to <paramref name="targetZone"/>
        /// while shrinking to zero.
        /// <paramref name="onArrival"/> is invoked once the card reaches the zone
        /// (use it to bump the count text and return the button to the pool).
        /// Requests are queued: simultaneous grants play sequentially rather than stacking.
        /// </summary>
        public void AnimateCardGranted(CardButton btn, Transform targetZone, Action onArrival)
        {
            // Park the card hidden until its turn in the queue comes up.
            btn.gameObject.SetActive(false);
            _grantQueue.Enqueue((btn, targetZone, onArrival));
            if (!_grantRunning)
                ProcessGrantQueue().Forget();
        }

        private async UniTaskVoid ProcessGrantQueue()
        {
            _grantRunning = true;
            try
            {
                while (_grantQueue.Count > 0)
                {
                    var (btn, zone, onArrival) = _grantQueue.Dequeue();
                    if (btn == null)
                        continue;

                    var completion = new UniTaskCompletionSource();
                    PlayGrantAnimation(btn, zone, () =>
                    {
                        onArrival?.Invoke();
                        completion.TrySetResult();
                    });
                    await completion.Task.AttachExternalCancellation(
                        this.GetCancellationTokenOnDestroy()
                    );
                }
            }
            finally
            {
                _grantRunning = false;
            }
        }

        private void PlayGrantAnimation(CardButton btn, Transform targetZone, Action onArrival)
        {
            btn.enabled = false;
            if (btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = false;

            var rt = btn.GetComponent<RectTransform>();
            if (_rootCanvas != null)
                btn.transform.SetParent(_rootCanvas.transform, false);
            rt.anchoredPosition = Vector2.zero;
            btn.transform.localScale = Vector3.zero;
            btn.gameObject.SetActive(true);

            Vector3 endPos = targetZone != null ? targetZone.position : btn.transform.position;

            DOTween
                .Sequence()
                .SetLink(gameObject)
                // Phase 1: pop scale-in with overshoot (0 → 1.1 → 1.0)
                .Append(
                    btn.transform.DOScale(1.1f, _grantScaleInDuration * 0.8f).SetEase(Ease.Linear)
                )
                .Append(
                    btn.transform.DOScale(1f, _grantScaleInDuration * 0.2f).SetEase(Ease.Linear)
                )
                // Phase 2: hold so the player can read the card
                .AppendCallback(() =>
                {
                    GameLogger.LogInfo(
                        "Card",
                        $"Grant animation holding for '{btn.CardData?.CardName}'",
                        this
                    );
                })
                .AppendInterval(_grantHoldDuration)
                // Phase 3: fly to zone while shrinking (ease-in² = accelerates toward zone)
                .Append(btn.transform.DOMove(endPos, _grantFlyDuration).SetEase(Ease.InQuad))
                .Join(btn.transform.DOScale(0f, _grantFlyDuration))
                .OnComplete(() =>
                {
                    btn.gameObject.SetActive(false);
                    btn.transform.localScale = Vector3.one;
                    btn.enabled = true;
                    if (cg != null)
                        cg.blocksRaycasts = true;
                    GameLogger.LogInfo(
                        "Card",
                        $"Grant animation complete for '{btn.CardData?.CardName}'",
                        this
                    );
                    onArrival?.Invoke();
                });
        }

        #endregion

        #region Internal — Draw
        private async UniTaskVoid StaggeredDraw(List<CardButton> buttons, Transform container)
        {
            var ct = this.GetCancellationTokenOnDestroy();
            // Snapshot the list immediately — the live _activeButtons list may be mutated
            // by ClearHand() or ExtractCard() while the coroutine is yielding.
            var snapshot = new CardButton[buttons.Count];
            buttons.CopyTo(snapshot);

            // Immediately hide all cards before the first yield so there is no frame where
            // cards are visible at their final arc positions before the stagger launches them.
            foreach (var btn in snapshot)
            {
                if (btn != null)
                    btn.transform.localScale = Vector3.zero;
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                var btn = snapshot[i];
                if (btn == null)
                    continue;

                // Stagger between cards only — the first card launches immediately so the
                // hand starts responding the same frame the draw begins.
                if (i > 0 && _drawStaggerDelay > 0f)
                    await UniTask.WaitForSeconds(_drawStaggerDelay, cancellationToken: ct);

                // A refresh/ClearHand during the stagger can return this button to the pool
                // (reparented away from the hand container). Skipping here prevents the classic
                // "active card parented under the pool" — we must not re-activate a recycled card.
                if (btn == null || btn.transform.parent != container)
                    continue;

                GameLogger.LogVerbose(
                    "Card",
                    $"Draw animation started for '{btn.CardData?.CardName}'",
                    this
                );
                btn.gameObject.SetActive(true);
                btn.transform.localScale = Vector3.zero;
                btn.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);
                btn.transform.SetSiblingIndex(0); // enter behind all existing cards; ArrangeCards restores proper z-order
                container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
            }

            container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
        }

        #endregion
    }
}
