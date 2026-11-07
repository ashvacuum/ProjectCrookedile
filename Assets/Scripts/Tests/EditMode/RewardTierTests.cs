using Crookedile.Data;
using Crookedile.Data.Cards;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>
    /// Which reward tier a win earns, and that a maxed-meter win guarantees a Rare even from a pool
    /// that never rolls one.
    /// </summary>
    public class RewardTierTests
    {
        [Test]
        public void GuaranteeRareAddsOneWhenTheDrawHasNone()
        {
            var cards = CardDatabase.Shared; // real database from Resources
            Assume.That(cards?.DefeatRewards, Is.Not.Null);
            Assume.That(cards.DefeatRewards.RareWeight, Is.EqualTo(0f));

            var offer = cards.GenerateRewardOffer(
                OriginType.NepoBaby,
                rng: new System.Random(7),
                config: cards.DefeatRewards,
                guaranteeRare: true
            );

            Assert.IsTrue(offer.Exists(c => c.Rarity == CardRarity.Rare));
        }

        [Test]
        public void EncounterOverrideIsTheBaseTierButAStrongWinStillBeatsIt()
        {
            var cards = CardDatabase.Shared;
            Assume.That(cards, Is.Not.Null);
            var encounterTier = UnityEngine.ScriptableObject.CreateInstance<RewardConfig>();
            try
            {
                Assert.AreSame(cards.Rewards, cards.RewardsForWin(10, 100));
                Assert.AreSame(encounterTier, cards.RewardsForWin(10, 100, encounterOverride: encounterTier));

                var strong = cards.RewardsForWin(100, 100);
                Assume.That(strong, Is.Not.SameAs(cards.Rewards), "no strong-win tier assigned");
                Assert.AreSame(strong, cards.RewardsForWin(100, 100, encounterOverride: encounterTier));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(encounterTier);
            }
        }
    }
}
