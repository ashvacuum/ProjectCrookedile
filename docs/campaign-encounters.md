# Campaign Encounters

> **Kind:** Systems · **Status:** Built · **Updated:** 2026-10-03
>
> **Summary:** How encounters are typed, authored, scheduled and drawn, with every field, outcome, requirement and default: battles, events, chaining, flags, pools, travel, enemy moves, statuses, run state, rewards and the editor tools.
>
> **Source of truth:** [`Data/Campaign/`](../Assets/Scripts/Data/Campaign/), [`EncounterDesignerWindow.cs`](../Assets/Scripts/Editor/EncounterDesignerWindow.cs) · **Related:** [`metagame-campaign.md`](metagame-campaign.md) · [`meta-progression.md`](meta-progression.md) · [`campaign-ideation.xlsx`](campaign-ideation.xlsx)

[`metagame-campaign.md`](metagame-campaign.md) is the campaign design (the "why"). This doc is the
reference for the code that exists: how each piece works, then every field you can set, its default
and what it does.

> **Status:** playable end to end. `CampaignFlow` drives `campaign.unity`, draws each day from
> a pool, dispatches battles and events, and honours chaining. The run is saved and continued
> through `SaveSystem` ([`meta-progression.md`](meta-progression.md)). Its view is a temporary
> playtest UI (UI Toolkit built at runtime, plus an IMGUI stat strip).

---

## 1. The shape of an encounter

```
EncounterData (abstract SO)          ID, DisplayName, Blurb, HourCost, DropWeight,
                                    District, DurationMinutes, OpeningMinute, ClosingMinute
├── BattleEncounterData              wraps a BattleSession, IsBoss, RewardOverride
└── EventEncounterData               Body text + EventOption[]
                                         └── RunRequirement[] (gate) + RunOutcome[] (does)

EncounterPoolData                    Days + EncounterPoolEntry[]   ← scheduling lives here
```

Create assets via `Assets → Create → Crookedile → Campaign → …`.

`EncounterData.ID` is the asset's file GUID (kept in sync in `OnValidate`). Everything keys off it —
`RunState.VisitedLocationIds`, saves, pool exclusions — so renaming or moving
an asset never resets its run state.

### 1.1 Base fields (on every encounter)

| Field | Type | Default | What it does |
|---|---|---|---|
| `ID` | string, read-only | the asset's file GUID | Stable identity. Keys `VisitedLocationIds`, saves, pool exclusions. Never edit; survives renames and moves, and a duplicated asset gets its own |
| `DisplayName` | string | empty | Name on the map. Falls back to the asset name in tooling when blank |
| `Blurb` | text (2–4 lines) | empty | Short line on the map *before* the player commits |
| `HourCost` | int ≥ 0 | **1** | Whole hours a visit takes; add **Extra Minutes** (0–59) for the rest. The day budget is 8 hours |
| `DropWeight` | float ≥ 0 | **1** | Default relative draw chance. A pool entry can override it (§5) |
| `District`, `OpeningMinute`, `ClosingMinute` | travel fields | local, all day | Where it is and when it can be entered. See §6 |

An encounter is **self-contained** — no built-in "and then". Sequencing is a property of a
*choice* (`GoToEncounterOutcome`) or a *dependency* (`HasVisitedEncounter`); see §4.

### 1.2 Why ScriptableObject and not `[SerializeReference]`

The rule this codebase follows, consistently:

- **ScriptableObject = the noun you reference, name, and count.** `CardData`, `EnemyData`,
  `AllyData`, `EncounterData`.
- **`[SerializeReference]` = the polymorphic verb living inside it.** `BattleEffect` in
  `CardData`, `BattlePassive` in `AllyData`, `RunOutcome` in `EventOption`.

Encounters are nouns. They need stable identity across a scene load (`RunState.PendingBattle`),
they need to be enumerable (`t:EncounterData` is an asset query with no inline equivalent), and
they need to be *shared* — a pool entry, a hand-authored map list, and the deferred
`StartBattle` outcome all point at the same asset.

There's also a hazard: `[SerializeReference]` stores its type as an assembly + namespace +
class-name string. Rename or move that class and Unity throws "managed reference missing type"
and the authored data is gone unless `[MovedFrom]` was remembered. SO assets reference the
script's GUID, so renaming class and file together is harmless. Encounters are content authored
over months — not where that risk belongs.

---

## 2. Battle encounters

`BattleEncounterData` — the campaign never touches battle design, it just hands off a
`BattleSession` via `RunState.PendingBattle`.

| Field | Type | Default | What it does |
|---|---|---|---|
| `Session` | `BattleSession` | none | The fight. **Should be exactly one round** — multi-round can only chain battle→battle |
| `IsBoss` | bool | false | The finale. On a day it's offered, the map shows only "Face <boss>" (no travel or time cost) and the day can't end without it. The ally reward on victory (M3) isn't built |
| `RewardOverride` | `RewardConfig` | none | Per-encounter reward weights. **Authored but not yet consumed** — `PostBattleFlow` still calls `GenerateRewardOffer(count: 3)` with the database's built-in weights |

### 2.1 `BattleSession.BattleRound` — the tuning values for a fight

