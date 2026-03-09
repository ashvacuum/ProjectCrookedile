namespace Crookedile.UI.Battle
{
    /// <summary>
    /// FSM states for the Battle UI layer.
    /// BattleUI holds a <c>StateMachine&lt;BattleUIState&gt;</c> and transitions between
    /// these states in response to EventBus events and player input.
    /// </summary>
    public enum BattleUIState
    {
        /// <summary>
        /// Opponent's turn, or TurnStart/TurnEnd processing.
        /// Hand is hidden and all player controls are disabled.
        /// </summary>
        Idle,

        /// <summary>
        /// Player's normal turn.
        /// Hand is shown with play-card callbacks; EndTurn button is enabled.
        /// </summary>
        PlayerTurn,

        /// <summary>
        /// A card effect is requesting interactive player input (e.g. choose a card from discard,
        /// upgrade a card, retain a card). Normal card play is disabled; <c>CardChoicePanel</c>
        /// is open. Automatically transitions back to <c>PlayerTurn</c> when the player confirms
        /// or cancels.
        /// </summary>
        WaitingForCardChoice,

        /// <summary>
        /// Battle is over (victory or defeat).
        /// Result panel is shown; hand is cleared; all controls disabled.
        /// </summary>
        BattleEnd,
    }
}
