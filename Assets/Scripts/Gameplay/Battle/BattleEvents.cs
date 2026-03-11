using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;

// ═══════════════════════════════════════════════════════════════════════════════
// BATTLE EVENTS — quick reference
//
// All events flow through the static EventBus (Assets/Scripts/Core/EventBus.cs).
//   Subscribe:   EventBus.Subscribe<FooEvent>(OnFoo);       // call in OnEnable
//   Unsubscribe: EventBus.Unsubscribe<FooEvent>(OnFoo);     // call in OnDisable
//   Publish:     EventBus.Publish(new FooEvent { ... });
//
// ── Lifecycle ────────────────────────────────────────────────────────────────
//   BattleStartedEvent       Publisher: BattleManager.StartBattle
//                            Subscribers: BattleUI, EnemyController
//
//   BattleEndedEvent         Publisher: BattleManager (BattleEndState)
//                            Subscribers: BattleUI, CampaignManager
//
// ── Turns ────────────────────────────────────────────────────────────────────
//   TurnStartedEvent         Publisher: BattleManager (TurnStartState)
//                            Subscribers: BattleUI, EnemyController
//
//   TurnEndedEvent           Publisher: BattleManager (TurnEndState)
//                            Subscribers: BattleUI
//
// ── Cards ────────────────────────────────────────────────────────────────────
//   CardDrawnEvent           Publisher: DeckManager.DrawCards
//                            Subscribers: BattleUI, PassiveResolver (future)
//
//   CardPlayedEvent          Publisher: BattleManager.PlayCard
//                            Subscribers: BattleUI
//
//   CardDiscardedEvent       Publisher: DeckManager.DiscardCard
//                            Subscribers: BattleUI
//
//   CardExhaustedEvent       Publisher: DeckManager.ExhaustCard
//                            Subscribers: BattleUI
//
// ── Effects ──────────────────────────────────────────────────────────────────
//   EffectAppliedEvent       Publisher: EffectResolver (per card effect)
//                            Subscribers: BattleUI (future), analytics
//
//   DamageDealtEvent         Publisher: EffectResolver
//                            Subscribers: BattleUI combat log
//
//   HealingAppliedEvent      Publisher: EffectResolver
//                            Subscribers: BattleUI combat log
//
//   StatusEffectAppliedEvent Publisher: EffectResolver
//                            Subscribers: BattleUI status icons
//
// ── Resources ────────────────────────────────────────────────────────────────
//   ActionPointsChangedEvent Publisher: BattleStats (GainActionPoints / PayCost)
//                            Subscribers: BattleUI AP display
//
//   ResolveChangedEvent      Publisher: BattleStats (DamageResolve / RestoreResolve)
//                            Subscribers: BattleUI health bar
//
//   ComposureChangedEvent    Publisher: BattleStats (GainComposure / LoseComposure)
//                            Subscribers: BattleUI composure display
//
//   HostilityChangedEvent    Publisher: BattleStats (GainHostility / ReduceHostility)
//                            Subscribers: BattleUI hostility bar, EnemySlotUI
//
// ── Enemy ────────────────────────────────────────────────────────────────────
//   EnemyIntentDeclaredEvent   Publisher: EnemyController (at start of player turn)
//                              Subscribers: BattleUI (EnemySlotUI)
//
//   EnemyHostilityChangedEvent Publisher: BattleManager.PlayCard (policy hostility shift)
//                              Subscribers: BattleUI (EnemySlotUI)
//
//   EnemyDefeatedEvent         Publisher: BattleManager
//                              Subscribers: BattleUI, BattleManager (focus auto-advance)
//
//   EnemySummonedEvent         Publisher: BattleManager.SummonMinions
//                              Subscribers: BattleUI (spawns new enemy slot)
//
// ── Player Input ─────────────────────────────────────────────────────────────
//   EndTurnRequestedEvent    Publisher: BattleUI end-turn button
//                            Subscribers: BattleManager
//
//   PlayCardRequestedEvent   Publisher: BattleUI card button click
//                            Subscribers: BattleManager
//
// ═══════════════════════════════════════════════════════════════════════════════

