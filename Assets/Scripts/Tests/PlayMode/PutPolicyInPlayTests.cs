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
    /// An ally with a battle-start <see cref="PutPolicyInPlayEffect"/> (Megachurch Board's shape):
    /// the Policy's passives are live from the first turn though the card was never played. The
    /// Policy here draws a card whenever an enemy gains a status, so a status applied to both
    /// enemies draws two.
    /// </summary>
    public class PutPolicyInPlayTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;
        private CardData _policy;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _policy = Policy(
                _objects,
                "Test Policy",
                1,
                Make<BattlePassive>(
                    ("_trigger", new StatusAppliedToEnemyTrigger()),
                    ("_effects", new List<BattleEffect> { Make<DrawCardsEffect>(("_amount", 1)) })
                )
            );

            var ally = ScriptableObject.CreateInstance<AllyData>();
            _objects.Add(ally);
            Set(ally, "_allyName", "Board");
            Set(
                ally,
                "_passives",
                new List<BattlePassive>
                {
                    Make<BattlePassive>(
                        ("_trigger", new BattleStartTrigger()),
                        (
                            "_effects",
                            new List<BattleEffect> { Make<PutPolicyInPlayEffect>(("_policy", _policy)) }
                        )
                    ),
                }
            );

            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                enemies.Add(enemy);
            }

            var filler = Card(_objects, "Filler", CardType.Rhetoric, 1);
            var host = new GameObject("Put Policy in play test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(new[] { ally });
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = enemies,
                    playerDeck = Enumerable.Repeat(filler, 15).ToList(),
                    startingOpinion = 50,
                    maxTurns = 20,
                }
            );
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator ThePolicysPassiveIsLiveWithoutPlayingIt()
        {
            var tag = Card(
                _objects,
                "Tag",
                CardType.Rhetoric,
                0,
                Make<ApplyStatusBehaviorEffect>(
                    ("_target", TargetType.AllOpponents),
                    ("_behavior", new JadedStatus()),
                    ("_stacks", 1),
                    ("_duration", StatusDurationType.Permanent)
                )
            );
            int before = _battle.PlayerDeck.HandCount;
            Resolver.ResolveCardEffects(tag, true);

            Assert.AreEqual(before + 2, _battle.PlayerDeck.HandCount, "one draw per enemy that gained a status");
            Assert.IsFalse(_battle.PlayerDeck.ExhaustPile.Contains(_policy), "the Policy card never moved");
            yield return null;
        }

        private EffectResolver Resolver =>
            (EffectResolver)
                typeof(BattleManager)
                    .GetField("_effectResolver", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(_battle);
    }
}
