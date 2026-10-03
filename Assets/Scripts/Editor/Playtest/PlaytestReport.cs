using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Crookedile.Data.Campaign;
using Crookedile.Data.Enemy;
using UnityEditor;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>
    /// Turns bot runs into battles.csv, campaigns.csv and report.md. The report ranks what is
    /// most likely broken; every threshold here is a tripwire for a human to look at, not a verdict.
    /// </summary>
    public static class PlaytestReport
    {
        public static string Write(
            string folder,
            List<BattleRecord> battles,
            List<CampaignRecord> campaigns,
            EncounterPoolData pool,
            TimeSpan elapsed,
            string notes = null
        )
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "battles.csv"), BattlesCsv(battles));
            File.WriteAllText(Path.Combine(folder, "campaigns.csv"), CampaignsCsv(campaigns));
            File.WriteAllText(Path.Combine(folder, "turns.csv"), TurnsCsv(battles));
            File.WriteAllText(Path.Combine(folder, "card_plays.csv"), PlaysCsv(battles));
            string mdPath = Path.Combine(folder, "report.md");
            File.WriteAllText(mdPath, Markdown(battles, campaigns, pool, elapsed, notes));
            return mdPath;
        }

        #region CSV

        private static string BattlesCsv(List<BattleRecord> rows)
        {
            var sb = new StringBuilder(
                "context,bot,origin,encounter,enemies,seed,deck,win,judgment,timed_out,softlock,turns,"
                    + "start_opinion,final_opinion,cards_played,max_cards_one_turn,rejected_plays,damage_to_player,errors,warnings\n"
            );
            foreach (var r in rows)
                sb.AppendLine(
                    string.Join(
                        ",",
                        Q(r.Context), Q(r.Bot), Q(r.Origin), Q(r.Encounter), Q(r.Enemies), r.Seed, r.DeckSize,
                        B(r.Win), B(r.Judgment), B(r.TimedOut), B(r.Softlock), r.Turns, r.StartOpinion,
                        r.FinalOpinion, r.CardsPlayed, r.MaxCardsOneTurn, r.RejectedPlays, r.DamageToPlayer,
                        r.Errors.Count, r.Warnings.Count
                    )
                );
            return sb.ToString();
        }

        private static string CampaignsCsv(List<CampaignRecord> rows)
        {
            var sb = new StringBuilder(
                "id,bot,origin,seed,end,day_reached,defeated_by,funds,credibility,deck,allies,battles,battles_won,events,idle_days,errors,route\n"
            );
            foreach (var r in rows)
                sb.AppendLine(
                    string.Join(
                        ",",
                        Q(r.Id), Q(r.Bot), Q(r.Origin), r.Seed, Q(r.EndReason), r.DayReached, Q(r.DefeatedBy),
                        r.Funds, r.Credibility, r.DeckSize, r.Allies, r.Battles, r.BattlesWon, r.Events,
                        r.DaysWithNothingToDo, r.Errors.Count, Q(string.Join(" > ", r.Visited))
                    )
                );
            return sb.ToString();
        }

        private static string TurnsCsv(List<BattleRecord> rows)
        {
            var sb = new StringBuilder(
                "context,bot,origin,encounter,seed,turn,opinion,max_opinion,avg_hostility,hostile,neutral,receptive,support,denial,echo_chamber\n"
            );
            foreach (var r in rows)
            foreach (var s in r.Snapshots)
                sb.AppendLine(
                    string.Join(
                        ",",
                        Q(r.Context), Q(r.Bot), Q(r.Origin), Q(r.Encounter), r.Seed, s.Turn < 0 ? "end" : s.Turn.ToString(),
                        s.Opinion, s.MaxOpinion, F(s.AvgHostility), s.Hostile, s.Neutral, s.Receptive, s.Support,
                        s.Denial, B(s.EchoChamber)
                    )
                );
            return sb.ToString();
        }

        private static string PlaysCsv(List<BattleRecord> rows)
        {
            var sb = new StringBuilder(
                "context,bot,origin,encounter,seed,turn,card,cost,target_mood,target_hostility,room_hostility,opinion_delta,room_hostility_delta,won_battle\n"
            );
            foreach (var r in rows)
            foreach (var p in r.Plays)
                sb.AppendLine(
                    string.Join(
                        ",",
                        Q(r.Context), Q(r.Bot), Q(r.Origin), Q(r.Encounter), r.Seed, p.Turn, Q(p.Card), p.Cost,
                        p.TargetMood, p.TargetHostility, F(p.RoomHostility), p.OpinionDelta, p.RoomHostilityDelta, B(r.Win)
                    )
                );
            return sb.ToString();
        }

        private static string F(float f) => f.ToString("0.00", CultureInfo.InvariantCulture);

        private static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        private static string B(bool b) => b ? "1" : "0";

        #endregion

        #region Markdown

        private static string Markdown(
            List<BattleRecord> battles,
            List<CampaignRecord> campaigns,
            EncounterPoolData pool,
            TimeSpan elapsed,
            string notes
        )
        {
            var md = new StringBuilder();
            md.AppendLine("# Playtest bot report");
            md.AppendLine();
            md.AppendLine(
                $"{DateTime.Now:yyyy-MM-dd HH:mm} · {battles.Count} battles · {campaigns.Count} campaign runs · took {elapsed:hh\\:mm\\:ss}"
            );
            md.AppendLine();
            md.AppendLine(
                "Heuristic bots, not optimal play: a flag means *look at this*, not *this is wrong*. "
                    + "Raw rows are in `battles.csv`, `campaigns.csv`, `turns.csv` and `card_plays.csv`."
            );
            if (!string.IsNullOrEmpty(notes))
                md.AppendLine().AppendLine(notes);

            Bugs(md, battles, campaigns);
            Encounters(md, battles);
            RoomOverTime(md, battles);
            CardsByHostility(md, battles);
            Cards(md, battles);
            Enemies(md, battles);
            if (campaigns.Count > 0)
                Campaigns(md, campaigns, pool);
            return md.ToString();
        }

        private static void Bugs(StringBuilder md, List<BattleRecord> battles, List<CampaignRecord> campaigns)
        {
            md.AppendLine().AppendLine("## Bugs and hangs");
            var errors = battles
                .SelectMany(b => b.Errors.Select(e => (e, where: $"{b.Encounter} ({b.Bot}, {b.Origin})")))
                .Concat(campaigns.SelectMany(c => c.Errors.Select(e => (e, where: $"run {c.Id}"))))
                .GroupBy(x => x.e)
                .OrderByDescending(g => g.Count())
                .ToList();
            if (errors.Count == 0)
                md.AppendLine("No errors or exceptions logged.");
            else
            {
                md.AppendLine("| Count | Error | First seen in |").AppendLine("|---:|---|---|");
                foreach (var g in errors.Take(25))
                    md.AppendLine($"| {g.Count()} | {Cell(g.Key)} | {Cell(g.First().where)} |");
            }

            var hangs = battles.Where(b => b.Softlock || b.TimedOut).ToList();
            md.AppendLine();
            md.AppendLine(
                hangs.Count == 0
                    ? "No softlocks or timeouts."
                    : $"**{hangs.Count} battles hung** (softlock = stopped advancing outside your turn; timeout = frame budget):"
            );
            foreach (var g in hangs.GroupBy(h => (h.Encounter, h.Softlock)).Take(15))
                md.AppendLine($"- {g.Key.Encounter}: {g.Count()}× {(g.Key.Softlock ? "softlock" : "timeout")}");

            var rejected = battles.Where(b => b.RejectedPlays > 0).ToList();
            if (rejected.Count > 0)
                md.AppendLine()
                    .AppendLine(
                        $"**{rejected.Sum(b => b.RejectedPlays)} rejected plays** in {rejected.Count} battles — "
                            + "the rules said a card was playable, then refused it."
                    );

            var combos = battles.Where(b => b.MaxCardsOneTurn >= 8).OrderByDescending(b => b.MaxCardsOneTurn).ToList();
            if (combos.Count > 0)
            {
                md.AppendLine().AppendLine("**Long turns** (8+ cards in one turn — possible infinite or free-card loop):");
                foreach (var b in combos.Take(10))
                    md.AppendLine($"- {b.MaxCardsOneTurn} cards: {b.Origin} vs {b.Encounter} ({b.Bot}, seed {b.Seed})");
            }

            var warnings = battles.SelectMany(b => b.Warnings).GroupBy(w => w).OrderByDescending(g => g.Count()).Take(10).ToList();
            if (warnings.Count > 0)
            {
                md.AppendLine().AppendLine("<details><summary>Most frequent warnings</summary>").AppendLine();
                foreach (var g in warnings)
                    md.AppendLine($"- {g.Count()}× {Cell(g.Key)}");
                md.AppendLine().AppendLine("</details>");
            }
        }

        private static void Encounters(StringBuilder md, List<BattleRecord> all)
        {
            var battles = all.Where(b => b.Context == "battle").ToList();
            if (battles.Count == 0)
                return;
            md.AppendLine().AppendLine("## Encounter difficulty");
            md.AppendLine(
                "Win rate per bot, standalone battles with starter decks. **Too easy** = Random wins 60%+. "
                    + "**Too hard** = no bot reaches 20%. **Always goes the distance** = 90%+ end on the turn limit."
            );
            var bots = battles.Select(b => b.Bot).Distinct().OrderBy(b => b).ToList();
            md.AppendLine()
                .AppendLine($"| Encounter | {string.Join(" | ", bots)} | Avg turns | Flag |")
                .AppendLine($"|---|{string.Concat(bots.Select(_ => "---:|"))}---:|---|");
            foreach (var g in battles.GroupBy(b => b.Encounter).OrderBy(g => g.Key))
            {
                var rates = bots.ToDictionary(bot => bot, bot => Rate(g.Where(b => b.Bot == bot)));
                var flags = new List<string>();
                if (rates.TryGetValue("Random", out float r) && r >= 0.6f)
                    flags.Add("too easy");
                if (rates.Values.All(v => v < 0.2f))
                    flags.Add("too hard");
                if (g.Count(b => b.Judgment) >= 0.9f * g.Count())
                    flags.Add("always goes the distance");
                md.AppendLine(
                    $"| {g.Key} | {string.Join(" | ", bots.Select(bot => Pct(rates[bot])))} | "
                        + $"{g.Average(b => b.Turns):0.0} | {string.Join(", ", flags)} |"
                );
            }

            md.AppendLine().AppendLine("Win rate by bot (all encounters and origins):").AppendLine();
            foreach (var g in battles.GroupBy(b => b.Bot).OrderByDescending(g => Rate(g)))
                md.AppendLine($"- **{g.Key}**: {Pct(Rate(g))} over {g.Count()} battles");

            md.AppendLine().AppendLine("Win rate by origin (all bots):").AppendLine();
            foreach (var g in battles.GroupBy(b => b.Origin))
                md.AppendLine($"- **{g.Key}**: {Pct(Rate(g))} over {g.Count()} battles, starter deck {g.First().DeckSize} cards");
        }

        private static void RoomOverTime(StringBuilder md, List<BattleRecord> all)
        {
            var battles = all.Where(b => b.Snapshots.Count > 0).ToList();
            if (battles.Count == 0)
                return;
            md.AppendLine().AppendLine("## Opinion and hostility over a battle");
            md.AppendLine(
                "Averaged at the start of each player turn across every battle. Opinion is % of the meter; "
                    + "room hostility is the average over living enemies (above 0 = hostile, below 0 = receptive). "
                    + "A healthy fight climbs while the room is managed — not a flat line, and not a cliff."
            );
            md.AppendLine()
                .AppendLine("| Turn | Battles | Opinion | Room hostility | Hostile / Neutral / Receptive | Echo chamber | Support | Denial |")
                .AppendLine("|---:|---:|---:|---:|---|---:|---:|---:|");
            foreach (var g in battles.SelectMany(b => b.Snapshots.Where(s => s.Turn > 0)).GroupBy(s => s.Turn).OrderBy(g => g.Key).Take(12))
                md.AppendLine(
                    $"| {g.Key} | {g.Count()} | {Pct(g.Average(OpinionPct))} | {g.Average(s => s.AvgHostility):+0.0;-0.0;0} | "
                        + $"{g.Average(s => s.Hostile):0.0} / {g.Average(s => s.Neutral):0.0} / {g.Average(s => s.Receptive):0.0} | "
                        + $"{Pct((float)g.Count(s => s.EchoChamber) / g.Count())} | {g.Average(s => s.Support):0} | {g.Average(s => s.Denial):0} |"
                );

            var standalone = battles.Where(b => b.Context == "battle").ToList();
            if (standalone.Count == 0)
                return;
            md.AppendLine()
                .AppendLine("Per encounter (standalone battles). **Opinion falls** = ends lower than it started; "
                    + "**room never calms** = ends at least as hostile as turn 1; **echo trap** = 30%+ of turns in the echo chamber.")
                .AppendLine()
                .AppendLine("| Encounter | Opinion T1 → T3 → end | Room hostility T1 → end | Echo turns | Flag |")
                .AppendLine("|---|---|---|---:|---|");
            foreach (var g in standalone.GroupBy(b => b.Encounter).OrderBy(g => g.Key))
            {
                float o1 = AvgAt(g, 1, OpinionPct), o3 = AvgAt(g, 3, OpinionPct), oEnd = AvgAt(g, -1, OpinionPct);
                float h1 = AvgAt(g, 1, s => s.AvgHostility), hEnd = AvgAt(g, -1, s => s.AvgHostility);
                var turns = g.SelectMany(b => b.Snapshots.Where(s => s.Turn > 0)).ToList();
                float echo = turns.Count == 0 ? 0f : (float)turns.Count(s => s.EchoChamber) / turns.Count;
                var flags = new List<string>();
                if (oEnd < o1)
                    flags.Add("opinion falls");
                if (h1 > 0 && hEnd >= h1)
                    flags.Add("room never calms");
                if (echo >= 0.3f)
                    flags.Add("echo trap");
                md.AppendLine(
                    $"| {g.Key} | {Pct(o1)} → {Pct(o3)} → {Pct(oEnd)} | {h1:+0.0;-0.0;0} → {hEnd:+0.0;-0.0;0} | {Pct(echo)} | {string.Join(", ", flags)} |"
                );
            }
        }

        private static float OpinionPct(TurnSnapshot s) => s.MaxOpinion > 0 ? (float)s.Opinion / s.MaxOpinion : 0f;

        /// <summary>Average of <paramref name="pick"/> over the snapshots taken on <paramref name="turn"/> (-1 = end of battle).</summary>
        private static float AvgAt(IEnumerable<BattleRecord> battles, int turn, Func<TurnSnapshot, float> pick)
        {
            var snaps = battles.SelectMany(b => b.Snapshots.Where(s => s.Turn == turn)).ToList();
            return snaps.Count == 0 ? 0f : snaps.Average(pick);
        }

        /// <summary>
        /// Card score = Opinion moved per AP spent (a 0-cost card counts as 1 AP), split by the
        /// target's mood when it was played. The overall score is the same ratio over every play,
        /// so moods are weighted by how often the card actually meets them.
        /// </summary>
        private static void CardsByHostility(StringBuilder md, List<BattleRecord> battles)
        {
            var plays = battles.SelectMany(b => b.Plays).ToList();
            if (plays.Count == 0)
                return;
            const int MinOverall = 10, MinBucket = 8;
            var moods = new[] { Mood.Hostile, Mood.Neutral, Mood.Receptive };

            float Score(IEnumerable<CardPlaySample> ps)
            {
                var list = ps.ToList();
                return list.Count == 0 ? float.NaN : (float)list.Sum(p => p.OpinionDelta) / list.Sum(p => Math.Max(1, p.Cost));
            }
            string S(float f) => float.IsNaN(f) ? "—" : f.ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture);

            md.AppendLine().AppendLine("## Card scores by hostility");
            md.AppendLine(
                "**Score** = Opinion moved per AP spent (0-cost counts as 1), split by the target's mood when the card was played. "
                    + "**Calm** = average change in the room's total hostility per play (negative calms the room, positive riles it). "
                    + $"Mood columns need {MinBucket}+ plays to show."
            );
            md.AppendLine()
                .AppendLine(
                    "Plays went into: "
                        + string.Join(", ", moods.Select(m => $"{m.ToString().ToLower()} {Pct((float)plays.Count(p => p.TargetMood == m) / plays.Count)}"))
                );

            var cards = plays
                .GroupBy(p => p.Card)
                .Where(g => g.Count() >= MinOverall)
                .Select(g => new
                {
                    Card = g.Key,
                    N = g.Count(),
                    Overall = Score(g),
                    ByMood = moods.ToDictionary(m => m, m => g.Count(p => p.TargetMood == m) >= MinBucket ? Score(g.Where(p => p.TargetMood == m)) : float.NaN),
                    Calm = (float)g.Average(p => p.RoomHostilityDelta),
                })
                .ToList();
            if (cards.Count == 0)
            {
                md.AppendLine().AppendLine($"No card reached {MinOverall} plays — run more seeds.");
                return;
            }

            md.AppendLine()
                .AppendLine("| Rank | Card | Score | vs Hostile | vs Neutral | vs Receptive | Calm | Plays | Best into |")
                .AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---|");
            int rank = 0;
            foreach (var c in cards.OrderByDescending(c => c.Overall).Take(20))
            {
                var best = c.ByMood.Where(kv => !float.IsNaN(kv.Value)).OrderByDescending(kv => kv.Value).Select(kv => kv.Key.ToString().ToLower()).FirstOrDefault() ?? "—";
                md.AppendLine(
                    $"| {++rank} | {c.Card} | {S(c.Overall)} | {S(c.ByMood[Mood.Hostile])} | {S(c.ByMood[Mood.Neutral])} | "
                        + $"{S(c.ByMood[Mood.Receptive])} | {c.Calm:+0.00;-0.00;0} | {c.N} | {best} |"
                );
            }

            md.AppendLine().AppendLine("**Best card into each mood:**").AppendLine();
            foreach (var m in moods)
            {
                var top = cards.Where(c => !float.IsNaN(c.ByMood[m])).OrderByDescending(c => c.ByMood[m]).Take(5).ToList();
                if (top.Count > 0)
                    md.AppendLine($"- **{m}:** {string.Join(", ", top.Select(c => $"{c.Card} ({S(c.ByMood[m])})"))}");
            }

            var swingy = cards
                .Select(c => (c.Card, scores: c.ByMood.Where(kv => !float.IsNaN(kv.Value)).ToList()))
                .Where(x => x.scores.Count >= 2)
                .Select(x => (x.Card, gap: x.scores.Max(kv => kv.Value) - x.scores.Min(kv => kv.Value),
                    hi: x.scores.OrderByDescending(kv => kv.Value).First().Key, lo: x.scores.OrderBy(kv => kv.Value).First().Key))
                .OrderByDescending(x => x.gap)
                .Take(8)
                .ToList();
            if (swingy.Count > 0)
            {
                md.AppendLine().AppendLine("**Most hostility-dependent** (biggest gap between best and worst mood — situational cards, or a mood bonus doing too much):").AppendLine();
                foreach (var x in swingy)
                    md.AppendLine($"- {x.Card}: {x.gap:0.0} per AP better into {x.hi.ToString().ToLower()} than {x.lo.ToString().ToLower()}");
            }

            md.AppendLine().AppendLine("**Room movers:**").AppendLine();
            md.AppendLine($"- Calms the room most: {string.Join(", ", cards.Where(c => c.Calm < 0).OrderBy(c => c.Calm).Take(5).Select(c => $"{c.Card} ({c.Calm:0.00})"))}");
            md.AppendLine($"- Riles the room most: {string.Join(", ", cards.Where(c => c.Calm > 0).OrderByDescending(c => c.Calm).Take(5).Select(c => $"{c.Card} (+{c.Calm:0.00})"))}");
        }

        private static void Cards(StringBuilder md, List<BattleRecord> battles)
        {
            md.AppendLine().AppendLine("## Cards");
            var plays = new Dictionary<string, int>();
            var delta = new Dictionary<string, int>();
            var best = new Dictionary<string, int>();
            var wonWhenPlayed = new Dictionary<string, (int won, int total)>();
            foreach (var b in battles)
            {
                foreach (var kv in b.CardPlays)
                {
                    plays[kv.Key] = plays.GetValueOrDefault(kv.Key) + kv.Value;
                    var w = wonWhenPlayed.GetValueOrDefault(kv.Key);
                    wonWhenPlayed[kv.Key] = (w.won + (b.Win ? 1 : 0), w.total + 1);
                }
                foreach (var kv in b.CardOpinionDelta)
                    delta[kv.Key] = delta.GetValueOrDefault(kv.Key) + kv.Value;
                foreach (var kv in b.CardBestPlay)
                    best[kv.Key] = Math.Max(best.GetValueOrDefault(kv.Key, int.MinValue), kv.Value);
            }
            if (plays.Count == 0)
            {
                md.AppendLine("No cards were played.");
                return;
            }
            float overall = Rate(battles);

            md.AppendLine("**Biggest single plays** — the most Opinion one card moved in one play:").AppendLine();
            md.AppendLine("| Card | Best play | Avg per play | Plays | Win rate when played |").AppendLine("|---|---:|---:|---:|---:|");
            foreach (var card in best.OrderByDescending(kv => kv.Value).Take(12).Select(kv => kv.Key))
                md.AppendLine(
                    $"| {card} | {best[card]:+#;-#;0} | {(float)delta[card] / plays[card]:+0.0;-0.0;0} | {plays[card]} | "
                        + $"{Pct((float)wonWhenPlayed[card].won / wonWhenPlayed[card].total)} |"
                );

            md.AppendLine()
                .AppendLine($"**Win-rate swing** — win rate in battles where the card was played, vs {Pct(overall)} overall (10+ battles):")
                .AppendLine();
            var swings = wonWhenPlayed
                .Where(kv => kv.Value.total >= 10)
                .Select(kv => (card: kv.Key, lift: (float)kv.Value.won / kv.Value.total - overall, n: kv.Value.total))
                .OrderByDescending(x => Math.Abs(x.lift))
                .Take(12);
            foreach (var s in swings)
                md.AppendLine($"- {s.card}: {(s.lift >= 0 ? "+" : "")}{s.lift * 100:0} pts ({s.n} battles)");

            var dead = plays.Where(kv => kv.Value >= 10 && delta[kv.Key] <= 0).Select(kv => kv.Key).ToList();
            if (dead.Count > 0)
                md.AppendLine()
                    .AppendLine(
                        $"**Never moves the meter** (10+ plays, net Opinion ≤ 0 — fine for setup cards, suspicious otherwise): {string.Join(", ", dead)}"
                    );

            var inDecks = battles.SelectMany(b => b.DeckCards).Distinct();
            var neverPlayed = inDecks.Where(c => !plays.ContainsKey(c)).ToList();
            if (neverPlayed.Count > 0)
                md.AppendLine()
                    .AppendLine($"**In decks but never played** (unplayable, unaffordable, or no bot wanted it): {string.Join(", ", neverPlayed)}");
        }

        private static void Enemies(StringBuilder md, List<BattleRecord> battles)
        {
            md.AppendLine().AppendLine("## Enemies");
            var used = new HashSet<string>(battles.SelectMany(b => b.EnemyMoves.Keys));
            var enemyNames = new HashSet<string>(battles.SelectMany(b => (b.Enemies ?? "").Split(" + ")));
            var unused = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyData"))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy == null || !enemyNames.Contains(enemy.EnemyName))
                    continue;
                foreach (var move in enemy.AggressiveMoves.Concat(enemy.NeutralMoves).Concat(enemy.ReceptiveMoves).Distinct())
                    if (move != null && !used.Contains(move.name))
                        unused.Add($"{enemy.EnemyName}: {move.name}");
            }
            md.AppendLine(
                unused.Count == 0
                    ? "Every move of every enemy that fought was used at least once."
                    : $"**Moves never used** (condition never met, stance never reached, or crowded out):\n\n- {string.Join("\n- ", unused.Distinct())}"
            );

            md.AppendLine().AppendLine("**Hardest rosters to beat** (player win rate, 10+ battles):").AppendLine();
            foreach (var g in battles.GroupBy(b => b.Enemies).Where(g => g.Count() >= 10).OrderBy(g => Rate(g)).Take(8))
                md.AppendLine($"- {g.Key}: {Pct(Rate(g))} ({g.Count()} battles, avg {g.Average(b => b.DamageToPlayer):0} Opinion taken)");
        }

        private static void Campaigns(StringBuilder md, List<CampaignRecord> runs, EncounterPoolData pool)
        {
            md.AppendLine().AppendLine("## Campaign runs");
            md.AppendLine("| Bot | Origin | Runs | Completed | Defeated | Stuck | Avg day reached | Avg funds | Avg cred | Avg deck |")
                .AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var g in runs.GroupBy(r => (r.Bot, r.Origin)))
                md.AppendLine(
                    $"| {g.Key.Bot} | {g.Key.Origin} | {g.Count()} | {g.Count(r => r.EndReason == "Completed")} | "
                        + $"{g.Count(r => r.EndReason == "Defeated")} | {g.Count(r => r.EndReason == "Stuck")} | "
                        + $"{g.Average(r => r.DayReached):0.0} | {g.Average(r => r.Funds):0} | {g.Average(r => r.Credibility):0} | {g.Average(r => r.DeckSize):0} |"
                );

            var killers = runs.Where(r => r.DefeatedBy != null).GroupBy(r => r.DefeatedBy).OrderByDescending(g => g.Count()).ToList();
            if (killers.Count > 0)
            {
                md.AppendLine().AppendLine("**What ends runs:**").AppendLine();
                foreach (var g in killers)
                    md.AppendLine($"- {g.Key}: {g.Count()} defeats");
            }

            if (pool != null)
            {
                var enders = new HashSet<string>(
                    pool.Entries.Where(e => CampaignHarness.IsDayEnder(e.Encounter)).Select(e => e.Encounter.name)
                );
                var hqPicks = runs.SelectMany(r => r.OptionsChosen)
                    .Where(kv => enders.Contains(kv.Key.Split(" / ")[0]))
                    .GroupBy(kv => kv.Key)
                    .Select(g => $"{g.Key.Split(" / ")[1]} {g.Sum(kv => kv.Value)}")
                    .ToList();
                md.AppendLine()
                    .AppendLine(
                        $"**End of day:** {(hqPicks.Count == 0 ? "never stopped at HQ" : "HQ — " + string.Join(", ", hqPicks))}; "
                            + $"{runs.Sum(r => r.DaysEndedWithoutHq)} days ended without it (HQ not on the map)."
                    );
            }

            var idle = runs.Sum(r => r.DaysWithNothingToDo);
            if (idle > 0)
                md.AppendLine().AppendLine($"**{idle} days** across all runs had nothing enterable but the day still had time left.");

            if (pool != null)
            {
                var offered = new HashSet<string>(runs.SelectMany(r => r.Offered));
                var entered = new HashSet<string>(runs.SelectMany(r => r.Visited.Select(v => v.Substring(v.IndexOf(':') + 1))));
                var neverOffered = pool.Entries.Where(e => e.Encounter != null && !offered.Contains(e.Encounter.name)).Select(e => e.Encounter.name).Distinct().ToList();
                var offeredNotTaken = offered.Where(o => !entered.Contains(o)).ToList();
                md.AppendLine();
                md.AppendLine(
                    neverOffered.Count == 0
                        ? "Every pool entry was offered at least once."
                        : $"**Pool entries never offered** (gate never passes, window too narrow, or weight crowded out): {string.Join(", ", neverOffered)}"
                );
                if (offeredNotTaken.Count > 0)
                    md.AppendLine().AppendLine($"**Offered but never entered:** {string.Join(", ", offeredNotTaken)}");
            }

            var lockedForever = runs.SelectMany(r => r.OptionsSeenLocked).Distinct()
                .Where(o => !runs.Any(r => r.OptionsChosen.ContainsKey(o)))
                .ToList();
            if (lockedForever.Count > 0)
                md.AppendLine().AppendLine($"**Options that were locked every time they were seen:** {string.Join("; ", lockedForever)}");

            md.AppendLine().AppendLine("**Most-picked options** (what the bots found worth taking):").AppendLine();
            foreach (var kv in runs.SelectMany(r => r.OptionsChosen).GroupBy(kv => kv.Key)
                         .Select(g => (g.Key, n: g.Sum(kv => kv.Value))).OrderByDescending(x => x.n).Take(10))
                md.AppendLine($"- {kv.Key}: {kv.n}");
        }

        private static float Rate(IEnumerable<BattleRecord> rows)
        {
            var list = rows.ToList();
            return list.Count == 0 ? 0f : (float)list.Count(b => b.Win) / list.Count;
        }

        private static string Pct(float f) => (f * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";

        private static string Cell(string s) => (s ?? "").Replace("|", "\\|").Replace("\n", " ");

        #endregion
    }
}
