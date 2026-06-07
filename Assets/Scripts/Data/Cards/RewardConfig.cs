using Crookedile.Data;
using UnityEngine;

namespace Crookedile.Data.Cards
{
    /// <summary>
    /// Central, tunable configuration for post-battle card rewards: the rarity weights and default
    /// offer count currently hardcoded in <c>CardDatabase.GenerateRewardOffer</c> (Basic 70 / Enhanced
    /// 25 / Rare 5). Additive for now — wire <c>CardDatabase</c> to read weights from here later
    /// (replace the static RewardWeights lookup with <see cref="WeightFor"/>). The Content Audit
    /// validates the weights are usable.
    ///
    /// Create via: Assets → Create → Crookedile → Reward Config
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Reward Config", fileName = "RewardConfig")]
    public class RewardConfig : ScriptableObject
    {
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

        public float BasicWeight => _basicWeight;
        public float EnhancedWeight => _enhancedWeight;
        public float RareWeight => _rareWeight;
        public int DefaultOfferCount => _defaultOfferCount;

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
    }
}
