using System.Collections.Generic;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// Registry of all <see cref="AllyData"/> in the game — the ally pool the run draws from.
    /// Look up by id. DATA SCAFFOLD: ally acquisition/persistence/registration is the future layer.
    ///
    /// Create via: Assets → Create → Crookedile → Ally Database
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Ally Database", fileName = "AllyDatabase")]
    public class AllyDatabase : ScriptableObject
    {
        [SerializeField]
        private List<AllyData> _allies = new List<AllyData>();

        public IReadOnlyList<AllyData> Allies => _allies;

        private Dictionary<string, AllyData> _byId;

        private void OnEnable() => BuildIndex();

        private void BuildIndex()
        {
            _byId = new Dictionary<string, AllyData>();
            if (_allies == null)
                return;
            foreach (var ally in _allies)
                if (ally != null && !string.IsNullOrEmpty(ally.Id))
                    _byId[ally.Id] = ally;
        }

        public AllyData GetById(string id)
        {
            if (_byId == null)
                BuildIndex();
            return id != null && _byId.TryGetValue(id, out var r) ? r : null;
        }
    }
}
