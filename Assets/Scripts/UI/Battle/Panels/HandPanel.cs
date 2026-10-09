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
    public class HandPanel : BattlePanel
    {
        [Header("Hand Container")]
        [Tooltip("Parent Transform that card buttons are placed inside.")]
        [SerializeField]
        private Transform cardButtonContainer;

        private readonly List<CardButton> _activeButtons = new List<CardButton>();

        /// <summary>Card pulled from hand on CardPlayedEvent, held until VFX resolves before flying to discard.</summary>
        private CardButton _pendingDiscardButton;

        private bool _rebuildQueued;

        #region Setup / events

        private void OnEnable()
        {
            On<CardPlayedEvent>(OnCardPlayed);
            On<CardPlayResolvedEvent>(OnCardPlayResolved);
            On<CardDrawnEvent>(OnCardDrawn);
            On<ActionPointsChangedEvent>(OnActionPointsChanged);
        }

        private void OnCardClicked(CardData card, int handIndex)
        {
            if (Battle != null && Battle.IsPlayerTurn)
                Battle.RequestPlayCard(card, handIndex);
            else
                GameLogger.LogWarning<HandPanel>(
                    $"Card play blocked: '{card?.CardName}' outside the player turn"
                );
        }

        private void OnCardPlayed(CardPlayedEvent evt)
        {
            if (Battle == null || !evt.IsPlayer)
                return; // enemy plays don't touch the player hand (it's hidden on the enemy turn)

            // Pull the played card from hand and hold it; the gap closes now, the card flies
            // to discard in OnCardPlayResolved so order is: VFX → discard → new draws appear.
            // A deal still in progress lands first so its loop can't fight the close-up.
            CardFlyAnimator.Instance?.FinishDraw();
            _pendingDiscardButton = ExtractCard(evt.Card);
            ArrangeCards(animated: true);
        }

        /// <summary>Played card fully resolved (VFX done): fly the held card to discard, then refresh.</summary>
        private void OnCardPlayResolved(CardPlayResolvedEvent evt)
        {
            if (Battle == null)
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
            if (Battle == null || !evt.IsPlayer)
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
            RefreshHighlights();
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
            RefreshHighlights();
        }

        /// <summary>
        /// Rims playable cards whose condition is met. Runs after each hand rebuild (turn-start
        /// draw, every resolved play) and on Energy changes; nothing changes in between.
        /// </summary>
        private void RefreshHighlights()
        {
            if (Battle == null)
                return;
            foreach (var btn in _activeButtons)
                if (btn != null)
                    btn.SetConditionHighlight(
                        btn.IsPlayable && Battle.IsCardConditionMet(btn.CardData)
                    );
        }

        /// <summary>
        /// Rebuilds the hand from <c>bm.PlayerDeck.Hand</c>. Cards not already on screen fly in;
        /// the rest re-arrange in place. Buttons are pooled, so a full clear+recreate is cheap.
        /// </summary>
        private void RebuildHand()
        {
            if (cardButtonContainer == null || Battle?.PlayerStats == null)
            {
                GameLogger.LogWarning<HandPanel>(
                    "RebuildHand skipped: setup incomplete — "
                        + $"container={(cardButtonContainer != null)} bm={(Battle != null)} "
                        + $"stats={(Battle?.PlayerStats != null)}. "
                        + "Hand will not appear. Check the HandPanel container and that a BattleManager is in the scene."
                );
                return;
            }
            if (BattlePoolManager.Instance == null || !Battle.IsPlayerTurn)
                return; // not our turn → BattleUI drives ClearHand / DiscardHandAnimated instead

            ValidateLayoutContainerOnce();

            // Remember where each on-screen card is, one entry per copy, so rebuilt buttons pick
            // up exactly where the old ones were and glide on from there. Only cards not in this
            // snapshot animate in as draws. A card still waiting in the deck mid-draw is inactive,
            // so it isn't recorded and deals in again.
            var previous = new Dictionary<CardData, Queue<(Vector3 pos, Quaternion rot)>>();
            foreach (var b in _activeButtons)
            {
                if (b?.CardData == null || !b.gameObject.activeSelf)
                    continue;
                if (!previous.TryGetValue(b.CardData, out var copies))
                    previous[b.CardData] = copies = new Queue<(Vector3, Quaternion)>();
                copies.Enqueue((b.transform.localPosition, b.transform.localRotation));
            }

            ClearHand();

            int currentAP = Battle.PlayerStats.CurrentActionPoints;
            bool isSilenced = Battle.PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false;
            var hand = Battle.PlayerDeck.Hand;
            var newButtons = new List<CardButton>();

            for (int i = 0; i < hand.Count; i++)
            {
                CardData card = hand[i];
                CardButton btn = BattlePoolManager.Instance.RentCard(
                    card.CardType,
                    cardButtonContainer
                );
                if (btn == null)
                    continue;

                int idx = i;
                var captured = card;
                btn.Initialize(
                    card,
                    i,
                    currentAP,
                    Battle.GetEffectiveCardCost(card),
                    card.IsUnplayable || (isSilenced && card.CardType == CardType.Rhetoric),
                    Battle.PlayerDeck.GetCardCostReduction(card) != 0,
                    () => OnCardClicked(captured, idx)
                );
                _activeButtons.Add(btn);
                if (previous.TryGetValue(card, out var copies) && copies.Count > 0)
                {
                    var (pos, rot) = copies.Dequeue();
                    btn.transform.localPosition = pos;
                    btn.transform.localRotation = rot;
                }
                else
                {
                    btn.SetCardBackForOrigin(Battle.PlayerOrigin);
                    newButtons.Add(btn);
                }
            }

            if (CardFlyAnimator.Instance != null && newButtons.Count > 0)
            {
                CardFlyAnimator.Instance.AnimateDrawIn(
                    _activeButtons,
                    newButtons,
                    cardButtonContainer.GetComponent<CardHandLayout>()
                );
            }
            else
            {
                // No deal to animate: the deck counter still needs to hear these cards left it.
                foreach (var _ in newButtons)
                    EventBus.Publish(new DrawnCardLaunchedEvent());
                ArrangeCards(animated: newButtons.Count == 0);
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

            CardFlyAnimator.Instance.CancelDraw();
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
            // Stop any staggered draw first so it can't re-touch buttons we're about to pool.
            CardFlyAnimator.Instance?.CancelDraw();

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
