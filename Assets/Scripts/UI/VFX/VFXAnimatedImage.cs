using UnityEngine;
using Crookedile.Managers;

namespace Crookedile.UI
{
    /// <summary>
    /// Placed on animated VFX UI prefabs (Image + Animator).
    /// Signals <see cref="VFXManager"/> when the animation is done so the instance
    /// can be returned to the object pool cleanly.
    ///
    /// Setup:
    ///   1. Add this component to your VFX prefab alongside Image and Animator.
    ///   2. In the animation clip, add an <b>AnimationEvent</b> on the very last frame
    ///      targeting this component's <see cref="OnAnimationComplete"/> method.
    ///   3. VFXManager will wire <see cref="OnComplete"/> at runtime — no manual setup needed.
    /// </summary>
    public class VFXAnimatedImage : MonoBehaviour
    {
        /// <summary>
        /// Assigned by <see cref="Managers.VFXManager"/> each time this instance is activated.
        /// Returns the instance to pool and clears itself.
        /// </summary>
        internal System.Action OnComplete;

        /// <summary>
        /// Called by an AnimationEvent on the last frame of the VFX animation clip.
        /// Fires <see cref="OnComplete"/> and deactivates this GameObject to return it to pool.
        /// </summary>
        public void OnAnimationComplete()
        {
            var callback = OnComplete;
            OnComplete = null;      // clear before invoke to avoid double-firing if SetActive re-triggers
            callback?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called by an AnimationEvent (string parameter = sound name) to play a named SFX
        /// at a specific frame of the VFX animation.
        /// Register the clip in AudioManager's Sound Library with a matching name.
        /// </summary>
        public void PlaySound(string soundName)
        {
            AudioManager.Instance?.PlaySound(soundName);
        }

        private void OnDisable()
        {
            // Safety: if the GameObject is disabled externally (e.g. scene unload),
            // clear the callback so pool references don't leak.
            OnComplete = null;
        }
    }
}
