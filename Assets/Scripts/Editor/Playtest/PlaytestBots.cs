using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data;
using Crookedile.Gameplay.Battle;

namespace Crookedile.EditorTools.Playtest
{
    /// <summary>
    /// A battle strategy. Deliberately dumb and cheap: each bot reads the board and the cards'
    /// own damage previews, never simulates ahead. The point is to find outliers across many
    /// runs, not to play well.
    /// </summary>
    public interface IBattleBot
    {
        string Name { get; }

        /// <summary>Hand index to play from <paramref name="playable"/>, or -1 to end the turn.</summary>
        int ChooseCard(BattleManager bm, List<int> playable, System.Random rng);

        /// <summary>Enemy index to focus before a play.</summary>
        int ChooseTarget(BattleManager bm, System.Random rng);

        List<CardData> AnswerChoice(CardChoiceRequest choice, System.Random rng);
    }

    public static class BotMath
    {
        /// <summary>
        /// Rough worth of a card: its opinion preview where the effect has one, otherwise a flat
        /// 2 per effect. Good enough to rank a hand; not a balance number.
        /// </summary>
        public static int Value(CardData card)
        {
            int value = 0;
            if (card?.Effects == null)
                return 0;
            foreach (var effect in card.Effects)
                value += effect?.GetDamagePreview()?.Amount is int amount && amount > 0 ? amount : 2;
            return value;
        }

        public static int MostHostile(BattleManager bm)
        {
            int best = bm.FocusedEnemyIndex;
            int bestHostility = int.MinValue;
            for (int i = 0; i < bm.Enemies.Count; i++)
            {
                var e = bm.Enemies[i];
                if (e.IsDefeated || e.Stats.CurrentHostility <= bestHostility)
                    continue;
                best = i;
                bestHostility = e.Stats.CurrentHostility;
            }
            return best;
        }

        public static int RandomLiving(BattleManager bm, System.Random rng)
        {
            var living = Enumerable.Range(0, bm.Enemies.Count).Where(i => !bm.Enemies[i].IsDefeated).ToList();
            return living.Count == 0 ? bm.FocusedEnemyIndex : living[rng.Next(living.Count)];
        }

        /// <summary>Picks exactly what a choice needs: the required count, or a random amount up to it.</summary>
        public static List<CardData> PickChoices(CardChoiceRequest choice, System.Random rng, bool best)
        {
            var pool = choice.Choices?.ToList() ?? new List<CardData>();
            int count = System.Math.Min(choice.RequiredCount, pool.Count);
            if (choice.AllowFewer)
                count = rng.Next(count + 1);
            var ordered = best
                ? pool.OrderByDescending(Value).ToList()
                : pool.OrderBy(_ => rng.Next()).ToList();
            return ordered.Take(count).ToList();
        }
    }

    /// <summary>Legal moves at random, and sometimes stops early. If this wins a fight, the fight is broken.</summary>
    public class RandomBot : IBattleBot
    {
        public string Name => "Random";

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng) =>
            playable.Count == 0 || rng.NextDouble() < 0.15 ? -1 : playable[rng.Next(playable.Count)];

