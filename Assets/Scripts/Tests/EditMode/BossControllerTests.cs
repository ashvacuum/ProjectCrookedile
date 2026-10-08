using System.Collections.Generic;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Boss;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    public class BossControllerTests
    {
        private readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets)
                Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        [Test]
        public void RevealedPlanCannotChangeAndExecutesOnceInOrder()
        {
            var boss = CreateBoss();
            boss.BeginPlanning(1);
            Assert.IsTrue(boss.TryChooseBundle("Special"));
            Assert.IsFalse(boss.TryChooseBundle("Fallback"));
            Assert.IsFalse(boss.TryTakeNextIntent(out _));
            Assert.IsTrue(boss.CommitPlan());
            Assert.IsFalse(boss.TryChooseBundle("Fallback"));
            foreach (var expected in boss.Intents)
            {
                Assert.IsTrue(boss.TryTakeNextIntent(out var actual));
                Assert.AreSame(expected, actual);
            }
            Assert.IsFalse(boss.TryTakeNextIntent(out _));
        }

        [Test]
        public void CooldownSkipsOneTurnAndFallbackRemainsAvailable()
        {
            var boss = CreateBoss();
            boss.BeginPlanning(1);
            Assert.IsTrue(boss.TryChooseBundle("Special"));
            boss.CommitPlan();
            boss.BeginPlanning(2);
            Assert.IsFalse(boss.TryChooseBundle("Special"));
            Assert.IsTrue(boss.CommitPlan());
            Assert.AreEqual("Fallback", boss.BundleName);
            boss.BeginPlanning(3);
            Assert.IsTrue(boss.TryChooseBundle("Special"));
        }

        [Test]
        public void BossDoesNotBreakEchoChamberOrEnterWideAudienceTargets()
        {
            var boss = CreateBoss();
            var audience = Audience(-3);
            Assert.IsTrue(new CrowdReactions(audience, 5, 1, 1, 1, 2).IsEchoChamber());
            var context = Context(boss, audience);
            var targets = context.GetTargets(TargetType.AllOpponents);
            Assert.AreEqual(1, targets.Count);
            Assert.AreSame(audience[0].Stats, targets[0].stats);
            Assert.AreNotSame(boss.Stats, targets[0].stats);
        }

        [Test]
        public void RevealedAudienceTargetStaysCommittedAfterStanceChange()
        {
            var boss = CreateBoss();
            var effect = new RaiseTargetHostilityEffect();
            Set(effect, "_target", TargetType.RandomReceptive);
            Set(boss.Data.Bundles[0].Moves[0], "_effects", new List<BattleEffect> { effect });
            var audience = Audience(-3);
            boss.BeginPlanning(1);
            boss.CommitPlan();
            boss.LockAudienceTargets(audience);
            audience[0].Stats.SetHostility(5);
            var targets = Context(boss, audience, boss.GetAudienceTargets(0))
                .GetTargets(TargetType.RandomReceptive);
            Assert.AreEqual(1, targets.Count);
            Assert.AreSame(audience[0].Stats, targets[0].stats);
        }

        [Test]
        public void NoEligibleAudienceAtRevealDoesNotRetargetLater()
        {
            var boss = CreateBoss();
            var effect = new RaiseTargetHostilityEffect();
            Set(effect, "_target", TargetType.RandomReceptive);
            Set(boss.Data.Bundles[0].Moves[0], "_effects", new List<BattleEffect> { effect });
            var audience = Audience(5);
            boss.BeginPlanning(1);
            boss.CommitPlan();
            boss.LockAudienceTargets(audience);
            audience[0].Stats.SetHostility(-3);
            Assert.IsEmpty(
                Context(boss, audience, boss.GetAudienceTargets(0))
                    .GetTargets(TargetType.RandomReceptive)
            );
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void HostilityTargetSelectionSkipsImmuneAudience(bool calming, bool useShift)
        {
            var boss = CreateBoss();
            var category = calming ? TargetType.RandomHostile : TargetType.RandomReceptive;
            BattleEffect effect =
                useShift ? new ShiftHostilityEffect()
                : calming ? new ReduceHostilityEffect()
                : new RaiseTargetHostilityEffect();
            Set(effect, "_target", category);
            if (useShift)
                Set(effect, "_amount", calming ? -2 : 2);
            Set(boss.Data.Bundles[0].Moves[0], "_effects", new List<BattleEffect> { effect });
            var audience = Audience(calming ? 5 : -3);
            audience.Add(Audience(calming ? 5 : -3)[0]);
            if (calming)
                audience[0]
                    .StatusEffects.ApplyStatus(
                        new HardenedStatus(),
                        1,
                        StatusDurationType.Permanent
                    );
            else
                audience[0]
                    .StatusEffects.ApplyStatus(
                        new FanaticStatus(),
                        1,
                        StatusDurationType.Permanent
                    );
            boss.BeginPlanning(1);
            boss.CommitPlan();
            for (int i = 0; i < 20; i++)
            {
                boss.LockAudienceTargets(audience);
                Assert.AreEqual(1, boss.GetAudienceTargets(0)[category]);
            }
        }

        [Test]
        public void AllFanaticAudienceLeavesRileTargetAbsent()
        {
            var boss = CreateBoss();
            var effect = new RaiseTargetHostilityEffect();
            Set(effect, "_target", TargetType.RandomReceptive);
            Set(boss.Data.Bundles[0].Moves[0], "_effects", new List<BattleEffect> { effect });
            var audience = Audience(-3);
            audience[0]
                .StatusEffects.ApplyStatus(new FanaticStatus(), 1, StatusDurationType.Permanent);
            boss.BeginPlanning(1);
            boss.CommitPlan();
            boss.LockAudienceTargets(audience);
            Assert.AreEqual(-1, boss.GetAudienceTargets(0)[TargetType.RandomReceptive]);
        }

        [Test]
        public void FanaticAppliedAfterRevealBlocksRileWithoutRetargeting()
        {
            var boss = CreateBoss();
            var effect = new RaiseTargetHostilityEffect();
            Set(effect, "_target", TargetType.RandomReceptive);
            Set(boss.Data.Bundles[0].Moves[0], "_effects", new List<BattleEffect> { effect });
            var audience = Audience(-3);
            boss.BeginPlanning(1);
            boss.CommitPlan();
            boss.LockAudienceTargets(audience);
            audience.Add(Audience(-3)[0]);
            audience[0]
                .StatusEffects.ApplyStatus(new FanaticStatus(), 1, StatusDurationType.Permanent);
            effect.Execute(Context(boss, audience, boss.GetAudienceTargets(0)));
            Assert.AreEqual(0, boss.GetAudienceTargets(0)[TargetType.RandomReceptive]);
            Assert.AreEqual(-3, audience[0].Stats.CurrentHostility);
            Assert.AreEqual(-3, audience[1].Stats.CurrentHostility);
        }

        private BossController CreateBoss()
        {
            var data = Asset<BossData>();
            Set(
                data,
                "_bundles",
                new List<BossMoveBundle>
                {
                    Bundle("Fallback", 0, Asset<EnemyMoveData>(), Asset<EnemyMoveData>()),
                    Bundle(
                        "Special",
                        1,
                        Asset<EnemyMoveData>(),
                        Asset<EnemyMoveData>(),
                        Asset<EnemyMoveData>()
                    ),
                }
            );
            return new BossController(data);
        }

        private static BossMoveBundle Bundle(
            string name,
            int cooldown,
            params EnemyMoveData[] moves
        )
        {
            var bundle = new BossMoveBundle();
            Set(bundle, "_name", name);
            Set(bundle, "_cooldownTurns", cooldown);
            Set(bundle, "_moves", new List<EnemyMoveData>(moves));
            return bundle;
        }

        private List<EnemyController> Audience(int hostility)
        {
            var data = Asset<EnemyData>();
            Set(data, "_startingHostility", hostility);
            return new List<EnemyController> { new EnemyController(data) };
        }

        private static EffectExecutionContext Context(
            BossController boss,
            List<EnemyController> audience,
            IReadOnlyDictionary<TargetType, int> targets = null
        )
        {
            var player = new BattleStats(3, true);
            var statuses = new StatusEffectManager("Player", player);
            return new EffectExecutionContext(
                player,
                audience[0].Stats,
                player,
                true,
                null,
                audience,
                statuses,
                audience[0].StatusEffects,
                statuses,
                fixedAudienceTargets: targets
            );
        }

        private T Asset<T>()
            where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
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
