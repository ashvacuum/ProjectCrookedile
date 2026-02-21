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
        IPointerClickHandler
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
        [SerializeField] private Color diplomacyColor = new Color(0.2f, 0.8f, 0.2f);   // Green
        [SerializeField] private Color hostilityColor = new Color(0.8f, 0.2f, 0.2f);   // Red
        [SerializeField] private Color manipulateColor = new Color(0.6f, 0.2f, 0.8f);  // Purple
        [SerializeField] private Color unaffordableColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);

        // ─── Hover / Scale ────────────────────────────────────────────────────────

        [Header("Hover Behaviour")]
        [Tooltip("How much to scale up on hover (1.1 = 10% bigger)")]
        [SerializeField] private float hoverScale = 1.12f;
        [Tooltip("How fast the scale animates")]
        [SerializeField] private float hoverLerpSpeed = 12f;
        [Tooltip("Extra vertical lift on hover (in pixels)")]
        [SerializeField] private float hoverLiftPixels = 20f;

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

        // ─── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            baseScale    = transform.localScale;
            basePosition = transform.localPosition;
            targetScale  = baseScale;
            targetPosition = basePosition;
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
            if (isHovered) return;
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

            hoverExitFeedback?.PlayFeedbacks();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isPlayable) return;
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
                CardType.Diplomacy  => diplomacyColor,
                CardType.Hostility  => hostilityColor,
                CardType.Manipulate => manipulateColor,
                _                   => Color.white
            };
        }

        // ─── Cleanup ──────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            onClickCallback = null;
        }
    }
}
