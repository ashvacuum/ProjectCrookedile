using System.Collections.Generic;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using UnityEngine;

namespace Crookedile.Data.Save
{
    /// <summary>
    /// Resolves the content IDs a save stores back to assets. Cards, enemies and allies come
    /// from their databases under Resources/Databases. Encounters come from the campaign's
    /// encounter pool, plus every encounter its events chain to (GoToEncounterOutcome), since
    /// a chained encounter needn't be in the pool itself.
    /// </summary>
    public class SaveContent
    {
        private readonly CardDatabase _cards;
        private readonly EnemyDatabase _enemies;
        private readonly AllyDatabase _allies;
        private readonly Dictionary<string, EncounterData> _encounters =
            new Dictionary<string, EncounterData>();

        public SaveContent(
            CardDatabase cards,
            EnemyDatabase enemies,
            AllyDatabase allies,
            EncounterPoolData pool
        )
        {
            _cards = cards;
            _enemies = enemies;
            _allies = allies;
            if (pool != null)
                foreach (var entry in pool.Entries)
                    AddEncounter(entry?.Encounter);
        }

        /// <summary>The content the game ships, with encounters from <paramref name="pool"/>.</summary>
        public static SaveContent Load(EncounterPoolData pool) =>
            new SaveContent(
                CardDatabase.Shared,
                Resources.Load<EnemyDatabase>("Databases/EnemyDatabase"),
                Resources.Load<AllyDatabase>("Databases/AllyDatabase"),
                pool
            );

        public CardDatabase Cards => _cards;

        public CardData Card(string id) =>
            string.IsNullOrEmpty(id) || _cards == null ? null : _cards.GetByID(id);

        public EnemyData Enemy(string id) =>
            string.IsNullOrEmpty(id) || _enemies == null ? null : _enemies.GetByID(id);

        public AllyData Ally(string id) =>
            string.IsNullOrEmpty(id) || _allies == null ? null : _allies.GetById(id);

        public EncounterData Encounter(string id) =>
            !string.IsNullOrEmpty(id) && _encounters.TryGetValue(id, out var e) ? e : null;

        /// <summary>Adds an encounter and, for events, everything their options can chain to.</summary>
        private void AddEncounter(EncounterData encounter)
        {
            if (encounter == null || string.IsNullOrEmpty(encounter.ID))
                return;
            if (_encounters.ContainsKey(encounter.ID))
                return; // already walked (also stops chain cycles)
            _encounters[encounter.ID] = encounter;

            if (encounter is EventEncounterData evt)
                foreach (var option in evt.Options)
                    if (option != null)
                        AddChainTargets(option.Outcomes);
        }

        private void AddChainTargets(IReadOnlyList<RunOutcome> outcomes)
        {
            if (outcomes == null)
                return;
            foreach (var outcome in outcomes)
            {
                switch (outcome)
                {
                    case GoToEncounterOutcome goTo:
                        AddEncounter(goTo.Target);
                        break;
                    case CoinFlipOutcome flip:
                        AddChainTargets(flip.OnSuccess);
                        AddChainTargets(flip.OnFailure);
                        break;
                }
            }
        }

        /// <summary>
        /// The district called <paramref name="name"/> on <paramref name="travel"/>'s road
        /// network (headquarters included), or null. Districts have no ID, so saves use the
        /// asset name.
        /// </summary>
        public static DistrictData District(CampaignTravelData travel, string name)
        {
            if (travel == null || string.IsNullOrEmpty(name))
                return null;
            if (travel.Headquarters != null && travel.Headquarters.name == name)
                return travel.Headquarters;
            foreach (var road in travel.Roads)
            {
                if (road?.From != null && road.From.name == name)
                    return road.From;
                if (road?.To != null && road.To.name == name)
                    return road.To;
            }
            return null;
        }
    }
}
