using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Crookedile.Tests.TestContent;

namespace Crookedile.Tests
{
    /// <summary>
    /// Celebrity mechanics (Glamour, Debt, Promises, Starstruck), each checked through the effect
    /// that implements it. Cards are built in code with the numbers each test states, so retuning a
    /// card asset never breaks these; CardSmokeTests covers the assets themselves.
    /// </summary>
    public class CelebrityCardTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;

        // Shared cards whose identity matters (deck contents, token swaps, Bailout's card).
        private CardData _pressure; // costs 1, Sway 6
        private CardData _soundbite; // free, Sway 2, exhausts
        private CardData _movieQuote; // free, 1 Glamour, exhausts

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _pressure = Card(
                _objects,
                "Pressure",
                CardType.Pressure,
                1,
                Make<ApplyOpinionEffect>(("_amount", 6))
            );
            _soundbite = Card(
                _objects,
                "Soundbite",
                CardType.Rhetoric,
                0,
                Make<ApplyOpinionEffect>(("_amount", 2)),
                new ExhaustThisCardEffect()
            );
            _movieQuote = Card(
                _objects,
                "Movie Quote",
                CardType.Rhetoric,
                0,
                Make<GainGlamourEffect>(("_amount", 1)),
                new ExhaustThisCardEffect()
            );

