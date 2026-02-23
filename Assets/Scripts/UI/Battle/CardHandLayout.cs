using UnityEngine;
using System.Collections.Generic;

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
    ///     angleStepDegrees   — degrees added per card left-to-right. 4–7° feels natural.
    ///     arcRadius          — circle size. Larger = flatter arc. 600–900 is a good range.
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

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Position, rotate, and z-sort all cards into a fan.
        /// Called by BattleUI after the hand is instantiated.
        /// </summary>
        public void ArrangeCards(List<CardButton> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            if (count == 1)
            {
                ApplyToCard(cards[0], Vector3.zero, 0f);
                return;
            }

            // Start with the designer-set step, clamped to the per-card tilt limit
            float effectiveStep = Mathf.Min(angleStepDegrees, maxAngleStepDegrees);

            // Crowding guard: shrink the step further if the arc would overflow the container.
            // The arc's half-width = arcRadius * sin(totalAngle / 2).
            // We want that to stay inside 45% of the container width on each side (90% total).
            RectTransform containerRt = GetComponent<RectTransform>();
            if (containerRt != null && arcRadius > 0f)
            {
                float maxSin        = Mathf.Clamp01(containerRt.rect.width * 0.45f / arcRadius);
                float maxStepForFit = 2f * Mathf.Asin(maxSin) * Mathf.Rad2Deg / (count - 1);
                effectiveStep       = Mathf.Min(effectiveStep, maxStepForFit);
            }

            // Card 0 starts at -half, card count-1 ends at +half → hand is always centred
            float startAngle = -(count - 1) * effectiveStep * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angleDeg = startAngle + effectiveStep * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // Both X and Y come from the same angle — cards sit on a true circle.
                // x: left-to-right spread along the arc.
                // y: 0 at centre, negative at edges (edge cards dip below the centre card).
                float x = arcRadius * Mathf.Sin(angleRad);
                float y = arcRadius * (Mathf.Cos(angleRad) - 1f);

                ApplyToCard(cards[i], new Vector3(x, y, 0f), angleDeg);
            }

            // Put centre card on top, edges behind — like a real held hand
            SetSiblingOrder(cards);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

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
        /// Reorders children so the centre card renders on top of its neighbours.
        /// Cards farthest from centre get the lowest sibling index (rendered behind).
        /// CardButton.OnPointerEnter() calls SetAsLastSibling() to pop a hovered card
        /// to the very front regardless of this order.
        /// </summary>
        private static void SetSiblingOrder(List<CardButton> cards)
        {
            int count = cards.Count;
            float centre = (count - 1) * 0.5f;

            // Sort card indices by distance from centre — descending.
            // Farthest from centre = lowest sibling index = renders behind.
            int[] sortedIndices = new int[count];
            for (int i = 0; i < count; i++) sortedIndices[i] = i;

            System.Array.Sort(sortedIndices, (a, b) =>
            {
                float distA = Mathf.Abs(a - centre);
                float distB = Mathf.Abs(b - centre);
                return distB.CompareTo(distA); // descending: farthest first
            });

            // Assign sibling indices in sorted order.
            // Unity renumbers siblings on each SetSiblingIndex call,
            // so we process from slot 0 upward.
            for (int slot = 0; slot < count; slot++)
                cards[sortedIndices[slot]].transform.SetSiblingIndex(slot);
        }
    }
}
