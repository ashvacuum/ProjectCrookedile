using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns all card-hand display logic AND flow choreography: card button pooling,
    /// play callbacks, affordability dimming, arc-fan layout, and the event-driven
    /// play → VFX → discard-animation → refresh sequencing (moved from BattleUI).
    ///
    /// Self-subscribes to the card events it cares about; BattleUI only drives the
    /// structural state changes (RefreshNormalHand / DiscardHandAnimated / ClearHand)
    /// and supplies the battle context via <see cref="Bind"/>.
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

        // Battle context for event-driven refreshes — set via Bind (BattleUI.Initialize).
        private BattleManager _bm;
        private System.Action<CardData, int> _onCardClicked;

        /// <summary>Card button extracted from hand on CardPlayedEvent, waiting for VFX to finish before animating to discard.</summary>
        private CardButton _pendingDiscardButton;

        private readonly HashSet<CardData> _pendingDrawnCards = new HashSet<CardData>();
        private bool _handRefreshPending;

        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        #endregion

        #region Event-driven flow (play → VFX → discard → refresh)

        /// <summary>
        /// Supplies the battle context the event-driven flows need. Called by
        /// <c>BattleUI.Initialize</c>; until then the event handlers no-op.
        /// </summary>
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
            if (_bm == null)
                return;

            if (evt.IsPlayer)
            {
                // Extract the card from hand immediately so the layout closes the gap,
                // but hold it — the discard animation fires in OnCardPlayResolved so the
                // sequence is: VFX resolves → card flies to discard → new draws appear.
                GameLogger.LogInfo(
                    "Card",
                    $"Extracted '{evt.Card.CardName}' from hand — awaiting VFX complete before discard"
                );
                _pendingDiscardButton = ExtractCard(evt.Card);
            }
            else
            {
                // Enemy card — no VFX sequencing needed; refresh hand immediately.
                QueueHandRefresh();
            }
        }

        /// <summary>
        /// Fires after a played card fully resolves (VFX done or none, effects applied).
        /// Begins the discard animation; once the card lands in the discard pile the hand
        /// refreshes — so newly drawn cards appear AFTER the discard, not during VFX.
        /// </summary>
        private void OnCardPlayResolved(CardPlayResolvedEvent evt)
        {
            if (_bm == null)
                return;

            GameLogger.LogInfo(
                "Card",
                $"CardPlayResolved for '{evt.Card?.CardName}' — starting discard animation"
            );

            if (_pendingDiscardButton != null)
            {
                var btn = _pendingDiscardButton;
                _pendingDiscardButton = null;

                CardFlyAnimator.Instance?.AnimateDiscardOut(
                    btn,
                    () =>
                    {
                        GameLogger.LogInfo(
                            "Card",
                            $"Discard animation done for '{btn.CardData?.CardName}' — returning to pool and refreshing hand"
                        );
                        BattlePoolManager.Instance?.ReturnCard(btn);

                        // Refresh hand AFTER discard so any drawn cards appear once the discard lands.
                        QueueHandRefresh();
                    }
                );
            }
            else
            {
                // No card to discard (no-VFX card that was already handled, or edge case).
                GameLogger.LogWarning(
                    "Card",
                    $"CardPlayResolved for '{evt.Card?.CardName}' but no pending discard button found"
                );
                QueueHandRefresh();
            }
        }

        private void OnCardDrawn(CardDrawnEvent evt)
        {
            if (_bm == null || !evt.IsPlayer)
                return; // enemy draws don't affect the player's hand panel
            _pendingDrawnCards.Add(evt.Card); // track which cards are new this batch
            QueueHandRefresh();
        }

        /// <summary>
        /// Keeps card affordability dimming in sync the moment AP changes, instead of
        /// waiting for the post-VFX hand refresh.
        /// </summary>
        private void OnActionPointsChanged(ActionPointsChangedEvent evt)
        {
            if (!evt.IsPlayer)
                return;
            RefreshAffordability(evt.NewValue);
        }

        private void QueueHandRefresh()
        {
            if (_handRefreshPending)
                return; // refresh already scheduled — events batch into it
            _handRefreshPending = true;
            RefreshHandNextFrame().Forget();
        }

        private async UniTaskVoid RefreshHandNextFrame()
        {
            // Wait one frame so all draw events from one effect batch together.
            await UniTask.NextFrame(this.GetCancellationTokenOnDestroy());
            _handRefreshPending = false;
            var drawn =
                _pendingDrawnCards.Count > 0 ? new HashSet<CardData>(_pendingDrawnCards) : null;
            _pendingDrawnCards.Clear();

            if (_bm == null || _onCardClicked == null)
                return;

            // If cards were drawn, merge them in and animate only the new ones;
            // otherwise just reposition and re-init the existing buttons.
            if (drawn != null)
                AddDrawnCards(drawn, _bm, _onCardClicked);
            else
                RearrangeCurrentHand(_bm, _onCardClicked);
        }

        #endregion

        #region Display API

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
            bool isSilenced = bm.PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false;
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
            bool isSilenced = bm.PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false;

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
            bool isSilenced = bm.PlayerStatusEffects?.HasStatus<SilencedStatus>() ?? false;
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
        /// Flies every card in hand to the discard pile, returning each button to the pool
        /// as it lands. The active list is cleared immediately so a subsequent hand rebuild
        /// can't double-manage the departing buttons. Falls back to an instant
        /// <see cref="ClearHand"/> when no <see cref="CardFlyAnimator"/> is present.
        /// </summary>
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
                CardFlyAnimator.Instance.AnimateDiscardOut(
                    captured,
                    () =>
                    {
                        if (BattlePoolManager.Instance != null)
                            BattlePoolManager.Instance.ReturnCard(captured);
                        else
                            Destroy(captured.gameObject);
                    }
                );
            }
            _activeButtons.Clear();
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

        #endregion
    }
}
