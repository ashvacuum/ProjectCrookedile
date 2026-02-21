using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Core;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the enemy's declared intent — what move they will execute on their next turn.
    ///
    /// Place this component on a panel near the enemy portrait in the battle scene.
    /// It subscribes to EnemyIntentDeclaredEvent (fired at the start of the player's turn)
    /// so the player always knows the threat before choosing their cards.
    ///
    /// Inspector wiring:
    ///   intentPanel      → the root GameObject of this intent display (show/hide)
    ///   intentIcon       → Image showing a sword / shield / etc. icon (optional)
    ///   intentNameText   → TMP_Text showing the move name, e.g. "Aggressive Debate"
    ///   intentDescText   → TMP_Text showing the description, e.g. "Will deal 8 damage"
    ///   intentTypeBadge  → Image colour-coded by move type (Attack=red, Defend=blue, …)
    /// </summary>
    public class EnemyIntentDisplay : MonoBehaviour
    {
        [Header("Intent Panel")]
        [Tooltip("Root panel GameObject. Shown when intent is known; hidden at battle start.")]
        [SerializeField] private GameObject intentPanel;

        [Header("Move Info")]
        [Tooltip("Icon representing the move type (sword for Attack, shield for Defend, etc.)")]
        [SerializeField] private Image intentIcon;

        [Tooltip("Move name text, e.g. 'Aggressive Debate'")]
        [SerializeField] private TMP_Text intentNameText;

        [Tooltip("Intent description text, e.g. 'Will deal 8 damage'")]
        [SerializeField] private TMP_Text intentDescText;

        [Header("Move Type Badge")]
        [Tooltip("Background image whose colour changes to reflect the move type")]
        [SerializeField] private Image intentTypeBadge;

        [Header("Move Type Colors")]
        [SerializeField] private Color attackColor  = new Color(0.80f, 0.20f, 0.20f); // Red
        [SerializeField] private Color defendColor  = new Color(0.20f, 0.50f, 0.80f); // Blue
        [SerializeField] private Color buffColor    = new Color(0.20f, 0.80f, 0.20f); // Green
        [SerializeField] private Color debuffColor  = new Color(0.60f, 0.20f, 0.80f); // Purple

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<EnemyIntentDeclaredEvent>(OnIntentDeclared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<EnemyIntentDeclaredEvent>(OnIntentDeclared);
        }

        // ─── Event Handlers ───────────────────────────────────────────────────────

        private void OnBattleStarted(BattleStartedEvent evt)
        {
            // Hide until the first intent is declared (end of Turn 1 setup)
            SetPanelVisible(false);
        }

        private void OnIntentDeclared(EnemyIntentDeclaredEvent evt)
        {
            ShowIntent(evt.Move);
        }

        // ─── Display ──────────────────────────────────────────────────────────────

        private void ShowIntent(EnemyMoveData move)
        {
            if (move == null)
            {
                SetPanelVisible(false);
                return;
            }

            SetPanelVisible(true);

            // Icon
            if (intentIcon != null)
            {
                intentIcon.sprite  = move.IntentIcon;
                intentIcon.enabled = move.IntentIcon != null;
            }

            // Text
            if (intentNameText != null)
                intentNameText.text = move.MoveName;

            if (intentDescText != null)
                intentDescText.text = move.IntentDescription;

            // Colour badge by move type
            if (intentTypeBadge != null)
                intentTypeBadge.color = GetColorForMoveType(move.MoveType);
        }

        private void SetPanelVisible(bool visible)
        {
            if (intentPanel != null)
                intentPanel.SetActive(visible);
        }

        private Color GetColorForMoveType(EnemyMoveType type)
        {
            return type switch
            {
                EnemyMoveType.Attack  => attackColor,
                EnemyMoveType.Defend  => defendColor,
                EnemyMoveType.Buff    => buffColor,
                EnemyMoveType.Debuff  => debuffColor,
                _                     => Color.white
            };
        }
    }
}
