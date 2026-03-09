using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Managers;
using Crookedile.Utilities;

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
        // ─── Inspector ────────────────────────────────────────────────────────────

        [Header("Transforms")]
        [Tooltip("Root battle canvas. The discarding card is re-parented here so it can\n" +
                 "fly past the hand-container clip boundary without being cropped.")]
        [SerializeField] private Canvas _rootCanvas;

        [Tooltip("Seconds between successive card launches in a draw batch.\n" +
                 "Set to 0 to launch all cards simultaneously.")]
        [SerializeField] private float _drawStaggerDelay = 0.4f;

        [Header("Discard Settings")]
        [Tooltip("Total duration (seconds) of the fly-to-discard animation.")]
        [SerializeField] private float _discardDuration = 0.28f;

        // ─── Draw API ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Snaps every button in <paramref name="buttons"/> to the deck position at small scale,
        /// then lets <c>CardButton.Update()</c>'s lerp carry each one to its pre-set arc target.
        /// Call this immediately after <c>CardHandLayout.ArrangeCards()</c> so the arc targets
        /// are already established.
        /// </summary>
        public void AnimateDrawIn(List<CardButton> buttons, Transform handContainer)
        {
            if (handContainer == null) return;
            StartCoroutine(StaggeredDraw(buttons, handContainer));
        }

        // ─── Discard API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Flies <paramref name="btn"/> from its current position to the discard pile,
        /// shrinking it to zero.  <paramref name="onComplete"/> is invoked when finished
        /// (use it to return the button to <see cref="BattlePoolManager"/>).
        /// Falls through immediately if either transform is unassigned.
        /// </summary>
        public void AnimateDiscardOut(CardButton btn, Action onComplete)
        {
            StartCoroutine(DiscardFlyRoutine(btn, onComplete));
        }

        // ─── Internal — Draw ──────────────────────────────────────────────────────

        

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
                if (btn != null) btn.transform.localScale = Vector3.zero;
            }

            foreach (var btn in snapshot)
            {
                if (btn == null) continue;
                
                if (_rootCanvas != null)
                    btn.transform.SetParent(FeedbackManager.Instance.CardHandParent, false);

                FeedbackManager.Instance.Play("DrawHand");
                GameLogger.LogVerbose("Card", $"Draw feedback played for '{btn.CardData?.CardName}'", this);
                btn.gameObject.SetActive(true);
                yield return new WaitForSeconds(_drawStaggerDelay);
                btn.transform.localScale = Vector3.one;
                btn.transform.SetParent(container, false);
                btn.transform.SetSiblingIndex(0);   // enter behind all existing cards; ArrangeCards restores proper z-order
                container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
            }

            container.GetComponent<CardHandLayout>()?.ArrangeCards(buttons);
        }

        // ─── Internal — Discard ───────────────────────────────────────────────────

        private IEnumerator DiscardFlyRoutine(CardButton btn, Action onComplete)
        {
            // Disable CardButton so its Update() lerp doesn't fight our position coroutine.
            btn.enabled = false;

            GameLogger.LogInfo("Card", $"Discard animation started for '{btn.CardData?.CardName}'", this);

            // Re-parent to root canvas PRESERVING world position so the card stays visually
            // in place. worldPositionStays: true avoids anchor / coordinate-space mismatch
            // and means we can lerp transform.position directly without any conversion.
            if (_rootCanvas != null)
                btn.transform.SetParent(FeedbackManager.Instance.CardHandParent, false);
            // Prevent hover / drag interactions while flying.
            if (btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = false;

            FeedbackManager.Instance.Play("DiscardHand");

            yield return new WaitForSeconds(_discardDuration);
            GameLogger.LogVerbose("Card", $"Discard animation complete for '{btn.CardData?.CardName}'", this);
            btn.enabled = true;
            onComplete?.Invoke();
        }
    }
}
