using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// UI panel representing the player character in battle.
    /// Displays the player's AP and active buff/debuff icons.
    /// Support/Denial are session-level and shown on OpinionMeterUI, not here.
    ///
    /// Provides <see cref="SlotRect"/> as a stable <see cref="RectTransform"/> anchor
    /// so VFXManager and FloatingTextManager can position effects at the player's on-screen location.
    ///
    /// Setup:
    ///   1. Add to a PlayerSlotPrefab and assign references in the Inspector.
    ///   2. Place the prefab in the battle scene UI and wire it to <see cref="BattleUI"/>.
    ///   3. <see cref="BattleUI"/> calls <see cref="Initialize"/> on BattleStartedEvent.
    /// </summary>
    public class PlayerSlotUI : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("Character portrait image for the active player origin.")]
        [SerializeField]
        private Image _portrait;

        [SerializeField]
        private TMP_Text _apText;

        [SerializeField]
        private StatusEffectPanelUI _statusEffectPanel;

        #region Runtime State

        private BattleManager _battleManager;

        /// <summary>
        /// The root RectTransform of this slot.
        /// Used by VFXManager and FloatingTextManager as the spawn anchor for effects targeted at the player.
        /// </summary>
        public RectTransform SlotRect => (RectTransform)transform;

        #endregion

        #region Initialization

        /// <summary>
        /// Called by <see cref="BattleUI"/> on BattleStartedEvent.
        /// Stores the manager reference, sets the portrait sprite, and performs the initial refresh.
        /// </summary>
        public void Initialize(BattleManager manager, Sprite portrait)
        {
            _battleManager = manager;

            if (_portrait != null && portrait != null)
                _portrait.sprite = portrait;

            Refresh();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reads the latest stats from BattleManager and updates all display elements.
        /// Called by <see cref="BattleUI"/> each time player stats change.
        /// </summary>
        public void Refresh()
        {
            if (_battleManager == null)
                return;
            var stats = _battleManager.PlayerStats;
            if (stats == null)
                return;

            if (_apText != null)
                _apText.SetText($"{stats.CurrentActionPoints}/{stats.MaxActionPoints}");

            _statusEffectPanel?.Refresh(_battleManager.PlayerStatusEffects);
        }

        #endregion
    }
}
