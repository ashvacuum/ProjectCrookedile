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

namespace Crookedile.Tests
{
    public class CelebrityCardTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
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
                    playerDeck = Enumerable.Repeat(Card("Hot Take"), 12).ToList(),
                    startingOpinion = 50,
                    maxTurns = 20,
                }
            );
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
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
            var resolver = (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);
            var context = resolver.CreateContext(true);
            var openTab = Card("Open Tab").Effects.OfType<OpenTabEffect>().Single();
            openTab.Execute(context, 2);
            openTab.Execute(context, 3);
            Assert.AreEqual(5, _battle.Celebrity.FreeBorrowsPerTurn);
            Card("Too Big to Fail").Effects.OfType<DebtWaiverEffect>().Single().Execute(context, 2);
            Assert.AreEqual(2, _battle.Celebrity.DebtWaivers);
            Card("Rain Check")
                .Effects.OfType<DelayDebtSettlementEffect>()
                .Single()
                .Execute(context, 3);
            Assert.AreEqual(3, _battle.Celebrity.SettlementsDelayed);
            Card("Overdraft").Effects.OfType<OverdraftEffect>().Single().Execute(context, 2);
            int energy = _battle.PlayerStats.CurrentActionPoints;
            new BorrowEffect().Execute(context);
            Assert.AreEqual(energy + 6, _battle.PlayerStats.CurrentActionPoints);
            Assert.AreEqual(6, _battle.Celebrity.Debt);
            var bailout = Card("Bailout").Effects.OfType<BailoutEffect>().Single();
            bailout.Execute(context, 2);
            bailout.Execute(context, 3);
            Assert.AreEqual(5, _battle.Celebrity.BailoutCapPerTurn);
            Assert.AreSame(Card("Soundbite"), _battle.Celebrity.BailoutCard);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DedicatedNepoEffectsApplyAndConsumeTheirTurnSetups()
        {
            _battle.Opinion.DecayOpinion(30);
            var resolver = (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);
            var context = resolver.CreateContext(true);
            var card = Card("Hot Take");
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
        public IEnumerator GlamourThresholdsAndCashOutUseActualAssets()
        {
            Glamour(5);
            Play("Press Release");
            Assert.AreEqual(5, _battle.CurrentGlamour);
            Glamour(1);
            Play("Press Release");
            Assert.AreEqual(11, _battle.CurrentGlamour);
            _battle.PlayerStatusEffects.RemoveStacksNotify<GlamourStatus>(int.MaxValue);
            Play("Thumbs Up");
            Assert.AreEqual(2, _battle.CurrentGlamour);
            Glamour(4);
            _battle.GainDenial(4);
            int before = _battle.Opinion.CurrentOpinion;
            Play("Iconic Line");
            Assert.AreEqual(0, _battle.CurrentGlamour);
            Assert.AreEqual(before + 8, _battle.Opinion.CurrentOpinion);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovieQuotesReplaceCurrentAndFutureTokens()
        {
            var soundbite = Card("Soundbite");
            var quote = Card("Movie Quote");
            _battle.PlayerDeck.AddCardToHand(soundbite);
            _battle.PlayerDeck.AddCardToDiscard(soundbite);
            _battle.PlayerDeck.AddCardToDeck(soundbite);
            Play("Signature Catchphrase");
            Assert.IsFalse(_battle.PlayerDeck.AllCards.Contains(soundbite));
            Assert.AreEqual(3, _battle.PlayerDeck.AllCards.Count(c => c == quote));
            _battle.PlayerDeck.AddCardToHand(soundbite);
            Assert.AreEqual(2, _battle.PlayerDeck.Hand.Count(c => c == quote));
            var result = Play("Movie Quote");
            Assert.AreEqual(1, _battle.CurrentGlamour);
            Assert.IsTrue(result.ShouldExhaust);
            Assert.AreEqual(2, Play("Soundbite").LastDamageDealt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CreditAppliesToOneNewDebtGainAndDiscountExpires()
        {
            _battle.Celebrity.GainDebt(4);
            Play("Line of Credit");
            Assert.AreEqual(1, _battle.Celebrity.GainDebt(3, _battle.PlayerDeck));
            Assert.AreEqual(5, _battle.Celebrity.Debt);
            Assert.AreEqual(-1, _battle.PlayerDeck.GetCostIncreaseThisTurn(Card("Hot Take")));
            Assert.AreEqual(3, _battle.Celebrity.GainDebt(3, _battle.PlayerDeck));
            _battle.PlayerDeck.StartTurn(0);
            Assert.AreEqual(0, _battle.PlayerDeck.GetCostIncreaseThisTurn(Card("Hot Take")));
            _battle.Celebrity.ResetTurn();
            Assert.AreEqual(3, _battle.Celebrity.GainDebt(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator OverpromiseTriplesOnlyTheNextCardsSway()
        {
            Play("Overpromise");
            Assert.AreEqual(3, _battle.Celebrity.Debt);
            Assert.AreEqual(6, Play("Soundbite").LastDamageDealt);
            Assert.AreEqual(2, Play("Soundbite").LastDamageDealt);
            Play("Overpromise");
            _battle.Celebrity.ResetTurn();
            Assert.AreEqual(2, Play("Soundbite").LastDamageDealt);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PaidPromisesReserveDebtAndOnlyNextTurnCanFulfil()
        {
            _battle.Celebrity.GainDebt(5);
            Play("Paid-Off Promises");
            Assert.AreEqual(2, _battle.Celebrity.Debt);
            Assert.AreEqual(3, _battle.Celebrity.PromisedDebt);
            var costly = ScriptableObject.CreateInstance<CardData>();
            _objects.Add(costly);
            Set(costly, "_costs", new List<CardCost> { new(CostType.ActionPoints, 2) });
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
            Play("Settle Up");
            Assert.AreEqual(3, _battle.Celebrity.Debt);
            Play("Paid-Off Promises");
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
            Play("Media Training");
            Glamour(6);
            Assert.IsFalse(_battle.TryUseMediaTraining());
            _battle.Celebrity.ResetTurn();
            int hand = _battle.PlayerDeck.HandCount;
            Assert.IsTrue(_battle.TryUseMediaTraining());
            Assert.AreEqual(4, _battle.CurrentGlamour);
            Assert.AreEqual(hand + 1, _battle.PlayerDeck.HandCount);
            Assert.IsFalse(_battle.TryUseMediaTraining());
            _battle.Celebrity.ResetTurn();
            _battle.Celebrity.OnCardPlayed(Card("Soundbite"));
            Assert.IsFalse(_battle.TryUseMediaTraining());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisarmingCharmSpendsGlamourWithoutChangingHostility()
        {
            Glamour(4);
            _battle.Enemies[0].Stats.SetHostility(5);
            Play("Disarming Charm");
            Assert.AreEqual(5, _battle.Enemies[0].Stats.CurrentHostility);
            Assert.AreEqual(3, _battle.Enemies[0].StatusEffects.GetStacks<WeakenedStatus>());
            Assert.AreEqual(2, _battle.CurrentGlamour);
            _battle.Enemies[0].Stats.SetHostility(-5);
            Play("Disarming Charm");
            Assert.AreEqual(2, _battle.CurrentGlamour);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarstruckPaysOffOnceFromAnotherCardsHostilityChange()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(-5);
            Play("Never Meet Your Heroes");
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
            Play("Never Meet Your Heroes");
            enemy.StatusEffects.RemoveStacksNotify<StarstruckStatus>(1);
            Play("Never Meet Your Heroes");
            Assert.AreEqual(3, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            enemy.StatusEffects.RemoveStacksNotify<StarstruckStatus>(int.MaxValue);
            enemy.Stats.SetHostility(10);
            Play("Never Meet Your Heroes");
            Assert.AreEqual(0, enemy.StatusEffects.GetStacks<StarstruckStatus>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptyPromiseSoothesNowAndAggravatesNextTurn()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(0);
            Play("Empty Promise");
            Assert.AreEqual(-4, enemy.Stats.CurrentHostility);
            _battle.RequestEndTurn();
            for (int frame = 0; frame < 120 && _battle.CurrentState != BattleState.PlayerTurn; frame++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
            Assert.AreEqual(-2, enemy.Stats.CurrentHostility);
        }

        [UnityTest]
        public IEnumerator RecoveryChargesDebtOnlyAfterSelectionAndBookingPreservesCopy()
        {
            CardChoiceRequestedEvent choice = null;
            void OnChoice(CardChoiceRequestedEvent e) => choice = e;
            EventBus.Subscribe<CardChoiceRequestedEvent>(OnChoice);
            try
            {
                _battle.PlayerDeck.AddCardToDiscard(Card("Thumbs Up"));
                Play("Second Take");
                Assert.IsNotNull(choice);
                Assert.AreEqual(0, _battle.Celebrity.Debt);
                choice.OnConfirmed(new List<CardData> { Card("Thumbs Up") });
                choice.OnConfirmed(new List<CardData> { Card("Thumbs Up") });
                Assert.AreEqual(1, _battle.Celebrity.Debt);
                choice = null;
                int total = _battle.PlayerDeck.AllCards.Count;
                Play("Advance Booking");
                Assert.IsNotNull(choice);
                choice.OnConfirmed(new List<CardData> { Card("Hot Take") });
                Assert.AreEqual(3, _battle.Celebrity.Debt);
                int hand = _battle.PlayerDeck.HandCount;
                _battle.PlayerDeck.DeliverBookedCards();
                Assert.AreEqual(hand + 1, _battle.PlayerDeck.HandCount);
                Assert.AreEqual(total, _battle.PlayerDeck.AllCards.Count);
            }
            finally
            {
                EventBus.Unsubscribe<CardChoiceRequestedEvent>(OnChoice);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StarstruckTicksThreeEnemyTurnsThenExpires()
        {
            var enemy = _battle.Enemies[0];
            enemy.Stats.SetHostility(-10);
            Play("Never Meet Your Heroes");
            for (int turn = 1; turn <= 3; turn++)
            {
                _battle.RequestEndTurn();
                for (
                    int frame = 0;
                    frame < 120 && _battle.CurrentState != BattleState.PlayerTurn;
                    frame++
                )
                    yield return null;
                Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
                Assert.AreEqual(-10 + turn * 2, enemy.Stats.CurrentHostility);
                Assert.AreEqual(
                    3 - turn,
                    enemy.StatusEffects.GetStacks<StarstruckStatus>()
                );
            }
        }

        [UnityTest]
        public IEnumerator PhotoOpRewardsOnlyFirstPressureAndPersecutionBypassesComposure()
        {
            _battle.PlayerStats.GainActionPoints(20);
            var photo = Card("Photo Op");
            _battle.PlayerDeck.AddCardToHand(photo);
            _battle.RequestPlayCard(photo, _battle.PlayerDeck.Hand.ToList().IndexOf(photo));
            var pressure = Card("Hot Take");
            _battle.RequestPlayCard(pressure, _battle.PlayerDeck.Hand.ToList().IndexOf(pressure));
            Assert.AreEqual(1, _battle.CurrentGlamour);
            _battle.RequestPlayCard(pressure, _battle.PlayerDeck.Hand.ToList().IndexOf(pressure));
            Assert.AreEqual(1, _battle.CurrentGlamour);
            Glamour(4);
            _battle.GainSupport(20);
            int before = _battle.Opinion.CurrentOpinion;
            Play("Victim Narrative");
            Assert.AreEqual(before - 5, _battle.Opinion.CurrentOpinion);
            Assert.AreEqual(20, _battle.CurrentSupport);
            Assert.AreEqual(10, _battle.CurrentGlamour);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CharmOffensiveSpendsAllEnergyAndScalesThreeEffects()
        {
            _battle.Celebrity.GainDebt(3);
            Play("Paid-Off Promises");
            _battle.Celebrity.ResetTurn();
            var card = Card("Charm Offensive");
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

        private EffectExecutionContext Play(string name) =>
            (
                (EffectResolver)
                    typeof(BattleManager)
                        .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(_battle)
            ).ResolveCardEffects(Card(name), true);

        private void Glamour(int amount) =>
            _battle.PlayerStatusEffects.ApplyStatus(
                StatusRegistry.Get<GlamourStatus>(),
                amount,
                StatusDurationType.Permanent
            );

        private static void Set(object target, string field, object value) =>
            target
                .GetType()
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static CardData Card(string name)
        {
#if UNITY_EDITOR
            var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(
                $"Assets/Data/Cards/Celebrity/GlamourIou/{name}.asset"
            );
#else
            var card = CardDatabase.Shared.GetAll().FirstOrDefault(c => c.CardName == name);
#endif
            Assert.IsNotNull(card, name);
            return card;
        }
    }
}
