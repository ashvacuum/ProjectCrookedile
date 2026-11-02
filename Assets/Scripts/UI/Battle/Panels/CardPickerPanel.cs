using System;
using System.Collections.Generic;
using Crookedile.Data.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    ///
    /// Gamepad: left/right walks a highlight along the cards and Submit toggles the focused one
    /// (a single pick confirms at once); down moves to the Confirm/Cancel buttons, which the
    /// EventSystem then drives, and up comes back; Cancel skips when there is a cancel button.
    /// Moving the mouse hands control back, as in battle (<see cref="BattleGamepadInput"/>).
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

        [Tooltip("Stick or d-pad deflection (0-1) that counts as one gamepad step.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float _stepThreshold = 0.5f;

        [Tooltip("Mouse movement in pixels per frame that hands control back to the mouse.")]
        [SerializeField]
        private float _mouseTakeoverPixels = 2f;

        private static CardPickerPanel _open;
        private static int _closedFrame = -1;

        /// <summary>
        /// A picker is open, or closed this frame, so other gamepad input must not also act on
        /// the press that closed it.
        /// </summary>
        public static bool IsOpen => _open != null || _closedFrame == Time.frameCount;

        private InputAction _navigate;
        private InputAction _submit;
        private InputAction _cancel;
        private bool _gamepad; // true from the first gamepad press until the mouse moves
        private Vector2Int _lastStep;
        private int _focusIndex;
        private CardButton _focused;

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
            InputActionAsset actions = InputSystem.actions;
            _navigate = actions?.FindAction("UI/Navigate");
            _submit = actions?.FindAction("UI/Submit");
            _cancel = actions?.FindAction("UI/Cancel");
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_navigate == null || _submit == null || _cancel == null || _spawned.Count == 0)
                return;

            Vector2 stick = _navigate.ReadValue<Vector2>();
            var step = new Vector2Int(Step(stick.x, _lastStep.x), Step(stick.y, _lastStep.y));
            _lastStep = new Vector2Int(Dir(stick.x), Dir(stick.y));
            bool submit = _submit.WasPressedThisFrame();
            bool cancel = _cancel.WasPressedThisFrame();

            if (_gamepad && MouseMoved())
            {
                _gamepad = false;
                FocusCard(null);
            }
            if (!_gamepad)
            {
                if (step == Vector2Int.zero && !submit && !cancel)
                    return;
                // The press that wakes the gamepad only shows where focus is.
                _gamepad = true;
                EventSystem.current?.SetSelectedGameObject(null);
                FocusCard(_spawned[Mathf.Clamp(_focusIndex, 0, _spawned.Count - 1)]);
                return;
            }

            if (cancel && cancelButton != null && cancelButton.isActiveAndEnabled)
            {
                OnCancelClicked();
                return;
            }

            // On the button row the EventSystem navigates and submits; up returns to the cards.
            if (EventSystem.current?.currentSelectedGameObject != null)
            {
                if (step.y > 0)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    FocusCard(_spawned[_focusIndex]);
                }
                return;
            }

            if (step.y < 0)
            {
                Button target =
                    confirmButton != null && confirmButton.interactable ? confirmButton : cancelButton;
                if (target != null && target.isActiveAndEnabled)
                {
                    FocusCard(null);
                    EventSystem.current?.SetSelectedGameObject(target.gameObject);
                }
                return;
            }

            _focusIndex = Mathf.Clamp(_focusIndex + step.x, 0, _spawned.Count - 1);
            FocusCard(_spawned[_focusIndex]);
            if (!submit || _focused == null)
                return;
            _focused.TryPlay(); // picker mode: toggles the selection
            if (_maxCount == 1 && _selected.Count == 1)
                OnConfirmClicked();
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
            _open = this;
            _focusIndex = 0;
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
            MarkClosed();
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
            MarkClosed();
            ClearCards();
            callback?.Invoke(new List<CardData>());
        }

        private void MarkClosed()
        {
            if (_open != this)
                return;
            _open = null;
            _closedFrame = Time.frameCount;
        }

        private void FocusCard(CardButton card)
        {
            if (card == _focused)
                return;
            if (_focused != null && _focused.isActiveAndEnabled)
                _focused.Unfocus();
            _focused = card;
            if (card != null)
                card.Focus();
        }

        /// <summary>
        /// -1 or +1 on the frame the stick or d-pad crosses the threshold, else 0.
        /// ponytail: one step per press, no hold-to-repeat, same as the battle hand.
        /// </summary>
        private int Step(float axis, int last) => Dir(axis) != last ? Dir(axis) : 0;

        private int Dir(float axis) =>
            axis > _stepThreshold ? 1
            : axis < -_stepThreshold ? -1
            : 0;

        private bool MouseMoved() =>
            Mouse.current != null
            && Mouse.current.delta.ReadValue().sqrMagnitude
                > _mouseTakeoverPixels * _mouseTakeoverPixels;

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
            FocusCard(null);
            foreach (var btn in _spawned)
                if (btn != null)
                    BattlePoolManager.Instance?.ReturnCard(btn);
            _spawned.Clear();
            _selected.Clear();
        }
    }
}
