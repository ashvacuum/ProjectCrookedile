# Encounter Authoring Reference — every knob, every value

*A designer/dev sheet: what an encounter is made of, every field you can set, its default,
and what it does. Written 2026-08-23 against the code as it stands.*

Companion docs: [`campaign-encounters.md`](campaign-encounters.md) is the *why* (architecture,
chaining rules, seeds, editor tools). [`metagame-campaign.md`](metagame-campaign.md) is the
campaign design. This file is the flat lookup table — if you want to know "what can I change",
it's here.

---

## 1. The shape of an encounter

```
EncounterData (abstract SO)              ID, DisplayName, Blurb, HourCost, DropWeight
├── BattleEncounterData                  Session, IsBoss, RewardOverride
└── EventEncounterData                   Body + EventOption[]
                                             └── RunRequirement[] (gate) + RunOutcome[] (does)

EncounterPoolData                        Days + EncounterPoolEntry[]   ← scheduling lives here
```

Create assets via `Assets → Create → Crookedile → Campaign → …`.

### 1.1 Base fields (on every encounter)

| Field | Type | Default | What it does |
|---|---|---|---|
| `ID` | string, read-only | auto GUID | Stable identity. Keys `VisitedLocationIds`, `EncounterDatabase`, pool exclusions. Never edit; survives renames/moves |
| `DisplayName` | string | empty | Name on the map. Falls back to the asset name in tooling when blank |
| `Blurb` | text (2–4 lines) | empty | Short line on the map *before* the player commits |
| `HourCost` | int ≥ 0 | **1** | Hours spent to visit. Day is 3 hours by default, so 1 = three visits/day |
| `DropWeight` | float ≥ 0 | **1** | Default relative draw chance. A pool entry can override it (§4) |

An encounter is **self-contained** — no built-in "and then". Sequencing is a property of a
*choice* (`GoToEncounterOutcome`) or a *dependency* (`HasVisitedEncounter`).

---

## 2. Battle encounters

`BattleEncounterData` — the campaign never touches battle design, it just hands off a
`BattleSession` via `RunState.PendingBattle`.

| Field | Type | Default | What it does |
|---|---|---|---|
| `Session` | `BattleSession` | none | The fight. **Should be exactly one round** — multi-round can only chain battle→battle |
| `IsBoss` | bool | false | Reserved for pick-1-of-3 relic on victory (M3). **Currently read by nothing but the Content Audit** |
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
no type picked is skipped at runtime and flagged by the Content Audit.

### 3.2 Outcomes — everything a choice can do

| Outcome | Fields | Effect |
|---|---|---|
| `AdjustFundsOutcome` | `Amount` (signed, default 10) | Funds ± amount, **clamped at 0** |
| `AdjustCredibilityOutcome` | `Amount` (signed, default 5) | Credibility ± amount, clamped at 0 |
| `GrantRelicOutcome` | `Relic` | Adds a relic. Duplicates ignored (unique per run) |
| `GainCardOutcome` | `Card`, `Count` (≥1) | Adds N copies to the deck. How an event hands you a curse |
| `GainRandomCardOutcome` | `RestrictRarity` + `Rarity` | Random card. Unrestricted = rolls the reward weights (70/25/5) |
| `RemoveCardOutcome` | `Card` | Removes one copy. No-op if not held |
| `RemoveRandomCardOutcome` | `RestrictType` + `Type`, `Count` | Removes N random cards, each rolled from what's left. The cost side of a bargain |
| `RemoveChosenCardOutcome` | `RestrictType` + `Type`, `Prompt` | Player picks a card to remove — opens a picker |
| `UpgradeRandomCardOutcome` | `RestrictType` + `Type` | Upgrades one random upgradeable card |
| `UpgradeChosenCardOutcome` | `RestrictType` + `Type`, `Prompt` | Player picks a card to upgrade |
| `GoToEncounterOutcome` | `Encounter` | Chains straight into another encounter — **any type, including a battle**. Costs no Hours |
| `SetFlagOutcome` | `Flag` (string), `Clear` | Records a narrative flag — the memory of *this choice*. See §3.4 |

Card outcomes resolve the database by path (`Resources/Databases/CardDatabase`), not an
inspector field — outcomes are plain serialized classes with nowhere to hang a reference.

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
| `HasRelic` | `Relic` | Run holds that relic |
| `DayAtLeast` | `Day` (≥1, default 3) | `Day ≥ n` |

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

## 4. Scheduling — `EncounterPoolData`

