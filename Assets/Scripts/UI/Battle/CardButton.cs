using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using MoreMountains.Feedbacks;
using Crookedile.Data.Cards;
using Crookedile.Data;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// 2D UI card component. Displays a card in the player's hand using Canvas/UI elements.
    /// Handles artwork, frames, cost display, hover state, and click-to-play.
    /// Replaces the old Card3DView + CardButton split. This is now the single card view component.
    /// </summary>
    public class CardButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        // ─── UI Structure References ──────────────────────────────────────────────

        [Header("Card Art")]
        [Tooltip("Shows the card's artwork sprite from CardData")]
        [SerializeField] private Image artworkImage;

        [Tooltip("Border frame image — set by card type (Diplomacy/Hostility/Manipulate)")]
        [SerializeField] private Image typeFrameImage;

        [Tooltip("Rarity overlay image — set by card rarity (Basic/Enhanced/Rare)")]
        [SerializeField] private Image rarityOverlayImage;

        [Header("Card Text")]
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardCostText;
        [SerializeField] private TMP_Text cardDescriptionText;
        [SerializeField] private TMP_Text flavorText;

        [Header("Card Type Color Strip")]
        [Tooltip("Optional colored strip at top/bottom indicating card type")]
        [SerializeField] private Image cardTypeStrip;

        // ─── Visual Settings ──────────────────────────────────────────────────────

        [Header("Visual Settings")]
        [Tooltip("Optional: reference to CardVisualSettings for frame/rarity sprites")]
        [SerializeField] private CardVisualSettings visualSettings;

        [Header("Card Type Colors")]
        [SerializeField] private Color pressureColor = new Color(0.2f, 0.8f, 0.2f);   // Green
        [SerializeField] private Color rhetoricColor = new Color(0.8f, 0.2f, 0.2f);   // Red
        [SerializeField] private Color policyColor   = new Color(0.2f, 0.5f, 0.9f);   // Blue
        [SerializeField] private Color unaffordableColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        // ─── Hover / Scale ────────────────────────────────────────────────────────

        [Header("Hover Behaviour")]
        [Tooltip("How much to scale up on hover (1.1 = 10% bigger)")]
        [SerializeField] private float hoverScale = 1.12f;
        [Tooltip("How fast the scale animates")]
        [SerializeField] private float hoverLerpSpeed = 12f;
        [Tooltip("Extra vertical lift on hover (in pixels)")]
        [SerializeField] private float hoverLiftPixels = 20f;

        // ─── Drag to Play ─────────────────────────────────────────────────────────

        [Header("Drag to Play")]
        [Tooltip("How many pixels upward the card must travel to trigger a play when not dropped on an enemy.")]
        [SerializeField] private float dragUpThreshold = 100f;

        // ─── MMFeedbacks ──────────────────────────────────────────────────────────

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

        // ─── Selection (CardSelectionPanel) ───────────────────────────────────────

        [Header("Selection")]
        [Tooltip("Overlay Image shown when this card is selected in a CardSelectionPanel. Assign a colored border child.")]
        [SerializeField] private Image _selectionOutline;

        // ─── Runtime State ────────────────────────────────────────────────────────

        private CardData cardData;
        private int handIndex;
        private Action onClickCallback;

        private bool isHovered;
        private bool isPlayable;

        private Vector3 baseScale;
        private Vector3 basePosition;
        private Vector3 targetScale;
        private Vector3 targetPosition;
        private int baseSiblingIndex;

        // Drag state
        private bool      _isDragging;
        private Vector2   _dragStartScreenPos;
        private bool      _dropWasHandled;
        private Transform _originalParent;
        private Canvas    _rootCanvas;

        /// <summary>The card currently being dragged, or null. Used by EnemySlotUI to show drop highlights.</summary>
        public static CardButton DraggedCard { get; private set; }

        /// <summary>Whether this card can currently be played (enough AP).</summary>
        public bool IsPlayable => isPlayable;

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            baseScale      = transform.localScale;
            basePosition   = transform.localPosition;
            targetScale    = baseScale;
            targetPosition = basePosition;

            Canvas c = GetComponentInParent<Canvas>();
            _rootCanvas = c != null ? c.rootCanvas : null;
        }

        private void Update()
        {
            // Smooth hover scale & lift
            transform.localScale    = Vector3.Lerp(transform.localScale,    targetScale,    Time.deltaTime * hoverLerpSpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * hoverLerpSpeed);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Initialize the card with data, current AP (to set affordability), and a click callback.
        /// </summary>
        public void Initialize(CardData card, int index, int currentActionPoints, Action onClick)
        {
            cardData        = card;
            handIndex       = index;
            onClickCallback = onClick;
            isPlayable      = CanAfford(currentActionPoints);

            baseScale      = transform.localScale;
            basePosition   = transform.localPosition;
            targetScale    = baseScale;
            targetPosition = basePosition;

            UpdateDisplay();
        }

        /// <summary>
        /// Refreshes all visuals — call when AP changes mid-turn so affordability updates.
        /// </summary>
        public void RefreshVisuals(int currentActionPoints)
        {
            if (cardData == null) return;
            isPlayable = CanAfford(currentActionPoints);
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
            basePosition = position;

            // Only update the target if we're not mid-hover; otherwise the card
            // would snap back to the new base while the player is still mousing over it.
            if (!isHovered)
                targetPosition = position;
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

            targetScale    = baseScale * hoverScale;
            targetPosition = basePosition + new Vector3(0f, hoverLiftPixels, 0f);

            // Bring this card in front of all its neighbours while hovered.
            // CardHandLayout.SetSiblingOrder() restores natural z-order on the next hand rebuild.
            transform.SetAsLastSibling();

            hoverEnterFeedback?.PlayFeedbacks();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isHovered) return;
            isHovered = false;

            targetScale    = baseScale;
            targetPosition = basePosition;
            transform.SetSiblingIndex(baseSiblingIndex);

            hoverExitFeedback?.PlayFeedbacks();
        }

        // ─── Drag Handlers ────────────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isPlayable) return;

            _isDragging         = true;
            _dropWasHandled     = false;
            _dragStartScreenPos = eventData.position;
            DraggedCard         = this;

            // Clear any active hover state so the card returns to its rest scale
            isHovered    = false;
            targetScale  = baseScale;

            // Re-parent to the root canvas so the card can travel anywhere on screen
            _originalParent = transform.parent;
            if (_rootCanvas != null)
            {
                transform.SetParent(_rootCanvas.transform, worldPositionStays: true);
                transform.SetAsLastSibling();
            }

            // Disable raycasts on the card so enemy slots behind it can receive pointer events
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            RectTransform canvasRt = _rootCanvas != null
                ? (RectTransform)_rootCanvas.transform
                : (RectTransform)transform.parent;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, eventData.position, eventData.pressEventCamera, out Vector2 local))
                transform.localPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            DraggedCard = null;

            // Restore raycast blocking
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;

            // Re-parent back to the card hand container
            transform.SetParent(_originalParent, worldPositionStays: true);
            transform.SetSiblingIndex(baseSiblingIndex);

            if (_dropWasHandled) return; // EnemySlotUI handled everything; BattleUI will remove card

            float yDelta = eventData.position.y - _dragStartScreenPos.y;
            if (yDelta >= dragUpThreshold)
            {
                selectFeedback?.PlayFeedbacks();
                onClickCallback?.Invoke();   // upward swipe → play
            }
            else
            {
                // Drag cancelled — snap card back to its resting position in the hand
                targetPosition = basePosition;
                targetScale    = baseScale;
            }
        }

        // ─── Drop API (called by EnemySlotUI) ────────────────────────────────────

        /// <summary>Called by EnemySlotUI.OnDrop before invoking PlayFromDrop, to suppress the upward-swipe check in OnEndDrag.</summary>
        public void NotifyDropHandled() => _dropWasHandled = true;

        /// <summary>Plays the card from a successful drop onto an enemy slot.</summary>
        public void PlayFromDrop()
        {
            selectFeedback?.PlayFeedbacks();
            onClickCallback?.Invoke();
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
        }

        private void UpdateArtwork()
        {
            if (artworkImage == null) return;

            Sprite artwork = cardData.GetArtwork();
            artworkImage.sprite  = artwork;
            artworkImage.enabled = artwork != null;
        }

        private void UpdateFrames()
        {
            if (visualSettings == null) return;

            // Type frame
            if (typeFrameImage != null)
            {
                Sprite frame = visualSettings.GetFrameForType(cardData.CardType);
                typeFrameImage.sprite  = frame;
                typeFrameImage.enabled = frame != null;
            }

            // Rarity overlay
            if (rarityOverlayImage != null)
            {
                Sprite rarity = visualSettings.GetFrameForRarity(cardData.Rarity);
                rarityOverlayImage.sprite  = rarity;
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
                flavorText.text    = cardData.FlavorText;
                flavorText.enabled = !string.IsNullOrEmpty(cardData.FlavorText);
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
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group != null)
                group.alpha = isPlayable ? 1f : 0.5f;
        }

        private string GetCostString()
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0)
                return "0";

            var cost = cardData.Costs[0];

            if (cost.CostType == CostType.None) return "Free";
            if (cost.IsXCost)                   return "X";

            int amount = cost.CurrentAmount;
            return amount <= 0 ? "0" : amount.ToString();
        }

        private bool CanAfford(int currentAP)
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0) return true;
            var cost = cardData.Costs[0];
            if (cost.CostType == CostType.None) return true;
            return currentAP >= cost.CurrentAmount;
        }

        private Color GetCardTypeColor(CardType type)
        {
            return type switch
            {
                CardType.Pressure => pressureColor,
                CardType.Rhetoric => rhetoricColor,
                CardType.Policy   => policyColor,
                _                 => Color.white
            };
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (DraggedCard == this) DraggedCard = null;
            onClickCallback = null;
        }
    }
}
