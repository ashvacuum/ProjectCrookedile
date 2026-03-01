using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Crookedile.Data;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;

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
    public class EnemySlotUI : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Display")] [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text resolveText;
        [SerializeField] private TMP_Text hostilityText;
        [SerializeField] private TMP_Text composureText;
        [SerializeField] private Image composureImage;
        [SerializeField] private EnemyIntentDisplay _intentDisplay;

        [Header("Interaction")] [SerializeField]
        private Button selectButton;

        [SerializeField] private Image selectionHighlight;
        [SerializeField] private Image dragDropHighlight;
        [SerializeField] private GameObject defeatedOverlay;

        private int _enemyIndex;
        private BattleManager _battleManager;
        private OriginType _playerOrigin;

        /// <summary>The enemy slot currently targeted by the card targeting arrow, or null.</summary>
        public static EnemySlotUI TargetedSlot { get; private set; }

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

        // ─── Initialization ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by BattleUI when spawning this slot. Must be called before the first frame.
        /// </summary>
        public void Initialize(int index, BattleManager manager, OriginType playerOrigin)
        {
            _enemyIndex = index;
            _battleManager = manager;
            _playerOrigin = playerOrigin;

            // Click-to-focus removed; focus is now set implicitly by drag-to-enemy
            if (selectButton != null) selectButton.interactable = false;
            if (defeatedOverlay != null) defeatedOverlay.SetActive(false);
            if (selectionHighlight != null) selectionHighlight.enabled = false;
            if (dragDropHighlight != null) dragDropHighlight.enabled = false;

            _intentDisplay?.ShowIntent(null); // hidden until intent is declared

            Refresh();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the latest stats from BattleManager and updates all display elements.
        /// </summary>
        public void Refresh()
        {
            if (_battleManager == null || _enemyIndex >= _battleManager.Enemies.Count) return;
            var enemy = _battleManager.Enemies[_enemyIndex];
            if (enemy.IsDefeated) return;

            if (nameText != null) nameText.SetText(enemy.EnemyData.EnemyName);
            if (resolveText != null)
                resolveText.SetText($"Resolve: {enemy.Stats.CurrentResolve}/{enemy.Stats.MaxResolve}");
            
            if(composureText != null)
                composureText.SetText(enemy.Stats.CurrentComposure > 0 ? $"Composure: {enemy.Stats.CurrentComposure}" : string.Empty);
            
            if(composureImage != null)
                composureImage.gameObject.SetActive(enemy.Stats.CurrentComposure > 0);

            if (hostilityText == null) return;
            var showExact = _playerOrigin == OriginType.Actor;
            var h = enemy.Stats.CurrentHostility;

            hostilityText.text = showExact
                ? $"Hostility: {h:+0;-0;0}"
                : h < 0
                    ? "Receptive"
                    : h > 0
                        ? "Hostile"
                        : "Guarded";

            hostilityText.color = h < 0 ? new Color(0.2f, 0.8f, 0.2f) // green  = receptive
                : h > 0 ? new Color(0.8f, 0.2f, 0.2f) // red    = hostile
                : Color.white; // white  = neutral
        }

        /// <summary>
        /// Updates the intent display when the enemy declares their next move.
        /// </summary>
        public void UpdateIntent(EnemyMoveData move)
        {
            _intentDisplay?.ShowIntent(move);
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
            if (nameText       != null) nameText.gameObject.SetActive(false);
            if (resolveText    != null) resolveText.gameObject.SetActive(false);
            if (hostilityText  != null) hostilityText.gameObject.SetActive(false);
            if (composureText  != null) composureText.gameObject.SetActive(false);
            if (composureImage != null) composureImage.gameObject.SetActive(false);
            if (_intentDisplay != null) _intentDisplay.gameObject.SetActive(false);
            if (selectionHighlight != null) selectionHighlight.enabled = false;
            if (dragDropHighlight  != null) dragDropHighlight.enabled  = false;
            if (selectButton       != null) selectButton.interactable  = false;

            // Show defeated state
            if (defeatedOverlay != null) defeatedOverlay.SetActive(true);
        }

        /// <summary>
        /// Briefly scales the hostility text up to signal a change.
        /// </summary>
        public void PulseHostility()
        {
            if (hostilityText != null)
                StartCoroutine(PulseText(hostilityText));
        }

        // ─── Targeting Handlers ───────────────────────────────────────────────────

        /// <summary>
        /// Sets this enemy as the focused target and plays the given card.
        /// Called by <c>CardButton.OnEndDrag</c> when the targeting arrow is released over this slot.
        /// </summary>
        public void PlayCardOnEnemy(CardButton card)
        {
            if (_battleManager == null || card == null) return;
            if (_enemyIndex < _battleManager.Enemies.Count &&
                _battleManager.Enemies[_enemyIndex].IsDefeated) return;

            _battleManager.SetFocusedEnemy(_enemyIndex);
            card.PlayFromDrop();
        }

        /// <summary>Shows the targeting highlight when the arrow enters this slot during targeting mode.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CardButton.IsTargeting) return;
            if (dragDropHighlight != null) dragDropHighlight.enabled = true;
            TargetedSlot = this;
        }

        /// <summary>Hides the targeting highlight and clears this slot as the target when the cursor leaves.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (dragDropHighlight != null) dragDropHighlight.enabled = false;
            if (TargetedSlot == this) TargetedSlot = null;
        } 

        // ─── Private ──────────────────────────────────────────────────────────────

        private IEnumerator PulseText(TMP_Text text)
        {
            Vector3 original = text.transform.localScale;
            text.transform.localScale = original * 1.2f;
            yield return new WaitForSeconds(0.15f);
            text.transform.localScale = original;
        }
    }
}