namespace Crookedile.Gameplay.Battle
{
    #region Battle Lifecycle Events

    /// <summary>
    /// Published by <c>BattleManager.StartBattle()</c> once the battle is fully initialized.
    /// Subscribers should use this to do any first-frame setup that requires a live BattleManager.
    /// </summary>
    public struct BattleStartedEvent : IGameEvent
    {
        /// <summary>
        /// The full setup snapshot used to initialize this battle —
        /// includes player origin, enemy roster, and starting deck configuration.
        /// </summary>
        public BattleSetup Setup;
    }

    /// <summary>
    /// Published by <c>BattleManager</c> when the battle concludes (victory or defeat).
    /// Subscribers should clean up battle-only state and transition to post-battle screens.
    /// </summary>
    public struct BattleEndedEvent : IGameEvent
    {
        /// <summary>
        /// Outcome of the battle. Check <c>Result.isVictory</c> to branch on win/loss.
        /// May contain post-battle reward data for the campaign layer.
        /// </summary>
        public BattleResult Result;
    }

    #endregion

    #region Turn Events

    /// <summary>
    /// Published at the start of each turn (player and enemy alike) by <c>BattleManager</c>.
    /// Use <see cref="IsPlayerTurn"/> to distinguish whose turn is beginning.
    /// </summary>
    public struct TurnStartedEvent : IGameEvent
    {
        /// <summary>1-based total turn count across the entire battle (increments every turn, not just player turns).</summary>
        public int TurnNumber;

        /// <summary>True if the player acts this turn; false if an enemy acts.</summary>
        public bool IsPlayerTurn;
    }

    /// <summary>
    /// Published at the end of each turn by <c>BattleManager</c>, after all end-of-turn cleanup runs.
    /// </summary>
    public struct TurnEndedEvent : IGameEvent
    {
        /// <summary>The turn number that just ended (matches the <see cref="TurnStartedEvent.TurnNumber"/> that opened this turn).</summary>
        public int TurnNumber;

        /// <summary>True if it was the player's turn that just ended; false if it was an enemy turn.</summary>
        public bool WasPlayerTurn;
    }

    #endregion

    #region Card Events

    /// <summary>
    /// Published by <c>DeckManager.DrawCards()</c> once per card successfully drawn into hand.
    /// </summary>
    public struct CardDrawnEvent : IGameEvent
    {
        /// <summary>The card data that was drawn.</summary>
        public CardData Card;

        /// <summary>True = drawn by the player; false = drawn by an enemy.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>BattleManager.PlayCard()</c> after a card is removed from hand and its effects begin resolving.
    /// </summary>
    public struct CardPlayedEvent : IGameEvent
    {
        /// <summary>The card data that was played.</summary>
        public CardData Card;

        /// <summary>True = played by the player; false = played by an enemy.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>BattleManager</c> once a played card's VFX animation fully completes,
    /// or immediately after effects resolve when the card has no VFX.
    /// <c>BattleUI</c> subscribes to this to begin the card discard animation, ensuring the
    /// sequence is always: VFX resolves → card flies to discard → new draws appear.
    /// </summary>
    public struct CardVFXCompleteEvent : IGameEvent
    {
        /// <summary>The card whose VFX (or immediate resolution) just finished.</summary>
        public CardData Card;
    }

    /// <summary>
    /// Published by <c>DeckManager.DiscardCard()</c> when a card moves from hand to the discard pile.
    /// </summary>
    public struct CardDiscardedEvent : IGameEvent
    {
        /// <summary>The card data that was discarded.</summary>
        public CardData Card;

        /// <summary>True = discarded from the player's hand; false = from an enemy's hand.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>DeckManager.ExhaustCard()</c> when a card is permanently removed from play for the battle.
    /// Exhausted cards go to the exhaust pile and cannot be reshuffled.
    /// </summary>
    public struct CardExhaustedEvent : IGameEvent
    {
        /// <summary>The card data that was exhausted.</summary>
        public CardData Card;

