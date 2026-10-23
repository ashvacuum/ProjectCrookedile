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
    /// <summary>
    /// Faith Leader Fanatics follow your lead (core-design.md section 7): they push when you
    /// attack, add Support when you defend, and on expiry revert to neutral with Jaded. Numbers
    /// come from FaithLeaderConfig, so the asserts read it rather than hardcoding them.
    /// </summary>
    public class FanaticFollowerTests
    {
        private readonly List<Object> _objects = new();
        private BattleManager _battle;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                Set(enemy, "_minHostility", -10);
                enemies.Add(enemy);
            }
            var host = new GameObject("Fanatic follower test");
            _objects.Add(host);
            _battle = host.AddComponent<BattleManager>();
            _battle.ConfigureForSimulation(new OriginPassive[0]);
            _battle.StartBattle(
                new BattleSetup
                {
                    enemies = enemies,
                    playerDeck = Enumerable.Repeat(Card("Pressure"), 12).ToList(),
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
        public IEnumerator EachFanaticPushesWhenYouPlayAnAttack()
        {
            MakeFanatic(1, 2);
            var fanaticPushes = new List<int>();
            void OnDamage(DamageDealtEvent e)
            {
                if (e.AttackerName == "Fanatic")
                    fanaticPushes.Add(e.Amount);
            }
            EventBus.Subscribe<DamageDealtEvent>(OnDamage);
            try
            {
                PlayFromHand(Card("Pressure"));
            }
            finally
            {
                EventBus.Unsubscribe<DamageDealtEvent>(OnDamage);
            }
            CollectionAssert.AreEqual(
                new[] { FaithLeaderConfig.Current.FollowSwayPerFanatic },
                fanaticPushes
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator EachFanaticAddsSupportWhenYouPlayADefend()
        {
            var support = Card("Support");
            int before = _battle.CurrentSupport;
            PlayFromHand(support);
            int alone = _battle.CurrentSupport - before;
            Assert.Greater(alone, 0);

            MakeFanatic(0, 2);
            MakeFanatic(1, 2);
            before = _battle.CurrentSupport;
            PlayFromHand(support);
            Assert.AreEqual(
                alone + 2 * FaithLeaderConfig.Current.FollowSupportPerFanatic,
                _battle.CurrentSupport - before
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator SilencedFanaticDoesNotFollow()
        {
            MakeFanatic(1, 2);
            _battle
                .Enemies[1]
                .StatusEffects.ApplyStatus(
                    StatusRegistry.Get<SilencedStatus>(),
                    1,
                    StatusDurationType.DecreasePerTurn
                );
            int fanaticPushes = 0;
            void OnDamage(DamageDealtEvent e)
            {
                if (e.AttackerName == "Fanatic")
                    fanaticPushes++;
            }
            EventBus.Subscribe<DamageDealtEvent>(OnDamage);
            try
            {
                PlayFromHand(Card("Pressure"));
            }
            finally
            {
                EventBus.Unsubscribe<DamageDealtEvent>(OnDamage);
            }
            Assert.AreEqual(0, fanaticPushes);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExpiredFanaticRevertsToNeutralWithJaded()
        {
            var enemy = _battle.Enemies[1];
            MakeFanatic(1, 1);
            enemy.Stats.SetHostility(-5);
            _battle.RequestEndTurn();
            yield return WaitForPlayerTurn();
            Assert.IsFalse(enemy.StatusEffects.HasStatus<FanaticStatus>());
            Assert.AreEqual(0, enemy.Stats.CurrentHostility);
            Assert.AreEqual(
                FaithLeaderConfig.Current.JadedOnDisillusion,
                enemy.StatusEffects.GetStacks<JadedStatus>()
            );
        }

        private void MakeFanatic(int enemyIndex, int stacks) =>
            _battle
                .Enemies[enemyIndex]
                .StatusEffects.ApplyStatus(
                    StatusRegistry.Get<FanaticStatus>(),
                    stacks,
                    StatusDurationType.DecreasePerTurn
                );

        private void PlayFromHand(CardData card)
        {
            _battle.PlayerDeck.AddCardToHand(card);
            _battle.PlayerStats.GainActionPoints(_battle.CardPlay.GetEffectiveCardCost(card));
            _battle.RequestPlayCard(card, _battle.PlayerDeck.Hand.ToList().LastIndexOf(card));
        }

        private IEnumerator WaitForPlayerTurn()
        {
            for (int i = 0; i < 120 && _battle.CurrentState != BattleState.PlayerTurn; i++)
                yield return null;
            Assert.AreEqual(BattleState.PlayerTurn, _battle.CurrentState);
        }

        private static void Set(object target, string field, object value) =>
            target
                .GetType()
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);

        private static CardData Card(string name)
        {
#if UNITY_EDITOR
            var card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(
                $"Assets/Data/Cards/FaithLeader/Basic/{name}.asset"
            );
#else
            var card = CardDatabase.Shared.GetAll().FirstOrDefault(c => c.CardName == name);
#endif
            Assert.IsNotNull(card, $"Card asset '{name}' not found");
            return card;
        }
    }
}
