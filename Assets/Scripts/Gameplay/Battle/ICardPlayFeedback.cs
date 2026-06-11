using System;
using Crookedile.Data.Cards;
using Cysharp.Threading.Tasks;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Direct async handshake between <see cref="BattleManager"/> and the UI layer for
    /// card-play VFX sequencing. Replaces the old CardPlayVFXRequested / CardVFXApplyEffects /
    /// CardVFXComplete bus round-trip: game flow must never block on an EventBus message.
    ///
    /// Implemented by <c>BattleFeedbackController</c> (UI assembly), which registers itself
    /// on <see cref="BattleManager.CardPlayFeedback"/> when enabled. When no implementation
    /// is registered, BattleManager resolves effects immediately with no animation.
    /// </summary>
    public interface ICardPlayFeedback
    {
        /// <summary>
        /// Plays the card's VFX animation. The implementation MUST invoke
        /// <paramref name="onApplyEffects"/> exactly once at the hit frame and complete the
        /// returned task exactly once when the animation finishes — including on any failure
        /// path (failing to spawn VFX must fire the callback and complete immediately, or
        /// the battle stalls).
        /// </summary>
        UniTask PlayCardVFX(CardData card, Action onApplyEffects);
    }
}
