using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// The Guilt-only Faith Leader engine: scaling per guilty enemy (Fire and Brimstone), Guilt
    /// when an enemy turns hostile (Still Small Voice), full-house energy (Tithe / Altar Call /
    /// Answered Prayer) and the per-conversion discount on the payoffs (Us vs Them, Zealotry, …).
    /// Each card is rebuilt in code from the blocks its asset uses, with the numbers stated here.
    /// </summary>
    public class FaithLeaderGuiltTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var filler = Card(
                _objects,
                "Filler",
                CardType.Pressure,
                1,
                Make<GainSupportEffect>(("_amount", 1))
            );
            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                enemies.Add(enemy);
            }
            var host = new GameObject("Faith Leader Guilt test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(System.Array.Empty<AllyData>());
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = enemies,
                    playerDeck = Enumerable.Repeat(filler, 12).ToList(),
                    startingOpinion = 50,
                    maxTurns = 20,
                }
            );
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
            foreach (var enemy in _battle.Enemies)
                enemy.Stats.SetHostility(0);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator StrengthScalesWithEachGuiltyEnemy()
        {
            // Fire and Brimstone: for each enemy with Guilt, gain 1 Strength.
            var brimstone = Card(
                _objects,
                "Fire and Brimstone",
                CardType.Pressure,
                0,
                Make<ForEachEnemyWithStatusEffect>(
                    ("_filterStatus", StatusRegistry.Get<GuiltStatus>()),
                    (
                        "_effects",
                        new List<BattleEffect>
                        {
                            Make<ApplyStatusBehaviorEffect>(
                                ("_target", TargetType.Self),
                                ("_behavior", StatusRegistry.Get<StrengthStatus>()),
                                ("_stacks", 1),
                                ("_duration", StatusDurationType.Permanent)
                            ),
                        }
                    )
                )
            );

            PlayFromHand(brimstone);
            Assert.AreEqual(0, Strength(), "no guilty enemy, no Strength");

            Guilt(0, 1);
            PlayFromHand(brimstone);
            Assert.AreEqual(1, Strength());

            Guilt(1, 2);
            PlayFromHand(brimstone);
            Assert.AreEqual(3, Strength(), "one per guilty enemy, not per Guilt stack");
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnEnemyTurningHostileGainsGuilt()
        {
            // Still Small Voice: a Policy; when an enemy becomes hostile, it gains 1 Guilt.
            var voice = Policy(
                _objects,
                "Still Small Voice",
                0,
                Make<BattlePassive>(
                    ("_trigger", new EnemyBecameHostileTrigger()),
                    (
                        "_effects",
                        new List<BattleEffect>
                        {
                            Make<ApplyStatusBehaviorEffect>(
                                ("_target", TargetType.TriggeringEnemy),
                                ("_behavior", StatusRegistry.Get<GuiltStatus>()),
                                ("_stacks", 1),
                                ("_duration", StatusDurationType.Permanent)
                            ),
                        }
                    )
                )
            );
            PlayFromHand(voice);

            var rile = Card(
                _objects,
                "Rile",
                CardType.Rhetoric,
                0,
                Make<RaiseTargetHostilityEffect>(
                    ("_amount", 8),
                    ("_target", TargetType.Opponent)
                )
            );
            var target = _battle.Enemies.First(e => !e.Stats.IsHostile);
            Assume.That(target, Is.Not.Null);
            PlayFromHand(rile);

            var riled = _battle.Enemies.Where(e => e.Stats.IsHostile).ToList();
            Assert.AreEqual(1, riled.Count, "the card made exactly one enemy hostile");
            Assert.AreEqual(1, riled[0].StatusEffects.GetStacks<GuiltStatus>());
            Assert.IsTrue(
                _battle
                    .Enemies.Where(e => !e.Stats.IsHostile)
                    .All(e => e.StatusEffects.GetStacks<GuiltStatus>() == 0),
                "enemies that stayed calm gain no Guilt"
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullHouseEnergyOnlyWhenEveryEnemyIsReceptive()
        {
            // Altar Call / Answered Prayer: if the whole room is receptive, gain 1 energy.
            var altarCall = Card(
                _objects,
                "Altar Call",
                CardType.Pressure,
                0,
                Make<IfContextValueEffect>(
                    ("_value", EffectContextValue.RoomFullyReceptive),
                    ("_comparison", ComparisonType.AtLeast),
                    ("_threshold", 1),
                    (
                        "_then",
                        new List<BattleEffect> { Make<GainActionPointsEffect>(("_amount", 1)) }
                    )
                )
            );

            _battle.Enemies[0].Stats.SetHostility(-8);
            Assume.That(_battle.Enemies[0].Stats.IsHostile, Is.False);
            Assert.IsFalse(_battle.IsEchoChamber, "one enemy is still neutral");
            int energy = _battle.PlayerStats.CurrentActionPoints;
            PlayFromHand(altarCall);
            Assert.AreEqual(energy, _battle.PlayerStats.CurrentActionPoints);

            _battle.Enemies[1].Stats.SetHostility(-8);
            Assert.IsTrue(_battle.IsEchoChamber, "every living enemy is receptive");
            energy = _battle.PlayerStats.CurrentActionPoints;
            PlayFromHand(altarCall);
            Assert.AreEqual(
                energy + 1,
                _battle.PlayerStats.CurrentActionPoints,
                "one energy per play, not per receptive enemy"
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator PayoffCostsOneLessPerConversionThisTurnAndFloorsAtZero()
        {
            var payoff = Discounted(Card(_objects, "Payoff", CardType.Pressure, 3));
            var cheap = Discounted(Card(_objects, "Cheap payoff", CardType.Pressure, 1));
            Assert.AreEqual(3, _battle.CardPlay.GetEffectiveCardCost(payoff));

            Convert(0);
            Assert.AreEqual(1, _battle.ConversionsThisTurn);
            Assert.AreEqual(2, _battle.CardPlay.GetEffectiveCardCost(payoff));

            Convert(1);
            Assert.AreEqual(2, _battle.ConversionsThisTurn);
            Assert.AreEqual(1, _battle.CardPlay.GetEffectiveCardCost(payoff));
            Assert.AreEqual(0, _battle.CardPlay.GetEffectiveCardCost(cheap), "floors at 0");
            yield return null;
        }

        private CardData Discounted(CardData card)
        {
            Set(card, "_costReductionPerX", EffectContextValue.ConversionsThisTurn);
            return card;
        }

        private void Convert(int enemyIndex)
        {
            Guilt(enemyIndex, FaithLeaderConfig.Current.ConversionThreshold);
            var convert = Card(
                _objects,
                "Convert",
                CardType.Pressure,
                0,
                Make<ConvertPacifiedEffect>(
                    ("_target", TargetType.AllOpponents),
                    (
                        "_awards",
                        new List<ConvertPacifiedEffect.AwardEntry>
                        {
                            new()
                            {
                                Behavior = StatusRegistry.Get<FanaticStatus>(),
                                Stacks = 1,
                                Duration = StatusDurationType.DecreasePerTurn,
                            },
                        }
                    )
                )
            );
            PlayFromHand(convert);
            Assert.IsTrue(_battle.Enemies[enemyIndex].StatusEffects.HasStatus<FanaticStatus>());
        }

        private void Guilt(int enemyIndex, int stacks) =>
            _battle
                .Enemies[enemyIndex]
                .StatusEffects.ApplyStatus(
                    StatusRegistry.Get<GuiltStatus>(),
                    stacks,
                    StatusDurationType.Permanent
                );

        private int Strength() => _battle.PlayerStatusEffects.GetStacks<StrengthStatus>();

        private void PlayFromHand(CardData card)
        {
            _battle.PlayerDeck.AddCardToHand(card);
            _battle.PlayerStats.GainActionPoints(_battle.CardPlay.GetEffectiveCardCost(card));
            _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().LastIndexOf(card));
        }
    }
}
