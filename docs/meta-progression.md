# Meta-progression: profiles, saves, unlocks, achievements (v0.2)

**Status: save system and unlocks built (2026-10-03); achievements, Steam and UI not built.** Replaces the
"Meta-progression" stub in `needs-detailing.md` section 9. Open decisions are in section 9 with a recommended default.

## 0. Built so far

Code: `Assets/Scripts/Data/Save/`, `Assets/Scripts/Data/Unlocks/`, `RunState.Save.cs`. No UI yet: the campaign
scene continues a saved run or starts a new one on entry, and the dev console lists unlocks (`unlocks`).

- **`SaveSystem`** (static): profiles (up to 3, one created on first use), `StartNewRun`, `ContinueRun`,
  `Checkpoint`, `EndRun`, `AbandonRun`, `GetUnlocks`, `GrantUnlock`, `MarkUnlocksSeen`, `SetUnlockAll`.
  `UseRoot(path)` points it at another folder (tests).
- **Files:** `persistentDataPath/profiles.idx` and `profiles/<id>/profile.sav` + `run.sav`, each with a `.bak`.
- **Format:** hand-written binary payloads (`ProfileData`, `ProfileIndex`, `RunSaveData`) inside the CRC envelope.
- **Run RNG:** `RunRng` (xorshift128 behind the `System.Random` API) so a resumed run continues the same stream.
- **Content IDs:** `CardData._id` is now the asset GUID (all 117 card assets rewritten). `AllyDatabase` moved to
  `Resources/Databases`. Encounters resolve through the Game Encounter Pool plus anything its events chain to.
- **Unlocks:** `UnlockCondition` on `CardData` (`CounterAtLeast`, `WonRunAs`, `GrantedByEvent`), answered by
  `UnlockRules`. Runs snapshot their unlocks at start. Campaign hooks: `UnlockContentOutcome`, `HasUnlocked`.
  Trust Fund unlocks by winning a run as Nepo Baby.
- **Lifecycle:** the campaign map checkpoints on every redraw and before battles. Quitting mid-battle restarts
  that battle. Winning the last day or losing a battle calls `EndRun`, which updates counters and reports unlocks.
- **Tests:** `Tests/EditMode` (Unity Test Runner, Edit Mode). `SaveCoreTests` covers the Unity-free core;
  `SaveSystemTests` covers profiles, save/continue/end and unlocks against the real databases.

## 1. What exists today

| Piece | State |
|---|---|
| `RunState` | In memory only. Lost on quit. Created by `CampaignFlow`, cleared from four places (`PostBattleFlow` ×2, `CampaignFlow`, `BattleTestStarter`). No single "run ended" moment. |
| `SaveManager` + `SaveData` | Referenced by nothing. `SaveData` describes the old design (Heat, Influence, a 45-day campaign). JSON through `JsonUtility`, "encrypted" with an AES key hardcoded in the source. |
| `CardData._isUnlockable` | Read by reward offers since 2026-10-03: a flagged card is never offered. Nothing can unlock it yet. Only Trust Fund is flagged. |
| `CheatsManager.UnlockAllCards` | Publishes `CheatUnlockAllCardsEvent`, which nothing handles. |
| `SteamManager` | Initializes Steamworks.NET (test AppID 480). No achievements or stats calls. |
| Content IDs | `AllyData`, `EnemyData` and `EncounterData` use their asset file GUID. **`CardData` mints a random `Guid.NewGuid()`**, against the project rule. |
| `RunOutcome` / `RunRequirement` | `[SerializeReference]` building blocks for event choices. The natural hooks for unlocks inside the campaign. |

## 2. The model: three layers

```
Profile (one per player slot, persists forever)
├── ProfileProgress   unlocked content, achievement state, lifetime stats
├── ProfileSettings   per-profile preferences (optional, see 9.6)
└── RunSave?          the in-progress run, at most one per profile
```

- **Profile** is the unit the player picks on the title screen ("New profile / Continue"). Up to 3 slots.
- **ProfileProgress** is everything that outlives a run: what's unlocked, which achievements are done, counters
  that achievements read.
- **RunSave** is a snapshot of `RunState`, written at safe points and deleted when the run ends. Resuming rebuilds
  `RunState` from it.

One rule holds it together: **the profile is the source of truth. Steam is a mirror** (section 7).

## 3. Storage and serialization

### Files

```
<persistentDataPath>/
  profiles.idx                 slot list + last-used profile (tiny)
  profiles/<profileId>/
    profile.sav                ProfileProgress (+ settings)
    profile.sav.bak            previous good copy
    run.sav                    RunSave, only while a run is in progress
    run.sav.bak
```

Every write goes to `*.tmp`, is read back and verified, then replaces the real file, keeping the previous one as
`.bak` (the existing `SaveManager.SaveGameWithValidation` already does this; keep that part). On load, a file that
fails its checksum falls back to `.bak`.

### Format: save DTOs + a binary serializer behind an interface

