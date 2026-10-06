using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Nepo Baby's class rules and class modifiers (docs/nepo-baby-class.md sections 8-9).
    /// Rules a card cannot express on its own live here; each card's own numbers (Sway, cost,
    /// caps) live on its card asset. Open design questions are flags whose default is the
    /// spec's default.
    ///
    /// Loaded from Resources/NepoBabyConfig. When the asset is missing, <see cref="Current"/>
    /// returns an in-memory instance with these defaults, so battles never depend on it.
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Class Config/Nepo Baby", fileName = "NepoBabyConfig")]
    public class NepoBabyConfig : ScriptableObject
    {
        [Title("Class modifiers")]
        [InfoBox(
            "Glass-cannon asymmetry against the Faith Leader baseline: applied to every Sway and "
                + "Support Nepo Baby's cards produce."
        )]
        [HideLabel]
        [SerializeField]
        private ClassModifiers _modifiers = new ClassModifiers
        {
            SwayMultiplier = 1.15f,
            ComposureMultiplier = 0.8f,
        };

        [Title("Burn")]
        [Tooltip(
            "Can a Policy be burned (exhausted from hand as a cost)? Decided: no, anywhere, with "
                + "no per-card exceptions."
        )]
        [SerializeField]
        private bool _policiesBurnable = false;

        [Tooltip(
            "Open question 8.1. Can Blow the Allowance burn a card with no valid play (unplayable "
                + "Heckles, every Scandal)? Off: it needs a card it can actually play."
        )]
        [SerializeField]
        private bool _seedCanTargetJunk = false;

        [Title("Replays")]
        [Tooltip(
            "Open question 8.4. Does every replay raise Hostility on all enemies by 1? Off: only "
                + "cards that say so (Encore, Dynasty, Trust Fund) charge Hostility for a replay."
        )]
        [SerializeField]
        private bool _replaysRaiseHostilityByDefault = false;

        [Title("Energy refunds")]
        [Tooltip(
            "Old Boys' Club: is a refund capped at the energy actually paid for the card? On: a "
                + "0-cost or free card refunds nothing."
        )]
        [SerializeField]
        private bool _refundCappedAtEnergyPaid = true;

        [Title("Return lane")]
        [Tooltip(
            "The Return lane's brake: a card returned to hand costs this much more for the rest "
                + "of the turn."
        )]
        [MinValue(0)]
        [SerializeField]
        private int _returnInTurnCostIncrease = 1;

        public ClassModifiers Modifiers => _modifiers;
        public bool PoliciesBurnable => _policiesBurnable;
        public bool SeedCanTargetJunk => _seedCanTargetJunk;
        public bool ReplaysRaiseHostilityByDefault => _replaysRaiseHostilityByDefault;
        public bool RefundCappedAtEnergyPaid => _refundCappedAtEnergyPaid;
        public int ReturnInTurnCostIncrease => _returnInTurnCostIncrease;

        private const string ResourcePath = "NepoBabyConfig";
        private static NepoBabyConfig _cached;
        private static NepoBabyConfig _defaults;

        /// <summary>
        /// The project's config asset, or a defaults-only instance when none exists.
        /// </summary>
        public static NepoBabyConfig Current
        {
            get
            {
                // Re-resolves on null rather than caching the miss — a domain reload clears this.
                if (_cached == null)
                    _cached = Resources.Load<NepoBabyConfig>(ResourcePath);
                if (_cached != null)
                    return _cached;
                if (_defaults == null)
                    _defaults = CreateInstance<NepoBabyConfig>();
                return _defaults;
            }
        }
    }
}
