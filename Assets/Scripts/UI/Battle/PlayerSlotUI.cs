using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// UI panel representing the player character in battle.
    /// Displays Resolve bar, Composure, AP, and active buff/debuff icons.
    /// Mirrors the visual structure of <see cref="EnemySlotUI"/> but omits
    /// enemy-specific elements (hostility, intent display, targeting highlights).
    ///
    /// Provides <see cref="SlotRect"/> as a stable <see cref="RectTransform"/> anchor
    /// so <see cref="Managers.VFXManager"/> and <see cref="Managers.FloatingTextManager"/>
    /// can position effects at the player's on-screen location.
    ///
    /// Setup:
    ///   1. Add to a <c>PlayerSlotPrefab</c> and assign references in the Inspector.
    ///   2. Place the prefab in the battle scene UI and wire it to <see cref="BattleUI"/>.
    ///   3. <see cref="BattleUI"/> calls <see cref="Initialize"/> on <c>BattleStartedEvent</c>.
    /// </summary>
    public class PlayerSlotUI : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("Character portrait image for the active player origin.")]
        [SerializeField] private Image _portrait;

        [SerializeField] private TMP_Text _resolveText;

        [Tooltip("Filled Image (fill method = Horizontal) driven by Resolve. Lerps smoothly when damaged.")]
        [SerializeField] private Image _resolveBarFill;

        [SerializeField] private TMP_Text _composureText;

        [Tooltip("GameObject wrapping the composure display — hidden when composure is 0.")]
        [SerializeField] private GameObject _composureObject;

        [SerializeField] private TMP_Text _apText;

        [SerializeField] private StatusEffectPanelUI _statusEffectPanel;

        [Header("HP Bar")]
        [Tooltip("Duration in seconds for the Resolve bar to animate to its new fill.")]
        [SerializeField] private float _barAnimDuration = 0.3f;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private BattleManager _battleManager;
        private float _targetFill = 1f;

        /// <summary>
        /// The root <see cref="RectTransform"/> of this slot.
        /// Used by <see cref="Managers.VFXManager"/> and <see cref="Managers.FloatingTextManager"/>
        /// as the spawn anchor for effects targeted at the player.
        /// </summary>
        public RectTransform SlotRect => (RectTransform)transform;

        // ─── Initialization ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="BattleUI"/> on <c>BattleStartedEvent</c>.
        /// Stores the manager reference, sets the portrait sprite, and snaps the bar to full
        /// so there is no lerp animation on first appearance.
        /// </summary>
        public void Initialize(BattleManager manager, Sprite portrait)
        {
            _battleManager = manager;

            if (_portrait != null && portrait != null)
                _portrait.sprite = portrait;

            Refresh();

            // Snap bar to full on spawn.
            if (_resolveBarFill != null) _resolveBarFill.fillAmount = _targetFill;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the latest stats from BattleManager and updates all display elements.
        /// Called by <see cref="BattleUI.UpdateStatsDisplay"/> each time player stats change.
        /// </summary>
        public void Refresh()
        {
            if (_battleManager == null) return;
            var stats = _battleManager.PlayerStats;
            if (stats == null) return;

            _targetFill = stats.MaxResolve > 0
                ? (float)stats.CurrentResolve / stats.MaxResolve
                : 0f;

            if (_resolveBarFill != null)
            {
                DOTween.Kill(_resolveBarFill);
                DOTween.To(() => _resolveBarFill.fillAmount, x => _resolveBarFill.fillAmount = x,
                           _targetFill, _barAnimDuration)
                       .SetEase(Ease.OutQuad)
                       .SetLink(gameObject);
            }

            if (_resolveText != null)
                _resolveText.SetText($"{stats.CurrentResolve}/{stats.MaxResolve}");

            // Composure — hide panel entirely when zero
            int composure = stats.CurrentComposure;
            if (_composureText   != null) _composureText.SetText(composure > 0 ? composure.ToString() : string.Empty);
            if (_composureObject != null) _composureObject.SetActive(composure > 0);

            // Action Points
            if (_apText != null)
                _apText.SetText($"{stats.CurrentActionPoints}/{stats.MaxActionPoints}");

            // Buff / debuff icons
            _statusEffectPanel?.Refresh(_battleManager.PlayerStatusEffects);
        }

    }
}
