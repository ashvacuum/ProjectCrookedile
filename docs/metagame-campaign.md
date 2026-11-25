# Metagame — The Campaign Map

> **Kind:** Design · **Status:** Partly built · **Updated:** 2026-11-26
>
> **Summary:** The campaign map: Potionomics-style free roam over a generated half-cube iso city (spec in city-generator.md), with build phases.
>
> **Source of truth:** this doc · **Related:** [`city-generator.md`](city-generator.md) · [`campaign-encounters.md`](campaign-encounters.md) · [`meta-progression.md`](meta-progression.md) · [`iso-tile-prompt.md`](reference/iso-tile-prompt.md)

*As of 2026-09-18. Supersedes the "StS map structure" sketch in `core-design.md` §10 — the
map is **Potionomics-style free roam**, not a branching node chain, and is **drawn as a 2:1
isometric city** (§1.5; half-cube meshes since 2026-11-23, `city-generator.md`). Build phases at the bottom; open design
questions marked ⚑ and mirrored in `needs-detailing.md`.*

---

## 1. Vision (locked direction)

The campaign is a **map you roam, paced by a time budget** — not a corridor of nodes.

- **Campaign HQ** is the main base. Runs start and return here.
- Each day (⚑ naming/cadence) grants **campaign action points**. Choosing any location on
  the map spends them. When they run out, the day ends: returning to HQ is offered only once no location
  left that day fits in the remaining time, never as a free way to cut the day short (*built 2026-11-26*;
  the 15-minute wait still lets the player run the clock down).
- Locations are NOT all battles. Three kinds:
  - **Encounters** — the card battles (the game).
  - **Events** — sceneries with options and choices: pick an option, get an outcome
    (items/relics/cards/opinion). No battle UI involved.
  - **HQ** — rest/manage (⚑ exact verbs undecided).
- **Relics** come from **boss victories** (guaranteed) and can also come from
  **event outcomes** (random).
- **Reward-quality scaling is in v1**: winning isn't binary — reward quality scales with
  how well you won (conversions, hostiles left, meter margin). Consumes the existing
  `RewardConfig`.

Deferred wholesale (unchanged from `needs-detailing.md` §9): viral moments / News Cycle.
Production overworld *art* is still deferred — but its **form is no longer open** (§1.5).
Campaign v1 may ship greybox tiles; it may not ship a layout that a real iso city could not
replace one-for-one.

## 1.5 Map presentation — half-cube iso city *(superseded 2026-11-23)*

The campaign map is a **city on a 2:1 isometric grid** — not a list of buttons, not a painted backdrop with
hotspots. It is drawn with half-cube meshes (a roof and two walls lit from face atlases), not sprites on a Tilemap,
and generated fresh each run from the seed. **The full spec is `city-generator.md`**: grid, data, generation rules,
rendering, lighting and the test-map painter. This section keeps only what the campaign layer relies on.

