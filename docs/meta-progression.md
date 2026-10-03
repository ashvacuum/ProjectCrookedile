# Meta-progression: profiles, saves, unlocks, achievements (v0.2)

> **Kind:** Systems · **Status:** Partly built · **Updated:** 2026-10-03
>
> **Summary:** Profiles, the binary save format, run save/continue, unlocks and the save debugging tools. Achievements and Steam are designed, not built.
>
> **Source of truth:** [`Data/Save/`](../Assets/Scripts/Data/Save/), [`Data/Unlocks/`](../Assets/Scripts/Data/Unlocks/) · **Related:** [`metagame-campaign.md`](metagame-campaign.md) · [`nepo-baby-class.md`](nepo-baby-class.md)

Save system and unlocks are built (2026-10-03); achievements, Steam and UI are not. **Locks are off for now**:
`UnlockRules.LocksEnabled` is false, so everything counts as unlocked (`locks true` in the dev console turns them on). Replaces the
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
- **Debugging:** `SaveDebug` + the **Crookedile > Save Debugger** window + dev-console commands. Readable dumps
  (IDs resolved to names, `savedump` writes `save-dump.txt`), named snapshots of every save file (`savesnap`,
  `saveload`, `savesnaps`), counter and grant edits (`savecounter`, `savegrant`, `saverevoke`), wipe, abandon,
  checkpoint now, and deliberate corruption (`savecorrupt run true`) to exercise the backup fallback. Snapshots
  live in `debug-snapshots/` under the save folder; wipe and restore only touch the save system's own files.
- **Tests:** `Tests/EditMode` (Unity Test Runner, Edit Mode). `SaveCoreTests` covers the Unity-free core;
  `SaveSystemTests` covers profiles, save/continue/end and unlocks against the real databases; `SaveDebugTests`
  covers the debug tools.

## 1. Design names → code

| Concept | Code | Status |
|---|---|---|
| Profile progress | `ProfileData` (counters, granted/seen unlocks, unlock-all) | Built |
| Profile list | `ProfileIndex` (`profiles.idx`) | Built |
| Run save | `RunSaveData`, written by `RunState.ToSaveData`, read by `RunState.Restore` | Built |
| Run lifecycle owner | `SaveSystem.StartNewRun` / `ContinueRun` / `Checkpoint` / `EndRun` / `AbandonRun` | Built |
| Serializer | Hand-written binary in `SaveEnvelope` (`SaveBinary` helpers) | Built |
| Unlock answer | `UnlockRules.IsUnlocked(card, profile)`, `UnlockRules.IsAvailableThisRun(card)` | Built |
| Unlock conditions | `CounterAtLeast`, `WonRunAs`, `GrantedByEvent` | Built |
| Counter keys | `ProfileCounters` constants | Built (small set) |
| Debug tools | `SaveDebug`, Save Debugger window, `save*` console commands | Built |
| Stats tracker; achievements as a view over unlocks | — | Not built |
| Steam mirror, Cloud | — | Not built |
| Profile settings, title / run-end / unlock screens | — | Not built |

## 2. The model: three layers

```
Profile (one per player slot, persists forever)
├── ProfileData       unlocked content, lifetime counters (achievement state later)
├── settings          not built (see 9.6)
└── RunSaveData?      the in-progress run, at most one per profile
```

- **Profile** is the unit the player will pick on a title screen. Up to 3 slots; one is created on first use.
- **`ProfileData`** is everything that outlives a run: explicit unlock grants, unlocks already shown, and counters
  that unlock conditions (and later achievements) read.
- **`RunSaveData`** is a snapshot of `RunState`, written at checkpoints and deleted when the run ends. Continuing
  rebuilds `RunState` from it.

One rule holds it together: **the profile is the source of truth. Steam will be a mirror** (section 7).

## 3. Storage and serialization

### Files

```
<persistentDataPath>/
  profiles.idx                 slot list + last-used profile (tiny)
  profiles/<profileId>/
    profile.sav                ProfileData
    profile.sav.bak            previous good copy
    run.sav                    RunSaveData, only while a run is in progress
    run.sav.bak
  debug-snapshots/<name>/      Save Debugger snapshots (dev only)
```

Every write goes to `*.tmp`, is read back and verified, then replaces the real file, keeping the previous one as
`.bak` (`SaveFileStore`). On load, a file that fails its checksum falls back to `.bak`.

### Format: binary save classes in a versioned envelope

- **Save classes are separate from runtime types.** `ProfileData`, `ProfileIndex` and `RunSaveData` hold only
  primitives, strings, lists and content IDs, never `ScriptableObject` references. `RunState` converts to and from
  `RunSaveData`. The file format stays stable while runtime classes change.
