using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using MoreMountains.Feedbacks;
using Crookedile.Data.Cards;
using Crookedile.Data;
using Crookedile.Utilities;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// 2D UI card component. Displays a card in the player's hand using Canvas/UI elements.
    /// Handles artwork, frames, cost display, hover state, and click-to-play.
    /// Replaces the old Card3DView + CardButton split. This is now the single card view component.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class CardButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        // ─── UI Structure References ──────────────────────────────────────────────

        [Header("Card Art")] [Tooltip("Shows the card's artwork sprite from CardData")] [SerializeField]
        private Image artworkImage;

        [Tooltip("Border frame image — set by card type (Diplomacy/Hostility/Manipulate)")] [SerializeField]
        private Image typeFrameImage;

        [Tooltip("Rarity overlay image — set by card rarity (Basic/Enhanced/Rare)")] [SerializeField]
        private Image rarityOverlayImage;

        [Header("Card Text")] [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardCostText;
        [SerializeField] private TMP_Text cardDescriptionText;
        [SerializeField] private TMP_Text flavorText;

        [Header("Card Type Color Strip")]
        [Tooltip("Optional colored strip at top/bottom indicating card type")]
        [SerializeField]
        private Image cardTypeStrip;

        // ─── Visual Settings ──────────────────────────────────────────────────────

        [Header("Visual Settings")]
        [Tooltip("Optional: reference to CardVisualSettings for frame/rarity sprites")]
        [SerializeField]
        private CardVisualSettings visualSettings;

        [Header("Card Type Colors")] [SerializeField]
        private Color pressureColor = new Color(0.2f, 0.8f, 0.2f); // Green

        [SerializeField] private Color rhetoricColor = new Color(0.8f, 0.2f, 0.2f); // Red
        [SerializeField] private Color policyColor = new Color(0.2f, 0.5f, 0.9f); // Blue
        [SerializeField] private Color unaffordableColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        // ─── Hover / Scale ────────────────────────────────────────────────────────

        [Header("Hover Behaviour")] [Tooltip("How much to scale up on hover (1.1 = 10% bigger)")] [SerializeField]
        private float hoverScale = 1.12f;

        [Tooltip("How fast the scale animates")] [SerializeField]
        private float hoverLerpSpeed = 12f;

        [Tooltip("Gap in pixels between the card's bottom edge and the screen bottom when hovered. " +
                 "All cards share this height regardless of their arc position.")]
        [SerializeField]
        private float hoverEdgePadding = 6f;

        // ─── Drag to Play ─────────────────────────────────────────────────────────

        [Header("Drag to Play")]
        [Tooltip("How many pixels upward the card must travel to trigger a play when not dropped on an enemy.")]
        [SerializeField]
        private float dragUpThreshold = 100f;

        // ─── MMFeedbacks ──────────────────────────────────────────────────────────

        [Header("Feedbacks")] [Tooltip("Plays when this card is drawn into hand")]
        public MMFeedbacks drawFeedback;

        [Tooltip("Plays when mouse enters the card")]
        public MMFeedbacks hoverEnterFeedback;

        [Tooltip("Plays when mouse leaves the card")]
        public MMFeedbacks hoverExitFeedback;

        [Tooltip("Plays when the card is selected / clicked")]
        public MMFeedbacks selectFeedback;

        [Tooltip("Plays when the card is discarded from hand")]
        public MMFeedbacks discardFeedback;

        // ─── Policy-Only Elements ─────────────────────────────────────────────────

        [Header("Policy-Only Elements")]
        [Tooltip("Icon shown when this Policy card leans Left. Leave null on Pressure/Rhetoric prefabs.")]
        [SerializeField]
        private Image _policyLeanLeftIcon;

        [Tooltip("Icon shown when this Policy card leans Center. Leave null on Pressure/Rhetoric prefabs.")]
        [SerializeField]
        private Image _policyLeanCenterIcon;

        [Tooltip("Icon shown when this Policy card leans Right. Leave null on Pressure/Rhetoric prefabs.")]
        [SerializeField]
        private Image _policyLeanRightIcon;

        // ─── Selection (CardSelectionPanel) ───────────────────────────────────────

        [Header("Selection")]
        [Tooltip(
            "Overlay Image shown when this card is selected in a CardSelectionPanel. Assign a colored border child.")]
        [SerializeField]
        private Image _selectionOutline;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private CardData cardData;
        private int handIndex;

        /// <summary>Read-only access to the card data — used by HandPanel.ExtractCard to match buttons.</summary>
        public CardData CardData => cardData;

        private Action onClickCallback;

        private bool isHovered;
        private bool isPlayable;

        // Cached cost values from Initialize so CanAfford and RefreshVisuals use the same numbers
        // as the AP-spend validation in BattleManager (status effect modifiers applied).
        private int _effectiveCost;
        private bool _forceUnplayable; // true when Silenced blocks a Rhetoric card, etc.

        private Vector3 baseScale;
        private Vector3 basePosition;
        private Vector3 targetScale;
        private Vector3 targetPosition;
        private Quaternion baseRotation;
        private Quaternion targetRotation;

        private int baseSiblingIndex;

        // True once SetBasePosition() has been called explicitly. Prevents Update() from
        // lerping cards toward (0,0) before a layout group has had a chance to position them.
        private bool _basePositionSet;

        // Drag / targeting state
        private bool _isDragging;
        private bool _isTargeting;
        private Vector2 _dragStartScreenPos;

        /// <summary>The card currently being dragged, or null.</summary>
        public static CardButton DraggedCard { get; private set; }

        /// <summary>True while a card drag has crossed the upward threshold and targeting mode is active.</summary>
        public static bool IsTargeting { get; private set; }

        /// <summary>
        /// The RectTransform of the most recently played card. Set just before the play callback
        /// fires so <c>BattleManager</c> can use it as the VFX spawn origin without the event
        /// needing to carry UI references.
        /// </summary>
        public static RectTransform LastPlayedRect { get; private set; }

        /// <summary>Whether this card can currently be played (enough AP).</summary>
        public bool IsPlayable => isPlayable;

        /// <summary>
        /// Set by <see cref="Initialize"/>. <see cref="BattlePoolManager"/> reads this in
        /// <c>ReturnCard</c> to route the button back to the correct pool without callers
        /// needing to track which pool a button was rented from.
        /// </summary>
        public CardType PooledCardType { get; private set; }

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            baseScale = Vector3.one;
            basePosition = transform.localPosition;
            targetScale = Vector3.one;
            targetPosition = basePosition;
            baseRotation = transform.localRotation;
            targetRotation = baseRotation;
        }

        private void Update()
        {
            // Smooth hover scale, lift, and rotation.
            // Position and rotation lerps are skipped until SetBasePosition() has been called —
            // this prevents cards spawned inside layout groups from animating before the layout
            // pass has assigned their real slot positions and arc-tilt angles.
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * hoverLerpSpeed);
            if (_basePositionSet)
            {
                transform.localPosition =
                    Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * hoverLerpSpeed);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation,
                    Time.deltaTime * hoverLerpSpeed);
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Initialize the card with data, current AP, pre-computed effective cost, an optional
        /// force-unplayable flag (e.g. Silenced blocking Rhetoric), and a click callback.
        /// </summary>
        public void Initialize(CardData card, int index, int currentActionPoints,
            int effectiveCost, bool forceUnplayable = false, Action onClick = null)
        {
            PooledCardType = card.CardType;
            cardData = card;
            handIndex = index;
            onClickCallback = onClick;
            _effectiveCost = effectiveCost;
            _forceUnplayable = forceUnplayable;
            isPlayable = !forceUnplayable && CanAfford(currentActionPoints);

            baseScale = Vector3.one;
            basePosition = transform.localPosition;
            targetScale = baseScale;
            targetPosition = basePosition;
            baseRotation = transform.localRotation;
            targetRotation = baseRotation;
            _basePositionSet = false; // cleared until SetBasePosition() is called by the layout

            UpdateDisplay();
        }

        /// <summary>
        /// Refreshes all visuals — call when AP changes mid-turn so affordability updates.
        /// Uses cached _effectiveCost and _forceUnplayable from Initialize.
        /// </summary>
        public void RefreshVisuals(int currentActionPoints)
        {
            if (cardData == null) return;
            isPlayable = !_forceUnplayable && CanAfford(currentActionPoints);
            UpdateDisplay();
        }

        /// <summary>
        /// Shows or hides the selection outline overlay.
        /// Called by CardSelectionPanel to indicate whether this card is currently selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionOutline != null)
                _selectionOutline.enabled = selected;
        }

        /// <summary>
        /// Updates the card's resting position after arc layout is applied.
        /// Called by CardHandLayout after it positions each card so hover-lift
        /// knows where to return the card when the mouse leaves.
        /// </summary>
        public void SetBasePosition(Vector3 position)
        {
            _basePositionSet = true;
            basePosition = position;

            // Capture the arc-tilt rotation that CardHandLayout wrote to localRotation
            // immediately before calling this method. This is the card's canonical resting angle.
            baseRotation = transform.localRotation;

            // Only update targets if not mid-hover; otherwise the card would snap back
            // to its new base values while the player is still mousing over it.
            if (!isHovered)
            {
                targetPosition = position;
                targetRotation = baseRotation;
            }
        }

        /// <summary>
        /// Records the canonical sibling index assigned by CardHandLayout.
        /// Called immediately after SetSiblingIndex so OnPointerExit can restore it.
        /// </summary>
        public void SetBaseSiblingIndex(int index)
        {
            baseSiblingIndex = index;
        }

        /// <summary>
        /// Plays the draw feedback. Call from BattleUI when this card enters hand.
        /// </summary>
        public void PlayDrawAnimation()
        {
            drawFeedback?.PlayFeedbacks();
        }

        /// <summary>
        /// Plays the discard feedback. Call from BattleUI before destroying this card.
        /// </summary>
        public void PlayDiscardAnimation()
        {
            discardFeedback?.PlayFeedbacks();
        }

        // ─── Pointer Handlers ─────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isHovered || _isDragging) return;
            isHovered = true;

            targetScale = baseScale * hoverScale;
            targetPosition = new Vector3(basePosition.x, ComputeHoverY(), basePosition.z);
            targetRotation = Quaternion.identity; // straighten the arc tilt on hover

            // Bring this card in front of all its neighbours while hovered.
            // CardHandLayout.SetSiblingOrder() restores natural z-order on the next hand rebuild.
            transform.SetAsLastSibling();

            hoverEnterFeedback?.PlayFeedbacks();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHovered) return;
            isHovered = false;

            targetScale = baseScale;
            targetPosition = basePosition;
            targetRotation = baseRotation; // return to arc tilt
            transform.SetSiblingIndex(baseSiblingIndex);

            hoverExitFeedback?.PlayFeedbacks();
        }

        /// <summary>
        /// Returns the local Y position that places this card's bottom edge just above the
        /// canvas bottom boundary by <see cref="hoverEdgePadding"/> pixels.
        /// Uniform across all cards regardless of arc position.
        /// Falls back to <see cref="basePosition"/>.y when no Canvas ancestor is found.
        /// </summary>
        private float ComputeHoverY()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return basePosition.y;

            var canvasRect = canvas.GetComponent<RectTransform>();
            var myRect = GetComponent<RectTransform>();

            // Bottom edge of the canvas converted to this card's parent local space
            Vector3 bottomWorld = canvas.transform.TransformPoint(
                new Vector3(0f, canvasRect.rect.yMin, 0f));
            float bottomLocal = transform.parent.InverseTransformPoint(bottomWorld).y;

            // Position card centre so its bottom edge sits hoverEdgePadding above the canvas edge.
            // Multiply by baseScale.y * hoverScale because the card is scaled up on hover —
            // rect.height alone is the unscaled size and would place the card too low.
            return bottomLocal + myRect.rect.height * 0.5f * baseScale.y * hoverScale + hoverEdgePadding;
        }

        // ─── Drag Handlers ────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isPlayable) return;

            _isDragging = true;
            _isTargeting = false;
            _dragStartScreenPos = eventData.position;
            DraggedCard = this;
            GameLogger.LogInfo("Card", $"Drag started: {cardData?.CardName}", this);

            // Clear hover state; card stays at its arc position (no re-parenting or cursor-follow).
            isHovered = false;
            targetScale = baseScale;
            targetRotation = Quaternion.identity;

            // Disable raycasts so enemy slots above the hand can receive pointer events.
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            float yDelta = eventData.position.y - _dragStartScreenPos.y;

            if (yDelta >= dragUpThreshold && !_isTargeting)
            {
                GameLogger.LogInfo("Card", $"Targeting mode entered (yDelta={yDelta:F0}, threshold={dragUpThreshold})", this);
                EnterTargetingMode(eventData);
            }
            else if (yDelta < dragUpThreshold && _isTargeting)
                ExitTargetingMode();

            if (_isTargeting)
                UpdateTargetingArrow(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Snapshot TargetedSlot BEFORE ExitTargetingMode, which calls ClearTargetedSlot
            // and would wipe it before we get a chance to act on it.
            EnemySlotUI targetedSlot = EnemySlotUI.TargetedSlot;
            bool wasTargeting = _isTargeting;

            GameLogger.LogInfo("Card",
                $"OnEndDrag: card={cardData?.CardName}  wasTargeting={wasTargeting}  targetedSlot={targetedSlot?.name ?? "none"}  requiresTarget={RequiresSpecificTarget()}  callback={(onClickCallback != null ? "set" : "null")}", this);

            if (_isTargeting)
                ExitTargetingMode();

            DraggedCard = null;

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;

            if (wasTargeting)
            {
                if (targetedSlot != null)
                {
                    GameLogger.LogInfo("Card", $"Playing '{cardData?.CardName}' on enemy slot: {targetedSlot.name}", this);
                    targetedSlot.PlayCardOnEnemy(this);
                }
                else if (!RequiresSpecificTarget())
                {
                    GameLogger.LogInfo("Card", $"AOE/self card '{cardData?.CardName}' — playing on current focus", this);
                    LastPlayedRect = GetComponent<RectTransform>();
                    selectFeedback?.PlayFeedbacks();
                    onClickCallback?.Invoke();
                }
                else
                {
                    GameLogger.LogWarning("Card", $"Single-target '{cardData?.CardName}' released in empty space — cancelled", this);
                    targetPosition = basePosition;
                    targetScale = baseScale;
                    targetRotation = baseRotation;
                }
            }
            else
            {
                GameLogger.LogVerbose("Card", $"Drag ended below threshold for '{cardData?.CardName}' — cancelled", this);
                targetPosition = basePosition;
                targetScale = baseScale;
                targetRotation = baseRotation;
            }
        }

        // ─── Targeting API (called by EnemySlotUI) ───────────────────────────────

        /// <summary>Plays the card after a successful targeting release onto an enemy slot.</summary>
        public void PlayFromDrop()
        {
            LastPlayedRect = GetComponent<RectTransform>();
            GameLogger.LogInfo("Card", $"PlayFromDrop: '{cardData?.CardName}' (callback={(onClickCallback != null ? "set" : "null")})", this);
            selectFeedback?.PlayFeedbacks();
            onClickCallback?.Invoke();
        }

        // ─── Targeting Helpers ────────────────────────────────────────────────────

        private void EnterTargetingMode(PointerEventData eventData)
        {
            _isTargeting = true;
            IsTargeting = true;

            targetScale = baseScale * hoverScale;
            targetRotation = Quaternion.identity;

            var rt = GetComponent<RectTransform>();
            CardTargetingArrow.Instance?.Show(rt, eventData.pressEventCamera);
            CardTargetingArrow.Instance?.UpdateEndPoint(eventData.position);
        }

        private void ExitTargetingMode()
        {
            _isTargeting = false;
            IsTargeting = false;

            targetScale = baseScale;

            CardTargetingArrow.Instance?.Hide();
            EnemySlotUI.ClearTargetedSlot();
        }

        private void UpdateTargetingArrow(PointerEventData eventData)
        {
            EnemySlotUI snap = EnemySlotUI.TargetedSlot;
            if (snap != null)
                CardTargetingArrow.Instance?.SnapTo(snap.GetComponent<RectTransform>());
            else
                CardTargetingArrow.Instance?.Unsnap();

            CardTargetingArrow.Instance?.UpdateEndPoint(eventData.position);
        }

        /// <summary>
        /// Returns true if any effect on this card requires aiming at a specific enemy
        /// (i.e. has TargetType.Opponent). AOE and self-targeting cards return false.
        /// </summary>
        private bool RequiresSpecificTarget()
        {
            if (cardData?.Effects == null) return false;
            foreach (CardEffect effect in cardData.Effects)
                if (effect.Category == EffectCategory.Damage && effect.Target == TargetType.Opponent)
                    return true;
            return false;
        }

        // ─── Internal Display ─────────────────────────────────────────────────────

        private void UpdateDisplay()
        {
            if (cardData == null) return;

            UpdateArtwork();
            UpdateFrames();
            UpdateText();
            UpdateTypeColor();
            UpdateAffordability();
            UpdatePolicyLean();
        }

        private void UpdateArtwork()
        {
            if (artworkImage == null) return;

            Sprite artwork = cardData.GetArtwork();
            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
        }

        private void UpdateFrames()
        {
            if (visualSettings == null) return;

            // Type frame
            if (typeFrameImage != null)
            {
                Sprite frame = visualSettings.GetFrameForType(cardData.CardType);
                typeFrameImage.sprite = frame;
                typeFrameImage.enabled = frame != null;
            }

            // Rarity overlay
            if (rarityOverlayImage != null)
            {
                Sprite rarity = visualSettings.GetFrameForRarity(cardData.Rarity);
                rarityOverlayImage.sprite = rarity;
                rarityOverlayImage.enabled = rarity != null;
            }
        }

        private void UpdateText()
        {
            if (cardNameText != null)
                cardNameText.text = cardData.GetDisplayName();

            if (cardCostText != null)
                cardCostText.text = GetCostString();

            if (cardDescriptionText != null)
                cardDescriptionText.text = cardData.Description;

            if (flavorText != null)
            {
                flavorText.text = cardData.CardType.ToString();
            }
        }

        private void UpdateTypeColor()
        {
            if (cardTypeStrip == null) return;
            cardTypeStrip.color = GetCardTypeColor(cardData.CardType);
        }

        private void UpdateAffordability()
        {
            // Dim the whole card if it can't be played
            var group = GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = isPlayable ? 1f : 0.5f;
        }

        private string GetCostString()
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0)
                return "0";

            var cost = cardData.Costs[0];

            if (cost.CostType == CostType.None) return "Free";
            if (cost.IsXCost) return "X";

            // Use the effective cost (post status-effect modifiers) so Focus/Energized/Entangled
            // are reflected in the cost display on the card.
            return _effectiveCost <= 0 ? "0" : _effectiveCost.ToString();
        }

        /// <summary>
        /// Shows only the lean icon that matches this Policy card's <see cref="PolicyLean"/>.
        /// All three image references are optional — Pressure/Rhetoric prefabs leave them null
        /// and nothing breaks.
        /// </summary>
        private void UpdatePolicyLean()
        {
            bool isPolicy = cardData?.CardType == CardType.Policy;

            if (_policyLeanLeftIcon != null)
                _policyLeanLeftIcon.enabled = isPolicy && cardData.PolicyLean == PolicyLean.Left;
            if (_policyLeanCenterIcon != null)
                _policyLeanCenterIcon.enabled = isPolicy && cardData.PolicyLean == PolicyLean.Center;
            if (_policyLeanRightIcon != null)
                _policyLeanRightIcon.enabled = isPolicy && cardData.PolicyLean == PolicyLean.Right;
        }

        private bool CanAfford(int currentAP)
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0) return true;
            var cost = cardData.Costs[0];
            if (cost.CostType == CostType.None) return true;
            // Compare against _effectiveCost (pre-computed from BattleManager.GetEffectiveCardCost)
            // so Focus/Energized/Entangled modifiers are respected.
            return currentAP >= _effectiveCost;
        }

        private Color GetCardTypeColor(CardType type)
        {
            return type switch
            {
                CardType.Pressure => pressureColor,
                CardType.Rhetoric => rhetoricColor,
                CardType.Policy => policyColor,
                _ => Color.white
            };
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (DraggedCard == this) DraggedCard = null;
            if (_isTargeting)
            {
                IsTargeting = false;
                CardTargetingArrow.Instance?.Hide();
            }

            onClickCallback = null;
        }
    }
}
