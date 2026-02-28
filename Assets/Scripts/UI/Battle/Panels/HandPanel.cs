using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns all card-hand display logic: card button pooling, normal play callbacks,
    /// improvise-mode discard callbacks, affordability dimming, and arc-fan layout.
    ///
    /// Extracted from <c>BattleUI</c> so the FSM state classes can call focused, single-
    /// responsibility methods (<c>RefreshNormalHand</c>, <c>RefreshImproviseHand</c>,
    /// <c>ClearHand</c>) without BattleUI managing hand state internally.
    /// </summary>
    public class HandPanel : MonoBehaviour
    {
        [Header("Hand Container")]
        [Tooltip("Parent Transform that card buttons are placed inside.")]
        [SerializeField] private Transform  cardButtonContainer;

        [Header("Fallback Prefabs (used only when BattlePoolManager singleton is absent)")]
        [SerializeField] private CardButton _pressurePrefab;
        [SerializeField] private CardButton _rhetoricPrefab;
        [SerializeField] private CardButton _policyPrefab;

        // ── Runtime ──────────────────────────────────────────────────────────
        private List<CardButton> _activeButtons = new List<CardButton>();

        /// <summary>
        /// Rebuilds the hand using normal play-card callbacks.
        /// Called by <c>PlayerTurnBattleUIState.OnEnter</c>.
        /// </summary>
        public void RefreshNormalHand(BattleManager bm, System.Action<CardData, int> onCardClicked)
        {
            if (cardButtonContainer == null || bm?.PlayerStats == null) return;
            if (!HasPrefabSource()) return;

            ClearHand();

            if (!bm.IsPlayerTurn) return;   // safety — Idle state should call ClearHand instead

            int  currentAP  = bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = bm.PlayerStatusEffects?.HasEffect(StatusEffectType.Silenced) ?? false;
            var  hand       = bm.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData captured = hand[i];
                CardButton btn = GetOrCreate(captured.CardType);
                if (btn == null) continue;

                int  idx          = i;
                int  effectiveCost = bm.GetEffectiveCardCost(captured);
                // Silenced prevents Rhetoric cards from being played
                bool silencedBlock = isSilenced && captured.CardType == CardType.Rhetoric;
                btn.Initialize(captured, idx, currentAP, effectiveCost, silencedBlock, () => onCardClicked(captured, idx));
                btn.PlayDrawAnimation();
                _activeButtons.Add(btn);
            }

            ArrangeCards();
        }

        /// <summary>
        /// Rebuilds the hand using AddToDiscard callbacks (improvise mode).
        /// Cards already present in <paramref name="panel"/>'s discard zone are hidden.
        /// Called by <c>ImproviseBattleUIState</c> whenever the discard zone changes.
        /// </summary>
        public void RefreshImproviseHand(BattleManager bm, CardSelectionPanel panel)
        {
            if (cardButtonContainer == null || bm?.PlayerStats == null) return;
            if (!HasPrefabSource()) return;

            ClearHand();

            var  excluded   = panel.SelectedForDiscard;
            int  currentAP  = bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = bm.PlayerStatusEffects?.HasEffect(StatusEffectType.Silenced) ?? false;
            var  hand       = bm.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                if (excluded.Contains(card)) continue;   // already queued — skip

                CardButton btn = GetOrCreate(card.CardType);
                if (btn == null) continue;

                CardData captured     = card;
                int      effectiveCost = bm.GetEffectiveCardCost(captured);
                bool     silencedBlock = isSilenced && captured.CardType == CardType.Rhetoric;
                btn.Initialize(card, i, currentAP, effectiveCost, silencedBlock,
                    () => { panel.AddToDiscard(captured); RefreshImproviseHand(bm, panel); });
                _activeButtons.Add(btn);
            }

            ArrangeCards();
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
                if (btn == null) continue;
                if (BattlePoolManager.Instance != null) BattlePoolManager.Instance.ReturnCard(btn);
                else Destroy(btn.gameObject);
            }
            _activeButtons.Clear();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private bool HasPrefabSource()
        {
            if (BattlePoolManager.Instance != null) return true;
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
                CardType.Policy   => _policyPrefab,
                _                 => _pressurePrefab,
            };
            if (prefab == null) return null;
            return Instantiate(prefab, cardButtonContainer);
        }

        private void ArrangeCards()
        {
            cardButtonContainer.GetComponent<CardHandLayout>()?.ArrangeCards(_activeButtons);
        }
    }
}
