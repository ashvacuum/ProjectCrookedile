using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Player Turn State — waits for the UI to call <see cref="BattleManager.RequestEndTurn"/>.</summary>
    internal class PlayerTurnState : BattleStateBase
    {
        public PlayerTurnState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter() =>
            GameLogger.LogInfo<BattleManager>("Player's turn started");

        public override void OnExit() =>
            GameLogger.LogInfo<BattleManager>("Player's turn ended");

        public override void OnUpdate()
        {
            // Waits for the UI to call RequestEndTurn()
        }
    }
}
