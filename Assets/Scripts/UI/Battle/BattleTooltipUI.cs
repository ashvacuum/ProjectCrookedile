using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Unified cursor-following tooltip singleton used by both status effect icons
    /// (<see cref="StatusEffectIconUI"/>) and enemy intent panels (<see cref="EnemyIntentDisplay"/>).
    ///
    /// Setup:
    ///   1. Add this component to a root panel GameObject on a canvas with a high sort order.
    ///   2. Assign <see cref="_canvas"/>, <see cref="_panel"/>, and the optional text/image fields.
    ///   3. The panel starts hidden; it is shown/hidden by callers via <see cref="Show"/> and <see cref="Hide"/>.
    ///
    /// Pass null/empty strings for optional parameters to hide those elements.
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

        [Tooltip("Optional icon image. Hidden when Show() is called without an icon.")]
        [SerializeField]
        private Image _icon;

        [Tooltip("Title / name line.")]
        [SerializeField]
        private TMP_Text _titleTxt;

        [Tooltip("Description / body text.")]
        [SerializeField]
        private TMP_Text _descTxt;

        [Tooltip("Optional extra line (e.g. 'Stacks: 3'). Hidden when Show() passes null/empty.")]
        [SerializeField]
        private TMP_Text _extraTxt;

        [Tooltip("Pixel offset from the cursor position to the top-left corner of the panel.")]
        [SerializeField]
        private Vector2 _cursorOffset = new Vector2(12f, -12f);

        #region Lifecycle
        private void Awake()
        {
            Instance = this;
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
            if (_panel == null || !_panel.gameObject.activeSelf)
                return;

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
        /// <summary>
        /// Show the tooltip near the cursor.
        /// </summary>
        /// <param name="title">Header line (move name or effect name).</param>
        /// <param name="description">Body text (what the effect / move does).</param>
        /// <param name="icon">Optional icon sprite — element hidden when null.</param>
        /// <param name="iconColor">Tint applied to the icon; defaults to white when <c>default</c>.</param>
        /// <param name="extraLine">Optional footer line (e.g. "Stacks: 3") — element hidden when null/empty.</param>
        public void Show(
            string title,
            string description,
            Sprite icon = null,
            Color iconColor = default,
            string extraLine = null
        )
        {
            if (_titleTxt != null)
                _titleTxt.text = title;
            if (_descTxt != null)
                _descTxt.text = description;

            if (_icon != null)
            {
                bool hasIcon = icon != null;
                _icon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    _icon.sprite = icon;
                    _icon.color = iconColor == default ? Color.white : iconColor;
                }
            }

            if (_extraTxt != null)
            {
                bool hasExtra = !string.IsNullOrEmpty(extraLine);
                _extraTxt.gameObject.SetActive(hasExtra);
                if (hasExtra)
                    _extraTxt.text = extraLine;
            }

            if (_panel != null)
                _panel.gameObject.SetActive(true);
        }

        /// <summary>Hides the tooltip panel immediately.</summary>
        public void Hide()
        {
            if (_panel != null)
                _panel.gameObject.SetActive(false);
        }
    }
}
        #endregion
