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
        IDropHandler,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [Header("Display")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text resolveText;
        [SerializeField] private TMP_Text hostilityText;
        [SerializeField] private TMP_Text intentText;

        [Header("Interaction")]
        [SerializeField] private Button     selectButton;
        [SerializeField] private Image      selectionHighlight;
        [SerializeField] private Image      dragDropHighlight;
        [SerializeField] private GameObject defeatedOverlay;

        private int            _enemyIndex;
        private BattleManager  _battleManager;
        private OriginType     _playerOrigin;

        // ─── Initialization ───────────────────────────────────────────────────────

        /// <summary>
        /// Called by BattleUI when spawning this slot. Must be called before the first frame.
        /// </summary>
        public void Initialize(int index, BattleManager manager, OriginType playerOrigin)
        {
            _enemyIndex    = index;
            _battleManager = manager;
            _playerOrigin  = playerOrigin;

            // Click-to-focus removed; focus is now set implicitly by drag-to-enemy
            if (selectButton       != null) selectButton.interactable = false;
            if (defeatedOverlay    != null) defeatedOverlay.SetActive(false);
            if (selectionHighlight != null) selectionHighlight.enabled = false;
            if (dragDropHighlight  != null) dragDropHighlight.enabled  = false;

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

            if (nameText    != null) nameText.SetText(enemy.EnemyData.EnemyName);
            if (resolveText != null) resolveText.SetText($"Resolve: {enemy.Stats.CurrentResolve}/{enemy.Stats.MaxResolve}");

            if (hostilityText != null)
            {
                bool showExact = _playerOrigin == OriginType.Actor;
                int  h         = enemy.Stats.CurrentHostility;

                hostilityText.text  = showExact
                    ? $"Hostility: {h:+0;-0;0}"
                    : h < 0 ? "Receptive" : h > 0 ? "Hostile" : "Guarded";

                hostilityText.color = h < 0 ? new Color(0.2f, 0.8f, 0.2f)   // green  = receptive
                                    : h > 0 ? new Color(0.8f, 0.2f, 0.2f)   // red    = hostile
                                    :         Color.white;                    // white  = neutral
            }
        }

        /// <summary>
        /// Updates the intent line when the enemy declares their next move.
        /// </summary>
        public void UpdateIntent(EnemyMoveData move)
        {
            if (intentText != null)
                intentText.SetText(move != null ? move.IntentDescription : string.Empty);
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
            if (defeatedOverlay != null) defeatedOverlay.SetActive(true);
            if (selectButton    != null) selectButton.interactable = false;
        }

        /// <summary>
        /// Briefly scales the hostility text up to signal a change.
        /// </summary>
        public void PulseHostility()
        {
            if (hostilityText != null)
                StartCoroutine(PulseText(hostilityText));
        }

        // ─── Drop / Drag Handlers ─────────────────────────────────────────────────

        /// <summary>
        /// Fires when a dragged CardButton is released over this slot.
        /// Silently focuses this enemy then plays the card.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (_battleManager == null) return;

            CardButton card = eventData.pointerDrag?.GetComponent<CardButton>();
            if (card == null || !card.IsPlayable) return;

            if (_enemyIndex < _battleManager.Enemies.Count &&
                _battleManager.Enemies[_enemyIndex].IsDefeated) return;

            _battleManager.SetFocusedEnemy(_enemyIndex);
            card.NotifyDropHandled();
            card.PlayFromDrop();
        }

        /// <summary>Shows the drag-drop highlight when a card is being dragged over this slot.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CardButton.DraggedCard != null && dragDropHighlight != null)
                dragDropHighlight.enabled = true;
        }

        /// <summary>Hides the drag-drop highlight when the cursor leaves this slot.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (dragDropHighlight != null)
                dragDropHighlight.enabled = false;
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