- **Envelope:** `magic "CRKS"`, envelope version, kind (index / profile / run), the payload's schema version, length,
  CRC-32, then the payload. A file of the wrong kind, truncated, or failing its CRC is rejected.
- **Serializer:** hand-written `BinaryWriter` payloads, one `Write`/`Read` pair per save class, with the schema
  version passed to `Read` for migrations. Chosen over Odin's binary format because it has no Unity or reflection
  dependency (works under IL2CPP unchanged, and the core is testable outside Unity) and the save classes are few.
  **Adding a field:** write it at the end, bump `SchemaVersion`, and read it only when the version says it's there.
- **No encryption.** A key hardcoded in the binary only stops honest players editing their own single-player save.
  The CRC catches corruption, which is the real risk.

### Content IDs

Saves store content by ID and resolve it through `SaveContent`: cards, enemies and allies from the databases in
`Resources/Databases`, encounters from the Game Encounter Pool plus every encounter its events chain to. A missing
ID on load (a cut card) is dropped with a warning, never a crash. Every content type's ID is its asset GUID,
`CardData` included.

## 4. Unlocks

Builds on the shape first proposed in `needs-detailing.md` section 9: **counting and answering are separate**,
and **the unlock condition lives on the content asset**, with no separate unlock database.

### The gate lives on the content

`CardData` carries an `[SerializeReference] UnlockCondition` beside `_isUnlockable` (shown only when the card is
unlockable). `AllyData` and `EncounterData` get the same pair when they need it. The set is deliberately small:

| Condition | Meaning | Status |
|---|---|---|
| `CounterAtLeast(key, n)` | A lifetime counter reached n ("win 3 runs", later "burn 100 cards") | Built |
| `WonRunAs(origin)` | Won a run as that origin | Built — Trust Fund uses it (Nepo Baby) |
| `GrantedByEvent` | Unlocked only by an explicit grant from a campaign event (below) | Built |
| Chained unlock | Unlocked once another piece of content is | Not built |

An unlockable card with **no** condition unlocks only through a grant. A grant unlocks any card, whatever its
condition.

**Answering "is this unlocked?" is a pure static function:** `UnlockRules.IsUnlocked(card, profile)`. No singleton
and no lifetime, so reward pools, the save system and editor tools all ask the same thing. (A Content Hub audit of
unreachable unlocks is not built yet.)

Conditions only read counters, which only grow, so once something is unlocked it stays unlocked.

### Beyond cards: campaigns, events and allies *(not built)*

The design is in [`unlocks.md`](unlocks.md). The code reuses what cards have, with nothing new in the profile:

- `AllyData`, `EncounterData` and `EncounterPoolData` get the same `_isUnlockable` + `UnlockCondition` pair, and
  `UnlockRules` answers for any of them. IDs are asset GUIDs and grants are ID strings already.
- Filters: the daily draw skips locked encounters; a recruit option for a locked ally shows disabled with how to unlock
  it; the run-start screen lists locked campaigns. A `GoToEncounterOutcome` chain still runs a locked encounter.
- `UnlockContentOutcome` and `HasUnlocked` take any of the four kinds. New condition: won a given campaign.
- `RunSaveData` records the run's campaign (schema bump), so Continue loads the right pool.
- The Encounter Designer's coverage strip and seed roller get an "as unlocked" toggle.

### Explicit grants from the campaign

`UnlockContentOutcome : RunOutcome` writes a card's ID into `ProfileData.GrantedUnlocks` ("the Fixer remembers
you"). `HasUnlocked : RunRequirement` lets event options and pool entries appear only in runs that started with
that card unlocked.

### When it takes effect: the next run

`SaveSystem.StartNewRun` copies the profile's unlocked set into `RunState.UnlockedContent`, and every acquisition site
(`CardDatabase.IsAcquirable`, `GenerateRewardOffer`, `HasUnlocked`) reads only that snapshot. Nothing re-checks
mid-run, and a seeded run stays reproducible. Test runs made with `RunState.Create` directly have an empty snapshot,
so locked cards never appear in them.

### Reveal

`SaveSystem.EndRun` returns the cards that run unlocked (unlocked now minus unlocked before). `GetUnlocks()` lists
every unlockable card with unlocked/seen/how-to-unlock, and `MarkUnlocksSeen` records a reveal so it happens once.
The reveal screen itself is not built.

### Dev

Console: `unlockall`, `lockall`, `unlocks`, plus the `save*` commands (`savegrant`, `saverevoke`, `savecounter`,
`savewipe`, …). The Save Debugger window has the same controls.

## 5. Achievements

**Achievements are unlocks** (decided 2026-10-03, `unlocks.md` rule 7). There is no separate achievement asset or
database: an achievement is an unlock condition on a piece of content, shown with that content's name, and the Steam
achievements mirror them. Not built beyond what cards already have.

