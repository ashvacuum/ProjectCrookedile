using System;
using System.Collections.Generic;
using System.Linq;
using Crookedile.Data;
using Crookedile.Data.Audio;
using Crookedile.Data.Battle;
using Crookedile.Data.Campaign;
using Crookedile.Data.Cards;
using Crookedile.Data.Enemy;
using Crookedile.Data.Localization;
using Crookedile.Data.VFX;
using Crookedile.Gameplay.Battle;
using UnityEditor;
using UnityEngine;

namespace Crookedile.EditorTools
{
    /// <summary>
    /// Project audits, one provider per kind of content. Each provider returns rows of item, detail
    /// and issues; the Database window shows them, either inside a type's own tab (its audit comes
    /// from the matching provider) or in the Checks tab. To add a check: an <see cref="IContentProvider"/>
    /// listed in <see cref="CheckProviders"/>.
    /// </summary>
    public static class ContentChecks
    {
        public enum Severity
        {
            Ok,
            Info,
            Warning,
            Error,
        }

        public readonly struct AuditIssue
        {
            public readonly Severity Severity;
            public readonly string Message;

            public AuditIssue(Severity severity, string message)
            {
                Severity = severity;
                Message = message;
            }
        }

        /// <summary>One entry in a category — a content item plus any problems found with it.</summary>
        public readonly struct Row
        {
            public readonly string Label;
            public readonly string Detail;
            public readonly UnityEngine.Object Context;
            public readonly List<AuditIssue> Issues;

            /// <summary>Optional sprite shown as a thumbnail at the left of the row.</summary>
            public readonly Sprite Thumbnail;

            public Row(
                string label,
                string detail,
                UnityEngine.Object context,
                List<AuditIssue> issues,
                Sprite thumbnail = null
            )
            {
                Label = label;
                Detail = detail;
                Context = context;
                Issues = issues ?? new List<AuditIssue>();
                Thumbnail = thumbnail;
            }

            public Severity Worst => Issues.Count == 0 ? Severity.Ok : Issues.Max(i => i.Severity);
        }

        public interface IContentProvider
        {
            string Category { get; }
            IEnumerable<Row> Rows();
        }

        /// <summary>The providers shown in the Checks tab: audits with no tab of their own.</summary>
        public static List<IContentProvider> CheckProviders() =>
            new List<IContentProvider>
            {
                new ReadinessProvider(),
                new ContentAssetNaming(),
                new EnemyMovesProvider(),
                new BossesProvider(),
                new EncounterPoolsProvider(),
                new OriginPassivesProvider(),
                new SharedArtProvider(),
                new DuplicateEffectsProvider(),
                new CardVisualsProvider(),
                new IntentsProvider(),
                new AudioVfxProvider(),
                new AudioVfxEventsProvider(),
                new LocalizationProvider(),
                new RewardProvider(),
                new UIRefsAuditProvider(),
            };

        internal static List<T> LoadAll<T>()
            where T : UnityEngine.Object
        {
            return AssetDatabase
                .FindAssets("t:" + typeof(T).Name)
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(o => o != null)
                .ToList();
        }

        internal sealed class BossesProvider : IContentProvider
        {
            public string Category => "Bosses";

            public IEnumerable<Row> Rows()
            {
                foreach (var boss in LoadAll<Crookedile.Data.Boss.BossData>())
                {
                    var issues = new List<AuditIssue>();
                    foreach (var issue in boss.GetConfigurationIssues())
                        issues.Add(new AuditIssue(Severity.Error, issue));
                    yield return new Row(
                        boss.DisplayName,
                        $"{boss.Bundles.Count} bundles",
                        boss,
                        issues,
                        boss.Portrait
                    );
                }
            }
        }

        internal static T LoadFirst<T>()
            where T : UnityEngine.Object => LoadAll<T>().FirstOrDefault();

        // -----------------------------------------------------------------
        // Providers
        // -----------------------------------------------------------------

        /// <summary>
        /// Asset-level readiness for a playable battle: each origin has a starter deck and a wired
        /// passive, and the project has enemies and at least one encounter. (Scene wiring — the
        /// BattleManager's passive array / intent theme / overlay text slots / BattleTestStarter
        /// session — can't be asset-scanned; verify those in the scene.)
        /// </summary>

        internal sealed class ReadinessProvider : IContentProvider
        {
            public string Category => "Readiness";

