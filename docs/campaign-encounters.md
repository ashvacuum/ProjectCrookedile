# Campaign Encounters — System Reference

*How encounters are typed, authored, scheduled, and drawn. Written 2026-07-28.*

Companion docs: [`metagame-campaign.md`](metagame-campaign.md) is the canonical design (the
"why"); [`campaign-build-checklist.md`](campaign-build-checklist.md) is the execution tracker
(what's done, what's next). **This doc is the reference for the code that exists.**

> **Status:** playable end to end. `CampaignFlow` drives `campaign.unity`, draws each day from
> a pool, dispatches battles and events, and honours chaining. Its view is IMGUI on purpose —
> see [`campaign-build-checklist.md`](campaign-build-checklist.md) M1.

---

## The type tree

```
EncounterData (abstract SO)          ID, DisplayName, Blurb, HourCost, DropWeight
├── BattleEncounterData              wraps a BattleSession, IsBoss, RewardOverride
└── EventEncounterData               Body text + EventOption[]
```

`EncounterData.ID` is an auto-generated GUID (same `OnValidate`/`Reset` idiom as `EnemyData`).
Everything keys off it — `RunState.VisitedLocationIds`, `EncounterDatabase`, pool exclusions —
so renaming or moving an asset never resets its run state.

### Why ScriptableObject and not `[SerializeReference]`

The rule this codebase follows, consistently:

- **ScriptableObject = the noun you reference, name, and count.** `CardData`, `EnemyData`,
  `RelicData`, `EncounterData`.
- **`[SerializeReference]` = the polymorphic verb living inside it.** `BattleEffect` in
  `CardData`, `BattlePassive` in `RelicData`, `RunOutcome` in `EventOption`.

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

## Chaining — encounters are not all battles

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

## Events — dialogue and choices

`EventEncounterData` holds body text plus a list of `EventOption`.

```
EventOption          Label (button text)
                     ResultText (shown after the pick)
                     RunRequirement[]  [SerializeReference]   gates the option
                     RunOutcome[]      [SerializeReference]   what it does
```

**Option gating** is how "you need the Bishop's Ring for this" works: put a `HasRelic` (or
`FundsAtLeast`, or anything else) in the option's `Requirements`. Unmet options render
**disabled with the reason attached**, never hidden — seeing the door you can't open is most of
what makes a gate interesting, and a hidden option just makes the event look shorter.
`CampaignFlow` re-checks availability in `ChooseOption` as well as in the view, because an
outcome applied from a locked option is silent and unrecoverable.

`RunOutcome` is the polymorphic base — abstract `Apply(RunState)` and `GetDescription()`,
mirroring the `BattleEffect` pattern one layer up (Odin type-picker dropdown, `[InfoBox]` live
description, `EditorSafeDescription` so one bad description can't break the whole asset's
inspector).

**Adding an outcome type:** create a `[Serializable]` class inheriting `RunOutcome`, implement
the two methods. No other file changes.

### Shipped outcomes

| Outcome | Effect |
|---|---|
| `AdjustFundsOutcome` | Signed change to `RunState.Funds`, clamped at 0 |
| `AdjustCredibilityOutcome` | Signed change to `RunState.Credibility`, clamped at 0 |
| `GrantRelicOutcome` | `RunState.AddRelic` — duplicates ignored, StS-style |
| `GainCardOutcome` | Adds N copies of a specific card. How an event hands you a curse |
| `GainRandomCardOutcome` | Draws from `CardDatabase` — one rarity, or the standard reward weights |
| `RemoveCardOutcome` | Removes one copy of a specific card. For cleansing events |
| `GoToEncounterOutcome` | Sets `RunState.NextEncounter` — this choice leads into another encounter |

The card outcomes resolve the database through `CardDatabaseLookup` (`Resources/Databases/CardDatabase`)
rather than an inspector field, because outcomes are plain serialized classes with nowhere to
hang an asset reference.

**Outcomes are signed on purpose.** `RunState.GainFunds` was gain-only and was replaced by
`AdjustFunds`/`AdjustCredibility`. A choice layer where every option is a pure gain has no
decision in it — a choice needs to be able to cost something.

**Not built:** `Nothing` (flavour-only option). The planned `StartBattle` outcome was
generalised into `GoToEncounterOutcome` — it points at any encounter, not just a battle, which
is the whole point.

### Two StS staples that are blocked, and why

**Upgrade a card.** `CardData` carries its upgrade in-place — an `_isUpgraded` bool plus
`_upgradedCosts`/`_upgradedEffects`/`_upgradedPassives` on the same asset. `RunState.Deck` holds
shared SO references, so flipping that flag would upgrade the card for every run and every
class at once, permanently, in the project asset. A deck-level upgrade needs per-run card
*instances* first. That's a real architectural change, not an outcome class.

**Remove a card of the player's choice.** Needs a deck-view picker outside battle. The battle
system has `CardChoiceRequestedEvent` and a `CardChoicePanel`, but they're wired to battle
context; reusing them on the map is unproven and the event panel itself doesn't exist yet.
`RemoveCardOutcome` handles the specific-card case ("lose a Doubt"), which covers cleansing
events without any UI.

---

## Scheduling — `EncounterPoolData`

The set an encounter can be drawn from, with a per-entry day window.

```
EncounterPoolEntry   Encounter
                     FirstDay / LastDay      inclusive; LastDay 0 = open-ended
                     Weight                  -1 = inherit (see below)
                     OncePerRun
                     Guaranteed              always appears in its window
```

