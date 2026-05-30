using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns all card-hand display logic: card button pooling, normal play callbacks,
    /// affordability dimming, and arc-fan layout.
    ///
    /// Extracted from <c>BattleUI</c> so the FSM state classes can call focused, single-
    /// responsibility methods (<c>RefreshNormalHand</c>, <c>ClearHand</c>) without BattleUI
    /// managing hand state internally.
    /// </summary>
    public class HandPanel : MonoBehaviour
    {
        [Header("Hand Container")]
        [Tooltip("Parent Transform that card buttons are placed inside.")]
        [SerializeField]
        private Transform cardButtonContainer;

        [Header("Fallback Prefabs (used only when BattlePoolManager singleton is absent)")]
        [SerializeField]
        private CardButton _pressurePrefab;

        [SerializeField]
        private CardButton _rhetoricPrefab;

        [SerializeField]
        private CardButton _policyPrefab;

        #region Runtime
        private List<CardButton> _activeButtons = new List<CardButton>();

        /// <summary>
        /// Rebuilds the hand using normal play-card callbacks.
        /// Called by <c>BattleUI.ConfigureForBattleState</c> on <see cref="BattleState.PlayerTurn"/>.
        /// </summary>
        public void RefreshNormalHand(BattleManager bm, System.Action<CardData, int> onCardClicked)
        {
            if (cardButtonContainer == null || bm?.PlayerStats == null)
                return;
            if (!HasPrefabSource())
                return;

            ClearHand();

            if (!bm.IsPlayerTurn)
                return; // safety — Idle state should call ClearHand instead

            int currentAP = bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = bm.PlayerStatusEffects?.HasEffect(StatusEffectType.Silenced) ?? false;
            var hand = bm.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData captured = hand[i];
                CardButton btn = GetOrCreate(captured.CardType);
                if (btn == null)
                    continue;

                int idx = i;
                int effectiveCost = bm.GetEffectiveCardCost(captured);
                bool forceUnplayable =
                    captured.IsUnplayable || (isSilenced && captured.CardType == CardType.Rhetoric);
                bool isCostDiscounted = bm.PlayerDeck.GetCardCostReduction(captured) != 0;
                btn.Initialize(
                    captured,
                    idx,
                    currentAP,
                    effectiveCost,
                    forceUnplayable,
                    isCostDiscounted,
                    () => onCardClicked(captured, idx)
                );
                btn.PlayDrawAnimation();
                _activeButtons.Add(btn);
            }

            PlayCardDrawAnimation(_activeButtons);
        }

        /// <summary>
        /// Re-initialises all currently displayed buttons (updated indices, AP, costs) and
        /// repositions them in the arc. Does NOT clear, re-pool, or re-create anything.
        /// Call after <see cref="ExtractCard"/> has already removed the played card's button.
        /// </summary>
        public void RearrangeCurrentHand(
            BattleManager bm,
            System.Action<CardData, int> onCardClicked
        )
        {
            if (cardButtonContainer == null || bm?.PlayerStats == null)
                return;

            int currentAP = bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = bm.PlayerStatusEffects?.HasEffect(StatusEffectType.Silenced) ?? false;

            for (int i = 0; i < _activeButtons.Count; i++)
            {
                var btn = _activeButtons[i];
                if (btn == null)
                    continue;
                var captured = btn.CardData;
                int capturedIdx = i;
                int effectiveCost = bm.GetEffectiveCardCost(captured);
                bool forceUnplayable =
                    captured.IsUnplayable || (isSilenced && captured.CardType == CardType.Rhetoric);
                bool isCostDiscounted = bm.PlayerDeck.GetCardCostReduction(captured) != 0;
                btn.Initialize(
                    captured,
                    i,
                    currentAP,
                    effectiveCost,
                    forceUnplayable,
                    isCostDiscounted,
                    () => onCardClicked(captured, capturedIdx)
                );
            }

            ArrangeCards(animated: true);
        }

        /// <summary>
        /// Removes and returns the <see cref="CardButton"/> for <paramref name="card"/> from the
        /// active list WITHOUT returning it to the pool. Use before a discard-fly animation.
        /// Returns <c>null</c> if no matching button is found.
        /// </summary>
        public CardButton ExtractCard(CardData card)
        {
            int idx = _activeButtons.FindIndex(b => b != null && b.CardData == card);
            if (idx < 0)
                return null;
            var btn = _activeButtons[idx];
            _activeButtons.RemoveAt(idx);
            return btn;
        }

        /// <summary>
        /// Merges newly drawn cards into the live hand without clearing existing buttons.
        /// Existing buttons are re-used and re-initialised; new buttons fly in from the deck.
        /// Uses list-based matching so duplicate <see cref="CardData"/> references are each consumed once.
        /// </summary>
        public void AddDrawnCards(
            IEnumerable<CardData> newCards,
            BattleManager bm,
            System.Action<CardData, int> onCardClicked
        )
        {
            if (cardButtonContainer == null || bm?.PlayerStats == null)
                return;
            if (!HasPrefabSource())
                return;

            int currentAP = bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = bm.PlayerStatusEffects?.HasEffect(StatusEffectType.Silenced) ?? false;
            var hand = bm.PlayerDeck.Hand;

            // List-based pool so duplicate CardData references are each matched once.
            var available = new List<CardButton>(_activeButtons);
            _activeButtons.Clear();
            var toAnimate = new List<CardButton>();

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                int effectiveCost = bm.GetEffectiveCardCost(card);
                bool forceUnplayable =
                    card.IsUnplayable || (isSilenced && card.CardType == CardType.Rhetoric);
                int capturedIdx = i;
                var captured = card;

                bool isCostDiscounted = bm.PlayerDeck.GetCardCostReduction(card) != 0;
                int existingIdx = available.FindIndex(b => b?.CardData == card);
                CardButton btn;
                if (existingIdx >= 0)
                {
                    btn = available[existingIdx];
                    available.RemoveAt(existingIdx);
                    // Existing button — refresh index, cost, callback; no animation.
                    btn.Initialize(
                        card,
                        i,
                        currentAP,
                        effectiveCost,
                        forceUnplayable,
                        isCostDiscounted,
                        () => onCardClicked(captured, capturedIdx)
                    );
                }
                else
                {
                    // Newly drawn — create a button and queue it for the fly-in.
                    btn = GetOrCreate(card.CardType);
                    if (btn == null)
                        continue;
                    btn.Initialize(
                        card,
                        i,
                        currentAP,
                        effectiveCost,
                        forceUnplayable,
                        isCostDiscounted,
                        () => onCardClicked(captured, capturedIdx)
                    );
                    btn.gameObject.SetActive(true); // always activate; StaggeredDraw hides+reveals on top
                    btn.PlayDrawAnimation();
                    toAnimate.Add(btn);
                }

                _activeButtons.Add(btn);
            }

            // Return any buttons whose cards are no longer in hand.
            foreach (var orphan in available)
            {
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnCard(orphan);
                else
                    Destroy(orphan.gameObject);
            }

            PlayCardDrawAnimation(toAnimate);
        }

        /// <summary>
        /// Updates affordability dimming on all visible card buttons without a full rebuild.
        /// Call this after AP changes mid-turn (e.g. a card was played).
        /// </summary>
        public void RefreshAffordability(int currentAP)
        {
            foreach (var btn in _activeButtons)
            {
                if (btn != null)
                    btn.RefreshVisuals(currentAP);
            }
        }

        /// <summary>
        /// Returns all active card buttons to the shared pool and clears the list.
        /// </summary>
        public void ClearHand()
        {
            foreach (var btn in _activeButtons)
            {
                if (btn == null)
                    continue;
                if (BattlePoolManager.Instance != null)
                    BattlePoolManager.Instance.ReturnCard(btn);
                else
                    Destroy(btn.gameObject);
            }
            _activeButtons.Clear();
        }

        #endregion

        #region Private helpers
        private bool HasPrefabSource()
        {
            if (BattlePoolManager.Instance != null)
                return true;
            return _pressurePrefab != null || _rhetoricPrefab != null || _policyPrefab != null;
        }

        private CardButton GetOrCreate(CardType cardType)
        {
            if (BattlePoolManager.Instance != null)
                return BattlePoolManager.Instance.RentCard(cardType, cardButtonContainer);

            // Fallback: direct instantiate (standalone testing without a pool manager)
            CardButton prefab = cardType switch
            {
                CardType.Rhetoric => _rhetoricPrefab,
                CardType.Policy => _policyPrefab,
                _ => _pressurePrefab,
            };
            if (prefab == null)
                return null;
            return Instantiate(prefab, cardButtonContainer);
        }

        private void ArrangeCards(bool animated = false)
        {
            cardButtonContainer
                .GetComponent<CardHandLayout>()
                ?.ArrangeCards(_activeButtons, animated);
        }

        private void PlayCardDrawAnimation(List<CardButton> buttons)
        {
            if (buttons == null || buttons.Count == 0)
                return;
            CardFlyAnimator.Instance?.AnimateDrawIn(buttons, cardButtonContainer);
        }
    }
}
        #endregion
