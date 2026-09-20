using UnityEngine;

namespace Crookedile.Data.Campaign
{
    [CreateAssetMenu(menuName = "Crookedile/Campaign/District", fileName = "New District")]
    public class DistrictData : ScriptableObject
    {
        [Tooltip("District name shown in travel previews. Blank uses the asset name.")]
        [SerializeField]
        private string _displayName;

        public string DisplayName
        {
            get { return string.IsNullOrWhiteSpace(_displayName) ? name : _displayName; }
        }
    }
}