**Projection (one angle, no exceptions).** True 2:1 dimetric: plan rotated 45°, camera elevation **30°** (the diamond's edges run at 26.565°, `atan(0.5)`).
One cell is 1 × 0.5 world units (256 × 128 px at PPU 256); a 4K screen reads ~15 cells across.

**The boundary that keeps this cheap:** the city is **scenery**, plus one thing: each district stands on a building
the generator picks by the district's placement rule (`city-generator.md` §4.8). The city never chooses where an
encounter happens, and travel times stay authored on the roads. Encounter scheduling stays a function of the pool and
the day window (`campaign-encounters.md`), never of terrain.

## 1.6 City generation *(superseded 2026-11-23)*

Replaced by `city-generator.md`: still seeded and one city per run, but with a coastal downtown, radial avenues,
streets, distance zones, landmarks and half-cube buildings. The sprite `CityGenerator` and `CityZoneData` were
deleted (`city-generator.md` §8).

### District travel and time of day (agreed 2026-09-21)

Campaign travel is now a gameplay mechanic, separate from the scenery generator above.
Authored `DistrictData` assets are nodes; `CampaignTravelData` holds roads between them,
HQ, and the start-of-day clock (08:00 by default). Roads can be one-way or bidirectional,
with base minutes and daily traffic windows. No tile traversal or vehicle simulation is required.

- Each encounter has a district and an entry window. Opening is inclusive and closing is
  exclusive; the encounter may finish after closing, but must fit in the remaining day.
  Time windows do not cross midnight. Blank district means a local encounter that does not
  move the player. Existing encounters default to all-day entry and retain their hour duration.
- Hours is still the daily budget, configured by origin or the campaign fallback. It is spent
  in minutes: travel + waiting until opening + encounter duration. The authored origins and
  fallback use eight hours, from 08:00 to 16:00. Midnight caps longer authored days.
- Roads use predictable traffic windows, with the highest active multiplier on overlapping
  windows. The quickest route uses traffic sampled at trip departure for every road in that
  trip. This keeps previews deterministic; traffic changes during travel are not simulated.
- The map previews travel, traffic severity/multiplier, arrival, wait, and finish before the
  player commits. Unreachable, expired, and unaffordable encounters remain visible with a reason.
  Early arrival includes explicit waiting in the visit cost. A separate 15-minute wait action
  lets the player depart later, including after rush hour.
- Daily draws stay cached. The same encounters open and expire as the clock advances; time
  passing does not reroll them or reevaluate daily draw dependencies. Intraday replenishment
  of the pool is not part of this change.
- `RunState` owns elapsed minutes and current district across battle scene loads. Days begin
  at HQ; the overnight return is free. Immediate narrative chains remain free and happen at
  the current location, regardless of the chained asset's district/window.
- The mandatory boss action remains available when normal visiting is impossible. It resolves
  as a free finale at the current location, bypassing travel, budget, and time windows; the
  ordinary boss visit still charges its previewed cost. This prevents a timing softlock.

Author the network on the encounter pool and encounter windows/districts on the encounter
assets. Encounter Designer's **Travel** tab previews the same calculation used by gameplay.
Ally assets have separate battle and overworld passive lists. The Odin `[SerializeReference]`
overworld list starts with `ReduceTravelTimePassive` and `ReduceEncounterDurationPassive`;
each owns its percentage and description. Recruited allies stack additively, capped at 100%, with rounding up and a one-minute
floor for positive costs. Waiting is never discounted. These bonuses apply to future visits
and are included in both the gameplay preview and its committed cost. Encounter Designer's
Travel and Simulate tabs accept a hypothetical ally roster for tuning; see
`campaign-encounters.md` for the authoring workflow.
Its **Simulate** tab uses earliest-finish visits including travel and waiting, without applying
event outcomes or mandatory-finale exceptions. District assets are referenced directly; no
new independent IDs or encounter registry is needed.

## 2. Naming guard

The overworld resource must NOT be called "Action Points" — battle already owns that term
(`BattleStats.CurrentActionPoints`). Placeholder until decided: **Hours** (a day = N hours,
each location costs hours). ⚑ Confirm name + how many per day + what refreshes them.

## 3. Encounter architecture (locked shape, 2026-07-02)

**Principle: `BattleSession` stays untouched as the battle/encounter design system** (and the
standalone test path via `BattleTestStarter` + `BattleSessionBuilderWindow`). The campaign
layer *wraps* it. Everything campaign-side follows the codebase's two established patterns:
content = ScriptableObject assets, polymorphic behavior = `[SerializeReference]` hierarchies.

### The type tree

```
EncounterData (abstract SO)          — display name, blurb/art, hour cost
├─ BattleEncounterData               — payload: a BattleSession (+ boss flag, reward tier)
├─ EventEncounterData                — body text + 2–4 EventOptions (the StS dialogue node)
└─ ShopEncounterData                 — stock: cards/relics + prices  (⚑ blocked on currency)

EventOption (plain [Serializable])   — label + requirements + outcomes
RunRequirement (abstract, [SerializeReference])  — gates an option / a map location
    HasRelic · HasCardOfType · FundsAtLeast · OriginIs · DayAtLeast · ...
RunOutcome (abstract, [SerializeReference])      — mutates the RUN, not a battle
    GainRelic · GainCard · RemoveCard · GainFunds · StartBattle · Nothing(flavor) · ...
```

**The load-bearing idea:** `RunRequirement`/`RunOutcome` are the campaign-scope mirror of
`PassiveCondition`/`BattleEffect`. Same Odin type-picker authoring, same Database-window
auditability (one provider each), same generator-seeding pattern. Battle-scope effects
mutate battle state through `EffectExecutionContext`; run-scope outcomes mutate `RunState`.
New encounter richness = new Requirement/Outcome subclasses, zero flow changes (Open/Closed,
same as adding a `BattleEffect`).

Notes:
- **Conditional options**: an `EventOption` whose requirements fail renders disabled/hidden
  ("[Requires Fixer's Rolodex]") — evaluated against `RunState`.
- **`StartBattle` as an outcome** lets events escalate into fights (it points at a
  `BattleEncounterData`), so "dialogue that turns into a battle" is authorable, not coded.
- **A shop is NOT a special event**: buying is a loop (browse/spend/repeat), not a one-shot
  choice — own type, own panel. But its inventory grants reuse `RunOutcome`.
- **HQ** is just an `EventEncounterData` at hour cost 0 until HQ verbs are decided (⚑).

### Map + flow

| Type | Contents |
|---|---|
| `CampaignMapData` (SO) | the location list for a campaign (v1: one asset = one campaign) |
| `MapLocation` (Serializable) | an `EncounterData` + unlock requirements (reuses `RunRequirement`) + repeatable flag |
| `CampaignFlow` (Mono, campaign scene) | lists locations, spends Hours, dispatches on encounter type: Battle → hand `BattleSession` to the battle scene; Event → EventPanel; Shop → ShopPanel (later) |
| `MapLocation._cell` | the location's grid cell — where its hotspot sits on the iso map (§1.5) |
| `ZoneData`, `BuildingData` (SO) | zone bands with weighted building and landmark lists; half-cube building definitions (`city-generator.md` §3). Scenery only; hold no encounter data |
| City generator (Mono, campaign scene) | coast, radial avenues, streets, zones, landmarks and buildings from `RunState.Seed` (`city-generator.md` §4) |

### Scenes & modes (locked shape)

**Two scenes, three modes.** Encounter mode is a panel, not a scene — a dialogue box does
not justify a scene load, and keeping the map visible behind it preserves context (StS does
exactly this).

| Mode | Where | Shift mechanism |
|---|---|---|
| Battle | `main.unity` (existing, untouched) | full scene load via `SceneLoader` |
| Exploration | `campaign.unity` (new) — iso city map + location hotspots + HQ | scene load from battle; default mode of the scene |
| Encounter | `campaign.unity` — EventPanel/ShopPanel over the map | panel toggle inside `CampaignFlow` (no load) |

**Handoff contract — `RunState` is the only courier between scenes:**
1. `CampaignFlow` sets `RunState.Current.PendingBattle` (the chosen `BattleEncounterData`)
   → loads `main`.
2. Battle starter: `PendingBattle` non-null → consume it; null → inspector `BattleSession`
   fallback. **Pressing Play directly in `main.unity` therefore stays the untouched test
   path.**
3. `PostBattleFlow`: campaign active → load `campaign` (map, with the battle's rewards
   applied); no campaign → current reload/queue behavior (testing path).

Rules: no additive scene loading, no persistent cross-scene managers beyond the existing
`SceneLoader`/`RunState` — battle already tears down and rebuilds cleanly per load, keep it.
Both scenes must stay independently playable: the campaign scene continues the active profile's
saved run, or starts a new one with its inspector origin and seed, when no `RunState` exists
(same spirit as `BattleTestStarter`). Campaign panels follow the established
self-subscribing prefab-island pattern from the BattleUI decomposition (panel owns its
pixels, `Bind(flow)` for wiring, bus events = notifications only).

`RunState` grows: allies (done, as `Allies`), time, `Day`, `Funds` (⚑ name), per-location
visited flags. It survives scene reloads (static `Current`) and quitting: `SaveSystem` writes it at
campaign checkpoints (`meta-progression.md`).

## 4. Build phases

### Phase R — relic runtime (independent of map shape; do first)
> **Built as allies.** The code name is Ally (`AllyData`, `AllyDatabase`, `RunState.Allies`,
> `RecruitAllyOutcome`, `HasAlly`); see `naming-glossary.md`. The steps below keep the design term.

1. `RunState.Relics` + `AddRelic()`.
2. `PassiveResolver` takes optional run-level passives, folded into `_allPassives`;
   `BattleManager` passes `RunState.Current` relic passives at construction. Relics then
   ARE passives mechanically — zero new behavior code. (Since 2026-11-06 the class's innate passive is itself a starting ally; see `allies.md` §4.1.)
3. Prototype relic generator (reflection pattern like `EnemyRosterGenerator`) → 4–6 relics
   + `RelicDatabase` asset. The Database window's Allies tab already audits them.
4. Debug visibility only (overlay text). HUD relic bar = user-wired panel, later.
5. Acquisition arrives with its systems: boss reward (Phase M3) + event outcome (M2).
   Until then a debug grant proves the pipeline.

### Phase M1 — campaign skeleton
`CampaignMapData`/`MapLocationData` + `RunState` hours/day. Overworld = one scene: click a
location → spend hours → load encounter or event → return. HQ ends the day / rests, once nothing else fits (§1).

Presentation is locked to the iso city (§1.5) but **may land greybox** — untextured 2:1
diamonds and colored blocks on the real grid, hotspots at their real cells. Prove the loop on
the real projection, then swap art in without moving anything. A flat button list is no
longer an acceptable M1 shape: it hides exactly the layout questions the iso map has to
answer. The city generator (`city-generator.md`) runs from day one — placeholder tiles and greybox half-cubes give a
real generated city immediately, and swapping in art later touches no code.

### Phase M2 — events
`EventData` + outcome types + a simple event panel (text + option buttons). Event outcomes
can grant relics (random-event relic path lands here).

### Phase M3 — bosses + reward scaling
Boss flag on encounters (via `BattleSession`); boss victory → pick 1 of 3 relics.
`BattleResult` carries end-of-battle crowd stats (converted count, hostiles remaining,
meter margin); reward offer rarity/count scales off them through `RewardConfig`
(partly consumed: `PostBattleFlow` offers at the win's tier, with the encounter's `RewardOverride` as the base, and
rare pity; scaling off crowd stats is not wired yet).

### Phase M4 — campaign win/loss framing
⚑ What ends the campaign: fixed day count (election day)? Boss ladder? Blocks nothing
in M1–M3; decide during playtests.

## 5. Open questions (⚑ roll-up)

1. Overworld resource: final name, amount per day, refresh rules.
2. ~~HQ verbs: rest = heal what?~~ **Decided 2026-10-26:** Credibility is the run's HP. HQ restores
   it (Ad Spend Against Scandals) or upgrades a card (Focus on your speech).
3. Campaign end condition + boss cadence (how many bosses per campaign?).
4. Event content voice/tone + how many events v1 needs (guess: 5–8).
5. Do encounters cost more hours than events? ~~Does losing a battle end the run?~~ **Decided
   2026-10-26:** a loss costs Credibility scaled by how badly you lost (5 narrow, 30 crushing);
   the run ends at 0 Credibility, whether a battle or an event choice took it there, and a lost boss fight ends it outright. **Decided 2026-11-06:** a won boss fight restores Credibility to full for the next week, and a boss day has no HQ: it ends by facing the boss.
6. "Win well" metric weights — converted vs hostiles-left vs margin.
