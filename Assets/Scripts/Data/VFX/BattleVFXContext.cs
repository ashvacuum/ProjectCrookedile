using System;

namespace Crookedile.Data.VFX
{
    /// <summary>
    /// Injected into a <see cref="Crookedile.UI.VFXAnimatedImage"/> after it is spawned.
    /// Carries callbacks that let the VFX animation drive battle effect timing:
    ///   • <see cref="OnApplyEffects"/> — resolves card damage/effects at the hit frame
    ///     (wired to an <c>ApplyEffects</c> Unity Animation Event mid-clip).
    ///   • <see cref="OnComplete"/>     — called when the animation ends, used to
    ///     unblock input in <c>BattleManager</c>.
    ///
    /// Cards without a VFX assigned continue to resolve effects immediately — no regression.
    /// </summary>
    public class BattleVFXContext
    {
        /// <summary>
        /// Fired by the <c>ApplyEffects</c> Animation Event at the hit frame of the VFX clip.
        /// Resolves card damage and other effects so they land visually in sync with the animation.
        /// If the clip has no <c>ApplyEffects</c> event, <see cref="Crookedile.UI.VFXAnimatedImage.OnAnimationComplete"/>
        /// fires this as a safety net before returning the instance to pool.
        /// </summary>
        public Action OnApplyEffects;

        /// <summary>
        /// Fired when the VFX animation completes (after pool return).
        /// Used by <c>BattleManager</c> to clear its <c>_vfxInFlight</c> flag and
        /// re-enable card play and end-turn input.
        /// </summary>
        public Action OnComplete;
    }
}
