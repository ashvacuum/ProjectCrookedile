using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Faith Leader's class rules: pacify conversion and the Fanatic followers it makes
    /// (docs/core-design.md section 7, "Fanatics follow your lead"). Every number is the ruling's
    /// placeholder; each card's own numbers live on its card asset.
    ///
    /// Loaded from Resources/FaithLeaderConfig. When the asset is missing, <see cref="Current"/>
    /// returns an in-memory instance with these defaults, so battles never depend on it.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Crookedile/Class Config/Faith Leader",
        fileName = "FaithLeaderConfig"
    )]
    public class FaithLeaderConfig : ScriptableObject
    {
        [Title("Conversion")]
        [Tooltip(
            "Pacify stacks an enemy needs before a conversion card converts it, before Jaded: "
                + "each Jaded stack on the enemy raises this by 1."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _conversionThreshold = 3;

        [Title("Fanatic followers")]
        [Tooltip(
            "Opinion each Fanatic pushes (through Denial) when you play a card that pushes the meter."
        )]
        [MinValue(0)]
        [SerializeField]
        private int _followSwayPerFanatic = 2;

        [Tooltip("Support each Fanatic adds when you play a card that gains Support.")]
        [MinValue(0)]
        [SerializeField]
        private int _followSupportPerFanatic = 2;

        [Tooltip(
            "Conversions you must make in a turn, playing no attack and no defend card, before "
                + "each Fanatic Silences an enemy at the end of your turn."
        )]
        [MinValue(1)]
        [SerializeField]
        private int _silenceMinConversions = 2;

        [Tooltip("Silenced stacks each Fanatic puts on its enemy (1 = that enemy's next action).")]
        [MinValue(1)]
        [SerializeField]
        private int _silenceStacks = 1;

        [Tooltip("Jaded stacks a Fanatic gains when its buff runs out and it reverts to neutral.")]
        [MinValue(0)]
        [SerializeField]
        private int _jadedOnDisillusion = 1;

        public int ConversionThreshold => _conversionThreshold;
        public int FollowSwayPerFanatic => _followSwayPerFanatic;
        public int FollowSupportPerFanatic => _followSupportPerFanatic;
        public int SilenceMinConversions => _silenceMinConversions;
        public int SilenceStacks => _silenceStacks;
        public int JadedOnDisillusion => _jadedOnDisillusion;

        private const string ResourcePath = "FaithLeaderConfig";
        private static FaithLeaderConfig _cached;
        private static FaithLeaderConfig _defaults;

        /// <summary>
        /// The project's config asset, or a defaults-only instance when none exists.
        /// </summary>
        public static FaithLeaderConfig Current
        {
            get
            {
                // Re-resolves on null rather than caching the miss — a domain reload clears this.
                if (_cached == null)
                    _cached = Resources.Load<FaithLeaderConfig>(ResourcePath);
                if (_cached != null)
                    return _cached;
                if (_defaults == null)
                    _defaults = CreateInstance<FaithLeaderConfig>();
                return _defaults;
            }
        }
    }
}