| Field | Type | Default | What it does |
|---|---|---|---|
| `label` | string | "Round" | Console log label only |
| `enemies` | `EnemyData[]` | empty | 1–5 enemies. List order = display order |
| `maxTurns` | int | **10** | Player turns before **Judgment**. `0` = no limit |
| `maxOpinion` | int ≥ 1 | **100** | Opinion Meter ceiling = the win threshold |
| `startingOpinion` | int ≥ 0 | **50** | Where the meter starts. Clamped to `0..maxOpinion` |

**Win/lose, exhaustively:**

| Condition | Result |
|---|---|
| Opinion ≥ `maxOpinion` | Victory, immediately |
| Opinion ≤ 0 | Defeat, immediately |
| `maxTurns` reached (and >0) | **Judgment**: victory if Opinion ≥ `maxOpinion / 2`, else defeat |

Defeat ends the run (`RunState.Clear`). Victory returns to the map with rewards.

---

## 3. Event encounters

`EventEncounterData` adds:

| Field | Type | What it does |
|---|---|---|
| `Body` | text (4–10 lines) | Scene text in the event panel (the `Blurb` is the map teaser) |
| `Options` | `EventOption[]` | Choices. **At least one**, or the player can't leave the panel |

### 3.1 `EventOption`

| Field | Type | What it does |
|---|---|---|
| `Label` | string | Button text |
| `ResultText` | text | Shown after the pick, before returning to the map. Optional |
| `Requirements` | `RunRequirement[]` | ALL must hold. Unmet options render **disabled with the reason**, never hidden |
| `Outcomes` | `RunOutcome[]` | Applied **in order, all of them**, when picked |

Both lists are `[SerializeReference]` — pick a concrete type from the Odin dropdown. A row with
no type picked is skipped at runtime and flagged by the Content Hub.

**Option gating** is how "you need the Bishop's Ring for this" works: put a `HasAlly` (or
`FundsAtLeast`, or anything else) in the option's `Requirements`. Unmet options render
**disabled with the reason attached**, never hidden — seeing the door you can't open is most of
what makes a gate interesting, and a hidden option just makes the event look shorter.
`CampaignFlow` re-checks availability in `ChooseOption` as well as in the view, because an
outcome applied from a locked option is silent and unrecoverable.

### 3.2 Outcomes — everything a choice can do

`RunOutcome` is the polymorphic base — abstract `Apply(RunState)` and `GetDescription()`,
mirroring the `BattleEffect` pattern one layer up (Odin type-picker dropdown, `[InfoBox]` live
description, `EditorSafeDescription` so one bad description can't break the whole asset's
inspector).

| Outcome | Fields | Effect |
|---|---|---|
| `AdjustFundsOutcome` | `Amount` (signed, default 10) | Funds ± amount, **clamped at 0** |
| `AdjustCredibilityOutcome` | `Amount` (signed, default 5) | Credibility ± amount, clamped at 0 |
| `AdjustCredibilityPercentOutcome` | `Percent` (signed) | Credibility ± a percentage of the origin's *starting* Credibility, so one asset means the same to every archetype |
| `RecruitAllyOutcome` | `Ally` | Adds an ally. Duplicates ignored (unique per run); its passives register into every battle |
| `GainCardOutcome` | `Card`, `Count` (≥1) | Adds N copies to the deck. How an event hands you a curse |
| `GainRandomCardOutcome` | `Scope` (Any / PlayerOrigin / Colorless), `RestrictType` + `Type`, `RestrictRarity` + `Rarity`, `Count` | Random card(s) from the chosen slice of the pool. Unrestricted rarity = rolls the reward weights (70/25/5). Skips cards the run hasn't unlocked |
| `GainCardFromPoolOutcome` | `Pool` (card list), `Count` | Random card from a hand-picked shortlist: "one of *these*", not "something" |
| `RemoveCardOutcome` | `Card` | Removes one copy. No-op if not held |
| `RemoveRandomCardOutcome` | `RestrictType` + `Type`, `Count` | Removes N random cards, each rolled from what's left. The cost side of a bargain |
| `RemoveChosenCardOutcome` | `RestrictType` + `Type`, `Prompt` | Player picks a card to remove — opens a picker |
| `UpgradeRandomCardOutcome` | `RestrictType` + `Type` | Upgrades one random upgradeable card |
| `UpgradeChosenCardOutcome` | `RestrictType` + `Type`, `Prompt` | Player picks a card to upgrade |
| `GoToEncounterOutcome` | `Encounter` | Chains straight into another encounter — **any type, including a battle**. Costs no Hours |
| `SetFlagOutcome` | `Flag` (string), `Clear` | Records a narrative flag — the memory of *this choice*. See §3.4 |
| `CoinFlipOutcome` | `SuccessChance`, `OnSuccess[]`, `OnFailure[]` | Rolls the run's seeded RNG and applies one outcome list. Only where the uncertainty is the point |
| `NextBattleHostilityOutcome` | `Amount` (signed) | Every enemy in the next battle starts this much more hostile (negative = more receptive) |
| `SpendTimeOutcome` | `Minutes` | Takes extra time off today's budget, on top of the encounter's duration |
| `AdvanceDayOutcome` | — | Ends the day as HQ does: next day, time refilled |
| `UnlockContentOutcome` | `Card` | Unlocks a card **for the profile**, from the next run on. Pair with a card whose unlock condition is `GrantedByEvent` to make the event the only way in. See `meta-progression.md` |

