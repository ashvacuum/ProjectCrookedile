using System;
using Crookedile.Data.Cards;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Crookedile.Data.Campaign
{
    /// <summary>
    /// Abstract base for everything an event choice can do to the run. Mirrors the
    /// <c>BattleEffect</c> pattern one layer up: each concrete owns only the fields it needs,
    /// stored via <c>[SerializeReference]</c> on <see cref="EventOption"/> so Odin renders a
    /// type-picker dropdown for heterogeneous lists.
    ///
    /// To add a new outcome: create a <c>[Serializable]</c> class inheriting from this and
    /// implement <see cref="Apply"/> and <see cref="GetDescription"/>. No other file changes.
    /// </summary>
    [Serializable]
    // Live description above each outcome's fields, so designers see what a choice does
    // without reading code. Same affordance as BattleEffect's InfoBox.
    [InfoBox(
        "@$value == null ? \"(no outcome chosen)\" : $value.EditorSafeDescription()",
        InfoMessageType.None
    )]
    public abstract class RunOutcome
    {
        /// <summary>Mutates <paramref name="state"/>. Called once when its option is chosen.</summary>
        public abstract void Apply(RunState state);

        /// <summary>Human-readable summary, shown in the inspector and usable as option subtext.</summary>
        public abstract string GetDescription();

        /// <summary>
        /// Guarded wrapper for the inspector InfoBox — one outcome with a latent null-deref in its
        /// description must not break the whole event asset's inspector. Referenced by reflection
        /// from <c>[InfoBox]</c>, so it must stay public.
        /// </summary>
        public string EditorSafeDescription()
        {
            try
            {
                return GetDescription();
            }
            catch (Exception e)
            {
                return $"(description error: {e.GetType().Name})";
            }
        }
    }

    // ponytail: the outcomes share this file while they're all a dozen lines each. Split to
    // one-per-file like Gameplay/Battle/Effects/ once they need folders.

    /// <summary>
    /// Cached handle to the shared <see cref="CardDatabase"/>. Outcomes are plain serialized
    /// classes with no inspector-assignable asset reference, so they resolve it by path — the
    /// same path <c>BattleTestStarter</c> uses.
    /// </summary>
    internal static class CardDatabaseLookup
    {
        private const string Path = "Databases/CardDatabase";
        private static CardDatabase _cached;

        public static CardDatabase Get()
        {
            // Re-resolves when null rather than caching the miss: a domain reload clears this,
            // and an editor-time first call can precede the database existing.
            if (_cached == null)
            {
                _cached = Resources.Load<CardDatabase>(Path);
                if (_cached == null)
                    Debug.LogWarning($"[RunOutcome] No CardDatabase at Resources/{Path}.");
            }
            return _cached;
        }
    }

    /// <summary>Adds to (or subtracts from) the run's Funds.</summary>
    [Serializable]
    public class AdjustFundsOutcome : RunOutcome
    {
        [Tooltip("Signed. Negative charges the player; the total clamps at zero, never negative.")]
        [SerializeField]
        private int _amount = 10;

        public override void Apply(RunState state) => state.AdjustFunds(_amount);

        public override string GetDescription() =>
            _amount >= 0 ? $"Gain {_amount} Funds" : $"Lose {-_amount} Funds";
    }

    /// <summary>Adds to (or subtracts from) the run's Credibility.</summary>
    [Serializable]
    public class AdjustCredibilityOutcome : RunOutcome
    {
        [Tooltip("Signed. Negative costs credibility; the total clamps at zero, never negative.")]
        [SerializeField]
        private int _amount = 5;

        public override void Apply(RunState state) => state.AdjustCredibility(_amount);

        public override string GetDescription() =>
            _amount >= 0 ? $"Gain {_amount} Credibility" : $"Lose {-_amount} Credibility";
    }

    /// <summary>
    /// Adjusts Credibility by a percentage of the origin's <em>starting</em> Credibility, so
    /// "restore a quarter of your standing" means the same thing to every archetype without
    /// authoring a per-origin number.
    ///
    /// The baseline is the origin's start rather than current Credibility on purpose: a percent
    /// of current does nothing at zero and snowballs when high, which is growth, not healing.
    /// </summary>
    [Serializable]
    public class AdjustCredibilityPercentOutcome : RunOutcome
    {
        [Tooltip(
            "Signed percent of the origin's STARTING Credibility. 25 restores a quarter of the "
                + "baseline; -25 costs that much. Rounds away from zero, so a non-zero percent "
                + "always moves at least 1."
        )]
        [SerializeField]
        private float _percent = 25f;

        /// <summary>
        /// The origin's starting Credibility — the baseline percentages are taken from. Read
        /// live from <see cref="OriginDatabase"/> rather than cached on the run, so retuning
        /// an origin retunes every percent outcome with it.
        /// </summary>
        private static int Baseline(RunState state) =>
            OriginDatabase.Shared?.GetCampaignStart(state.Origin).credibility ?? 0;

        public override void Apply(RunState state)
        {
            int baseline = Baseline(state);
            if (baseline <= 0 || Mathf.Approximately(_percent, 0f))
                return;

            // Ceil the magnitude: a 10% heal off a baseline of 5 should be 1, not rounded to 0.
            int delta = Mathf.CeilToInt(Mathf.Abs(baseline * _percent * 0.01f));
            state.AdjustCredibility(_percent >= 0f ? delta : -delta);
        }

        public override string GetDescription() =>
            _percent >= 0f
                ? $"Gain {_percent:0.##}% of starting Credibility"
                : $"Lose {-_percent:0.##}% of starting Credibility";
    }

    /// <summary>
    /// Adds a specific card to the deck. The workhorse for consequences that stick: this is how
    /// an event hands you a curse, or a boon you'll draw for the rest of the run.
    /// </summary>
    [Serializable]
    public class GainCardOutcome : RunOutcome
    {
        [SerializeField]
        private CardData _card;

        [Tooltip("How many copies to add.")]
        [Min(1)]
        [SerializeField]
        private int _count = 1;

        public override void Apply(RunState state)
        {
            for (int i = 0; i < _count; i++)
                state.AddCardToDeck(_card);
        }

        public override string GetDescription() =>
            _card == null ? "Gain card: (NONE SET)"
            : _count > 1 ? $"Gain {_count}× {_card.CardName}"
            : $"Gain {_card.CardName}";
    }

    /// <summary>
    /// Adds a random card to the deck, drawn from <see cref="CardDatabase"/>. Leave the rarity
    /// unset to roll on the database's own reward weights (Basic 70 / Enhanced 25 / Rare 5).
    /// </summary>
    [Serializable]
    public class GainRandomCardOutcome : RunOutcome
    {
        [Tooltip("Restrict the draw to one rarity. Untick to roll on the standard reward weights.")]
        [SerializeField]
        private bool _restrictRarity;

        [ShowIf(nameof(_restrictRarity))]
        [SerializeField]
        private CardRarity _rarity = CardRarity.Basic;

        public override void Apply(RunState state)
        {
            var db = CardDatabaseLookup.Get();
            if (db == null)
                return;

            CardData card;
            if (_restrictRarity)
            {
                var pool = db.GetByRarity(_rarity);
                if (pool.Count == 0)
                {
                    Debug.LogWarning(
                        $"[GainRandomCardOutcome] No {_rarity} cards in the database."
                    );
                    return;
                }
                // Draw from the run's stream so the same seed offers the same card here too.
                card = pool[state.Rng.Next(pool.Count)];
            }
            else
            {
                card = db.GetRandomByRarityWeight(state.Rng);
            }
            state.AddCardToDeck(card);
        }

        public override string GetDescription() =>
            _restrictRarity ? $"Gain a random {_rarity} card" : "Gain a random card";
    }

    /// <summary>
    /// Removes one copy of a specific card from the deck. Use for cleansing events — "confess,
    /// and lose a Doubt". No-op when the deck holds no copy.
    /// </summary>
    /// <remarks>For "remove any card" use <see cref="RemoveChosenCardOutcome"/>.</remarks>
    [Serializable]
    public class RemoveCardOutcome : RunOutcome
    {
        [SerializeField]
        private CardData _card;

        public override void Apply(RunState state) => state.RemoveCardFromDeck(_card);

        public override string GetDescription() =>
            _card != null ? $"Remove {_card.CardName}" : "Remove card: (NONE SET)";
    }

    /// <summary>
    /// Removes one random card from the deck — the cost side of a bargain, where
    /// <see cref="RemoveChosenCardOutcome"/> is a reward. Restrict by type to aim it: only a
    /// Scandal is a boon, only a Pressure is a real loss.
    /// </summary>
    [Serializable]
    public class RemoveRandomCardOutcome : RunOutcome
    {
        [Tooltip("Restrict the roll to one card type. Untick to remove any card in the deck.")]
        [SerializeField]
        private bool _restrictType;

        [ShowIf(nameof(_restrictType))]
        [SerializeField]
        private CardType _type = CardType.Scandal;

        [Tooltip("How many cards to remove. Each is rolled separately from what's left.")]
        [Min(1)]
        [SerializeField]
        private int _count = 1;

        public override void Apply(RunState state)
        {
            for (int i = 0; i < _count; i++)
            {
                var candidates = new System.Collections.Generic.List<CardData>();
                for (int j = 0; j < state.Deck.Count; j++)
                {
                    CardData card = state.Deck[j];
                    if (card == null)
                        continue;
                    if (_restrictType && card.CardType != _type)
                        continue;

                    candidates.Add(card);
                }

                if (candidates.Count == 0)
                {
                    // Not silent: an event that promised to burn a Scandal and found none reads
                    // as broken, and a restricted deck legitimately runs out.
                    Debug.LogWarning(
                        "[RemoveRandomCardOutcome] Nothing matching left in the deck — no-op."
                    );
                    return;
                }

                // The run's stream, so a replayed seed removes the same card.
                state.RemoveCardFromDeck(candidates[state.Rng.Next(candidates.Count)]);
            }
        }

        public override string GetDescription()
        {
            string what = _restrictType ? $"random {_type} card" : "random card";
            return _count > 1 ? $"Remove {_count} {what}s" : $"Remove a {what}";
        }
    }

    /// <summary>
    /// Upgrades one random upgradeable card in the deck. Restrict by type for themed events —
    /// "the seminary sharpens your sermons" upgrading a Rhetoric.
    /// </summary>
    [Serializable]
    public class UpgradeRandomCardOutcome : RunOutcome
    {
        [Tooltip("Restrict the roll to one card type. Untick to upgrade any upgradeable card.")]
        [SerializeField]
        private bool _restrictType;

        [ShowIf(nameof(_restrictType))]
        [SerializeField]
        private CardType _type = CardType.Pressure;

        public override void Apply(RunState state)
        {
            var candidates = state.GetUpgradeableCards(_restrictType ? _type : (CardType?)null);
            if (candidates.Count == 0)
            {
                // Silent no-op would read as a broken event, and the deck legitimately can run
                // out of upgradeable cards late in a run.
                Debug.LogWarning(
                    "[UpgradeRandomCardOutcome] Nothing upgradeable in the deck — no-op."
                );
                return;
            }

            // The run's stream, so a replayed seed upgrades the same card.
            state.UpgradeCardInDeck(candidates[state.Rng.Next(candidates.Count)]);
        }

        public override string GetDescription() =>
            _restrictType ? $"Upgrade a random {_type} card" : "Upgrade a random card";
    }

    /// <summary>
    /// Lets the player pick a card from the deck to upgrade. Raises a
    /// <see cref="RunState.CardChoice"/> the campaign screen draws; no-op when nothing in the deck
    /// can be upgraded, so the player is never shown a picker with no valid answer.
    /// </summary>
    [Serializable]
    public class UpgradeChosenCardOutcome : RunOutcome
    {
        [Tooltip("Restrict the choice to one card type. Untick to offer any upgradeable card.")]
        [SerializeField]
        private bool _restrictType;

        [ShowIf(nameof(_restrictType))]
        [SerializeField]
        private CardType _type = CardType.Pressure;

        [Tooltip("Prompt shown above the card list.")]
        [SerializeField]
        private string _prompt = "Choose a card to upgrade";

        public override void Apply(RunState state)
        {
            state.RequestCardChoice(
                _prompt,
                state.GetUpgradeableCards(_restrictType ? _type : (CardType?)null),
                state.UpgradeCardInDeck
            );
        }

        public override string GetDescription() =>
            _restrictType ? $"Upgrade a chosen {_type} card" : "Upgrade a chosen card";
    }

    /// <summary>
    /// Lets the player pick a card to remove from the deck — the classic "purge" reward. Offers
    /// the whole deck; restrict by type for events that only clean up one kind of card.
    /// </summary>
    [Serializable]
    public class RemoveChosenCardOutcome : RunOutcome
    {
        [Tooltip("Restrict the choice to one card type — e.g. only let Scandals be purged.")]
        [SerializeField]
        private bool _restrictType;

        [ShowIf(nameof(_restrictType))]
        [SerializeField]
        private CardType _type = CardType.Scandal;

        [Tooltip("Prompt shown above the card list.")]
        [SerializeField]
        private string _prompt = "Choose a card to remove";

        public override void Apply(RunState state)
        {
            var candidates = new System.Collections.Generic.List<CardData>();

            for (int i = 0; i < state.Deck.Count; i++)
            {
                CardData card = state.Deck[i];
                if (card == null)
                    continue;
                if (_restrictType && card.CardType != _type)
                    continue;

                candidates.Add(card);
            }

            state.RequestCardChoice(_prompt, candidates, state.RemoveCardFromDeck);
        }

        public override string GetDescription() =>
            _restrictType ? $"Remove a chosen {_type} card" : "Remove a chosen card";
    }

    /// <summary>
    /// Sends the run into another encounter instead of back to the map — a battle, another
    /// event, whatever. This is how a choice has consequences beyond a stat change: "refuse the
    /// bribe" leads into the fight, "pay him" doesn't.
    ///
    /// Prefer this over authoring extra rounds into a <c>BattleSession</c>: it works for any
    /// encounter type, and lets events sit between fights.
    /// </summary>
    [Serializable]
    public class GoToEncounterOutcome : RunOutcome
    {
        [SerializeField]
        private EncounterData _encounter;

        public override void Apply(RunState state) => state.SetNextEncounter(_encounter);

        public override string GetDescription()
        {
            if (_encounter == null)
                return "Leads to: (NONE SET)";
            // DisplayName is often left blank while roughing out an encounter; fall back to the
            // asset name so the inspector row is never just "Leads to:".
            string label = string.IsNullOrEmpty(_encounter.DisplayName)
                ? _encounter.name
                : _encounter.DisplayName;
            return $"Leads to: {label}";
        }
    }

    /// <summary>
    /// Records a narrative flag on the run — the memory of *this choice*, not merely of having
    /// visited the encounter. Pair with <c>HasFlag</c> on a later pool entry's Requirements
    /// (hard unlock) or BoostIf (raised chance), or on a later event option.
    /// </summary>
    [Serializable]
    public class SetFlagOutcome : RunOutcome
    {
        [Tooltip(
            "Flag name, e.g. \"took_bribe\". Free-form and case-sensitive — keep a convention "
                + "and check the spelling against wherever you test it; nothing validates it."
        )]
        [SerializeField]
        private string _flag;

        [Tooltip("Clear the flag instead of setting it.")]
        [SerializeField]
        private bool _clear;

        public override void Apply(RunState state)
        {
            if (_clear)
                state.ClearFlag(_flag);
            else
                state.SetFlag(_flag);
        }

        public override string GetDescription() =>
            string.IsNullOrWhiteSpace(_flag) ? "Set flag: (NONE SET)"
            : _clear ? $"Clear flag \"{_flag}\""
            : $"Set flag \"{_flag}\"";
    }

    /// <summary>Grants a relic. Duplicates are ignored by <see cref="RunState.AddRelic"/>.</summary>
    [Serializable]
    public class GrantRelicOutcome : RunOutcome
    {
        [SerializeField]
        private RelicData _relic;

        public override void Apply(RunState state) => state.AddRelic(_relic);

        // An unset relic silently no-ops at runtime; surfacing it in the description is what
        // makes that visible while authoring, since there is no health-check consumer up here yet.
        public override string GetDescription() =>
            _relic != null ? $"Gain relic: {_relic.RelicName}" : "Gain relic: (NONE SET)";
    }

    /// <summary>
    /// Ends the day: advances the day counter and refills Hours, exactly as returning to HQ
    /// does. This is the cost for an event that eats the rest of your evening.
    ///
    /// The map redraws on its own — closing the event refreshes locations, and the new day
    /// makes that refresh a real re-draw rather than a no-op. Pairs badly with
    /// <see cref="GoToEncounterOutcome"/> on the same option: the chain runs first and the new
    /// day's map only appears once it resolves.
    /// </summary>
    [Serializable]
    public class AdvanceDayOutcome : RunOutcome
    {
        public override void Apply(RunState state) => state.AdvanceDay();

        public override string GetDescription() => "End the day";
    }
}
