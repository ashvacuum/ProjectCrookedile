using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Arranges CardButtons in a true circular arc fan, like cards held in a hand.
    ///
    ///   Each card is positioned on a circle of radius <see cref="arcRadius"/>:
    ///     x = R · sin(angle)        — horizontal position from centre
    ///     y = R · (cos(angle) − 1)  — vertical dip (0 at centre, negative at edges)
    ///   Both X and Y come from the same angle, so cards actually sit on the circle.
    ///
    ///   Tuning quick-reference:
    ///     angleStepDegrees    — degrees added per card left-to-right. 4–7° feels natural.
    ///     arcRadius           — circle size. Larger = flatter arc. 600–900 is a good range.
    ///     maxAngleStepDegrees — per-card tilt clamp; prevents extreme lean with small hands.
    ///
    ///   Sibling / z order:
    ///     Cards are reordered so the centre card renders on top of its neighbours.
    ///     When the player hovers a card, CardButton brings it to the very front.
    ///
    ///   Attach to the hand container. Remove Horizontal Layout Group if present.
    ///   BattleUI calls ArrangeCards() automatically after rebuilding the hand.
    /// </summary>
    public class CardHandLayout : MonoBehaviour
    {
        [Header("Arc Shape")]
        [Tooltip("Degrees added per card stepping left-to-right. 4–7° feels natural. 0 = all upright.")]
        [SerializeField] private float angleStepDegrees = 5f;

        [Tooltip("Radius of the imaginary circle cards sit on. " +
                 "Controls how much edge cards dip below centre. Larger = flatter. Try 600–900.")]
        [SerializeField] private float arcRadius = 750f;

        [Tooltip("Maximum per-card tilt in degrees. Prevents extreme lean with a tiny hand.")]
        [SerializeField] private float maxAngleStepDegrees = 7f;

        [Header("Card Size")]
        [Tooltip("Width of a single card in canvas pixels. Used to compute visible area per card when the hand is crowded.")]
        [SerializeField] private float cardWidth = 120f;

        [Header("Lerp Animation")]
        [Tooltip("Duration in seconds for the animated card arrangement lerp.")]
        [SerializeField] private float lerpDuration = 0.2f;

        [Header("Hover Spread")]
        [Tooltip("Extra degrees added to the angle step when any card is hovered, " +
                 "making the hand fan out slightly. 1–3° feels natural.")]
        [SerializeField] private float hoverSpreadAngleBonus = 2f;

        [Header("Debug Preview")]
        [Tooltip("Draw ghost card outlines in the Scene view to preview arc layout without needing real cards in the hand.")]
        [SerializeField] private bool showDebugLayout = false;

        [Tooltip("Number of card slots to preview.")]
        [SerializeField, Range(1, 10)] private int debugPreviewCount = 5;

        [Tooltip("Card height in canvas pixels used only for the ghost outline (width comes from Card Size above).")]
        [SerializeField] private float debugCardHeight = 168f;

        [Tooltip("Outline colour for regular ghost card slots.")]
        [SerializeField] private Color debugCardColor = new Color(0.35f, 0.75f, 1f, 0.85f);

        [Tooltip("Outline colour for the centre card slot.")]
        [SerializeField] private Color debugCentreColor = new Color(1f, 0.9f, 0.2f, 0.95f);

        [Tooltip("Draw the arc curve the cards sit on.")]
        [SerializeField] private bool debugShowArc = true;

        [Tooltip("Colour of the arc curve.")]
        [SerializeField] private Color debugArcColor = new Color(1f, 1f, 1f, 0.2f);

        // ─── Runtime ──────────────────────────────────────────────────────────────

        private Sequence _lerpSequence;

        // Hover spread — cached card list so SetHoverSpread() can recompute without parameters
        private List<CardButton> _cachedCards    = new List<CardButton>();
        private bool             _hoverSpreadActive;
        private CardButton       _hoverSpreadCard;   // pivot card; cards to its left shift left, right shift right

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Position, rotate, and z-sort all cards into a fan.
        /// Pass <paramref name="animated"/> = <c>true</c> to lerp cards smoothly into position
        /// instead of snapping instantly.
        /// </summary>
        public void ArrangeCards(List<CardButton> cards, bool animated = false)
        {
            // Reset spread state and cache the new card list.
            // Any active hover-spread is cleared because a full rearrange implies
            // the hand has changed (card played, turn started, etc.).
            _hoverSpreadActive = false;
            _hoverSpreadCard   = null;
            _cachedCards = new List<CardButton>(cards);

            int count = cards.Count;
            if (count == 0) return;

            // Cancel any in-progress animation before starting a new arrangement.
            _lerpSequence?.Kill();
            _lerpSequence = null;
            foreach (var card in cards)
                if (card != null) card.transform.DOKill();

            // Compute target transforms for every card.
            var targets = new List<(CardButton btn, Vector3 pos, float angle)>(count);
            if (count == 1)
            {
                targets.Add((cards[0], Vector3.zero, 0f));
            }
            else
            {
                float effectiveStep = ComputeEffectiveStep(count);
                float startAngle    = -(count - 1) * effectiveStep * 0.5f;
                for (int i = 0; i < count; i++)
                {
                    float angleDeg = startAngle + effectiveStep * i;
                    targets.Add((cards[i], ComputeLocalPosition(angleDeg), angleDeg));
                }
            }

            // Sibling order is always applied immediately — it's render-only, not positional.
            SetSiblingOrder(cards);

            if (animated && lerpDuration > 0f)
                StartLerpSequence(targets);
            else
                foreach (var (btn, pos, angle) in targets)
                    ApplyToCard(btn, pos, angle);
        }

        // ─── Hover Spread API ─────────────────────────────────────────────────────

        /// <summary>
        /// Fans the hand apart from the hovered card when <paramref name="active"/> is true:
        /// cards to its left shift left, cards to its right shift right, creating a gap around it.
        /// When <paramref name="active"/> is false all cards return to their base arc positions.
        /// Pass <paramref name="hoveredCard"/> when activating so the layout knows the pivot.
        /// Uses <see cref="CardButton.SetLayoutTarget"/> so the existing per-card
        /// <c>Update()</c> lerp handles smooth animation — no coroutine conflict.
        /// </summary>
        public void SetHoverSpread(bool active, CardButton hoveredCard = null)
        {
            if (_hoverSpreadActive == active && _hoverSpreadCard == hoveredCard) return;
            _hoverSpreadActive = active;
            _hoverSpreadCard   = active ? hoveredCard : null;
            ApplyHoverSpread();
        }

        private void ApplyHoverSpread()
        {
            int count = _cachedCards == null ? 0 : _cachedCards.Count;
            if (count == 0) return;

            if (count == 1)
            {
                // Single card — no neighbours to spread; still update base so it stays consistent.
                _cachedCards[0].SetLayoutTarget(Vector3.zero, 0f);
                return;
            }

            float step       = ComputeEffectiveStep(count);
            float startAngle = -(count - 1) * step * 0.5f;

            // Locate the pivot — the card the player is hovering.
            // −1 means no pivot (spread inactive or card not in cached list).
            int pivotIdx = _hoverSpreadActive && _hoverSpreadCard != null
                ? _cachedCards.IndexOf(_hoverSpreadCard)
                : -1;

            for (int i = 0; i < count; i++)
            {
                if (_cachedCards[i] == null) continue;

                float angleDeg = startAngle + step * i;

                if (pivotIdx >= 0 && i != pivotIdx)
                {
                    // Cards to the pivot's left  (i < pivotIdx) shift further left  (−bonus).
                    // Cards to the pivot's right (i > pivotIdx) shift further right (+bonus).
                    angleDeg += i < pivotIdx ? -hoverSpreadAngleBonus : hoverSpreadAngleBonus;
                }

                _cachedCards[i].SetLayoutTarget(ComputeLocalPosition(angleDeg), angleDeg);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void StartLerpSequence(List<(CardButton btn, Vector3 targetPos, float targetAngle)> targets)
        {
            _lerpSequence = DOTween.Sequence().SetLink(gameObject);
            foreach (var (btn, pos, angle) in targets)
            {
                var rt = btn.GetComponent<RectTransform>();
                _lerpSequence.Join(rt.DOLocalMove(pos, lerpDuration).SetEase(Ease.InOutSine));
                _lerpSequence.Join(rt.DOLocalRotate(new Vector3(0f, 0f, -angle), lerpDuration, RotateMode.Fast)
                                     .SetEase(Ease.InOutSine));
            }
            _lerpSequence.OnComplete(() =>
            {
                foreach (var (btn, pos, angle) in targets)
                    ApplyToCard(btn, pos, angle);
                _lerpSequence = null;
            });
        }

        /// <summary>Apply position + rotation to one card and update its hover base.</summary>
        private static void ApplyToCard(CardButton card, Vector3 localPos, float angleDeg)
        {
            RectTransform rt = card.GetComponent<RectTransform>();
            rt.localPosition = localPos;
            // Negative angle: positive angleDeg (right side) tilts clockwise = natural fan
            rt.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);

            card.SetBasePosition(localPos);
        }

        /// <summary>
        /// Reorders children left-to-right: the leftmost card gets sibling index 0
        /// (rendered behind all others) and the rightmost card gets the highest sibling
        /// index (rendered in front), creating a natural hand-held fan overlap.
        /// CardButton.OnPointerEnter() calls SetAsLastSibling() to pop a hovered card
        /// to the very front regardless of this order.
        /// </summary>
        private static void SetSiblingOrder(List<CardButton> cards)
        {
            int count = cards.Count;

            // Left → right: cards[0] (leftmost) = sibling 0 (behind),
            //               cards[count-1] (rightmost) = last sibling (in front).
            // Unity renumbers siblings on each SetSiblingIndex call; iterating
            // left-to-right keeps the final positions consistent.
            for (int i = 0; i < count; i++)
            {
                cards[i].transform.SetSiblingIndex(i);
                cards[i].SetBaseSiblingIndex(i);
            }
        }

        // ─── Arc Math (shared by ArrangeCards and OnDrawGizmos) ──────────────────

        /// <summary>
        /// Returns the effective angle step for <paramref name="count"/> cards,
        /// honouring the inspector limits and the crowding guard.
        /// </summary>
        private float ComputeEffectiveStep(int count)
        {
            if (count <= 1) return 0f;

            float step = Mathf.Min(angleStepDegrees, maxAngleStepDegrees);

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null && arcRadius > 0f)
            {
                float maxSin  = Mathf.Clamp01(rt.rect.width * 0.45f / arcRadius);
                float maxStep = 2f * Mathf.Asin(maxSin) * Mathf.Rad2Deg / (count - 1);
                step = Mathf.Min(step, maxStep);
            }

            return step;
        }

        /// <summary>
        /// Returns the local-space position for a card sitting at <paramref name="angleDeg"/>
        /// on the arc circle.
        /// </summary>
        private Vector3 ComputeLocalPosition(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(
                arcRadius * Mathf.Sin(rad),
                arcRadius * (Mathf.Cos(rad) - 1f),
                0f);
        }

        // ─── Scene-View Debug Preview ─────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugLayout || debugPreviewCount <= 0) return;

            int   count        = debugPreviewCount;
            float effectiveStep = ComputeEffectiveStep(count);
            float startAngle   = count == 1 ? 0f : -(count - 1) * effectiveStep * 0.5f;
            float centreIndex  = (count - 1) * 0.5f;

            Matrix4x4 containerMatrix = transform.localToWorldMatrix;

            // ── Ghost card outlines ──────────────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                float angleDeg = startAngle + effectiveStep * i;
                Vector3 localPos = ComputeLocalPosition(angleDeg);

                // Card matrix: container → local card position + rotation
                Matrix4x4 cardMatrix = containerMatrix
                    * Matrix4x4.TRS(localPos, Quaternion.Euler(0f, 0f, -angleDeg), Vector3.one);

                Gizmos.matrix = cardMatrix;
                Gizmos.color  = Mathf.Abs(i - centreIndex) < 0.01f ? debugCentreColor : debugCardColor;

                // Draw the card outline as a flat wire cube (z-depth 1 so it's visible in scene view)
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(cardWidth, debugCardHeight, 1f));

                // Small dot at card pivot
                Gizmos.DrawSphere(Vector3.zero, cardWidth * 0.04f);
            }

            // ── Arc curve ────────────────────────────────────────────────────────
            if (debugShowArc && count > 1 && arcRadius > 0f)
            {
                Gizmos.matrix = containerMatrix;
                Gizmos.color  = debugArcColor;

                float firstAngle = startAngle;
                float lastAngle  = startAngle + effectiveStep * (count - 1);

                // Draw the arc as 32 line segments between the first and last card angle
                const int segments = 32;
                Vector3 prev = ComputeLocalPosition(firstAngle);
                for (int s = 1; s <= segments; s++)
                {
                    float t    = s / (float)segments;
                    float ang  = Mathf.Lerp(firstAngle, lastAngle, t);
                    Vector3 pt = ComputeLocalPosition(ang);
                    Gizmos.DrawLine(prev, pt);
                    prev = pt;
                }
            }

            // Reset so other gizmos aren't affected
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color  = Color.white;
        }
#endif
    }
}