`CoinFlipOutcome` is the one exception to "no branching within an option". Use it for a gamble
the player knowingly takes; a consequence they should get to *choose* is still its own option.
Chains nested inside it run, but the Encounter Designer only draws top-level chain edges.

The card outcomes resolve the database through `CardDatabase.Shared` (`Resources/Databases/CardDatabase`)
rather than an inspector field, because outcomes are plain serialized classes with nowhere to
hang an asset reference.

**Outcomes are signed on purpose.** `RunState.GainFunds` was gain-only and was replaced by
`AdjustFunds`/`AdjustCredibility`. A choice layer where every option is a pure gain has no
decision in it — a choice needs to be able to cost something.

**Not built:** `Nothing` (flavour-only option). The planned `StartBattle` outcome was
generalised into `GoToEncounterOutcome` — it points at any encounter, not just a battle, which
is the whole point.

Upgrades swap the deck entry for a runtime `Instantiate` clone, so the shared SO is never
mutated. `CanUpgrade` is false on a card with no upgrade authored; the pickers no-op rather
than open an empty list.

**Adding a new outcome:** `[Serializable] class X : RunOutcome`, implement `Apply(RunState)`
and `GetDescription()`. No other file changes.

### 3.3 Requirements — everything a gate can test

Same authoring shape (`[SerializeReference]`, type dropdown, live description). Used in three
places: option gates, pool hard gates, pool weight boosts.

| Requirement | Field | Tests |
|---|---|---|
| `HasFlag` | `Flag` (string) | A `SetFlagOutcome` set that flag this run — **the choice-level test** |
| `HasVisitedEncounter` | `Encounter` | That encounter was resolved this run (knows you were there, not what you did) |
| `FundsAtLeast` | `Amount` (default 50) | `Funds ≥ amount` |
| `CredibilityAtLeast` | `Amount` (default 25) | `Credibility ≥ amount` |
| `HasAlly` | `Ally` | Run holds that ally |
| `OriginIs` | `Origin` | The run's origin. The gate for class-specific options |
| `DayAtLeast` | `Day` (≥1, default 3) | `Day ≥ n` |
| `HasUnlocked` | `Card` | The run started with that card unlocked (reads the run's unlock snapshot) |

Every one has a **`Negate`** toggle on the base class, so "hasn't visited X" (mutually
exclusive branches) needs no extra type. A null `RunState` (edit-time preview) passes.

### 3.4 Narrative flags — "this choice changes what shows up later"

`SetFlagOutcome` writes a free-form string into `RunState.Flags`; `HasFlag` reads it. That pair
is the whole system, and it works in all three requirement slots.

**Raised chance** — the usual case. On the *later* encounter's pool entry:

```
Bribe Night  →  option "Take the envelope"  →  SetFlagOutcome  flag = took_bribe

The Auditor (pool entry)
  Weight           1
  BoostIf          HasFlag "took_bribe"
  BoostMultiplier  4        ← 4× as likely to be drawn on any eligible day
```

**Hard unlock** — same flag in `Requirements` instead: the encounter cannot appear at all until
the flag is set. Use `Requirements` for content that makes no sense unprompted, `BoostIf` for
content that should merely lean toward the story you're actually in.

**Locked-out branch** — tick `Negate` on the `HasFlag`: appears only if you *didn't* take the
bribe. Mutually exclusive arcs cost one flag, not two.

**Gated dialogue** — the same `HasFlag` in an `EventOption.Requirements` makes an option
appear disabled-with-reason until the flag is set: "You know what he did. (Requires: flag
"took_bribe")".

Conventions, since nothing validates the strings:

- lowercase snake_case, named for the *fiction* (`took_bribe`), not the mechanic (`event3_opt1`)
- one flag per beat that something later reads — don't set flags nothing tests
- `Clear` exists for arcs that close ("the debt is paid"); most flags are one-way
- flags live on `RunState`, so they reset with the run and never persist between runs

Flags are deliberately *not* an enum: a story beat shouldn't need a code change and a recompile.
The cost is typos being silent — check the spelling against wherever you test it.

---

## 4. Chaining — encounters are not all battles

**A `BattleSession` is a test-harness construct**, not campaign content. It's an ordered
gauntlet of fights, and a multi-round one can only ever chain **battle → battle** with nothing
possible between them. A `BattleEncounterData` should normally wrap a session of exactly
**one round**.

**An encounter is self-contained.** It has no "and then go here" of its own — sequencing is a
property of a *choice*, not of an encounter. There are exactly two ways one encounter can lead
to another, and they differ in *when*:

| Mechanism | Resolves | Costs Hours | Example |
|---|---|---|---|
| `GoToEncounterOutcome` on an `EventOption` | Immediately, same visit | No | "Refuse the bribe → fight his muscle now" |
| `HasVisitedEncounter` requirement on a pool entry | Later, as a map location | Yes | "After meeting the Fixer, The Favour appears from day 4" |

A `GoToEncounterOutcome` can point at a **battle**, which is what makes "refuse him and fight"
one option on one event rather than a two-round `BattleSession`.

