using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Battle End State — publishes the result event.</summary>
    internal class BattleEndState : BattleStateBase
    {
        public BattleEndState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter()
        {
            BattleResult result = _manager.GetBattleResult();
            GameLogger.LogInfo<BattleManager>(
                $"Battle ended — {(result.isVictory ? "VICTORY" : "DEFEAT")}"
            );
            EventBus.Publish(new BattleEndedEvent { Result = result });
        }
    }
}
