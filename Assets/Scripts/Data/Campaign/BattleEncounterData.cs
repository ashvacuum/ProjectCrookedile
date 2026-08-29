using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
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
        [Tooltip(
            "The fight this location leads to. Normally a session of exactly one round — see "
                + "BattleSession's own notes on why sequences belong to event choices instead."
        )]
        [InlineEditor(Expanded = true)]
        [InlineButton(nameof(CreateSession), "New")]
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

#if UNITY_EDITOR
        /// <summary>
        /// Creates this encounter's session as a child of the encounter asset itself, rather
        /// than a loose file you have to name, place and then find again. One fight belongs to
        /// one location, so the session travels with it — copy or delete the encounter and the
        /// session follows.
        /// </summary>
        private void CreateSession()
        {
            if (_session != null)
            {
                UnityEditor.EditorGUIUtility.PingObject(_session);
                return;
            }

            _session = CreateInstance<BattleSession>();
            _session.name = $"{name} Session";
            UnityEditor.AssetDatabase.AddObjectToAsset(_session, this);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
