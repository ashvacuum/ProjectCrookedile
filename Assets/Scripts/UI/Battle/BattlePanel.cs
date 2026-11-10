using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// A <see cref="UIPanel"/> in the battle scene. <see cref="Battle"/> is the scene's battle, so a
    /// new panel needs no BattleUI field and no Bind call: drop it under the battle canvas.
    /// </summary>
    public abstract class BattlePanel : UIPanel
    {
        /// <summary>The running battle; null before the BattleManager wakes.</summary>
        protected BattleManager Battle => BattleManager.Current;

        /// <summary>
        /// Queues a <see cref="UIPanel.Refresh"/> on every event that can move a number a battle
        /// panel shows. One list for all of them, so a panel that paints from <see cref="Battle"/>
        /// cannot miss a change another panel sees.
        /// </summary>
        protected void RefreshOnBattleChanges()
        {
            RefreshOn<BattleStartedEvent>();
            RefreshOn<BattleStateChangedEvent>();
            RefreshOn<CardPlayedEvent>();
            RefreshOn<EnemySummonedEvent>();
            RefreshOn<SupportChangedEvent>();
            RefreshOn<DenialChangedEvent>();
            RefreshOn<StatusEffectAppliedEvent>();
            RefreshOn<OpinionChangedEvent>();
            RefreshOn<TurnLimitUpdatedEvent>();
        }
    }
}
