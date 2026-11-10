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
    /// Self-wiring: takes its portrait from <see cref="BattleStartedEvent"/> and repaints from
    /// <see cref="BattlePanel.Battle"/> whenever battle state changes.
    /// </summary>
    public class PlayerSlotUI : BattlePanel
    {
        [Header("Display")]
        [Tooltip("Character portrait image for the active player origin.")]
        [SerializeField]
        private Image _portrait;

        [SerializeField]
        private TMP_Text _apText;

        [SerializeField]
        private StatusEffectPanelUI _statusEffectPanel;

        /// <summary>
        /// The root RectTransform of this slot.
        /// Used by VFXManager and FloatingTextManager as the spawn anchor for effects targeted at the player.
        /// </summary>
        public RectTransform SlotRect => (RectTransform)transform;

        private void OnEnable()
        {
            On<BattleStartedEvent>(OnBattleStarted);
            On<ActionPointsChangedEvent>(evt =>
            {
                if (evt.IsPlayer)
                    QueueRefresh();
            });
            RefreshOnBattleChanges();
        }

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            Sprite portrait = evt.Setup.GetPlayerPortrait();
            if (_portrait != null && portrait != null)
                _portrait.sprite = portrait;
        }

        protected override void Refresh()
        {
            var stats = Battle?.PlayerStats;
            if (stats == null)
                return;

            if (_apText != null)
                _apText.SetText($"{stats.CurrentActionPoints}/{stats.MaxActionPoints}");

            _statusEffectPanel?.Refresh(Battle.PlayerStatusEffects);
        }
    }
}
