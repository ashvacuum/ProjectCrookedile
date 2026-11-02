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
            CardChoiceRequest pendingChoice = null;
            void OnEnded(BattleEndedEvent e) => result = e.Result;
            void OnChoice(CardChoiceRequest e) => pendingChoice = e;
            bm.CardChoicePrompt = OnChoice;
            void OnActing(EnemyActingEvent e) =>
                Bump(rec.EnemyMoves, e.Move != null ? e.Move.name : "(none)");
            void OnBossActing(BossActingEvent e) => Bump(rec.EnemyMoves, "Boss: " + e.Move.name);
            void OnBossPlan(BossIntentsDeclaredEvent e) =>
                Bump(rec.BossBundles, e.Boss?.BundleName ?? "(no plan)");
            rec.Boss = setup.boss != null ? setup.boss.DisplayName : null;

            // Utility credit goes to the card being played, or to the card whose passive is firing.
            string playing = null;
            var firingPassives = new Stack<string>();
            string Source() => firingPassives.Count > 0 ? firingPassives.Peek() : playing;
            // ponytail: FIFO lots; Support from untracked sources (origin, receptive rule) isn't lotted,
            // so tracked cards can be credited with absorbs it covered. Fine for ranking cards.
            var supportLots = new List<(string card, int left)>();
            var energyLots = new List<(string card, int left)>();
            void OnPassive(CardPassiveFiringEvent e)
            {
                if (e.Starting)
                    firingPassives.Push(e.Card.CardName);
                else if (firingPassives.Count > 0)
                    firingPassives.Pop();
            }
            void OnSupport(SupportChangedEvent e)
            {
                if (e.IsDecay)
                {
                    supportLots.Clear();
                    return;
                }
                int gained = e.NewValue - e.OldValue;
                string src = Source();
                if (gained <= 0 || src == null)
                    return;
                Bump(rec.SupportGained, src, gained);
                supportLots.Add((src, gained));
            }
            void OnHit(DamageDealtEvent e)
            {
                if (!e.IsToPlayer || e.Absorbed <= 0)
                    return;
                int left = e.Absorbed;
                for (int i = 0; i < supportLots.Count && left > 0; i++)
                {
                    int take = Math.Min(supportLots[i].left, left);
                    Bump(rec.DamagePrevented, supportLots[i].card, take);
                    supportLots[i] = (supportLots[i].card, supportLots[i].left - take);
                    left -= take;
                }
                supportLots.RemoveAll(l => l.left <= 0);
            }
            void OnEnergy(ActionPointsChangedEvent e)
            {
                int gained = e.NewValue - e.OldValue;
                string src = Source();
                if (!e.IsPlayer || gained <= 0 || src == null) // the turn refill has no source
                    return;
                Bump(rec.EnergyGained, src, gained);
                energyLots.Add((src, gained));
            }
            void OnTurnEnded(TurnEndedEvent e) => energyLots.Clear(); // unspent bonus energy is gone

            // Setup credit: a Fanatic's follow pushes are shared among the plays that made it (the
            // Guilt fuel and the conversion), so setup cards score for the payoff they bought.
            // ponytail: keyed by enemy index (a mid-battle summon shifts it) and a re-Fanatic'd
            // enemy keeps its old makers unless it converts again; per-enemy ids if that matters.
            var lastRow = new Dictionary<string, int>(); // card -> its latest play's row
            var fuelRows = new Dictionary<int, List<int>>(); // enemy -> rows that Guilted it
            var fanaticRows = new Dictionary<int, List<int>>(); // enemy -> rows that made it a Fanatic
            var setupCredit = new Dictionary<int, int>(); // row -> Opinion credited
            int SourceRow()
            {
                if (firingPassives.Count > 0)
                    return lastRow.TryGetValue(firingPassives.Peek(), out int row) ? row : -1;
                // The play in progress takes the next row once it lands.
                return playing != null ? rec.Plays.Count : -1;
            }
            void OnStatus(StatusEffectAppliedEvent e)
            {
                if (e.IsToPlayer || e.Stacks <= 0)
                    return;
                int row = SourceRow();
                if (row < 0)
                    return;
                if (e.Behavior != null && e.Behavior.CountsTowardPacify)
                {
                    if (!fuelRows.TryGetValue(e.EnemyIndex, out var rows))
                        fuelRows[e.EnemyIndex] = rows = new List<int>();
                    rows.Add(row);
                }
                else if (e.Behavior is FanaticStatus && !fanaticRows.ContainsKey(e.EnemyIndex))
                    fanaticRows[e.EnemyIndex] = new List<int> { row }; // made a Fanatic without converting
            }
            void OnConverted(EnemyConvertedEvent e)
            {
                fuelRows.Remove(e.EnemyIndex, out var fuel);
                if (e.WasSilenced)
                    return;
                var rows = fuel ?? new List<int>();
                int converter = SourceRow();
                if (converter >= 0)
                    rows.Add(converter);
                fanaticRows[e.EnemyIndex] = rows.Distinct().ToList();
            }
            void OnFanaticPush(DamageDealtEvent e)
            {
                if (
                    e.IsToPlayer
                    || e.AttackerName != "Fanatic"
                    || e.Applied <= 0
                    || !fanaticRows.TryGetValue(e.SourceEnemyIndex, out var rows)
                    || rows.Count == 0
                )
                    return;
                for (int i = 0; i < rows.Count; i++)
                {
                    // Even split; the first maker takes the remainder.
                    int share = e.Applied / rows.Count + (i == 0 ? e.Applied % rows.Count : 0);
                    setupCredit[rows[i]] = (setupCredit.TryGetValue(rows[i], out int c) ? c : 0) + share;
                }
            }
            // Every drop of the meter, not DamageDealtEvent: smears go straight through the ledger
            // and never publish one, and they are exactly the hits worth counting.
            void OnOpinion(OpinionChangedEvent e)
            {
                if (e.NewValue < e.OldValue)
                    rec.DamageToPlayer += e.OldValue - e.NewValue;
                else if (firingPassives.Count > 0)
                    Bump(rec.PassiveOpinion, firingPassives.Peek(), e.NewValue - e.OldValue);
            }
            void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    rec.Errors.Add(FirstLine(message));
                else if (type == LogType.Warning)
                    rec.Warnings.Add(FirstLine(message));
            }

            EventBus.Subscribe<BattleEndedEvent>(OnEnded);
            EventBus.Subscribe<EnemyActingEvent>(OnActing);
            EventBus.Subscribe<BossActingEvent>(OnBossActing);
            EventBus.Subscribe<BossIntentsDeclaredEvent>(OnBossPlan);
            EventBus.Subscribe<OpinionChangedEvent>(OnOpinion);
            EventBus.Subscribe<CardPassiveFiringEvent>(OnPassive);
            EventBus.Subscribe<SupportChangedEvent>(OnSupport);
            EventBus.Subscribe<DamageDealtEvent>(OnHit);
            EventBus.Subscribe<ActionPointsChangedEvent>(OnEnergy);
            EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatus);
            EventBus.Subscribe<EnemyConvertedEvent>(OnConverted);
            EventBus.Subscribe<DamageDealtEvent>(OnFanaticPush);
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

                int frames = 0,
                    stuckFrames = 0,
                    playsThisTurn = 0,
                    turnSeen = -1;
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
                    else if (
                        bm.CurrentState == BattleState.PlayerTurn
                        && bm.IsPlayerTurn
                        && !bm.IsCardResolving
                    )
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

                        int pick =
                            playsThisTurn >= MaxPlaysPerTurn
                                ? -1
                                : bot.ChooseCard(bm, playable, rng);
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
                                target == null
                                || (!target.Stats.IsHostile && !target.Stats.IsReceptive)
                                    ? Mood.Neutral
                                : target.Stats.IsHostile ? Mood.Hostile
                                : Mood.Receptive;
                            int fanatics = FanaticCount(bm);
                            int roomBefore = RoomHostilitySum(bm);
                            float roomAvgBefore = RoomHostilityAvg(bm);
                            int lotsBefore = energyLots.Count; // energy banked before this card was paid for
                            playing = card.CardName;
                            Guard(rec, () => bm.RequestPlayCard(card, pick));
                            // Answer the card's own prompts (Blow the Allowance's burn pick) before
                            // measuring, or what they do lands after the delta and on no card.
                            for (int prompts = 0; pendingChoice != null && prompts < 8; prompts++)
                            {
                                var choice = pendingChoice;
                                pendingChoice = null;
                                Guard(
                                    rec,
                                    () => choice.OnConfirmed?.Invoke(bot.AnswerChoice(choice, rng))
                                );
                            }
                            playing = null;
                            if (bm.CardsPlayedThisTurn > playedBefore)
                            {
                                // Bonus energy banked earlier paid for this card: credit its source with a play.
                                int owed = cost;
                                var enabledBy = new HashSet<string>();
                                for (
                                    int i = 0;
                                    i < lotsBefore && i < energyLots.Count && owed > 0;
                                    i++
                                )
                                {
                                    int take = Math.Min(energyLots[i].left, owed);
                                    if (take <= 0)
                                        continue;
                                    enabledBy.Add(energyLots[i].card);
                                    energyLots[i] = (energyLots[i].card, energyLots[i].left - take);
                                    owed -= take;
                                }
                                energyLots.RemoveAll(l => l.left <= 0);
                                foreach (var src in enabledBy)
                                    Bump(rec.CardsEnabled, src, 1);
                            }

                            if (bm.CardsPlayedThisTurn > playedBefore)
                            {
                                playsThisTurn++;
                                rec.CardsPlayed++;
                                rec.MaxCardsOneTurn = Math.Max(rec.MaxCardsOneTurn, playsThisTurn);
                                int delta = bm.CurrentOpinion - before;
                                Bump(rec.CardPlays, card.CardName);
                                Bump(rec.CardOpinionDelta, card.CardName, delta);
                                rec.CardBestPlay[card.CardName] = Math.Max(
                                    rec.CardBestPlay.TryGetValue(card.CardName, out int best)
                                        ? best
                                        : int.MinValue,
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
                                        Fanatics = fanatics,
                                    }
                                );
                                lastRow[card.CardName] = rec.Plays.Count - 1;
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
                rec.WinLine = result.winLine;
                rec.MaxOpinion = bm.MaxOpinion;
                rec.Judgment = setup.maxTurns.HasValue && rec.Turns >= setup.maxTurns.Value;

                foreach (var (row, credit) in setupCredit)
                {
                    if (row >= rec.Plays.Count)
                        continue; // credited to a play that never landed
                    var sample = rec.Plays[row];
                    sample.SetupOpinion += credit;
                    rec.Plays[row] = sample;
                }
            }
            finally
            {
                // Runs even when the battle is abandoned mid-way (an exception further up disposes
                // this iterator), so a failed battle never leaves its listeners on the bus.
                EventBus.Unsubscribe<BattleEndedEvent>(OnEnded);
                EventBus.Unsubscribe<EnemyActingEvent>(OnActing);
                EventBus.Unsubscribe<BossActingEvent>(OnBossActing);
                EventBus.Unsubscribe<BossIntentsDeclaredEvent>(OnBossPlan);
                EventBus.Unsubscribe<OpinionChangedEvent>(OnOpinion);
                EventBus.Unsubscribe<CardPassiveFiringEvent>(OnPassive);
                EventBus.Unsubscribe<SupportChangedEvent>(OnSupport);
                EventBus.Unsubscribe<DamageDealtEvent>(OnHit);
                EventBus.Unsubscribe<ActionPointsChangedEvent>(OnEnergy);
                EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
                EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatus);
                EventBus.Unsubscribe<EnemyConvertedEvent>(OnConverted);
                EventBus.Unsubscribe<DamageDealtEvent>(OnFanaticPush);
                Application.logMessageReceived -= OnLog;

                // OnDestroy disposes the passive resolver and crowd, taking their bus subscriptions
                // with them. Wait a frame so it has happened before the next battle subscribes.
                Object.Destroy(go);
            }
            yield return null;
            onDone?.Invoke(result);
        }

        /// <summary>Living Fanatics in the room (Silenced or Stunned ones included).</summary>
        public static int FanaticCount(BattleManager bm) =>
            bm.Enemies.Count(e => !e.IsDefeated && e.StatusEffects.HasStatus<FanaticStatus>());

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
            return living.Count == 0
                ? 0f
                : (float)living.Sum(e => e.Stats.CurrentHostility) / living.Count;
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
