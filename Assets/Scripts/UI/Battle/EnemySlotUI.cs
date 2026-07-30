using Crookedile.Data;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// UI panel representing one enemy in a multi-enemy room.
    /// Displays the enemy's name, hostility state, Denial shield, and current intent.
    /// Clicking the panel focuses this enemy as the player's target.
    ///
    /// Spawned at runtime by BattleUI.BuildEnemySlots() for each enemy in the battle.
    /// Designed to be used as a prefab: assign text/button/image references in the Inspector.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class EnemySlotUI
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler
    {
        [Header("Display")]
        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text hostilityText;

        [SerializeField]
        private Image enemySprite;

        [SerializeField]
        private EnemyIntentDisplay _intentDisplay;

        [SerializeField]
        private StatusEffectPanelUI _statusEffectPanel;

        [Header("Hostility Bar")]
        [Tooltip("Right-half fill — grows rightward as hostility goes positive. Tint red/orange.")]
        [SerializeField]
        private Image _hostileFill;

        [Tooltip("Left-half fill — grows leftward as hostility goes negative. Tint green.")]
        [SerializeField]
        private Image _receptiveFill;

        [Tooltip("Seconds for the hostility bar fills to tween to their new amount.")]
        [SerializeField]
        private float _hostilityBarDuration = 0.25f;

        [Header("Stance Badge")]
        [Tooltip("Label reading Hostile / Neutral / Receptive, derived from hostility vs NeutralZone.")]
        [SerializeField]
        private TMP_Text _stanceText;

        [Tooltip("Background behind the stance label — tinted by the stance colors below.")]
        [SerializeField]
        private Image _stanceBackground;

        [SerializeField]
        private Color _hostileStanceColor = new Color(0.75f, 0.18f, 0.18f);

        [SerializeField]
        private Color _neutralStanceColor = new Color(0.35f, 0.35f, 0.38f);

        [SerializeField]
        private Color _receptiveStanceColor = new Color(0.18f, 0.6f, 0.3f);

        [Tooltip(
            "TMPEffects animator on the stance label. Optional — without it the label still "
                + "updates, just without the grow animation."
        )]
        [SerializeField]
        private TMPEffects.Components.TMPAnimator _stanceAnimator;

        [Tooltip(
            "TMPEffects show-animation tag replayed whenever the stance changes. "
                + "Tune the grow with parameters, e.g. <+grow d=0.25 start=0.4>."
        )]
        [SerializeField]
        private string _stanceGrowTag = "<+grow>";

        [Header("Name Hover")]
        [Tooltip(
            "Duration in seconds for the enemy name to fade in or out when the HP bar is hovered."
        )]
        [SerializeField]
        private float _nameFadeDuration = 0.2f;

        [SerializeField]
        private Image selectionHighlight;

        [SerializeField]
        private Image dragDropHighlight;

        [SerializeField]
        private GameObject defeatedOverlay;

        private int _enemyIndex;
        private BattleManager _battleManager;

        /// <summary>The enemy slot currently targeted by the card targeting arrow, or null.</summary>
        public static EnemySlotUI TargetedSlot { get; private set; }

        /// <summary>
        /// The RectTransform of the most recently targeted or hovered enemy slot.
        /// Set on pointer-enter during targeting and on PlayCardOnEnemy.
        /// Read by BattleManager to position VFX on the enemy rather than the card.
        /// </summary>
        public static RectTransform LastTargetedRect { get; private set; }

        /// <summary>
        /// Clears the targeted slot and removes its drag-drop highlight.
        /// Called by <c>CardButton.ExitTargetingMode</c>.
        /// </summary>
        public static void ClearTargetedSlot()
        {
            if (TargetedSlot != null && TargetedSlot.dragDropHighlight != null)
                TargetedSlot.dragDropHighlight.enabled = false;
            TargetedSlot = null;
        }

        #region Initialization
        /// <summary>
        /// Called by BattleUI when spawning this slot. Must be called before the first frame.
        /// </summary>
        public void Initialize(int index, BattleManager manager, EnemyData enemyData)
        {
            _enemyIndex = index;
            _battleManager = manager;

            if (defeatedOverlay != null)
                defeatedOverlay.SetActive(false);
            if (selectionHighlight != null)
                selectionHighlight.enabled = false;
            if (dragDropHighlight != null)
                dragDropHighlight.enabled = false;

            // Name is always visible — no HP bar to hover over
            if (nameText != null)
                nameText.alpha = 1f;

            _intentDisplay?.ShowIntent(null); // hidden until intent is declared

            _statusEffectPanel?.Clear(); // reset any icons left over from a previously pooled slot
            _currentStanceLabel = null; // first Refresh writes the badge without animating it
            Refresh();

            if (enemySprite != null)
                enemySprite.sprite = enemyData.Portrait;
        }

        #endregion

        #region Public API
        /// <summary>
        /// Reads the latest stats from BattleManager and updates all display elements.
        /// </summary>
        public void Refresh()
        {
            if (_battleManager == null || _enemyIndex >= _battleManager.Enemies.Count)
                return;
            var enemy = _battleManager.Enemies[_enemyIndex];
            if (enemy.IsDefeated)
                return;

            if (nameText != null)
                nameText.SetText(enemy.EnemyData.EnemyName);

            // Hostility split bar
            var h = enemy.Stats.CurrentHostility;
            float posT =
                enemy.EnemyData.MaxHostility > 0
                    ? Mathf.Clamp01((float)Mathf.Max(0, h) / enemy.EnemyData.MaxHostility)
                    : 0f;
            float negT =
                enemy.EnemyData.MinHostility < 0
                    ? Mathf.Clamp01((float)Mathf.Max(0, -h) / -enemy.EnemyData.MinHostility)
                    : 0f;

            TweenFill(_hostileFill, posT);
            TweenFill(_receptiveFill, negT);

            // Exact signed hostility — always shown. The number line is the core read of
            // the game (pacify thresholds, the pressure multiplier, the ±conversion edges),
            // so every origin sees the actual value, not just the bar.
            if (hostilityText != null)
            {
                hostilityText.gameObject.SetActive(true);
                hostilityText.text = $"{h:+0;-0;0}";
            }

            UpdateStanceBadge(enemy.Stats);

            // Buff/debuff icons
            var effects = _battleManager?.Enemies[_enemyIndex]?.StatusEffects;
            if (effects != null)
                _statusEffectPanel?.Refresh(effects);
        }

        /// <summary>
        /// Names the enemy's stance in words and colors the badge behind it. The signed number
        /// tells you how far past the line they are; this tells you which side of it they're on.
        /// Refresh() runs on every stat change, so the text/color writes are skipped when the
        /// stance hasn't actually moved — otherwise the grow animation would replay constantly.
        /// </summary>
        private void UpdateStanceBadge(Crookedile.Gameplay.BattleStats stats)
        {
            string label =
                stats.IsHostile ? "Hostile"
                : stats.IsReceptive ? "Receptive"
                : "Neutral";
            if (label == _currentStanceLabel)
                return;

            bool firstWrite = _currentStanceLabel == null;
            _currentStanceLabel = label;

            if (_stanceBackground != null)
                _stanceBackground.color =
                    stats.IsHostile ? _hostileStanceColor
                    : stats.IsReceptive ? _receptiveStanceColor
                    : _neutralStanceColor;

            if (_stanceAnimator != null && _stanceAnimator.isActiveAndEnabled)
            {
                // Go through the animator, not the TMP_Text — it owns the tag parsing. ShowAll
                // drives every character back through the show animation, which is what replays
                // the grow; SetText alone leaves them already "shown".
                _stanceAnimator.SetText(_stanceGrowTag + label);
                if (!firstWrite)
                    _stanceAnimator.ShowAll();
            }
            else if (_stanceText != null)
                _stanceText.SetText(label);
        }

        // Last stance word written to the badge; null until the first Refresh.
        private string _currentStanceLabel;

        /// <summary>
        /// Updates the intent display when the enemy declares their next move.
        /// Passes the enemy's live status effects so Weakened/Strength are reflected in the damage preview.
        /// </summary>
        public void UpdateIntent(EnemyMoveData move)
        {
            var statusEffects =
                (_battleManager != null && _enemyIndex < _battleManager.Enemies.Count)
                    ? _battleManager.Enemies[_enemyIndex].StatusEffects
                    : null;
            var targetStatus = _battleManager?.PlayerStatusEffects;
            _intentDisplay?.ShowIntent(move, statusEffects, targetStatus);
        }

        /// <summary>
        /// Highlights or un-highlights this slot to show it is the current focus target.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.enabled = selected;
        }

        /// <summary>
        /// Shows the defeated overlay and disables the select button.
        /// Called when this enemy is removed from active combat.
        /// </summary>
        public void MarkDefeated()
        {
            // Hide all live display elements
            if (nameText != null)
                nameText.gameObject.SetActive(false);
            if (hostilityText != null)
                hostilityText.gameObject.SetActive(false);
            if (_stanceBackground != null)
                _stanceBackground.gameObject.SetActive(false);
            else if (_stanceText != null)
                _stanceText.gameObject.SetActive(false);
            if (_intentDisplay != null)
                _intentDisplay.gameObject.SetActive(false);
            if (selectionHighlight != null)
                selectionHighlight.enabled = false;
            if (dragDropHighlight != null)
                dragDropHighlight.enabled = false;

            // Show defeated state
            if (defeatedOverlay != null)
                defeatedOverlay.SetActive(true);
        }

        /// <summary>
        /// Briefly scales the hostility bar to signal a change.
        /// Punches the active bar half (or the bar root if both are inactive).
        /// </summary>
        public void PulseHostility()
        {
            var target =
                _hostileFill != null && _hostileFill.fillAmount > 0 ? _hostileFill.transform
                : _receptiveFill != null && _receptiveFill.fillAmount > 0 ? _receptiveFill.transform
                : _hostileFill?.transform.parent ?? _hostileFill?.transform;
            if (target != null)
                PulseTransform(target);
        }

        /// <summary>
        /// Briefly scales the intent display to signal this enemy is about to act.
        /// </summary>
        public void PulseIntent()
        {
            if (_intentDisplay != null)
                PulseTransform(_intentDisplay.transform);
        }

        /// <summary>
        /// Hides the intent panel after the enemy's move resolves.
        /// The panel will reappear when the next EnemyIntentDeclaredEvent fires.
        /// </summary>
        public void ClearIntent() => _intentDisplay?.ShowIntent(null);

        #endregion

        #region Name Hover Fade
        /// <summary>
        /// Fades the enemy name label in. Called by <see cref="HPBarHoverTrigger"/> on pointer-enter.
        /// </summary>
        public void ShowNameLabel()
        {
            if (nameText == null || !nameText.gameObject.activeInHierarchy)
                return;
            DOTween.Kill(nameText);
            DOTween
                .To(() => nameText.alpha, x => nameText.alpha = x, 1f, _nameFadeDuration)
                .SetLink(gameObject);
        }

        /// <summary>
        /// Fades the enemy name label out. Called by <see cref="HPBarHoverTrigger"/> on pointer-exit.
        /// </summary>
        public void HideNameLabel()
        {
            if (nameText == null)
                return;
            DOTween.Kill(nameText);
            DOTween
                .To(() => nameText.alpha, x => nameText.alpha = x, 0f, _nameFadeDuration)
                .SetLink(gameObject);
        }

        #endregion

        #region Targeting Handlers
        /// <summary>
        /// Sets this enemy as the focused target and plays the given card.
        /// Called by <c>CardButton.OnEndDrag</c> when the targeting arrow is released over this slot.
        /// </summary>
        public void PlayCardOnEnemy(CardButton card)
        {
            if (_battleManager == null || card == null)
                return;
            if (
                _enemyIndex < _battleManager.Enemies.Count
                && _battleManager.Enemies[_enemyIndex].IsDefeated
            )
                return;

            LastTargetedRect = GetComponent<RectTransform>();
            GameLogger.LogInfo(
                "Card",
                $"'{card.CardData?.CardName}' targeted at enemy slot [{_enemyIndex}]  LastTargetedRect set",
                this
            );
            _battleManager.SetFocusedEnemy(_enemyIndex);
            card.ConfirmPlay();
        }

        /// <summary>
        /// Click-to-target play: if a card is armed (clicked in hand), play it on this enemy.
        /// With no armed card a click is ignored — focus advances via gameplay, not manual clicks.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            CardButton armed = CardButton.ArmedCard;
            if (armed == null)
                return;

            // Hide the arrow before the play triggers a hand rebuild, then play on this slot.
            CardButton.DisarmCurrent();
            PlayCardOnEnemy(armed);
        }

        /// <summary>Shows the targeting highlight when the arrow enters this slot during targeting mode.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CardButton.IsTargeting)
                return;
            if (dragDropHighlight != null)
                dragDropHighlight.enabled = true;
            TargetedSlot = this;
            LastTargetedRect = GetComponent<RectTransform>();
            GameLogger.LogVerbose("Card", $"Arrow hovering enemy slot [{_enemyIndex}]", this);
        }

        /// <summary>Hides the targeting highlight and clears this slot as the target when the cursor leaves.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (dragDropHighlight != null)
                dragDropHighlight.enabled = false;
            if (TargetedSlot == this)
            {
                GameLogger.LogVerbose("Card", $"Arrow left enemy slot [{_enemyIndex}]", this);
                TargetedSlot = null;
            }
        }

        #endregion

        #region Private
        private static void PulseTransform(Transform t)
        {
            t.DOKill();
            t.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0f).SetLink(t.gameObject);
        }

        private void TweenFill(Image img, float target)
        {
            if (img == null)
                return;
            DOTween.Kill(img);
            DOTween
                .To(() => img.fillAmount, x => img.fillAmount = x, target, _hostilityBarDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        #endregion
    }
}