It writes `RunState.NextEncounter` — "don't return to the map yet, resolve this first" —
consumed by `CampaignFlow.ResolveChainOrRefresh`. That's distinct from `RunState.PendingBattle`,
which means "the battle scene should load this on the next scene load".

> **Removed 2026-07-28:** `EncounterData.NextEncounter`, an unconditional per-asset chain. It
> meant "this *always* leads there", which contradicts self-contained encounters, and everything
> it did is covered better by one of the two mechanisms above.

`CampaignFlow.ResolveChainOrRefresh` consumes `NextEncounter` on every return to the map, so a
chain resolves instead of re-rendering the locations. A battle's chain is queued *before* the
scene load so it survives the round-trip.

---

## 5. Scheduling — `EncounterPoolData`

| Pool field | Default | What it does |
|---|---|---|
| `Days` | **7** | Campaign length. Past this the run ends ("Campaign complete") |
| `Entries` | — | One row per encounter, table-list |

### 5.1 `EncounterPoolEntry`

| Field | Default | What it does |
|---|---|---|
| `Encounter` | none | The asset. A blank row can never be drawn |
| `FirstDay` | 1 | First day it can appear, inclusive |
| `LastDay` | 0 | Last day, inclusive. **0 = open-ended** |
| `Weight` | **-1** | Per-pool draw weight override. `-1` = inherit `DropWeight`. `0` disables the row without deleting it |
| `OncePerRun` | **true** | Once drawn, never offered again |
| `Guaranteed` | false | Always appears every day in its window, **ahead of the random picks and ignoring the per-day count**. This is how a day-7 boss or day-1 opener is made certain |
| `Requirements` | empty | **Hard gate** — all must hold or it can't appear at all |
| `BoostIf` | empty | **Soft nudge** — when all hold, weight × multiplier. Stays available either way |
| `BoostMultiplier` | **2** | Applied when every BoostIf holds. **A 0 here erases the weight it's meant to favour** — the Content Hub flags it |

Effective weight = `ResolvedWeight × (BoostActive ? BoostMultiplier : 1)`.

**`Guaranteed` is how fixed structure is built.** A weight only makes an encounter *likely*,
and "likely" isn't something you can design a run around. Guaranteed entries are added to the
day's offering before the random picks and ignore the per-day count, so a day-7 boss can't be
crowded out by a full slate of events. Set `FirstDay = LastDay = 7` and tick it. Same mechanism
for a fixed day-1 opener. The Gantt shows these as `ALWAYS`.

### 5.2 Dependencies — one encounter unlocking or favouring another

`Requirements` (hard gate) and `BoostIf` (soft nudge) take the same `RunRequirement` list as
option gates (§3.3). `HasVisitedEncounter` keys off `RunState.VisitedLocationIds`, which is why encounters needed a
stable GUID id.

**A null `RunState` means "requirements pass."** Edit-time tooling has no run to test against,
and a preview that showed an empty board would be useless. So `DrawForDay`, `EligibleOn`, and
`IsEligibleOn` all take an optional state: `CampaignFlow` passes the live run, the designer
window passes null.

### 5.3 Drop chance resolves in two levels

1. **`EncounterData.DropWeight`** — the encounter's own chance, defaulting to 1. This is the
   "default if left empty".
2. **`EncounterPoolEntry.Weight`** — a per-pool override. Defaults to `-1`, meaning inherit.
   Set a number only when the encounter should be rarer or commoner *in this pool* than it is
   by default. `0` disables the row without deleting it.

`EncounterPoolEntry.ResolvedWeight` is the single resolution point — draws, eligibility checks,
and the Gantt view all read it, so the fallback can't drift between them.

### 5.4 Drawing

`CampaignFlow` draws `_locationsPerDay` (**3**) per day, deterministic from `(seed, day)`.
Guaranteed entries first, then weighted picks from what's eligible and not already visited.
Fewer than requested (possibly zero) when the eligible set is too small — the Encounter
Designer's coverage strip is what makes an empty day visible.

```csharp
List<EncounterData> today = pool.DrawForDay(day, count, seed, runState.VisitedLocationIds);
```

Deterministic from `(seed, day)`. One `System.Random` per day, so day 5 is reproducible without
day 4 having run first. Uses its own RNG rather than `RandomHelper`/`UnityEngine.Random`
deliberately: **seeding the campaign must never perturb battle RNG, or vice versa.**

### 5.5 Seeds — what they do and don't cover

`RunState.Seed` is set at `Create`. Passing `0` (the default, and every existing call site)
picks a random one; any other value replays that exact campaign.

Two streams derive from it, and one thing deliberately doesn't:

| Stream | Covers | Source |
|---|---|---|
| Encounter draws | Which locations appear on which day | `EncounterPoolData.DrawForDay`, own `System.Random` per (seed, day) |
| `RunState.Rng` | Card reward offers, `GainRandomCardOutcome`, `CoinFlipOutcome` | One `System.Random` per run, offset from the seed |
| **Not seeded** | Battle shuffles, chance effects, Confused rolls | `UnityEngine.Random` |

