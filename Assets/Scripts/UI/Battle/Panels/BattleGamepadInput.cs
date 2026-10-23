using System.Collections.Generic;
using Crookedile.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Gamepad (and arrow-key) play for the battle, through the same card and slot methods the
    /// mouse uses. Left/right walks a highlight along the hand; Submit plays the focused card. A
    /// card that needs a target arms instead: left/right then cycles living enemies, Submit plays,
    /// Cancel backs out. Battle/EndTurn ends the turn. Moving the mouse hands control back to it.
    /// Reads the project-wide actions, so it needs no wiring: drop it under the battle canvas.
    /// </summary>
    [Debuggable("Input")]
    public class BattleGamepadInput : BattlePanel
    {
        [Tooltip("Stick or d-pad deflection (0-1) that counts as one step left or right.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float _stepThreshold = 0.5f;

        [Tooltip("Mouse movement in pixels per frame that hands control back to the mouse.")]
        [SerializeField]
        private float _mouseTakeoverPixels = 2f;

        private InputAction _navigate;
        private InputAction _submit;
        private InputAction _cancel;
        private InputAction _endTurn;

        private HandPanel _hand;
        private EnemyRowPanel _enemies;

        private bool _active; // true from the first gamepad press until the mouse moves
        private int _lastStep;
        private int _cardIndex;
        private CardButton _focused;
        private int _targetIndex = -1;

        private void OnEnable()
        {
            InputActionAsset actions = InputSystem.actions;
            _navigate = actions?.FindAction("UI/Navigate");
            _submit = actions?.FindAction("UI/Submit");
            _cancel = actions?.FindAction("UI/Cancel");
            _endTurn = actions?.FindAction("Battle/EndTurn");
            if (_navigate == null || _submit == null || _cancel == null || _endTurn == null)
                GameLogger.LogWarning<BattleGamepadInput>(
                    "Project-wide input actions are missing UI/Navigate, UI/Submit, UI/Cancel or "
                        + "Battle/EndTurn; gamepad battle input is off."
                );
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        private void Update()
        {
            if (_navigate == null || _submit == null || _cancel == null || _endTurn == null)
                return;

            int step = ReadStep();
            bool submit = _submit.WasPressedThisFrame();
            bool cancel = _cancel.WasPressedThisFrame();
            bool endTurn = _endTurn.WasPressedThisFrame();

            if (_active && MouseMoved())
                Release();

            if (!_active)
            {
                if (step == 0 && !submit && !cancel && !endTurn)
                    return;
                // A mouse click leaves its button selected; Submit must not also press it.
                EventSystem.current?.SetSelectedGameObject(null);
                _active = true;
            }

            if (Battle == null || !Battle.IsPlayerTurn)
            {
                FocusCard(null);
                return;
            }

            if (_hand == null)
                _hand = FindFirstObjectByType<HandPanel>();
            if (_enemies == null)
                _enemies = FindFirstObjectByType<EnemyRowPanel>();
            if (_hand == null)
                return;

            if (endTurn)
            {
                CardButton.DisarmCurrent();
                FocusCard(null);
                Battle.RequestEndTurn();
                return;
            }

            if (CardButton.ArmedCard != null && CardButton.ArmedCard == _focused)
                UpdateTargeting(step, submit, cancel);
            else
                UpdateHand(step, submit);
        }

        private void UpdateHand(int step, bool submit)
        {
            _targetIndex = -1;
            var cards = _hand.Cards;
            if (cards.Count == 0)
            {
                FocusCard(null);
                return;
            }

            _cardIndex = Mathf.Clamp(_cardIndex + step, 0, cards.Count - 1);
            CardButton card = cards[_cardIndex];
            // ponytail: a card still mid-deal is inactive and can't be focused; waits for the deal.
            FocusCard(card != null && card.gameObject.activeSelf ? card : null);

            if (submit && _focused != null && _focused.TryPlay() && CardButton.ArmedCard != _focused)
                _focused = null; // played at once; the card is leaving the hand
        }

        private void UpdateTargeting(int step, bool submit, bool cancel)
        {
            if (cancel)
            {
                CardButton.DisarmCurrent();
                _targetIndex = -1;
                return;
            }

            var slots = _enemies != null ? _enemies.Slots : null;
            if (slots == null)
                return;

            if (!IsTargetable(slots, _targetIndex))
                _targetIndex = IsTargetable(slots, Battle.FocusedEnemyIndex)
                    ? Battle.FocusedEnemyIndex
                    : NextTargetable(slots, -1, 1);
            if (step != 0)
                _targetIndex = NextTargetable(slots, _targetIndex, step);
            if (_targetIndex < 0)
                return;

            EnemySlotUI slot = slots[_targetIndex];
            if (EnemySlotUI.TargetedSlot != slot)
            {
                EnemySlotUI.ClearTargetedSlot();
                slot.SetTargeted(true);
            }

            if (submit)
            {
                _focused = null;
                _targetIndex = -1;
                slot.PlayArmedCard();
            }
        }

        private static bool IsTargetable(
            IReadOnlyList<EnemySlotUI> slots,
            int index
        ) => index >= 0 && index < slots.Count && slots[index] != null && slots[index].IsTargetable;

        /// <summary>The next targetable slot from <paramref name="from"/> in <paramref name="dir"/>, wrapping; -1 if none.</summary>
        private static int NextTargetable(
            IReadOnlyList<EnemySlotUI> slots,
            int from,
            int dir
        )
        {
            for (int n = 1; n <= slots.Count; n++)
            {
                int i = ((from + dir * n) % slots.Count + slots.Count) % slots.Count;
                if (IsTargetable(slots, i))
                    return i;
            }
            return -1;
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

        /// <summary>Drops focus and any gamepad targeting so the mouse starts clean.</summary>
        private void Release()
        {
            if (CardButton.ArmedCard != null && CardButton.ArmedCard == _focused)
                CardButton.DisarmCurrent();
            FocusCard(null);
            _targetIndex = -1;
            _active = false;
        }

        /// <summary>
        /// -1 or +1 on the frame the stick or d-pad crosses the threshold, else 0.
        /// ponytail: one step per press, no hold-to-repeat; add a repeat delay if long hands feel slow.
        /// </summary>
        private int ReadStep()
        {
            float x = _navigate.ReadValue<Vector2>().x;
            int dir =
                x > _stepThreshold ? 1
                : x < -_stepThreshold ? -1
                : 0;
            int step = dir != _lastStep ? dir : 0;
            _lastStep = dir;
            return step;
        }

        private bool MouseMoved() =>
            Mouse.current != null
            && Mouse.current.delta.ReadValue().sqrMagnitude
                > _mouseTakeoverPixels * _mouseTakeoverPixels;
    }
}
