using Crookedile.Data;
using Crookedile.Data.Cards;
using UnityEngine;
using MoreMountains.Feedbacks;
using TMPro;

namespace Crookedile.UI
{
    /// <summary>
    /// Visual representation of a card in 3D space with MMFeedbacks integration
    /// </summary>
    public class Card3DView : MonoBehaviour
    {
        [Header("Card Data")]
        [SerializeField] private MeshRenderer cardRenderer;
        [SerializeField] private TextMeshPro cardNameText;
        [SerializeField] private TextMeshPro cardCostText;
        [SerializeField] private TextMeshPro cardDescriptionText;

        [Header("MMFeedbacks")]
        public MMFeedbacks drawFeedback;
        public MMFeedbacks hoverEnterFeedback;
        public MMFeedbacks hoverExitFeedback;
        public MMFeedbacks selectFeedback;
        public MMFeedbacks discardFeedback;

        private CardData cardData;
        private bool isHovered;
        private bool isSelected;

        public CardData CardData => cardData;
        public bool IsSelected => isSelected;

        public void Initialize(CardData data)
        {
            cardData = data;
            UpdateVisuals();
        }

        /// <summary>
        /// Updates all card visuals to reflect current card state.
        /// Call this when costs or other card properties change during battle.
        /// </summary>
        public void RefreshVisuals()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (cardData == null) return;

            if (cardNameText != null)
                cardNameText.text = cardData.CardName;

            if (cardCostText != null)
                cardCostText.text = GetCostDisplayText();

            if (cardDescriptionText != null)
                cardDescriptionText.text = cardData.Description;

            // TODO: Set card art on material
        }

        private string GetCostDisplayText()
        {
            var costs = cardData.GetCosts();

            // No costs = free
            if (costs == null || costs.Count == 0)
                return "0";

            // Get the first cost (should be Action Points in this system)
            var primaryCost = costs[0];

            // X-cost cards show "X"
            if (primaryCost.IsXCost)
                return "X";

            // Free cards show "0"
            if (primaryCost.CostType == CostType.None)
                return "0";

            // CurrentAmount includes all dynamic modifiers (buffs, debuffs, turn-based changes)
            // This will automatically update if the cost changes temporarily
            int currentCost = primaryCost.CurrentAmount;

            // Show 0 if cost has been reduced to 0
            return currentCost <= 0 ? "0" : currentCost.ToString();
        }

        public void OnDrawn()
        {
            drawFeedback?.PlayFeedbacks();
        }

        public void OnHoverEnter()
        {
            if (isHovered) return;
            isHovered = true;
            hoverEnterFeedback?.PlayFeedbacks();
        }

        public void OnHoverExit()
        {
            if (!isHovered) return;
            isHovered = false;
            hoverExitFeedback?.PlayFeedbacks();
        }

        public void OnSelected()
        {
            isSelected = true;
            selectFeedback?.PlayFeedbacks();
        }

        public void OnDeselected()
        {
            isSelected = false;
        }

        public void OnDiscarded()
        {
            discardFeedback?.PlayFeedbacks();
        }
    }
}