        /// <summary>True = exhausted from the player's deck; false = from an enemy's deck.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>DeckManager.AddCardToDeck()</c>, <c>AddCardsToDeck()</c>,
    /// <c>AddCardToDiscard()</c>, and <c>AddCardsToDiscard()</c> when a card is granted
    /// directly into the draw pile or discard pile during battle.
    /// Not fired for normal draws, hand additions, or discards from hand.
    /// </summary>
    public struct CardGrantedEvent : IGameEvent
    {
        /// <summary>The card data that was granted.</summary>
        public CardData Card;

        /// <summary>True = granted to the player; false = to an enemy.</summary>
        public bool IsPlayer;

        /// <summary>Number of copies that were added.</summary>
        public int Count;

        /// <summary>True = card landed in the discard pile; false = draw pile.</summary>
        public bool ToDiscard;
    }

    #endregion

    #region Effect Events

    /// <summary>
    /// Published by <c>EffectResolver</c> once per <see cref="CardEffect"/> after it resolves.
    /// Fires regardless of target or effect type — useful for analytics, achievement tracking,
    /// and implementing triggered card effects that react to specific effect types.
    /// </summary>
    public struct EffectAppliedEvent : IGameEvent
    {
        /// <summary>The individual card effect that was applied (type, amount, target, etc.).</summary>
        public CardEffect Effect;

        /// <summary>True = the effect was applied by/for the player; false = by/for an enemy.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>EffectResolver</c> whenever Resolve damage is successfully dealt (after all modifiers).
    /// Only fires when <c>Amount &gt; 0</c>.
    /// </summary>
    public struct DamageDealtEvent : IGameEvent
    {
        /// <summary>Actual Resolve damage dealt after composure reduction and hostility multiplier.</summary>
        public int Amount;

        /// <summary>True = player is the damage target; false = an enemy is the target.</summary>
        public bool IsToPlayer;

        /// <summary>Display name of the attacker ("Player" or the enemy's name).</summary>
        public string AttackerName;

        /// <summary>Zero-based index of the attacking enemy in BattleManager.Enemies.
        /// -1 when the player is the attacker.</summary>
        public int SourceEnemyIndex;

        /// <summary>Zero-based index of the enemy that received the damage.
        /// -1 when the player is the damage target (use <see cref="IsToPlayer"/> to confirm).</summary>
        public int TargetEnemyIndex;
    }

    /// <summary>
    /// Published by <c>EffectResolver</c> whenever Resolve healing is applied.
    /// Only fires when <c>Amount &gt; 0</c>.
    /// </summary>
    public struct HealingAppliedEvent : IGameEvent
    {
        /// <summary>Amount of Resolve restored (capped by max Resolve).</summary>
        public int Amount;

        /// <summary>True = player received the healing; false = an enemy received it.</summary>
        public bool IsToPlayer;
    }

    /// <summary>
    /// Published by <c>EffectResolver</c> when a status effect is applied or refreshed on a combatant.
    /// </summary>
    public struct StatusEffectAppliedEvent : IGameEvent
    {
        /// <summary>The type of status effect applied (e.g. Burning, Shielded, Stunned).</summary>
        public StatusEffectType StatusType;

        /// <summary>Number of stacks applied (positive = added; negative = removed).</summary>
        public int Stacks;

        /// <summary>True = applied to the player; false = applied to an enemy.</summary>
        public bool IsToPlayer;
    }

    #endregion

    #region Resource Events

    /// <summary>
    /// Published by <c>BattleStats</c> whenever Action Points change (gain or spend).
    /// Used by the UI to refresh the AP display without polling every frame.
    /// </summary>
    public struct ActionPointsChangedEvent : IGameEvent
    {
        /// <summary>AP value before the change.</summary>
        public int OldValue;

        /// <summary>AP value after the change.</summary>
        public int NewValue;

        /// <summary>True = the player's AP changed; false = an enemy's AP changed.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>BattleStats</c> whenever Resolve changes (damage or healing).
    /// Used by the UI to update health bars reactively.
    /// </summary>
    public struct ResolveChangedEvent : IGameEvent
    {
        /// <summary>Resolve value before the change.</summary>
        public int OldValue;

