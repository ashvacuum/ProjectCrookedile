using System.Collections.Generic;
using System.Reflection;
using Crookedile.Data;
using Crookedile.Data.Campaign;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>
    /// Campaign Credibility outcomes: the HQ heal (a percent of max, raised by a
    /// StrongerCredibilityHealPassive ally) and raising or cutting max Credibility.
    /// </summary>
    public class CredibilityOutcomeTests
    {
        private RunState _run;
        private readonly List<Object> _objects = new();

        [SetUp]
        public void SetUp()
        {
            _run = RunState.Create(OriginType.FaithLeader, null); // real OriginDatabase from Resources
            Assume.That(_run.MaxCredibility, Is.GreaterThan(20));
            _run.AdjustCredibility(-_run.Credibility + 50);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in _objects)
                Object.DestroyImmediate(item);
            _objects.Clear();
            RunState.Clear();
        }

        [Test]
        public void HealIsAPercentOfMaxWhateverCredibilityIsLeft()
        {
            int heal = Mathf.CeilToInt(_run.MaxCredibility * 0.1f);
            Make<HealPercentOfMaxCredibilityOutcome>(("_percent", 10f)).Apply(_run);
            Assert.AreEqual(50 + heal, _run.Credibility);

            _run.AdjustCredibility(-_run.Credibility + 3);
            Make<HealPercentOfMaxCredibilityOutcome>(("_percent", 10f)).Apply(_run);
            Assert.AreEqual(3 + heal, _run.Credibility, "a low run heals as much as a healthy one");
        }

        [Test]
        public void AllyRaisesTheHealToItsPercentButNeverLowersIt()
        {
            var ally = ScriptableObject.CreateInstance<AllyData>();
            _objects.Add(ally);
            Set(
                ally,
                "_overworldPassives",
                new List<OverworldPassive>
                {
                    Make<StrongerCredibilityHealPassive>(("_percentOfMax", 20f)),
                }
            );
            _run.Allies.Add(ally);

            int expected = Mathf.CeilToInt(_run.MaxCredibility * 0.2f);
            Make<HealPercentOfMaxCredibilityOutcome>(("_percent", 10f)).Apply(_run);
            Assert.AreEqual(50 + expected, _run.Credibility);

            _run.AdjustCredibility(-_run.Credibility + 10);
            int own = Mathf.CeilToInt(_run.MaxCredibility * 0.3f);
            Make<HealPercentOfMaxCredibilityOutcome>(("_percent", 30f)).Apply(_run);
            Assert.AreEqual(10 + own, _run.Credibility, "a bigger heal keeps its own percent");
        }

        [Test]
        public void RaisingMaxHealsAndCuttingItClampsButNeverBelowOne()
        {
            int max = _run.MaxCredibility;
            Make<AdjustMaxCredibilityOutcome>(("_amount", 10)).Apply(_run);
            Assert.AreEqual(max + 10, _run.MaxCredibility);
            Assert.AreEqual(60, _run.Credibility, "a raise heals by the same amount");

            Make<AdjustMaxCredibilityOutcome>(("_amount", -(max + 10 - 30))).Apply(_run);
            Assert.AreEqual(30, _run.MaxCredibility);
            Assert.AreEqual(30, _run.Credibility, "a cut clamps current to the new max");

            Make<AdjustMaxCredibilityOutcome>(("_amount", -1000)).Apply(_run);
            Assert.AreEqual(1, _run.MaxCredibility);
            Assert.AreEqual(1, _run.Credibility, "a cut alone never ends the run");
        }

        private static T Make<T>(params (string field, object value)[] fields)
            where T : new()
        {
            var item = new T();
            foreach (var (field, value) in fields)
                Set(item, field, value);
            return item;
        }

        private static void Set(object target, string field, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
                if (info == null)
                    continue;
                info.SetValue(target, value);
                return;
            }
            Assert.Fail($"{target.GetType().Name} has no field {field}");
        }
    }
}
