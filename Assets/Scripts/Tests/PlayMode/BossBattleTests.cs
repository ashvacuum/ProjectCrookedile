using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Boss;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Crookedile.Tests
{
    public class BossBattleTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator BehaviourDesignerSelectsBundleFromOpinion()
        {
#if UNITY_EDITOR
            var boss = UnityEditor.AssetDatabase.LoadAssetAtPath<BossData>(
                "Assets/Data/Bosses/Debate Prototype/Rival Candidate.asset"
            );
            Assert.IsNotNull(boss, "Generate the sample debate before running this test.");
            var card = Asset<CardData>();
            var audience = Asset<EnemyData>();
            Set(audience, "_minHostility", -10);
            var host = new GameObject("Planning graph test");
            _objects.Add(host);
            var battle = host.AddComponent<BattleManager>();
            battle.ConfigureForSimulation(new OriginPassive[0]);
            battle.StartBattle(
                new BattleSetup
                {
                    boss = boss,
                    enemies = new List<EnemyData> { audience, audience },
                    playerDeck = new List<CardData> { card, card, card, card, card },
                    startingOpinion = 80,
                }
            );
            for (int i = 0; i < 120 && battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, battle.CurrentState);
            Assert.AreEqual("Protect the lead", battle.Boss.BundleName);
            Assert.AreEqual(3, battle.Boss.Intents.Count);
            var revealed = battle.Boss.BundleName;
            battle.Opinion.DecayOpinion(60);
            yield return null;
            Assert.AreEqual(revealed, battle.Boss.BundleName);
            battle.RequestEndTurn();
            for (int i = 0; i < 120 && battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, battle.CurrentState);
            Assert.AreEqual("Opening statements", battle.Boss.BundleName);
            foreach (var member in battle.Enemies)
                member.Stats.SetHostility(-6);
            battle.RequestEndTurn();
            for (int i = 0; i < 120 && battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, battle.CurrentState);
            Assert.AreEqual("Win them back", battle.Boss.BundleName);
#else
            yield return null;
#endif
        }

        [UnityTest]
        public IEnumerator FinalBossAndAudienceResponsePrecedeJudgment()
        {
            var card = Asset<CardData>();
            var first = Move("First", 5);
            var second = Move("Second", 5);
            var audienceMove = Move("Audience", 3);
            var boss = Asset<BossData>();
            var bundle = new BossMoveBundle();
            Set(bundle, "_name", "Test");
            Set(bundle, "_moves", new List<EnemyMoveData> { first, second });
            Set(boss, "_bundles", new List<BossMoveBundle> { bundle });
            var audience = Asset<EnemyData>();
            Set(audience, "_startingHostility", 5);
            Set(audience, "_aggressiveMoves", new List<EnemyMoveData> { audienceMove });
            var host = new GameObject("Boss test");
            _objects.Add(host);
            var battle = host.AddComponent<BattleManager>();
            battle.ConfigureForSimulation(new OriginPassive[0]);
            var order = new List<string>();
            void BossActing(BossActingEvent e) => order.Add(e.Move.MoveName);
            void EnemyActing(EnemyActingEvent e) => order.Add("Audience");
            void Judgment(JudgmentEvent e) => order.Add("Judgment");
            EventBus.Subscribe<BossActingEvent>(BossActing);
            EventBus.Subscribe<EnemyActingEvent>(EnemyActing);
            EventBus.Subscribe<JudgmentEvent>(Judgment);
            try
            {
                battle.StartBattle(
                    new BattleSetup
                    {
                        boss = boss,
                        enemies = new List<EnemyData> { audience },
                        playerDeck = new List<CardData> { card, card, card, card, card },
                        maxTurns = 1,
                        startingOpinion = 50,
                    }
                );
                for (int i = 0; i < 120 && battle.CurrentState != BattleState.PlayerTurn; i++)
                    yield return null;
                Assert.AreEqual(BattleState.PlayerTurn, battle.CurrentState);
                Assert.AreEqual(2, battle.Boss.Intents.Count);
                Assert.AreEqual(1, battle.Enemies.Count);
                battle.RequestEndTurn();
                for (int i = 0; i < 120 && battle.CurrentState != BattleState.BattleEnd; i++)
                    yield return null;
                CollectionAssert.AreEqual(
                    new[] { "First", "Second", "Audience", "Judgment" },
                    order
                );
                Assert.AreEqual(37, battle.GetBattleResult().finalOpinion);
                Assert.IsFalse(battle.GetBattleResult().isVictory);

                battle.StartBattle(
                    new BattleSetup
                    {
                        enemies = new List<EnemyData> { audience },
                        playerDeck = new List<CardData> { card, card, card, card, card },
                        maxTurns = 1,
                        startingOpinion = 50,
                    }
                );
                Assert.IsNull(battle.Boss);
                for (int i = 0; i < 120 && battle.CurrentState != BattleState.PlayerTurn; i++)
                    yield return null;
                battle.RequestEndTurn();
                Assert.AreEqual(BattleState.BattleEnd, battle.CurrentState);
                Assert.AreEqual(50, battle.GetBattleResult().finalOpinion);
            }
            finally
            {
                EventBus.Unsubscribe<BossActingEvent>(BossActing);
                EventBus.Unsubscribe<EnemyActingEvent>(EnemyActing);
                EventBus.Unsubscribe<JudgmentEvent>(Judgment);
            }
        }

        private EnemyMoveData Move(string name, int amount)
        {
            var move = Asset<EnemyMoveData>();
            var effect = new ApplyOpinionEffect();
            Set(effect, "_amount", amount);
            Set(move, "_moveName", name);
            Set(move, "_effects", new List<BattleEffect> { effect });
            return move;
        }

        private T Asset<T>()
            where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _objects.Add(asset);
            return asset;
        }

        private static void Set(object owner, string name, object value)
        {
            owner
                .GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(owner, value);
        }
    }
}