**Battle RNG stays unseeded on purpose.** Sharing one global stream would make the campaign
seed depend on how many times combat happened to roll — adding a single shuffle anywhere would
silently change every later reward. It would also make reloading a fight replay identical
draws. Isolated streams mean the seed keeps meaning the same thing as the game grows.

The reward stream still diverges if the player makes different choices, which is correct: the
seed fixes the *map*, and identical play gives identical results.

Both `CardDatabase.GenerateRewardOffer` and `GetRandomByRarityWeight` take an optional
`System.Random`; passing null keeps the old `UnityEngine.Random` behaviour, so editor tools and
non-run callers are unaffected.

---

## 6. Time of day and district travel

Day selection remains on pool rows; the encounter asset owns its daily entry window and
district. The pool's **Travel** field references a `CampaignTravelData` road network.
Create assets through **Assets → Create → Crookedile → Campaign → District / Travel Network**.
Assign HQ on the network, add roads, and assign districts on encounters. Odin exposes road
endpoints, direction, clear travel minutes, and traffic windows directly in the inspector.

Under an encounter's **Travel and time** foldout:

- **District:** blank = local, no travel and no relocation.
- **Extra Minutes:** 0–59, added to the existing Hours field. Hours 0 + Extra Minutes 30 = 30 minutes.
- **Opening Minute:** inclusive minute since midnight; 480 = 08:00.
- **Closing Minute:** exclusive latest-entry minute; 1020 = 17:00. 0 means midnight.
  A valid window has closing after opening. Entry before closing can finish afterwards.

The current routing assets live in `Assets/Data/Campaign Routing`. HQ, the residential
barangay, market/plaza, civic center, business district, and riverside form six connected
districts. The civic-to-business bridge carries directional commuter traffic; the market-to-riverside
bridge provides a quieter southern detour. Rush windows are 07:30–09:30 and 15:00–17:00.
Use Encounter Designer's travel graph and inline Odin inspectors to edit these assets.

The daily budget is eight hours (08:00–16:00), including travel and waiting. Brgy Fiesta
is a five-hour commitment with entry from 09:00 until 11:00 exclusive. Other paid visits
take 45–120 minutes; HQ remains free. Morning market visits, office meetings, and afternoon
civic events have different entry windows. These are entry deadlines, not forced finish times.

Road traffic windows are also inclusive at start and exclusive at end. Split overnight
windows into two rows. The largest active multiplier wins if windows overlap. Fastest-route
search samples all roads at trip departure; previews and commits use the same result.

An 08:00 departure with 30-minute travel to a 09:00 event includes 30 minutes of waiting.
A 30-minute event then finishes at 09:30, spending 90 minutes from the daily Hours budget.
The player can also wait 15 minutes on the map to change departure time. No path, arrival at
or after closing, invalid windows, insufficient total time, and crossing midnight block entry.
The clock and last district survive battle returns. Advancing the day resets both time and HQ.

Daily offerings remain cached across time changes and scene loads. Future/expired entries
remain visible; they are not rerolled. Direct narrative chains retain their existing free,
same-visit semantics and do not relocate the player. The mandatory boss finale bypasses time
and travel restrictions so the run cannot be stranded; its separate button states this exception.

**Encounter Designer → Travel** previews every pool entry from a chosen district, departure
minute, and remaining budget. Click **Edit** on an encounter or the network button to open
its full Odin inspector in the Designer's **Authoring** tab.

The Travel tab's **District connections** diagram shows HQ, road endpoints, and every
district referenced by the pool (including disconnected districts). Drag nodes to arrange
them; positions live on the shared district assets and support Undo. Select a district to
filter its encounters, or **Show all encounters** to include local encounters too.
**Show route** on an encounter highlights the same quickest route used by gameplay.
Scrubbing departure time updates road durations and route selection: blue marks the
selected route, orange marks delayed roads, and arrows show allowed directions. Road
labels show minutes and traffic multiplier before ally discounts; visit previews include allies.

**New district** creates HQ when none exists; otherwise it connects the new district to
the selected district (or HQ) with a 15-minute two-way road. **Add road** connects the
selected district to an existing district, then opens the network inspector. Click a road
label to edit road direction, base duration, and traffic windows through Odin. Save assets
from Authoring to persist edits to disk.

Each encounter has a 00:00–24:00 timeline: green is the entry window; below it, blue is
travel, amber is waiting, purple is encounter duration, and gray is the remaining budget.
The vertical marker is departure. Blocked visits retain their reason. This previews pool
entries, not a day's generated offer: use Timeline and Simulate for daily draws and gating.

**Authoring** edits the original assets directly with Odin's validation, polymorphic type
pickers, and Undo. Use **Pool** to edit scheduling requirements and boosts or create an event
or battle. The pool's **Travel → New** button creates and assigns a network. **New district**
creates a district beside the pool; the **New** button on an encounter's District field also
assigns it immediately. Expand district references on encounters and road endpoints to edit
them inline. The network itself expands inline on the pool. **Save assets** persists pending
asset edits. These are shared assets: editing a district updates every reference to it.
**Simulate** greedily visits the reachable encounter finishing earliest, including travel,
waiting, and duration, and only consumes encounters visited. Requirements still pass in this
editor simulation and event outcomes are not applied; it is not a complete playthrough model.

