using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Nepo Baby's burn, return/replay and calm lanes (docs/nepo-baby-class.md sections 4 and 6),
    /// each checked through the effect that implements it. Cards are built in code; class rules
    /// come from NepoBabyConfig, so the asserts read it rather than hardcoding them.
    /// </summary>
    public class NepoBabyMechanicTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;
        private CardData _pressure; // costs 1, Sway 5

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _pressure = Card(
                _objects,
                "Pressure",
                CardType.Pressure,
                1,
                Make<ApplyOpinionEffect>(("_amount", 5))
            );
            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                enemies.Add(enemy);
            }
            var host = new GameObject("Nepo Baby mechanic test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(System.Array.Empty<AllyData>());
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = enemies,
                    playerOrigin = OriginType.NepoBaby,
                    playerDeck = Enumerable.Repeat(_pressure, 12).ToList(),
                    startingOpinion = 50,
                    maxTurns = 20,
                }
            );
            yield return WaitForPlayerTurn();
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
        public IEnumerator BurnOffersNoPolicyAndPaysPerBurnedCard()
        {
            var policy = Policy(_objects, "Policy", 1);
            _battle.PlayerDeck.AddCardToHand(policy);
            _battle.PlayerDeck.AddCardToHand(_pressure);
            IReadOnlyList<CardData> offered = null;
            _battle.CardChoicePrompt = request =>
            {
                offered = request.Choices;
                request.OnConfirmed(new List<CardData> { _pressure });
            };

            int support = _battle.CurrentSupport;
            var ctx = Play(
                Make<BurnEffect>(
                    ("_perBurnedCard", new List<BattleEffect> { Make<GainSupportEffect>(("_amount", 3)) })
                )
            );

            CollectionAssert.DoesNotContain(offered, policy);
            Assert.AreEqual(1, ctx.CardsBurned);
            Assert.AreEqual(_pressure.PrintedCost, ctx.LastBurnedPrintedCost);
            CollectionAssert.Contains(_battle.PlayerDeck.ExhaustPile, _pressure);
            Assert.AreEqual(support + 3, _battle.CurrentSupport);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReturnedCardComesBackAtTheReturnBrake()
        {
            var heirloom = Card(
                _objects,
                "Heirloom",
                CardType.Pressure,
                1,
                Make<ApplyOpinionEffect>(("_amount", 5)),
                new ReturnToHandEffect()
            );
            _battle.PlayerDeck.AddCardToHand(heirloom);
            _battle.PlayerStats.GainActionPoints(1);
            _battle.RequestPlayCard(heirloom, _battle.PlayerDeck.Hand.ToList().IndexOf(heirloom));

            CollectionAssert.Contains(_battle.PlayerDeck.Hand, heirloom);
            Assert.AreEqual(
                NepoBabyConfig.Current.ReturnInTurnCostIncrease,
                _battle.PlayerDeck.GetCostIncreaseThisTurn(heirloom)
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReplayHostilityFollowsTheConfig()
        {
            var config = NepoBabyConfig.Current;
            int original = config.ReplayHostilityPerEnemy;
            var support = Card(
                _objects,
                "Support",
                CardType.Pressure,
                1,
                Make<GainSupportEffect>(("_amount", 2))
            );
            try
            {
                foreach (int hostility in new[] { 0, 2 })
                {
                    Set(config, "_replayHostilityPerEnemy", hostility);
                    var before = _battle.Enemies.Select(e => e.Stats.CurrentHostility).ToList();
                    Assert.IsTrue(_battle.CardPlay.ReplayCard(support));
                    for (int i = 0; i < _battle.Enemies.Count; i++)
                        Assert.AreEqual(
                            before[i] + hostility,
                            _battle.Enemies[i].Stats.CurrentHostility,
                            $"replay with ReplayHostilityPerEnemy = {hostility}"
                        );
                }
            }
            finally
            {
                Set(config, "_replayHostilityPerEnemy", original);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CalmStopsAtNeutral()
        {
            var enemy = _battle.Enemies[_battle.FocusedEnemyIndex];
            var calm = Make<ReduceHostilityEffect>(("_amount", 2), ("_stopAtNeutral", true));

            enemy.Stats.SetHostility(5);
            Play(calm);
            Assert.AreEqual(3, enemy.Stats.CurrentHostility);

            enemy.Stats.SetHostility(1);
            Play(calm);
            Assert.AreEqual(0, enemy.Stats.CurrentHostility);
            yield return null;
        }

        private EffectResolver Resolver =>
            (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);

        /// <summary>Resolves the given effects as one free player card, without moving it.</summary>
        private EffectExecutionContext Play(params BattleEffect[] effects) =>
            Resolver.ResolveCardEffects(Card(_objects, "Test card", CardType.Rhetoric, 0, effects), true);

        private IEnumerator WaitForPlayerTurn()
        {
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
        }
    }
}
