# Crookedile

*A Filipino political roguelite deck-builder. Working title (formerly "Palakasan" / "Philippine Political Card Game").*

> You are not winning a fight. You are working a crowd — managing who speaks, how loudly, and in what direction they push public opinion.

---

> [!IMPORTANT]
> **The design is in active flux.** The game went through a major combat + class redesign. Everything under `docs/` is current. Everything under [`docs/deprecated/`](docs/deprecated/README.md) predates that redesign and is not in use — kept only because some of its ideas are still worth mining.

## Start here

| Doc | What it settles |
|---|---|
| [`docs/core-design.md`](docs/core-design.md) | Core fantasy, the **Opinion Meter**, hostility, the **Echo Chamber** rule, voice intents, turn structure, the three archetypes |
| [`docs/needs-detailing.md`](docs/needs-detailing.md) | The open design calls, ordered by how much they block — read before authoring content |
| [`docs/naming-glossary.md`](docs/naming-glossary.md) | Combat vocabulary → "working a crowd" vocabulary. Read before naming anything |

## What the game is

- **Roguelite deck-builder**, political satire. The card **battles are the core**, wrapped in an **overworld campaign**: navigate a town map and accumulate **Support** toward winning the election by a deadline.
- **No HP.** The per-battle battleground is a shared **Opinion Meter** (win at 100, lose at 0, Judgment at the turn limit). Directional session shields — **Support** (guards against drops) and **Denial** (guards against rises) — protect it. *(Per-battle "Support" the shield is distinct from campaign "Support points" the win condition — a naming overlap to resolve.)*
- **Hostility** is a signed per-enemy stance you manage (hostile ↔ receptive). The central tension is the **Echo Chamber**: convert the *whole* room and your gains halve and your lead decays — so you always want a villain present.
- **Three archetypes:** **Nepo Baby** (a glass cannon who burns, pulls and replays his own deck, paid for in the room's Hostility), **Celebrity** (an "open canvas" drafting into Attention / Scandal / Drama King), **Faith Leader** (stack statuses to convert enemies into one-turn meter-pumping followers).

---

## Documentation map

### Design — canonical
- [`core-design.md`](docs/core-design.md) — the combat model and the three archetypes.
- [`crookedile-starter-decks.md`](docs/crookedile-starter-decks.md) — per-class starter decks and the reward-pool "potential" layer.
- [`nepo-baby-class.md`](docs/nepo-baby-class.md) — the Nepo Baby class: burn / return / calm lanes, all 42 cards, its config, and build notes.
- [`celebrity-glamour-iou.md`](docs/celebrity-glamour-iou.md) — the Celebrity's Glamour / IOU build, played as a variant against the canonical design.
- [`enemy-design-bible.md`](docs/enemy-design-bible.md) — v2 shared-meter enemy model: enemies are conditions to manage, not HP bars to delete.
- [`metagame-campaign.md`](docs/metagame-campaign.md) — the campaign map. Potionomics-style free roam drawn as a 2:1 isometric sprite city (§1.5–1.6), superseding the StS node-chain sketch in `core-design.md` §10.

### Systems — code reference
- [`campaign-encounters.md`](docs/campaign-encounters.md) — encounter types, event choices and outcomes, drop-chance resolution, seeded pools, the encounter database, and the Gantt tool.
- [`encounter-authoring-reference.md`](docs/encounter-authoring-reference.md) — every `[SerializeReference]` building block for encounters: outcomes, requirements, option wiring, flags.
- [`meta-progression.md`](docs/meta-progression.md) — profiles, the binary save format, run save/continue, unlocks, and the save debugging tools; achievements and Steam are designed, not built.
- [`ui-vfx.md`](docs/ui-vfx.md) — canvas-space VFX: flipbooks, card shine, fly trails, and when UIParticle is actually warranted.

### Planning & tracking
- [`needs-detailing.md`](docs/needs-detailing.md) — design questions still awaiting a call, ordered by how much they block.
- [`doc-audit.md`](docs/doc-audit.md) — standing audit of the docs against the code: what is stale and in what order to fix it.

### Art & audio
- [`art-bible.md`](docs/art-bible.md) — canonical art direction + resolution spec for artists. The Content Hub tabs are the live blank-slot checker.
- [`art-prompt-database.md`](docs/art-prompt-database.md) — per-asset image prompts.
- [`reference/style-mock-prompt.md`](docs/reference/style-mock-prompt.md) — style-mock generation prompt.
- [`reference/iso-tile-prompt.md`](docs/reference/iso-tile-prompt.md) — campaign-map generation prompt: 2:1 dimetric tiles and buildings, plus the acceptance check every sprite has to pass.
- [`reference/music-prompt.md`](docs/reference/music-prompt.md) — BGM prompts.

### Deprecated
- [`deprecated/README.md`](docs/deprecated/README.md) — 29 superseded docs, each banner-marked with what superseded it, plus a short list of ideas still worth mining.

---

## Codebase orientation

Engine: **Unity 6 (URP 17), C#**. Dependencies: DOTween, Odin Inspector, UniTask, UIParticle, TMPEffects, Steamworks.NET, Input System.

| Path | What lives there |
|---|---|
| `Assets/Scripts/Gameplay/Battle/` | `BattleManager` (FSM/flow), `OpinionLedger` (opinion + shields), `CrowdReactions` (hostility/echo/turncoat), `PassiveResolver`, polymorphic `BattleEffect`s under `Effects/` |
| `Assets/Scripts/Data/` | ScriptableObject data + `GameDatabase<T>` databases (cards, enemies, allies, origins, encounters), `RunState` |
| `Assets/Scripts/Data/Campaign/` | Encounter types, event outcomes, encounter pools — see [`campaign-encounters.md`](docs/campaign-encounters.md) |
| `Assets/Scripts/Data/Save/`, `Data/Unlocks/` | Profiles, run save/continue, unlock conditions, save debugging — see [`meta-progression.md`](docs/meta-progression.md) |
| `Assets/Scripts/Tests/EditMode/` | NUnit tests (Unity Test Runner → Edit Mode) |
| `Assets/Scripts/Tests/PlayMode/` | Per-card smoke test: every card vs a receptive, neutral and hostile enemy (Test Runner → Play Mode) |
| `Assets/Scripts/UI/Battle/` | Battle UI, decomposed into self-subscribing panel islands |
| `Assets/Scripts/Editor/` | Authoring tools — see below |
| `Assets/Data/` | Authored ScriptableObject assets — cards, enemies, passives, encounters, VFX events |
| `Assets/Prefabs/` | `Battle/`, `UI/`, `VFX/` |
| `Assets/Resources/` | **Only** what's loaded by runtime path — see below |

> [!IMPORTANT]
> **`Assets/Resources/` is deliberately small.** Everything in a Resources folder ships in every build, uncompressed and unstrippable, and is scanned at startup — so only assets genuinely loaded by *path string* belong there:
> - `DOTweenSettings.asset` — pinned by DOTween's own loader
> - `DebugSettings.asset` — log levels, loaded by `GameLogger` before the first scene
> - `StatusEffectIconMap.asset` — `Resources.Load` by name, [AuthoringCatalogWindow.cs:52](Assets/Scripts/Editor/AuthoringCatalogWindow.cs:52)
> - `Databases/CardDatabase.asset` — [BattleTestStarter.cs:355](Assets/Scripts/UI/Battle/BattleTestStarter.cs:355), card outcomes, the save system
> - `Databases/{EnemyDatabase,AllyDatabase,OriginDatabase}.asset` — the save system resolves saved IDs through them; `OriginDatabase.Shared` reads starting values
> - `NepoBabyConfig.asset` — Nepo Baby's class rules (`NepoBabyConfig.Current`)
> - `UI/CampaignMap.uss`, `UI/DefaultRuntimeTheme.tss` — the campaign screen's runtime-built UI
>
> Authored content is referenced by direct GUID reference and belongs in `Assets/Data/`. **Do not add assets to `Resources/` unless something loads them by string path.**

**Architecture in one line:** a static `EventBus` for *notifications only* (never gameplay commands), an FSM for turn flow, and `[SerializeReference]` polymorphic effects authored as data.

**The data-shape rule:** ScriptableObject for the noun you reference, name, and count (`CardData`, `EnemyData`, `EncounterData`). `[SerializeReference]` for the polymorphic verb inside it (`BattleEffect`, `BattlePassive`, `RunOutcome`). Reasoning in [`campaign-encounters.md`](docs/campaign-encounters.md#why-scriptableobject-and-not-serializereference).

### Editor tools (`Crookedile` menu)
- **Content Hub** — audits all content for completeness; check here before assuming data is fine.
- **Card Database** / **Enemy Database** — dashboards with health views over authored content.
- **Authoring Catalog** — reflection-built reference of every `[SerializeReference]` building block the inspector offers (effects, triggers, conditions, status behaviors).
- **Encounter Designer** — Timeline, Table, Dependencies, Flags, Simulate, Travel and Authoring views over an encounter pool.
- **Save Debugger** — profiles, counters, unlocks, readable run saves, snapshots and corruption tests.
- **Battle Inspector**, **Battle Session Builder**, **Playtest Bot** — battle debugging and automated playtests.
- **Campaign** — `Create Campaign Scene`, `Fix Build Settings Scenes`, `Run Travel Checks`.

The in-game dev console (backquote) runs `[CheatCommand]` methods, including `addcard` and the `save*` commands; cheats need the `CHEATS_ENABLED` define.

---

## Content note

Satire of political violence, corruption, religious manipulation, class inequality, and nepotism. No real politicians are depicted; all content is fictional parody.

---

*Status: active development. Battles and the seven-day campaign are playable end to end in `main.unity` and `campaign.unity`, with runs saved and continued per profile. The campaign and save screens are playtest UI; production UI, achievements and Steam integration are still to come.*