        /// <summary>Resolve value after the change (clamped to [0, MaxResolve]).</summary>
        public int NewValue;

        /// <summary>True = the player's Resolve changed; false = an enemy's Resolve changed.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>BattleStats</c> whenever Composure stacks change (gained or lost).
    /// Composure reduces incoming Resolve damage while stacks remain.
    /// </summary>
    public struct ComposureChangedEvent : IGameEvent
    {
        /// <summary>Composure stack count before the change.</summary>
        public int OldValue;

        /// <summary>Composure stack count after the change.</summary>
        public int NewValue;

        /// <summary>True = the player's Composure changed; false = an enemy's Composure changed.</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>BattleStats</c> whenever an <em>enemy's</em> Hostility number changes.
    /// Hostility is an enemy-only stat — negative = receptive, zero = neutral, positive = hostile.
    /// Hostility multiplies incoming Resolve damage; the player does not have a Hostility value.
    /// </summary>
    public struct HostilityChangedEvent : IGameEvent
    {
        /// <summary>Hostility value before the change.</summary>
        public int OldValue;

        /// <summary>Hostility value after the change.</summary>
        public int NewValue;

        /// <summary>True = the player's hostility changed; false = an enemy's hostility changed.
        /// Note: in current design the player's hostility stays 0 — this flag is reserved for symmetry.</summary>
        public bool IsPlayer;
    }

    #endregion

    #region Enemy Events

    /// <summary>
    /// Published by <c>EnemyController</c> at the start of the player's turn, after the enemy
    /// selects their next move. The UI displays this intent so the player can react before acting.
    /// Timing mirrors Slay the Spire: intent is revealed at the top of the player's turn.
    /// </summary>
    public struct EnemyIntentDeclaredEvent : IGameEvent
    {
        /// <summary>The move the enemy intends to execute on their upcoming turn.</summary>
        public EnemyMoveData Move;

        /// <summary>Zero-based index into <c>BattleManager.Enemies</c> that declared this intent.</summary>
        public int EnemyIndex;
    }

    /// <summary>
    /// Published by <c>BattleManager.PlayCard()</c> when a card's policy lean shifts an enemy's Hostility.
    /// Negative = moved toward receptive; positive = moved toward hostile.
    /// </summary>
    public struct EnemyHostilityChangedEvent : IGameEvent
    {
        /// <summary>Hostility value before the card was played.</summary>
        public int OldValue;

        /// <summary>Hostility value after applying the card's policy-lean shift.</summary>
        public int NewValue;

        /// <summary>Zero-based index into <c>BattleManager.Enemies</c> whose hostility shifted.</summary>
        public int EnemyIndex;
    }

    /// <summary>
    /// Published by <c>BattleManager</c> the moment an enemy's Resolve reaches zero.
    /// The enemy is removed from active combat after this event fires.
    /// </summary>
    public struct EnemyDefeatedEvent : IGameEvent
    {
        /// <summary>Zero-based index of the defeated enemy in <c>BattleManager.Enemies</c>.</summary>
        public int    EnemyIndex;

        /// <summary>Display name of the defeated enemy (for battle log and UI feedback).</summary>
        public string EnemyName;
    }

    /// <summary>
    /// Published by <c>BattleManager.SummonMinions()</c> each time a new enemy is added
    /// to the fight via a <c>SummonMinion</c> move. Subscribers should spawn a new enemy
    /// slot UI for the given index.
    /// </summary>
    public struct EnemySummonedEvent : IGameEvent
    {
        /// <summary>The data asset describing the summoned enemy.</summary>
        public EnemyData EnemyData;

        /// <summary>Zero-based index of the new enemy in <c>BattleManager.Enemies</c>.</summary>
        public int EnemyIndex;
    }

    /// <summary>
    /// Published by <c>BattleManager.OpponentTurnState</c> just before an enemy resolves
    /// its declared move. Used by the UI to shake that enemy's intent panel and signal
    /// which enemy is about to attack.
    /// </summary>
    public struct EnemyActingEvent : IGameEvent
    {
        /// <summary>Zero-based index into <c>BattleManager.Enemies</c> that is about to act.</summary>
        public int EnemyIndex;