            public IEnumerable<Row> Rows()
            {
                var cards = LoadAll<CardData>();
                var passives = LoadAll<OriginPassive>();

                foreach (OriginType origin in Enum.GetValues(typeof(OriginType)))
                {
                    // The deck a run actually starts with: OriginDatabase's authored deck, or
                    // tagged starter cards when the origin has none.
                    int deck = CardDatabase.Shared?.GetStarterDeck(origin)?.Count ?? 0;
                    var deckIssues = new List<AuditIssue>();
                    if (deck == 0)
                        deckIssues.Add(
                            new AuditIssue(
                                Severity.Error,
                                "No starter deck (author one on OriginDatabase)."
                            )
                        );
                    else if (deck < 5)
                        deckIssues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "Fewer than 5 starter cards — thin deck."
                            )
                        );
                    yield return new Row(
                        $"{origin} starter deck",
                        $"{deck} card(s)",
                        null,
                        deckIssues
                    );

                    var p = passives.FirstOrDefault(x => x.Origin == origin);
                    var passIssues = new List<AuditIssue>();
                    if (p == null)
                        passIssues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No OriginPassive asset for this origin."
                            )
                        );
                    else if (p.Passives == null || p.Passives.Count == 0)
                        passIssues.Add(
                            new AuditIssue(Severity.Warning, "OriginPassive has no passives wired.")
                        );
                    yield return new Row(
                        $"{origin} passive",
                        p != null ? p.name : "(none)",
                        p,
                        passIssues
                    );
                }

                int enemies = LoadAll<EnemyData>().Count(e => e.Moves != null && e.Moves.Count > 0);
                var enemyIssues = new List<AuditIssue>();
                if (enemies == 0)
                    enemyIssues.Add(
                        new AuditIssue(Severity.Error, "No enemies with moves — nothing to fight.")
                    );
                yield return new Row("Enemies with moves", $"{enemies}", null, enemyIssues);

                var sessions = LoadAll<BattleSession>();
                int playable = sessions.Count(s =>
                    s.rounds != null
                    && s.rounds.Any(r => r != null && r.enemies != null && r.enemies.Count > 0)
                );
                var sessionIssues = new List<AuditIssue>();
                if (sessions.Count == 0)
                    sessionIssues.Add(
                        new AuditIssue(
                            Severity.Info,
                            "No BattleSession — BattleTestStarter must use its own enemies list."
                        )
                    );
                else if (playable == 0)
                    sessionIssues.Add(
                        new AuditIssue(
                            Severity.Warning,
                            "BattleSession(s) exist but no round has enemies."
                        )
                    );
                yield return new Row(
                    "Encounters (BattleSession)",
                    $"{playable}/{sessions.Count} playable",
                    null,
                    sessionIssues
                );
            }
        }

        internal sealed class EnemyMovesProvider : IContentProvider
        {
            public string Category => "Enemy moves";

            public IEnumerable<Row> Rows()
            {
                foreach (var move in LoadAll<EnemyMoveData>().OrderBy(m => m.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(move.IntentDescription))
                        issues.Add(new AuditIssue(Severity.Warning, "No intent description."));
                    bool needsEffects =
                        move.MoveType != EnemyMoveType.Idle
                        && move.MoveType != EnemyMoveType.SummonMinion;
                    if (needsEffects && (move.Effects == null || move.Effects.Count == 0))
                        issues.Add(new AuditIssue(Severity.Warning, "No effects."));
                    if (move.MoveType == EnemyMoveType.SummonMinion && move.MinionToSummon == null)
                        issues.Add(new AuditIssue(Severity.Error, "Summon move has no minion."));
                    yield return new Row(move.name, move.MoveType.ToString(), move, issues);
                }
            }
        }

        /// <summary>
        /// Cards whose base effect lists are identical: same effect types, same order, same tuning
        /// values. Upgraded effects are compared separately so a card that only differs once
        /// upgraded still shows up under its base list.
        /// </summary>
        internal sealed class DuplicateEffectsProvider : IContentProvider
        {
            public string Category => "Duplicate effects";

            // ponytail: JsonUtility signature; nested [SerializeReference] fields may serialize by
            // reference id and miss a match. Switch to SerializedProperty walking if that bites.
            private static string Signature(List<BattleEffect> effects) =>
                effects == null || effects.Count == 0
                    ? null
                    : string.Join(
                        "|",
                        effects.Select(e =>
                            e == null ? "null" : e.GetType().FullName + JsonUtility.ToJson(e)
                        )
                    );

            private static string Summary(List<BattleEffect> effects) =>
                string.Join(", ", effects.Select(e => e?.GetType().Name ?? "(empty)"));

            public IEnumerable<Row> Rows()
            {
                var cards = LoadAll<CardData>().Distinct().ToList();
                bool any = false;
                foreach (
                    var (label, pick) in new (string, Func<CardData, List<BattleEffect>>)[]
                    {
                        ("base", c => c.Effects),
                        ("upgraded", c => c.UpgradedEffects),
                    }
                )
                {
                    // An upgraded list that copies the card's own base list is not a second
                    // duplicate; the base pass already reports it.
                    var groups = cards
                        .Select(c => (card: c, sig: Signature(pick(c))))
                        .Where(x =>
                            x.sig != null
                            && (label == "base" || x.sig != Signature(x.card.Effects))
                        )
                        .GroupBy(x => x.sig)
                        .Where(g => g.Count() > 1)
                        .OrderByDescending(g => g.Count());

                    foreach (var group in groups)
                    {
                        any = true;
                        var members = group.Select(x => x.card).ToList();
                        string summary = Summary(pick(members[0]));
                        foreach (var card in members)
                            yield return new Row(
                                card.name,
                                $"{label}: {summary}  —  shared by {members.Count}",
                                card,
                                new List<AuditIssue>
                                {
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Same {label} effects as: "
                                            + string.Join(
                                                ", ",
                                                members.Where(o => o != card).Select(o => o.name)
                                            )
                                    ),
                                },
                                card.Artwork
                            );
                    }
                }

                if (!any)
                    yield return new Row(
                        "(none)",
                        "every card has a unique effect list",
                        null,
                        new List<AuditIssue>()
                    );
            }
        }

        /// <summary>
        /// Cross-content art duplication: collects every art reference across the project
        /// (card artwork, enemy portraits, status icons, intent icons, ally icons) and flags any
        /// sprite asset used by more than one item. Sharing art is sometimes intentional (a
        /// placeholder, or a deliberately reused icon) and sometimes a copy-paste mistake — this tab
        /// surfaces every case so you can decide. All-green means every flagged item has unique art.
        /// </summary>
        internal sealed class SharedArtProvider : IContentProvider
        {
            public string Category => "Shared art";

            // One art reference: which sprite, on which content item, and a human label for it.
            private readonly struct ArtRef
            {
                public readonly Sprite Sprite;
                public readonly string Label;
                public readonly UnityEngine.Object Context;

                public ArtRef(Sprite sprite, string label, UnityEngine.Object context)
                {
                    Sprite = sprite;
                    Label = label;
                    Context = context;
                }
            }

            private static IEnumerable<ArtRef> Collect()
            {
                foreach (var card in LoadAll<CardData>())
                    if (card.Artwork != null)
                        yield return new ArtRef(card.Artwork, $"Card: {card.name}", card);

                foreach (var enemy in LoadAll<EnemyData>())
                    if (enemy.Portrait != null)
                        yield return new ArtRef(
                            enemy.Portrait,
                            $"Enemy: {enemy.EnemyName ?? enemy.name}",
                            enemy
                        );

                foreach (var ally in LoadAll<AllyData>())
                    if (ally.Icon != null)
                        yield return new ArtRef(
                            ally.Icon,
                            $"Ally: {ally.AllyName ?? ally.name}",
                            ally
                        );

                var iconMap = LoadFirst<StatusEffectIconMapSO>();
                if (iconMap != null)
                    foreach (StatusBehavior behavior in StatusRegistry.All)
                        if (iconMap.TryGet(behavior.Id, out var icon, out _) && icon != null)
                            yield return new ArtRef(
                                icon,
                                $"Status: {behavior.DisplayName}",
                                iconMap
                            );

                var theme = LoadFirst<EnemyIntentTheme>();
                if (theme != null)
                    foreach (EnemyMoveType type in Enum.GetValues(typeof(EnemyMoveType)))
                    {
                        var icon = theme.GetVisual(type).icon;
                        if (icon != null)
                            yield return new ArtRef(icon, $"Intent: {type}", theme);
                    }
            }

            public IEnumerable<Row> Rows()
            {
                // Group by the underlying sprite asset; only sprites shared by 2+ items matter.
                var groups = Collect()
                    .GroupBy(r => r.Sprite)
                    .Where(g => g.Count() > 1)
                    .OrderByDescending(g => g.Count());

                bool any = false;
                foreach (var group in groups)
                {
                    any = true;
                    var members = group.ToList();
                    string sprite = group.Key != null ? group.Key.name : "(missing)";
                    foreach (var m in members)
                    {
                        var others = members
                            .Where(o => o.Context != m.Context || o.Label != m.Label)
                            .Select(o => o.Label);
                        yield return new Row(
                            m.Label,
                            $"art '{sprite}'  —  shared by {members.Count}",
                            m.Context,
                            new List<AuditIssue>
                            {
                                new AuditIssue(
                                    Severity.Warning,
                                    $"Shares art '{sprite}' with: {string.Join(", ", others)}"
                                ),
                            },
                            group.Key
                        );
                    }
                }

                if (!any)
                    yield return new Row(
                        "(none)",
                        "every item has unique art",
                        null,
                        new List<AuditIssue>()
                    );
            }
        }

        internal sealed class IntentsProvider : IContentProvider
        {
            public string Category => "Intents";

            public IEnumerable<Row> Rows()
            {
                var theme = LoadFirst<EnemyIntentTheme>();
                foreach (EnemyMoveType type in Enum.GetValues(typeof(EnemyMoveType)))
                {
                    var issues = new List<AuditIssue>();
                    if (theme == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No EnemyIntentTheme asset."));
                    else if (theme.GetVisual(type).icon == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No icon in theme."));
                    yield return new Row(type.ToString(), "", theme, issues);
                }
            }
        }

        internal sealed class AudioVfxProvider : IContentProvider
        {
            public string Category => "Audio / VFX";

            public IEnumerable<Row> Rows()
            {
                var map = LoadFirst<BattleSoundMap>();
                foreach (BattleAudioTrigger trigger in Enum.GetValues(typeof(BattleAudioTrigger)))
                {
                    var issues = new List<AuditIssue>();
                    string detail = "unmapped";
                    if (map == null)
                        issues.Add(new AuditIssue(Severity.Error, "No BattleSoundMap asset."));
                    else if (!map.TryGet(trigger, out var entry))
                        issues.Add(new AuditIssue(Severity.Warning, "Unmapped — no sound or VFX."));
                    else
                    {
                        detail =
                            $"{(entry.Sound != null ? "sfx" : "—")} / {(entry.Visual != null ? "vfx" : "—")}";
                        if (entry.Sound == null && entry.Visual == null)
                            issues.Add(new AuditIssue(Severity.Info, "Mapped but empty."));
                    }
                    yield return new Row(trigger.ToString(), detail, map, issues);
                }
            }
        }

        internal sealed class AlliesProvider : IContentProvider
        {
            public string Category => "Allies";

            public IEnumerable<Row> Rows()
            {
                var allies = LoadAll<AllyData>();
                if (allies.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no allies authored yet — create them via Assets → Create → Crookedile",
                        null,
                        new List<AuditIssue>()
                    );
                    yield break;
                }

                var databases = LoadAll<AllyDatabase>();
                var seenIds = new Dictionary<string, string>();
                foreach (var ally in allies.OrderBy(r => r.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(ally.AllyName))
                        issues.Add(new AuditIssue(Severity.Warning, "No display name."));
                    if (ally.Icon == null)
                        issues.Add(new AuditIssue(Severity.Info, "No icon."));

                    // Id must be unique — AllyDatabase indexes by it (last one wins silently).
                    if (string.IsNullOrEmpty(ally.Id))
                        issues.Add(new AuditIssue(Severity.Error, "Empty id."));
                    else if (seenIds.TryGetValue(ally.Id, out var other))
                        issues.Add(
                            new AuditIssue(Severity.Error, $"Duplicate id (also on {other}).")
                        );
                    else
                        seenIds[ally.Id] = ally.name;

                    // Unregistered allies are invisible to acquisition (boss/event offers).
                    if (!databases.Any(db => db.Allies != null && db.Allies.Contains(ally)))
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "Not in any AllyDatabase — acquisition can't offer it."
                            )
                        );

                    if (ally.OverworldPassives != null)
                    {
                        foreach (var passive in ally.OverworldPassives)
                        {
                            if (passive == null)
                            {
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        "Overworld passive has no type selected."
                                    )
                                );
                            }
                        }
                    }

                    if (ally.Passives == null || ally.Passives.Count == 0)
                    {
                        if (!ally.HasOverworldPassives)
                        {
                            issues.Add(
                                new AuditIssue(
                                    Severity.Warning,
                                    "No battle or overworld passives (does nothing)."
                                )
                            );
                        }
                    }
                    else
                    {
                        // A passive without a trigger is never bucketed by PassiveResolver;
                        // without effects it fires into nothing. Both are silent at runtime.
                        foreach (var bp in ally.Passives)
                        {
                            if (bp == null)
                                continue;
                            if (bp.Trigger == null)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive '{bp.Name}' has no trigger (never fires)."
                                    )
                                );
                            if (bp.Effects == null || bp.Effects.Count == 0)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive '{bp.Name}' has no effects."
                                    )
                                );
                        }
                    }

                    yield return new Row(
                        ally.AllyName ?? ally.name,
                        ally.Rarity.ToString(),
                        ally,
                        issues,
                        ally.Icon
                    );
                }
            }
        }

        internal sealed class RewardProvider : IContentProvider
        {
            public string Category => "Reward config";

            public IEnumerable<Row> Rows()
            {
                var configs = LoadAll<RewardConfig>();
                if (configs.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "rewards use hardcoded weights in CardDatabase",
                        null,
                        new List<AuditIssue>
                        {
                            new AuditIssue(Severity.Info, "No RewardConfig asset."),
                        }
                    );
                    yield break;
                }
                foreach (var cfg in configs)
                {
                    var issues = new List<AuditIssue>();
                    if (!cfg.IsValid)
                        issues.Add(
                            new AuditIssue(Severity.Error, "Weights sum to 0 or offer count < 1.")
                        );
                    yield return new Row(
                        cfg.name,
                        $"B{cfg.BasicWeight}/E{cfg.EnhancedWeight}/R{cfg.RareWeight}  x{cfg.DefaultOfferCount}",
                        cfg,
                        issues
                    );
                }
            }
        }

        /// <summary>
        /// Battle sessions: every <see cref="BattleSession"/> and its rounds. Flags empty
        /// sessions, rounds with no enemies, null enemy slots, and rounds over the 5-enemy
        /// display cap.
        ///
        /// Named "Battle sessions", not "Encounters": a session is a test-harness gauntlet.
        /// Campaign encounters are audited by <see cref="CampaignEncountersProvider"/>.
        /// </summary>
        internal sealed class EncountersProvider : IContentProvider
        {
            public string Category => "Battle sessions";

            public IEnumerable<Row> Rows()
            {
                var sessions = LoadAll<BattleSession>();
                if (sessions.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no BattleSession assets — BattleTestStarter uses its own enemies list",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "No encounters.") }
                    );
                    yield break;
                }

                foreach (var session in sessions.OrderBy(s => s.name))
                {
                    var issues = new List<AuditIssue>();
                    int rounds = session.RoundCount;
                    if (rounds == 0)
                        issues.Add(new AuditIssue(Severity.Error, "No rounds."));

                    for (int i = 0; i < rounds; i++)
                    {
                        var round = session.GetRound(i);
                        if (round?.boss != null)
                            foreach (var issue in round.boss.GetConfigurationIssues())
                                issues.Add(
                                    new AuditIssue(Severity.Error, $"{round.label}: {issue}")
                                );
                        int enemies = round?.enemies?.Count ?? 0;
                        string label = string.IsNullOrWhiteSpace(round?.label)
                            ? $"Round {i + 1}"
                            : round.label;
                        if (enemies == 0)
                            issues.Add(new AuditIssue(Severity.Error, $"{label}: no enemies."));
                        else
                        {
                            if (round.enemies.Any(e => e == null))
                                issues.Add(
                                    new AuditIssue(Severity.Warning, $"{label}: empty enemy slot.")
                                );
                            if (enemies > 5)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"{label}: {enemies} enemies (display cap is 5)."
                                    )
                                );
                        }
                    }

                    yield return new Row(session.name, $"{rounds} round(s)", session, issues);
                }
            }
        }

        /// <summary>
        /// Campaign encounters: every <see cref="EncounterData"/> asset, battles and events.
        /// Catches the failures this content is actually prone to — an event with no options
        /// (unleavable), an option that does nothing and says nothing, a battle encounter with
        /// no session (refuses to start), and half-picked <c>[SerializeReference]</c> rows.
        /// </summary>
        internal sealed class CampaignEncountersProvider : IContentProvider
        {
            public string Category => "Campaign encounters";

            public IEnumerable<Row> Rows()
            {
                var encounters = LoadAll<EncounterData>();
                if (encounters.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no EncounterData assets authored yet",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "No encounters.") }
                    );
                    yield break;
                }

                foreach (var encounter in encounters.OrderBy(e => e.name))
                {
                    var issues = new List<AuditIssue>();

                    if (string.IsNullOrEmpty(encounter.ID))
                        issues.Add(
                            new AuditIssue(
                                Severity.Error,
                                "No ID — visited-state and pool exclusions key off it."
                            )
                        );
                    if (string.IsNullOrWhiteSpace(encounter.DisplayName))
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No display name (falls back to asset name)."
                            )
                        );

                    string detail = encounter.GetType().Name;

                    switch (encounter)
                    {
                        case BattleEncounterData battle:
                            detail = battle.IsBoss ? "Battle (BOSS)" : "Battle";
                            bool hasRival =
                                battle.Session != null
                                && battle.Session.rounds.Any(r => r?.boss != null);
                            if (battle.IsBoss != hasRival)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Error,
                                        "Boss classification must match the session's assigned rival."
                                    )
                                );
                            if (battle.Session == null)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Error,
                                        "No BattleSession — this encounter refuses to start."
                                    )
                                );
                            else if (battle.Session.RoundCount > 1)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Session has {battle.Session.RoundCount} rounds. Campaign "
                                            + "encounters should wrap one fight — chain with a "
                                            + "GoToEncounterOutcome instead."
                                    )
                                );
                            break;

                        case EventEncounterData evt:
                            detail = $"Event, {evt.Options.Count} option(s)";
                            if (string.IsNullOrWhiteSpace(evt.Body))
                                issues.Add(new AuditIssue(Severity.Warning, "No body text."));
                            if (evt.Options.Count == 0)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Error,
                                        "No options — the player can't leave this event."
                                    )
                                );
                            AuditOptions(evt, issues);
                            break;
                    }

                    yield return new Row(encounter.name, detail, encounter, issues);
                }
            }

            private static void AuditOptions(EventEncounterData evt, List<AuditIssue> issues)
            {
                for (int i = 0; i < evt.Options.Count; i++)
                {
                    var option = evt.Options[i];
                    string tag = $"Option {i + 1}";
                    if (option == null)
                    {
                        issues.Add(new AuditIssue(Severity.Error, $"{tag}: null."));
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(option.Label))
                        issues.Add(
                            new AuditIssue(Severity.Error, $"{tag}: no label — blank button.")
                        );

                    // A null entry is a row added in the inspector whose type was never picked.
                    if (option.Outcomes.Any(o => o == null))
                        issues.Add(
                            new AuditIssue(Severity.Error, $"{tag}: an outcome has no type picked.")
                        );
                    if (option.Requirements.Any(r => r == null))
                        issues.Add(
                            new AuditIssue(
                                Severity.Error,
                                $"{tag}: a requirement has no type picked."
                            )
                        );

                    if (option.Outcomes.Count == 0 && string.IsNullOrWhiteSpace(option.ResultText))
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                $"{tag}: no outcomes and no result text — picking it does and says nothing."
                            )
                        );
                }
            }
        }

        /// <summary>
        /// Encounter pools: the scheduling layer. The headline check is day coverage — a day
        /// with nothing eligible hands the player an empty map, and it's invisible from the
        /// asset inspector.
        /// </summary>
        internal sealed class EncounterPoolsProvider : IContentProvider
        {
            public string Category => "Encounter pools";

            public IEnumerable<Row> Rows()
            {
                var pools = LoadAll<EncounterPoolData>();
                if (pools.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no EncounterPoolData assets",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "No pools.") }
                    );
                    yield break;
                }

                foreach (var pool in pools.OrderBy(p => p.name))
                {
                    var issues = new List<AuditIssue>();

                    // Ids present in this pool — a dependency on anything outside it can never
                    // be satisfied by playing, which is silent at runtime.
                    var idsInPool = new HashSet<string>(
                        pool.Entries.Where(e => e?.Encounter != null).Select(e => e.Id)
                    );

                    for (int i = 0; i < pool.Entries.Count; i++)
                    {
                        var entry = pool.Entries[i];
                        string tag = $"Entry {i + 1}";
                        if (entry?.Encounter == null)
                        {
                            issues.Add(new AuditIssue(Severity.Error, $"{tag}: no encounter set."));
                            continue;
                        }

                        tag = entry.Encounter.name;

                        if (entry.FirstDay > pool.Days)
                            issues.Add(
                                new AuditIssue(
                                    Severity.Warning,
                                    $"{tag}: starts day {entry.FirstDay}, past the pool's {pool.Days} days — unreachable."
                                )
                            );
                        if (entry.LastDay > 0 && entry.LastDay < entry.FirstDay)
                            issues.Add(
                                new AuditIssue(
                                    Severity.Error,
                                    $"{tag}: last day {entry.LastDay} is before first day {entry.FirstDay}."
                                )
                            );
                        if (!entry.Guaranteed && entry.ResolvedWeight <= 0f)
                            issues.Add(
                                new AuditIssue(
                                    Severity.Warning,
                                    $"{tag}: weight 0 and not guaranteed — can never be drawn."
                                )
                            );
                        // Existing assets deserialize a missing multiplier as 0, which would
                        // zero the weight of anything it's meant to favour.
                        if (entry.BoostIf.Count > 0 && entry.BoostMultiplier <= 0f)
                            issues.Add(
                                new AuditIssue(
                                    Severity.Error,
                                    $"{tag}: boost multiplier is {entry.BoostMultiplier:0.##} — the boost erases the entry instead of favouring it."
                                )
                            );

                        foreach (var req in entry.Requirements.Concat(entry.BoostIf))
                        {
                            if (req == null)
                            {
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Error,
                                        $"{tag}: a condition has no type picked."
                                    )
                                );
                                continue;
                            }
                            if (
                                req is HasVisitedEncounter visited
                                && visited.Encounter != null
                                && !idsInPool.Contains(visited.Encounter.ID)
                            )
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"{tag}: depends on '{visited.Encounter.name}', which isn't in this pool — can never be satisfied here."
                                    )
                                );
                        }
                    }

                    // Coverage: the failure the whole scheduling layer is prone to.
                    var emptyDays = new List<int>();
                    for (int day = 1; day <= pool.Days; day++)
                        if (!pool.EligibleOn(day).Any())
                            emptyDays.Add(day);

                    if (emptyDays.Count > 0)
                        issues.Add(
                            new AuditIssue(
                                Severity.Error,
                                $"Nothing eligible on day(s) {string.Join(", ", emptyDays)} — empty map."
                            )
                        );

                    bool hasBoss = pool.Entries.Any(e =>
                        e?.Encounter is BattleEncounterData b && b.IsBoss
                    );
                    if (!hasBoss)
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                "No boss encounter — the run has no finale, and End Day never becomes Face the boss."
                            )
                        );

                    yield return new Row(
                        pool.name,
                        $"{pool.Days} days, {pool.Entries.Count} entries",
                        pool,
                        issues
                    );
                }
            }
        }

        /// <summary>
        /// Card visuals: the shared CardVisualSettings (every back/frame slot filled) and each
        /// CardVisualAtlas (texture set, mapping entries complete).
        /// </summary>
        internal sealed class CardVisualsProvider : IContentProvider
        {
            public string Category => "Card visuals";

            public IEnumerable<Row> Rows()
            {
                var settings = LoadFirst<CardVisualSettings>();
                if (settings == null)
                {
                    yield return new Row(
                        "Card Visual Settings",
                        "(none)",
                        null,
                        new List<AuditIssue>
                        {
                            new AuditIssue(Severity.Warning, "No CardVisualSettings asset."),
                        }
                    );
                }
                else
                {
                    var issues = new List<AuditIssue>();
                    if (settings.DefaultCardBack == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No default card back."));
                    foreach (
                        CardType type in new[]
                        {
                            CardType.Pressure,
                            CardType.Rhetoric,
                            CardType.Policy,
                            CardType.Heckle,
                            CardType.Scandal,
                        }
                    )
                        if (settings.GetFrameForType(type) == null)
                            issues.Add(
                                new AuditIssue(Severity.Info, $"No frame for {type} cards.")
                            );
                    foreach (
                        CardRarity rarity in new[]
                        {
                            CardRarity.Basic,
                            CardRarity.Enhanced,
                            CardRarity.Rare,
                        }
                    )
                        if (settings.GetFrameForRarity(rarity) == null)
                            issues.Add(new AuditIssue(Severity.Info, $"No {rarity} rarity frame."));
                    yield return new Row("Card Visual Settings", settings.name, settings, issues);
                }

                foreach (var atlas in LoadAll<CardVisualAtlas>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    if (atlas.AtlasTexture == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No atlas texture assigned."));

                    var so = new SerializedObject(atlas);
                    var mappings = so.FindProperty("cardMappings");
                    int blankId = 0,
                        noRef = 0;
                    if (mappings != null)
                    {
                        for (int i = 0; i < mappings.arraySize; i++)
                        {
                            var entry = mappings.GetArrayElementAtIndex(i);
                            if (
                                string.IsNullOrWhiteSpace(
                                    entry.FindPropertyRelative("cardId")?.stringValue
                                )
                            )
                                blankId++;
                            if (
                                entry.FindPropertyRelative("cardReference")?.objectReferenceValue
                                == null
                            )
                                noRef++;
                        }
                    }
                    if (blankId > 0)
                        issues.Add(
                            new AuditIssue(
                                Severity.Warning,
                                $"{blankId} mapping(s) with blank cardId."
                            )
                        );
                    if (noRef > 0)
                        issues.Add(
                            new AuditIssue(
                                Severity.Info,
                                $"{noRef} mapping(s) with no CardData reference."
                            )
                        );

                    int count = mappings?.arraySize ?? 0;
                    yield return new Row(atlas.name, $"{count} mapping(s)", atlas, issues);
                }
            }
        }

        /// <summary>
        /// Origin passives: validates each OriginPassive asset is wired (has passives, each with a
        /// trigger and at least one effect), and surfaces the known Faith Leader timing caveat.
        /// </summary>
        internal sealed class OriginPassivesProvider : IContentProvider
        {
            public string Category => "Origin passives";

            public IEnumerable<Row> Rows()
            {
                var passives = LoadAll<OriginPassive>();
                if (passives.Count == 0)
                {
                    yield return new Row(
                        "(none)",
                        "no OriginPassive assets",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Warning, "None found.") }
                    );
                    yield break;
                }

                foreach (var op in passives.OrderBy(p => p.Origin))
                {
                    var issues = new List<AuditIssue>();
                    var list = op.Passives;
                    if (list == null || list.Count == 0)
                        issues.Add(
                            new AuditIssue(Severity.Error, "No passives wired (does nothing).")
                        );
                    else
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var bp = list[i];
                            if (bp == null)
                            {
                                issues.Add(
                                    new AuditIssue(Severity.Warning, $"Passive #{i + 1} is null.")
                                );
                                continue;
                            }
                            if (bp.Trigger == null)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive #{i + 1} has no trigger."
                                    )
                                );
                            if (bp.Effects == null || bp.Effects.Count == 0)
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Warning,
                                        $"Passive #{i + 1} has no effects."
                                    )
                                );
                            // Known caveat: SupportGainedTrigger fires on ANY Support increase, so a
                            // turn-1 receptive/Ritual Support gain can consume a "first time" passive
                            // before the intended Support card.
                            if (
                                bp.Trigger != null
                                && bp.Trigger.GetType().Name == "SupportGainedTrigger"
                            )
                                issues.Add(
                                    new AuditIssue(
                                        Severity.Info,
                                        "SupportGainedTrigger fires on any Support increase — verify a "
                                            + "turn-1 Support gain can't consume a first-time passive early."
                                    )
                                );
                        }
                    }

                    yield return new Row(
                        string.IsNullOrWhiteSpace(op.PassiveName) ? op.name : op.PassiveName,
                        $"{op.Origin} — {list?.Count ?? 0} passive(s)",
                        op,
                        issues,
                        op.Icon
                    );
                }
            }
        }

        /// <summary>
        /// Audio/VFX event assets (distinct from the trigger-coverage check): AudioEvent and
        /// AudioClipData with no clip, and VFXEvent set to None (a no-op).
        /// </summary>
        internal sealed class AudioVfxEventsProvider : IContentProvider
        {
            public string Category => "Audio / VFX events";

            public IEnumerable<Row> Rows()
            {
                foreach (var evt in LoadAll<AudioEvent>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    var clip = new SerializedObject(evt)
                        .FindProperty("_clip")
                        ?.objectReferenceValue;
                    if (clip == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No clip (no-op)."));
                    yield return new Row(evt.name, "AudioEvent", evt, issues);
                }

                foreach (var clip in LoadAll<AudioClipData>().OrderBy(a => a.name))
                {
                    var issues = new List<AuditIssue>();
                    if (clip.Clip == null)
                        issues.Add(new AuditIssue(Severity.Warning, "No clip."));
                    if (string.IsNullOrWhiteSpace(clip.ClipName))
                        issues.Add(new AuditIssue(Severity.Info, "No lookup name."));
                    yield return new Row(
                        string.IsNullOrWhiteSpace(clip.ClipName) ? clip.name : clip.ClipName,
                        "AudioClipData",
                        clip,
                        issues
                    );
                }

                foreach (var vfx in LoadAll<VFXEvent>().OrderBy(v => v.name))
                {
                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrEmpty(vfx.AnimationStateName))
                        issues.Add(
                            new AuditIssue(Severity.Info, "Animation state is None (no visual).")
                        );
                    yield return new Row(vfx.name, "VFXEvent", vfx, issues);
                }
            }
        }

        /// <summary>
        /// Localization: every entry in the LocalizationData table — flags blank keys, duplicate
        /// keys, and missing English / Tagalog text.
        /// </summary>
        internal sealed class LocalizationProvider : IContentProvider
        {
            public string Category => "Localization";

            public IEnumerable<Row> Rows()
            {
                var data = LoadFirst<LocalizationData>();
                if (data == null)
                {
                    yield return new Row(
                        "(none)",
                        "no LocalizationData asset",
                        null,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "None found.") }
                    );
                    yield break;
                }

                var strings = new SerializedObject(data).FindProperty("_strings");
                int count = strings?.arraySize ?? 0;
                var seen = new HashSet<string>();

                for (int i = 0; i < count; i++)
                {
                    var entry = strings.GetArrayElementAtIndex(i);
                    string key = entry.FindPropertyRelative("_key")?.stringValue ?? "";
                    string english = entry.FindPropertyRelative("_english")?.stringValue ?? "";
                    string tagalog = entry.FindPropertyRelative("_tagalog")?.stringValue ?? "";

                    var issues = new List<AuditIssue>();
                    if (string.IsNullOrWhiteSpace(key))
                        issues.Add(new AuditIssue(Severity.Error, "Blank key."));
                    else if (!seen.Add(key))
                        issues.Add(new AuditIssue(Severity.Error, "Duplicate key."));
                    if (string.IsNullOrWhiteSpace(english))
                        issues.Add(new AuditIssue(Severity.Warning, "No English text."));
                    if (string.IsNullOrWhiteSpace(tagalog))
                        issues.Add(new AuditIssue(Severity.Info, "No Tagalog text."));

                    yield return new Row(
                        string.IsNullOrWhiteSpace(key) ? $"(entry {i})" : key,
                        english.Length > 40 ? english.Substring(0, 40) + "…" : english,
                        data,
                        issues
                    );
                }

                if (count == 0)
                    yield return new Row(
                        data.name,
                        "no strings",
                        data,
                        new List<AuditIssue> { new AuditIssue(Severity.Info, "Empty table.") }
                    );
            }
        }
    }
}
