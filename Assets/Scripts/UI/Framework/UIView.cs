using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Base class for anything the UIRouter can push/pop — full screens and popups alike.
    /// Default Show/Hide is SetActive; override for tweened transitions later.
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>True while this view is on a router stack (visible or covered).</summary>
        public bool IsOnStack { get; private set; }

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
