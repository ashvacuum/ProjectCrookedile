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
    /// Celebrity's starting ally, the Publicist: at battle start every enemy is Exposed, and the
    /// first hit on each one deals double. The ally is built in code with the same blocks as
    /// Data/Allies/Publicist.asset, so retuning the asset never breaks this.
    /// </summary>
    public class CelebrityStartingAllyTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var ally = ScriptableObject.CreateInstance<AllyData>();
            _objects.Add(ally);
            Set(ally, "_allyName", "Publicist");
            var passive = Make<BattlePassive>(
                ("_trigger", new BattleStartTrigger()),
                (
                    "_effects",
                    new List<BattleEffect>
                    {
                        Make<ApplyStatusBehaviorEffect>(
                            ("_target", TargetType.AllOpponents),
                            ("_behavior", new ExposedStatus()),
                            ("_stacks", 1),
                            ("_duration", StatusDurationType.Permanent)
                        ),
                    }
                )
            );
            Set(ally, "_passives", new List<BattlePassive> { passive });

            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                enemies.Add(enemy);
            }

            var pressure = Card(
                _objects,
                "Pressure",
                CardType.Pressure,
                1,
                Make<ApplyOpinionEffect>(("_amount", 6))
            );
            var host = new GameObject("Celebrity starting ally test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(new[] { ally });
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = enemies,
                    playerDeck = Enumerable.Repeat(pressure, 12).ToList(),
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
        public IEnumerator EveryEnemyStartsExposedAndTheFirstHitOnEachDealsDouble()
        {
            Assert.IsTrue(
                _battle.Enemies.All(e => e.StatusEffects.GetStacks<ExposedStatus>() == 1),
                "every enemy starts Exposed"
            );

            var hit = Card(
                _objects,
                "Hit",
                CardType.Pressure,
                0,
                Make<ApplyOpinionEffect>(("_amount", 4))
            );
            int before = _battle.Opinion.CurrentOpinion;
            Resolver.ResolveCardEffects(hit, true);
            int first = _battle.Opinion.CurrentOpinion - before;

            Assert.AreEqual(
                1,
                _battle.Enemies.Count(e => e.StatusEffects.GetStacks<ExposedStatus>() == 0),
                "the hit consumes Exposed on its target only"
            );

            before = _battle.Opinion.CurrentOpinion;
            Resolver.ResolveCardEffects(hit, true);
            int second = _battle.Opinion.CurrentOpinion - before;

            Assert.Greater(second, 0);
            Assert.AreEqual(second * 2, first, "the Exposed hit dealt double");
            yield return null;
        }

        private EffectResolver Resolver =>
            (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);
    }
}
