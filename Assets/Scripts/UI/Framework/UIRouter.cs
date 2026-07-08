using System.Collections.Generic;
using Crookedile.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crookedile.UI
{
    /// <summary>
    /// Per-scene navigation owner: a screen stack (exclusive full views — pushing hides the
    /// one below) and a popup stack (overlays — the router owns ONE shared dimmer/input
    /// blocker it keeps directly under the top popup, so modality is a mechanism, not a
    /// convention every panel re-implements).
    ///
    /// UI→UI navigation goes through here. Panels never show/hide sibling panels directly.
    /// </summary>
    [Debuggable("UIRouter", LogLevel.Info)]
    public class UIRouter : MonoBehaviour
    {
        #region Fields

        [Tooltip(
            "Full-screen blocker + dim behind the top popup. A disabled Image on its own "
                + "GameObject under the popup layer; the router toggles and re-orders it."
        )]
        [SerializeField]
        private GameObject _dimmer;

        [Tooltip("Escape/back pops the top popup (never pops screens).")]
        [SerializeField]
        private bool _escapePopsPopup = true;

        private readonly List<UIView> _screens = new List<UIView>();
        private readonly List<UIView> _popups = new List<UIView>();

        #endregion

        #region Properties

        /// <summary>The currently visible screen, or null.</summary>
        public UIView CurrentScreen => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        /// <summary>The top popup, or null when no popup is open.</summary>
        public UIView TopPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        /// <summary>True while at least one popup is open (input beneath is blocked).</summary>
        public bool HasOpenPopup => _popups.Count > 0;

        #endregion

        #region Screens

        /// <summary>Pushes a screen: hides the current one, shows this one on top.</summary>
        public void PushScreen(UIView screen)
        {
            if (screen == null || screen.IsOnStack)
            {
                GameLogger.LogWarning<UIRouter>(
                    $"PushScreen rejected: {(screen == null ? "null" : screen.name + " already on a stack")}"
                );
                return;
            }

            CurrentScreen?.Hide();
            _screens.Add(screen);
            screen.MarkOnStack(true);
            screen.Show();
            screen.OnPushed();
            GameLogger.LogInfo<UIRouter>($"PushScreen: {screen.name} (depth {_screens.Count})");
        }

        /// <summary>Pops the top screen and re-shows the one beneath it.</summary>
        public void PopScreen()
        {
            if (_screens.Count == 0)
            {
                GameLogger.LogWarning<UIRouter>("PopScreen on an empty screen stack");
                return;
            }

            UIView top = _screens[_screens.Count - 1];
            _screens.RemoveAt(_screens.Count - 1);
            top.OnPopped();
            top.MarkOnStack(false);
            top.Hide();
            CurrentScreen?.Show();
            GameLogger.LogInfo<UIRouter>($"PopScreen: {top.name} (depth {_screens.Count})");
        }

        /// <summary>Pops the current screen (if any) and pushes a replacement — no history entry.</summary>
        public void ReplaceScreen(UIView screen)
        {
            if (_screens.Count > 0)
                PopScreen();
            PushScreen(screen);
        }

        #endregion

        #region Popups

        /// <summary>Shows a popup above everything, moving the shared dimmer beneath it.</summary>
        public void PushPopup(UIView popup)
        {
            if (popup == null || popup.IsOnStack)
            {
                GameLogger.LogWarning<UIRouter>(
                    $"PushPopup rejected: {(popup == null ? "null" : popup.name + " already on a stack")}"
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
            if (_popups.Count == 0)
            {
                GameLogger.LogWarning<UIRouter>("PopPopup on an empty popup stack");
                return;
            }

            UIView top = _popups[_popups.Count - 1];
            _popups.RemoveAt(_popups.Count - 1);
            top.OnPopped();
            top.MarkOnStack(false);
            top.Hide();
            UpdateDimmer();
            GameLogger.LogInfo<UIRouter>($"PopPopup: {top.name} (depth {_popups.Count})");
        }

        /// <summary>Closes a specific popup wherever it sits in the stack (e.g. dismissed by its own button).</summary>
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
        /// Keeps the shared dimmer active and ordered directly beneath the top popup —
        /// it swallows clicks for everything below and dims the scene.
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
            UIView top = _popups[_popups.Count - 1];
            _dimmer.transform.SetSiblingIndex(
                Mathf.Max(0, top.transform.GetSiblingIndex())
            );
        }

        #endregion

        #region Input

        private void Update()
        {
            if (
                _escapePopsPopup
                && HasOpenPopup
                && Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame
            )
                PopPopup();
        }

        #endregion
    }
}
