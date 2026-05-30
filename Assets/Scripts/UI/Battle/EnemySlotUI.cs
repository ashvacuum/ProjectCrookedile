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
    /// Displays the enemy's name, Resolve, hostility state, and current intent.
    /// Clicking the panel focuses this enemy as the player's target.
    ///
    /// Spawned at runtime by BattleUI.BuildEnemySlots() for each enemy in the battle.
    /// Designed to be used as a prefab: assign text/button/image references in the Inspector.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class EnemySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Display")]
        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text resolveText;

        [Tooltip(
            "Filled Image (fill method = Horizontal) driven by Resolve. Lerps smoothly when damaged."
        )]
        [SerializeField]
        private Image resolveBarFill;

        [SerializeField]
        private TMP_Text hostilityText;

        [SerializeField]
        private TMP_Text composureText;

        [SerializeField]
        private GameObject composureObject;

        [SerializeField]
        private Image enemySprite;

        [SerializeField]
        private EnemyIntentDisplay _intentDisplay;

        [SerializeField]
        private StatusEffectPanelUI _statusEffectPanel;

        [Header("HP Bar")]
        [Tooltip("How fast the HP bar lerps toward the target fill. Higher = snappier.")]
        [SerializeField]
        private float barLerpSpeed = 8f;

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
        private OriginType _playerOrigin;
        private float _targetFill = 1f;

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
        public void Initialize(
            int index,
            BattleManager manager,
            OriginType playerOrigin,
            EnemyData enemyData
        )
        {
            _enemyIndex = index;
            _battleManager = manager;
            _playerOrigin = playerOrigin;

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

            if (composureText != null)
                composureText.SetText(
                    enemy.Stats.CurrentComposure > 0
                        ? $"{enemy.Stats.CurrentComposure}"
                        : string.Empty
                );

            if (composureObject != null)
                composureObject.SetActive(enemy.Stats.CurrentComposure > 0);

            if (hostilityText == null)
                return;
            var showExact = _playerOrigin == OriginType.Actor;
            var h = enemy.Stats.CurrentHostility;

            hostilityText.text =
                showExact ? $"Hostility: {h:+0;-0;0}"
                : h < 0 ? "Receptive"
                : h > 0 ? "Hostile"
                : "Guarded";

            hostilityText.color =
                h < 0 ? new Color(0.2f, 0.8f, 0.2f) // green  = receptive
                : h > 0 ? new Color(0.8f, 0.2f, 0.2f) // red    = hostile
                : Color.white; // white  = neutral

            // Buff/debuff icons
            var effects = _battleManager?.Enemies[_enemyIndex]?.StatusEffects;
            if (effects != null)
                _statusEffectPanel?.Refresh(effects);
        }

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
        /// Called when this enemy's Resolve reaches zero.
        /// </summary>
        public void MarkDefeated()
        {
            // Hide all live display elements
            if (nameText != null)
                nameText.gameObject.SetActive(false);
            if (resolveText != null)
                resolveText.gameObject.SetActive(false);
            if (hostilityText != null)
                hostilityText.gameObject.SetActive(false);
            if (composureText != null)
                composureText.gameObject.SetActive(false);
            if (composureObject != null)
                composureObject.SetActive(false);
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
        /// Briefly scales the hostility text up to signal a change.
        /// </summary>
        public void PulseHostility()
        {
            if (hostilityText != null)
                PulseTransform(hostilityText.transform);
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

        #region HP Bar (resolve bar removed from prefab; Update kept null-safe)
        private void Update()
        {
            // resolveBarFill has been removed from the enemy slot prefab.
            // This method is kept as a no-op so any remaining [SerializeField] reference
            // on older prefabs doesn't cause errors.
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
            card.PlayFromDrop();
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
    }
}
        #endregion
        #endregion
