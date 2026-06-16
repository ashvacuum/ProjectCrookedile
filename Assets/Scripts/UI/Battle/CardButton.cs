using System;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Utilities;
using DG.Tweening;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// 2D UI card component. Displays a card in the player's hand using Canvas/UI elements.
    /// Handles artwork, frames, cost display, hover state, and click-to-play.
    /// Replaces the old Card3DView + CardButton split. This is now the single card view component.
    /// </summary>
    [Debuggable("Card", LogLevel.Info)]
    public class CardButton
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerExitHandler,
            IPointerClickHandler,
            IBeginDragHandler,
            IDragHandler,
            IEndDragHandler
    {
        #region UI Structure References
        [Header("Card Art")]
        [Tooltip("Shows the card's artwork sprite from CardData")]
        [SerializeField]
        private Image artworkImage;

        [Tooltip("Border frame image — set by card type (Diplomacy/Hostility/Manipulate)")]
        [SerializeField]
        private Image typeFrameImage;

        [Tooltip("Rarity overlay image — set by card rarity (Basic/Enhanced/Rare)")]
        [SerializeField]
        private Image rarityOverlayImage;

        [Header("Card Text")]
        [SerializeField]
        private TMP_Text cardNameText;

        [SerializeField]
        private TMP_Text cardCostText;

        [SerializeField]
        private TMP_Text cardDescriptionText;

        [SerializeField]
        private TMP_Text flavorText;

        [Header("Card Type Color Strip")]
        [Tooltip("Optional colored strip at top/bottom indicating card type")]
        [SerializeField]
        private Image cardTypeStrip;

        #endregion

        #region Visual Settings
        [Header("Visual Settings")]
        [Tooltip("Optional: reference to CardVisualSettings for frame/rarity sprites")]
        [SerializeField]
        private CardVisualSettings visualSettings;

        [Header("Card Type Colors")]
        [SerializeField]
        private Color pressureColor = new Color(0.2f, 0.8f, 0.2f); // Green

        [SerializeField]
        private Color rhetoricColor = new Color(0.8f, 0.2f, 0.2f); // Red

        [SerializeField]
        private Color policyColor = new Color(0.2f, 0.5f, 0.9f); // Blue

        [SerializeField]
        private Color statusColor = new Color(0.6f, 0.3f, 0.85f); // Purple

        [SerializeField]
        private Color curseColor = new Color(0.25f, 0.05f, 0.05f); // Dark crimson

        [SerializeField]
        private Color unaffordableColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        [Tooltip(
            "Default color for the cost text when no discount is active. "
                + "Should match the prefab's TMP default color."
        )]
        [SerializeField]
        private Color _defaultCostColor = Color.white;

        #endregion

        #region Hover / Scale
        [Header("Hover Behaviour")]
        [Tooltip("How much to scale up on hover (1.1 = 10% bigger)")]
        [SerializeField]
        private float hoverScale = 1.12f;

        [Tooltip("Duration in seconds for hover scale/lift/rotation tweens.")]
        [SerializeField]
        private float hoverTweenDuration = 0.15f;

        [Tooltip(
            "Gap in pixels between the card's bottom edge and the screen bottom when hovered. "
                + "All cards share this height regardless of their arc position."
        )]
        [SerializeField]
        private float hoverEdgePadding = 6f;

        #endregion

        #region Drag to Play
        [Header("Drag to Play")]
        [Tooltip(
            "How many pixels upward the card must travel to trigger a play when not dropped on an enemy."
        )]
        [SerializeField]
        private float dragUpThreshold = 100f;

        #endregion

        #region MMFeedbacks
        [Header("Feedbacks")]
        [Tooltip("Plays when this card is drawn into hand")]
        public MMFeedbacks drawFeedback;

        [Tooltip("Plays when mouse enters the card")]
        public MMFeedbacks hoverEnterFeedback;

        [Tooltip("Plays when mouse leaves the card")]
        public MMFeedbacks hoverExitFeedback;

        [Tooltip("Plays when the card is selected / clicked")]
        public MMFeedbacks selectFeedback;

        [Tooltip("Plays when the card is discarded from hand")]
        public MMFeedbacks discardFeedback;

        #endregion

        #region Policy-Only Elements
        [Header("Policy-Only Elements")]
        [Tooltip(
            "Icon shown when this Policy card leans Left. Leave null on Pressure/Rhetoric prefabs."
        )]
        [SerializeField]
        private Image _policyLeanLeftIcon;

        [Tooltip(
            "Icon shown when this Policy card leans Center. Leave null on Pressure/Rhetoric prefabs."
        )]
        [SerializeField]
        private Image _policyLeanCenterIcon;

        [Tooltip(
            "Icon shown when this Policy card leans Right. Leave null on Pressure/Rhetoric prefabs."
        )]
        [SerializeField]
        private Image _policyLeanRightIcon;

        #endregion

        #region Selection (CardChoicePanel)
        [Header("Selection")]
        [Tooltip(
            "Overlay Image shown when this card is selected in a CardChoicePanel. Assign a colored border child."
        )]
        [SerializeField]
        private Image _selectionOutline;

        #endregion

        #region Runtime State
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
        private bool _isCostDiscounted; // true when a battle effect has reduced/zeroed the cost

        private Vector3 baseScale;
        private Vector3 basePosition;
        private Quaternion baseRotation;

        private int baseSiblingIndex;

        // True once SetBasePosition() has been called explicitly. Prevents Update() from
        // lerping cards toward (0,0) before a layout group has had a chance to position them.
        private bool _basePositionSet;

        // Picker mode: card is a plain selectable button (reward / choose-a-card grids), not a
        // hand card. Suppresses hover-lift-to-canvas-bottom and drag-to-play; a plain click
        // fires onClickCallback instead. Set by RewardScreen / CardChoicePanel after Initialize.
        private bool _pickerMode;

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

        #endregion

        // Cached parent lookups — hover enter/exit is the hottest input path, so avoid
        // walking the hierarchy on every pointer event. Invalidated on re-parenting
        // (pool rent/return moves cards between containers).
        private Canvas _parentCanvasCache;
        private CardHandLayout _handLayoutCache;

        private Canvas ParentCanvas
        {
            get
            {
                if (_parentCanvasCache == null)
                    _parentCanvasCache = GetComponentInParent<Canvas>();
                return _parentCanvasCache;
            }
        }

        private CardHandLayout HandLayout
        {
            get
            {
                if (_handLayoutCache == null)
                    _handLayoutCache = GetComponentInParent<CardHandLayout>();
                return _handLayoutCache;
            }
        }

        #region Unity Lifecycle
        private void Awake()
        {
            baseScale = Vector3.one;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
        }

        private void OnTransformParentChanged()
        {
            _parentCanvasCache = null;
            _handLayoutCache = null;
        }

        #endregion

        #region Public API
        /// <summary>
        /// Initialize the card with data, current AP, pre-computed effective cost, optional flags
        /// (force-unplayable e.g. Silenced; cost-discounted e.g. MakeCardFree), and a click callback.
        /// </summary>
        public void Initialize(
            CardData card,
            int index,
            int currentActionPoints,
            int effectiveCost,
            bool forceUnplayable = false,
            bool isCostDiscounted = false,
            Action onClick = null
        )
        {
            PooledCardType = card.CardType;
            cardData = card;
            handIndex = index;
            onClickCallback = onClick;
            _effectiveCost = effectiveCost;
            _forceUnplayable = forceUnplayable;
            _isCostDiscounted = isCostDiscounted;
            isPlayable = !forceUnplayable && CanAfford(currentActionPoints);

            transform.DOKill();
            baseScale = Vector3.one;
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            _basePositionSet = false;
            _pickerMode = false; // default to hand behavior; pickers re-enable after Initialize

            UpdateDisplay();
        }

        /// <summary>
        /// Switches this button to picker mode (reward / choose-a-card grids): hover just pops
        /// the scale, drag-to-play is disabled, and a plain click fires the onClick callback.
        /// Call AFTER <see cref="Initialize"/> (which resets it to hand behavior).
        /// </summary>
        public void SetPickerMode(bool on) => _pickerMode = on;

        /// <summary>
        /// Refreshes all visuals — call when AP changes mid-turn so affordability updates.
        /// Uses cached _effectiveCost and _forceUnplayable from Initialize.
        /// </summary>
        public void RefreshVisuals(int currentActionPoints)
        {
            if (cardData == null)
                return;
            isPlayable = !_forceUnplayable && CanAfford(currentActionPoints);
            UpdateDisplay();
        }

        /// <summary>
        /// Shows or hides the selection outline overlay.
        /// Called by CardChoicePanel to indicate whether this card is currently selected.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_selectionOutline != null)
                _selectionOutline.enabled = selected;
        }

        /// <summary>
        /// Updates this card's target layout position during hover-spread, without a full
        /// rearrange. Non-hovered cards animate smoothly to the new position via
        /// <c>Update()</c>'s lerp. Hovered cards only update their base so the hover-lift
        /// position is preserved; they'll animate to the spread base when hover ends.
        /// Called by <see cref="CardHandLayout.SetHoverSpread"/>.
        /// </summary>
        public void SetLayoutTarget(Vector3 localPos, float angleDeg)
        {
            basePosition = localPos;
            baseRotation = Quaternion.Euler(0f, 0f, -angleDeg);
            if (!isHovered && _basePositionSet)
            {
                transform.DOKill();
                transform.DOLocalMove(localPos, hoverTweenDuration).SetEase(Ease.OutQuad);
                transform
                    .DOLocalRotateQuaternion(baseRotation, hoverTweenDuration)
                    .SetEase(Ease.OutQuad);
            }
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
            // Capture the arc-tilt rotation that CardHandLayout wrote to localRotation.
            baseRotation = transform.localRotation;
            // Card is already at this position (placed by ApplyToCard). Just record the base.
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

        #endregion

        #region Pointer Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isHovered || _isDragging)
                return;
            isHovered = true;

            if (_pickerMode)
            {
                // Plain selectable: pop the scale only — no lift to the canvas bottom, no spread.
                transform.DOKill();
                transform.DOScale(baseScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
                transform.SetAsLastSibling();
                hoverEnterFeedback?.PlayFeedbacks();
                return;
            }

            transform.DOKill();
            transform.DOScale(baseScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
            transform
                .DOLocalMove(
                    new Vector3(basePosition.x, ComputeHoverY(), basePosition.z),
                    hoverTweenDuration
                )
                .SetEase(Ease.OutQuad);
            transform
                .DOLocalRotateQuaternion(Quaternion.identity, hoverTweenDuration)
                .SetEase(Ease.OutQuad);

            // Bring this card in front of all its neighbours while hovered.
            transform.SetAsLastSibling();

            hoverEnterFeedback?.PlayFeedbacks();
            HandLayout?.SetHoverSpread(true, this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHovered)
                return;
            isHovered = false;

            if (_pickerMode)
            {
                transform.DOKill();
                transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);
                hoverExitFeedback?.PlayFeedbacks();
                return;
            }

            transform.DOKill();
            transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);
            transform.DOLocalMove(basePosition, hoverTweenDuration).SetEase(Ease.OutQuad);
            transform
                .DOLocalRotateQuaternion(baseRotation, hoverTweenDuration)
                .SetEase(Ease.OutQuad);
            transform.SetSiblingIndex(baseSiblingIndex);

            hoverExitFeedback?.PlayFeedbacks();
            HandLayout?.SetHoverSpread(false);
        }

        /// <summary>
        /// Returns the local Y position that places this card's bottom edge just above the
        /// canvas bottom boundary by <see cref="hoverEdgePadding"/> pixels.
        /// Uniform across all cards regardless of arc position.
        /// Falls back to <see cref="basePosition"/>.y when no Canvas ancestor is found.
        /// </summary>
        private float ComputeHoverY()
        {
            Canvas canvas = ParentCanvas;
            if (canvas == null)
                return basePosition.y;

            var canvasRect = canvas.GetComponent<RectTransform>();
            var myRect = GetComponent<RectTransform>();

            // Bottom edge of the canvas converted to this card's parent local space
            Vector3 bottomWorld = canvas.transform.TransformPoint(
                new Vector3(0f, canvasRect.rect.yMin, 0f)
            );
            float bottomLocal = transform.parent.InverseTransformPoint(bottomWorld).y;

            // Position card centre so its bottom edge sits hoverEdgePadding above the canvas edge.
            // Multiply by baseScale.y * hoverScale because the card is scaled up on hover —
            // rect.height alone is the unscaled size and would place the card too low.
            return bottomLocal
                + myRect.rect.height * 0.5f * baseScale.y * hoverScale
                + hoverEdgePadding;
        }

        /// <summary>Picker-mode selection. Hand cards are played by dragging, so they ignore plain clicks.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_pickerMode)
                onClickCallback?.Invoke();
        }

        #endregion

        #region Drag Handlers
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_pickerMode)
                return; // pickers select by click, never drag-to-play

            if (!isPlayable)
            {
                // Visible "nope" so a rejected drag reads as unplayable, not unresponsive.
                transform.DOKill();
                transform.localRotation = baseRotation;
                transform
                    .DOPunchRotation(new Vector3(0f, 0f, 6f), 0.3f, vibrato: 8, elasticity: 1f)
                    .SetLink(gameObject);
                return;
            }

            _isDragging = true;
            _isTargeting = false;
            _dragStartScreenPos = eventData.position;
            DraggedCard = this;
            GameLogger.LogInfo("Card", $"Drag started: {cardData?.CardName}", this);

            // Clear hover state; card stays at its arc position (no re-parenting or cursor-follow).
            isHovered = false;
            transform.DOKill();
            transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);
            transform
                .DOLocalRotateQuaternion(Quaternion.identity, hoverTweenDuration)
                .SetEase(Ease.OutQuad);

            // Drag bypasses OnPointerExit so we must collapse the spread manually here.
            GetComponentInParent<CardHandLayout>()?.SetHoverSpread(false);

            // Disable raycasts so enemy slots above the hand can receive pointer events.
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
                cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            float yDelta = eventData.position.y - _dragStartScreenPos.y;

            if (yDelta >= dragUpThreshold && !_isTargeting)
            {
                GameLogger.LogInfo(
                    "Card",
                    $"Targeting mode entered (yDelta={yDelta:F0}, threshold={dragUpThreshold})",
                    this
                );
                EnterTargetingMode(eventData);
            }
            else if (yDelta < dragUpThreshold && _isTargeting)
                ExitTargetingMode();

            if (_isTargeting)
                UpdateTargetingArrow(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            _isDragging = false;

            // Snapshot TargetedSlot BEFORE ExitTargetingMode, which calls ClearTargetedSlot
            // and would wipe it before we get a chance to act on it.
            EnemySlotUI targetedSlot = EnemySlotUI.TargetedSlot;
            bool wasTargeting = _isTargeting;

            GameLogger.LogInfo(
                "Card",
                $"OnEndDrag: card={cardData?.CardName}  wasTargeting={wasTargeting}  targetedSlot={targetedSlot?.name ?? "none"}  requiresTarget={RequiresSpecificTarget()}  callback={(onClickCallback != null ? "set" : "null")}",
                this
            );

            if (_isTargeting)
                ExitTargetingMode();

            DraggedCard = null;

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
                cg.blocksRaycasts = true;

            if (wasTargeting)
            {
                if (targetedSlot != null)
                {
                    GameLogger.LogInfo(
                        "Card",
                        $"Playing '{cardData?.CardName}' on enemy slot: {targetedSlot.name}",
                        this
                    );
                    targetedSlot.PlayCardOnEnemy(this);
                }
                else if (!RequiresSpecificTarget())
                {
                    GameLogger.LogInfo(
                        "Card",
                        $"AOE/self card '{cardData?.CardName}' — playing on current focus",
                        this
                    );
                    LastPlayedRect = GetComponent<RectTransform>();
                    selectFeedback?.PlayFeedbacks();
                    onClickCallback?.Invoke();
                }
                else
                {
                    GameLogger.LogWarning(
                        "Card",
                        $"Single-target '{cardData?.CardName}' released in empty space — cancelled",
                        this
                    );
                    transform.DOKill();
                    transform.DOLocalMove(basePosition, hoverTweenDuration).SetEase(Ease.OutQuad);
                    transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);
                    transform
                        .DOLocalRotateQuaternion(baseRotation, hoverTweenDuration)
                        .SetEase(Ease.OutQuad);
                }
            }
            else
            {
                GameLogger.LogVerbose(
                    "Card",
                    $"Drag ended below threshold for '{cardData?.CardName}' — cancelled",
                    this
                );
                transform.DOKill();
                transform.DOLocalMove(basePosition, hoverTweenDuration).SetEase(Ease.OutQuad);
                transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);
                transform
                    .DOLocalRotateQuaternion(baseRotation, hoverTweenDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        #endregion

        #region Targeting API (called by EnemySlotUI)
        /// <summary>Plays the card after a successful targeting release onto an enemy slot.</summary>
        public void PlayFromDrop()
        {
            LastPlayedRect = GetComponent<RectTransform>();
            GameLogger.LogInfo(
                "Card",
                $"PlayFromDrop: '{cardData?.CardName}' (callback={(onClickCallback != null ? "set" : "null")})",
                this
            );
            selectFeedback?.PlayFeedbacks();
            onClickCallback?.Invoke();
        }

        #endregion

        #region Targeting Helpers
        private void EnterTargetingMode(PointerEventData eventData)
        {
            _isTargeting = true;
            IsTargeting = true;

            transform.DOKill();
            transform.DOScale(baseScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
            transform
                .DOLocalRotateQuaternion(Quaternion.identity, hoverTweenDuration)
                .SetEase(Ease.OutQuad);

            var rt = GetComponent<RectTransform>();
            CardTargetingArrow.Instance?.Show(rt, eventData.pressEventCamera);
            CardTargetingArrow.Instance?.UpdateEndPoint(eventData.position);
        }

        private void ExitTargetingMode()
        {
            _isTargeting = false;
            IsTargeting = false;

            transform.DOKill();
            transform.DOScale(baseScale, hoverTweenDuration).SetEase(Ease.OutQuad);

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
            if (cardData?.Effects == null)
                return false;
            foreach (var effect in cardData.Effects)
                if (effect != null && effect.Target == TargetType.Opponent)
                    return true;
            return false;
        }

        #endregion

        #region Internal Display
        private void UpdateDisplay()
        {
            if (cardData == null)
                return;

            UpdateArtwork();
            UpdateFrames();
            UpdateText();
            UpdateTypeColor();
            UpdateAffordability();
            UpdatePolicyLean();
        }

        private void UpdateArtwork()
        {
            if (artworkImage == null)
                return;

            Sprite artwork = cardData.GetArtwork();
            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
        }

        private void UpdateFrames()
        {
            if (visualSettings == null)
                return;

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
            {
                string suffix = visualSettings != null ? visualSettings.UpgradedNameSuffix : "+";
                cardNameText.text = cardData.GetDisplayName(suffix);

                if (
                    visualSettings != null
                    && cardData.IsUpgraded
                    && visualSettings.UpgradedNameColor.a > 0f
                )
                    cardNameText.color = visualSettings.UpgradedNameColor;
            }

            if (cardCostText != null)
            {
                cardCostText.text = GetCostString();

                if (
                    _isCostDiscounted
                    && visualSettings != null
                    && visualSettings.DiscountedCostColor.a > 0f
                )
                    cardCostText.color = visualSettings.DiscountedCostColor;
                else
                    cardCostText.color = _defaultCostColor;
            }

            if (cardDescriptionText != null)
                cardDescriptionText.text = cardData.Description;

            if (flavorText != null)
            {
                flavorText.text = cardData.CardType.ToString();
            }
        }

        private void UpdateTypeColor()
        {
            if (cardTypeStrip == null)
                return;
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

            // A card may carry an Energy cost, a Patronage cost (Nepo Baby), or both (double-gated).
            CardCost ap = null;
            CardCost patronage = null;
            foreach (var c in cardData.Costs)
            {
                if (c.CostType == CostType.ActionPoints)
                    ap = c;
                else if (c.CostType == CostType.Patronage)
                    patronage = c;
            }

            if (ap != null && ap.IsXCost)
                return "X";

            // Energy uses the effective cost (post Focus/Energized/Entangled); Patronage is flat.
            string apPart =
                ap != null ? (_effectiveCost <= 0 ? "0" : _effectiveCost.ToString()) : null;
            string patPart = patronage != null ? $"{patronage.CurrentAmount}P" : null;

            if (apPart != null && patPart != null)
                return $"{apPart} + {patPart}";
            if (patPart != null)
                return patPart;
            if (apPart != null)
                return apPart;

            // No Energy/Patronage cost — Free (None) or unknown.
            return cardData.Costs[0].CostType == CostType.None ? "Free" : "0";
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
                _policyLeanCenterIcon.enabled =
                    isPolicy && cardData.PolicyLean == PolicyLean.Center;
            if (_policyLeanRightIcon != null)
                _policyLeanRightIcon.enabled = isPolicy && cardData.PolicyLean == PolicyLean.Right;
        }

        private bool CanAfford(int currentAP)
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0)
                return true;
            var cost = cardData.Costs[0];
            if (cost.CostType == CostType.None)
                return true;
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
                CardType.Heckle => statusColor,
                CardType.Scandal => curseColor,
                _ => Color.white,
            };
        }

        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            transform.DOKill();
            if (DraggedCard == this)
                DraggedCard = null;
            if (_isTargeting)
            {
                IsTargeting = false;
                CardTargetingArrow.Instance?.Hide();
            }

            onClickCallback = null;
        }

        #endregion
    }
}
