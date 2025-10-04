namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Represents the different states of a card battle.
    /// </summary>
    public enum BattleState
    {
        /// <summary>
        /// Battle is initializing - setting up decks, stats, etc.
        /// </summary>
        Initialize,

        /// <summary>
        /// Starting a new turn - drawing cards, refreshing resources.
        /// </summary>
        TurnStart,

        /// <summary>
        /// Player is choosing and playing cards.
        /// </summary>
        PlayerTurn,

        /// <summary>
        /// Opponent AI is making decisions and playing cards.
        /// </summary>
        OpponentTurn,

        /// <summary>
        /// Ending the current turn - clearing temporary effects.
        /// </summary>
        TurnEnd,

        /// <summary>
        /// Battle has ended - victory or defeat.
        /// </summary>
        BattleEnd
    }
}
