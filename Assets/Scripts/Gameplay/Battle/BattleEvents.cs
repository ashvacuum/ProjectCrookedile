using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;

namespace Crookedile.Gameplay.Battle
{
    #region Battle Lifecycle Events

    /// <summary>
    /// Published when a battle starts.
    /// </summary>
    public struct BattleStartedEvent : IGameEvent
    {
        public BattleSetup Setup;
    }

    /// <summary>
    /// Published when a battle ends.
    /// </summary>
    public struct BattleEndedEvent : IGameEvent
    {
        public BattleResult Result;
    }

    #endregion

    #region Turn Events

    /// <summary>
    /// Published when a new turn starts.
    /// </summary>
    public struct TurnStartedEvent : IGameEvent
    {
        public int TurnNumber;
        public bool IsPlayerTurn;
    }

    /// <summary>
    /// Published when a turn ends.
    /// </summary>
    public struct TurnEndedEvent : IGameEvent
    {
        public int TurnNumber;
        public bool WasPlayerTurn;
    }

    #endregion

    #region Card Events

    /// <summary>
    /// Published when a card is drawn.
    /// </summary>
    public struct CardDrawnEvent : IGameEvent
    {
        public CardData Card;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when a card is played.
    /// </summary>
    public struct CardPlayedEvent : IGameEvent
    {
        public CardData Card;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when a card is discarded.
    /// </summary>
    public struct CardDiscardedEvent : IGameEvent
    {
        public CardData Card;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when a card is exhausted.
    /// </summary>
    public struct CardExhaustedEvent : IGameEvent
    {
        public CardData Card;
        public bool IsPlayer;
    }

    #endregion

    #region Effect Events

    /// <summary>
    /// Published when a card effect is applied.
    /// </summary>
    public struct EffectAppliedEvent : IGameEvent
    {
        public CardEffect Effect;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when Resolve damage is dealt.
    /// </summary>
    public struct DamageDealtEvent : IGameEvent
    {
        public int Amount;
        public bool IsToPlayer;
    }

    /// <summary>
    /// Published when Resolve healing is applied.
    /// </summary>
    public struct HealingAppliedEvent : IGameEvent
    {
        public int Amount;
        public bool IsToPlayer;
    }

    /// <summary>
    /// Published when a status effect is applied.
    /// </summary>
    public struct StatusEffectAppliedEvent : IGameEvent
    {
        public StatusEffectType StatusType;
        public int Stacks;
        public bool IsToPlayer;
    }

    #endregion

    #region Resource Events

    /// <summary>
    /// Published when Action Points change.
    /// </summary>
    public struct ActionPointsChangedEvent : IGameEvent
    {
        public int OldValue;
        public int NewValue;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when Resolve changes.
    /// </summary>
    public struct ResolveChangedEvent : IGameEvent
    {
        public int OldValue;
        public int NewValue;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when Composure changes.
    /// </summary>
    public struct ComposureChangedEvent : IGameEvent
    {
        public int OldValue;
        public int NewValue;
        public bool IsPlayer;
    }

    /// <summary>
    /// Published when Hostility changes.
    /// </summary>
    public struct HostilityChangedEvent : IGameEvent
    {
        public int OldValue;
        public int NewValue;
        public bool IsPlayer;
    }

    #endregion

    #region Enemy Events

    /// <summary>
    /// Published when the enemy selects and reveals their next move (intent).
    /// Raised at the start of the player's turn so the UI can show the threat
    /// before the player decides which cards to play — matching Slay the Spire timing.
    /// </summary>
    public struct EnemyIntentDeclaredEvent : IGameEvent
    {
        /// <summary>The move the enemy intends to execute on their upcoming turn.</summary>
        public EnemyMoveData Move;
        /// <summary>Index into BattleManager.Enemies that declared this intent.</summary>
        public int EnemyIndex;
    }

    /// <summary>
    /// Published when an enemy's hostility number shifts due to a card played.
    /// Negative = receptive, zero = neutral, positive = hostile.
    /// </summary>
    public struct EnemyHostilityChangedEvent : IGameEvent
    {
        public int OldValue;
        public int NewValue;
        /// <summary>Index into BattleManager.Enemies whose hostility changed.</summary>
        public int EnemyIndex;
    }

    /// <summary>
    /// Published when an enemy's Resolve reaches zero.
    /// </summary>
    public struct EnemyDefeatedEvent : IGameEvent
    {
        public int    EnemyIndex;
        public string EnemyName;
    }

    #endregion

    #region Player Input Events

    /// <summary>
    /// Published when player requests to end their turn.
    /// </summary>
    public struct EndTurnRequestedEvent : IGameEvent { }

    /// <summary>
    /// Published when player requests to play a card.
    /// </summary>
    public struct PlayCardRequestedEvent : IGameEvent
    {
        public CardData Card;
        public int HandIndex;
    }

    #endregion
}