Run **Crookedile → Campaign → Run Travel Checks** for route, traffic, clock, window, and
visit-state regression checks. This uses temporary in-memory assets and does not replace the
active run. Existing content needs no migration: unset fields mean local all-day encounters.

### 6.1 Ally overworld passives

On the **Ally ScriptableObject (`AllyData`)**, add entries under **Overworld passives** using
Odin's `[SerializeReference]` type picker. Each entry owns its settings and description,
separately from the ally's existing battle passive list. The first two authorable types are:

- **Reduce Travel Time Passive:** reduces the selected route's duration after traffic.
- **Reduce Encounter Duration Passive:** reduces the duration of both event and battle visits.
  It does not speed up combat animations or reduce waiting for an event to open.

Each has a **Reduction Percent** field (25% on a newly added entry). An empty list grants
no overworld benefits. Recruited allies' passive percentages add together independently for each
category, capped at 100%. Fractional minutes round up, and a positive duration stays at least
one minute. Already-free travel/encounters remain free. An ally may grant either bonus,
both, battle passives, or any combination; the generated description and Content Hub audit
recognize campaign-only allies.

These are plain serializable classes, not separate ScriptableObject assets. They are saved
inside the ally asset and remain editable inline from Encounter Designer. Both are listed
in **Crookedile → Authoring Catalog → Overworld passives**, including their fields and usages.

To add another visit modifier, create a `[Serializable]` subclass of `OverworldPassive`,
implement `ModifyVisit(EncounterData, ref VisitModifiers)` and `GetDescription()`, and give
its private serialized fields tooltips. Odin discovers the type automatically; no switch,
registry, or change to `AllyData` is needed. `ModifyVisit` is a pure preview hook: only change
the provided modifiers, never mutate the run, encounter, or passive instance. New kinds of
overworld triggers beyond visit-cost calculation will need their own runtime hook.

For example, 25% travel reduction changes a 30-minute traffic-adjusted trip to 23 minutes.
50% encounter reduction changes a 30-minute encounter to 15 minutes. If faster travel gets
you there before opening, the extra waiting still costs time. Traffic severity continues to
describe road conditions; the preview lists the ally's saved minutes separately.

In **Encounter Designer → Authoring**, use **New ally** or select an existing ally in
**Editing asset**. In an event's `RecruitAllyOutcome`, expand the **Ally** reference to edit
that same asset inline with Odin. Recruitment activates bonuses for subsequent visits; it
does not refund the visit already in progress. In **Travel** or **Simulate**, use **Add preview
ally** to choose a hypothetical recruited roster and **Edit ally** to tune it in Authoring.
The roster is shared between these two previews; it does not recruit anyone in the live run.

The runtime, Travel preview, and simulation all use the same cost calculation. Duplicate ally
references count once. Timing checks also cover stacking, rounding, opening/closing windows,
and charging exactly the previewed cost after recruitment.

---

## 7. Enemy abilities — what a fight is built from

Encounters don't own abilities; enemies do. Values you can set per `EnemyData`:

| Field | Default | What it does |
|---|---|---|
| `EnemyName`, `Portrait` | — | Display |
| `StartingHostility` | 0 | Position on the hostility line. Negative = receptive, positive = hostile |
| `MaxHostility` | **+5** | Ceiling |
| `MinHostility` | **-3** | Floor (most receptive) |
| `NeutralZone` | **0** | Buffer around 0 that must be crossed to commit to a side. `2` = must exceed +2 to go Aggressive, below -2 to go Receptive |
| `StartingEffects` | empty | Statuses applied at battle start: behavior + `Stacks` + `Duration` |
| `Passives` | empty | Reactive trigger/condition/effect abilities, same system as card and origin passives |
| `MovePattern` | Sequential | `Sequential` / `Random` / `RandomSequential` (random start offset, then in order) |
| `AggressiveMoves` / `NeutralMoves` / `ReceptiveMoves` | empty | **The move list is chosen by current stance** — an enemy's options are driven entirely by how it feels about the player |

### 7.1 `EnemyMoveData`

| Field | What it does |
|---|---|
| `MoveName` | Internal name |
| `MoveType` | Intent category — drives the badge icon/colour (see below) |
| `IntentDescription` | The telegraph line the player reads before it acts |
| `Effects` | Polymorphic `BattleEffect` list. Avoid CardManipulation effects — enemies have no deck |
| `MoveVFX` | Optional, non-blocking |
| `CounterCardType` | *Counter moves only.* Fires only if the player played that card type this turn, else fizzles to Idle |
| `MinionToSummon`, `MinionCount` | *Summon moves only.* Capped so total enemies stay ≤ 5 |
| `Condition` + `ConditionTurn` / `ConditionPercent` | When the move is eligible (below) |

**Intent types:** `Attack`, `Defend`, `Buff`, `Debuff`, `OffensiveBuff`, `DebuffAttack`,
`SummonMinion`, `Idle`, `DefendOpinion`, `RileOthers`, `Ward`, `Counter`.
*Integers are serialized — append new ones, never reorder.*

**Move conditions** (re-checked every selection, so mid-battle changes count):