        public int ChooseTarget(BattleManager bm, System.Random rng) => BotMath.RandomLiving(bm, rng);

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: false);
    }

    /// <summary>Plays the highest-value card it can afford, aiming at the most hostile enemy.</summary>
    public class GreedyBot : IBattleBot
    {
        public string Name => "Greedy";

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng) =>
            playable.Count == 0
                ? -1
                : playable.OrderByDescending(i => BotMath.Value(bm.PlayerDeck.Hand[i])).First();

        public int ChooseTarget(BattleManager bm, System.Random rng) => BotMath.MostHostile(bm);

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: true);
    }

    /// <summary>Cheapest card first, until nothing is affordable — maximum cards per turn.</summary>
    public class TempoBot : IBattleBot
    {
        public string Name => "Tempo";

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng) =>
            playable.Count == 0
                ? -1
                : playable.OrderBy(i => bm.GetEffectiveCardCost(bm.PlayerDeck.Hand[i])).First();

        public int ChooseTarget(BattleManager bm, System.Random rng) => BotMath.MostHostile(bm);

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: true);
    }

    /// <summary>One card a turn, then passes. Finds wins that come from passives, stalls and the clock.</summary>
    public class HoarderBot : IBattleBot
    {
        public string Name => "Hoarder";

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng) =>
            playable.Count == 0 || bm.CardsPlayedThisTurn >= 1
                ? -1
                : playable.OrderByDescending(i => BotMath.Value(bm.PlayerDeck.Hand[i])).First();

        public int ChooseTarget(BattleManager bm, System.Random rng) => bm.FocusedEnemyIndex;

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: false);
    }

    /// <summary>
    /// Card scores learned from a previous run's card_plays.csv: Opinion per AP, per card, per
    /// target mood — the same numbers as the report's "Card scores by hostility".
    /// </summary>
    public class ScoreTable
    {
        private const int MinSamples = 5;

        private readonly Dictionary<(string card, Mood mood), (int opinion, int ap, int n)> _byMood = new();
        private readonly Dictionary<string, (int opinion, int ap, int n)> _overall = new();

        /// <summary>Where the scores came from, for the report header.</summary>
        public string Source { get; private set; } = "no previous run — fell back to damage previews";

        /// <summary>The newest Playtests/&lt;time&gt;/card_plays.csv, or an empty table when there is none.</summary>
        public static ScoreTable LoadLatest()
        {
            var table = new ScoreTable();
            string root = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? ".", "Playtests");
            if (!System.IO.Directory.Exists(root))
                return table;
            string file = System.IO.Directory.GetDirectories(root)
                .OrderByDescending(d => d)
                .Select(d => System.IO.Path.Combine(d, "card_plays.csv"))
                .FirstOrDefault(System.IO.File.Exists);
            if (file != null)
                table.Load(file);
            return table;
        }

        private void Load(string file)
        {
            int rows = 0;
            foreach (var line in System.IO.File.ReadLines(file).Skip(1))
            {
                var f = SplitCsv(line);
                // context,bot,origin,encounter,seed,turn,card,cost,target_mood,target_hostility,room_hostility,opinion_delta,...
                if (f.Count < 12 || f[1] == "Scored") // learn from the other bots, not from itself
                    continue;
                if (!int.TryParse(f[7], out int cost) || !int.TryParse(f[11], out int opinion)
                    || !System.Enum.TryParse(f[8], out Mood mood))
                    continue;
                int ap = System.Math.Max(1, cost);
                Add(_byMood, (f[6], mood), opinion, ap);
                Add(_overall, f[6], opinion, ap);
                rows++;
            }
            Source = $"{rows} plays from {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file))}";
        }

        /// <summary>Expected Opinion per AP for <paramref name="card"/> into a target of <paramref name="mood"/>.</summary>
        public float Score(CardData card, int cost, Mood mood)
        {
            if (_byMood.TryGetValue((card.CardName, mood), out var m) && m.n >= MinSamples)
                return (float)m.opinion / m.ap;
            if (_overall.TryGetValue(card.CardName, out var o) && o.n >= MinSamples)
                return (float)o.opinion / o.ap;
            return (float)BotMath.Value(card) / System.Math.Max(1, cost); // never seen: trust its preview
        }

        private static void Add<TKey>(Dictionary<TKey, (int opinion, int ap, int n)> map, TKey key, int opinion, int ap)
        {
            var v = map.TryGetValue(key, out var x) ? x : default;
            map[key] = (v.opinion + opinion, v.ap + ap, v.n + 1);
        }

        private static List<string> SplitCsv(string line)
        {
            var fields = new List<string>();
            var sb = new System.Text.StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else if (c == '"')
                        quoted = false;
                    else
                        sb.Append(c);
                }
                else if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                    sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }
    }

    /// <summary>
    /// Plays by the last run's card scores: every playable card against every living target, and
    /// the pair with the best expected Opinion per AP for that target's current mood wins. Its win
    /// rate against Greedy is the test of whether the scores mean anything.
    /// </summary>
    public class ScoredBot : IBattleBot
    {
        private ScoreTable _table;
        private int _target = -1;

        public string Name => "Scored";

        /// <summary>Loaded on first use, so opening the window doesn't read CSVs.</summary>
        public ScoreTable Table => _table ??= ScoreTable.LoadLatest();

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng)
        {
            _target = -1;
            float bestScore = float.NegativeInfinity;
            int bestCard = -1;
            foreach (int i in playable)
            {
                var card = bm.PlayerDeck.Hand[i];
                int cost = bm.GetEffectiveCardCost(card);
                for (int t = 0; t < bm.Enemies.Count; t++)
                {
                    var enemy = bm.Enemies[t];
                    if (enemy.IsDefeated)
                        continue;
                    float score = Table.Score(card, cost, MoodOf(enemy));
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestCard = i;
                        _target = t;
                    }
                }
            }
            // A card that historically loses Opinion is only worth it as setup; the bot can't tell,
            // so it keeps the AP rather than play a known loser.
            return bestScore < 0f ? -1 : bestCard;
        }

        public int ChooseTarget(BattleManager bm, System.Random rng) =>
            _target >= 0 ? _target : BotMath.MostHostile(bm);

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: true);

        private static Mood MoodOf(EnemyController e) =>
            e.Stats.IsHostile ? Mood.Hostile
            : e.Stats.IsReceptive ? Mood.Receptive
            : Mood.Neutral;
    }

    /// <summary>A campaign strategy: which location, which option, which reward. Fights with <see cref="BattleBot"/>.</summary>
    public interface ICampaignBot
    {
        string Name { get; }
        IBattleBot BattleBot { get; }

        /// <summary>Where to go next from <paramref name="enterable"/>, or null to end the day.</summary>
        EncounterData ChooseLocation(RunState state, List<EncounterData> enterable, System.Random rng);

        EventOption ChooseOption(RunState state, List<EventOption> available, System.Random rng);

        /// <summary>A reward card, or null to skip.</summary>
        CardData ChooseReward(List<CardData> offers, System.Random rng);

        CardData ChooseFromDeck(List<CardData> candidates, System.Random rng);
    }

    /// <summary>Wanders: random places, random answers, random rewards.</summary>
    public class RandomCampaignBot : ICampaignBot
    {
        public string Name => "RandomRun";
        public IBattleBot BattleBot { get; } = new TempoBot();

        public EncounterData ChooseLocation(RunState s, List<EncounterData> enterable, System.Random rng) =>
            enterable.Count == 0 || rng.NextDouble() < 0.1 ? null : enterable[rng.Next(enterable.Count)];

        public EventOption ChooseOption(RunState s, List<EventOption> available, System.Random rng) =>
            available[rng.Next(available.Count)];

        public CardData ChooseReward(List<CardData> offers, System.Random rng) =>
            offers.Count == 0 || rng.NextDouble() < 0.2 ? null : offers[rng.Next(offers.Count)];

        public CardData ChooseFromDeck(List<CardData> candidates, System.Random rng) =>
            candidates[rng.Next(candidates.Count)];
    }

    /// <summary>
    /// Min-maxes the meta economy: takes whichever option its outcome score likes best, fills
    /// the day with the cheapest visits, and always takes the strongest reward. Finds the
    /// exploits a player would find.
    /// </summary>
    public class GreedyCampaignBot : ICampaignBot
    {
        public string Name => "GreedyRun";
        public IBattleBot BattleBot { get; } = new GreedyBot();

        public EncounterData ChooseLocation(RunState s, List<EncounterData> enterable, System.Random rng)
        {
            // HQ ends the day, so it is the last resort; otherwise the shortest visit first,
            // which fits the most encounters into the hour budget.
            var real = enterable.Where(e => e.DurationMinutes > 0).ToList();
            if (real.Count == 0)
                return null;
            return real.OrderBy(e => s.PlanVisit(e).FinishMinute).ThenBy(_ => rng.Next()).First();
        }

        public EventOption ChooseOption(RunState s, List<EventOption> available, System.Random rng) =>
            available.OrderByDescending(o => o.Outcomes.Sum(x => Score(x, s))).ThenBy(_ => rng.Next()).First();

        public CardData ChooseReward(List<CardData> offers, System.Random rng) =>
            offers.OrderByDescending(c => c.Rarity).ThenByDescending(BotMath.Value).FirstOrDefault();

        public CardData ChooseFromDeck(List<CardData> candidates, System.Random rng) =>
            candidates.OrderByDescending(BotMath.Value).First();

        /// <summary>How much the bot likes an outcome. Reads private amounts; editor-only, so reflection is fine.</summary>
        private static float Score(RunOutcome outcome, RunState s)
        {
            object Field(string name) =>
                outcome.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(outcome);
            int Amount() => Field("_amount") is int a ? a : 0;

            // Credibility does nothing in battle yet, so a heal is only worth its full points
            // when the run is actually hurting; at or above the starting value, an upgrade wins.
            int baseline = OriginDatabase.Shared?.GetCampaignStart(s.Origin).credibility ?? 0;
            float need = s.Credibility < baseline ? 1f : 0.25f;
            return outcome switch
            {
                AdjustFundsOutcome => Amount() * 0.5f,
                AdjustCredibilityOutcome => Amount() * need,
                AdjustCredibilityPercentOutcome => (Field("_percent") is float p ? p : 0f) / 100f * baseline * need,
                GainCardOutcome or GainRandomCardOutcome or GainCardFromPoolOutcome => 6f,
                UpgradeRandomCardOutcome or UpgradeChosenCardOutcome => 8f,
                RemoveChosenCardOutcome => 5f,
                RecruitAllyOutcome => 12f,
                NextBattleHostilityOutcome => -3f * Amount(),
                SpendTimeOutcome => -4f,
                GoToEncounterOutcome => -1f,
                _ => 0f,
            };
        }
    }
}