            var enemy = ScriptableObject.CreateInstance<EnemyData>();
            _objects.Add(enemy);
            Set(enemy, "_minHostility", -10);
            var host = new GameObject("Celebrity card test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(new OriginPassive[0]);
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = new List<EnemyData> { enemy },
                    playerDeck = Enumerable.Repeat(_pressure, 12).ToList(),
                    startingOpinion = 50,
                    maxTurns = 20,
                }
            );
            yield return WaitForPlayerTurn();
            _battle.Enemies[0].Stats.SetHostility(0);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator DedicatedDebtEffectsStackAndRespectAmountOverrides()
        {
            var context = Resolver.CreateContext(true);
            new DebtWaiverEffect().Execute(context, 2);
            Assert.AreEqual(2, _battle.Celebrity.DebtWaivers);
            new DelayDebtSettlementEffect().Execute(context, 3);
            Assert.AreEqual(3, _battle.Celebrity.SettlementsDelayed);
            int energy = _battle.PlayerStats.CurrentActionPoints;
            Make<BorrowEffect>(("_energy", 2), ("_debt", 2)).Execute(context);
            Assert.AreEqual(energy + 2, _battle.PlayerStats.CurrentActionPoints);
            Assert.AreEqual(2, _battle.Celebrity.Debt);
            var bailout = Make<BailoutEffect>(("_soundbite", _soundbite));
            bailout.Execute(context, 2);
            bailout.Execute(context, 3);
            Assert.AreEqual(5, _battle.Celebrity.BailoutCapPerTurn);
            Assert.AreSame(_soundbite, _battle.Celebrity.BailoutCard);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DedicatedNepoEffectsApplyAndConsumeTheirTurnSetups()
        {
            _battle.Opinion.DecayOpinion(30);
            var context = Resolver.CreateContext(true);
            var card = _pressure;
            int cost = _battle.CardPlay.GetEffectiveCardCost(card);
            Assert.Greater(cost, 0);
            new GrantFreePlaysEffect().Execute(context, 2);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(0, _battle.CardPlay.GetEffectiveCardCost(card));
                _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().IndexOf(card));
            }
            Assert.AreEqual(cost, _battle.CardPlay.GetEffectiveCardCost(card));
            new DiscountNextCardEffect().Execute(context, cost);
            Assert.AreEqual(0, _battle.CardPlay.GetEffectiveCardCost(card));
            _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().IndexOf(card));
            Assert.AreEqual(cost, _battle.CardPlay.GetEffectiveCardCost(card));
            int replays = 0;
            void OnReplay(CardReplayedEvent e) => replays++;
            EventBus.Subscribe<CardReplayedEvent>(OnReplay);
            try
            {
                new ReplayNextCardEffect().Execute(context, 1);
                _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().IndexOf(card));
                Assert.AreEqual(1, replays);
                Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
                _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().IndexOf(card));
                Assert.AreEqual(1, replays);
            }
            finally
            {
                EventBus.Unsubscribe<CardReplayedEvent>(OnReplay);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator GlamourThresholdsGateGainsAndCashOutConsumesAll()
        {
            var gainFiveAtSix = IfGlamour(ComparisonType.AtLeast, 6, GainGlamour(5));
            Glamour(5);
            Play(gainFiveAtSix);
            Assert.AreEqual(5, _battle.CurrentGlamour);
            Glamour(1);
            Play(gainFiveAtSix);
            Assert.AreEqual(11, _battle.CurrentGlamour);
            _battle.PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(int.MaxValue);
            Play(IfGlamour(ComparisonType.AtMost, 2, GainGlamour(2)));
            Assert.AreEqual(2, _battle.CurrentGlamour);
            Glamour(4);
            _battle.GainDenial(4);
            int before = _battle.Opinion.CurrentOpinion;
            Play(Make<ConsumeGlamourForOpinionEffect>(("_swayPerGlamour", 2)));
            Assert.AreEqual(0, _battle.CurrentGlamour);
            Assert.AreEqual(before + 8, _battle.Opinion.CurrentOpinion);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovieQuotesReplaceCurrentAndFutureTokens()
        {
            _battle.PlayerDeck.AddCardToHand(_soundbite);
            _battle.PlayerDeck.AddCardToDiscard(_soundbite);
            _battle.PlayerDeck.AddCardToDeck(_soundbite);
            Play(
                Make<SignatureCatchphraseEffect>(
                    ("_soundbite", _soundbite),
                    ("_movieQuote", _movieQuote)
                )
            );
            Assert.IsFalse(_battle.PlayerDeck.AllCards.Contains(_soundbite));
            Assert.AreEqual(3, _battle.PlayerDeck.AllCards.Count(c => c == _movieQuote));
            _battle.PlayerDeck.AddCardToHand(_soundbite);
            Assert.AreEqual(2, _battle.PlayerDeck.Hand.Count(c => c == _movieQuote));
            var result = Play(_movieQuote);
            Assert.AreEqual(1, _battle.CurrentGlamour);
            Assert.IsTrue(result.ShouldExhaust);
            Assert.AreEqual(2, Play(_soundbite).LastDamageDealt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreditAppliesToOneNewDebtGainAndDiscountExpires()
        {
            _battle.Celebrity.GainDebt(4);
            Play(Make<LineOfCreditEffect>(("_debtReduction", 2), ("_cardDiscount", 1)));
            Assert.AreEqual(1, _battle.Celebrity.GainDebt(3, _battle.PlayerDeck));
            Assert.AreEqual(5, _battle.Celebrity.Debt);
            Assert.AreEqual(-1, _battle.PlayerDeck.GetCostIncreaseThisTurn(_pressure));
            Assert.AreEqual(3, _battle.Celebrity.GainDebt(3, _battle.PlayerDeck));
            _battle.PlayerDeck.StartTurn(0);
            Assert.AreEqual(0, _battle.PlayerDeck.GetCostIncreaseThisTurn(_pressure));
            _battle.Celebrity.ResetTurn();
            Assert.AreEqual(3, _battle.Celebrity.GainDebt(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverpromiseTriplesOnlyTheNextCardsSway()
        {
            var overpromise = new BattleEffect[]
            {
                Make<BorrowEffect>(("_energy", 0), ("_debt", 3)),
                Make<OverpromiseEffect>(("_swayMultiplier", 3)),
            };
            Play(overpromise);
            Assert.AreEqual(3, _battle.Celebrity.Debt);
            Assert.AreEqual(6, Play(_soundbite).LastDamageDealt);
            Assert.AreEqual(2, Play(_soundbite).LastDamageDealt);
            Play(overpromise);
            _battle.Celebrity.ResetTurn();
            Assert.AreEqual(2, Play(_soundbite).LastDamageDealt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PaidPromisesReserveDebtAndOnlyNextTurnCanFulfil()
        {
            _battle.Celebrity.GainDebt(5);
            Play(PaidOffPromises());
            Assert.AreEqual(2, _battle.Celebrity.Debt);
            Assert.AreEqual(3, _battle.Celebrity.PromisedDebt);
            var costly = Card(_objects, "Costs 2", CardType.Rhetoric, 2);
            _battle.Celebrity.OnCardPlayed(costly);
            Assert.AreEqual(3, _battle.Celebrity.PromisedDebt);
            _battle.Celebrity.ResetTurn();
            _battle.PlayerStats.GainActionPoints(10);
            int before = _battle.PlayerStats.CurrentActionPoints;
            _battle.Celebrity.Settle(
                _battle.Opinion,
                _battle.PlayerStats,
                _battle.PlayerDeck.AddCardsToHand
            );
            Assert.AreEqual(before - 2, _battle.PlayerStats.CurrentActionPoints);
            _battle.Celebrity.OnCardPlayed(costly);
            Assert.AreEqual(0, _battle.Celebrity.PromisedDebt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissedPromisesCollectAtNextTurnEndAndSettleUpRemovesOne()
        {
            _battle.Celebrity.GainDebt(4);
            Play(Make<ForgiveDebtEffect>(("_max", 1), ("_supportPerDebt", 0)));
            Assert.AreEqual(3, _battle.Celebrity.Debt);
            Play(PaidOffPromises());
            _battle.Celebrity.ResetTurn();
            _battle.PlayerStats.SpendActionPoints(_battle.PlayerStats.CurrentActionPoints);
            int before = _battle.Opinion.CurrentOpinion;
            _battle.Celebrity.SettlePromises(
                _battle.Opinion,
                _battle.PlayerStats,
                _battle.PlayerDeck.AddCardsToHand
            );
            Assert.AreEqual(before - 3, _battle.Opinion.CurrentOpinion);
            Assert.AreEqual(0, _battle.Celebrity.PromisedDebt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MediaTrainingPaysOnceAndClosesAfterCardPlay()
        {
            Play(Make<MediaTrainingEffect>(("_glamourCost", 2), ("_drawCount", 1)));
            Glamour(6);
            Assert.IsFalse(_battle.TryUseMediaTraining());
            _battle.Celebrity.ResetTurn();
            int hand = _battle.PlayerDeck.HandCount;
            Assert.IsTrue(_battle.TryUseMediaTraining());
            Assert.AreEqual(4, _battle.CurrentGlamour);
            Assert.AreEqual(hand + 1, _battle.PlayerDeck.HandCount);
            Assert.IsFalse(_battle.TryUseMediaTraining());
            _battle.Celebrity.ResetTurn();
            _battle.Celebrity.OnCardPlayed(_soundbite);
            Assert.IsFalse(_battle.TryUseMediaTraining());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisarmingCharmSpendsGlamourWithoutChangingHostility()
        {
            var charm = Make<DisarmingCharmEffect>(("_glamourCost", 2), ("_weakenedStacks", 3));
            Glamour(4);
            _battle.Enemies[0].Stats.SetHostility(5);
            Play(charm);
            Assert.AreEqual(5, _battle.Enemies[0].Stats.CurrentHostility);
            Assert.AreEqual(3, _battle.Enemies[0].StatusEffects.GetStacks<WeakenedStatus>());
            Assert.AreEqual(2, _battle.CurrentGlamour);
            _battle.Enemies[0].Stats.SetHostility(-5);
            Play(charm);
            Assert.AreEqual(2, _battle.CurrentGlamour);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarstruckPaysOffOnceFromAnotherCardsHostilityChange()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(-5);
            Play(Starstruck(3));
            Assert.AreEqual(3, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            int before = _battle.Opinion.CurrentOpinion;
            enemy.Stats.GainHostility(6);
            // Crossing straight from receptive to hostile also pays the normal 3-Opinion betrayal cost.
            Assert.AreEqual(before + 7, _battle.Opinion.CurrentOpinion);
            Assert.AreEqual(0, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            enemy.Stats.GainHostility(1);
            Assert.AreEqual(before + 7, _battle.Opinion.CurrentOpinion);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarstruckRefreshesOnReapplyAndSkipsHostileTargets()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(-10);
            Play(Starstruck(3));
            enemy.StatusEffects.RemoveStacksNotify<StarstruckStatus>(1);
            Play(Starstruck(3));
            Assert.AreEqual(3, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            enemy.StatusEffects.RemoveStacksNotify<StarstruckStatus>(int.MaxValue);
            enemy.Stats.SetHostility(10);
            Play(Starstruck(3));
            Assert.AreEqual(0, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyPromiseSoothesNowAndAggravatesNextTurn()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(0);
            Play(
                Make<ReduceHostilityEffect>(("_amount", 4)),
                Make<DelayedEffect>(
                    ("_turnsDelay", 1),
                    (
                        "_effects",
                        new List<BattleEffect>
                        {
                            Make<RaiseAllOpponentsHostilityEffect>(("_amount", 2)),
                        }
                    )
                )
            );
            Assert.AreEqual(-4, enemy.Stats.CurrentHostility);
            _battle.RequestEndTurn();
            yield return WaitForPlayerTurn();
            Assert.AreEqual(-2, enemy.Stats.CurrentHostility);
        }

        [UnityTest]
        public IEnumerator RecoveryChargesDebtOnlyAfterSelectionAndBookingPreservesCopy()
        {
            CardChoiceRequest choice = null;
            _battle.CardChoicePrompt = e => choice = e;
            try
            {
                var discarded = Card(_objects, "In discard", CardType.Rhetoric, 0);
                _battle.PlayerDeck.AddCardToDiscard(discarded);
                Play(Make<BorrowCardSelectionEffect>(("_nextTurn", false), ("_debt", 1)));
                Assert.IsNotNull(choice);
                Assert.AreEqual(0, _battle.Celebrity.Debt);
                choice.OnConfirmed(new List<CardData> { discarded });
                choice.OnConfirmed(new List<CardData> { discarded });
                Assert.AreEqual(1, _battle.Celebrity.Debt);
                choice = null;
                int total = _battle.PlayerDeck.AllCards.Count;
                Play(Make<BorrowCardSelectionEffect>(("_nextTurn", true), ("_debt", 2)));
                Assert.IsNotNull(choice);
                choice.OnConfirmed(new List<CardData> { _pressure });
                Assert.AreEqual(3, _battle.Celebrity.Debt);
                int hand = _battle.PlayerDeck.HandCount;
                _battle.PlayerDeck.DeliverBookedCards();
                Assert.AreEqual(hand + 1, _battle.PlayerDeck.HandCount);
                Assert.AreEqual(total, _battle.PlayerDeck.AllCards.Count);
            }
            finally
            {
                _battle.CardChoicePrompt = null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarstruckTicksThreeEnemyTurnsThenExpires()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(-10);
            Play(Starstruck(3));
            for (int turn = 1; turn <= 3; turn++)
            {
                _battle.RequestEndTurn();
                yield return WaitForPlayerTurn();
                Assert.AreEqual(-10 + turn * 2, enemy.Stats.CurrentHostility);
                Assert.AreEqual(3 - turn, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            }
        }

        [UnityTest]
        public IEnumerator PhotoOpRewardsOnlyFirstPressureAndPersecutionBypassesComposure()
        {
            _battle.PlayerStats.GainActionPoints(20);
            var photo = Policy(
                _objects,
                "Glamour on first Pressure",
                1,
                Make<BattlePassive>(
                    (
                        "_trigger",
                        Make<CardPlayedTrigger>(
                            ("_filterByType", true),
                            ("_filterType", CardType.Pressure)
                        )
                    ),
                    ("_oncePerTurn", true),
                    ("_effects", new List<BattleEffect> { GainGlamour(1) })
                )
            );
            _battle.PlayerDeck.AddCardToHand(photo);
            _battle.RequestPlayCard(photo, _battle.PlayerDeck.Hand.ToList().IndexOf(photo));
            _battle.RequestPlayCard(_pressure, _battle.PlayerDeck.Hand.ToList().IndexOf(_pressure));
            Assert.AreEqual(1, _battle.CurrentGlamour);
            _battle.RequestPlayCard(_pressure, _battle.PlayerDeck.Hand.ToList().IndexOf(_pressure));
            Assert.AreEqual(1, _battle.CurrentGlamour);
            Glamour(4);
            _battle.GainSupport(20);
            int before = _battle.Opinion.CurrentOpinion;
            Play(
                Make<LowerOwnOpinionEffect>(("_amount", 5), ("_bypassSupport", true)),
                Make<GainGlamourEffect>(
                    ("_amount", 0),
                    ("_amountSource", EffectContextValue.CurrentGlamour)
                )
            );
            Assert.AreEqual(before - 5, _battle.Opinion.CurrentOpinion);
            Assert.AreEqual(20, _battle.CurrentSupport);
            Assert.AreEqual(10, _battle.CurrentGlamour);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharmOffensiveSpendsAllEnergyAndScalesThreeEffects()
        {
            _battle.Celebrity.GainDebt(3);
            Play(PaidOffPromises());
            _battle.Celebrity.ResetTurn();
            var card = Card(
                _objects,
                "Charm Offensive",
                CardType.Pressure,
                -1,
                Make<CharmOffensiveEffect>(("_swayAndComposurePerEnergy", 2))
            );
            _battle.PlayerDeck.AddCardToHand(card);
            _battle.Enemies[0].Stats.SetHostility(5);
            _battle.PlayerStats.SpendActionPoints(_battle.PlayerStats.CurrentActionPoints);
            _battle.PlayerStats.GainActionPoints(3);
            _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().IndexOf(card));
            Assert.AreEqual(0, _battle.PlayerStats.CurrentActionPoints);
            Assert.AreEqual(3, _battle.CardPlay.LastEnergyPaid);
            Assert.AreEqual(0, _battle.Celebrity.PromisedDebt);
            Assert.AreEqual(6, _battle.CurrentSupport);
            // The ordinary crowd reaction raises Hostility by one after the card's -3.
            Assert.AreEqual(3, _battle.Enemies[0].Stats.CurrentHostility);
            yield return null;
        }

        private EffectResolver Resolver =>
            (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);

        /// <summary>Resolves a card's effects as a player play, without paying or moving it.</summary>
        private EffectExecutionContext Play(CardData card) =>
            Resolver.ResolveCardEffects(card, true);

        /// <summary>Resolves the given effects as one free player card.</summary>
        private EffectExecutionContext Play(params BattleEffect[] effects) =>
            Play(Card(_objects, "Test card", CardType.Rhetoric, 0, effects));

        private static GainGlamourEffect GainGlamour(int amount) =>
            Make<GainGlamourEffect>(("_amount", amount));

        private static IfContextValueEffect IfGlamour(
            ComparisonType comparison,
            int threshold,
            params BattleEffect[] then
        ) =>
            Make<IfContextValueEffect>(
                ("_value", EffectContextValue.CurrentGlamour),
                ("_comparison", comparison),
                ("_threshold", threshold),
                ("_then", then.ToList())
            );

        private static ApplyStatusBehaviorEffect Starstruck(int turns) =>
            Make<ApplyStatusBehaviorEffect>(
                ("_target", TargetType.Opponent),
                (
                    "_behavior",
                    Make<StarstruckStatus>(("_hostilityPerTurn", 2), ("_swayOnHostile", 10))
                ),
                ("_stacks", turns),
                ("_duration", StatusDurationType.DecreasePerTurn)
            );

        private BattleEffect[] PaidOffPromises() =>
            new BattleEffect[]
            {
                Make<PaidOffPromisesEffect>(("_debtAmount", 3), ("_minimumPrintedCost", 2)),
                new ExhaustThisCardEffect(),
            };

        private void Glamour(int amount) =>
            _battle.PlayerStatusEffects.ApplyStatus(
                StatusRegistry.Get<GlamourStatus>(),
                amount,
                StatusDurationType.Permanent
            );

        private IEnumerator WaitForPlayerTurn()
        {
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
        }
    }
}
