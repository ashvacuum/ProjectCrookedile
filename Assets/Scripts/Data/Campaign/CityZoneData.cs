using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// One building a zone can spawn, with the footprint it occupies and its relative chance.
    /// </summary>
    [Serializable]
    public class CityBuilding
    {
        [Tooltip(
            "Prefab dropped on the map. Its pivot must sit at the base diamond's centre — "
                + "see art-bible.md §9. A wrong pivot sorts wrong, not just sits wrong."
        )]
        [SerializeField]
        private GameObject _prefab;

        [Tooltip(
            "Footprint in cells — width along the grid's X, length along its Y. "
                + "(1,1) is a single cell; (3,1) is a row of stalls; (2,3) a covered court."
        )]
        [MinValue(1)]
        [SerializeField]
        private Vector2Int _footprint = Vector2Int.one;

        [Tooltip("Relative chance against the other buildings in this zone. 0 disables it.")]
        [Min(0f)]
        [SerializeField]
        private float _weight = 1f;

        public GameObject Prefab => _prefab;
        public Vector2Int Footprint => _footprint;
        public float Weight => _weight;

        /// <summary>Cells covered. Packing tries the biggest of these first.</summary>
        public int Area => Mathf.Max(1, _footprint.x) * Mathf.Max(1, _footprint.y);

        /// <summary>A usable row — an unset prefab or a zero weight can never be placed.</summary>
        public bool IsValid => _prefab != null && _weight > 0f && _footprint is { x: > 0, y: > 0 };
    }

    /// <summary>
    /// A band of the generated city — what gets built there and how often.
    /// <see cref="CityGenerator"/> holds these ordered centre-outward, so zone 0 is downtown
    /// and the last one is the edge of town.
    ///
    /// Scenery only. A zone never knows about encounters: where an encounter happens is decided
    /// by the pool and the day window (`campaign-encounters.md`), never by terrain. Keeping the
    /// two apart is what stops the generator from fighting the Encounter Designer's schedule —
    /// see `metagame-campaign.md` §1.5.
    ///
    /// Create via: Assets → Create → Crookedile → Campaign → City Zone
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Campaign/City Zone", fileName = "New City Zone")]
    public class CityZoneData : ScriptableObject
    {
        [Tooltip("Designer-facing name. Shown in the generator's zone list.")]
        [SerializeField]
        private string _displayName = "New Zone";

        [Tooltip("Buildings this zone draws from, picked by weight.")]
        [TableList(AlwaysExpanded = true)]
        [SerializeField]
        private List<CityBuilding> _buildings = new();

        public string DisplayName => _displayName;
        public IReadOnlyList<CityBuilding> Buildings => _buildings;

        /// <summary>
        /// The distinct footprints this zone can place, biggest first. Packing walks these in
        /// order so the large pieces claim their room before fillers eat it.
        /// </summary>
        public List<Vector2Int> FootprintsLargestFirst()
        {
            var sizes = new List<Vector2Int>();
            foreach (var building in _buildings)
                if (building.IsValid && !sizes.Contains(building.Footprint))
                    sizes.Add(building.Footprint);

            sizes.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y));
            return sizes;
        }
    }
}