**`Guaranteed` is how fixed structure is built.** A weight only makes an encounter *likely*,
and "likely" isn't something you can design a run around. Guaranteed entries are added to the
day's offering before the random picks and ignore the per-day count, so a day-7 boss can't be
crowded out by a full slate of events. Set `FirstDay = LastDay = 7` and tick it. Same mechanism
for a fixed day-1 opener. The Gantt shows these as `ALWAYS`.

### Dependencies — one encounter unlocking or favouring another

Two mechanics, both built on `RunRequirement`, a `[SerializeReference]` condition tested against
the run. Same authoring shape as `RunOutcome`: type-picker dropdown, live `[InfoBox]` description.

| Field on the pool entry | Effect |
|---|---|
| `Requirements` | **Hard gate.** All must hold or the encounter can't appear at all |
| `BoostIf` + `BoostMultiplier` | **Soft nudge.** When all hold, weight is multiplied. Stays available either way |

Shipped conditions: `HasVisitedEncounter`, `FundsAtLeast`, `CredibilityAtLeast`, `HasRelic`,
`DayAtLeast`. Every one has a `Negate` toggle on the base, so "hasn't visited X" — mutually
exclusive branches — costs no extra type.

`HasVisitedEncounter` keys off `RunState.VisitedLocationIds`, which is why encounters needed a
stable GUID id.

**A null `RunState` means "requirements pass."** Edit-time tooling has no run to test against,
and a preview that showed an empty board would be useless. So `DrawForDay`, `EligibleOn`, and
`IsEligibleOn` all take an optional state: `CampaignFlow` passes the live run, the designer
window passes null.

### Drop chance resolves in two levels

1. **`EncounterData.DropWeight`** — the encounter's own chance, defaulting to 1. This is the
   "default if left empty".
2. **`EncounterPoolEntry.Weight`** — a per-pool override. Defaults to `-1`, meaning inherit.
   Set a number only when the encounter should be rarer or commoner *in this pool* than it is
   by default. `0` disables the row without deleting it.

`EncounterPoolEntry.ResolvedWeight` is the single resolution point — draws, eligibility checks,
and the Gantt view all read it, so the fallback can't drift between them.

### Drawing

```csharp
List<EncounterData> today = pool.DrawForDay(day, count, seed, runState.VisitedLocationIds);
```

Deterministic from `(seed, day)`. One `System.Random` per day, so day 5 is reproducible without
day 4 having run first. Uses its own RNG rather than `RandomHelper`/`UnityEngine.Random`
deliberately: **seeding the campaign must never perturb battle RNG, or vice versa.**

Returns fewer than requested — possibly none — when the eligible set is too small. The caller
decides whether that's a content bug or an acceptable quiet day.

### Seeds — what they do and don't cover

`RunState.Seed` is set at `Create`. Passing `0` (the default, and every existing call site)
picks a random one; any other value replays that exact campaign.

Two streams derive from it, and one thing deliberately doesn't:

| Stream | Covers | Source |
|---|---|---|
| Encounter draws | Which locations appear on which day | `EncounterPoolData.DrawForDay`, own `System.Random` per (seed, day) |
| `RunState.Rng` | Card reward offers, `GainRandomCardOutcome` | One `System.Random` per run, offset from the seed |
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

## Where a run starts

Starting Funds, Credibility, and day length live on **`OriginDatabase.Entry`**, per archetype —
the same asset that already owns each origin's portrait, passive, starter tag, and Max AP.
`RunState.Create` reads them via `OriginDatabase.Shared`, so no run-creation path can start a
player at zero by forgetting to pass them along.

| Origin | Funds | Credibility | Reads as |
|---|---|---|---|
| Faith Leader | 20 | 40 | Broke but trusted — has to earn its way through events |
| Nepo Baby | 120 | 10 | Can buy any option on the board, nobody believes a word |
| Celebrity | 60 | 30 | Comfortable and well-liked, until the first scandal |

Those are seeded defaults from the Origin Database generator, not balance — tune them in the
inspector. `MaxHours` of `0` means "use the run's default" (3), so day length only differs per
origin if you deliberately set it.

This is a real design lever rather than flavour: starting Funds decides which event options are
even *reachable* on day one, which is the cheapest way to make two archetypes feel different in
the campaign layer without any new systems.

> **Naming trap:** `core-design.md` §140 records a **battle** resource called "Credibility"
> that was cut as over-engineered (Overload / exposure cliff / fabricated tags). The campaign
> meta stat here is unrelated — battle never reads it.

## Content Audit coverage

**Crookedile → Content Audit** has two campaign categories:

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

## `EncounterDatabase`

`GameDatabase<EncounterData>`, same as `CardDatabase` and `EnemyDatabase`. Auto-populates via
**Refresh Database** in the inspector, keyed by `EncounterData.ID`.

One database across all subtypes — `t:EncounterData` matches derived assets, so battles and
events land in one lookup rather than a database per type.

- `GetOfType<T>()` — every encounter of a concrete type
- `GetUndrawable()` — encounters with weight ≤ 0: authored but can never be drawn. Usually a
  slip rather than intent, and invisible from the pool view.

---

## Editor tool — Encounter Designer

**Crookedile → Encounter Designer.** Two tabs over one pool. Editing stays in the pool asset's
own inspector (Odin `TableList` + type-pickers); both tabs are read-only views.

**Timeline** — the Gantt, plus a seed simulator.

- A bar per entry spanning its day window, coloured by encounter type. `w2` = weight overridden
  on that row, `w2*` = inherited from the encounter's `DropWeight`. Click a row label to ping
  the asset.
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

## Known shortcuts

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
