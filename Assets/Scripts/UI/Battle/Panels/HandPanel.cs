using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;

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
        [Tooltip("Prefab with a CardButton component — used to seed the object pool.")]
        [SerializeField] private GameObject cardButtonPrefab;

        // ── Runtime ──────────────────────────────────────────────────────────
        private List<CardButton>       _activeButtons = new List<CardButton>();
        private ObjectPool<CardButton> _cardPool;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (cardButtonPrefab != null)
            {
                var prefabComp = cardButtonPrefab.GetComponent<CardButton>();
                if (prefabComp != null)
                    _cardPool = new ObjectPool<CardButton>(prefabComp, initialSize: 7, parent: cardButtonContainer);
            }
        }

        private void OnDestroy()
        {
            _cardPool?.Clear();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the hand using normal play-card callbacks.
        /// Called by <c>PlayerTurnBattleUIState.OnEnter</c>.
        /// </summary>
        public void RefreshNormalHand(BattleManager bm, System.Action<CardData, int> onCardClicked)
        {
            if (cardButtonContainer == null || cardButtonPrefab == null || bm?.PlayerStats == null) return;

            ClearHand();

            if (!bm.IsPlayerTurn) return;   // safety — Idle state should call ClearHand instead

            int currentAP = bm.PlayerStats.CurrentActionPoints;
            var hand      = bm.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardButton btn = GetOrCreate();
                if (btn == null) continue;

                int       idx      = i;
                CardData  captured = hand[i];
                btn.Initialize(captured, idx, currentAP, () => onCardClicked(captured, idx));
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
            if (cardButtonContainer == null || cardButtonPrefab == null || bm?.PlayerStats == null) return;

            ClearHand();

            var excluded  = panel.SelectedForDiscard;
            int currentAP = bm.PlayerStats.CurrentActionPoints;
            var hand      = bm.PlayerDeck.Hand;

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                if (excluded.Contains(card)) continue;   // already queued — skip

                CardButton btn = GetOrCreate();
                if (btn == null) continue;

                CardData captured = card;
                btn.Initialize(card, i, currentAP,
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
        /// Returns all active card buttons to the pool and clears the list.
        /// </summary>
        public void ClearHand()
        {
            foreach (var btn in _activeButtons)
            {
                if (btn == null) continue;
                if (_cardPool != null) _cardPool.Return(btn);
                else Destroy(btn.gameObject);
            }
            _activeButtons.Clear();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private CardButton GetOrCreate()
        {
            if (_cardPool != null) return _cardPool.Get();
            if (cardButtonPrefab == null) return null;
            return Instantiate(cardButtonPrefab, cardButtonContainer).GetComponent<CardButton>();
        }

        private void ArrangeCards()
        {
            cardButtonContainer.GetComponent<CardHandLayout>()?.ArrangeCards(_activeButtons);
        }
    }
}
