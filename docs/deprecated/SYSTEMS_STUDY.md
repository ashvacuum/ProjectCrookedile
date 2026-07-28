> [!WARNING]
> **Deprecated — no longer in use.** Snapshot of a codebase state that no longer exists.
> Kept because the ideas may still be worth mining; the specifics are not.
> **Superseded by:** [`readme.md`](../../readme.md), [`docs/roadmap.md`](../roadmap.md)
> Index: [`docs/deprecated/README.md`](README.md)  ·  Back to [`readme.md`](../../readme.md)

---

> [!WARNING]
> **Pre-redesign document — may be inaccurate (flagged 2026-06).** Crookedile underwent a major combat + class redesign. Canonical design now lives in [`docs/core-design.md`](docs/core-design.md) and [`docs/crookedile-starter-decks.md`](docs/crookedile-starter-decks.md); current code/architecture is summarized in [`readme.md`](readme.md). Treat specifics below as historical until reconciled.

# CROOKEDILE — Systems Study

> Architecture analysis, system interdependencies, and implementation status.
> For game design content, see [GAME_WIKI.md](GAME_WIKI.md).

---

## Table of Contents

1. [System Map](#1-system-map)
2. [System-by-System Analysis](#2-system-by-system-analysis)
3. [Data Flow Diagrams](#3-data-flow-diagrams)
4. [Design vs. Implementation Gap](#4-design-vs-implementation-gap)
5. [Implementation Status](#5-implementation-status)
6. [Critical Blockers & Next Steps](#6-critical-blockers--next-steps)
7. [Known Inconsistencies](#7-known-inconsistencies)
8. [Architecture Principles](#8-architecture-principles)

---

## 1. System Map

### Layer Diagram

```
┌──────────────────────────────────────────────────────────┐
│  META LAYER  (persists across runs)                      │
│  ┌────────────────┐  ┌──────────────────────────────┐   │
│  │ Political      │  │ Progression                  │   │
│  │ Capital (PC)   │  │ (Achievements, Unlocks,      │   │
│  │                │  │  Prestige, NG+)              │   │
│  └────────────────┘  └──────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
          │ feeds into
┌──────────────────────────────────────────────────────────┐
│  CAMPAIGN LAYER  (per-run, resets on defeat)             │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ Map/Location │  │ Resource     │  │ NPC Relation- │  │
│  │ System       │  │ System       │  │ ship System   │  │
│  │              │  │ (₱,L,U,H,S, │  │ (Palakasan)   │  │
│  │ 45 Days      │  │  +Origin     │  │               │  │
│  │ Node Map     │  │  Resources)  │  │               │  │
│  └──────────────┘  └──────────────┘  └───────────────┘  │
│         │                │                  │            │
│         └────────────────┼──────────────────┘            │
│                          │ triggers                       │
│  ┌──────────────┐  ┌─────▼──────────────────────────┐   │
│  │ Event System │  │ BATTLE SYSTEM                  │   │
│  │ (random,     │  │ (BattleManager, DeckManager,   │   │
│  │  scripted,   │  │  EffectResolver, BattleStats,  │   │
│  │  milestone)  │  │  StatusEffectManager)          │   │
│  └──────────────┘  └───────────────────────────────-┘   │
└──────────────────────────────────────────────────────────┘
          │ feeds into
┌──────────────────────────────────────────────────────────┐
│  DATA LAYER  (static configuration)                      │
│  ┌──────────┐  ┌───────────┐  ┌──────────┐  ┌────────┐  │
│  │ CardData │  │OriginStats│  │ Location │  │ Event  │  │
│  │ (ScriptO)│  │ (ScriptO) │  │ Data     │  │ Data   │  │
│  └──────────┘  └───────────┘  └──────────┘  └────────┘  │
└──────────────────────────────────────────────────────────┘
```

### System Dependencies

```
BattleManager
  ├── DeckManager          (manages card zones)
  ├── BattleStats          (tracks Resolve/Composure/Hostility/AP)
  ├── EffectResolver       (executes card effects)
  │     └── StatusEffectManager  (applies buffs/debuffs)
  ├── BattleEvents         (EventBus for all battle communication)
  ├── OpponentAI           [NOT IMPLEMENTED — critical blocker]
  └── BattleUI             (reads state via EventBus)

CampaignManager [NOT IMPLEMENTED]
  ├── MapManager           [NOT IMPLEMENTED]
  ├── ResourceManager      [NOT IMPLEMENTED]
  ├── EventManager         [NOT IMPLEMENTED]
  ├── RelationshipManager  [NOT IMPLEMENTED]
  └── BattleManager        (per-battle instantiation)

MetaProgressionManager [NOT IMPLEMENTED]
  ├── AchievementSystem    [NOT IMPLEMENTED]
  ├── PoliticalCapital     [NOT IMPLEMENTED]
  └── UnlockSystem         [NOT IMPLEMENTED]
```

---

## 2. System-by-System Analysis

### Battle System

**Purpose:** The real-time gameplay layer. All conflict resolution goes through here — debates, negotiations, confrontations.

**Key Concept:** Political negotiation as a card game. Not combat — conversations. Resolve = "will to negotiate," not health. Winning with low Hostility creates allies; winning with high Hostility creates enemies.

**Core Loop:**
```
Draw Hand → Play Cards (spend AP) → TurnEnd (Hostility damage) → Opponent Turn → Repeat
```

**What makes it interesting:**
- Composure forces deferred-damage strategy (build now, burst later)
- Hostility creates a risk amplifier (the more aggressive you are, the more damage you take)
- The ally/enemy outcome system connects battles to the campaign layer

**Coupling Points:**
- **Into Campaign:** BattleResult outputs allies/enemies, Heat changes, Support changes, card rewards
- **From Campaign:** BattleSetup receives player deck, origin stats, opponent deck
- **From Data Layer:** CardData ScriptableObjects define all card properties

---

### Resource System

**Purpose:** The campaign-layer economy. Resources flow in from battles, events, and locations; flow out into purchases, bribes, and abilities.

**Key Tension:** Heat is both earned through "good" resource generation (corruption, aggression) and requires resources to reduce. This creates a feedback loop the player must manage.

**Resource Interaction Map:**
```
Campaign Funds (₱) ─── can convert to ──→ Lagay (+Heat)
                    ─── spend on ──→ Cards, Services, Heat Reduction
                    ─── earn from ──→ Kickbacks, Fundraising

Lagay (L) ──────────── generates ──────→ Heat
          ──────────── can silence ─────→ Journalists (reduces Heat risk)
          ──────────── enables ─────────→ Auto-win battles

Heat (H) ───────────── when 100 ────────→ Scandal Event
         ───────────── 51-75H daily ────→ Random Scandal (20%)
         ───────────── reduces ─────────→ Support at election (1H > 50 = -1 Support)

Support ─────────────── target ──────────→ 10,000 to win
        ─────────────── can convert ─────→ ₱ via Fundraiser (10%)

Utang na Loob (U) ──── affects ──────────→ NPC relationships, Shop prices
                  ──── converts ───────────→ Support at end (1U = 50 Support)
                  ──── negative ───────────→ Triggers betrayal events

Origin Resources ─────── each spends to ─→ Special battle/campaign abilities
(Fear/Clout/Faith/Influence)              and modify card effectiveness
```

**The Corruption Spiral (critical design pattern):**
```
Lagay → Advantage + Heat → High Heat → Need more Lagay → More Heat → Scandal
```
This is intentional. The game should reward players who avoid it and punish players who fall into it.

---

### Origin System

**Purpose:** Differentiates runs through fundamentally different starting conditions, secondary mechanics, and available cards. Each origin should feel like a different game strategy.

**Design Philosophy:** Origins don't just change stats — they change what options are available. Religious Leader can't use Lagay at all. Nepo Baby can't earn Utang na Loob normally. These restrictions are as important as the bonuses.

**Origin × Resource Matrix:**

| | ₱ | L | U | H | Support | Secondary |
|---|---|---|---|---|---------|-----------|
| Strongman | Low | Medium | Neutral | **High (30)** | Low | Fear |
| Celebrity | Medium | None | Positive | Low | **High** | Clout |
| Religious Leader | **Very Low** | **BLOCKED** | **High** | Low-Medium | Medium | Faith |
| Nepo Baby | **Very High** | **High** | **Negative** | High (40) | Medium | Influence |

**Unique Mechanical Restrictions:**
- Strongman: Journalists +1 encounter frequency; Charm −25%
- Celebrity: Scandal damage ×2 (fallen idol); Attack −30%
- Religious Leader: No Lagay (instant +80H); Scandal penalty ×3
- Nepo Baby: U naturally capped at 0; Scandal Heat +50%

**Coupling Points:**
- Origin selection at run start sets initial ResourceManager state
- BattleManager reads OriginStats for AP count, passive ability
- Card shop filters card pool based on origin compatibility

---

### Card System

**Purpose:** The player's agency within battle. Deck building across a run creates a power progression arc.

**Three Archetypes (matching origins):**
- **Diplomacy (Faith Leader):** Composure build → Blessing burst. Patient, safer, creates allies.
- **Manipulate (Nepo Baby):** Card/AP advantage. Flexible, enables big turns.
- **Hostility (Actor/Celebrity):** High damage, high Hostility. Fast wins, creates enemies, takes more damage.

**Deck Evolution Arc:**
```
Run Start (10 cards) → Battle Rewards (choose 1 of 3) → Shop Purchases
→ Rest Site upgrades → Remove bad cards → Optimal 12-18 card deck
```

**Key Balance Insight — Hostility as Risk Meter:**
Hostility doesn't just make you deal more damage — it means the opponent deals significantly more damage to you. At 3 Hostility, incoming damage is 2.5×. This makes pure aggression unsustainable without Hostility management cards (Fan Favorite, Deflect) or the Actor's Ego Trip conversion.

**Coupling Points:**
- Cards are CardData ScriptableObjects (data layer)
- Battle resolves effects via EffectResolver
- Post-battle: ally/enemy decision determines whether ally's signature card is offered
- Campaign: ₱ used to buy cards from shops, upgrade at rest sites

---

### Campaign Layer (Map + Events + Locations)

**Purpose:** The macro game. The 45-day countdown structures every decision — where to go, what to do, how to manage time.

**Daily Structure forces tradeoffs:**
```
Morning: Visit Location (battle/event/shop) OR Rest (−10H)
Afternoon: Another Location OR Card Workshop OR Fundraiser
Evening: Automatic random event + news cycle
```

Every action costs time. You cannot visit all locations. This creates meaningful path selection.

**Location Ownership System:**
Winning battles or quests at a location "claims" it — generating passive Support per day. This creates a snowball: strong players own more locations → more passive income → even stronger. Counter-balanced by Heat accumulation from aggressive claiming.

**Event Types and Their Role:**
| Event Type | Function |
|-----------|---------|
| Milestone (TV Debate, COMELEC) | Scripted high-stakes checkpoints, force player preparation |
| Heat-triggered | Punishment for going over thresholds — force resource response |
| Utang na Loob | Reward/punish NPC relationship choices |
| Random (absurdist) | Variety, comedy, unexpected situations |
| Project-based | ₱/Support opportunity with moral corruption choice |

**Coupling Points:**
- Map node visited → triggers Battle OR Event OR Shop
- Location faction alignment affects card effectiveness in that area
- NPC relationships (U) affect available battle difficulty and event outcomes
- Time limit forces all decisions to be made under pressure

---

### NPC Relationship System (Palakasan)

**Purpose:** Models the real Filipino political concept of reciprocal obligation and clan networks. Creates a consequence graph that persists across battles.

**Palakasan Web:**
```
Ally A ── (family) ── Ally B
  │                       │
(business)             (compadre)
  │                       │
NPC C ── (political) ── NPC D (your enemy)
```

Helping Ally A → NPC C gives you a discount.
Betraying Ally A → NPC C becomes hostile.

**Coupling Points:**
- Battle outcome (Ally/Enemy) feeds this system
- U resource tracks aggregate relationship score
- At U thresholds, system events trigger
- NPC relationships affect shop prices, battle difficulty, event options

---

### Meta Progression (Political Capital)

**Purpose:** Provides long-term reward structure across multiple runs. Makes every run feel like progress even if you lose.

**PC Flow:**
```
Run completion (win or lose) → Earn PC → Spend on unlocks
                                    → New origins → Different playstyles
                                    → Legacy perks → Easier future runs
                                    → Card unlocks → Wider pool
                                    → QoL upgrades → Better access
```

**Design Tension:** Legacy perks make future runs easier. Too many purchases could make the game trivial. Balance principle: max ~30% advantage from meta perks. Difficulty modifiers let skilled players opt for harder runs.

---

## 3. Data Flow Diagrams

### Battle Data Flow

```
[CardData ScriptableObject]
        │
        ▼
[DeckManager] ─── builds ──→ [Hand]
                                │
                    Player selects card
                                │
                                ▼
              [BattleManager.PlayCard]
                   │         │
          Validate AP    Pay AP cost
                   │
                   ▼
          [EffectResolver.ResolveCard]
               │         │         │
          Damage     Composure  Hostility
          calc       gain/loss  gain
               │         │         │
               ▼         ▼         ▼
          [BattleStats updates for both combatants]
                         │
                         ▼
              [EventBus publishes events]
                   │           │
                   ▼           ▼
             [BattleUI]   [BattleLog]
              updates      entries
```

### Campaign Data Flow (Designed, Not Implemented)

```
[Player chooses map node]
        │
        ▼
[CampaignManager resolves node type]
   ├── Battle → [BattleSetup] → [BattleManager] → [BattleResult]
   │              (player deck, origin stats, opponent data)
   │                                                │
   │                              ┌────────────────-┘
   │                              │ (allies/enemies, Heat Δ, Support Δ, ₱ Δ)
   │                              ▼
   │                    [ResourceManager updates]
   │                    [RelationshipManager updates]
   │
   ├── Event → [EventManager] → [EventOutcome] → [ResourceManager updates]
   │
   ├── Shop  → [ShopUI] → [Card Purchase] → [DeckManager.AddCard]
   │
   └── Rest  → [−10H] + [Optional: upgrade or remove 1 card]
```

### Resource → Outcome Flow

```
Support ─────────────────────────────────── Day 45 → WIN if ≥ 10,000
Heat ───────────── at 100 ────────────────→ Scandal event (may be LOSE)
       └─ 1H per turn above 50 ──────────→ Support penalty at election
Utang na Loob ── at thresholds ──────────→ Event triggers
               └─ end of run ────────────→ +50 Support per U
Lagay use ───────────────────────────────→ +5-15H per use
₱ below emergency ───────────────────────→ Cannot react to events
```

---

## 4. Design vs. Implementation Gap

The game has two overlapping origin systems that need reconciliation:

### The Origin Naming Inconsistency

The **design documents** (origins.md, game_overview.md) describe four origins:
1. Strongman (Fear mechanic)
2. Celebrity/Artista (Clout mechanic)
3. Religious Leader (Faith mechanic)
4. Nepo Baby (Influence mechanic)

The **implementation** (BATTLE_SYSTEM.md, BATTLE_SYSTEM_TASKS.md, cards.md) has three implemented origins:
1. **Faith Leader** — maps to Religious Leader
2. **Nepo Baby** — identical
3. **Actor** — maps to Celebrity

**Gap:** Strongman has no implementation. The "Actor" name used in battle code doesn't match "Celebrity" in design docs.

**Recommendation:** The battle implementation uses simplified names for the three MVP origins. Strongman is a 4th origin not yet in the battle system. The naming inconsistency (Actor vs Celebrity) should be standardized — either rename the design doc or update the code. Design intent suggests "Celebrity" is the canonical name.

### What's Designed but Not Built

| System | Design State | Implementation State |
|--------|-------------|---------------------|
| Campaign map (45-day structure) | Fully designed | Not started |
| Resource system (₱, L, U, H, Support) | Fully designed | Not started |
| Location ownership | Fully designed | Not started |
| NPC relationship web (Palakasan) | Fully designed | Not started |
| Event system | Fully designed (960+ lines of events) | Not started |
| Strongman origin | Fully designed | Not started |
| Card shop / rest site | Fully designed | Not started |
| Ally/enemy system | Fully designed | Not started |
| Meta progression (PC, achievements) | Fully designed | Not started |
| Opponent AI | Partially designed | Not started (critical blocker) |
| Starter deck content (CardData assets) | Designed | Not started (blocking playable battles) |

### What's Built but Not Designed in Detail

| System | Status |
|--------|--------|
| BattleManager (state machine) | Implemented ✅ |
| DeckManager (card zones) | Implemented ✅ |
| BattleStats (Resolve/Composure/Hostility/AP) | Implemented ✅ |
| EffectResolver (card effects) | Implemented ✅ |
| StatusEffectManager (buffs/debuffs) | Implemented ✅ |
| BattleEvents (EventBus) | Implemented ✅ |
| BattleUI (stats display + card hand + log) | Implemented ✅ |
| CardButton (2D card view — artwork, frames, hover, affordability, MMFeedbacks) | Implemented ✅ |
| CardData / CardEffect / CardCost (data layer) | Implemented ✅ |
| EffectResolverTest (unit tests) | Implemented ✅ |

### Removed Systems

| System | Reason |
|--------|--------|
| Card3DView | 3D approach impractical — replaced by CardButton (2D) |
| CardHandManager | 3D world-space hand layout — no longer needed |
| CardInputHandler | Physics raycast input for 3D cards — replaced by IPointerClickHandler |
| CardPrefabSetup | 3D prefab builder — replaced by Unity UI prefab |
| BattleCardHandBridge | Synced 3D visuals with battle logic — no longer needed |
| Card.prefab (3D) | 3D quad card prefab — deleted |

---

## 5. Implementation Status

### Current Build Completion by Layer

```
META LAYER          ░░░░░░░░░░  0%   — Not started
CAMPAIGN LAYER      ░░░░░░░░░░  0%   — Not started
  └─ Resource Sys   ░░░░░░░░░░  0%
  └─ Map System     ░░░░░░░░░░  0%
  └─ Event System   ░░░░░░░░░░  0%
  └─ NPC/Relations  ░░░░░░░░░░  0%
BATTLE SYSTEM       ████████░░  80%  — Core done, AI + content missing
  └─ Core Logic     ██████████  100% — Phase 1 complete
  └─ Battle UI      █████████░  90%  — Code complete, Unity prefab setup remaining
  └─ 2D Card View   ██████████  100% — CardButton upgraded (artwork/frames/hover/MMF)
  └─ Opponent AI    ░░░░░░░░░░  0%   — Phase 2, CRITICAL BLOCKER
  └─ Starter Decks  ░░░░░░░░░░  0%   — Phase 3, blocking playability
  └─ Testing        ░░░░░░░░░░  0%   — Phase 5
DATA LAYER          ████░░░░░░  40%
  └─ CardData.cs    ██████████  100% — Structure complete
  └─ CardAssets     ░░░░░░░░░░  0%   — No cards created yet
  └─ OriginStats    ██████░░░░  60%  — Structure done, content pending
```

### Battle System Task Breakdown

**Phase 1 — Core Systems** ✅ 100% (15/15 tasks complete)
- CardData, CardEffect, CardCost, Enums, StatusEffect
- BattleStats, DeckManager, StatusEffectManager
- EffectResolver, BattleManager, BattleState
- Integration + EffectResolverTest

**Phase 2 — Opponent AI** ⚠️ 0% (CRITICAL — without this, battles can't run)
- [ ] OpponentAI.cs — card evaluation, play affordable cards, end turn logic
- [ ] Integrate into BattleManager.OpponentTurnState
- Estimated: 2–3 hours

**Phase 3 — Battle Content** ⚠️ 0% (HIGH — needed to test with real cards)
- [ ] StarterDeckData.cs ScriptableObject
- [ ] Create 10 CardData assets per origin × 3 origins = 30 cards
- [ ] OriginStats.cs — verify faith/nepo/actor stats
- [ ] Implement origin passive abilities in BattleManager
- [ ] BattleSetupData.cs
- Estimated: 3–4 hours

**Phase 4 — Battle UI** ✅ 100% (7/7 core tasks complete)
- BattleUI.cs, CardButton.cs, Battle Log — all working

**Phase 5 — Testing** ⏳ 0% (MEDIUM — validate everything works)
- Test scene creation, 3-origin battles, all card effects, all status effects, balance pass

**Phase 6 — Meta-Game** ⏳ 0% (LOW — future work)
- BattleRewardManager, Ally/Enemy system, Campaign/Map system

### Definition of "Playable Battle"

For end-to-end battle testing, ALL of these must exist:
- [ ] OpponentAI (Phase 2)
- [ ] All 3 starter decks as CardData assets (Phase 3)
- [ ] Unity scene with BattleUI prefab configured
- [ ] OriginStats assets for all 3 origins

---

## 6. Critical Blockers & Next Steps

### Immediate Blockers (In Priority Order)

#### #1 — Starter Deck Content (Phase 3)
**Blocker:** No CardData ScriptableObject assets exist for the 30 starter cards.
**Impact:** Cannot start a real battle with actual cards.
**Solution:** Create CardData assets in Unity for all 30 cards (10 × 3 origins). The card designs are fully specified in BATTLE_SYSTEM.md and cards.md.
**Estimated effort:** 2–3 hours

#### #2 — Opponent AI (Phase 2)
**Blocker:** Opponent turn is a no-op (auto-passes).
**Impact:** Cannot test two-sided battles.
**Solution:** Implement basic OpponentAI.cs with:
- Score each card in hand by situation (Resolve difference, current Composure/Hostility)
- Play highest-value affordable card
- End turn when out of AP or no valid moves
**Estimated effort:** 2–3 hours

#### #3 — Unity Scene Setup
**Blocker:** BattleManager and BattleUI exist in code but no configured Unity scene.
**Impact:** Nothing can be tested in the editor.
**Solution:** Follow BATTLE_UI_SETUP.md and SCENE_SETUP_GUIDE.md to configure a test battle scene.
**Estimated effort:** 1–2 hours

### Medium-Term Work (After Playable Battle)

1. **Balance Testing** — Are battles too fast/slow? Is any origin dominant?
2. **BattleRewardManager** — Post-battle card selection (choose 1 of 3)
3. **Ally/Enemy system** — Persistent consequence from battle outcomes
4. **Campaign skeleton** — Simple day counter, node map, CampaignManager

### Long-Term Work (Campaign Layer)

These systems are fully designed but require significant implementation:
- ResourceManager (₱, L, U, H, Support tracking)
- MapManager (Slay the Spire-style node navigation)
- EventManager (800+ event definitions to implement)
- LocationSystem (22+ locations, ownership mechanics)
- NPC/RelationshipManager (Palakasan web)
- MetaProgressionManager (PC, achievements, unlocks)

---

## 7. Known Inconsistencies

### Inconsistency 1 — Origin Names

**Design docs use:** Strongman, Celebrity, Religious Leader, Nepo Baby
**Code uses:** Faith Leader, Nepo Baby, Actor

**Resolution needed:** Standardize. Recommend:
- "Faith Leader" → "Religious Leader" (more consistent with design)
- "Actor" → "Celebrity" (matches design, also in-world term "Artista")
- Or: keep code names as "implementation handles" and add in-world name fields

---

### Inconsistency 2 — Number of Origins

**Design docs:** 4 origins (Strongman, Celebrity, Religious Leader, Nepo Baby)
**Battle implementation:** 3 origins (Faith Leader, Nepo Baby, Actor)

**Missing:** Strongman has no implementation. The design is complete (origins.md, resources.md), but no code or card assets exist.

**Resolution:** Strongman should be treated as Phase 6 content — after the 3-origin battle system is working, add Strongman as a 4th origin.

---

### Inconsistency 3 — Campaign Resource Overlap

The **design documents** describe a full campaign resource system (₱, L, U, H, Support, Fear/Clout/Faith/Influence) with complex interactions.

The **battle system** only uses in-battle resources: Resolve, Composure, Hostility, Action Points.

**The design intent** (from BATTLE_SYSTEM.md and cards.md) is that meta resources (₱, Heat, Influence) affect the **campaign layer**, not individual battles. Post-battle, outcomes affect these meta resources.

**Resolution:** This is not a bug — it's the correct design. The campaign layer (when built) will bridge the two resource systems. Battle outcomes produce "BattleResult" objects with changes to meta resources.

---

### Inconsistency 4 — Starter Deck Card Count

**origins.md** specifies: 15 cards per origin starter deck
**cards.md and BATTLE_SYSTEM.md** specify: 10 cards per origin starter deck

**Resolution:** The implementation specs (10 cards) are more recent and align with Griftlands-style design. Use 10. The 15-card count in origins.md is outdated.

---

### Inconsistency 5 — Card Type Names in Origins

**origins.md** uses: Attack, Charm, Defense, Leverage, Power card types
**cards.md and BATTLE_SYSTEM.md** use: Diplomacy, Hostility, Manipulate

**Resolution:** The Diplomacy/Hostility/Manipulate system (from implementation) is the canonical system. The old design used campaign-level card type names that don't map cleanly to battle mechanics. Origins.md needs updating to reflect the current card type vocabulary.

---

## 8. Architecture Principles

### Patterns in Use

**EventBus (ScriptableObject-based)**
- All battle events communicate through EventBus, not direct method calls
- `BattleEvents.cs` defines all published/subscribed events
- Enables loose coupling: BattleUI doesn't reference BattleManager directly
- Example: `BattleStartedEvent`, `CardPlayedEvent`, `ResolveChangedEvent`

**State Machine (BattleManager)**
- Battle has explicit states: Initialize → TurnStart → PlayerTurn → OpponentTurn → TurnEnd → BattleEnd
- State transitions are explicit, debuggable
- No "if current state is X, do Y" logic scattered across code

**No Singleton Abuse**
- BattleManager is instantiated per-battle (not a singleton)
- Only global services are singletons: AudioManager, SaveManager, etc.
- Each battle is fully isolated and disposable

**ScriptableObject Data Layer**
- CardData, OriginStats, StarterDeckData are all ScriptableObjects
- Configurable in Unity Editor without code changes
- Can create variants without modifying scripts

**Type-Safe Logging**
- `GameLogger.LogInfo<T>()` — no string literals for component names
- Auto-registers categories from type names
- Easy to filter/search logs by system

### SOLID Compliance Audit

| Principle | Status | Notes |
|-----------|--------|-------|
| Single Responsibility | ✅ Good | Each manager has one clear job |
| Open/Closed | ✅ Good | CardEffect system is extensible without modifying EffectResolver |
| Liskov Substitution | ✅ Good | Effect types follow consistent interface |
| Interface Segregation | ✅ Good | EventBus listeners only subscribe to relevant events |
| Dependency Injection | ✅ Good | BattleManager receives deps via constructor/init, not singletons |

### Performance Targets (from technical.md)

| Metric | Target |
|--------|--------|
| Battle frame rate | 60 FPS stable |
| Card play latency | < 16ms |
| Scene load time | < 2 seconds |
| Memory per battle | < 50MB |

---

*SYSTEMS_STUDY.md — Last updated based on docs audit, February 2026*
*Primary sources: BATTLE_SYSTEM.md, BATTLE_SYSTEM_TASKS.md, cards.md, origins.md, resources.md, game_overview.md, technical.md*
