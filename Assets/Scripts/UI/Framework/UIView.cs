using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Base class for anything the UIRouter can push/pop — full screens and popups alike.
    /// Default Show/Hide is SetActive; override for tweened transitions later.
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        [Tooltip(
            "Scene router. Modal views push/pop themselves through it (dimmer + input "
                + "blocking). Falls back to plain Show/Hide while unassigned, so an unwired "
                + "scene still works."
        )]
        [SerializeField]
        private UIRouter _router;

        /// <summary>True while this view is on a router stack (visible or covered).</summary>
        public bool IsOnStack { get; private set; }

        /// <summary>
        /// False for popups the player must answer (e.g. the battle result) — the router's
        /// ESC handling skips them.
        /// </summary>
        public virtual bool EscapeClosable => true;

        /// <summary>Opens this view as a popup via the router (dimmer + input blocking under it).</summary>
        protected void PushAsPopup()
        {
            if (_router != null)
                _router.PushPopup(this);
            else
                Show();
        }

        /// <summary>Closes this view if it is an open popup (or just hides it when unrouted).</summary>
        protected void CloseAsPopup()
        {
            if (_router != null)
                _router.ClosePopup(this);
            else
                Hide();
        }

        /// <summary>Shows the view. Default = SetActive(true). Override to tween.</summary>
        public virtual void Show() => gameObject.SetActive(true);

        /// <summary>Hides the view. Default = SetActive(false). Override to tween.</summary>
        public virtual void Hide() => gameObject.SetActive(false);

        /// <summary>Called by the router when this view enters a stack (after Show).</summary>
        public virtual void OnPushed() { }

        /// <summary>Called by the router when this view leaves a stack (before Hide).</summary>
        public virtual void OnPopped() { }

        internal void MarkOnStack(bool value) => IsOnStack = value;
    }
}
