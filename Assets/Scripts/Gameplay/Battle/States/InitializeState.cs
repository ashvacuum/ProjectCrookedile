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

            // Draw the opening hand. The first player turn deliberately draws nothing
            // (TurnStartState), so this IS turn 1's hand — a 0 here means an empty turn 1.
            // Guard against a misconfigured Starting Hand Size by falling back to the
            // per-turn draw, then to 5, so a battle can never open handless.
            int openingHand = _manager.StartingHandSize;
            if (openingHand <= 0)
            {
                int fallback = _manager.CardsPerTurn > 0 ? _manager.CardsPerTurn : 5;
                GameLogger.LogWarning<BattleManager>(
                    $"Starting Hand Size is {openingHand} on BattleManager — set it in the "
                        + $"Inspector. Using {fallback} for the opening hand this battle."
                );
                openingHand = fallback;
            }
            _manager.PlayerDeck.StartBattle(openingHand);

            // Fire battle-start passive AFTER the opening hand is dealt
            // (e.g. Faith Leader's Opening Prayer draws 1 extra card on top of the base hand)
            _manager.Passives?.FireBattleStart(_manager.PlayerDeck);

            _manager.TransitionToState(BattleState.TurnStart);
        }
    }
}