- **Save DTOs are separate from runtime types.** `ProfileProgressDto`, `RunSaveDto`, etc. hold only primitives,
  strings, lists and content IDs, never `ScriptableObject` references. `RunState` converts to and from a DTO. This
  keeps the file format stable while runtime classes change, and is the main thing JSON-of-runtime-objects gets wrong.
- **Envelope:** `magic "CRKS"`, `formatVersion`, `payloadKind`, `payloadLength`, `CRC32`, then the payload.
  The version drives explicit migration steps (`Migrate_1_to_2`, …) run on load.
- **Serializer (built):** hand-written `BinaryWriter` payloads, one `Write`/`Read` pair per save class, with the
  schema version passed to `Read` for migrations. Chosen over Odin's binary format because it has no Unity or
  reflection dependency (works under IL2CPP unchanged, and the core is testable outside Unity) and the save classes
  are few. Adding a field means writing it at the end and bumping the schema version.
- **No encryption.** A key hardcoded in the binary only stops honest players editing their own single-player save.
  The CRC catches corruption, which is the real risk. (Add obfuscation later if a leaderboard ever needs it.)

### Content IDs

Saves store content by ID and resolve it through the databases (`CardDatabase`, `AllyDatabase`, …). A missing ID on
load (a cut card) is dropped with a warning, never a crash. **`CardData._id` moves to the asset GUID** like the other
content types; no player saves exist yet, so this costs nothing now and a lot later.

## 4. Unlocks

Builds on the shape already proposed in `needs-detailing.md` section 9: **counting and answering are separate**,
and **the unlock condition lives on the content asset**, with no separate unlock database.

### The gate lives on the content

`CardData` gets an `[SerializeReference] UnlockCondition` beside `_isUnlockable`. `AllyData` and `EncounterData` get
the same pair when they need it. Conditions follow the `RunRequirement` pattern, and the set is deliberately small:

