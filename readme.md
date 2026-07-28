# Crookedile

*A Filipino political roguelite deck-builder. Working title (formerly "Palakasan" / "Philippine Political Card Game").*

> You are not winning a fight. You are working a crowd — managing who speaks, how loudly, and in what direction they push public opinion.

---

> [!IMPORTANT]
> **The design is in active flux.** The game went through a major combat + class redesign. Everything under `docs/` is current. Most root-level `SCREAMING_CASE.md` and lowercase design files predate that redesign and are flagged [historical](#historical-pre-redesign) — don't trust their specifics until reconciled.

## Start here

| Doc | What it settles |
|---|---|
| [`docs/core-design.md`](docs/core-design.md) | Core fantasy, the **Opinion Meter**, hostility, the **Echo Chamber** rule, voice intents, turn structure, the three archetypes |
| [`docs/roadmap.md`](docs/roadmap.md) | The 3-month demo scope call — what ships, what's explicitly cut, phase gates |
| [`docs/naming-glossary.md`](docs/naming-glossary.md) | Combat vocabulary → "working a crowd" vocabulary. Read before naming anything |

## What the game is

- **Roguelite deck-builder**, political satire. The card **battles are the core**, wrapped in an **overworld campaign**: navigate a town map and accumulate **Support** toward winning the election by a deadline.
- **No HP.** The per-battle battleground is a shared **Opinion Meter** (win at 100, lose at 0, Judgment at the turn limit). Directional session shields — **Support** (guards against drops) and **Denial** (guards against rises) — protect it. *(Per-battle "Support" the shield is distinct from campaign "Support points" the win condition — a naming overlap to resolve.)*
- **Hostility** is a signed per-enemy stance you manage (hostile ↔ receptive). The central tension is the **Echo Chamber**: convert the *whole* room and your gains halve and your lead decays — so you always want a villain present.
- **Three archetypes:** **Nepo Baby** (summon bodies; a hand-gated *Patronage* economy), **Celebrity** (an "open canvas" drafting into Attention / Scandal / Drama King), **Faith Leader** (stack statuses to convert enemies into one-turn meter-pumping followers).

---

## Documentation map

### Design — canonical
- [`core-design.md`](docs/core-design.md) — the combat model and the three archetypes.
- [`crookedile-starter-decks.md`](docs/crookedile-starter-decks.md) — per-class starter decks and the reward-pool "potential" layer.
- [`enemy-design-bible.md`](docs/enemy-design-bible.md) — v2 shared-meter enemy model: enemies are conditions to manage, not HP bars to delete.
- [`metagame-campaign.md`](docs/metagame-campaign.md) — the campaign map. Potionomics-style free roam, superseding the StS node-chain sketch in `core-design.md` §10.
- [`faith-leader-identity.md`](docs/faith-leader-identity.md) — FL identity lock + a 20-card list authorable with existing effects.

### Systems — code reference
- [`campaign-encounters.md`](docs/campaign-encounters.md) — encounter types, event choices and outcomes, drop-chance resolution, seeded pools, the encounter database, and the Gantt tool.
- [`ui-vfx.md`](docs/ui-vfx.md) — canvas-space VFX: flipbooks, card shine, fly trails, and when UIParticle is actually warranted.
- [`opinion-meter-passes.md`](docs/opinion-meter-passes.md) — implementation spec for the opinion-meter redesign (passes 2–4).

### Planning & tracking
- [`roadmap.md`](docs/roadmap.md) — demo scope, phases, gates.
- [`campaign-build-checklist.md`](docs/campaign-build-checklist.md) — execution tracker for the campaign layer (M1–M2), with the hardening decisions behind each call.
- [`work-now.md`](docs/work-now.md) — tasks where the decision is made and only execution remains.
- [`needs-detailing.md`](docs/needs-detailing.md) — design questions still awaiting a call, ordered by how much they block.
- [`test-plan.md`](docs/test-plan.md) — rebuilding the test suite against the StatusBehavior API.
- [`card-audit.md`](docs/card-audit.md) — every authored card against the fantasies in `core-design.md`.

### Art & audio
- [`art-bible.md`](docs/art-bible.md) — canonical art direction + resolution spec for artists.
- [`art-needed.md`](docs/art-needed.md) — the thin gap checklist (superseded by the art bible).
- [`reference/style-mock-prompt.md`](docs/reference/style-mock-prompt.md) — style-mock generation prompt.
- [`reference/music-prompt.md`](docs/reference/music-prompt.md) — BGM prompts.

---

## Codebase orientation

Engine: **Unity 6 (URP 17), C#**. Dependencies: DOTween, Odin Inspector, UniTask, UIParticle.

| Path | What lives there |
|---|---|
| `Assets/Scripts/Gameplay/Battle/` | `BattleManager` (FSM/flow), `OpinionLedger` (opinion + shields), `CrowdReactions` (hostility/echo/turncoat), `PassiveResolver`, polymorphic `BattleEffect`s under `Effects/` |
| `Assets/Scripts/Data/` | ScriptableObject data + `GameDatabase<T>` databases (cards, enemies, relics, origins, encounters) |
| `Assets/Scripts/Data/Campaign/` | Encounter types, event outcomes, encounter pools — see [`campaign-encounters.md`](docs/campaign-encounters.md) |
| `Assets/Scripts/UI/Battle/` | Battle UI, decomposed into self-subscribing panel islands |
| `Assets/Scripts/Editor/` | Authoring tools — see below |

**Architecture in one line:** a static `EventBus` for *notifications only* (never gameplay commands), an FSM for turn flow, and `[SerializeReference]` polymorphic effects authored as data.

**The data-shape rule:** ScriptableObject for the noun you reference, name, and count (`CardData`, `EnemyData`, `EncounterData`). `[SerializeReference]` for the polymorphic verb inside it (`BattleEffect`, `BattlePassive`, `RunOutcome`). Reasoning in [`campaign-encounters.md`](docs/campaign-encounters.md#why-scriptableobject-and-not-serializereference).

### Editor tools (`Crookedile` menu)
- **Card Database** / **Enemy Database** — dashboards with health views over authored content.
- **Authoring Catalog** — reflection-built reference of every `[SerializeReference]` building block the inspector offers (effects, triggers, conditions, status behaviors).
- **Encounter Gantt** — day timeline for an encounter pool, coverage warnings, and a seed roller that runs the real draw.

---

## Historical (pre-redesign)

Original campaign-era design and setup notes. Kept for reference; **reconcile against the canonical docs before trusting**.

- **Design source:** `game_overview.md`, `origins.md`, `resources.md`, `cards.md`, `locations.md`, `events.md`, `progression.md`, `technical.md`
- **Consolidated wiki / study:** `GAME_WIKI.md`, `SYSTEMS_STUDY.md`, `IMPLEMENTATION_STATUS.md`
- **Battle / UI / setup guides:** `BATTLE_SYSTEM.md`, `BATTLE_SYSTEM_TASKS.md`, `BATTLE_INTEGRATION_FLOW.md`, `BATTLE_UI_SETUP.md`, `CARD_2D_SETUP.md`, `CARD_ACQUISITION.md`, `CARD_EFFECTS.md`, `SCENE_SETUP_GUIDE.md`, `STARTER_CARDS_GUIDE.md`

## Content note

Satire of political violence, corruption, religious manipulation, class inequality, and nepotism. No real politicians are depicted; all content is fictional parody.

---

*Status: active development. The single-encounter battle loop is the current focus; the campaign layer's data and tooling are built but not yet wired to a scene.*
