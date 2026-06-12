using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Gameplay.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Crookedile.UI.Battle
{
    /// <summary>
    /// Owns battle log display AND narration: subscribes to battle events itself and
    /// formats its own copy, so BattleUI doesn't route log strings. Also exposes
    /// <see cref="AddEntry"/> for input-driven lines (e.g. "Player ended turn").
    ///
    /// Notification-only consumer — never calls back into gameplay.
    /// </summary>
    public class BattleLogPanel : MonoBehaviour
    {
        [Header("Log Display")]
        [SerializeField]
        private TMP_Text battleLogText;

        [SerializeField]
        private ScrollRect battleLogScrollRect;

        [SerializeField]
        private int maxLogLines = 20;

        private readonly List<string> _lines = new List<string>();

        // Optional — only needed for entries that look up names (Turncoat). Set via Bind.
        private BattleManager _battleManager;

        /// <summary>Unsubscribe actions collected by <see cref="Sub{T}"/>; run on disable.</summary>
        private readonly List<System.Action> _eventUnsubscribers = new List<System.Action>();

        /// <summary>Gives the log access to the battle for name lookups. Called by BattleUI.Initialize.</summary>
        public void Bind(BattleManager manager) => _battleManager = manager;

        #region Event subscription

        private void OnEnable() => SubscribeToEvents();

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

        private void SubscribeToEvents()
        {
            Sub<BattleStartedEvent>(_ => AddEntry("=== Battle Started ==="));
            Sub<TurnStartedEvent>(evt =>
                AddEntry($"--- Turn {evt.TurnNumber}: {(evt.IsPlayerTurn ? "Player" : "Opponent")} ---")
            );
            Sub<CardPlayedEvent>(evt =>
                AddEntry($"{(evt.IsPlayer ? "Player" : "Opponent")} played: {evt.Card.CardName}")
            );
            Sub<EnemyIntentDeclaredEvent>(evt =>
            {
                if (evt.Move != null)
                    AddEntry($"Enemy [{evt.EnemyIndex}] intends: {evt.Move.IntentDescription}");
            });
            Sub<EnemyDefeatedEvent>(evt => AddEntry($"{evt.EnemyName} defeated!"));
            Sub<EnemySummonedEvent>(evt => AddEntry($"{evt.EnemyData.EnemyName} was summoned!"));
            Sub<BattleEndedEvent>(evt =>
                AddEntry(evt.Result.isVictory ? "=== VICTORY ===" : "=== DEFEAT ===")
            );
            Sub<DamageDealtEvent>(evt =>
            {
                if (!evt.IsToPlayer)
                    return;
                string suffix = evt.Absorbed > 0 ? $" ({evt.Absorbed} absorbed by Support)" : "";
                AddEntry($"{evt.AttackerName} dealt {evt.Applied} damage{suffix}");
            });
            Sub<JudgmentEvent>(evt =>
                AddEntry(
                    $"=== JUDGMENT: Opinion {evt.FinalOpinion} / {evt.Threshold * 2} — {(evt.IsVictory ? "VICTORY" : "DEFEAT")} ==="
                )
            );
            Sub<EnemySkippedTurnEvent>(evt => AddEntry($"{evt.EnemyName} held back this turn."));
            Sub<EchoChamberChangedEvent>(evt =>
                AddEntry(
                    evt.Active
                        ? "Echo chamber! The room agrees with you — opinion gains are halved and your lead will bleed. Provoke someone."
                        : "Echo chamber broken — the room has a dissenter again."
                )
            );
            Sub<EnemyTurncoatEvent>(evt =>
            {
                string name =
                    _battleManager != null
                    && evt.EnemyIndex >= 0
                    && evt.EnemyIndex < _battleManager.Enemies.Count
                        ? _battleManager.Enemies[evt.EnemyIndex].EnemyData.EnemyName
                        : "An ally";
                AddEntry($"{name} turned on you! They'll hit harder for a turn.");
            });
        }

        #endregion

        #region Public API
        /// <summary>
        /// Appends a new line to the battle log and auto-scrolls to the bottom.
        /// Older lines are trimmed when the buffer exceeds <c>maxLogLines</c>.
        /// </summary>
        public void AddEntry(string message)
        {
            _lines.Add(message);

            if (_lines.Count > maxLogLines)
                _lines.RemoveAt(0);

            Flush();
        }

        /// <summary>Clears all log entries.</summary>
        public void Clear()
        {
            _lines.Clear();
            if (battleLogText != null)
                battleLogText.text = string.Empty;
        }

        #endregion

        #region Private
        private void Flush()
        {
            if (battleLogText == null)
                return;

            battleLogText.text = string.Join("\n", _lines);

            if (battleLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                battleLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        #endregion
    }
}
