using Crookedile.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Tunable configuration for card rewards: rarity weights, offer count and the rare pity
    /// timer. <c>CardDatabase</c> reads the one assigned to it. The Database window's Checks tab
    /// validates the weights are usable.
    ///
    /// Create via: Assets → Create → Crookedile → Cards → Reward Config
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Cards/Reward Config", fileName = "RewardConfig")]
    public class RewardConfig : ScriptableObject
    {
        // Weights are relative, so the authored numbers say nothing on their own — 70/25/5 and
        // 700/250/50 are the same config. The share line is what you actually tune against.
        // On the field rather than the class: Odin resolves @Method() against the instance here.
        [InfoBox("@ShareSummary()", InfoMessageType.None, VisibleIf = nameof(IsValid))]
        [InfoBox(
            "Every weight is 0 — no card can be offered at all.",
            InfoMessageType.Error,
            VisibleIf = "@!IsValid"
        )]
        [Header("Rarity weights (relative)")]
        [Min(0f)]
        [SerializeField]
        private float _basicWeight = 70f;

        [Min(0f)]
        [SerializeField]
        private float _enhancedWeight = 25f;

        [Min(0f)]
        [SerializeField]
        private float _rareWeight = 5f;

        [Header("Offer")]
        [Min(1)]
        [Tooltip("How many cards to offer per reward screen.")]
        [SerializeField]
        private int _defaultOfferCount = 3;

        [Header("Rare pity")]
        [Min(0f)]
        [Tooltip(
            "Rare weight added for every non-rare card offered since the last rare appeared. Resets when a "
                + "reward offer shows a rare. 0 turns the pity timer off."
        )]
        [SerializeField]
        private float _rarePityStep = 1f;

        public float BasicWeight => _basicWeight;
        public float EnhancedWeight => _enhancedWeight;
        public float RareWeight => _rareWeight;
        public int DefaultOfferCount => _defaultOfferCount;
        public float RarePityStep => _rarePityStep;

        /// <summary>Rare weight once <paramref name="cardsSinceRare"/> non-rare offers have gone by.</summary>
        public float RareWeightWithPity(int cardsSinceRare) =>
            _rareWeight + _rarePityStep * System.Math.Max(0, cardsSinceRare);

        /// <summary>Relative draw weight for a rarity bucket.</summary>
        public float WeightFor(CardRarity rarity) =>
            rarity switch
            {
                CardRarity.Basic => _basicWeight,
                CardRarity.Enhanced => _enhancedWeight,
                CardRarity.Rare => _rareWeight,
                _ => 0f,
            };

        /// <summary>True if the weights can produce a draw and the offer count is sane.</summary>
        public bool IsValid =>
            (_basicWeight + _enhancedWeight + _rareWeight) > 0f && _defaultOfferCount >= 1;

        /// <summary>The weights as the percentages they actually resolve to.</summary>
        private string ShareSummary()
        {
            float total = _basicWeight + _enhancedWeight + _rareWeight;
            if (total <= 0f)
                return "";
            return $"Basic {_basicWeight / total:P0}   "
                + $"Enhanced {_enhancedWeight / total:P0}   "
                + $"Rare {_rareWeight / total:P0}";
        }
    }
}
