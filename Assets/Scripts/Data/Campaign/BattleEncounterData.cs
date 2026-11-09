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

        [Tooltip(
            "Campaign boss classification and rewards. Boss encounters must assign a rival in their session round."
        )]
        [SerializeField]
        private bool _isBoss;

        [Tooltip(
            "A hard optional fight. Winning one offers a pick of allies from the elite pool for the player's class."
        )]
        [ValidateInput("@!(_isElite && _isBoss)", "An encounter is a boss or an elite, not both.")]
        [SerializeField]
        private bool _isElite;

        [Tooltip(
            "Base reward tier for winning this encounter, replacing the database default. A strong win or a maxed boss meter still earns its own tier. Optional."
        )]
        [SerializeField]
        private RewardConfig _rewardOverride;

        public BattleSession Session => _session;
        public bool IsBoss => _isBoss;
        public bool IsElite => _isElite;
        public RewardConfig RewardOverride => _rewardOverride;

#if UNITY_EDITOR
        /// <summary>
        /// Creates the session as a child of this asset, so it travels with the encounter when
        /// copied or deleted.
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