| Condition | Meaning |
|---|---|
| `StatAtLeast(key, n)` | A lifetime counter reached n. Covers "convert 50 enemies", "reach day 7", "burn 100 cards". |
| `WonRunAs(origin)` | Won a run as that origin (origin isn't a counter). |
| `HasAchievement(achievement)` | That achievement is earned. **This is how achievements unlock content.** |
| `HasUnlocked(content)` | Chains: unlocked once something else is. |
| `GrantedByEvent` | Unlocked only by an explicit grant from a campaign event (below). |

**Answering "is this unlocked?" is a pure static function** over `ProfileProgress`
(`Unlocks.IsUnlocked(content, progress)`). No singleton and no lifetime, so `CardDatabase.GetAcquirable`, the reward
screen *and* the Content Hub (offline, no game running) all call the same thing. The Content Hub can then audit
reachability: a locked card whose condition reads a counter nothing increments is flagged.

Conditions are monotonic (counters only grow), so once something is unlocked it stays unlocked.

### Explicit grants from the campaign

A new `UnlockContentOutcome : RunOutcome` writes the content's ID into `ProfileProgress.GrantedUnlocks` ("the fixer
remembers you"). It's for content flagged `GrantedByEvent`, so an event can be the *only* way to unlock something.
A new `HasUnlocked : RunRequirement` lets event options and map locations appear only once the profile has
unlocked something.

### When it takes effect: the next run

Recommended (9.3), and already the leaning in `needs-detailing.md`. `RunState.Create` resolves the unlocked set
**once** into the run, and every acquisition site reads that snapshot. Nothing re-checks mid-battle, and a seeded
run stays reproducible.

### Reveal

At run end, `newly unlocked = unlocked now − unlocked at run start`. The run-end screen shows them, and
`ProfileProgress.SeenUnlocks` keeps the reveal from repeating.

### Dev

Wire the existing `unlockall` cheat (it sets an all-unlocked override on the profile); add `lockall` and
`resetprofile`.

## 5. Achievements

**Kept separate from unlocks** (per `needs-detailing.md`): an achievement is a named, displayed milestone; an unlock
condition is a gate. Most unlocks need no achievement card, and some achievements unlock nothing. They meet through
`HasAchievement`, and both read the same counters.

### Data

`AchievementData : ScriptableObject` (ID from asset GUID, in an `AchievementDatabase`): title, description, icon,
hidden flag, Steam API name (empty = local only), and one `[SerializeReference] AchievementCondition` from the same
small set as unlocks (`StatAtLeast`, `WonRunAs`, plus `RunStatAtLeast` and `AllOf`/`AnyOf`).

### Counters are the design surface

Achievements and unlocks never subscribe to battle internals. One `StatsTracker` (a `Singleton<T>`, disposed the way
`PassiveResolver` unsubscribes) listens on the `EventBus` and bumps named counters in `ProfileProgress`.

- **Keys are `const string`s on the tracker**, offered to conditions through an Odin `[ValueDropdown]`. Designers pick
  counters but don't invent them (code increments them), and strings stay stable in saves and assets where enum
  ordinals would shift.
- **Run-scoped counters** ("burn 30 cards in one run") live on the run and are folded into best-ever values at run end.
- First set: runs started/won per origin, battles won, elites and bosses beaten, highest day reached, cards burned,
  replays, cards pulled, enemies converted.

**Checked at checkpoints:** end of battle, end of run, and when a counter changes (a compare, so cheap).

### Examples

| Achievement | Condition | Unlocks (via `HasAchievement` on the content) |
|---|---|---|
| Born on Third Base | Win a run as Nepo Baby | **Trust Fund** |
| Old Money | Burn 100 cards (lifetime) | a Burn-lane card |
| Encore! Encore! | Replay 3 cards in one turn | nothing |
| Full Conversion | Convert 50 enemies as Faith Leader | a Faith Leader Rare |

## 6. Campaign and run lifecycle

The missing piece is **one owner for a run's lifetime**. Add `RunLifecycle` (or methods on `RunState`) with:

- `StartRun(origin, seed)`: copies the unlock set, creates `RunState`, writes `run.sav`.
- `Checkpoint()`: writes `run.sav`. Called on the campaign map after every encounter, event choice and day change.
- `EndRun(RunResult)`: publishes `RunEndedEvent { origin, victory, day, runStats }`, folds run stats into the
  profile, evaluates achievements, saves the profile, deletes `run.sav`, then shows the run-end screen with any
  unlocks earned.

The four scattered `RunState.Clear()` calls become `EndRun` (real runs) or stay as plain clears (test harnesses).

**Resume granularity: the campaign map** (recommended, 9.2). Quitting mid-battle resumes at the map with the
battle still pending, so the fight restarts from its start (Slay the Spire's rule). No battle state is ever saved.

**Deterministic resume:** `System.Random` can't be saved, so the run RNG becomes a small serializable PRNG
(xorshift128, four `uint`s of state) stored in the `RunSave`. Same seed, same choices, same run, even across a
save and reload.

**What a `RunSave` holds:** origin, RNG state, deck (card ID + upgraded), allies, funds, credibility, hours/minutes,
day, district, visited locations, flags, today's locations, pending battle/next encounter, next-battle Hostility,
pending card choice, the run's unlock snapshot and run stats.

## 7. Steam

- `IAchievementPlatform` with `SteamAchievementPlatform` (Steamworks.NET `SteamUserStats.SetAchievement` +
  `StoreStats`, gated on `SteamManager.Initialized`) and a no-op version for non-Steam and dev builds.
- **Local first:** an achievement unlocks in the profile, then mirrors to Steam. On startup, anything unlocked locally
  but missing on Steam is pushed again (handles offline play). Steam never unlocks anything locally.
- Optional: mirror a few counters as Steam stats for progress bars.
- **Steam Cloud:** use Auto-Cloud on `profiles/`. Needs the real AppID (`SteamManager` still uses 480).
- **Profiles vs Steam accounts:** Steam achievements belong to the Steam account, so any profile on that account
  earns them. Accepted; it's how most games with profiles behave.

## 8. Build order

Each step is shippable on its own.

1. **Foundations:** card IDs → asset GUIDs; save DTOs, envelope, `ISaveSerializer` (Odin binary), atomic writes;
   `ProfileManager` (create/select/rename/delete, `profiles.idx`); delete `SaveManager`/`SaveData`. First real NUnit
   EditMode tests: round-trip and corruption fallback.
2. **Unlocks:** `UnlockCondition` on content, the static `Unlocks.IsUnlocked`, unlock-aware acquisition,
   `UnlockContentOutcome`, `HasUnlocked`, a Content Hub reachability audit, wire `unlockall`.
3. **Run lifecycle:** `RunLifecycle` with `EndRun`, `RunEndedEvent`, the run-end reveal screen.
4. **Achievements:** `StatsTracker` and counters, `AchievementData` + database, `HasAchievement`, an achievements
   screen. Trust Fund becomes earnable here.
5. **Run save/resume:** serializable RNG, `RunSave` snapshot, checkpoints, "Continue run" on the title screen.
6. **Steam:** achievement mirror and resync, Cloud, real AppID.

## 9. Open decisions (recommended default first)

1. **Serializer.** *Decided: hand-written binary* (see section 3).
2. **Resume granularity.** *Built as: campaign map only; mid-battle quits restart the battle* · full mid-battle saves (needs
   every status, pile and pending choice serialized; large and fragile).
3. **When unlocks apply.** *Built as: next run* · immediately, mid-run.
4. **Profile count.** *3 slots* · unlimited.
5. **Unlock sources.** *Counters, achievements and campaign events only* · add a meta-currency shop later (a
   separate design).
7. **Are encounters unlockable?** *Cards and allies first; encounters later.* Locking encounters makes the pool
   differ per save, so the Encounter Designer's coverage strip and schedule simulation would need an "as unlocked"
   toggle (raised in `needs-detailing.md`).
6. **Settings.** *Global (audio, video, input) with per-profile gameplay options only* · fully per profile.
