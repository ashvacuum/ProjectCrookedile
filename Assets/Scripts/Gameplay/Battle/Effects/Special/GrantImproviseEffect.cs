using System;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Publishes <see cref="ImproviseGrantedEvent"/> so that <c>BattleManager</c> can activate
    /// the Actor's Improvise window for the current battle.
    ///
    /// Designed for use in the Actor's <c>OriginPassive._passives</c> list with a
    /// <c>BattleStartTrigger</c> and <c>OneShot = true</c>.
    ///
    /// The actual discard-and-redraw mechanic is handled by
    /// <c>BattleManager.TryPlayerImprovise()</c>, which the UI calls after card selection.
    /// </summary>
    [Serializable]
    public class GrantImproviseEffect : BattleEffect
    {
        public override void Execute(EffectExecutionContext ctx, int? amountOverride = null)
        {
            EventBus.Publish(new ImproviseGrantedEvent());
            GameLogger.LogInfo<GrantImproviseEffect>("Improvise granted — Actor can swap cards once per turn.");
        }

        public override string GetDescription() => "Grant Improvise: once per turn, discard any cards and draw the same number back.";
    }
}
