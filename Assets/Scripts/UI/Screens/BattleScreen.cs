using System;
using System.Collections.Generic;
using System.Text;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Gameplay;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Screens
{
    /// <summary>
    /// The whole battle screen, rendered wholesale from <see cref="BattleManager"/>.
    ///
    /// <para>One script, one prefab, one canvas. Any published event marks the screen dirty and the
    /// next <c>LateUpdate</c> repaints everything — so there is no incremental sync to get wrong and
    /// no per-panel subscription list that can drift. Combat data is tiny (one meter, a few enemies,
    /// a few cards); repainting all of it a handful of times per turn costs nothing.</para>
    ///
    /// <para>This screen reads the sim and never mutates it: clicks call the manager's request
    /// methods (<see cref="BattleManager.RequestPlayCard"/>, <see cref="BattleManager.RequestEndTurn"/>)
    /// and the sim decides.</para>
    ///
    /// <para>ponytail: no parallel CombatState/CardState mirror — BattleManager, DeckManager and
    /// BattleStats already hold every value below, and a copy would need the exact syncing this
    /// design exists to delete. Introduce state objects only if the sim ever needs to run headless.</para>
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        #region Inspector

        [Tooltip("Assign in-scene, or call Initialize() the way BattleTestStarter wires BattleUI.")]
        [SerializeField]
        private BattleManager _battleManager;

        [Header("Opinion Meter")]
        [Tooltip("Image with Type=Filled; fillAmount is driven from opinion / max opinion.")]
        [SerializeField]
        private Image _opinionFill;

        [SerializeField]
        private TMP_Text _opinionText;

        [SerializeField]
        private TMP_Text _shieldsText;

        [Header("Turn")]
        [SerializeField]
        private TMP_Text _turnText;

        [SerializeField]
        private TMP_Text _energyText;

        [SerializeField]
        private TMP_Text _pilesText;

        [Tooltip("Echo Chamber banner. Left empty when the rule is not active.")]
        [SerializeField]
        private TMP_Text _warningText;

        [SerializeField]
        private Button _endTurnButton;

        [Header("Rows")]
        [Tooltip("Single repeated widget for enemies and cards alike.")]
        [SerializeField]
        private BattleChip _chipPrefab;

        [SerializeField]
        private RectTransform _enemyRow;

        [SerializeField]
        private RectTransform _handRow;

        [Header("Stance Tints")]
        [SerializeField]
        private Color _hostileTint = new Color(0.65f, 0.22f, 0.22f);

        [SerializeField]
        private Color _neutralTint = new Color(0.35f, 0.35f, 0.38f);

        [SerializeField]
        private Color _receptiveTint = new Color(0.22f, 0.5f, 0.32f);

        [Header("Card Tints")]
        [SerializeField]
        private Color _playableTint = new Color(0.9f, 0.88f, 0.8f);

        [SerializeField]
        private Color _unplayableTint = new Color(0.4f, 0.4f, 0.4f);

        #endregion

        #region Runtime

        private readonly List<BattleChip> _enemyChips = new List<BattleChip>();
        private readonly List<BattleChip> _handChips = new List<BattleChip>();
        private readonly StringBuilder _detail = new StringBuilder();

        private bool _dirty = true;

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            EventBus.AnyEventPublished += MarkDirty;

            if (_endTurnButton != null)
                _endTurnButton.onClick.AddListener(OnEndTurnClicked);

            _dirty = true;
        }

        private void OnDisable()
        {
            EventBus.AnyEventPublished -= MarkDirty;

            if (_endTurnButton != null)
                _endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }

        /// <summary>Supplies the battle context when the manager is created at runtime.</summary>
        public void Initialize(BattleManager manager)
        {
            _battleManager = manager;
            _dirty = true;
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (!_dirty)
                return;

            _dirty = false;
            Render();
        }

        #endregion

        #region Render

        /// <summary>Repaints the entire screen from current sim state.</summary>
        public void Render()
        {
            if (_battleManager == null)
                return;

            RenderMeter();
            RenderTurn();
            RenderEnemies();
            RenderHand();
        }

        private void RenderMeter()
        {
            int current = _battleManager.CurrentOpinion;
            int max = Mathf.Max(1, _battleManager.MaxOpinion);

            if (_opinionFill != null)
                _opinionFill.fillAmount = (float)current / max;

            SetText(_opinionText, current + " / " + max);
            SetText(
                _shieldsText,
                "Support " + _battleManager.CurrentSupport + "   Denial " + _battleManager.CurrentDenial
            );
        }

        private void RenderTurn()
        {
            string phase = _battleManager.IsPlayerTurn ? "Your Turn" : "Opponent's Turn";
            SetText(
                _turnText,
                "Turn " + _battleManager.CurrentTurn + " / " + _battleManager.MaxTurns + "   " + phase
            );

            BattleStats stats = _battleManager.PlayerStats;
            SetText(
                _energyText,
                stats != null ? "AP " + stats.CurrentActionPoints + " / " + stats.MaxActionPoints : "AP -"
            );

            DeckManager deck = _battleManager.PlayerDeck;
            SetText(
                _pilesText,
                deck != null
                    ? "Draw " + deck.DeckCount + "   Discard " + deck.DiscardCount + "   Exhaust " + deck.ExhaustCount
                    : string.Empty
            );

            SetText(
                _warningText,
                _battleManager.IsEchoChamber ? "ECHO CHAMBER — gains halved, meter decaying" : string.Empty
            );

            if (_endTurnButton != null)
                _endTurnButton.interactable = _battleManager.IsPlayerTurn;
        }

        private void RenderEnemies()
        {
            IReadOnlyList<EnemyController> enemies = _battleManager.Enemies;
            int count = enemies != null ? enemies.Count : 0;

            EnsureChips(_enemyChips, _enemyRow, count, OnEnemyClicked);

            for (int i = 0; i < _enemyChips.Count; i++)
            {
                EnemyController enemy = i < count ? enemies[i] : null;

                _enemyChips[i].gameObject.SetActive(enemy != null);

                if (enemy == null)
                    continue;

                bool isFocused = i == _battleManager.FocusedEnemyIndex;
                string name = enemy.EnemyData != null ? enemy.EnemyData.EnemyName : "Enemy " + i;

                _enemyChips[i].Bind(
                    i,
                    isFocused ? "> " + name : name,
                    DescribeStance(enemy.Stats),
                    DescribeEnemyDetail(enemy),
                    enemy.EnemyData != null ? enemy.EnemyData.Portrait : null,
                    StanceTint(enemy.Stats),
                    true
                );
            }
        }

        private void RenderHand()
        {
            DeckManager deck = _battleManager.PlayerDeck;
            IReadOnlyList<CardData> hand = deck != null ? deck.Hand : null;
            int count = hand != null ? hand.Count : 0;

            EnsureChips(_handChips, _handRow, count, OnCardClicked);

            BattleStats stats = _battleManager.PlayerStats;
            int actionPoints = stats != null ? stats.CurrentActionPoints : 0;

            for (int i = 0; i < _handChips.Count; i++)
            {
                CardData card = i < count ? hand[i] : null;

                _handChips[i].gameObject.SetActive(card != null);

                if (card == null)
                    continue;

                int cost = _battleManager.GetEffectiveCardCost(card);

                // Affordability preview only — BattleManager.RequestPlayCard is the real gate
                // (Patronage, Silenced, Confused and the rest live there).
                bool isPlayable =
                    _battleManager.IsPlayerTurn && !card.IsUnplayable && cost <= actionPoints;

                _handChips[i].Bind(
                    i,
                    card.CardName,
                    cost.ToString(),
                    card.Description,
                    card.Artwork,
                    isPlayable ? _playableTint : _unplayableTint,
                    isPlayable
                );
            }
        }

        /// <summary>
        /// Grows the pool to <paramref name="required"/>. Never shrinks — surplus chips are
        /// deactivated by the render loop and reused on the next bigger board.
        /// </summary>
        private void EnsureChips(
            List<BattleChip> chips,
            RectTransform parent,
            int required,
            Action<int> onClicked
        )
        {
            if (_chipPrefab == null || parent == null)
                return;

            while (chips.Count < required)
            {
                BattleChip chip = Instantiate(_chipPrefab, parent);
                chip.Initialize(onClicked);
                chips.Add(chip);
            }
        }

        #endregion

        #region Display helpers

        private string DescribeStance(BattleStats stats)
        {
            if (stats == null)
                return string.Empty;

            string label = "Neutral";

            if (stats.IsHostile)
                label = "Hostile";
            else if (stats.IsReceptive)
                label = "Receptive";

            return label + " " + stats.CurrentHostility.ToString("+0;-0;0");
        }

        private string DescribeEnemyDetail(EnemyController enemy)
        {
            _detail.Clear();

            EnemyMoveData intent = enemy.CurrentIntent;

            if (intent == null)
                _detail.Append("—");
            else
                _detail.Append(
                    !string.IsNullOrEmpty(intent.IntentDescription)
                        ? intent.IntentDescription
                        : intent.MoveName
                );

            IReadOnlyList<StatusEffect> statuses =
                enemy.StatusEffects != null ? enemy.StatusEffects.ActiveEffects : null;

            if (statuses != null)
            {
                for (int i = 0; i < statuses.Count; i++)
                {
                    _detail.Append('\n').Append(statuses[i].DisplayName).Append(' ');
                    _detail.Append(statuses[i].Stacks);
                }
            }

            return _detail.ToString();
        }

        private Color StanceTint(BattleStats stats)
        {
            if (stats == null)
                return _neutralTint;

            if (stats.IsHostile)
                return _hostileTint;

            if (stats.IsReceptive)
                return _receptiveTint;

            return _neutralTint;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;

            label.text = value ?? string.Empty;
        }

        #endregion

        #region Input

        private void OnEnemyClicked(int index)
        {
            if (_battleManager == null)
                return;

            _battleManager.SetFocusedEnemy(index);
        }

        private void OnCardClicked(int handIndex)
        {
            if (_battleManager == null || !_battleManager.IsPlayerTurn)
                return;

            CardData card =
                _battleManager.PlayerDeck != null
                    ? _battleManager.PlayerDeck.GetCardInHand(handIndex)
                    : null;

            if (card == null)
                return;

            // Targeted cards resolve against the focused enemy — click the enemy first, then the
            // card. ponytail: drag-to-target belongs on a real card widget, not here.
            _battleManager.RequestPlayCard(card, handIndex);
        }

        private void OnEndTurnClicked()
        {
            if (_battleManager != null && _battleManager.IsPlayerTurn)
                _battleManager.RequestEndTurn();
        }

        #endregion
    }
}
