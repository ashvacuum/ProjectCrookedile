using System.Collections.Generic;
using Crookedile.Core;
using Crookedile.Data.Cards;
using Crookedile.Gameplay.Battle;
using Crookedile.Utilities;
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
    /// Outcomes are grouped under their cause: everything that happens between a card's
    /// <see cref="CardPlayedEvent"/> and <see cref="CardPlayResolvedEvent"/> (or while an enemy
    /// acts, until the next enemy or turn) is indented beneath that line. Outcomes are read from
    /// the events systems already publish, so a new effect is logged as soon as it moves
    /// Opinion, draws, applies a status, etc.
    ///
    /// Notification-only consumer — never calls back into gameplay.
    /// </summary>
    public class BattleLogPanel : BattlePanel
    {
        [Header("Log Display")]
        [SerializeField]
        private TMP_Text battleLogText;

        [SerializeField]
        private ScrollRect battleLogScrollRect;

        [Tooltip("Oldest lines are dropped past this count.")]
        [SerializeField]
        private int maxLogLines = 150;

        private readonly List<string> _lines = new List<string>();

        private enum Group
        {
            None,
            Card,
            Enemy,
        }

        /// <summary>What the lines being logged right now belong to; non-None lines are indented.</summary>
        private Group _group;

        /// <summary>The card whose play is resolving, while <see cref="_group"/> is Card.</summary>
        private CardData _groupCard;

        /// <summary>Index in <see cref="_lines"/> of the draw line consecutive draws merge into, or -1.</summary>
        private int _drawLine = -1;
        private readonly List<string> _drawnNames = new List<string>();

        #region Event subscription

        private void OnEnable() => SubscribeToEvents();

        private void SubscribeToEvents()
        {
            // --- Structure: battle, turns, and the causes outcomes group under ---
            On<BattleStartedEvent>(_ =>
            {
                EndGroup();
                AddEntry("=== Battle Started ===");
            });
            On<TurnStartedEvent>(evt =>
            {
                EndGroup();
                AddEntry($"--- Turn {evt.TurnNumber}: {(evt.IsPlayerTurn ? "Player" : "Opponent")} ---");
            });
            On<TurnEndedEvent>(_ => EndGroup());
            On<CardPlayedEvent>(evt =>
            {
                EndGroup();
                AddEntry($"{(evt.IsPlayer ? "You" : "Opponent")} played <b>{evt.Card.CardName}</b>");
                _group = Group.Card;
                _groupCard = evt.Card;
            });
            On<CardPlayResolvedEvent>(_ =>
            {
                if (_group == Group.Card)
                    EndGroup();
            });
            On<EnemyActingEvent>(evt =>
            {
                EndGroup();
                string move = evt.Move != null && !string.IsNullOrEmpty(evt.Move.MoveName)
                    ? $" uses <b>{evt.Move.MoveName}</b>"
                    : " acts";
                AddEntry($"{EnemyName(evt.EnemyIndex)}{move}");
                _group = Group.Enemy;
            });

            // --- Opinion ---
            On<BossActingEvent>(evt =>
            {
                EndGroup();
                AddEntry(
                    $"{evt.Boss.Data.DisplayName} uses <b>{evt.Move.MoveName}</b> ({evt.IntentIndex + 1}/{evt.Boss.Intents.Count})"
                );
                _group = Group.Enemy;
            });
            On<DamageDealtEvent>(evt =>
            {
                if (evt.IsToPlayer)
                {
                    string absorbed =
                        evt.Absorbed > 0 ? $" ({evt.Absorbed} absorbed by Support)" : "";
                    AddOutcome($"{evt.AttackerName} hit you: -{evt.Applied} Opinion{absorbed}");
                }
                else
                {
                    string absorbed =
                        evt.Absorbed > 0 ? $" ({evt.Absorbed} blocked by Denial)" : "";
                    AddOutcome(
                        $"Pressured {EnemyName(evt.TargetEnemyIndex)}: +{evt.Applied} Opinion{absorbed}"
                    );
                }
            });
            On<HealingAppliedEvent>(evt =>
            {
                if (evt.IsToPlayer)
                    AddOutcome($"+{evt.Amount} Opinion");
            });

            // --- Cards ---
            On<CardDrawnEvent>(evt =>
            {
                if (evt.IsPlayer && evt.Card != null)
                    AddDraw(evt.Card.CardName);
            });
            On<DeckReshuffledEvent>(evt =>
            {
                if (evt.IsPlayer)
                    AddOutcome($"Shuffled {evt.Count} cards from discard into the draw pile");
            });
            On<CardDiscardedEvent>(evt =>
            {
                // Only effect-driven discards: the played card's own trip to discard and the
                // end-of-turn hand discard would drown out everything else.
                if (evt.IsPlayer && _group == Group.Card && evt.Card != _groupCard)
                    AddOutcome($"Discarded {evt.Card.CardName}");
            });
            On<CardExhaustedEvent>(evt =>
            {
                if (evt.IsPlayer)
                    AddOutcome($"Exhausted {evt.Card.CardName}");
            });
            On<CardGrantedEvent>(evt =>
            {
                if (!evt.IsPlayer)
                    return;
                string copies = evt.Count > 1 ? $"{evt.Count}x " : "";
                string pile = evt.ToDiscard ? "discard" : "draw";
                AddOutcome($"Added {copies}{evt.Card.CardName} to your {pile} pile");
            });
            On<CardRecoveredEvent>(evt =>
            {
                if (evt.IsPlayer)
                    AddOutcome($"Returned {evt.Card.CardName} to hand");
            });
            On<CardRetainedEvent>(evt =>
            {
                if (evt.IsPlayer)
                    AddOutcome($"{evt.Card.CardName} will stay in hand");
            });
            On<CardUpgradedEvent>(evt =>
            {
                if (evt.IsPlayer)
                    AddOutcome($"Upgraded {evt.OldCard.CardName} to {evt.NewCard.CardName}");
            });

            // --- Statuses and resources ---
            On<StatusEffectAppliedEvent>(evt =>
            {
                if (evt.Stacks == 0 || evt.Behavior == null)
                    return;
                string who = evt.IsToPlayer ? "You" : EnemyName(evt.EnemyIndex);
                string verb = evt.Stacks > 0 ? "gained" : "lost";
                AddOutcome($"{who} {verb} {Mathf.Abs(evt.Stacks)} {evt.Behavior.DisplayName}");
            });
            On<ActionPointsChangedEvent>(evt =>
            {
                // Gains from effects only: spends and the turn-start refill are routine.
                if (evt.IsPlayer && _group == Group.Card && evt.NewValue > evt.OldValue)
                    AddOutcome($"+{evt.NewValue - evt.OldValue} Energy");
            });
            On<SupportChangedEvent>(evt =>
            {
                // Losses show on the hit that caused them; decay is routine.
                if (!evt.IsDecay && evt.NewValue > evt.OldValue)
                    AddOutcome($"+{evt.NewValue - evt.OldValue} Support");
            });
            On<DenialChangedEvent>(evt =>
            {
                if (!evt.IsDecay && evt.NewValue > evt.OldValue)
                    AddOutcome($"The room gained {evt.NewValue - evt.OldValue} Denial");
            });
            On<HostilityChangedEvent>(evt =>
            {
                if (evt.IsPlayer || evt.NewValue == evt.OldValue)
                    return;
                AddOutcome(
                    $"{EnemyName(evt.EnemyIndex)} hostility {evt.OldValue} → {evt.NewValue} ({Mood(evt.NewValue)})"
                );
            });

            // --- Enemies and the room ---
            On<EnemyIntentDeclaredEvent>(evt =>
            {
                if (evt.Move != null)
                    AddEntry($"{EnemyName(evt.EnemyIndex)} intends: {evt.Move.IntentDescription}");
            });
            On<EnemyConvertedEvent>(evt =>
                AddOutcome(
                    evt.WasSilenced
                        ? $"{EnemyName(evt.EnemyIndex)} is Hardened and was silenced instead"
                        : $"{EnemyName(evt.EnemyIndex)} converted! +{evt.OpinionBurst} Opinion"
                )
            );
            On<EnemyTurncoatEvent>(evt =>
                AddOutcome($"{EnemyName(evt.EnemyIndex)} turned on you! They'll hit harder for a turn.")
            );
            On<EnemyDefeatedEvent>(evt => AddOutcome($"{evt.EnemyName} defeated!"));
            On<EnemySummonedEvent>(evt => AddOutcome($"{evt.EnemyData.EnemyName} was summoned!"));
            On<EnemySkippedTurnEvent>(evt => AddEntry($"{evt.EnemyName} held back this turn."));
            On<EchoChamberChangedEvent>(evt =>
                AddOutcome(
                    evt.Active
                        ? "Echo chamber! The room agrees with you — opinion gains are halved and your lead will bleed. Provoke someone."
                        : "Echo chamber broken — the room has a dissenter again."
                )
            );
            On<JudgmentEvent>(evt =>
            {
                EndGroup();
                AddEntry(
                    $"=== JUDGMENT: Opinion {evt.FinalOpinion} / {evt.Threshold * 2} — {(evt.IsVictory ? "VICTORY" : "DEFEAT")} ==="
                );
            });
            On<BattleEndedEvent>(evt =>
            {
                EndGroup();
                AddEntry(evt.Result.isVictory ? "=== VICTORY ===" : "=== DEFEAT ===");
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
            _drawLine = -1;
            Append(message);
        }

        /// <summary>Clears all log entries.</summary>
        public void Clear()
        {
            _lines.Clear();
            _drawLine = -1;
            EndGroup();
            if (battleLogText != null)
                battleLogText.text = string.Empty;
        }

        #endregion

        #region Debug overlay (IMGUI)
        // ponytail: reuses the same _lines buffer the uGUI panel already builds; no separate
        // event subscription. Toggle from the dev console: `battlelog`.
        private bool _showDebugOverlay;
        private Vector2 _debugScroll;

        [CheatCommand("battlelog", "Toggle the IMGUI battle log overlay", Category = "Debug")]
        private void ToggleDebugOverlay() => _showDebugOverlay = !_showDebugOverlay;

        private void OnGUI()
        {
            if (!_showDebugOverlay)
                return;

            var rect = new Rect(20f, Screen.height - 320f, 520f, 300f);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Battle Log", GUI.skin.box);
            _debugScroll = GUILayout.BeginScrollView(_debugScroll);
            for (int i = 0; i < _lines.Count; i++)
                GUILayout.Label(_lines[i]);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Private
        /// <summary>A line caused by whatever is resolving now — indented when inside a group.</summary>
        private void AddOutcome(string message) =>
            AddEntry(_group == Group.None ? message : Indent(message));

        /// <summary>Consecutive draws merge into one line: "Drew 3 cards: A, B, C".</summary>
        private void AddDraw(string cardName)
        {
            if (_drawLine < 0)
                _drawnNames.Clear();
            _drawnNames.Add(cardName);
            string text =
                _drawnNames.Count == 1
                    ? $"Drew {_drawnNames[0]}"
                    : $"Drew {_drawnNames.Count} cards: {string.Join(", ", _drawnNames)}";
            if (_group != Group.None)
                text = Indent(text);

            if (_drawLine >= 0)
            {
                _lines[_drawLine] = text;
                Flush();
            }
            else
            {
                Append(text);
                _drawLine = _lines.Count - 1;
            }
        }

        private void Append(string message)
        {
            _lines.Add(message);
            if (_lines.Count > maxLogLines)
            {
                _lines.RemoveAt(0);
                if (_drawLine >= 0)
                    _drawLine--;
            }
            Flush();
        }

        private void EndGroup()
        {
            _group = Group.None;
            _groupCard = null;
            _drawLine = -1;
        }

        private static string Indent(string message) => "    - " + message;

        private string EnemyName(int index) =>
            Battle != null && index >= 0 && index < Battle.Enemies.Count
                ? Battle.Enemies[index].EnemyData.EnemyName
                : "An enemy";

        private static string Mood(int hostility) =>
            hostility > 0 ? "hostile"
            : hostility < 0 ? "receptive"
            : "neutral";

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
