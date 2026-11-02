using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private CardData _attack;
        private CardData _defend;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _attack = TestContent.Card(
                _objects,
                "Sway 5",
                CardType.Pressure,
                1,
                TestContent.Make<ApplyOpinionEffect>(("_amount", 5))
            );
            _defend = TestContent.Card(
                _objects,
                "Support 4",
                CardType.Pressure,
                1,
                TestContent.Make<GainSupportEffect>(("_amount", 4))
            );
            var enemies = new List<EnemyData>();
            for (int i = 0; i < 2; i++)
            {
                var enemy = ScriptableObject.CreateInstance<EnemyData>();
                _objects.Add(enemy);
                TestContent.Set(enemy, "_minHostility", -10);
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
                    playerDeck = Enumerable.Repeat(_attack, 12).ToList(),
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
                PlayFromHand(_attack);
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
            var support = _defend;
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
                PlayFromHand(_attack);
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

        [UnityTest]
        public IEnumerator ConvertedFanaticGainsJadedOnlyOnDisillusion()
        {
            var convert = ConvertCard();
            var enemy = _battle.Enemies[1];
            enemy.StatusEffects.ApplyStatus(
                StatusRegistry.Get<GuiltStatus>(),
                FaithLeaderConfig.Current.ConversionThreshold,
                StatusDurationType.Permanent
            );
            PlayFromHand(convert);
            Assert.IsTrue(enemy.StatusEffects.HasStatus<FanaticStatus>());
            Assert.AreEqual(0, enemy.StatusEffects.GetStacks<JadedStatus>());

            _battle.RequestEndTurn();
            yield return WaitForPlayerTurn();
            Assert.IsFalse(enemy.StatusEffects.HasStatus<FanaticStatus>());
            Assert.AreEqual(
                FaithLeaderConfig.Current.JadedOnDisillusion,
                enemy.StatusEffects.GetStacks<JadedStatus>()
            );
        }

        [UnityTest]
        public IEnumerator OverStackedFanaticFollowsHarder()
        {
            var config = FaithLeaderConfig.Current;
            _battle
                .Enemies[1]
                .StatusEffects.ApplyStatus(
                    StatusRegistry.Get<GuiltStatus>(),
                    config.ConversionThreshold + 2,
                    StatusDurationType.Permanent
                );
            PlayFromHand(ConvertCard());
            CollectionAssert.AreEqual(
                new[] { config.FollowSwayPerFanatic + 2 * config.FollowBonusPerExtraStack },
                FanaticPushes(() => PlayFromHand(_attack))
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator FanaticStrengthAddsToItsPush()
        {
            MakeFanatic(1, 2);
            _battle
                .Enemies[1]
                .StatusEffects.ApplyStatus(
                    StatusRegistry.Get<StrengthStatus>(),
                    2,
                    StatusDurationType.Permanent
                );
            CollectionAssert.AreEqual(
                new[] { FaithLeaderConfig.Current.FollowSwayPerFanatic + 2 },
                FanaticPushes(() => PlayFromHand(_attack))
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator CommandCardsMakeEveryFanaticActNow()
        {
            var config = FaithLeaderConfig.Current;
            MakeFanatic(0, 2);
            MakeFanatic(1, 2);
            var push = TestContent.Card(
                _objects,
                "Push now",
                CardType.Pressure,
                1,
                new FanaticsPushEffect()
            );
            CollectionAssert.AreEqual(
                new[] { config.FollowSwayPerFanatic, config.FollowSwayPerFanatic },
                FanaticPushes(() => PlayFromHand(push))
            );

            var defend = TestContent.Card(
                _objects,
                "Defend now",
                CardType.Pressure,
                1,
                new FanaticsSupportEffect()
            );
            int before = _battle.CurrentSupport;
            PlayFromHand(defend);
            Assert.AreEqual(
                2 * config.FollowSupportPerFanatic,
                _battle.CurrentSupport - before
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator SilenceCommandSilencesAHostileEnemyPerFanatic()
        {
            MakeFanatic(1, 2);
            var hostile = _battle.Enemies[0];
            hostile.Stats.SetHostility(5);
            var silence = TestContent.Card(
                _objects,
                "Silence now",
                CardType.Pressure,
                1,
                ForEachFanatic(
                    TestContent.Make<ApplyStatusBehaviorEffect>(
                        ("_target", TargetType.RandomHostile),
                        ("_behavior", StatusRegistry.Get<SilencedStatus>()),
                        ("_stacks", 1)
                    )
                )
            );
            PlayFromHand(silence);
            Assert.IsTrue(hostile.StatusEffects.HasStatus<SilencedStatus>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExtendingTheBuffKeepsAFanaticFollowing()
        {
            var enemy = _battle.Enemies[1];
            MakeFanatic(1, 1);
            var extend = TestContent.Card(
                _objects,
                "Extend",
                CardType.Pressure,
                1,
                ForEachFanatic(
                    TestContent.Make<ApplyStatusBehaviorEffect>(
                        ("_target", TargetType.TriggeringEnemy),
                        ("_behavior", StatusRegistry.Get<FanaticStatus>()),
                        ("_stacks", 1)
                    )
                )
            );
            PlayFromHand(extend);
            _battle.RequestEndTurn();
            yield return WaitForPlayerTurn();
            Assert.IsTrue(enemy.StatusEffects.HasStatus<FanaticStatus>());
            Assert.AreEqual(0, enemy.StatusEffects.GetStacks<JadedStatus>());
        }

        private CardData ConvertCard() =>
            TestContent.Card(
                _objects,
                "Convert",
                CardType.Pressure,
                1,
                TestContent.Make<ConvertPacifiedEffect>(
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

        private static ForEachEnemyWithStatusEffect ForEachFanatic(BattleEffect effect) =>
            TestContent.Make<ForEachEnemyWithStatusEffect>(
                ("_filterStatus", StatusRegistry.Get<FanaticStatus>()),
                ("_effects", new List<BattleEffect> { effect })
            );

        private static List<int> FanaticPushes(System.Action play)
        {
            var pushes = new List<int>();
            void OnDamage(DamageDealtEvent e)
            {
                if (e.AttackerName == "Fanatic")
                    pushes.Add(e.Amount);
            }
            EventBus.Subscribe<DamageDealtEvent>(OnDamage);
            try
            {
                play();
            }
            finally
            {
                EventBus.Unsubscribe<DamageDealtEvent>(OnDamage);
            }
            return pushes;
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

    }
}
