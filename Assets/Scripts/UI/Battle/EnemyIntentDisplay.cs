using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the enemy's declared intent — what move they will execute on their next turn.
    ///
    /// Embed this as a child of each EnemySlotUI prefab. EnemySlotUI drives it directly
    /// via ShowIntent() — no EventBus subscriptions needed here.
    ///
    /// Inspector wiring:
    ///   intentPanel      → the root GameObject of this intent display (show/hide)
    ///   intentIcon       → Image showing a sword / shield / etc. icon (optional)
    ///   intentNameText   → TMP_Text showing the move name, e.g. "Aggressive Debate"
    ///   intentDescText   → TMP_Text showing the description, e.g. "Will deal 8 damage"
    ///   intentTypeBadge  → Image colour-coded by move type (Attack=red, Defend=blue, …)
    ///   intentTheme      → ScriptableObject mapping EnemyMoveType → Sprite + Color
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

        [Header("Intent Theme")]
        [Tooltip("ScriptableObject mapping each EnemyMoveType to a Sprite and Color. " +
                 "Create via: Assets → Create → Crookedile → Enemy → Intent Theme")]
        [SerializeField] private EnemyIntentTheme intentTheme;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake() => SetPanelVisible(false);

        // ─── Display ──────────────────────────────────────────────────────────────

        public void ShowIntent(EnemyMoveData move, StatusEffectManager attackerStatus = null)
        {
            if (move == null)
            {
                SetPanelVisible(false);
                return;
            }

            SetPanelVisible(true);

            // Look up icon and colour from the theme asset (fallback: no icon, white badge)
            var (icon, color) = intentTheme != null
                ? intentTheme.GetVisual(move.MoveType)
                : (null, Color.white);

            // Icon
            if (intentIcon != null)
            {
                intentIcon.sprite  = icon;
                intentIcon.enabled = icon != null;
            }

            // Text
            if (intentNameText != null)
                intentNameText.text = move.MoveName;

            if (intentDescText != null)
            {
                bool isOffensive = move.MoveType == EnemyMoveType.Attack
                                || move.MoveType == EnemyMoveType.OffensiveBuff
                                || move.MoveType == EnemyMoveType.DebuffAttack;
                intentDescText.text = isOffensive ? BuildDamagePreview(move, attackerStatus) : string.Empty;
            }

            // Colour badge by move type
            if (intentTypeBadge != null)
                intentTypeBadge.color = color;
        }

        /// <summary>
        /// Sums all damage effects in the move, applies attacker status modifiers (Weakened, Strength),
        /// and returns a display string. Fixed damage → total number. Random damage → "min-max".
        /// DamageEqualToComposure → "?". Returns empty string if no damage effects are present.
        /// </summary>
        private static string BuildDamagePreview(EnemyMoveData move, StatusEffectManager attackerStatus)
        {
            int fixedTotal = 0;
            int randMin = 0, randMax = 0;
            bool hasFixed = false, hasRandom = false, hasComposure = false;

            foreach (var effect in move.Effects)
            {
                if (effect.Category != EffectCategory.Damage) continue;
                switch (effect.DamageType)
                {
                    case DamageType.FixedDamage:
                        fixedTotal += effect.DamageAmount;
                        hasFixed = true;
                        break;
                    case DamageType.RandomDamage:
                        randMin += effect.RandomDamageMin;
                        randMax += effect.RandomDamageMax;
                        hasRandom = true;
                        break;
                    case DamageType.DamageEqualToComposure:
                        hasComposure = true;
                        break;
                }
            }

            if (!hasFixed && !hasRandom && !hasComposure) return string.Empty;
            if (hasComposure) return hasFixed ? $"{fixedTotal}+" : "?";

            // Apply attacker status modifiers (Weakened reduces, Strength increases)
            if (hasRandom)
            {
                int adjMin = Mathf.Max(0, attackerStatus != null
                    ? attackerStatus.ModifyDamageDealt(randMin + fixedTotal)
                    : randMin + fixedTotal);
                int adjMax = Mathf.Max(0, attackerStatus != null
                    ? attackerStatus.ModifyDamageDealt(randMax + fixedTotal)
                    : randMax + fixedTotal);
                return $"{adjMin}-{adjMax}";
            }

            int adj = Mathf.Max(0, attackerStatus != null
                ? attackerStatus.ModifyDamageDealt(fixedTotal)
                : fixedTotal);
            return adj > 0 ? adj.ToString() : string.Empty;
        }

        private void SetPanelVisible(bool visible)
        {
            if (intentPanel != null)
                intentPanel.SetActive(visible);
        }
    }
}
