using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using DG.Tweening;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns card-hand display and the play → VFX → discard → refresh sequencing.
    ///
    /// One rebuild path: every card event (and the PlayerTurn state change) funnels through
    /// <see cref="RequestHandRefresh"/>, which coalesces all changes from one frame into a
    /// single rebuild on the next. The hand is rebuilt as a pure function of
    /// <c>bm.PlayerDeck.Hand</c>; cards that weren't already on screen fly in, the rest just
    /// re-arrange. No incremental-merge bookkeeping, no full-vs-partial refresh race.
    /// </summary>
    public class HandPanel : MonoBehaviour
    {
        [Header("Hand Container")]
        [Tooltip("Parent Transform that card buttons are placed inside.")]
        [SerializeField]
        private Transform cardButtonContainer;

        private readonly List<CardButton> _activeButtons = new List<CardButton>();

        private BattleManager _bm;
        private System.Action<CardData, int> _onCardClicked;

        /// <summary>Card pulled from hand on CardPlayedEvent, held until VFX resolves before flying to discard.</summary>
        private CardButton _pendingDiscardButton;

        private bool _rebuildQueued;

        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        #region Setup / events

        /// <summary>Supplies the battle context the refresh needs. Called by <c>BattleUI.Initialize</c>.</summary>
        public void Bind(BattleManager bm, System.Action<CardData, int> onCardClicked)
        {
            _bm = bm;
            _onCardClicked = onCardClicked;
        }

        private void OnEnable()
        {
            Sub<CardPlayedEvent>(OnCardPlayed);
            Sub<CardPlayResolvedEvent>(OnCardPlayResolved);
            Sub<CardDrawnEvent>(OnCardDrawn);
            Sub<ActionPointsChangedEvent>(OnActionPointsChanged);
        }

        private void OnDisable()
        {
            foreach (var unsub in _eventUnsubscribers)
                unsub();
            _eventUnsubscribers.Clear();
        }

        private void Sub<T>(System.Action<T> handler)
            where T : IGameEvent
        {
            EventBus.Subscribe(handler);
            _eventUnsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            if (_bm == null || !evt.IsPlayer)
                return; // enemy plays don't touch the player hand (it's hidden on the enemy turn)

            // Pull the played card from hand and hold it; the gap closes now, the card flies
            // to discard in OnCardPlayResolved so order is: VFX → discard → new draws appear.
            _pendingDiscardButton = ExtractCard(evt.Card);
            ArrangeCards(animated: true);
        }

        /// <summary>Played card fully resolved (VFX done): fly the held card to discard, then refresh.</summary>
        private void OnCardPlayResolved(CardPlayResolvedEvent evt)
        {
            if (_bm == null)
                return;

            var btn = _pendingDiscardButton;
            _pendingDiscardButton = null;
            if (btn == null)
            {
                RequestHandRefresh();
                return;
            }

            if (CardFlyAnimator.Instance == null)
            {
                BattlePoolManager.Instance?.ReturnCard(btn);
                RequestHandRefresh();
            }
            else
            {
                CardFlyAnimator.Instance.AnimateDiscardOut(
                    btn,
                    () =>
                    {
                        BattlePoolManager.Instance?.ReturnCard(btn);
                        RequestHandRefresh();
                    }
                );
            }
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (_bm == null || !evt.IsPlayer)
                return;
            RequestHandRefresh();
        }

        private void OnActionPointsChanged(ActionPointsChangedEvent evt)
        {
            if (!evt.IsPlayer)
                return;
            foreach (var btn in _activeButtons)
                if (btn != null)
                    btn.RefreshVisuals(evt.NewValue);
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Marks the hand dirty. Every change in a frame (PlayerTurn start, a batch of draws,
        /// a resolved discard) coalesces into one rebuild in <see cref="LateUpdate"/>. Plain
        /// dirty flag — no async, so it can't get stuck if a continuation is cancelled.
        /// </summary>
        public void RequestHandRefresh() => _rebuildQueued = true;

        private void LateUpdate()
        {
            if (!_rebuildQueued)
                return;
            _rebuildQueued = false;
            RebuildHand();
        }

        /// <summary>
        /// Rebuilds the hand from <c>bm.PlayerDeck.Hand</c>. Cards not already on screen fly in;
        /// the rest re-arrange in place. Buttons are pooled, so a full clear+recreate is cheap.
        /// </summary>
        private void RebuildHand()
        {
            if (cardButtonContainer == null || _bm?.PlayerStats == null || _onCardClicked == null)
            {
                GameLogger.LogWarning<HandPanel>(
                    "RebuildHand skipped: setup incomplete — "
                        + $"container={(cardButtonContainer != null)} bm={(_bm != null)} "
                        + $"stats={(_bm?.PlayerStats != null)} clickCb={(_onCardClicked != null)}. "
                        + "Hand will not appear. Check HandPanel wiring / Bind() ordering."
                );
                return;
            }
            if (BattlePoolManager.Instance == null || !_bm.IsPlayerTurn)
                return; // not our turn → BattleUI drives ClearHand / DiscardHandAnimated instead

            ValidateLayoutContainerOnce();

            // Remember which cards were already shown so only genuinely new ones animate in.
            var previous = new HashSet<CardData>();
            foreach (var b in _activeButtons)
                if (b?.CardData != null)
                    previous.Add(b.CardData);

            ClearHand();

            int currentAP = _bm.PlayerStats.CurrentActionPoints;
            bool isSilenced = _bm.PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false;
            var hand = _bm.PlayerDeck.Hand;
            var newButtons = new List<CardButton>();

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                CardButton btn = BattlePoolManager.Instance.RentCard(card.CardType, cardButtonContainer);
                if (btn == null)
                    continue;

                int idx = i;
                var captured = card;
                btn.Initialize(
                    card,
                    i,
                    currentAP,
                    _bm.GetEffectiveCardCost(card),
                    card.IsUnplayable || (isSilenced && card.CardType == CardType.Rhetoric),
                    _bm.PlayerDeck.GetCardCostReduction(card) != 0,
                    () => _onCardClicked(captured, idx)
                );
                _activeButtons.Add(btn);
                if (!previous.Contains(card)) // set-diff; duplicate CardData refs collapse, acceptable
                    newButtons.Add(btn);
            }

            // Existing (already-shown) cards are visible at scale 1 right away; only the
            // genuinely new cards get the staggered pop-in.
            foreach (var btn in _activeButtons)
                if (!newButtons.Contains(btn))
                    EnsureVisible(btn);

            if (CardFlyAnimator.Instance != null && newButtons.Count > 0)
            {
                foreach (var btn in newButtons)
                    btn.PlayDrawAnimation();
                CardFlyAnimator.Instance.AnimateDrawIn(_activeButtons, newButtons, cardButtonContainer);
            }
            else
            {
                foreach (var btn in newButtons)
                    EnsureVisible(btn);
                ArrangeCards(animated: false);
            }
        }

        #endregion

        #region Display API (BattleUI)

        /// <summary>Flies every card in hand to the discard pile, returning each button to the pool as it lands.</summary>
        public void DiscardHandAnimated()
        {
            if (CardFlyAnimator.Instance == null)
            {
                ClearHand();
                return;
            }

            foreach (var btn in _activeButtons)
            {
                if (btn == null)
                    continue;
                var captured = btn;
                CardFlyAnimator.Instance.AnimateDiscardOut(captured, () => ReturnCard(captured));
            }
            _activeButtons.Clear();
        }

        /// <summary>Returns all active card buttons to the pool and clears the list.</summary>
        public void ClearHand()
        {
            foreach (var btn in _activeButtons)
                ReturnCard(btn);
            _activeButtons.Clear();
        }

        #endregion

        #region Helpers

        /// <summary>Removes and returns the button for <paramref name="card"/> WITHOUT pooling it (used before a discard-fly).</summary>
        private CardButton ExtractCard(CardData card)
        {
            int idx = _activeButtons.FindIndex(b => b != null && b.CardData == card);
            if (idx < 0)
                return null;
            var btn = _activeButtons[idx];
            _activeButtons.RemoveAt(idx);
            return btn;
        }

        private static void ReturnCard(CardButton btn)
        {
            if (btn != null)
                BattlePoolManager.Instance?.ReturnCard(btn);
        }

        private void ArrangeCards(bool animated)
        {
            cardButtonContainer
                .GetComponent<CardHandLayout>()
                ?.ArrangeCards(_activeButtons, animated);
        }

        private static void EnsureVisible(CardButton btn)
        {
            if (btn == null)
                return;
            btn.transform.DOKill();
            btn.gameObject.SetActive(true);
            btn.transform.localScale = Vector3.one;
        }

        // Cards are positioned by CardHandLayout (arc fan). The container must have it, and any UI
        // LayoutGroup/ContentSizeFitter would fight the arc every frame — ensure + disable, once.
        private bool _layoutChecked;

        private void ValidateLayoutContainerOnce()
        {
            if (_layoutChecked)
                return;
            _layoutChecked = true;

            if (cardButtonContainer.GetComponent<CardHandLayout>() == null)
            {
                cardButtonContainer.gameObject.AddComponent<CardHandLayout>();
                GameLogger.LogWarning<HandPanel>(
                    $"'{cardButtonContainer.name}' had no CardHandLayout — added one with default arc settings."
                );
            }

            var layoutGroup = cardButtonContainer.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layoutGroup != null)
                layoutGroup.enabled = false;

            var fitter = cardButtonContainer.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter != null)
                fitter.enabled = false;
        }

        #endregion
    }
}
