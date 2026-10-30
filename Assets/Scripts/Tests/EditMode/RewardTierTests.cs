using Crookedile.Data;
using Crookedile.Data.Cards;
using NUnit.Framework;

namespace Crookedile.Tests
{
    /// <summary>A maxed-meter win guarantees a Rare even from a pool that never rolls one.</summary>
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
    }
}
