using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.UI
{
    /// <summary>
    /// Composition root for the battle scene: injects the BattleManager into every
    /// <c>IBindable&lt;BattleManager&gt;</c> view under it. The single sanctioned bridge
    /// between the battle game layer and battle UI.
    /// </summary>
    public class BattleUIRoot : UIRoot<BattleManager>
    {
        [Tooltip("The scene's BattleManager — the only game ref the UI layer holds.")]
        [SerializeField]
        private BattleManager _battleManager;

        protected override BattleManager Context => _battleManager;
    }
}
