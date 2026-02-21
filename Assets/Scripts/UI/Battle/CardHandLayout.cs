using UnityEngine;
using System.Collections.Generic;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Arranges CardButtons in an overlapping fan, like cards held in a hand.
    ///
    ///   Tuning quick-reference:
    ///     cardSpacingPx   — horizontal gap between card centres. Less = more overlap.
    ///                       Card width 120 px → 65 px spacing = ~46% overlap.
    ///     arcRadius       — size of the imaginary circle whose edge cards sit on.
    ///                       Controls how much edge cards dip below the centre card.
    ///                       Larger = flatter; 600-900 is a natural hand feel.
    ///     arcAngleDegrees — total rotation spread across all cards. Controls tilt.
    ///                       20-35° is realistic. 0 = all cards perfectly upright.
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
        [Header("Overlap & Spacing")]
        [Tooltip("Horizontal distance between card centres in pixels. " +
                 "Less than card width = overlap. 65 gives ~46% overlap on a 120 px card.")]
        [SerializeField] private float cardSpacingPx = 65f;

        [Header("Arc Shape")]
        [Tooltip("Radius of the imaginary circle the cards sit on. " +
                 "Controls how much edge cards dip below centre. Try 600–900.")]
        [SerializeField] private float arcRadius = 750f;

        [Tooltip("Total rotation spread across the whole hand in degrees. " +
                 "Controls how much edge cards tilt. Try 20–35.")]
        [SerializeField] private float arcAngleDegrees = 25f;

        [Tooltip("Maximum per-card tilt in degrees. Prevents extreme lean with a tiny hand.")]
        [SerializeField] private float maxAngleStepDegrees = 7f;

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

            // Per-card angle step, clamped to prevent absurd tilting with small hands
            float angleStep  = Mathf.Min(arcAngleDegrees / (count - 1), maxAngleStepDegrees);
            float totalAngle = angleStep * (count - 1);
            float startAngle = -totalAngle * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float angleDeg = startAngle + angleStep * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // X: evenly spaced — controls overlap directly and intuitively
                float x = (i - (count - 1) * 0.5f) * cardSpacingPx;

                // Y: arc dip — edge cards sit lower than centre card
                // cos(0) = 1  →  centre card y = 0
                // cos grows smaller at edges  →  edge cards dip (negative y)
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
