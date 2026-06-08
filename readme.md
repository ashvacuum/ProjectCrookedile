# Crookedile

*A Filipino political roguelite deck-builder. Working title (formerly "Palakasan" / "Philippine Political Card Game").*

> You are not winning a fight. You are working a crowd — managing who speaks, how loudly, and in what direction they push public opinion.

---

> [!IMPORTANT]
> **The design is in active flux.** The game went through a major combat + class redesign. The **canonical, current** design lives in the two docs under `docs/` below. Most of the *other* root-level `.md` files predate that redesign and are flagged as historical — don't trust their specifics until reconciled.

## Start here (canonical, current)
- **[`docs/core-design.md`](docs/core-design.md)** — core fantasy, the **Opinion Meter**, hostility, the **Echo Chamber** rule, voice intents, turn structure, and the **three archetypes**.
- **[`docs/crookedile-starter-decks.md`](docs/crookedile-starter-decks.md)** — per-class starter decks and the reward-pool "potential" layer.

## What the game is (current)
- **Roguelite deck-builder**, political satire. The card **battles are the core** and the current focus — but they're wrapped in an **overworld campaign**: you navigate a town map and accumulate **Support** toward winning the election by a deadline. That campaign layer is still part of the vision, just **deferred and unsettled** (its exact shape — an abstract StS-style node map vs. a Potionomics-style navigable town — is an open design question; see `docs/core-design.md` §10 and the campaign-era docs below).
- **No HP.** The per-battle battleground is a shared **Opinion Meter** (win at 100, lose at 0, Judgment at the turn limit). Directional session shields — **Support** (guards against drops) and **Denial** (guards against rises) — protect it. *(Note: per-battle "Support" the shield is distinct from campaign "Support points" the win condition — a naming overlap to resolve.)*
- **Hostility** is a signed per-enemy stance you manage (hostile ↔ receptive). The central tension is the **Echo Chamber**: convert the *whole* room and your gains halve and your lead decays — so you always want a villain present.
- **Three archetypes:** **Nepo Baby** (summon bodies; a hand-gated *Patronage* economy), **Celebrity** (an "open canvas" that drafts into Attention / Scandal / Drama King), **Faith Leader** (stack statuses to convert enemies into one-turn meter-pumping followers).

## Codebase orientation
- Engine: **Unity 2021+ (C#)**. Active branch: `test-new-gameplay`.
- Gameplay: `Assets/Scripts/Gameplay/Battle/` — `BattleManager` (FSM/flow), `OpinionLedger` (opinion + shields), `CrowdReactions` (hostility/echo/turncoat), `PassiveResolver`, the polymorphic `BattleEffect` system under `Effects/`.
- Data: ScriptableObjects in `Assets/Scripts/Data/`. UI: `Assets/Scripts/UI/Battle/`. Editor tools: `Assets/Scripts/Editor/` (incl. the **Card Database** dashboard window).
- Architecture in one line: a static **EventBus** for *notifications only* (never gameplay commands), an FSM for turn flow, and `[SerializeReference]` polymorphic effects authored as data.

## Legacy / historical docs (pre-redesign — flagged, may be inaccurate)
These were the original campaign-era design + setup notes. Kept for reference; **reconcile against the canonical docs before trusting**:

- Design source: `game_overview.md`, `origins.md`, `resources.md`, `cards.md`, `locations.md`, `events.md`, `progression.md`, `technical.md`
- Consolidated wiki / study: `GAME_WIKI.md`, `SYSTEMS_STUDY.md`, `IMPLEMENTATION_STATUS.md`
- Battle/UI/setup guides: `BATTLE_SYSTEM.md`, `BATTLE_SYSTEM_TASKS.md`, `BATTLE_INTEGRATION_FLOW.md`, `BATTLE_UI_SETUP.md`, `CARD_2D_SETUP.md`, `CARD_ACQUISITION.md`, `CARD_EFFECTS.md`, `SCENE_SETUP_GUIDE.md`, `STARTER_CARDS_GUIDE.md`, `cards.md`
- Other current docs under `docs/`: `naming-glossary.md`, `opinion-meter-passes.md`

## Content note
Satire of political violence, corruption, religious manipulation, class inequality, and nepotism. No real politicians are depicted; all content is fictional parody.

---
*Status: active development — the single-encounter battle loop is the current focus. The overworld campaign (town map, Support-to-win) remains part of the design but is deferred and not yet locked.*