| Pool field | Default | What it does |
|---|---|---|
| `Days` | **7** | Campaign length. Past this the run ends ("Campaign complete") |
| `Entries` | — | One row per encounter, table-list |

### 4.1 `EncounterPoolEntry`

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
| `BoostMultiplier` | **2** | Applied when every BoostIf holds. **A 0 here erases the weight it's meant to favour** — the Content Audit flags it |

Effective weight = `ResolvedWeight × (BoostActive ? BoostMultiplier : 1)`.

### 4.2 Draw

`CampaignFlow` draws `_locationsPerDay` (**3**) per day, deterministic from `(seed, day)`.
Guaranteed entries first, then weighted picks from what's eligible and not already visited.
Fewer than requested (possibly zero) when the eligible set is too small — the Encounter
Designer's coverage strip is what makes an empty day visible.

---

## 5. Enemy abilities — what a fight is built from

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

### 5.1 `EnemyMoveData`

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

## 6. Statuses — the in-battle vocabulary

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

## 7. Meta state — what persists between encounters

`RunState` is the run. Everything an encounter can move:

| Value | Start | Changed by | Notes |
|---|---|---|---|
| `Funds` | per origin | `AdjustFundsOutcome` | Clamped at 0, no ceiling |
| `Credibility` | per origin | `AdjustCredibilityOutcome` | Clamped at 0, no ceiling. **Meta only — battle never reads it** |
| `Hours` | `MaxHours` (3) | `HourCost` on visit | Refills on day end |
| `Day` | 1 | End Day | Run ends past `pool.Days` |
| `Deck` | starter deck | card outcomes + post-battle rewards | Holds SO refs; upgrades store clones |
| `Relics` | empty | `GrantRelicOutcome` | Unique per run; passives register into every battle |
| `VisitedLocationIds` | empty | every resolved encounter | Drives `OncePerRun` and `HasVisitedEncounter` |
| `Flags` | empty | `SetFlagOutcome` | Narrative memory of *choices*. Read by `HasFlag`. Run-scoped, never saved |
| `Seed` | random (or set) | — | Fixes the *map* and reward offers. Battle RNG is deliberately unseeded |

**Origin starting values** (`OriginDatabase`, tunable in the inspector):

| Origin | Funds | Credibility | Reads as |
|---|---|---|---|
| Faith Leader | 20 | 40 | Broke but trusted |
| Nepo Baby | 120 | 10 | Buys anything, believed by nobody |
| Celebrity | 60 | 30 | Comfortable until the first scandal |

`MaxHours` of 0 on an origin means "use the run default" (3).

---

## 8. Rewards

Post-battle, on victory → Continue:

| Knob | Where | Default |
|---|---|---|
| Offer count | `PostBattleFlow` (hardcoded) / `RewardConfig.DefaultOfferCount` | **3** |
| Rarity weights | `CardDatabase.GenerateRewardOffer` / `RewardConfig` | Basic **70** / Enhanced **25** / Rare **5** |
| RNG | `RunState.Rng` | Seeded per run |

Rewards are a **pick-1-of-3 card, or skip**. Nothing else drops yet: no Funds, no relics, no
boss reward. `BattleEncounterData.RewardOverride` and `IsBoss` are authored fields that
nothing consumes — wiring `GenerateRewardOffer` to read `RewardConfig` is the open task.

---

## 9. Gaps worth knowing before you author around them

- **`RewardOverride` and `IsBoss` are inert.** Set them for future-proofing, don't expect them to change a run today.
- **Battle rewards are cards only.** An event outcome is currently the only way to hand out Funds, Credibility, or a relic.
- **No per-day weight curve.** An encounter's odds vary only because the *eligible set* varies.
- **The campaign screen is IMGUI.** `CampaignFlow.OnGUI` is a harness for judging the loop, not the shipping map.
- **`campaign-encounters.md` §"Two StS staples that are blocked"** is stale — chosen-card upgrade and removal both ship now (`UpgradeChosenCardOutcome`, `RemoveChosenCardOutcome`), via `RunState.RequestCardChoice`.

## 10. Validation

**Crookedile → Content Audit** catches: events with no options, options with no label, options
that do nothing and say nothing, unpicked `[SerializeReference]` rows, battle encounters with
no session, sessions with >1 round, uncovered days, unreachable windows, weight-0 rows,
dangling dependencies, pools with no boss, and `BoostMultiplier = 0`.

**Crookedile → Encounter Designer** shows the day-window Gantt, per-day coverage/weight, a
seed roller running the real `DrawForDay`, and the dependency graph.