| Condition | Parameter | Eligible when |
|---|---|---|
| `None` | — | Always |
| `OnlyIfNoMinionsAlive` | — | No living enemy matches `MinionToSummon` |
| `OnTurnOrAfter` | `ConditionTurn` | Turn ≥ n |
| `BeforeTurn` | `ConditionTurn` | Turn < n |
| `EveryNTurns` | `ConditionTurn` | Turn divisible by n |
| `OpinionAtOrAbove` | `ConditionPercent` (0–100) | Meter ≥ n% — desperation/phase moves |
| `OpinionAtOrBelow` | `ConditionPercent` | Meter ≤ n% — finishers |

---

## 8. Statuses — the in-battle vocabulary

Applied by card effects, enemy moves, or an enemy's `StartingEffects`. Every one takes a stack
count and a duration type.

**Durations:** `DecreasePerTurn` (default, -1/turn) · `RemoveEndOfTurn` · `Permanent` ·
`RemoveAtPlayerTurnStart`.

| Debuffs | Buffs |
|---|---|
| `Weakened` — deals N less Opinion | `Strength` — deals N more Opinion |
| `Frail` | `Dexterity` — N more Support per card |
| `Vulnerable` | `Focus` — cards cost N less AP this turn |
| `Entangled` | `Energized` — cards cost N less AP this turn |
| `Exposed` | `Plated` — reduces incoming Opinion by N |
| `Smear` — take N Opinion at end of turn | `Regeneration` — raise Opinion by N at end of turn |
| `Confused` | `Intangible` |
| `Silenced` | `Thorns` — reflect N Opinion when hit |
| `Stunned` | `Warded` — guards allies from hostility shifts/debuffs |
| `Rattled` | `Hardened` — resists hostility gains by N |
| `Doubt` | `Fanatic` |
| `Jaded` — Pacify cost +N, permanent | `Devotion` |
| `Guilt` | `Ritual` — gain N Support at start of each turn |
| `Shame` | `Momentum` / `Echo` / `Turncoat` — deals +N Opinion while freshly betrayed |

---

## 9. Run state — what persists between encounters

`RunState` is the run. Everything an encounter can move:

| Value | Start | Changed by | Notes |
|---|---|---|---|
| `Funds` | per origin | `AdjustFundsOutcome` | Clamped at 0, no ceiling |
| `Credibility` | per origin | `AdjustCredibility(Percent)Outcome` | Clamped at 0, no ceiling. **Meta only — battle never reads it** |
| Time (`MinutesRemaining`) | `MaxHours` × 60 | visits (travel + duration), `SpendTimeOutcome`, Wait | Refills on day end; the day also ends at midnight |
| `Day` | 1 | End Day, `AdvanceDayOutcome` | Run ends past `pool.Days` |
| `Deck` | starter deck | card outcomes + post-battle rewards | Holds SO refs; upgrades store clones |
| `Allies` | empty | `RecruitAllyOutcome` | Unique per run; passives register into every battle |
| `VisitedLocationIds` | empty | every resolved encounter | Drives `OncePerRun` and `HasVisitedEncounter` |
| `Flags` | empty | `SetFlagOutcome` | Narrative memory of *choices*. Read by `HasFlag`. Run-scoped: saved with the run, gone when it ends |
| `NextBattleHostility` | 0 | `NextBattleHostilityOutcome` | Spent by the next battle |
| `UnlockedContent` | the profile's unlocks | — | Snapshot taken when the run starts; read by reward pools and `HasUnlocked` |
| `Seed` / `Rng` | random (or set) | — | Fixes the *map* and reward offers. Battle RNG is deliberately unseeded |

**The run is saved** (`SaveSystem`) on every map redraw and before each battle, so quitting and
re-entering the campaign continues it. Quitting mid-battle restarts that battle. A new field that
must survive this goes in `RunSaveData` and `RunState.Save.cs` too.

### 9.1 Where a run starts

Starting Funds, Credibility, and day length live on **`OriginDatabase.Entry`**, per archetype —
the same asset that already owns each origin's portrait, passive, starter tag, and Max AP.
`RunState.Create` reads them via `OriginDatabase.Shared`, so no run-creation path can start a
player at zero by forgetting to pass them along.

| Origin | Funds | Credibility | Reads as |
|---|---|---|---|
| Faith Leader | 150 | 90 | Least money, most trusted — has to earn its way through events |
| Nepo Baby | 600 | 65 | Can buy any option on the board, trusted least |
| Celebrity | 250 | 79 | Comfortable and well-liked, until the first scandal |

Current values in `OriginDatabase` (all three have Max hours 8). They are placeholders, not
balance — tune them in the inspector. `MaxHours` of `0` means "use the run's default" (8), so day length only differs per
origin if you deliberately set it.

This is a real design lever rather than flavour: starting Funds decides which event options are
even *reachable* on day one, which is the cheapest way to make two archetypes feel different in
the campaign layer without any new systems.

> **Naming trap:** `core-design.md` §140 records a **battle** resource called "Credibility"
> that was cut as over-engineered (Overload / exposure cliff / fabricated tags). The campaign
> meta stat here is unrelated — battle never reads it.

---

## 10. Rewards

Post-battle, on victory → Continue:

