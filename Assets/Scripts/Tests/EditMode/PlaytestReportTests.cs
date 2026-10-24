using Crookedile.EditorTools.Playtest;
using NUnit.Framework;

namespace Crookedile.Tests
{
    public class PlaytestReportTests
    {
        [Test]
        public void Wilson_SmallPerfectSampleStaysWide()
        {
            var (lo, hi) = PlaytestReport.Wilson(3, 3);

            Assert.AreEqual(0.44f, lo, 0.01f);
            Assert.AreEqual(1f, hi, 1e-6f);
        }

        [Test]
        public void Wilson_LargeSampleNarrowsAroundTheRate()
        {
            var (lo, hi) = PlaytestReport.Wilson(600, 1000);

            Assert.AreEqual(0.57f, lo, 0.01f);
            Assert.AreEqual(0.63f, hi, 0.01f);
        }

        [Test]
        public void Wilson_NoSamplesIsTotallyUncertain()
        {
            Assert.AreEqual((0f, 1f), PlaytestReport.Wilson(0, 0));
        }
    }
}