        /// <summary>The move this enemy is about to execute. Used by <c>BattleFeedbackController</c>
        /// to play <see cref="EnemyMoveData.MoveVFX"/> on the player slot.</summary>
        public EnemyMoveData Move;
    }

    #endregion

    #region Player Input Events

    /// <summary>
    /// Published by <c>BattleUI</c> when the player clicks the End Turn button.
    /// <c>BattleManager</c> validates and processes the turn transition on receipt.
    /// </summary>
    public struct EndTurnRequestedEvent : IGameEvent { }

    /// <summary>
    /// Published by <c>BattleUI</c> when the player clicks a card in their hand.
    /// <c>BattleManager</c> validates affordability and triggers card resolution on receipt.
    /// </summary>
    public struct PlayCardRequestedEvent : IGameEvent
    {
        /// <summary>The card data the player wants to play.</summary>
        public CardData Card;

        /// <summary>Zero-based index of this card in the player's current hand (used to remove the correct copy if duplicates exist).</summary>
        public int HandIndex;
    }

    #endregion

    #region Card Choice Events

    /// <summary>
    /// Published by <c>EffectResolver</c> when a card effect requires the player to make an
    /// interactive card selection (e.g. ChooseFromDiscardToHand, UpgradeCardThisBattle).
    ///
    /// <c>BattleUI</c> receives this, transitions to <c>WaitingForCardChoice</c>, and opens
    /// <c>CardChoicePanel</c>. The panel invokes <see cref="OnConfirmed"/> with the player's
    /// selection, which executes the actual deck manipulation.
    ///
    /// NOTE: This is a class (not struct) because it holds a delegate.
    /// </summary>
    public class CardChoiceRequestedEvent : IGameEvent
    {
        /// <summary>Header text shown in the panel (e.g. "Choose a card from Discard").</summary>
        public string Title;

        /// <summary>All cards available to pick from.</summary>
        public System.Collections.Generic.IReadOnlyList<CardData> Choices;

        /// <summary>Exact number of cards the player must select before Confirm activates.</summary>
        public int RequiredCount;

        /// <summary>
        /// Invoked with the confirmed selection once the player presses Confirm.
        /// An empty list means the player cancelled — all callbacks must treat an empty list as a no-op.
        /// </summary>
        public System.Action<System.Collections.Generic.List<CardData>> OnConfirmed;
    }

    /// <summary>
    /// Published by <c>DeckManager.SwapCardInHand()</c> when a card is upgraded in-battle.
    /// </summary>
    public struct CardUpgradedEvent : IGameEvent
    {
        /// <summary>The original (non-upgraded) card that was swapped out.</summary>
        public CardData OldCard;

        /// <summary>The upgraded card that replaced it in hand.</summary>
        public CardData NewCard;

        /// <summary>True if the player's card was upgraded (false for an enemy upgrade).</summary>
        public bool IsPlayer;
    }

    // ── Card Retention / Recovery ─────────────────────────────────────────────

    /// <summary>
    /// Published by <c>DeckManager.RetainCard()</c> when a card in hand is marked to be
    /// retained at end of turn instead of being discarded.
    /// </summary>
    public struct CardRetainedEvent : IGameEvent
    {
        /// <summary>The card that was marked as retained.</summary>
        public CardData Card;

        /// <summary>True if the player's card was retained (always true in current usage).</summary>
        public bool IsPlayer;
    }

    /// <summary>
    /// Published by <c>DeckManager.MoveFromDiscardToHand()</c> when a card is successfully
    /// moved from the discard pile back into the player's hand.
    /// </summary>
    public struct CardRecoveredEvent : IGameEvent
    {
        /// <summary>The card that was moved from discard to hand.</summary>
        public CardData Card;

        /// <summary>True if the player's card was recovered (always true in current usage).</summary>
        public bool IsPlayer;
    }

    #endregion

}
