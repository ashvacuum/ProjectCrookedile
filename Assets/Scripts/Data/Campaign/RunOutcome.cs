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
    // ponytail: specific card only. Player-choice removal ("remove any card") needs a deck-view
    // picker outside battle, which does not exist yet — see the doc's Known gaps.
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
}
