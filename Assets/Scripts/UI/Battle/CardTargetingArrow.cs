using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// UI-based targeting arrow drawn from a card toward the cursor or a snapped enemy slot.
    /// Activated by <c>CardButton</c> when the player drags a card upward past the play threshold.
    ///
    /// ── Scene Setup ──────────────────────────────────────────────────────────────
    /// 1. Add a GameObject as a child of the root battle Canvas.
    /// 2. Add two child objects: a <b>Line</b> Image (pivot 0, 0.5) and an optional
    ///    <b>ArrowHead</b> Image. Assign them to <c>_line</c> and <c>_arrowHead</c>.
    /// 3. Assign the root Canvas's RectTransform to <c>_canvasRect</c>.
    /// 4. Place this GameObject near the top of the canvas sibling order so it draws
    ///    above cards and enemy slots.
    /// </summary>
    public class CardTargetingArrow : MonoBehaviour
    {
        public static CardTargetingArrow Instance { get; private set; }

        [Tooltip("Stretched Image that forms the line body. Pivot must be (0, 0.5).")]
        [SerializeField] private RectTransform _line;

        [Tooltip("Optional arrow sprite placed at the end point. Can be null.")]
        [SerializeField] private RectTransform _arrowHead;

        [Tooltip("Root Canvas RectTransform — used to convert world positions to canvas-local space.")]
        [SerializeField] private RectTransform _canvasRect;

        private RectTransform _startRect;
        private RectTransform _snapTarget;
        private Camera        _eventCamera;
        private Vector2       _currentScreenPos;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Activates the targeting arrow. The line starts at the given card's position.
        /// </summary>
        /// <param name="startRect">Card's RectTransform (line origin).</param>
        /// <param name="eventCamera">Event camera from PointerEventData; null for Overlay canvas.</param>
        public void Show(RectTransform startRect, Camera eventCamera)
        {
            _startRect        = startRect;
            _snapTarget       = null;
            _eventCamera      = eventCamera;
            _currentScreenPos = Vector2.zero;
            SetVisible(true);
        }

        /// <summary>Deactivates the targeting arrow.</summary>
        public void Hide()
        {
            _startRect  = null;
            _snapTarget = null;
            SetVisible(false);
        }

        /// <summary>
        /// Updates the free end of the arrow to follow the cursor.
        /// Call from <c>CardButton.OnDrag</c> every frame while targeting.
        /// When a snap target is set, the snap position overrides the cursor.
        /// </summary>
        public void UpdateEndPoint(Vector2 screenPos)
        {
            _currentScreenPos = screenPos;
            RefreshArrow();
        }

        /// <summary>Snaps the arrow end to the given enemy slot RectTransform.</summary>
        public void SnapTo(RectTransform target)
        {
            _snapTarget = target;
            RefreshArrow();
        }

        /// <summary>Clears the snap target so the arrow follows the cursor again.</summary>
        public void Unsnap()
        {
            _snapTarget = null;
            RefreshArrow();
        }

        // ─── Private ──────────────────────────────────────────────────────────────

        private void RefreshArrow()
        {
            if (_line == null || _startRect == null || _canvasRect == null) return;

            Vector2 start = CanvasLocalOf(_startRect.position);
            Vector2 end;

            if (_snapTarget != null)
            {
                end = CanvasLocalOf(_snapTarget.position);
            }
            else
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, _currentScreenPos, _eventCamera, out end);
            }

            Vector2 dir      = end - start;
            float   distance = dir.magnitude;
            float   angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Stretch the line from start toward end.
            _line.localPosition    = new Vector3(start.x, start.y, 0f);
            _line.sizeDelta        = new Vector2(distance, _line.sizeDelta.y);
            _line.localEulerAngles = new Vector3(0f, 0f, angle);

            // Place and rotate the arrowhead at the end point.
            if (_arrowHead != null)
            {
                _arrowHead.localPosition    = new Vector3(end.x, end.y, 0f);
                _arrowHead.localEulerAngles = new Vector3(0f, 0f, angle);
            }
        }

        private Vector2 CanvasLocalOf(Vector3 worldPos)
        {
            return _canvasRect.InverseTransformPoint(worldPos);
        }

        private void SetVisible(bool visible)
        {
            if (_line     != null) _line.gameObject.SetActive(visible);
            if (_arrowHead != null) _arrowHead.gameObject.SetActive(visible);
        }
    }
}
