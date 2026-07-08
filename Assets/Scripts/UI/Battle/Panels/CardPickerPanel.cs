using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// One interactive card picker for every "choose card(s)" flow: post-battle rewards
    /// (<see cref="OpenSingle"/>) and in-battle card-choice effects (<see cref="Open"/> —
    /// ChooseFromDiscard, Upgrade, Retain, ReduceCost, MakeFree, mulligan, etc.).
    ///
    /// The player taps cards to toggle selection (highlighted via <see cref="CardButton.SetSelected"/>);
    /// Confirm activates once the selection is within [minCount, maxCount]. Cancel/skip confirms
    /// with an empty selection. Buttons are rented from <see cref="BattlePoolManager"/> in
    /// picker mode, so a missing pool simply shows nothing (no fallback prefabs).
    ///
    /// Attach to a panel that is inactive by default; <c>cardContainer</c> should use a GridLayoutGroup.
    /// Opens as a router popup (dimmer + input blocking) when the router is wired.
    /// </summary>
    public class CardPickerPanel : UIView
    {
        [Header("UI References")]
        [SerializeField]
        private TMP_Text titleText;

        [Tooltip("Parent Transform where card buttons spawn. Assign a GridLayoutGroup child.")]
        [SerializeField]
        private Transform cardContainer;

        [Tooltip("Activates once the selection size is within [min, max].")]
        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private TMP_Text confirmButtonText;

        [Tooltip("Optional. Confirms with an empty selection (skip / no-op).")]
        [SerializeField]
        private Button cancelButton;

        private readonly List<CardData> _selected = new List<CardData>();
        private readonly List<CardButton> _spawned = new List<CardButton>();

        private int _minCount;
        private int _maxCount;
        private string _confirmLabel = "Confirm";
        private Action<List<CardData>> _onConfirmed;

        private void Awake()
        {
            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(OnCancelClicked);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Opens the picker. The player must select between <paramref name="minCount"/> and
        /// <paramref name="maxCount"/> cards to confirm. <paramref name="onConfirmed"/> receives
        /// the selection (empty on cancel).
        /// </summary>
        public void Open(
            string title,
            IReadOnlyList<CardData> choices,
            int minCount,
            int maxCount,
            string confirmLabel,
            Action<List<CardData>> onConfirmed
        )
        {
            _minCount = Mathf.Max(0, minCount);
            _maxCount = Mathf.Max(1, maxCount);
            _confirmLabel = string.IsNullOrEmpty(confirmLabel) ? "Confirm" : confirmLabel;
            _onConfirmed = onConfirmed;
            _selected.Clear();

            if (titleText != null)
                titleText.text = title;

            ClearCards();
            SpawnCards(choices);
            RefreshConfirmButton();
            PushAsPopup();
        }

        /// <summary>
        /// Reward convenience: pick exactly one card, single-<see cref="CardData"/> callback
        /// (null when skipped). Selecting a different card replaces the current pick.
        /// </summary>
        public void OpenSingle(
            string title,
            IReadOnlyList<CardData> offers,
            string confirmLabel,
            Action<CardData> onPick
        ) =>
            Open(
                title,
                offers,
                minCount: 1,
                maxCount: 1,
                confirmLabel,
                selection => onPick?.Invoke(selection.Count > 0 ? selection[0] : null)
            );

        /// <summary>Hides the picker and returns all spawned buttons to the pool.</summary>
        public void Close()
        {
            ClearCards();
            _onConfirmed = null;
            _selected.Clear();
            CloseAsPopup();
        }

        private void SpawnCards(IReadOnlyList<CardData> choices)
        {
            if (cardContainer == null || choices == null || BattlePoolManager.Instance == null)
                return;

            for (int i = 0; i < choices.Count; i++)
            {
                CardData card = choices[i];
                if (card == null)
                    continue;

                CardButton btn = BattlePoolManager.Instance.RentCard(card.CardType, cardContainer);
                if (btn == null)
                    continue;

                int baseCost =
                    card.Costs != null && card.Costs.Count > 0 ? card.Costs[0].BaseAmount : 0;
                CardData captured = card;
                CardButton capturedBtn = btn;

                // AP = MaxValue so cards never read as unaffordable in this picker context.
                btn.Initialize(
                    card,
                    i,
                    int.MaxValue,
                    effectiveCost: baseCost,
                    onClick: () => OnCardClicked(captured, capturedBtn)
                );
                btn.SetPickerMode(true); // plain click-to-select; no hand hover-lift / drag-to-play

                // Force layout so SetBasePosition receives the real grid slot, not (0,0).
                Canvas.ForceUpdateCanvases();
                btn.SetBasePosition(btn.transform.localPosition);

                _spawned.Add(btn);
            }
        }

        private void OnCardClicked(CardData card, CardButton btn)
        {
            if (_selected.Contains(card))
            {
                _selected.Remove(card);
                btn.SetSelected(false);
            }
            else
            {
                // Single-pick mode: a new selection replaces the old one.
                if (_maxCount == 1 && _selected.Count == 1)
                {
                    CardData prev = _selected[0];
                    _selected.Clear();
                    _spawned.Find(b => b != null && b.CardData == prev)?.SetSelected(false);
                }

                if (_selected.Count < _maxCount)
                {
                    _selected.Add(card);
                    btn.SetSelected(true);
                }
                // else at max (multi-pick) — ignore until something is deselected.
            }

            RefreshConfirmButton();
        }

        private void OnConfirmClicked()
        {
            if (_selected.Count < _minCount || _selected.Count > _maxCount)
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
            callback?.Invoke(new List<CardData>()); // empty = skip / no-op
        }

        /// <summary>
        /// Router dismissal (ESC) = Cancel: flush the buttons back to the pool and report an
        /// empty selection so the pending flow (e.g. BattleUI's card choice) always resolves.
        /// No-op after a normal Confirm/Cancel — the callback is already consumed by then.
        /// </summary>
        public override void OnPopped()
        {
            var callback = _onConfirmed;
            _onConfirmed = null;
            ClearCards();
            callback?.Invoke(new List<CardData>());
        }

        private void RefreshConfirmButton()
        {
            if (confirmButton != null)
                confirmButton.interactable =
                    _selected.Count >= _minCount && _selected.Count <= _maxCount;

            if (confirmButtonText == null)
                return;

            if (_maxCount == 1)
                confirmButtonText.text = _confirmLabel; // reward: "Take Card"
            else if (_minCount == _maxCount)
                confirmButtonText.text = $"{_confirmLabel} ({_selected.Count}/{_maxCount})";
            else
                confirmButtonText.text = $"{_confirmLabel} ({_selected.Count})";
        }

        private void ClearCards()
        {
            foreach (var btn in _spawned)
                if (btn != null)
                    BattlePoolManager.Instance?.ReturnCard(btn);
            _spawned.Clear();
            _selected.Clear();
        }
    }
}
