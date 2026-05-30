using System.Collections;
using Crookedile.Data.VFX;
using Crookedile.Managers;
using Crookedile.Utilities;
using UnityEngine;

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
    /// Safety guarantees (two layers):
    ///   1. Timeout coroutine — if <see cref="OnAnimationComplete"/> never fires within
    ///      clip duration + buffer (missing Animation Event in the clip), it is called
    ///      automatically so <c>_vfxInFlight</c> is never permanently stuck.
    ///   2. OnDisable force-complete — if the GameObject is disabled before the animation
    ///      finishes (parent deactivated, scene change, etc.) battle callbacks fire immediately
    ///      and the pool-return is deferred one frame via <see cref="VFXManager.DeferredCallback"/>
    ///      to avoid the "cannot SetParent while activating/deactivating parent" Unity error.
    ///
    /// Setup:
    ///   1. Add this component to your VFX prefab alongside Image and Animator.
    ///   2. In the animation clip, add an <b>AnimationEvent</b> on the very last frame
    ///      targeting this component's <see cref="OnAnimationComplete"/> method.
    ///   3. VFXManager will wire <see cref="OnComplete"/> at runtime — no manual setup needed.
    /// </summary>
    [Debuggable("VFX", LogLevel.Info)]
    public class VFXAnimatedImage : MonoBehaviour
    {
        /// <summary>
        /// Assigned by <see cref="Managers.VFXManager"/> each time this instance is activated.
        /// Contains the full chain: pool-return first, then <see cref="BattleVFXContext.OnComplete"/>.
        /// </summary>
        internal System.Action OnComplete;

        private BattleVFXContext _context;
        private bool _effectsApplied;
        private Coroutine _completionTimeout;
        private string _currentStateName;

        /// <summary>
        /// The bare pool-return action wired by VFXManager, stored separately so
        /// <see cref="OnDisable"/> can defer only the SetParent operations without
        /// re-invoking <see cref="BattleVFXContext.OnComplete"/> a second time.
        /// </summary>
        private System.Action _poolReturnCallback;

        // Extra buffer added to the clip length before the timeout fires.
        private const float TimeoutBuffer = 0.5f;

        #region Battle Context
        /// <summary>
        /// Injects a <see cref="BattleVFXContext"/> so animation events can drive card-effect timing.
        /// Chains the context's <see cref="BattleVFXContext.OnComplete"/> with the pool-return callback
        /// already wired by VFXManager so both fire when the animation ends.
        /// </summary>
        public void SetBattleContext(BattleVFXContext context)
        {
            _context = context;
            _effectsApplied = false;

            // Capture the current pool-return callback as a LOCAL variable for the lambda closure.
            // If we captured _poolReturnCallback (a field) instead, OnAnimationComplete would null
            // that field before invoking the chain, making the lambda see null and skip pool-return.
            var poolReturn = OnComplete;
            _poolReturnCallback = OnComplete; // also stored in field so OnDisable can detect + defer it

            GameLogger.LogInfo(
                "VFX",
                $"SetBattleContext on '{gameObject.name}' — parent='{transform.parent?.name ?? "none"}'",
                this
            );

            // Chain: pool-return fires first (local capture, immune to field clearing), then battle callback.
            OnComplete = () =>
            {
                GameLogger.LogInfo("VFX", $"OnComplete chain firing on '{gameObject.name}'", this);
                poolReturn?.Invoke(); // local capture — always has the value
                context.OnComplete?.Invoke();
            };
        }

        #endregion

        #region Activation
        /// <summary>
        /// Plays a specific animation state on this instance's Animator and starts a timeout
        /// coroutine that force-fires <see cref="OnAnimationComplete"/> if the clip's own
        /// Animation Event never does so (e.g. the event is missing from the clip).
        /// </summary>
        public void PlayAnimation(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return;

            _currentStateName = stateName;
            var anim = GetComponent<Animator>();
            anim?.Play(stateName, 0, 0f);

            float clipLength = anim != null ? GetClipLength(anim, stateName) : -1f;
            if (clipLength < 0f)
                GameLogger.LogWarning(
                    "VFX",
                    $"Clip '{stateName}' not found on '{gameObject.name}' — using 3s fallback timeout.",
                    this
                );
            else
                GameLogger.LogInfo(
                    "VFX",
                    $"Playing '{stateName}' on '{gameObject.name}' (clipLength={clipLength:F2}s)  parent='{transform.parent?.name ?? "none"}'",
                    this
                );

            // Always start a completion timer — code drives VFX lifecycle, not Animation Events.
            // Animation Events (OnAnimationComplete, ApplyEffects) are optional enhancements that
            // can fire early, but the coroutine is the guaranteed fallback so _vfxInFlight can
            // never be permanently stuck by a missing or misconfigured clip/event.
            if (_completionTimeout != null)
                StopCoroutine(_completionTimeout);
            float safeDuration = clipLength > 0f ? clipLength + TimeoutBuffer : 3f;
            _completionTimeout = StartCoroutine(CompletionTimeout(safeDuration));
        }

        private IEnumerator CompletionTimeout(float delay)
        {
            yield return new WaitForSeconds(delay);
            _completionTimeout = null;

            if (OnComplete == null && _context == null)
                yield break; // already completed normally

            GameLogger.LogWarning(
                "VFX",
                $"Timeout ({delay:F2}s) hit for state '{_currentStateName}' on '{gameObject.name}' — "
                    + $"OnAnimationComplete Animation Event was never fired. Force-completing now.",
                this
            );
            OnAnimationComplete();
        }

        private static float GetClipLength(Animator anim, string stateName)
        {
            if (anim.runtimeAnimatorController == null)
                return -1f;
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
                if (clip.name == stateName)
                    return clip.length;
            return -1f;
        }

        #endregion

        #region Animation Events
        /// <summary>
        /// Called by an <b>ApplyEffects</b> AnimationEvent at the hit frame of the VFX clip.
        /// Idempotent — repeated calls are ignored.
        /// </summary>
        public void ApplyEffects()
        {
            if (_effectsApplied)
                return;
            _effectsApplied = true;
            GameLogger.LogInfo(
                "VFX",
                $"ApplyEffects fired on '{gameObject.name}'  context={(_context != null ? "set" : "null")}",
                this
            );
            _context?.OnApplyEffects?.Invoke();
        }

        /// <summary>
        /// Called by an AnimationEvent on the last frame of the VFX clip, OR by the timeout
        /// coroutine if that event is missing. Idempotent — safe to call multiple times.
        /// </summary>
        public void OnAnimationComplete()
        {
            // Cancel timeout — we completed before it fired (normal path).
            if (_completionTimeout != null)
            {
                StopCoroutine(_completionTimeout);
                _completionTimeout = null;
            }

            GameLogger.LogInfo(
                "VFX",
                $"OnAnimationComplete on '{gameObject.name}'  hasContext={_context != null}  effectsApplied={_effectsApplied}  hasCallback={OnComplete != null}",
                this
            );

            // Safety net: apply effects if the hit-frame event was not keyed in the clip.
            if (_context != null && !_effectsApplied)
            {
                _effectsApplied = true;
                GameLogger.LogWarning(
                    "VFX",
                    $"No ApplyEffects event fired — resolving in OnAnimationComplete safety net on '{gameObject.name}'",
                    this
                );
                _context.OnApplyEffects?.Invoke();
            }
            _context = null;

            // Clear _poolReturnCallback BEFORE SetActive(false) triggers OnDisable, so OnDisable
            // sees it as null and knows this is the normal completion path (not a premature kill).
            _poolReturnCallback = null;

            var callback = OnComplete;
            OnComplete = null; // clear BEFORE invoke to prevent double-fire if SetActive re-triggers
            callback?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Called by an AnimationEvent (string parameter = sound name).
        /// </summary>
        public void PlaySound(string soundName)
        {
            AudioManager.Instance?.PlaySound(soundName);
        }

        #endregion

        #region Lifecycle
        private void OnDisable()
        {
            // Coroutine is stopped by Unity automatically when disabled; clear the reference.
            _completionTimeout = null;

            // Normal completion path: OnAnimationComplete already cleared all three before SetActive(false).
            if (OnComplete == null && _context == null && _poolReturnCallback == null)
            {
                _effectsApplied = false;
                return;
            }

            #region Abnormal disable: VFX killed before completing
            GameLogger.LogWarning(
                "VFX",
                $"OnDisable on '{gameObject.name}' with live callbacks "
                    + $"(parent='{transform.parent?.name ?? "none"}', hasContext={_context != null}, "
                    + $"hasPoolReturn={_poolReturnCallback != null}) — force-completing to unblock game state.",
                this
            );

            var ctx = _context;
            var poolReturn = _poolReturnCallback;
            bool alreadyApplied = _effectsApplied;

            // Clear all fields before invoking to prevent any re-entry.
            OnComplete = null;
            _context = null;
            _poolReturnCallback = null;
            _effectsApplied = false;

            // Apply card effects if the hit-frame event never fired.
            if (ctx != null && !alreadyApplied)
            {
                GameLogger.LogWarning(
                    "VFX",
                    $"Force-applying card effects for '{gameObject.name}' in OnDisable safety path",
                    this
                );
                ctx.OnApplyEffects?.Invoke();
            }

            // Fire battle-state callback immediately — this only sets _vfxInFlight = false and
            // publishes CardVFXCompleteEvent. No SetParent involved, safe to call from OnDisable.
            ctx?.OnComplete?.Invoke();

            // Defer the pool-return (SetParent calls) to the next frame via VFXManager.
            // Calling SetParent during a parent's activation/deactivation throws a Unity error.
            if (poolReturn != null)
            {
                GameLogger.LogInfo(
                    "VFX",
                    $"Deferring pool-return for '{gameObject.name}' to next frame",
                    this
                );
                VFXManager.Instance?.DeferredCallback(poolReturn);
            }
        }
    }
}
            #endregion
        #endregion
