using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Shared battle tooltip: one name + description box per entry.
    ///
    /// <see cref="Show"/> opens a single box that follows the cursor (status icons, enemy intents).
    /// <see cref="ShowBeside"/> stacks one box per entry beside an anchor (a hovered card lists
    /// every keyword it uses), flipping to the anchor's left when the right side runs out of room.
    ///
    /// Extra boxes are clones of this panel; the first instance is the singleton and lays the
    /// clones out, the clones only hold text.
    /// </summary>
    public class BattleTooltipUI : MonoBehaviour
    {
        public static BattleTooltipUI Instance { get; private set; }

        [Tooltip("Root canvas — used for screen-to-local cursor conversion.")]
        [SerializeField]
        private Canvas _canvas;

        [Tooltip("Panel RectTransform that is shown/hidden and repositioned each frame.")]
        [SerializeField]
        private RectTransform _panel;

        [Tooltip("Title / name line.")]
        [SerializeField]
        private TMP_Text _titleTxt;

        [Tooltip("Description / body text.")]
        [SerializeField]
        private TMP_Text _descTxt;

        [Tooltip("Pixel offset from the cursor position to the top-left corner of the panel.")]
        [SerializeField]
        private Vector2 _cursorOffset = new Vector2(12f, -12f);

        [Tooltip("Horizontal gap between an anchor (e.g. a hovered card) and its tooltip stack.")]
        [SerializeField]
        private float _anchorGap = 12f;

        [Tooltip("Vertical gap between stacked tooltip boxes.")]
        [SerializeField]
        private float _stackSpacing = 8f;

        // Row 0 is this panel; further rows are clones. Only populated on the singleton.
        private readonly List<BattleTooltipUI> _rows = new List<BattleTooltipUI>();
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _anchor;
        private bool _anchored;
        private int _shown;

        #region Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _rows.Add(this);
            }
            if (_panel != null)
                _panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (Instance != this || _panel == null || !_panel.gameObject.activeSelf)
                return;

            if (_anchored)
            {
                if (_anchor == null || !_anchor.gameObject.activeInHierarchy)
                    Hide();
                else
                    LayOutBesideAnchor();
                return;
            }

            Vector2 mousePos =
                Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform,
                mousePos,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var localPoint
            );

            _panel.anchoredPosition = localPoint + _cursorOffset;
        }

        #endregion

        #region Public API
        /// <summary>Show one box near the cursor.</summary>
        public void Show(string title, string description)
        {
            _anchored = false;
            _anchor = null;
            SetRows(new[] { (title, description) });
        }

        /// <summary>
        /// Show one box per entry, stacked top-down beside <paramref name="anchor"/>. Hides when
        /// <paramref name="entries"/> is empty or the anchor goes away.
        /// </summary>
        public void ShowBeside(
            RectTransform anchor,
            IReadOnlyList<(string title, string description)> entries
        )
        {
            if (anchor == null || entries.Count == 0)
            {
                Hide();
                return;
            }
            _anchored = true;
            _anchor = anchor;
            SetRows(entries);
            LayOutBesideAnchor();
        }

        /// <summary>Hides every tooltip box immediately.</summary>
        public void Hide()
        {
            _anchored = false;
            _anchor = null;
            _shown = 0;
            foreach (var row in _rows)
                row._panel.gameObject.SetActive(false);
        }
        #endregion

        private void SetRows(IReadOnlyList<(string title, string description)> entries)
        {
            while (_rows.Count < entries.Count)
                _rows.Add(Instantiate(this, _panel.parent));

            _shown = entries.Count;
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool on = i < _shown;
                row._panel.gameObject.SetActive(on);
                if (!on)
                    continue;
                if (row._titleTxt != null)
                    row._titleTxt.text = entries[i].title;
                if (row._descTxt != null)
                    row._descTxt.text = entries[i].description;
            }
        }

        // ponytail: boxes are the prefab's fixed size; add a ContentSizeFitter to the panel and a
        // LayoutRebuilder pass here if long descriptions start overflowing.
        private void LayOutBesideAnchor()
        {
            var parent = (RectTransform)_panel.parent;
            _anchor.GetWorldCorners(_corners); // 0 bottom-left, 1 top-left, 2 top-right
            Vector2 topLeft = parent.InverseTransformPoint(_corners[1]);
            Vector2 topRight = parent.InverseTransformPoint(_corners[2]);

            float width = _panel.rect.width;
            float x = topRight.x + _anchorGap;
            if (x + width > parent.rect.xMax)
                x = topLeft.x - _anchorGap - width;

            float total = 0f;
            for (int i = 0; i < _shown; i++)
                total += _rows[i]._panel.rect.height + (i > 0 ? _stackSpacing : 0f);

            // Start level with the anchor's top, pushed up if the stack would leave the screen.
            float y = Mathf.Max(topRight.y, parent.rect.yMin + total);
            for (int i = 0; i < _shown; i++)
            {
                var rt = _rows[i]._panel;
                rt.localPosition = new Vector3(x, y, 0f); // pivot is top-left
                y -= rt.rect.height + _stackSpacing;
            }
        }
    }
}
