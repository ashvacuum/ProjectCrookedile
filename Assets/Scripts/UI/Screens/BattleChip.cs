using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Screens
{
    /// <summary>
    /// The one repeated widget on <see cref="BattleScreen"/>: a clickable box with up to three
    /// labels, an optional icon, and a tintable frame. Enemy slots, hand cards, and anything else
    /// the screen repeats are all this prefab bound with different text.
    ///
    /// ponytail: deliberately one generic widget instead of EnemyWidget/CardWidget/StatusIconWidget.
    /// Split it when a row needs behaviour of its own (drag-to-target, Spine stances), not before.
    /// Every serialized ref is optional, so a prefab with only a title label still renders.
    /// </summary>
    public class BattleChip : MonoBehaviour
    {
        #region Inspector

        [SerializeField]
        private TMP_Text _title;

        [Tooltip("Short right-hand value: card cost, enemy stance + hostility.")]
        [SerializeField]
        private TMP_Text _subtitle;

        [Tooltip("Multi-line detail: card rules text, enemy intent + status stacks.")]
        [SerializeField]
        private TMP_Text _body;

        [SerializeField]
        private Image _icon;

        [Tooltip("Background/border image the screen tints by stance or playability.")]
        [SerializeField]
        private Image _frame;

        [SerializeField]
        private Button _button;

        #endregion

        #region Runtime

        private int _index = -1;
        private Action<int> _onClicked;

        #endregion

        /// <summary>Row index this chip currently shows. Passed back on click.</summary>
        public int Index => _index;

        private void Awake()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// Sets the click handler once, at spawn. Kept out of <see cref="Bind"/> so re-rendering
        /// never allocates a closure and a chip can't end up wired to a stale row.
        /// </summary>
        public void Initialize(Action<int> onClicked) => _onClicked = onClicked;

        /// <summary>Paints the chip. Called for every visible row on every render.</summary>
        public void Bind(
            int index,
            string title,
            string subtitle,
            string body,
            Sprite icon,
            Color tint,
            bool interactable
        )
        {
            _index = index;

            SetText(_title, title);
            SetText(_subtitle, subtitle);
            SetText(_body, body);

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }

            if (_frame != null)
                _frame.color = tint;

            if (_button != null)
                _button.interactable = interactable;
        }

        private void HandleClick()
        {
            if (_onClicked != null)
                _onClicked(_index);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }
    }
}
