# Crookedile

Unity deck-builder: a political-campaign roguelike. Battles are card fights over an Opinion
Meter; between them a seven-day campaign map offers encounters that cost Hours.

`docs/` is canonical for design — `core-design.md`, `metagame-campaign.md`,
`campaign-encounters.md`, `enemy-design-bible.md`. When code and docs disagree about intent, the
docs win; say so rather than quietly following the code.

## Environment

- **Unity 6000.3.7f1.** **Input System only** (`activeInputHandler: 1`) — `Input.GetKeyDown` and
  friends throw at runtime. Use `Keyboard.current` / `Mouse.current`. This has silently killed
  working features twice.
- **Odin (Sirenix) is available everywhere**, auto-referenced without an asmdef entry. Reach for
  attributes before writing a custom editor.
- Assemblies: `Core` → `Utilities` → `Runtime` → `UI`, plus `Editor` and `Tests`. Runtime code
  cannot see `UI`; editor-only APIs need `#if UNITY_EDITOR` when they live in a runtime class.

## Patterns that carry the codebase

- **`[SerializeReference]` polymorphic content.** `BattleEffect`, `RunOutcome`, `RunRequirement`,
  `BattlePassive`, `StatusBehavior`. To add one: a `[Serializable]` subclass with its own fields
  and description override. No registry, no factory, no other file changes.
- **`GameLogger`, not `Debug.Log`.** `GameLogger.LogInfo<T>(...)` takes its category from the
  class's `[Debuggable("Category")]` attribute, which is inherited — mark a base class once.
- **Asset IDs come from the asset's file GUID** (`EncounterData`, `EnemyData`, `AllyData`).
  Never mint `Guid.NewGuid()` for a new id field: duplicating an asset copies the value and two
  assets answer to one id.
- **`EventBus`** for cross-system notifications; `Singleton<T>` for managers.
- **Saves go through `SaveSystem`** (`Data/Save`): binary save classes with a schema version, content stored by
  asset-GUID ID. A new `RunState` field that must survive a reload also goes in `RunSaveData` and
  `RunState.Save.cs`. See `docs/meta-progression.md`.
- Databases (`GameDatabase<T>`) self-refresh on asset import via `DatabaseAutoRefresh`.

## Conventions

- Private serialized fields `_camelCase` with `[SerializeField]`; expose via read-only properties.
- `[Tooltip]` on anything a designer edits — these assets are authored, not just read.
- Formatting is CSharpier-style; match the file you're in rather than reformatting it.
- Comments state the invariant that holds now. Explaining what a change fixed belongs in the
  commit message.
- Mark deliberate simplifications with a `ponytail:` comment naming the ceiling and the upgrade
  path.

## Docs

- Every doc in `docs/` opens with the standard header (Kind, Status, Updated, Summary, Source of truth,
  Related — format in `tools/check_docs.py`). The readme's documentation map is generated from those headers.
- Run `python3 tools/check_docs.py --fix` after adding or editing a doc, adding or renaming a card, or pulling
  the card-art workbook. It regenerates the map, adds workbook rows for new cards, and fails on dead links,
  unlisted files in `docs/`, or workbook rows naming cards that no longer exist. Bump a doc's Updated date
  when you change it.
- The card assets own the workbook's Card, Rarity and Type columns; the Google Sheet owns Mechanic and Subject.

## Authoring tools (menu: Crookedile)

- **Content Hub** — audits all content for completeness. Check here before assuming data is fine.
- **Encounter Designer** — day-window timeline, dependency graph, multi-seed schedule simulation,
  and CSV import from `docs/campaign-ideation.xlsx`.
- **Authoring Catalog** — every `[SerializeReference]` building block with its fields, from
  reflection.
- **Save Debugger** — profiles, counters, unlocks (toggle grants), the run save as readable text,
  named snapshots of the whole save folder, and corruption tests for the backup fallback. The dev
  console has the same tools as `save*` commands (`saveinfo`, `savesnap`, `saveload`, `savewipe`, …).
- Backquote (`` ` ``) opens the in-game dev console: `[CheatCommand]` methods plus log control
  (`logs`, `log <category> <level>`, `filter`). Cheats need the `CHEATS_ENABLED` define
  (Ctrl+Shift+C).

## Known soft spots

- Tests are thin. `Tests/EditMode` (NUnit, Unity Test Runner > Edit Mode) covers the save system;
  `Tests/PlayMode/CardSmokeTests` (Play Mode) plays every card against a receptive, neutral and
  hostile enemy and fails on loops, errors or a stuck battle — a smoke test, not a check of card text;
  `Tests/EffectResolverTest.cs` is a manual MonoBehaviour harness, not NUnit.