| Knob | Where | Default |
|---|---|---|
| Offer count | `PostBattleFlow` (hardcoded) / `RewardConfig.DefaultOfferCount` | **3** |
| Rarity weights | `CardDatabase.GenerateRewardOffer` / `RewardConfig` | Basic **70** / Enhanced **25** / Rare **5** |
| RNG | `RunState.Rng` | Seeded per run |

Rewards are a **pick-1-of-3 card, or skip**. Nothing else drops yet: no Funds, no allies, no
boss reward. The offer skips cards the run hasn't unlocked, and Policies tagged with another
origin (class Policies stay in their class; untagged ones are universal). `BattleEncounterData.RewardOverride` and `IsBoss` are authored fields that
nothing consumes — wiring `GenerateRewardOffer` to read `RewardConfig` is the open task.

---

## 11. Validation and tools

### 11.1 Content Hub

**Crookedile → Content Hub** has two campaign categories:

- **Campaign encounters** — events with no options (unleavable), options with no label, options
  that do nothing *and* say nothing, `[SerializeReference]` rows where no type was picked,
  battle encounters with no session, and sessions with more than one round (chain with a
  `GoToEncounterOutcome` instead).
- **Encounter pools** — day coverage (a day with nothing eligible is an empty map, and is
  invisible from the inspector), unreachable windows, weight-0 non-guaranteed entries,
  dependencies on encounters outside the pool (never satisfiable), pools with no boss, and a
  boost multiplier of 0 — which older assets deserialize by default and which *erases* the
  weight of whatever it's meant to favour.

The pre-existing **Encounters** category was renamed **Battle sessions**, which is what it
actually audits — a session is a test-harness gauntlet, not campaign content.

> Pool coverage is evaluated without a `RunState`, so requirement gates count as passing. A day
> covered only by a gated encounter reads as covered; the dangling-dependency check is what
> catches the common version of that mistake.

### 11.2 Finding encounters

There is no encounter database. The save system resolves encounter IDs through the campaign pool and every encounter
its events chain to (`SaveContent`), and the editor tools find them with an asset query (`t:EncounterData`).

### 11.3 Encounter Designer

**Crookedile → Encounter Designer.** Timeline, Table, Dependencies, Flags, Simulate, Travel,
and Authoring views over one pool. Authoring embeds the original asset's Odin inspector;
Table provides compact bulk scheduling edits. Click encounters in Timeline, Table,
Dependencies, or Travel (or a flag owner in Flags) to edit them in Authoring without leaving
the window. Undo/Redo and authoring edits invalidate cached previews; rerun simulations to
see updated results.

**Timeline** — the Gantt, plus a seed simulator.

- A bar per entry spanning its day window, coloured by encounter type. `w2` = weight overridden
  on that row, `w2*` = inherited from the encounter's `DropWeight`. Click a row label to edit
  the asset in Authoring.
- **Coverage strip** — eligible count and total weight per day, red when a day has nothing
  eligible. That day would hand the player an empty map, and it's the one authoring mistake in
  this asset that fails silently.
- **Seed roller** — enter a seed, hit Roll, and every day's real draw appears under its column.
  Runs the actual `DrawForDay` with once-per-run exclusions carried forward, so it matches what
  a live run produces rather than approximating per-day.

A dash in the preview means the pool ran dry for that slot: not enough distinct eligible
encounters remained once once-per-run picks were spent.

**Dependencies** — the unlock graph. Nodes are encounters, laid out left to right by depth in
the chain (longest hard-requirement path). Solid arrow = hard gate, dotted = weight boost. Drag
to untangle; positions aren't saved.

The two tabs answer different questions and neither subsumes the other. A gated entry's Timeline
bar shows when it *could* appear, not whether it will — those rows are tagged `[dep]` there to
stop the timeline being read as the whole truth. Depth is computed by iterative relaxation
rather than recursion, so a cyclic authoring mistake settles instead of blowing the stack.

---

## 12. Gaps and known shortcuts

- **`RewardOverride` is inert.** Set it for future-proofing; it doesn't change a run today.
- **Battle rewards are cards only.** An event outcome is currently the only way to hand out Funds, Credibility, or an ally.
- **No per-day weight curve.** An encounter's odds vary only because the *eligible set* varies.
- **The campaign screen is a playtest UI.** `CampaignFlow` builds a UI Toolkit list at runtime plus an IMGUI stat strip; it is a harness for judging the loop, not the shipping map.
- **A pending card pick isn't saved.** Quitting while a chosen-card upgrade/removal picker is open loses that pick on resume.
- **Flat weights, no per-day curve.** Chance varies by day only because the *eligible set*
  varies. An encounter whose own odds ramp across days needs an `AnimationCurve` on the entry —
  one field and one lookup in `ResolvedWeight`. Worth adding only if flat weights feel wrong in
  balancing.
- **Weight inheritance uses a `-1` sentinel**, not a bool + `ShowIf` pair. One field, and the
  tooltip carries the meaning. Swap to the toggle if designers keep typing `0` for "default"
  and silently disabling rows.
- **`EncounterPoolEntry` is a plain `[Serializable]` class**, not polymorphic. If a second
  *kind* of scheduling rule ever appears ("only after a boss", "only if Credibility < 20"),
  that's when a `[SerializeReference] EncounterSchedule` base earns its place — the encounter
  stays an SO, only the *when* rule becomes polymorphic.
