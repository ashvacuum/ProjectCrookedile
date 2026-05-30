using System;
using System.Collections;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Utilities;
using DG.Tweening;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Handles card draw and discard fly animations.
    ///
    /// Draw:  after <see cref="CardHandLayout"/> positions each card at its arc slot,
    ///        <see cref="AnimateDrawIn"/> snaps every button back to the deck position
    ///        (tiny scale).  <c>CardButton.Update()</c>'s existing lerp then carries each
    ///        card smoothly to its target, creating a "deal from deck" fan-out effect.
    ///
    /// Discard: <see cref="AnimateDiscardOut"/> re-parents the played card to the root
    ///          canvas (so it can fly past the hand-container clip boundary), then runs a
    ///          coroutine that moves it to the discard pile position while shrinking to zero.
    ///          World-space <c>transform.position</c> is used throughout — no anchor arithmetic,
    ///          works correctly for both Screen Space Overlay and Screen Space Camera canvases.
    ///
    /// Setup:
    ///   1. Add this component to a scene GameObject (e.g. a child of BattleUI).
    ///   2. Assign <see cref="_deckTransform"/> to the deck pile button's RectTransform.
    ///   3. Assign <see cref="_discardTransform"/> to the discard pile button's RectTransform.
    ///   4. Assign <see cref="_rootCanvas"/> to the root battle Canvas.
    ///   5. Tune the draw/discard parameters in the Inspector.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class CardFlyAnimator : Singleton<CardFlyAnimator>
    {
        #region Inspector
        [Header("Transforms")]
        [Tooltip(
            "Root battle canvas. The discarding card is re-parented here so it can\n"
                + "fly past the hand-container clip boundary without being cropped."
        )]
        [SerializeField]
        private Canvas _rootCanvas;

        [Tooltip(
            "Intermediate parent for cards during draw/discard animations — sits outside "
                + "the hand container so cards aren't clipped."
        )]
        [SerializeField]
        private Transform _cardHandParent;

        [Tooltip(
            "Seconds between successive card launches in a draw batch.\n"
                + "Set to 0 to launch all cards simultaneously."
        )]
        [SerializeField]
        private float _drawStaggerDelay = 0.4f;

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

        #region Draw API
        /// <summary>
        /// Snaps every button in <paramref name="buttons"/> to the deck position at small scale,
        /// then lets <c>CardButton.Update()</c>'s lerp carry each one to its pre-set arc target.
        /// Call this immediately after <c>CardHandLayout.ArrangeCards()</c> so the arc targets
        /// are already established.
        /// </summary>
        public void AnimateDrawIn(List<CardButton> buttons, Transform handContainer)
        {
            if (handContainer == null)
                return;
            StartCoroutine(StaggeredDraw(buttons, handContainer));
        }

        #endregion

        #region Discard API
        /// <summary>
        /// Flies <paramref name="btn"/> from its current position to the discard pile,
        /// shrinking it to zero.  <paramref name="onComplete"/> is invoked when finished
        /// (use it to return the button to <see cref="BattlePoolManager"/>).
        /// Falls through immediately if either transform is unassigned.
        /// </summary>
        public void AnimateDiscardOut(CardButton btn, Action onComplete)
        {
            btn.enabled = false;
            if (btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = false;

            if (_cardHandParent != null)
                btn.transform.SetParent(_cardHandParent, false);

            DOVirtual
                .DelayedCall(
                    _discardDuration,
                    () =>
                    {
                        GameLogger.LogVerbose(
                            "Card",
                            $"Discard animation complete for '{btn.CardData?.CardName}'",
                            this
                        );
                        btn.enabled = true;
                        onComplete?.Invoke();
                    }
                )
                .SetLink(gameObject);
        }

        #endregion

        #region Card Grant API
        /// <summary>
        /// Shows <paramref name="btn"/> at the center of the battle canvas with a pop scale-in,
        /// holds it so the player can read it, plays "GrantCard" feedback, then flies it to
        /// <paramref name="targetZone"/> while shrinking to zero.
        /// <paramref name="onArrival"/> is invoked once the card reaches the zone
        /// (use it to bump the count text and return the button to the pool).
        /// </summary>
        public void AnimateCardGranted(CardButton btn, Transform targetZone, Action onArrival)
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
                // Phase 2: hold; play feedback at the start
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

        #region Internal — Draw
        private IEnumerator StaggeredDraw(List<CardButton> buttons, Transform container)
        {
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

            foreach (var btn in snapshot)
            {
                if (btn == null)
                    continue;

                if (_cardHandParent != null)
                    btn.transform.SetParent(_cardHandParent, false);

                GameLogger.LogVerbose(
                    "Card",
                    $"Draw animation started for '{btn.CardData?.CardName}'",
                    this
                );
                btn.gameObject.SetActive(true);
                yield return new WaitForSeconds(_drawStaggerDelay);
                btn.transform.localScale = Vector3.zero;
                btn.transform.SetParent(container, false);
                btn.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack);
                btn.transform.SetSiblingIndex(0); // enter behind all existing cards; ArrangeCards restores proper z-order
                container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
            }

            container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
        }
    }
}
        #endregion
        #endregion
