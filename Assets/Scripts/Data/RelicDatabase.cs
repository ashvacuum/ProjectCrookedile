using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Registry of all <see cref="RelicData"/> in the game — the relic pool the run draws from.
    /// Look up by id. DATA SCAFFOLD: relic acquisition/persistence/registration is the future layer.
    ///
    /// Create via: Assets → Create → Crookedile → Relic Database
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Relic Database", fileName = "RelicDatabase")]
    public class RelicDatabase : ScriptableObject
    {
        [SerializeField]
        private List<RelicData> _relics = new List<RelicData>();

        public IReadOnlyList<RelicData> Relics => _relics;

        private Dictionary<string, RelicData> _byId;

        private void OnEnable() => BuildIndex();

        private void BuildIndex()
        {
            _byId = new Dictionary<string, RelicData>();
            if (_relics == null)
                return;
            foreach (var relic in _relics)
                if (relic != null && !string.IsNullOrEmpty(relic.Id))
                    _byId[relic.Id] = relic;
        }

        public RelicData GetById(string id)
        {
            if (_byId == null)
                BuildIndex();
            return id != null && _byId.TryGetValue(id, out var r) ? r : null;
        }
    }
}
