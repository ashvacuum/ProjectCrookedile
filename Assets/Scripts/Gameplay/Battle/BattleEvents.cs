using Crookedile.Core;
using Crookedile.Data.Cards;

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
