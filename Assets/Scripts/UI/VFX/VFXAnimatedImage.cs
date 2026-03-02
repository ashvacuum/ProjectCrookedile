using UnityEngine;
using Crookedile.Data.VFX;
using Crookedile.Managers;

namespace Crookedile.UI
{
    /// <summary>
    /// Placed on animated VFX UI prefabs (Image + Animator).
    /// Signals <see cref="VFXManager"/> when the animation is done so the instance
    /// can be returned to the object pool cleanly.
    ///
    /// Optionally accepts a <see cref="BattleVFXContext"/> via <see cref="SetBattleContext"/>
    /// to gate card-effect resolution on animation events rather than firing immediately:
    ///   • Add an <b>ApplyEffects</b> AnimationEvent at the hit frame → damage lands in sync.
    ///   • Add a <b>PlaySound(string)</b> AnimationEvent on any frame → SFX plays there.
    ///   • If no <b>ApplyEffects</b> event is present, effects resolve automatically in
    ///     <see cref="OnAnimationComplete"/> as a safety net so no card ever hangs.
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

        private BattleVFXContext _context;
        private bool _effectsApplied;

        // ─── Battle Context ───────────────────────────────────────────────────────

        /// <summary>
        /// Injects a <see cref="BattleVFXContext"/> so animation events can drive card-effect timing.
        /// Call this immediately after <see cref="Managers.VFXManager.PlayAndGetInstance"/> returns.
        /// Chains the context's <see cref="BattleVFXContext.OnComplete"/> with the pool-return callback
        /// already wired by VFXManager so both fire when the animation ends.
        /// </summary>
        public void SetBattleContext(BattleVFXContext context)
        {
            _context = context;
            _effectsApplied = false;

            // Chain: pool-return (already wired by VFXManager) fires first, then the battle callback.
            var poolReturn = OnComplete;
            OnComplete = () => { poolReturn?.Invoke(); context.OnComplete?.Invoke(); };
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a specific animation state on this instance's Animator.
        /// Called by <see cref="Managers.VFXManager"/> immediately after activating the instance,
        /// so the correct clip plays regardless of which state the Animator was in when pooled.
        /// </summary>
        public void PlayAnimation(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return;
            var anim = GetComponent<Animator>();
            // Layer 0, normalizedTime 0 — always restart from the beginning of the named state.
            anim?.Play(stateName, 0, 0f);

            // Apply native sprite dimensions so every animation displays at the correct size
            // even if the clip has no m_SizeDelta curves baked in.
            // Safe when size curves ARE present — the Animator overwrites sizeDelta each frame anyway.
            if (VFXAnimationStateExtensions.NativeSizes.TryGetValue(stateName, out Vector2 nativeSize))
                GetComponent<RectTransform>().sizeDelta = nativeSize;
        }

        // ─── Animation Events ─────────────────────────────────────────────────────

        /// <summary>
        /// Called by an <b>ApplyEffects</b> AnimationEvent at the hit frame of the VFX clip.
        /// Fires <see cref="BattleVFXContext.OnApplyEffects"/> once, resolving card damage/effects
        /// in sync with the animation. Idempotent — repeated calls are ignored.
        /// </summary>
        public void ApplyEffects()
        {
            if (_effectsApplied) return;
            _effectsApplied = true;
            _context?.OnApplyEffects?.Invoke();
        }

        /// <summary>
        /// Called by an AnimationEvent on the last frame of the VFX animation clip.
        /// Fires <see cref="OnComplete"/> and deactivates this GameObject to return it to pool.
        /// Also acts as a safety net: if no <b>ApplyEffects</b> event fired during the clip,
        /// effects are resolved here so no card effect is ever lost.
        /// </summary>
        public void OnAnimationComplete()
        {
            // Safety net: if no ApplyEffects animation event was keyed in the clip, resolve now.
            if (_context != null && !_effectsApplied)
            {
                _effectsApplied = true;
                _context.OnApplyEffects?.Invoke();
            }
            _context = null;

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
            // clear all callbacks so pool references don't leak.
            OnComplete = null;
            _context = null;
            _effectsApplied = false;
        }
    }
}
