using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using NUnit.Framework;
using UnityEngine;

namespace Crookedile.Tests
{
    /// <summary>A free-this-turn price covers one play; a card returned to hand after it pays its stacked increases.</summary>
    public class CardCostOverridesTests
    {
        [Test]
        public void FreeThisTurnIsUsedUpByOnePlay()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            var overrides = new CardCostOverrides();

            overrides.MakeFreeThisTurn(card); // Call in a Favor
            Assert.IsTrue(overrides.IsFreeThisTurn(card));

            overrides.ConsumeFreeThisTurn(card); // played
            overrides.IncreaseThisTurn(card, 1); // returned to hand: the Return brake
            Assert.IsFalse(overrides.IsFreeThisTurn(card));
            Assert.AreEqual(1, overrides.GetIncreaseThisTurn(card));

            Object.DestroyImmediate(card);
        }
    }
}
