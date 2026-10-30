using Crookedile.Data;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>A lost battle's Credibility cost scales from narrow (just short of the win line) to crushing (0 Opinion).</summary>
    public class DefeatCostTests
    {
        [Test]
        public void CostScalesWithHowBadlyYouLost()
        {
            var db = ScriptableObject.CreateInstance<OriginDatabase>(); // defaults: narrow 5, crushing 20
            Assert.AreEqual(5, db.DefeatCredibilityCost(finalOpinion: 59, winLine: 60));
            Assert.AreEqual(20, db.DefeatCredibilityCost(finalOpinion: 0, winLine: 60));
            Assert.AreEqual(14, db.DefeatCredibilityCost(finalOpinion: 24, winLine: 60));
            Object.DestroyImmediate(db);
        }

        [Test]
        public void HealsStopAtMaxCredibility()
        {
            var run = RunState.Create(OriginType.NepoBaby, null); // real OriginDatabase from Resources
            Assume.That(run.MaxCredibility, Is.GreaterThan(0));
            run.AdjustCredibility(1000);
            Assert.AreEqual(run.MaxCredibility, run.Credibility);
            RunState.Clear();
        }
    }
}
