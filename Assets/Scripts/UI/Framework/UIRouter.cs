using System.Collections.Generic;
using Crookedile.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crookedile.UI
{
    /// <summary>
    /// Owns the canvas's popup stack and the one shared dimmer it keeps directly beneath the
    /// top popup — so modality is a mechanism rather than something every panel re-implements.
    ///
    /// <para>There is deliberately no screen stack. Nothing in this game navigates "back": every
    /// return to the campaign map is a re-entry that rebuilds (hours spent, locations consumed,
    /// day advanced), so a stack would only promise restored state it then throws away. Exclusive
    /// views are plain <c>SetActive</c>. Popups genuinely nest — the reward picker opens over the
    /// still-open battle result — which is the whole reason this stack exists.</para>
    /// </summary>
    [Debuggable("UIRouter", LogLevel.Info)]
    public class UIRouter : MonoBehaviour
    {
        [Tooltip(
            "Full-screen blocker + dim behind the top popup. A disabled Image on its own "
                + "GameObject under the popup layer; the router toggles and re-orders it."
        )]
        [SerializeField]
        private GameObject _dimmer;

        [Tooltip("Escape/back pops the top popup.")]
        [SerializeField]
        private bool _escapePopsPopup = true;

        private readonly List<UIView> _popups = new List<UIView>();

        /// <summary>The top popup, or null when no popup is open.</summary>
        public UIView TopPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        /// <summary>True while at least one popup is open (input beneath is blocked).</summary>
        public bool HasOpenPopup => _popups.Count > 0;

        /// <summary>Shows a popup above everything, moving the shared dimmer beneath it.</summary>
        public void PushPopup(UIView popup)
        {
            if (popup == null || popup.IsOnStack)
            {
                GameLogger.LogWarning<UIRouter>(
                    $"PushPopup rejected: {(popup == null ? "null" : popup.name + " already open")}"
                );
                return;
            }

            _popups.Add(popup);
            popup.MarkOnStack(true);
            popup.transform.SetAsLastSibling();
            popup.Show();
            popup.OnPushed();
            UpdateDimmer();
            GameLogger.LogInfo<UIRouter>($"PushPopup: {popup.name} (depth {_popups.Count})");
        }

        /// <summary>Closes the top popup.</summary>
        public void PopPopup()
        {
            if (_popups.Count > 0)
                ClosePopup(_popups[_popups.Count - 1]);
        }

        /// <summary>Closes a popup wherever it sits in the stack (e.g. dismissed by its own button).</summary>
        public void ClosePopup(UIView popup)
        {
            if (popup == null || !_popups.Remove(popup))
                return;

            popup.OnPopped();
            popup.MarkOnStack(false);
            popup.Hide();
            UpdateDimmer();
            GameLogger.LogInfo<UIRouter>($"ClosePopup: {popup.name} (depth {_popups.Count})");
        }

        /// <summary>
        /// Keeps the shared dimmer active and ordered directly beneath the top popup — it
        /// swallows clicks for everything below and dims the scene.
        /// </summary>
        private void UpdateDimmer()
        {
            if (_dimmer == null)
                return;

            if (_popups.Count == 0)
            {
                _dimmer.SetActive(false);
                return;
            }

            _dimmer.SetActive(true);
            _dimmer.transform.SetSiblingIndex(
                Mathf.Max(0, _popups[_popups.Count - 1].transform.GetSiblingIndex())
            );
        }

        private void Update()
        {
            if (
                _escapePopsPopup
                && HasOpenPopup
                && TopPopup.EscapeClosable
                && Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame
            )
                PopPopup();
        }
    }
}
