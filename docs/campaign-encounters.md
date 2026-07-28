# Campaign Encounters — System Reference

*How encounters are typed, authored, scheduled, and drawn. Written 2026-07-28.*

Companion docs: [`metagame-campaign.md`](metagame-campaign.md) is the canonical design (the
"why"); [`campaign-build-checklist.md`](campaign-build-checklist.md) is the execution tracker
(what's done, what's next). **This doc is the reference for the code that exists.**

> **Status:** the data layer and authoring tools are built. **Nothing calls them at runtime
> yet** — `CampaignFlow` and `campaign.unity` are still unbuilt M1 items. Encounters are
> authorable and inspectable today; they are inert in play.

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

## Events — dialogue and choices

`EventEncounterData` holds body text plus a list of `EventOption`.

```
EventOption          Label (button text)
                     ResultText (shown after the pick)
                     RunOutcome[]  [SerializeReference]
```

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

**Outcomes are signed on purpose.** `RunState.GainFunds` was gain-only and was replaced by
`AdjustFunds`/`AdjustCredibility`. A choice layer where every option is a pure gain has no
decision in it — a choice needs to be able to cost something.

**Not built:** `GainCard`, `StartBattle`, `Nothing`, and `RunRequirement` gating (greying out
options the player can't afford). Each is a small addition against the same base; see the
checklist's M2 section.

---

## Scheduling — `EncounterPoolData`

The set an encounter can be drawn from, with a per-entry day window.

```
EncounterPoolEntry   Encounter
                     FirstDay / LastDay      inclusive; LastDay 0 = open-ended
                     Weight                  -1 = inherit (see below)
                     OncePerRun
```

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

### Seeds

`RunState.Seed` is set at `Create`. Passing `0` (the default, and every existing call site)
picks a random one; any other value replays that exact campaign. This is what makes a run
shareable and a bug report reproducible.

---

## `EncounterDatabase`

`GameDatabase<EncounterData>`, same as `CardDatabase` and `EnemyDatabase`. Auto-populates via
**Refresh Database** in the inspector, keyed by `EncounterData.ID`.

One database across all subtypes — `t:EncounterData` matches derived assets, so battles and
events land in one lookup rather than a database per type.

- `GetOfType<T>()` — every encounter of a concrete type
- `GetUndrawable()` — encounters with weight ≤ 0: authored but can never be drawn. Usually a
  slip rather than intent, and invisible from the pool view.

---

## Editor tool — Encounter Gantt

**Crookedile → Encounter Gantt.** A day-by-day timeline of a pool. Editing stays in the pool
asset's own inspector (Odin `TableList`); this window is the view plus a seed simulator.

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
