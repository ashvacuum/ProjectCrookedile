using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Crookedile.Data.Cards;
using Crookedile.Data;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// UI button representing a card in hand.
    /// Shows card name, cost, and description.
    /// </summary>
    public class CardButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardCostText;
        [SerializeField] private TMP_Text cardDescriptionText;
        [SerializeField] private Image cardTypeIcon;

        [Header("Card Type Colors")]
        [SerializeField] private Color diplomacyColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color hostilityColor = new Color(0.8f, 0.2f, 0.2f); // Red
        [SerializeField] private Color manipulateColor = new Color(0.6f, 0.2f, 0.8f); // Purple

        private CardData cardData;
        private int handIndex;
        private Action onClickCallback;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Initialize card button with card data and click callback.
        /// </summary>
        public void Initialize(CardData card, int index, Action onClick)
        {
            cardData = card;
            handIndex = index;
            onClickCallback = onClick;

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (cardData == null) return;

            // Card name
            if (cardNameText != null)
                cardNameText.text = cardData.GetDisplayName();

            // Card cost
            if (cardCostText != null)
            {
                string costString = GetCostString();
                cardCostText.text = costString;
            }

            // Card description (effects)
            if (cardDescriptionText != null)
            {
                cardDescriptionText.text = cardData.Description;
            }

            // Card type color
            if (cardTypeIcon != null)
            {
                cardTypeIcon.color = GetCardTypeColor(cardData.CardType);
            }
        }

        private string GetCostString()
        {
            if (cardData.Costs == null || cardData.Costs.Count == 0)
                return "0 AP";

            // Get first cost (cards only have one cost in battle)
            var cost = cardData.Costs[0];

            if (cost.CostType == CostType.None)
                return "Free";

            if (cost.IsXCost)
                return "X AP";

            return $"{cost.CurrentAmount} AP";
        }

        private Color GetCardTypeColor(CardType type)
        {
            return type switch
            {
                CardType.Diplomacy => diplomacyColor,
                CardType.Hostility => hostilityColor,
                CardType.Manipulate => manipulateColor,
                _ => Color.white
            };
        }

        private void OnButtonClicked()
        {
            onClickCallback?.Invoke();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }
    }
}
