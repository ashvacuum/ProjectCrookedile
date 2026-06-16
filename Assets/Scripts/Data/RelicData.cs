using System.Collections.Generic;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.Data
{
    /// <summary>
    /// A relic — an accumulated, persistent passive that warps strategy across a run (StS-style).
    /// Reuses the polymorphic <see cref="BattlePassive"/> system (trigger + conditions + effects), the
    /// same way <c>OriginPassive</c> does, so relic behaviour is fully data-authored.
    ///
    /// DATA SCAFFOLD ONLY: acquisition, persistence in RunState, and registering relic passives with
    /// PassiveResolver are NOT wired yet — that is the future relic layer.
    ///
    /// Create via: Assets → Create → Crookedile → Relic Data
    /// </summary>
    [CreateAssetMenu(menuName = "Crookedile/Relic Data", fileName = "New Relic")]
    public class RelicData : ScriptableObject
    {
        [Tooltip("Stable unique id (auto-generated GUID).")]
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _relicName;

        [TextArea(2, 4)]
        [SerializeField]
        private string _description;

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private CardRarity _rarity = CardRarity.Basic;

        [Tooltip("The persistent passive(s) this relic grants. Same hierarchy as card/origin passives.")]
        [SerializeReference]
        [SerializeField]
        private List<BattlePassive> _passives = new List<BattlePassive>();

        public string Id => _id;
        public string RelicName => _relicName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public CardRarity Rarity => _rarity;
        public IReadOnlyList<BattlePassive> Passives => _passives;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = System.Guid.NewGuid().ToString();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
