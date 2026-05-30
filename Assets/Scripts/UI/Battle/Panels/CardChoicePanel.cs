using System;
using System.Collections.Generic;
using Crookedile.Data;
using Crookedile.Data.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// General-purpose interactive card picker panel.
    ///
    /// Displays a scrollable grid of cards from any list; the player taps to toggle selection
    /// (highlighted via <see cref="CardButton.SetSelected"/>); once exactly
    /// <see cref="_requiredCount"/> cards are selected the Confirm button activates.
    ///
    /// Used by <see cref="BattleUI"/>'s <c>WaitingForCardChoice</c> state to implement:
    ///   • ChooseFromDiscardToHand   (choices = discard pile)
    ///   • ChooseFromDiscardToDeck   (choices = discard pile)
    ///   • UpgradeCardThisBattle     (choices = upgradeable hand cards)
    ///   • MakeCardRetain            (choices = hand)
    ///   • ReduceCardCost            (choices = hand)
    ///   • MakeCardFree              (choices = hand)
    ///
    /// Attach to a Panel that is inactive by default.
    /// <c>cardContainer</c> should use a <c>GridLayoutGroup</c>.
    /// </summary>
    public class CardChoicePanel : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text displayed at the top of the panel (e.g. 'Choose a card from Discard').")]
        [SerializeField]
        private TMP_Text titleText;

        [Tooltip(
            "Parent Transform where CardButton instances are spawned. Assign a GridLayoutGroup child."
        )]
        [SerializeField]
        private Transform cardContainer;

        [Header("Fallback Prefabs (used only when BattlePoolManager singleton is absent)")]
        [SerializeField]
        private CardButton _pressurePrefab;

        [SerializeField]
        private CardButton _rhetoricPrefab;

        [SerializeField]
        private CardButton _policyPrefab;

        [Tooltip("Activates once the player has selected exactly RequiredCount cards.")]
        [SerializeField]
        private Button confirmButton;

        [Tooltip("Text on the confirm button — updated to show 'Confirm (1/1)' etc.")]
        [SerializeField]
        private TMP_Text confirmButtonText;

        [Tooltip(
            "Optional cancel button — passes an empty list to the callback (no-op for all effects)."
        )]
        [SerializeField]
        private Button cancelButton;

        #region Per-session state
        private readonly List<CardData> _selected = new List<CardData>();
        private readonly List<CardButton> _spawnedButtons = new List<CardButton>();

        private int _requiredCount;
        private Action<List<CardData>> _onConfirmed;

        #endregion

        #region Lifecycle
        private void Awake()
        {
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Opens the panel and populates it with <paramref name="choices"/>.
        /// The player must select exactly <paramref name="requiredCount"/> cards before
        /// they can confirm. <paramref name="onConfirmed"/> is invoked with the selection
        /// (or an empty list on cancel).
        /// </summary>
        public void Open(
            string title,
            IReadOnlyList<CardData> choices,
            int requiredCount,
            Action<List<CardData>> onConfirmed
        )
        {
            _requiredCount = requiredCount;
            _onConfirmed = onConfirmed;

            if (titleText != null)
                titleText.text = title;

            ClearCards();
            SpawnCards(choices);
            RefreshConfirmButton();
            gameObject.SetActive(true);
        }

        /// <summary>Deactivates the panel and destroys all spawned card buttons.</summary>
        public void Close()
        {
            ClearCards();
            gameObject.SetActive(false);
        }

        #endregion

        #region Internal
        private void SpawnCards(IReadOnlyList<CardData> choices)
        {
            if (cardContainer == null || choices == null)
                return;
            if (
                BattlePoolManager.Instance == null
                && _pressurePrefab == null
                && _rhetoricPrefab == null
                && _policyPrefab == null
            )
                return;

            for (int i = 0; i < choices.Count; i++)
            {
                CardData card = choices[i];
                if (card == null)
                    continue;

                CardButton btn =
                    BattlePoolManager.Instance != null
                        ? BattlePoolManager.Instance.RentCard(card.CardType, cardContainer)
                        : InstantiateFallback(card.CardType);

                if (btn == null)
                    continue;

                int baseCost =
                    card.Costs != null && card.Costs.Count > 0 ? card.Costs[0].BaseAmount : 0;
                int capturedIndex = i;
                CardButton capturedBtn = btn;

                // AP = MaxValue so the card never shows as unaffordable in this picker context.
                btn.Initialize(
                    card,
                    capturedIndex,
                    int.MaxValue,
                    effectiveCost: baseCost,
                    onClick: () => OnCardClicked(card, capturedBtn)
                );

                // Force layout so SetBasePosition receives the real slot position.
                Canvas.ForceUpdateCanvases();
                btn.SetBasePosition(btn.transform.localPosition);

                _spawnedButtons.Add(btn);
            }
        }

        private CardButton InstantiateFallback(CardType cardType)
        {
            CardButton prefab = cardType switch
            {
                CardType.Rhetoric => _rhetoricPrefab,
                CardType.Policy => _policyPrefab,
                _ => _pressurePrefab,
            };
            if (prefab == null)
                return null;
            return Instantiate(prefab, cardContainer);
        }

        private void OnCardClicked(CardData card, CardButton btn)
        {
            if (_selected.Contains(card))
            {
                // Deselect
                _selected.Remove(card);
                btn.SetSelected(false);
            }
            else if (_selected.Count < _requiredCount)
            {
                // Select
                _selected.Add(card);
                btn.SetSelected(true);
            }
            // If already at requiredCount, clicking a new card does nothing until one is deselected.

            RefreshConfirmButton();
        }

        private void OnConfirmClicked()
        {
            if (_selected.Count != _requiredCount)
                return; // guard — button should be disabled anyway

            var confirmed = new List<CardData>(_selected);
            var callback = _onConfirmed;
            Close();
            callback?.Invoke(confirmed);
        }

        private void OnCancelClicked()
        {
            var callback = _onConfirmed;
            Close();
            callback?.Invoke(new List<CardData>()); // empty list = no-op
        }

        private void RefreshConfirmButton()
        {
            bool ready = _selected.Count == _requiredCount;
            if (confirmButton != null)
                confirmButton.interactable = ready;

            if (confirmButtonText != null)
                confirmButtonText.text = $"Confirm ({_selected.Count}/{_requiredCount})";
        }

        private void ClearCards()
        {
            foreach (var btn in _spawnedButtons)
            {
                if (btn == null)
                    continue;
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnCard(btn);
                else
                    Destroy(btn.gameObject);
            }

            _spawnedButtons.Clear();
            _selected.Clear();
        }
    }
}
        #endregion
