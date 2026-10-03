using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Core;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>
    /// Plays one real battle with a bot: a fresh <see cref="BattleManager"/> on its own
    /// GameObject, driven only through the same public calls the UI makes, observed only through
    /// the event bus. Battles run one at a time — the bus is global, so two at once would hear
    /// each other.
    /// </summary>
    public static class BattleHarness
    {
        /// <summary>Hard ceiling per battle; a healthy 10-turn fight takes a few hundred frames.</summary>
        private const int MaxFrames = 6000;

        /// <summary>Frames the battle may sit outside the player's turn without changing state.</summary>
        private const int SoftlockFrames = 600;

        /// <summary>Card plays in one turn before the bot stops and ends it (infinite-combo guard).</summary>
        private const int MaxPlaysPerTurn = 40;

        public static IEnumerator Run(
            BattleSetup setup,
            IBattleBot bot,
            OriginPassive[] originPassives,
            int seed,
            BattleRecord rec,
            Action<BattleResult> onDone = null
        )
        {
            UnityEngine.Random.InitState(seed);
            var rng = new System.Random(seed);
            rec.DeckSize = setup.playerDeck.Count;
            foreach (var card in setup.playerDeck)
                if (card != null)
                    rec.DeckCards.Add(card.CardName);
            rec.Enemies = string.Join(" + ", setup.enemies.Select(e => e.EnemyName));

            var go = new GameObject("PlaytestBattle");
            var bm = go.AddComponent<BattleManager>();
            bm.ConfigureForSimulation(originPassives);

            BattleResult result = null;
            CardChoiceRequestedEvent pendingChoice = null;
            void OnEnded(BattleEndedEvent e) => result = e.Result;
            void OnChoice(CardChoiceRequestedEvent e) => pendingChoice = e;
            void OnActing(EnemyActingEvent e) => Bump(rec.EnemyMoves, e.Move != null ? e.Move.name : "(none)");
            // Every drop of the meter, not DamageDealtEvent: smears go straight through the ledger
            // and never publish one, and they are exactly the hits worth counting.
            void OnOpinion(OpinionChangedEvent e)
            {
                if (e.NewValue < e.OldValue)
                    rec.DamageToPlayer += e.OldValue - e.NewValue;
            }
            void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    rec.Errors.Add(FirstLine(message));
                else if (type == LogType.Warning)
                    rec.Warnings.Add(FirstLine(message));
            }

            EventBus.Subscribe<BattleEndedEvent>(OnEnded);
            EventBus.Subscribe<CardChoiceRequestedEvent>(OnChoice);
            EventBus.Subscribe<EnemyActingEvent>(OnActing);
            EventBus.Subscribe<OpinionChangedEvent>(OnOpinion);
            Application.logMessageReceived += OnLog;

            try
            {
                bm.StartBattle(setup);
            }
            catch (Exception ex)
            {
                rec.Errors.Add("StartBattle threw: " + FirstLine(ex.Message));
            }
            try
            {
                rec.StartOpinion = bm.CurrentOpinion;

                int frames = 0, stuckFrames = 0, playsThisTurn = 0, turnSeen = -1;
                var lastState = bm.CurrentState;
                while (result == null && rec.Errors.Count < 20)
                {
                    if (++frames > MaxFrames)
                    {
                        rec.TimedOut = true;
                        break;
                    }

                    if (bm.CurrentState != lastState)
                    {
                        lastState = bm.CurrentState;
                        stuckFrames = 0;
                    }

                    if (pendingChoice != null)
                    {
                        var choice = pendingChoice;
                        pendingChoice = null;
                        Guard(rec, () => choice.OnConfirmed?.Invoke(bot.AnswerChoice(choice, rng)));
                    }
                    else if (bm.CurrentState == BattleState.PlayerTurn && bm.IsPlayerTurn && !bm.IsCardResolving)
                    {
                        stuckFrames = 0;
                        if (bm.CurrentTurn != turnSeen)
                        {
                            turnSeen = bm.CurrentTurn;
                            playsThisTurn = 0;
                            rec.Snapshots.Add(Snapshot(bm));
                        }

                        var hand = bm.PlayerDeck.Hand;
                        var playable = new List<int>();
                        for (int i = 0; i < hand.Count; i++)
                            if (bm.CanPlayCard(hand[i]))
                                playable.Add(i);

                        int pick = playsThisTurn >= MaxPlaysPerTurn ? -1 : bot.ChooseCard(bm, playable, rng);
                        if (pick < 0 || pick >= hand.Count)
                        {
                            Guard(rec, bm.RequestEndTurn);
                        }
                        else
                        {
                            var card = hand[pick];
                            int before = bm.CurrentOpinion;
                            int playedBefore = bm.CardsPlayedThisTurn;
                            int cost = bm.GetEffectiveCardCost(card);
                            Guard(rec, () => bm.SetFocusedEnemy(bot.ChooseTarget(bm, rng)));
                            var target = bm.FocusedEnemy;
                            int targetHostility = target?.Stats.CurrentHostility ?? 0;
                            var targetMood =
                                target == null || (!target.Stats.IsHostile && !target.Stats.IsReceptive) ? Mood.Neutral
                                : target.Stats.IsHostile ? Mood.Hostile
                                : Mood.Receptive;
                            int roomBefore = RoomHostilitySum(bm);
                            float roomAvgBefore = RoomHostilityAvg(bm);
                            Guard(rec, () => bm.RequestPlayCard(card, pick));

                            if (bm.CardsPlayedThisTurn > playedBefore)
                            {
                                playsThisTurn++;
                                rec.CardsPlayed++;
                                rec.MaxCardsOneTurn = Math.Max(rec.MaxCardsOneTurn, playsThisTurn);
                                int delta = bm.CurrentOpinion - before;
                                Bump(rec.CardPlays, card.CardName);
                                Bump(rec.CardOpinionDelta, card.CardName, delta);
                                rec.CardBestPlay[card.CardName] = Math.Max(
                                    rec.CardBestPlay.TryGetValue(card.CardName, out int best) ? best : int.MinValue,
                                    delta
                                );
                                rec.Plays.Add(
                                    new CardPlaySample
                                    {
                                        Card = card.CardName,
                                        Cost = cost,
                                        TargetMood = targetMood,
                                        TargetHostility = targetHostility,
                                        RoomHostility = roomAvgBefore,
                                        OpinionDelta = delta,
                                        RoomHostilityDelta = RoomHostilitySum(bm) - roomBefore,
                                        Turn = bm.PlayerTurnsElapsed + 1,
                                    }
                                );
                            }
                            else
                            {
                                rec.RejectedPlays++;
                                // A card the rules said was playable but wasn't: stop offering it this
                                // turn by ending the turn, so the bot can't spin on it forever.
                                Guard(rec, bm.RequestEndTurn);
                            }
                        }
                    }
                    else if (++stuckFrames > SoftlockFrames)
                    {
                        rec.Softlock = true;
                        break;
                    }

                    yield return null;
                }

                rec.Turns = bm.PlayerTurnsElapsed;
                rec.FinalOpinion = bm.CurrentOpinion;
                var final = Snapshot(bm);
                final.Turn = -1; // marks the end-of-battle snapshot
                rec.Snapshots.Add(final);
                if (result != null)
                    rec.Win = result.isVictory;
                rec.Judgment = setup.maxTurns.HasValue && rec.Turns >= setup.maxTurns.Value;
            }
            finally
            {
                // Runs even when the battle is abandoned mid-way (an exception further up disposes
                // this iterator), so a failed battle never leaves its listeners on the bus.
                EventBus.Unsubscribe<BattleEndedEvent>(OnEnded);
                EventBus.Unsubscribe<CardChoiceRequestedEvent>(OnChoice);
                EventBus.Unsubscribe<EnemyActingEvent>(OnActing);
                EventBus.Unsubscribe<OpinionChangedEvent>(OnOpinion);
                Application.logMessageReceived -= OnLog;

                // OnDestroy disposes the passive resolver and crowd, taking their bus subscriptions
                // with them. Wait a frame so it has happened before the next battle subscribes.
                Object.Destroy(go);
            }
            yield return null;
            onDone?.Invoke(result);
        }

        private static TurnSnapshot Snapshot(BattleManager bm)
        {
            var snap = new TurnSnapshot
            {
                Turn = bm.PlayerTurnsElapsed + 1,
                Opinion = bm.CurrentOpinion,
                MaxOpinion = bm.MaxOpinion,
                AvgHostility = RoomHostilityAvg(bm),
                Support = bm.CurrentSupport,
                Denial = bm.CurrentDenial,
                EchoChamber = bm.IsEchoChamber,
            };
            foreach (var e in bm.Enemies)
            {
                if (e.IsDefeated)
                    continue;
                if (e.Stats.IsHostile)
                    snap.Hostile++;
                else if (e.Stats.IsReceptive)
                    snap.Receptive++;
                else
                    snap.Neutral++;
            }
            return snap;
        }

        private static int RoomHostilitySum(BattleManager bm) =>
            bm.Enemies.Where(e => !e.IsDefeated).Sum(e => e.Stats.CurrentHostility);

        private static float RoomHostilityAvg(BattleManager bm)
        {
            var living = bm.Enemies.Where(e => !e.IsDefeated).ToList();
            return living.Count == 0 ? 0f : (float)living.Sum(e => e.Stats.CurrentHostility) / living.Count;
        }

        private static void Guard(BattleRecord rec, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                rec.Errors.Add($"{ex.GetType().Name}: {FirstLine(ex.Message)}");
            }
        }

        private static void Bump(Dictionary<string, int> map, string key, int by = 1) =>
            map[key] = (map.TryGetValue(key, out int n) ? n : 0) + by;

        private static string FirstLine(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "(empty)";
            int nl = s.IndexOf('\n');
            return (nl >= 0 ? s.Substring(0, nl) : s).Trim();
        }
    }
}
