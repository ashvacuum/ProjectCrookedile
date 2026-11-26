using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Data.Save;
using Crookedile.Gameplay.Battle;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Crookedile.Tests.TestContent;

namespace Crookedile.Tests
{
    /// <summary>
    /// Mid-battle resume: a battle replayed from its seed and recorded decisions ends up
    /// exactly where the original stood.
    /// </summary>
    public class BattleReplayTests
    {
        private const int Seed = 4242;
        private readonly List<Object> _objects = new();
        private CardData _push;
        private CardData _brace;
        private List<EnemyData> _enemies;

        [SetUp]
        public void SetUp()
        {
            _push = Card(_objects, "Push", CardType.Pressure, 1, Make<ApplyOpinionEffect>(("_amount", 3)));
            _brace = Card(_objects, "Brace", CardType.Rhetoric, 1, Make<GainSupportEffect>(("_amount", 2)));
            _enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                _enemies.Add(enemy);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
        }

        private BattleSetup Setup() =>
            new BattleSetup
            {
                enemies = _enemies,
                // Mixed so the seeded shuffle decides every hand.
                playerDeck = Enumerable.Repeat(_push, 6).Concat(Enumerable.Repeat(_brace, 6)).ToList(),
                startingOpinion = 50,
                maxTurns = 20,
                seed = Seed,
            };

        private BattleManager NewBattle()
        {
            var host = new GameObject("Replay test");
            _objects.Add(host);
            var battle = host.AddComponent<BattleManager>();
            battle.ConfigureForSimulation(System.Array.Empty<AllyData>());
            return battle;
        }

        private static IEnumerator WaitForPlayerTurn(BattleManager battle)
        {
            for (
                int i = 0;
                i < 300
                    && !(battle.CurrentState == BattleState.PlayerTurn && !battle.IsCardResolving)
                    && battle.CurrentState != BattleState.BattleEnd;
                i++
            )
                yield return null;
        }

        private static string Snapshot(BattleManager battle) =>
            $"turn {battle.CurrentTurn}, opinion {battle.CurrentOpinion}, hand "
            + string.Join(",", battle.PlayerDeck.Hand.Select(c => c.CardName));

        [UnityTest]
        public IEnumerator ReplayRebuildsTheSameBattle()
        {
            var original = NewBattle();
            original.StartBattle(Setup());
            yield return WaitForPlayerTurn(original);

            // Three turns of play: alternate targets so the focus is part of the record.
            for (int turn = 0; turn < 3 && original.CurrentState != BattleState.BattleEnd; turn++)
            {
                for (int play = 0; play < 2; play++)
                {
                    var hand = original.PlayerDeck.Hand;
                    int pick = -1;
                    for (int i = 0; i < hand.Count && pick < 0; i++)
                        if (original.CanPlayCard(hand[i]))
                            pick = i;
                    if (pick < 0)
                        break;
                    original.SetFocusedEnemy((turn + play) % 2);
                    original.RequestPlayCard(hand[pick], pick);
                    yield return WaitForPlayerTurn(original);
                }
                original.RequestEndTurn();
                yield return null;
                yield return WaitForPlayerTurn(original);
            }

            string expected = Snapshot(original);
            var inputs = new List<BattleInput>(original.Inputs);
            Assert.Greater(inputs.Count, 3, "The original battle recorded its decisions.");
            Object.DestroyImmediate(original.gameObject);

            var replayed = NewBattle();
            yield return replayed.StartBattleWithReplay(Setup(), inputs).ToCoroutine();
            yield return WaitForPlayerTurn(replayed);

            Assert.AreEqual(expected, Snapshot(replayed));
            Assert.AreEqual(inputs.Count, replayed.Inputs.Count, "The replay re-recorded every decision.");
        }

        [Test]
        public void BattleLogRoundTrips()
        {
            var log = new BattleLogData
            {
                EncounterId = "enc",
                BattleIndex = 2,
                Inputs =
                {
                    new BattleInput { Kind = BattleInputKind.PlayCard, HandIndex = 3, TargetIndex = 1, Picks = new List<int>() },
                    new BattleInput { Kind = BattleInputKind.CardChoice, Picks = new List<int> { 0, 2 } },
                    new BattleInput { Kind = BattleInputKind.EndTurn, Picks = new List<int>() },
                },
            };
            var read = BattleLogData.FromBytes(log.ToBytes());
            Assert.AreEqual("enc", read.EncounterId);
            Assert.AreEqual(2, read.BattleIndex);
            Assert.AreEqual(3, read.Inputs.Count);
            Assert.AreEqual(3, read.Inputs[0].HandIndex);
            Assert.AreEqual(1, read.Inputs[0].TargetIndex);
            CollectionAssert.AreEqual(new[] { 0, 2 }, read.Inputs[1].Picks);
            Assert.AreEqual(BattleInputKind.EndTurn, read.Inputs[2].Kind);
        }
    }
}