### Counters are the design surface

Unlocks never subscribe to battle internals. One `StatsTracker` (a `Singleton<T>`, disposed the way
`PassiveResolver` unsubscribes) will listen on the `EventBus` and bump named counters in `ProfileData`. Today the
counters are bumped by `SaveSystem` at run start and end and by `RunState.RecordBattleVictory`.

- **Keys are `const string`s** (`ProfileCounters`), offered to conditions through an Odin `[ValueDropdown]`. Designers pick
  counters but don't invent them (code increments them), and strings stay stable in saves and assets where enum
  ordinals would shift.
- **Run-scoped counters** ("burn 30 cards in one run") live on the run and are folded into best-ever values at run end.
- Built so far: runs started, won, lost, won per origin, battles won, highest day. Still to add with the tracker:
  elites and bosses beaten, cards burned, replays, cards pulled, enemies converted, Glamour peak, Debt settled.

**Checked at checkpoints:** end of battle, end of run, and when a counter changes (a compare, so cheap).

## 6. Campaign and run lifecycle

**Built:** `SaveSystem` owns a run's lifetime.

- `StartNewRun(origin, seed)`: the origin's starter deck, the profile's unlock snapshot, `runs_started`, first save.
- `ContinueRun(pool, out openEvent)`: rebuilds `RunState` from `run.sav` (falling back to `.bak`).
- `Checkpoint(openEventId)`: writes `run.sav` for campaign runs only. `CampaignFlow` calls it on every map redraw
  and before each battle; an event that's open but unanswered is saved so a resume reopens it.
- `EndRun(victory)`: folds the run's counters into the profile, updates won/lost/per-origin/highest-day, saves the
  profile, deletes `run.sav`, clears `RunState`, and returns the cards that became unlocked. Called when the last day
  is survived (victory) and when a campaign battle is lost.
- `AbandonRun()`: drops the run without counting it.

`CampaignFlow` continues the profile's saved run on entry, or starts a new one with its inspector origin and seed.

**Resume granularity: the campaign map.** Quitting mid-battle resumes into that battle, restarted from its first
round (Slay the Spire's rule). No battle state is ever saved.

**Deterministic resume:** the run RNG is `RunRng`, a xorshift128 generator behind the `System.Random` API, whose
four-`uint` state is saved. Same seed and same choices give the same run, across a save and reload.

**What a `RunSaveData` holds:** origin, seed, RNG state, deck (card ID + upgraded), allies, funds, credibility, time
(minutes remaining and elapsed, max hours), day, district (by asset name), visited locations, flags, today's
locations, pending battle, next encounter, open event, next-battle Hostility, battle queue and round, the run's
unlock snapshot and run counters.

**Not saved:** a "pick a card" prompt open when the player quits is lost on resume. **Not built:** a
`RunEndedEvent`, achievement evaluation at run end, and the run-end screen.

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

1. **Foundations — done.** Card IDs → asset GUIDs; save classes, envelope, atomic writes; profiles; old
   `SaveManager`/`SaveData` deleted; first NUnit Edit Mode tests.
2. **Unlocks — done**, except the Content Hub reachability audit. `UnlockCondition` on cards, `UnlockRules`,
   unlock-aware acquisition, `UnlockContentOutcome`, `HasUnlocked`, `unlockall`/`lockall`.
3. **Run lifecycle — done** in `SaveSystem`, except `RunEndedEvent` and the run-end reveal screen.
4. **Unlocks beyond cards — when locks go on** (everything is unlocked for now). Classes, campaigns, events and allies.
5. **Achievements.** `StatsTracker` and more counters, then an achievements view over the unlocks (no separate data).
6. **Run save/resume — done**, except a title screen with "Continue run" (the campaign scene continues
   automatically for now).
7. **Steam — not started.** Achievement mirror and resync, Cloud, real AppID.
8. **Save debugging — done.** `SaveDebug`, the Save Debugger window, `save*` console commands.

## 9. Open decisions (recommended default first)

1. **Serializer.** *Decided: hand-written binary* (see section 3).
2. **Resume granularity.** *Built as: campaign map only; mid-battle quits restart the battle* · full mid-battle saves (needs
   every status, pile and pending choice serialized; large and fragile).
3. **When unlocks apply.** *Built as: next run* · immediately, mid-run.
4. **Profile count.** *Built as: 3 slots* · unlimited.
5. **Unlock sources.** *Counters and campaign events only; achievements are the same thing* · a meta-currency shop
   (not planned).
6. **Settings.** *Global (audio, video, input) with per-profile gameplay options only* · fully per profile.
7. **What else is unlockable?** *Decided 2026-10-03: classes, campaigns, events and allies too.* Design in
   `unlocks.md`; not built yet, only cards are unlockable today.
