using System.Collections.Generic;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay.Battle;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Displays the enemy's declared intent — what move they will execute on their next turn.
    ///
    /// Embed this as a child of each EnemySlotUI prefab. EnemySlotUI drives it directly
    /// via ShowIntent() — no EventBus subscriptions needed here.
    ///
    /// Hovering the intent panel opens <see cref="BattleTooltipUI"/> with the move's
    /// authored <see cref="Crookedile.Data.Enemy.EnemyMoveData.Description"/>.
    ///
    /// Inspector wiring:
    ///   intentPanel      → the root GameObject of this intent display (show/hide)
    ///   intentIcon       → Image showing a sword / shield / etc. icon (optional)
    ///   intentNameText   → TMP_Text showing the move name, e.g. "Aggressive Debate"
    ///   intentDescText   → TMP_Text showing the description, e.g. "Will deal 8 damage"
    ///   intentTypeBadge  → Image colour-coded by move type (Attack=red, Defend=blue, …)
    ///   intentTheme      → ScriptableObject mapping EnemyMoveType → Sprite + Color
    ///
    /// NOTE: The intentPanel's background Image must have Raycast Target = true for
    /// hover events to fire.
    /// </summary>
    public class EnemyIntentDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Intent Panel")]
        [Tooltip("Root panel GameObject. Shown when intent is known; hidden at battle start.")]
        [SerializeField]
        private GameObject intentPanel;

        [Header("Move Info")]
        [Tooltip("Icon representing the move type (sword for Attack, shield for Defend, etc.)")]
        [SerializeField]
        private Image intentIcon;

        [Tooltip("Move name text, e.g. 'Aggressive Debate'")]
        [SerializeField]
        private TMP_Text intentNameText;

        [Tooltip(
            "When false the move-name label is hidden (still data-set so the tooltip works). "
                + "Enable in the Inspector if you want the text visible on screen."
        )]
        [SerializeField]
        private bool _showMoveName = false;

        [Tooltip("Intent description text, e.g. 'Will deal 8 damage'")]
        [SerializeField]
        private TMP_Text intentDescText;

        [Tooltip(
            "Size of the per-hit damage value in multi-hit previews, as a percentage of "
                + "the base text size. E.g. 70 renders '4\u00d72' as '4\u00d7<size=70%>2</size>'. "
                + "Set to 100 to disable sub-sizing."
        )]
        [Range(40, 100)]
        [SerializeField]
        private int _multiHitSubTextSize = 70;

        [Header("Move Type Badge")]
        [Tooltip("Background image whose colour changes to reflect the move type")]
        [SerializeField]
        private Image intentTypeBadge;

        [Header("Intent Theme")]
        [Tooltip(
            "ScriptableObject mapping each EnemyMoveType to a Sprite and Color. "
                + "Create via: Assets → Create → Crookedile → Enemy → Intent Theme"
        )]
        [SerializeField]
        private EnemyIntentTheme intentTheme;

        [Header("Bob Animation")]
        [Tooltip("Pixels the intent panel travels up and down per sine cycle.")]
        [SerializeField]
        private float _bobAmplitude = 4f;

        [Tooltip("Bob cycles per second.")]
        [SerializeField]
        private float _bobSpeed = 0.9f;

        [Header("Damage Text Punch")]
        [Tooltip("Pixels the damage-number text snaps upward the instant intent is revealed.")]
        [SerializeField]
        private float _punchRise = 10f;

        [Tooltip("Seconds to reach the peak of the punch.")]
        [SerializeField]
        private float _punchRiseDuration = 0.06f;

        [Tooltip("Seconds to ease back to the resting position after the peak.")]
        [SerializeField]
        private float _punchFallDuration = 0.35f;

        private EnemyMoveData _currentMove;

        // Bob state
        private float _bobPhase; // random per-instance offset so enemies don't sync
        private bool _isBobbing;
        private RectTransform _intentPanelRect;
        private Vector2 _bobAnchor; // panel's designed anchoredPosition (captured in Awake)

        // Punch animation state
        private RectTransform _descTextRect;
        private Vector2 _descTextAnchor; // desc text's authored anchoredPosition (captured in Awake)
        #region Unity Lifecycle
        private void Awake()
        {
            // Cache the RectTransform and remember the panel's authored resting position
            // before SetPanelVisible hides it — so the bob has a stable origin.
            _intentPanelRect =
                intentPanel != null ? intentPanel.GetComponent<RectTransform>() : null;
            if (_intentPanelRect != null)
                _bobAnchor = _intentPanelRect.anchoredPosition;

            // Cache the desc text rect so the punch coroutine has a stable resting origin.
            _descTextRect =
                intentDescText != null ? intentDescText.GetComponent<RectTransform>() : null;
            if (_descTextRect != null)
                _descTextAnchor = _descTextRect.anchoredPosition;

            // Random phase offset keeps multiple enemies from bobbing in unison
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);

            SetPanelVisible(false);
        }

        private void Update()
        {
            if (!_isBobbing || _intentPanelRect == null)
                return;
            float yOffset =
                Mathf.Sin(Time.time * _bobSpeed * Mathf.PI * 2f + _bobPhase) * _bobAmplitude;
            _intentPanelRect.anchoredPosition = _bobAnchor + new Vector2(0f, yOffset);
        }

        #endregion

        #region Display
        public void ShowIntent(
            EnemyMoveData move,
            StatusEffectManager attackerStatus = null,
            StatusEffectManager targetStatus = null
        )
        {
            _currentMove = move;

            if (move == null)
            {
                SetPanelVisible(false);
                return;
            }

            SetPanelVisible(true);

            // Look up icon and colour from the theme asset (fallback: no icon, white badge)
            var (icon, color) =
                intentTheme != null ? intentTheme.GetVisual(move.MoveType) : (null, Color.white);

            // Icon
            if (intentIcon != null)
            {
                intentIcon.sprite = icon;
                intentIcon.enabled = icon != null;
            }

            // Text — always store the name so the tooltip can read it; visibility is optional
            if (intentNameText != null)
            {
                intentNameText.text = move.MoveName;
                intentNameText.enabled = _showMoveName;
            }

            if (intentDescText != null)
            {
                bool isOffensive =
                    move.MoveType == EnemyMoveType.Attack
                    || move.MoveType == EnemyMoveType.OffensiveBuff
                    || move.MoveType == EnemyMoveType.DebuffAttack;
                string preview = isOffensive
                    ? BuildDamagePreview(move, attackerStatus, targetStatus, _multiHitSubTextSize)
                    : string.Empty;
                intentDescText.text = preview;
                if (!string.IsNullOrEmpty(preview))
                    TriggerDescPunch();
            }

            // Colour badge by move type
            if (intentTypeBadge != null)
                intentTypeBadge.color = color;
        }

        #endregion

        #region Pointer Events
        public void OnPointerEnter(PointerEventData _)
        {
            if (_currentMove == null || BattleTooltipUI.Instance == null)
                return;
            if (string.IsNullOrEmpty(_currentMove.Description))
                return;
            BattleTooltipUI.Instance.Show(_currentMove.MoveName, _currentMove.Description);
        }

        public void OnPointerExit(PointerEventData _)
        {
            BattleTooltipUI.Instance?.Hide();
        }

        #endregion

        #region Damage Preview
        /// <summary>
        /// Builds the intent description string shown beneath the move name.
        /// Supports multi-hit: equal fixed-damage effects → "N×&lt;size=X%&gt;amount&lt;/size&gt;";
        /// equal random → "N×&lt;size=X%&gt;min-max&lt;/size&gt;".
        /// Falls back to summing for single or mixed effects (existing behaviour).
        /// Returns empty string if no damage effects are present.
        /// </summary>
        private static string BuildDamagePreview(
            EnemyMoveData move,
            StatusEffectManager attackerStatus,
            StatusEffectManager targetStatus,
            int multiHitSubTextSize = 70
        )
        {
            // Collect damage previews from BattleEffect subclasses
            var previews = new List<DamagePreview>();
            foreach (var e in move.Effects)
            {
                var p = e?.GetDamagePreview();
                if (p.HasValue)
                    previews.Add(p.Value);
            }

            if (previews.Count == 0)
                return string.Empty;

            // Two-step preview: attacker mods (Strength/Weakened/Exposed) then target mods
            // (Vulnerable/Plated/Intangible). Neither step has side effects.
            static int Preview(int raw, StatusEffectManager atk, StatusEffectManager tgt)
            {
                int d = atk != null ? atk.PreviewDamageDealt(raw) : raw;
                d = tgt != null ? tgt.PreviewDamageTaken(d) : d;
                return Mathf.Max(0, d);
            }

            // Multi-hit: all identical fixed → "N×amount"
            if (
                previews.Count > 1
                && previews.TrueForAll(p => p.Type == DamagePreviewType.Fixed)
                && previews.TrueForAll(p => p.Amount == previews[0].Amount)
            )
            {
                int adj = Preview(previews[0].Amount, attackerStatus, targetStatus);
                return adj > 0
                    ? $"{previews.Count}\u00d7<size={multiHitSubTextSize}%>{adj}</size>"
                    : string.Empty;
            }

            // Multi-hit: all identical random → "N×min-max"
            if (
                previews.Count > 1
                && previews.TrueForAll(p => p.Type == DamagePreviewType.Random)
                && previews.TrueForAll(p =>
                    p.MinAmount == previews[0].MinAmount && p.MaxAmount == previews[0].MaxAmount
                )
            )
            {
                int adjMin = Preview(previews[0].MinAmount, attackerStatus, targetStatus);
                int adjMax = Preview(previews[0].MaxAmount, attackerStatus, targetStatus);
                return $"{previews.Count}\u00d7<size={multiHitSubTextSize}%>{adjMin}-{adjMax}</size>";
            }

            // Single or mixed — sum
            int fixedTotal = 0;
            int randMin = 0,
                randMax = 0;
            bool hasFixed = false,
                hasRandom = false,
                hasShield = false;

            foreach (var p in previews)
            {
                switch (p.Type)
                {
                    case DamagePreviewType.Fixed:
                        fixedTotal += p.Amount;
                        hasFixed = true;
                        break;
                    case DamagePreviewType.Random:
                        randMin += p.MinAmount;
                        randMax += p.MaxAmount;
                        hasRandom = true;
                        break;
                    case DamagePreviewType.EqualToShield:
                        hasShield = true;
                        break;
                }
            }

            if (!hasFixed && !hasRandom && !hasShield)
                return string.Empty;
            if (hasShield)
                return hasFixed ? $"{fixedTotal}+" : "?";

            // Apply full two-step preview pipeline
            if (hasRandom)
            {
                int adjMin = Preview(randMin + fixedTotal, attackerStatus, targetStatus);
                int adjMax = Preview(randMax + fixedTotal, attackerStatus, targetStatus);
                return $"{adjMin}-{adjMax}";
            }

            int finalAdj = Preview(fixedTotal, attackerStatus, targetStatus);
            return finalAdj > 0 ? finalAdj.ToString() : string.Empty;
        }

        private void SetPanelVisible(bool visible)
        {
            if (intentPanel != null)
                intentPanel.SetActive(visible);

            _isBobbing = visible;

            if (!visible)
            {
                // Snap both the panel and the desc text back to their resting positions
                if (_intentPanelRect != null)
                    _intentPanelRect.anchoredPosition = _bobAnchor;
                if (_descTextRect != null)
                    _descTextRect.anchoredPosition = _descTextAnchor;

                // Kill any in-progress punch and snap back to rest
                _descTextRect?.DOKill();
            }
        }

        #endregion

        #region Punch Animation
        private void TriggerDescPunch()
        {
            if (_descTextRect == null)
                return;
            _descTextRect.DOKill();
            _descTextRect.anchoredPosition = _descTextAnchor;
            // Phase 1: fast linear rise; Phase 2: quadratic ease-out fall
            DOTween
                .Sequence()
                .SetLink(gameObject)
                .Append(
                    _descTextRect
                        .DOAnchorPosY(_descTextAnchor.y + _punchRise, _punchRiseDuration)
                        .SetEase(Ease.Linear)
                )
                .Append(
                    _descTextRect
                        .DOAnchorPosY(_descTextAnchor.y, _punchFallDuration)
                        .SetEase(Ease.OutQuad)
                );
        }
        #endregion
    }
}
