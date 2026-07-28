using Crookedile.Data.Cards;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// A campaign-map location that leads to a fight. Wraps an existing
    /// <see cref="BattleSession"/> unchanged — the campaign layer never touches battle
    /// design, it only hands this off via <c>RunState.PendingBattle</c>.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → Battle Encounter
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Campaign/Battle Encounter", fileName = "New Battle Encounter")]
    public class BattleEncounterData : EncounterData
    {
        [SerializeField]
        private BattleSession _session;

        [Tooltip("Boss victory grants a pick-1-of-3 relic (M3). Unused until then.")]
        [SerializeField]
        private bool _isBoss;

        [Tooltip("Overrides the default reward weights for this encounter's card offer (M3). Optional.")]
        [SerializeField]
        private RewardConfig _rewardOverride;

        public BattleSession Session => _session;
        public bool IsBoss => _isBoss;
        public RewardConfig RewardOverride => _rewardOverride;
    }
}
