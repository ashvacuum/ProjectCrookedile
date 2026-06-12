using Crookedile.Utilities;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>Initialize State — resets per-battle session state and draws the opening hand.</summary>
    internal class InitializeState : BattleStateBase
    {
        public InitializeState(BattleManager manager)
            : base(manager) { }

        public override void OnEnter()
        {
            GameLogger.LogInfo<BattleManager>("Initializing battle...");

            // Banked pools and the card-play pipeline reset per battle, not per turn.
            _manager.ResetBattleSessionState();

            // Draw player's opening hand; enemies have no deck
            _manager.PlayerDeck.StartBattle(_manager.StartingHandSize);

            // Fire battle-start passive AFTER the opening hand is dealt
            // (e.g. Faith Leader's Opening Prayer draws 1 extra card on top of the base hand)
            _manager.Passives?.FireBattleStart(_manager.PlayerDeck);

            _manager.TransitionToState(BattleState.TurnStart);
        }
    }
}
