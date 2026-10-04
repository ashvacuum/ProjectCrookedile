using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.Gameplay.Battle
{
    /// <summary>
    /// Owns the player card-play pipeline, extracted from BattleManager: cost
    /// validation/payment, the Celebrity first-card-upgraded passive, the VFX handshake,
    /// effect resolution side-effects (crowd reaction, Momentum, Echo replay), and the
    /// Confused hand-override bookkeeping.
    ///
    /// BattleManager owns the battle flow and decides WHEN a card may be played
    /// (turn/state gates in RequestPlayCard); this class owns HOW a play resolves.
    /// </summary>
    [Debuggable("CardPlay", LogLevel.Info)]
    public class CardPlayController
    {
        private readonly BattleManager _mgr;

        // Celebrity passive: the first card played each battle is played upgraded.
        private bool _firstCardPlayedThisBattle;

        // True while a card's VFX animation is in flight; blocks card plays and End Turn.
        private bool _vfxInFlight;

        // Confused status — maps hand index → randomized effect amounts (one per effect on the card)
        private readonly Dictionary<int, int[]> _confusedOverrides = new Dictionary<int, int[]>();

        // Per-type play counts this turn — Counter intents read presence; payoff effects read
        // counts via EffectContextValue (e.g. "X per Policy played this turn").
        private readonly Dictionary<CardType, int> _typeCountsThisTurn =
            new Dictionary<CardType, int>();
        private int _cardsPlayedThisTurn;

        // Cards played from hand this turn — gates once-per-turn cards (OncePerTurnTag).
        private readonly HashSet<CardData> _playedThisTurn = new HashSet<CardData>();

        // Turn-scoped arming, cleared at player turn start. Replays: the next non-Policy card
        // played from hand resolves this many extra times. Free plays: the next N cards cost 0.
        // Discount: the next card costs this much less.
        private int _pendingReplays;
        private int _freePlays;
        private int _nextCardDiscount;

        // The card the latest play started with and how many times it has replayed since —
        // CardReplayedEvent.ReplayNumber (1 = its second play, 2 = its third).
        private CardData _chainCard;
        private int _chainReplays;

        // Replays resolving inside other replays right now. A replay whose effects replay again
        // (Encore replaying an earlier Encore) would otherwise recurse forever.
        private int _replayDepth;
        private const int MaxReplayDepth = 3;

        /// <summary>
        /// Tag for cards that can be played at most once per turn ("Not My Problem"). A card
        /// property expressed as a tag, like the Celebrity's Borrow tag.
        /// </summary>
        public const string OncePerTurnTag = "onceperturn";

        public CardPlayController(BattleManager manager) => _mgr = manager;

        /// <summary>AP actually paid for the latest card play (0 for free plays and replays).</summary>
        public int LastEnergyPaid { get; private set; }

        /// <summary>The latest non-Policy card to finish resolving this battle (Encore's target).</summary>
        public CardData LastNonPolicyPlayed { get; private set; }

        /// <summary>The latest Rhetoric card to finish resolving this battle (I Know a Guy's target).</summary>
        public CardData LastRhetoricPlayed { get; private set; }

        /// <summary>True while a card play (VFX) is still resolving — input should be blocked.</summary>
        public bool IsResolving => _vfxInFlight;

        /// <summary>Maps hand index to randomized effect amounts while the player has Confused.</summary>
        public IReadOnlyDictionary<int, int[]> ConfusedOverrides => _confusedOverrides;

        /// <summary>Resets per-battle state. Call from battle initialization.</summary>
        public void ResetForBattle()
        {
            _firstCardPlayedThisBattle = false;
            _vfxInFlight = false;
            _confusedOverrides.Clear();
            _typeCountsThisTurn.Clear();
            _cardsPlayedThisTurn = 0;
            ResetTurnArming();
            _chainCard = null;
            _chainReplays = 0;
            LastEnergyPaid = 0;
            LastNonPolicyPlayed = null;
            LastRhetoricPlayed = null;
        }

        private void ResetTurnArming()
        {
            _playedThisTurn.Clear();
            _pendingReplays = 0;
            _freePlays = 0;
            _nextCardDiscount = 0;
        }

        /// <summary>True if the player played at least one card of this type this turn.</summary>
        public bool WasTypePlayedThisTurn(CardType cardType) =>
            _typeCountsThisTurn.TryGetValue(cardType, out int n) && n > 0;

        /// <summary>How many cards of this type the player has played this turn.</summary>
        public int CountPlayedThisTurn(CardType cardType) =>
            _typeCountsThisTurn.TryGetValue(cardType, out int n) ? n : 0;

        /// <summary>Total cards played this turn (includes the one currently resolving).</summary>
        public int CardsPlayedThisTurn => _cardsPlayedThisTurn;

        /// <summary>
        /// Per-player-turn upkeep: randomizes hand amounts while Confused, clears stale
        /// overrides otherwise.
        /// </summary>
        public void OnPlayerTurnStart()
        {
            _typeCountsThisTurn.Clear();
            _cardsPlayedThisTurn = 0;
            ResetTurnArming();

            if (_mgr.PlayerStatusEffects.HasStatus<ConfusedStatus>())
                ApplyConfusedOverrides();
            else
                _confusedOverrides.Clear();
        }

        #region Play pipeline

        /// <summary>Plays a card from the player's hand. Caller has already validated turn state.</summary>
        public void PlayCard(CardData card, int handIndex)
        {
            BattleStats stats = _mgr.PlayerStats;

            if (!CanPlayCard(card, stats))
            {
                GameLogger.LogWarning<CardPlayController>($"Cannot play card: {card.CardName}");
                return;
            }

            // Counter intents check presence; payoff effects read counts. Incremented before
            // effects resolve, so "per Policy played this turn" INCLUDES the card being played.
            _typeCountsThisTurn.TryGetValue(card.CardType, out int played);
            _typeCountsThisTurn[card.CardType] = played + 1;
            _cardsPlayedThisTurn++;
            _playedThisTurn.Add(card);
            _chainCard = card;
            _chainReplays = 0;

            // Celebrity passive ("mastering his craft"): the first card played each battle is played
            // as its upgraded version. Swap to the upgraded instance before paying costs so the
            // upgraded cost AND effects apply. One-shot — consumed on the first play of the battle.
            if (!_firstCardPlayedThisBattle)
            {
                _firstCardPlayedThisBattle = true;
                if (_mgr.PlayerOrigin == OriginType.Actor && !card.IsUpgraded && card.CanUpgrade)
                {
                    var upgraded = card.CreateUpgradedInstance();
                    if (_mgr.PlayerDeck.SwapCardInHand(card, upgraded))
                        card = upgraded;
                }
            }

            PayCardCosts(card, stats);

            // Capture Confused overrides before the card leaves the hand (indices are stable here)
            _confusedOverrides.TryGetValue(handIndex, out int[] amountOverrides);

            if (!_mgr.PlayerDeck.PlayCardAtIndex(handIndex))
            {
                GameLogger.LogError<CardPlayController>("Failed to play card from hand");
                return;
            }

            // Shift Confused override indices — the played card is gone, so subsequent indices move down
            ShiftConfusedOverridesAfterPlay(handIndex);

            _mgr.Celebrity.OnCardPlayed(card, LastEnergyPaid);
            EventBus.Publish(new CardPlayedEvent { Card = card, IsPlayer = true });
            // PassiveResolver listens to CardPlayedEvent via EventBus — no direct call needed

            GameLogger.LogInfo<CardPlayController>(
                $"Player played: {card.CardName}  hasVFX={(card.CardVFX != null)}"
            );

            if (card.CardVFX != null && _mgr.CardPlayFeedback != null)
            {
                // VFX path: await the UI layer's animation. The implementation guarantees
                // the hit-frame callback fires and the task completes exactly once, including
                // on failure paths, so the battle can never be left blocked.
                ResolveCardPlayWithVFX(card, amountOverrides).Forget();
            }
            else
            {
                // No VFX (or no feedback layer registered) — resolve effects immediately.
                ApplyCardEffects(card, amountOverrides);
                CompleteCardPlay(card);
            }
        }

        private async UniTaskVoid ResolveCardPlayWithVFX(CardData card, int[] amountOverrides)
        {
            _vfxInFlight = true;
            try
            {
                await _mgr.CardPlayFeedback.PlayCardVFX(
                    card,
                    onApplyEffects: () => ApplyCardEffects(card, amountOverrides)
                );
            }
            finally
            {
                // Always unblock input and publish the resolved notification, even if the
                // feedback implementation faulted mid-animation.
                CompleteCardPlay(card);
            }
        }

        /// <summary>
        /// Finalises a card play: unblocks input and publishes the
        /// <see cref="CardPlayResolvedEvent"/> notification (BattleUI starts the discard
        /// animation on it). Sole publisher of that event.
        /// </summary>
        private void CompleteCardPlay(CardData card)
        {
            _vfxInFlight = false;
            RecordResolved(card);
            EventBus.Publish(new CardPlayResolvedEvent { Card = card });
        }

        /// <summary>Updates the "last card played" trackers once a card has resolved.</summary>
        private void RecordResolved(CardData card)
        {
            if (card == null)
                return;
            if (card.CardType != CardType.Policy)
                LastNonPolicyPlayed = card;
            if (card.CardType == CardType.Rhetoric)
                LastRhetoricPlayed = card;
        }

        /// <summary>
        /// Resolves all gameplay effects for a played card — damage, policy shifts, Momentum, Echo.
        /// Called either immediately (no VFX) or from the VFX animation's ApplyEffects event (with VFX).
        /// </summary>
        private void ApplyCardEffects(CardData card, int[] amountOverrides)
        {
            var ctx = _mgr.Resolver.ResolveCardEffects(card, isPlayerCard: true, amountOverrides);

            // Activated passive (Policy card): its effects resolved above; now switch on its
            // passives for the rest of the battle. The card is exhausted below so it leaves play.
            if (card.IsActivatedPassive)
                _mgr.Passives?.ActivateCardPassives(card);

            // If any effect flagged exhaust — or this is an activated-passive card — move it from
            // discard → exhaust pile now (PlayCardAtIndex already moved it hand → discard).
            if (ctx.ShouldExhaust || card.IsActivatedPassive)
                _mgr.PlayerDeck.ExhaustFromDiscard(card);

            // The crowd reacts: policy/single-target hostility shifts + echo-chamber refresh.
            _mgr.Crowd.OnCardPlayed(card, _mgr.FocusedEnemy, _mgr.FocusedEnemyIndex);
            foreach (var enemy in _mgr.Enemies)
                enemy.CheckBecameHostile();
            _mgr.CheckAndAdvanceFocusAfterCardPlay();
            TriggerMomentum();

            // Immediately end the battle if the meter maxed/zeroed during resolution.
            if (_mgr.CheckAndEndBattleIfOver())
                return;

            // Echo — replay the card a second time; consume the stack BEFORE the replay to
            // prevent a second Echo stack (if any) from triggering an infinite chain.
            int echoStacks = _mgr.PlayerStatusEffects.GetStacks<EchoStatus>();
            if (echoStacks > 0)
            {
                _mgr.PlayerStatusEffects.RemoveStacksNotify<EchoStatus>(1);
                GameLogger.LogInfo<CardPlayController>(
                    $"Echo triggered — replaying {card.CardName}"
                );
                _mgr.Resolver.ResolveCardEffects(card, isPlayerCard: true);
                _mgr.CheckAndAdvanceFocusAfterCardPlay();
                _mgr.CheckAndEndBattleIfOver();
            }

            // Armed replays ("play your next card twice") — consumed by the next non-Policy card.
            // Consumed before replaying so a replay can never re-arm itself.
            if (_pendingReplays > 0 && card.CardType != CardType.Policy)
            {
                int replays = _pendingReplays;
                _pendingReplays = 0;
                for (int i = 0; i < replays; i++)
                    if (!ReplayCard(card))
                        break;
            }
        }

        #endregion

        #region Replays and extra plays

        /// <summary>Arms a replay: the next non-Policy card played from hand this turn resolves once more.</summary>
        public void ArmReplayOfNextCard(int count = 1)
        {
            if (count <= 0)
                return;
            _pendingReplays += count;
            GameLogger.LogInfo<CardPlayController>(
                $"Next card this turn plays {_pendingReplays + 1} times"
            );
        }

        /// <summary>The next <paramref name="count"/> cards played this turn cost 0.</summary>
        public void GrantFreePlays(int count)
        {
            if (count > 0)
                _freePlays += count;
        }

        /// <summary>The next card played this turn costs <paramref name="amount"/> less (stacks).</summary>
        public void DiscountNextCard(int amount)
        {
            if (amount > 0)
                _nextCardDiscount += amount;
        }

        /// <summary>
        /// Resolves <paramref name="card"/>'s effects again without it leaving its pile — a
        /// replay. Not a play: it publishes <see cref="CardReplayedEvent"/>, never
        /// <see cref="CardPlayedEvent"/>, so "first card each turn" effects ignore it, and it adds
        /// no single-target Hostility. Policies never replay (their passives would re-activate).
        /// Returns false when nothing replayed (no card, a Policy, or the battle ended).
        /// </summary>
        public bool ReplayCard(CardData card)
        {
            if (card == null || card.CardType == CardType.Policy || _mgr.CheckAndEndBattleIfOver())
                return false;
            if (_replayDepth >= MaxReplayDepth)
            {
                GameLogger.LogWarning<CardPlayController>(
                    $"Replay of {card.CardName} refused: replays nested {_replayDepth} deep"
                );
                return false;
            }

            if (card == _chainCard)
                _chainReplays++;
            else
            {
                _chainCard = card;
                _chainReplays = 1;
            }
            int replayNumber = _chainReplays;

            GameLogger.LogInfo<CardPlayController>(
                $"Replaying {card.CardName} (extra play #{replayNumber})"
            );
            _replayDepth++;
            try
            {
                _mgr.Resolver.ResolveCardEffects(card, isPlayerCard: true);
            }
            finally
            {
                _replayDepth--;
            }

            if (
                _mgr.PlayerOrigin == OriginType.NepoBaby
                && NepoBabyConfig.Current.ReplaysRaiseHostilityByDefault
            )
                foreach (var enemy in _mgr.Enemies)
                    if (!enemy.IsDefeated)
                        enemy.Stats.GainHostility(1);

            SettleAfterResolution();
            EventBus.Publish(new CardReplayedEvent { Card = card, ReplayNumber = replayNumber });
            return true;
        }

        /// <summary>
        /// Plays a card that is no longer in hand (Blow the Allowance's burned card) for free. It
        /// IS a play — counted, and published as <see cref="CardPlayedEvent"/> so play triggers
        /// fire — but it adds no single-target Hostility and pays no energy. The card stays in
        /// whatever pile it is in.
        /// </summary>
        public void PlayOutOfHand(CardData card)
        {
            if (card == null || _mgr.CheckAndEndBattleIfOver())
                return;

            _typeCountsThisTurn.TryGetValue(card.CardType, out int played);
            _typeCountsThisTurn[card.CardType] = played + 1;
            _cardsPlayedThisTurn++;
            _chainCard = card;
            _chainReplays = 0;
            LastEnergyPaid = 0;

            EventBus.Publish(new CardPlayedEvent { Card = card, IsPlayer = true });
            GameLogger.LogInfo<CardPlayController>($"Played {card.CardName} out of hand, free");

            _mgr.Resolver.ResolveCardEffects(card, isPlayerCard: true);
            SettleAfterResolution();
            TriggerMomentum();
            _mgr.CheckAndEndBattleIfOver();
            RecordResolved(card);
        }

        /// <summary>
        /// The room settles after extra effects resolve: stance transitions, echo chamber,
        /// focus. The single-target Hostility bump is deliberately absent (that belongs to
        /// playing a card from hand).
        /// </summary>
        private void SettleAfterResolution()
        {
            foreach (var enemy in _mgr.Enemies)
                enemy.CheckBecameHostile();
            _mgr.Crowd.RefreshEchoChamberState();
            _mgr.CheckAndAdvanceFocusAfterCardPlay();
            _mgr.CheckAndEndBattleIfOver();
        }

        #endregion

        #region Costs

        public bool CanPlayCard(CardData card, BattleStats stats)
        {
            // Scandals and flagged Status cards are never playable
            if (card.IsUnplayable)
                return false;

            if (card.HasTag(OncePerTurnTag) && _playedThisTurn.Contains(card))
                return false;

            foreach (var cost in card.GetCosts())
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    if (stats.CurrentActionPoints < GetEffectiveCardCost(card))
                        return false;
                }
            }
            return true;
        }

        private void PayCardCosts(CardData card, BattleStats stats)
        {
            int paid = 0;
            foreach (var cost in card.GetCosts())
            {
                if (cost.CostType == CostType.ActionPoints)
                {
                    int effective = GetEffectiveCardCost(card);
                    stats.SpendActionPoints(effective);
                    paid += effective;
                    GameLogger.LogInfo<CardPlayController>(
                        $"Paid {effective} AP for {card.CardName}"
                    );
                }
            }
            LastEnergyPaid = paid;

            // Turn-scoped price breaks apply to the next card played, whatever it costs.
            if (_freePlays > 0)
                _freePlays--;
            else
                _nextCardDiscount = 0;
        }

        /// <summary>
        /// Single source of truth for the effective AP cost of a card this battle.
        /// Applies (in order): status effect modifiers (Focus, Energized, Entangled),
        /// per-card battle overrides (ReduceCardCost / MakeCardFree effects), the dynamic
        /// printed discount, then this turn's changes (Return-lane increases, the next-card
        /// discount; a free play or a made-free-this-turn card costs 0 outright).
        /// Result is floored at 0. Reads the upgraded cost list on upgraded cards.
        /// </summary>
        public int GetEffectiveCardCost(CardData card)
        {
            var costs = card?.GetCosts();
            if (costs == null || costs.Count == 0)
                return 0;
            // Find the AP cost wherever it sits in the list rather than assuming Costs[0].
            CardCost cost = null;
            foreach (var c in costs)
                if (c.CostType == CostType.ActionPoints)
                {
                    cost = c;
                    break;
                }
            if (cost == null)
                return 0;

            if (cost.IsXCost)
                return _mgr.PlayerStats.CurrentActionPoints;

            // Open Tab: the first N Borrow cards each turn are free.
            if (_mgr.Celebrity.NextBorrowIsFree && card.HasTag(CelebrityRules.BorrowTag))
                return 0;

            StatusEffectManager statusMgr = _mgr.PlayerStatusEffects;
            int baseCost =
                statusMgr != null
                    ? statusMgr.ModifyCardCost(cost.CurrentAmount)
                    : cost.CurrentAmount;

            // Per-card battle override (ReduceCardCost / MakeCardFree)
            int reduction = _mgr.PlayerDeck?.GetCardCostReduction(card) ?? 0;
            if (reduction == int.MaxValue)
                return 0; // MakeCardFree sentinel

            // Dynamic printed discount ("costs 1 less per X") — live board read each query.
            int dynamic = GetDynamicCostReduction(card);

            // This turn: a free play or a made-free card wins outright; otherwise the Return
            // lane's increase and the next-card discount adjust the price.
            if (_freePlays > 0 || (_mgr.PlayerDeck?.IsFreeThisTurn(card) ?? false))
                return 0;
            int increase = _mgr.PlayerDeck?.GetCostIncreaseThisTurn(card) ?? 0;

            return Mathf.Max(0, baseCost - reduction - dynamic + increase - _nextCardDiscount);
        }

        /// <summary>
        /// Live "costs 1 less per X" discount from <see cref="CardData.CostReductionPerX"/>.
        /// Sentinels (None/FixedAmount) mean no discount, matching the per-X scaling convention.
        /// </summary>
        private int GetDynamicCostReduction(CardData card)
        {
            var perX = card.CostReductionPerX;
            if (perX == EffectContextValue.None || perX == EffectContextValue.FixedAmount)
                return 0;
            if (_mgr.Resolver == null)
                return 0;
            // ponytail: fresh context per query (a few per hand per repaint) — pool if profiling
            // ever cares.
            return Mathf.Max(0, _mgr.Resolver.CreateContext(isPlayerCard: true).GetValue(perX));
        }

        #endregion

        #region Confused / Momentum

        /// <summary>
        /// Randomizes the displayed/resolved amounts for each card currently in the player's hand.
        /// Called at turn start while the player has the Confused status. Values are [0, 3] inclusive.
        /// </summary>
        private void ApplyConfusedOverrides()
        {
            _confusedOverrides.Clear();
            var hand = _mgr.PlayerDeck.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var effects = hand[i].Effects;
                if (effects == null || effects.Count == 0)
                    continue;
                var overrides = new int[effects.Count];
                for (int j = 0; j < effects.Count; j++)
                    overrides[j] = Random.Range(0, 4); // [0, 3] inclusive
                _confusedOverrides[i] = overrides;
            }
            GameLogger.LogInfo<CardPlayController>(
                $"Confused: randomized amounts for {_confusedOverrides.Count} cards in hand"
            );
        }

        /// <summary>
        /// If the player has Momentum stacks, presses the opinion meter by stacks against a
        /// random living enemy. Called once per card play (before Echo replay).
        /// </summary>
        private void TriggerMomentum()
        {
            int stacks = _mgr.PlayerStatusEffects?.GetStacks<MomentumStatus>() ?? 0;
            if (stacks <= 0)
                return;

            var living = new List<int>();
            for (int i = 0; i < _mgr.Enemies.Count; i++)
                if (!_mgr.Enemies[i].IsDefeated)
                    living.Add(i);
            if (living.Count == 0)
                return;

            int targetIndex = living[Random.Range(0, living.Count)];
            // Momentum presses the opinion meter through the ledger (absorbs once, then raises opinion).
            GameLogger.LogInfo<CardPlayController>(
                $"Momentum pressing opinion by {stacks} vs {_mgr.Enemies[targetIndex].EnemyData.EnemyName}"
            );
            _mgr.Opinion.ApplyOpinionShift(
                stacks,
                toPlayer: false,
                attackerName: "Player",
                sourceEnemyIndex: -1,
                targetEnemyIndex: targetIndex
            );
        }

        /// <summary>
        /// After a card is removed from the hand, all hand indices above the played index shift
        /// down by 1. This keeps the Confused overrides aligned with the updated hand layout.
        /// </summary>
        private void ShiftConfusedOverridesAfterPlay(int playedIndex)
        {
            if (_confusedOverrides.Count == 0)
                return;
            var shifted = new Dictionary<int, int[]>(_confusedOverrides.Count);
            foreach (var kvp in _confusedOverrides)
            {
                if (kvp.Key == playedIndex)
                    continue; // this entry is now gone
                int newKey = kvp.Key > playedIndex ? kvp.Key - 1 : kvp.Key;
                shifted[newKey] = kvp.Value;
            }
            _confusedOverrides.Clear();
            foreach (var kvp in shifted)
                _confusedOverrides[kvp.Key] = kvp.Value;
        }

        #endregion
    }
}
