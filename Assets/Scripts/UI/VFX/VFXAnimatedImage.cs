using System.Threading;
using Crookedile.Data.VFX;
using Crookedile.Managers;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Placed on animated VFX UI prefabs (Image + Animator).
    /// Signals <see cref="VFXManager"/> when the animation is done so the instance
    /// can be returned to the object pool cleanly.
    ///
    /// Timing is FULLY CODE-DRIVEN: <see cref="PlayAnimation"/> starts a playback coroutine
    /// that fires <see cref="ApplyEffects"/> at the hit time (a normalized fraction of the
    /// clip, authored on <see cref="Crookedile.Data.VFX.VFXEvent"/>) and
    /// <see cref="OnAnimationComplete"/> at the clip's end. No AnimationEvents need to be
    /// keyed in any clip. Clips that still contain legacy ApplyEffects/OnAnimationComplete
    /// events are tolerated — both methods are idempotent, so an early event simply preempts
    /// the coroutine harmlessly. Do not key events in new clips.
    ///
    /// Safety guarantee: OnDisable force-complete — if the GameObject is disabled before the
    /// animation finishes (parent deactivated, scene change, etc.) battle callbacks fire
    /// immediately and the pool-return is deferred one frame via
    /// <see cref="VFXManager.DeferredCallback"/> to avoid the "cannot SetParent while
    /// activating/deactivating parent" Unity error.
    ///
    /// Setup:
    ///   1. Add this component to your VFX prefab alongside Image and Animator.
    ///   2. VFXManager wires <see cref="OnComplete"/> and drives playback — no clip events needed.
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
        private CancellationTokenSource _playbackCts;
        private string _currentStateName;

        /// <summary>
        /// The bare pool-return action wired by VFXManager, stored separately so
        /// <see cref="OnDisable"/> can defer only the SetParent operations without
        /// re-invoking <see cref="BattleVFXContext.OnComplete"/> a second time.
        /// </summary>
        private System.Action _poolReturnCallback;

        // Playback duration used when the named clip cannot be found on the Animator.
        private const float FallbackClipLength = 1f;

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
        /// Plays a specific animation state on this instance's Animator and starts the
        /// playback-driver coroutine that fires <see cref="ApplyEffects"/> at
        /// <paramref name="hitTimeNormalized"/> of the clip length and
        /// <see cref="OnAnimationComplete"/> at the clip's end. The coroutine IS the
        /// lifecycle — no AnimationEvents are required in the clip.
        /// </summary>
        public void PlayAnimation(string stateName, float hitTimeNormalized = 0.5f)
        {
            if (string.IsNullOrEmpty(stateName))
                return;

            _currentStateName = stateName;
            var anim = GetComponent<Animator>();
            anim?.Play(stateName, 0, 0f);

            float clipLength = anim != null ? GetClipLength(anim, stateName) : -1f;
            if (clipLength < 0f)
            {
                GameLogger.LogWarning(
                    "VFX",
                    $"Clip '{stateName}' not found on '{gameObject.name}' — using {FallbackClipLength}s fallback duration.",
                    this
                );
                clipLength = FallbackClipLength;
            }
            else
            {
                GameLogger.LogInfo(
                    "VFX",
                    $"Playing '{stateName}' on '{gameObject.name}' (clipLength={clipLength:F2}s, hit@{hitTimeNormalized:P0})  parent='{transform.parent?.name ?? "none"}'",
                    this
                );
            }

            CancelPlaybackDriver();
            _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy()
            );
            DrivePlayback(clipLength, Mathf.Clamp01(hitTimeNormalized), _playbackCts.Token)
                .Forget();
        }

        /// <summary>
        /// Code-driven playback timeline: hit moment → effects, clip end → completion.
        /// Both targets are idempotent, so legacy AnimationEvents still keyed in old clips
        /// can fire first without causing double-application. Cancelled by
        /// <see cref="OnAnimationComplete"/>, <see cref="OnDisable"/>, or destruction.
        /// </summary>
        private async UniTaskVoid DrivePlayback(
            float clipLength,
            float hitTimeNormalized,
            CancellationToken ct
        )
        {
            float hitDelay = clipLength * hitTimeNormalized;
            if (hitDelay > 0f)
                await UniTask.WaitForSeconds(hitDelay, cancellationToken: ct);
            ApplyEffects();

            float remaining = clipLength - hitDelay;
            if (remaining > 0f)
                await UniTask.WaitForSeconds(remaining, cancellationToken: ct);
            OnAnimationComplete();
        }

        /// <summary>Cancels and disposes the playback-driver task, if one is running.</summary>
        private void CancelPlaybackDriver()
        {
            if (_playbackCts == null)
                return;
            _playbackCts.Cancel();
            _playbackCts.Dispose();
            _playbackCts = null;
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

        #region Playback Callbacks
        /// <summary>
        /// Fires the battle context's hit-frame callback. Called by the playback-driver
        /// coroutine at the authored hit time (or by a legacy AnimationEvent in old clips).
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
        /// Finalises playback: pool-return + battle completion callback. Called by the
        /// playback-driver coroutine at the clip's end (or by a legacy AnimationEvent in
        /// old clips). Idempotent — safe to call multiple times.
        /// </summary>
        public void OnAnimationComplete()
        {
            // Stop the driver if something else (legacy clip event) completed us first.
            CancelPlaybackDriver();

            GameLogger.LogInfo(
                "VFX",
                $"OnAnimationComplete on '{gameObject.name}'  hasContext={_context != null}  effectsApplied={_effectsApplied}  hasCallback={OnComplete != null}",
                this
            );

            // Safety net: apply effects if the hit moment somehow never fired.
            if (_context != null && !_effectsApplied)
            {
                _effectsApplied = true;
                GameLogger.LogWarning(
                    "VFX",
                    $"Hit moment never fired — resolving in OnAnimationComplete safety net on '{gameObject.name}'",
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
        /// Legacy entry point for old clips with a PlaySound(string) AnimationEvent keyed.
        /// New VFX should route audio through BattleSoundMap instead of clip events.
        /// </summary>
        public void PlaySound(string soundName)
        {
            AudioManager.Instance?.PlaySound(soundName);
        }

        #endregion

        #region Lifecycle
        private void OnDisable()
        {
            // Unlike coroutines, UniTasks do NOT stop on disable — cancel explicitly.
            CancelPlaybackDriver();

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
            // publishes CardPlayResolvedEvent. No SetParent involved, safe to call from OnDisable.
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

            #endregion
        }

        #endregion
    }
}
