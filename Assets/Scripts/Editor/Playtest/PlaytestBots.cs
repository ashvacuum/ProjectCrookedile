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

        /// <summary>Whether this bot plays <paramref name="origin"/>; the runner skips the rest.</summary>
        bool Plays(OriginType origin) => true;
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
    /// target mood — the same numbers as the report's "Card scores by hostility". Opinion includes
    /// setup credit (the Fanatic follows a play set up), and each mood score is split again by
    /// whether Fanatics were following, so attacks rank higher once the room has followers.
    /// </summary>
    public class ScoreTable
    {
        private const int MinSamples = 5;

        private readonly Dictionary<(string card, Mood mood, bool fanatics), (int opinion, int ap, int n)> _byFanatics = new();
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
            var lines = System.IO.File.ReadLines(file).ToList();
            // Later columns by name: runs from before they existed have neither.
            var header = lines.Count > 0 ? SplitCsv(lines[0]) : new List<string>();
            int fanaticsCol = header.IndexOf("fanatics");
            int setupCol = header.IndexOf("setup_opinion");
            foreach (var line in lines.Skip(1))
            {
                var f = SplitCsv(line);
                // context,bot,origin,encounter,seed,turn,card,cost,target_mood,target_hostility,room_hostility,opinion_delta,...
                if (f.Count < 12 || f[1] == "Scored") // learn from the other bots, not from itself
                    continue;
                if (!int.TryParse(f[7], out int cost) || !int.TryParse(f[11], out int opinion)
                    || !System.Enum.TryParse(f[8], out Mood mood))
                    continue;
                if (setupCol >= 0 && setupCol < f.Count && int.TryParse(f[setupCol], out int setup))
                    opinion += setup;
                int ap = System.Math.Max(1, cost);
                if (fanaticsCol >= 0 && fanaticsCol < f.Count && int.TryParse(f[fanaticsCol], out int fanatics))
                    Add(_byFanatics, (f[6], mood, fanatics > 0), opinion, ap);
                Add(_byMood, (f[6], mood), opinion, ap);
                Add(_overall, f[6], opinion, ap);
                rows++;
            }
            Source = $"{rows} plays from {System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file))}";
        }

        /// <summary>
        /// Expected Opinion per AP for <paramref name="card"/> into a target of <paramref name="mood"/>,
        /// with or without Fanatics following.
        /// </summary>
        public float Score(CardData card, int cost, Mood mood, bool fanatics)
        {
            if (_byFanatics.TryGetValue((card.CardName, mood, fanatics), out var f) && f.n >= MinSamples)
                return (float)f.opinion / f.ap;
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
            bool fanatics = BattleHarness.FanaticCount(bm) > 0;
            foreach (int i in playable)
            {
                var card = bm.PlayerDeck.Hand[i];
                int cost = bm.GetEffectiveCardCost(card);
                for (int t = 0; t < bm.Enemies.Count; t++)
                {
                    var enemy = bm.Enemies[t];
                    if (enemy.IsDefeated)
                        continue;
                    float score = Table.Score(card, cost, MoodOf(enemy), fanatics);
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

    /// <summary>
    /// Faith Leader only: plays the conversion build on purpose (core-design.md section 7). Gets the
    /// conversion Policy (Gospel) down, picks the cheapest enemy to convert and stacks Guilt on it,
    /// then leads its Fanatics with attacks, defends and command cards, extends a buff about to run
    /// out, and takes the quiet Silence turn when it has converted enough. Cards are recognised by
    /// their effects, not their names. Otherwise it plays like Greedy.
    /// ponytail: one plan, no lookahead; it never weighs converting against just pushing.
    /// </summary>
    public class ConvertBot : IBattleBot
    {
        public string Name => "Convert";

        public bool Plays(OriginType origin) => origin == OriginType.FaithLeader;

        private BattleManager _battle;
        private bool _engine; // a conversion Policy is in play this battle
        private int _turn = -1;
        private bool _attackedOrDefended; // this turn, so the quiet Silence turn is off
        private int _target = -1;

        public int ChooseCard(BattleManager bm, List<int> playable, System.Random rng)
        {
            if (bm != _battle)
            {
                _battle = bm;
                _engine = false;
                _turn = -1;
            }
            if (bm.CurrentTurn != _turn)
            {
                _turn = bm.CurrentTurn;
                _attackedOrDefended = false;
            }
            _target = -1;
            if (playable.Count == 0)
                return -1;

            var hand = bm.PlayerDeck.Hand;
            var config = FaithLeaderConfig.Current;
            int Pick(int index, int target = -1)
            {
                var card = hand[index];
                _target = target;
                if (CardFacts.IsConversionPolicy(card))
                    _engine = true;
                if (CardFacts.IsAttackOrDefend(card))
                    _attackedOrDefended = true;
                return index;
            }
            int First(System.Func<CardData, bool> match) =>
                playable.Where(i => match(hand[i])).OrderByDescending(i => BotMath.Value(hand[i])).DefaultIfEmpty(-1).First();

            // 1. The engine: without the Policy, Guilt never converts.
            int policy = First(CardFacts.IsConversionPolicy);
            if (policy >= 0)
                return Pick(policy);

            int fanatics = BattleHarness.FanaticCount(bm);

            // 2. The quiet turn: enough conversions and no attack or defend yet, so Fanatics Silence.
            if (fanatics > 0 && !_attackedOrDefended && bm.ConversionsThisTurn >= config.SilenceMinConversions)
            {
                int quiet = First(c => !CardFacts.IsAttackOrDefend(c));
                return quiet >= 0 ? Pick(quiet, ConvertTarget(bm)) : -1;
            }

            // 3. Keep followers following: extend a buff on its last turn.
            bool expiring = bm.Enemies.Any(e => !e.IsDefeated && e.StatusEffects.GetStacks<FanaticStatus>() == 1);
            if (expiring)
            {
                int extend = First(CardFacts.ExtendsFanatics);
                if (extend >= 0)
                    return Pick(extend);
            }

            // 4. Finish a conversion this card can complete.
            int target = ConvertTarget(bm);
            if (_engine && target >= 0)
            {
                int need = GuiltNeeded(bm.Enemies[target], config);
                int finish = playable
                    .Where(i => CardFacts.GuiltApplied(hand[i]) >= need)
                    .OrderBy(i => bm.GetEffectiveCardCost(hand[i]))
                    .DefaultIfEmpty(-1)
                    .First();
                if (finish >= 0)
                    return Pick(finish, target);
            }

            // 5. Lead the Fanatics: commands, then anything they copy.
            if (fanatics > 0)
            {
                int command = First(CardFacts.CommandsFanatics);
                if (command >= 0)
                    return Pick(command, BotMath.MostHostile(bm));
                int lead = First(CardFacts.IsAttackOrDefend);
                if (lead >= 0)
                    return Pick(lead, BotMath.MostHostile(bm));
            }

            // 6. Make a Fanatic directly where a receptive enemy allows it.
            int receptive = Enumerable.Range(0, bm.Enemies.Count)
                .Where(i => !bm.Enemies[i].IsDefeated && bm.Enemies[i].Stats.IsReceptive
                    && !bm.Enemies[i].StatusEffects.HasStatus<FanaticStatus>())
                .DefaultIfEmpty(-1)
                .First();
            if (receptive >= 0)
            {
                int chosen = First(CardFacts.MakesFanatic);
                if (chosen >= 0)
                    return Pick(chosen, receptive);
            }

            // 7. Build toward the next conversion.
            if (_engine && target >= 0)
            {
                int stack = First(c => CardFacts.GuiltApplied(c) > 0);
                if (stack >= 0)
                    return Pick(stack, target);
            }

            // 8. Otherwise play like Greedy.
            return Pick(playable.OrderByDescending(i => BotMath.Value(hand[i])).First(), BotMath.MostHostile(bm));
        }

        public int ChooseTarget(BattleManager bm, System.Random rng) =>
            _target >= 0 ? _target : BotMath.MostHostile(bm);

        public List<CardData> AnswerChoice(CardChoiceRequest c, System.Random rng) =>
            BotMath.PickChoices(c, rng, best: true);

        /// <summary>The living, non-Hardened, non-Fanatic enemy that needs the least Guilt to convert; -1 if none.</summary>
        private static int ConvertTarget(BattleManager bm)
        {
            var config = FaithLeaderConfig.Current;
            int best = -1,
                bestNeed = int.MaxValue;
            for (int i = 0; i < bm.Enemies.Count; i++)
            {
                var e = bm.Enemies[i];
                if (e.IsDefeated || e.Stats.IsHardened || e.StatusEffects.HasStatus<FanaticStatus>())
                    continue;
                int need = GuiltNeeded(e, config);
                if (need < bestNeed)
                {
                    best = i;
                    bestNeed = need;
                }
            }
            return best;
        }

        private static int GuiltNeeded(EnemyController e, FaithLeaderConfig config) =>
            config.ConversionThreshold
            + e.StatusEffects.GetStacks<JadedStatus>()
            - e.StatusEffects.GetStacks<GuiltStatus>();
    }

    /// <summary>
    /// What a card does for the conversion build, read off its effects (nested ones and its passives
    /// included). Editor-only, so reading private fields by reflection is fine.
    /// </summary>
    public static class CardFacts
    {
        public static bool IsConversionPolicy(CardData card) =>
            card.CardType == CardType.Policy && Effects(card).Any(e => e is ConvertPacifiedEffect);

        /// <summary>Guilt (or any pacify status) the card applies at a fixed amount.</summary>
        public static int GuiltApplied(CardData card) =>
            Effects(card)
                .OfType<ApplyStatusBehaviorEffect>()
                .Where(e => Field<StatusBehavior>(e, "_behavior")?.CountsTowardPacify == true)
                .Sum(e => Field<int>(e, "_stacks"));

        public static bool CommandsFanatics(CardData card) =>
            Effects(card).Any(e => e is FanaticsPushEffect || e is FanaticsSupportEffect)
            || ForEachFanatic(card).Any(inner => !AppliesFanatic(inner));

        public static bool ExtendsFanatics(CardData card) => ForEachFanatic(card).Any(AppliesFanatic);

        /// <summary>Applies Fanatic straight onto an enemy (God's Chosen), outside any Fanatic loop.</summary>
        public static bool MakesFanatic(CardData card) =>
            !ExtendsFanatics(card) && Effects(card).Any(AppliesFanatic);

        /// <summary>Something Fanatics copy: the card pushes the meter or gains Support.</summary>
        public static bool IsAttackOrDefend(CardData card) =>
            Effects(card).Any(e => e is ApplyOpinionEffect || e is GainSupportEffect);

        private static bool AppliesFanatic(BattleEffect e) =>
            e is ApplyStatusBehaviorEffect && Field<StatusBehavior>(e, "_behavior") is FanaticStatus;

        private static IEnumerable<BattleEffect> ForEachFanatic(CardData card) =>
            Effects(card)
                .OfType<ForEachEnemyWithStatusEffect>()
                .Where(e => Field<StatusBehavior>(e, "_filterStatus") is FanaticStatus)
                .SelectMany(e => Field<List<BattleEffect>>(e, "_effects") ?? new List<BattleEffect>());

        /// <summary>Every effect on the card's current side, its passives' effects, and every effect nested inside those.</summary>
        public static IEnumerable<BattleEffect> Effects(CardData card)
        {
            if (card == null)
                return Enumerable.Empty<BattleEffect>();
            var roots = new List<object>();
            var effects = card.IsUpgraded && card.UpgradedEffects?.Count > 0 ? card.UpgradedEffects : card.Effects;
            if (effects != null)
                roots.AddRange(effects);
            if (card.GetPassives() != null)
                roots.AddRange(card.GetPassives());
            var found = new List<BattleEffect>();
            foreach (var root in roots)
                Walk(root, found, 0);
            return found;
        }

        private static void Walk(object node, List<BattleEffect> found, int depth)
        {
            if (node == null || depth > 8)
                return;
            if (node is BattleEffect effect)
                found.Add(effect);
            for (var type = node.GetType(); type != null && type != typeof(object); type = type.BaseType)
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    object value = f.GetValue(node);
                    if (value is BattleEffect || value is BattlePassive)
                        Walk(value, found, depth + 1);
                    else if (value is System.Collections.IEnumerable list && !(value is string))
                        foreach (var item in list)
                            if (item is BattleEffect || item is BattlePassive)
                                Walk(item, found, depth + 1);
                }
        }

        private static T Field<T>(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var f = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null)
                    return f.GetValue(target) is T value ? value : default;
            }
            return default;
        }
    }

    /// <summary>Greedy's map and event choices, but takes the conversion build's cards as rewards and fights with <see cref="ConvertBot"/>.</summary>
    public class ConvertCampaignBot : ICampaignBot
    {
        private readonly GreedyCampaignBot _greedy = new();

        public string Name => "ConvertRun";
        public IBattleBot BattleBot { get; } = new ConvertBot();

        public bool Plays(OriginType origin) => origin == OriginType.FaithLeader;

        public EncounterData ChooseLocation(RunState s, List<EncounterData> enterable, System.Random rng) =>
            _greedy.ChooseLocation(s, enterable, rng);

        public EventOption ChooseOption(RunState s, List<EventOption> available, System.Random rng) =>
            _greedy.ChooseOption(s, available, rng);

        /// <summary>The engine first, then cards that convert or lead, then Greedy's pick.</summary>
        public CardData ChooseReward(List<CardData> offers, System.Random rng)
        {
            int Rank(CardData c) =>
                CardFacts.IsConversionPolicy(c) ? 4
                : CardFacts.GuiltApplied(c) > 0 ? 3
                : CardFacts.CommandsFanatics(c) || CardFacts.ExtendsFanatics(c) ? 2
                : 0;
            var best = offers.OrderByDescending(Rank).FirstOrDefault();
            return best != null && Rank(best) > 0 ? best : _greedy.ChooseReward(offers, rng);
        }

        public CardData ChooseFromDeck(List<CardData> candidates, System.Random rng) =>
            _greedy.ChooseFromDeck(candidates, rng);
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

        /// <summary>Whether this bot plays <paramref name="origin"/>; the runner skips the rest.</summary>
        bool Plays(OriginType origin) => true;
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

            // Credibility is the run's HP: a heal is worth its points once losses have cut into it,
            // and nothing at the cap. Percent heals scale off the starting value.
            int baseline = OriginDatabase.Shared?.GetCampaignStart(s.Origin).credibility ?? 0;
            float need = s.MaxCredibility <= 0 || s.Credibility < s.MaxCredibility ? 1f : 0f;
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
