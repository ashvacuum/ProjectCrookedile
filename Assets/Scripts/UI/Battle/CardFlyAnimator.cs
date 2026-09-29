using System;
using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.VFX;
using Crookedile.Managers;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Published each time a drawn card leaves the deck on screen. The draw pile counter ticks
    /// down on this rather than when the model draws, so the number matches what you see.
    /// </summary>
    public struct DrawnCardLaunchedEvent : IGameEvent { }

    /// <summary>
    /// Handles card draw, reshuffle, discard, and grant fly animations.
    ///
    /// Draw:  <see cref="AnimateDrawIn"/> launches new cards one at a time from
    ///        <see cref="DeckTransform"/>, face-down and small. Each flips face-up in flight
    ///        and glides into its slot, and the hand re-fans to make room as each card
    ///        arrives. Every card chases its slot with a critically damped spring, so a
    ///        re-fan retargets cards already in flight without a hitch.
    ///
    /// Reshuffle: <see cref="AnimateReshuffle"/> arcs a few card backs from discard to deck.
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
    ///   4. Assign <see cref="_visualSettings"/> (origin card backs for reshuffle ghosts).
    ///   5. Tune the draw/discard parameters in the Inspector.
    ///   <see cref="DeckTransform"/> is supplied at runtime by <see cref="CardZoneBar"/>.
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
            "Seconds between successive cards leaving the deck in a draw batch.\n"
                + "Set to 0 to launch all cards simultaneously."
        )]
        [SerializeField]
        private float _drawStaggerDelay = 0.08f;

        [Tooltip(
            "Spring smoothing time (seconds) for cards gliding to their hand slot, including "
                + "the re-fan as each new card arrives. Lower = snappier; a card settles in "
                + "roughly 3x this."
        )]
        [SerializeField]
        [Min(0.01f)]
        private float _drawSmoothTime = 0.09f;

        [Tooltip("Scale a drawn card starts at as it leaves the deck.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float _drawStartScale = 0.35f;

        [Tooltip(
            "Seconds for a drawn card to flip from its back to its face, starting at launch. "
                + "0 = drawn cards fly face-up."
        )]
        [SerializeField]
        [Min(0f)]
        private float _drawFlipDuration = 0.22f;

        [Header("Reshuffle Settings")]
        [Tooltip("Source of the player's origin card back shown on the reshuffle ghosts.")]
        [SerializeField]
        private CardVisualSettings _visualSettings;

        [Tooltip("Most card backs flown from discard to deck on a reshuffle.")]
        [SerializeField]
        [Min(0)]
        private int _reshuffleMaxGhosts = 5;

        [Tooltip("Seconds for one ghost's arc from discard to deck.")]
        [SerializeField]
        private float _reshuffleGhostDuration = 0.35f;

        [Tooltip("Seconds between successive ghosts leaving the discard pile.")]
        [SerializeField]
        private float _reshuffleGhostStagger = 0.05f;

        [Tooltip("Size of a ghost card back, in root canvas pixels.")]
        [SerializeField]
        private Vector2 _reshuffleGhostSize = new Vector2(66f, 99f);

        [Tooltip("Height of the ghost arc above the straight discard-to-deck line.")]
        [SerializeField]
        private float _reshuffleArcHeight = 80f;

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

        [Header("Fly Trail")]
        [Tooltip(
            "VFX played as a child of the card for the duration of a fly, so it rides along "
                + "with the move tween. Leave empty for no trail."
        )]
        [SerializeField]
        private VFXEvent _flyTrail;

        #endregion

        #region Runtime
        private readonly Queue<(CardButton btn, Transform zone, Action onArrival)> _grantQueue =
            new Queue<(CardButton, Transform, Action)>();
        private bool _grantRunning;

        /// <summary>The draw in progress, or null. The loop exits as soon as this stops being its run.</summary>
        private DrawRun _draw;

        /// <summary>Hard stop for a draw loop that never settles (e.g. a slot that keeps moving).</summary>
        private const float MaxDrawSeconds = 3f;

        /// <summary>Draw pile button — where drawn cards fly from. Set by <see cref="CardZoneBar"/>.</summary>
        public Transform DeckTransform { get; set; }

        private sealed class DrawCard
        {
            public CardButton Btn;
            public bool IsNew;
            public bool Launched;
            public bool Landed;
            public float LaunchTime;
            public Vector3 TargetPos;
            public float TargetAngle;
            public Vector3 Velocity;
            public float AngleVelocity;
            public float Scale;
            public float ScaleVelocity;
            public VFXAnimatedImage Trail;
        }

        private sealed class DrawRun
        {
            public CardHandLayout Layout;

            /// <summary>Whole hand, left to right — the order slots are assigned in.</summary>
            public List<DrawCard> Cards;
        }

        #endregion

        #region Fly Trail
        /// <summary>
        /// Spawns <see cref="_flyTrail"/> as a child of <paramref name="card"/> so the pooled VFX
        /// image follows the move tween without any per-frame position copying. Returns null when
        /// no trail is assigned; callers must stop the returned instance with
        /// <see cref="VFXAnimatedImage.OnAnimationComplete"/> when the fly ends, since the clip
        /// length and the tween duration are authored independently.
        /// </summary>
        // ponytail: trail is a child of the card, so it shrinks with the DOScale(0) on discard/grant
        // flies. Reads as the trail collapsing into the pile. Parent it to the VFX canvas and copy
        // position per frame if you ever want it to stay full-size and lag behind the path instead.
        private VFXAnimatedImage StartTrail(Transform card)
        {
            if (_flyTrail == null || VFXManager.Instance == null)
                return null;
            return VFXManager.Instance.PlayAndGetInstance(_flyTrail, card as RectTransform);
        }

        #endregion

        #region Draw API
        /// <summary>
        /// Deals <paramref name="newCards"/> into the hand. <paramref name="allCards"/> is the
        /// whole hand left to right, including the new cards; the rest are already on screen and
        /// glide from wherever they are now. New cards launch one per
        /// <see cref="_drawStaggerDelay"/> from <see cref="DeckTransform"/>, and each launch
        /// re-fans the visible hand, so the hand grows one slot at a time.
        ///
        /// Cards are placed at PostLateUpdate, after DOTween's Update pass, so a stray hover
        /// tween can't pull a card out of the deal. A draw already running is finished first.
        /// New cards ignore the pointer until the deal ends.
        /// </summary>
        public void AnimateDrawIn(
            List<CardButton> allCards,
            List<CardButton> newCards,
            CardHandLayout layout
        )
        {
            FinishDraw();

            var run = new DrawRun { Layout = layout, Cards = new List<DrawCard>(allCards.Count) };
            var launchOrder = new List<DrawCard>(newCards.Count);
            foreach (var btn in allCards)
            {
                if (btn == null)
                    continue;
                bool isNew = newCards.Contains(btn);
                var card = new DrawCard
                {
                    Btn = btn,
                    IsNew = isNew,
                    Launched = !isNew,
                    Landed = !isNew,
                    Scale = btn.transform.localScale.y,
                };
                run.Cards.Add(card);
                btn.gameObject.SetActive(!isNew); // new cards wait, hidden, until launched
                if (isNew)
                    launchOrder.Add(card);
            }

            if (layout == null || launchOrder.Count == 0)
            {
                _draw = run;
                FinishDraw();
                return;
            }

            _draw = run;
            RunDraw(run, launchOrder).Forget();
        }

        private async UniTaskVoid RunDraw(DrawRun run, List<DrawCard> launchOrder)
        {
            float elapsed = 0f;
            int next = 0;
            bool first = true;
            while (_draw == run)
            {
                float dt = first ? 0f : Time.deltaTime;
                first = false;
                elapsed += dt;

                int launchedNow = next;
                while (next < launchOrder.Count && elapsed >= next * _drawStaggerDelay)
                    launchOrder[next++].Launched = true;
                if (next > launchedNow)
                {
                    Refan(run);
                    for (int i = launchedNow; i < next; i++)
                        Launch(launchOrder[i], run.Layout.transform, elapsed);
                }

                bool settled = next == launchOrder.Count;
                foreach (var card in run.Cards)
                    if (card.Launched && card.Btn != null)
                        settled &= StepCard(card, dt, elapsed);

                if (settled || elapsed > MaxDrawSeconds)
                    break;

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
                if (this == null)
                    return;
            }

            if (_draw == run)
                FinishDraw();
        }

        /// <summary>
        /// Re-slots the launched cards as a hand of their own size, keeping hand order, and
        /// restacks siblings: landed cards left to right, cards still in flight on top.
        /// </summary>
        private static void Refan(DrawRun run)
        {
            var visible = run.Cards.FindAll(c => c.Launched && c.Btn != null);
            var slots = run.Layout.ComputeSlots(visible.Count);
            for (int i = 0; i < visible.Count; i++)
            {
                visible[i].TargetPos = slots[i].pos;
                visible[i].TargetAngle = slots[i].angle;
                visible[i].Btn.transform.SetSiblingIndex(i);
            }
            foreach (var card in visible)
                if (!card.Landed)
                    card.Btn.transform.SetAsLastSibling();
        }

        private void Launch(DrawCard card, Transform container, float elapsed)
        {
            var t = card.Btn.transform;
            card.LaunchTime = elapsed;
            card.Scale = _drawStartScale;
            // ponytail: with no deck button bound, cards rise from just below their slot.
            t.localPosition =
                DeckTransform != null
                    ? container.InverseTransformPoint(DeckTransform.position)
                    : card.TargetPos + Vector3.down * 250f;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * _drawStartScale;
            card.Btn.SetFaceDown(_drawFlipDuration > 0f);
            if (card.Btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = false;
            card.Btn.gameObject.SetActive(true);
            card.Trail = StartTrail(t);
            EventBus.Publish(new DrawnCardLaunchedEvent());
        }

        /// <summary>Springs one card toward its slot for a frame; true once it is at rest.</summary>
        private bool StepCard(DrawCard card, float dt, float elapsed)
        {
            var t = card.Btn.transform;
            t.localPosition = Vector3.SmoothDamp(
                t.localPosition,
                card.TargetPos,
                ref card.Velocity,
                _drawSmoothTime,
                Mathf.Infinity,
                dt
            );
            float targetZ = -card.TargetAngle;
            float z = Mathf.SmoothDampAngle(
                t.localEulerAngles.z,
                targetZ,
                ref card.AngleVelocity,
                _drawSmoothTime,
                Mathf.Infinity,
                dt
            );
            t.localRotation = Quaternion.Euler(0f, 0f, z);
            card.Scale = Mathf.SmoothDamp(
                card.Scale,
                1f,
                ref card.ScaleVelocity,
                _drawSmoothTime,
                Mathf.Infinity,
                dt
            );

            // Flip: squash the width to zero with the back showing, swap to the face, widen again.
            float widthFactor = 1f;
            bool flipped = true;
            if (card.IsNew && _drawFlipDuration > 0f)
            {
                float p = (elapsed - card.LaunchTime) / _drawFlipDuration;
                flipped = p >= 1f;
                if (!flipped)
                    widthFactor = Mathf.Abs(Mathf.Cos(p * Mathf.PI));
                card.Btn.SetFaceDown(p < 0.5f);
            }
            t.localScale = new Vector3(card.Scale * widthFactor, card.Scale, 1f);

            bool atRest =
                flipped
                && (t.localPosition - card.TargetPos).sqrMagnitude < 1f
                && Mathf.Abs(Mathf.DeltaAngle(z, targetZ)) < 0.5f
                && Mathf.Abs(card.Scale - 1f) < 0.01f;
            if (atRest && !card.Landed)
            {
                card.Landed = true;
                StopTrail(card);
            }
            return atRest;
        }

        /// <summary>Returns a card to its resting look: face-up, full scale, clickable, no trail.</summary>
        private static void ResetDrawVisuals(DrawCard card)
        {
            StopTrail(card);
            card.Btn.SetFaceDown(false);
            card.Btn.transform.localScale = Vector3.one;
            if (card.Btn.TryGetComponent<CanvasGroup>(out var cg))
                cg.blocksRaycasts = true;
        }

        private static void StopTrail(DrawCard card)
        {
            card.Trail?.OnAnimationComplete();
            card.Trail = null;
        }

        /// <summary>
        /// Ends the draw in progress with every card snapped into its final slot, face-up and
        /// clickable. Cards still waiting in the deck appear at once. Call before anything else
        /// moves hand cards (a card being played) so the draw loop can't fight it.
        /// </summary>
        public void FinishDraw()
        {
            var run = _draw;
            if (run == null)
                return;
            _draw = null;

            var hand = new List<CardButton>(run.Cards.Count);
            foreach (var card in run.Cards)
            {
                if (card.Btn == null)
                    continue;
                if (!card.Launched)
                    EventBus.Publish(new DrawnCardLaunchedEvent());
                ResetDrawVisuals(card);
                card.Btn.gameObject.SetActive(true);
                hand.Add(card.Btn);
            }
            if (run.Layout != null)
                run.Layout.ArrangeCards(hand);
        }

        /// <summary>
        /// Stops the draw in progress without placing anything: cards are left face-up at full
        /// scale where they are. Call when the hand is being torn down (pooled or discarded) so
        /// the loop can't keep driving buttons that have been returned or re-rented.
        /// </summary>
        public void CancelDraw()
        {
            var run = _draw;
            if (run == null)
                return;
            _draw = null;
            foreach (var card in run.Cards)
                if (card.Btn != null)
                    ResetDrawVisuals(card);
        }

        #endregion

        #region Reshuffle API
        /// <summary>
        /// Arcs up to <see cref="_reshuffleMaxGhosts"/> card backs from <paramref name="from"/>
        /// (discard) to <paramref name="to"/> (deck) on the root canvas.
        /// <paramref name="onComplete"/> fires when the last one lands, or at once when there
        /// is nothing to show (no canvas, no card back, zero cards).
        /// </summary>
        // ponytail: ghosts are plain Images created and destroyed per reshuffle — a handful, a
        // few times a battle. Pool them if a profiler ever flags it.
        public void AnimateReshuffle(
            Transform from,
            Transform to,
            int count,
            OriginType origin,
            Action onComplete
        )
        {
            int ghostCount = Mathf.Min(count, _reshuffleMaxGhosts);
            Sprite back =
                _visualSettings != null ? _visualSettings.GetCardBackForOrigin(origin) : null;
            if (ghostCount <= 0 || from == null || to == null || _rootCanvas == null || back == null)
            {
                onComplete?.Invoke();
                return;
            }

            Transform parent = _rootCanvas.transform;
            Vector3 start = parent.InverseTransformPoint(from.position);
            Vector3 end = parent.InverseTransformPoint(to.position);
            var ghosts = new List<GameObject>(ghostCount);
            var seq = DOTween.Sequence().SetLink(gameObject);

            for (int i = 0; i < ghostCount; i++)
            {
                var go = new GameObject("ReshuffleGhost", typeof(RectTransform), typeof(Image));
                ghosts.Add(go);
                var rt = (RectTransform)go.transform;
                rt.SetParent(parent, false);
                rt.SetAsLastSibling();
                rt.sizeDelta = _reshuffleGhostSize;
                rt.localPosition = start;
                rt.localScale = Vector3.zero; // invisible until its turn
                var img = go.GetComponent<Image>();
                img.sprite = back;
                img.raycastTarget = false;

                float at = i * _reshuffleGhostStagger;
                float d = _reshuffleGhostDuration;
                seq.Insert(at, rt.DOScale(1f, d * 0.25f).SetEase(Ease.OutQuad));
                seq.Insert(
                    at,
                    rt.DOLocalJump(end, _reshuffleArcHeight, 1, d).SetEase(Ease.InOutSine)
                );
                seq.Insert(at, rt.DOLocalRotate(new Vector3(0f, 0f, -20f), d).SetEase(Ease.OutQuad));
                seq.Insert(at + d * 0.75f, rt.DOScale(0.5f, d * 0.25f).SetEase(Ease.InQuad));
            }

            seq.OnComplete(() => onComplete?.Invoke());
            seq.OnKill(() =>
            {
                foreach (var go in ghosts)
                    if (go != null)
                        Destroy(go);
            });
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

            var trail = StartTrail(btn.transform);
            var seq = DOTween.Sequence().SetLink(btn.gameObject);
            if (_discardTransform != null)
                seq.Join(
                    btn.transform.DOMove(_discardTransform.position, _discardDuration)
                        .SetEase(Ease.InQuad)
                );
            seq.Join(btn.transform.DOScale(0f, _discardDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                // Stop the trail BEFORE the button is pooled, otherwise VFXAnimatedImage.OnDisable
                // hits its force-complete path and logs a warning on every discard.
                trail?.OnAnimationComplete();
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
                    PlayGrantAnimation(
                        btn,
                        zone,
                        () =>
                        {
                            onArrival?.Invoke();
                            completion.TrySetResult();
                        }
                    );
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
            VFXAnimatedImage trail = null;

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
                .AppendCallback(() => trail = StartTrail(btn.transform))
                .Append(btn.transform.DOMove(endPos, _grantFlyDuration).SetEase(Ease.InQuad))
                .Join(btn.transform.DOScale(0f, _grantFlyDuration))
                .OnComplete(() =>
                {
                    trail?.OnAnimationComplete();
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
    }
}
