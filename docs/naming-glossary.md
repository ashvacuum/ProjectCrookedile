# Crookedile — Naming Glossary (combat → "working a crowd")

*The codebase grew out of an HP/combat prototype, so it was full of fight vocabulary
(`damage`, `Resolve`, `attacker`, `Shield`, `heal`). The game is **"work a crowd, not win a
fight"** — managing the **opinion meter** with **pressure**, **Support/Denial**, and **Voice intents**.*

**Status (verified against the code 2026-09-06):** sections **A, B2 and most of B4 are done** —
those renames shipped, and two of them landed on a *different* name than this doc proposed. **B1,
B3 and C have not moved.** Each section below carries its own status line; the Decision column now
records what actually happened rather than what was hoped for.

**How to use this doc:** read the section status first. Where a row says `DONE`, the Proposed
column is history — the **Landed as** note is the name to use. Where a row is still open, fill the
Decision column before renaming (serialized classes/enums need `[MovedFrom]` + asset handling).

> ⚑ **`[MovedFrom]` aliases are permanent.** Several renamed classes still carry their old names
> as `[UnityEngine.Scripting.APIUpdating.MovedFrom]` strings so existing assets deserialize. A
> grep hit on a "deleted" name is usually one of these. Do not clean them up — the assets break.

Legend for **Kind**: `class` = effect/SO class · `enum` = enum or value · `event` = EventBus struct ·
`method` = method/hook · `field` = field/property · `concept` = general vocabulary.

---

## A. Dead / vestigial — leftover HP-game code

**Status: DONE.** None of these symbols exist any more. The game has no HP.

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Resolve` (the HP noun) | concept | DELETE | **DONE** | The verb survives on purpose: `EffectResolver`, `PassiveResolver`, `ResolveCardEffects`. |
| `BattleResourceType.Resolve` | enum | DELETE | **DONE** | Whole enum gone — zero references. |
| `HealResolveEffect` | class | DELETE | **DONE** | Became `RaiseOpinionEffect`; old name survives only as its `[MovedFrom]` alias. |
| `RestoreResolveEffect` | class | DELETE | **DONE** | Became `RaiseOpinionTrackedEffect`; same alias caveat. |
| `BattleEffectType.ResolveDamage` | enum | DELETE | **DONE** | |
| `BattleEffectType.ResolveHeal` | enum | DELETE | **DONE** | |
| `BattleEffectType.RandomDamage` | enum | DELETE | **DONE** | |
| `BattleEffectType` (whole enum) | enum | DELETE? | **DONE — deleted** | Verified: zero references. The polymorphic `BattleEffect` hierarchy replaced it. |

**Two leftovers this section didn't catch:**

- `ResolveBelowCondition.cs` contains the class `OpinionBelowCondition` — the class was renamed,
  the file wasn't. Rename the file.
- `BattleStats.IsDefeated` still exists and is read in three places, despite there being no HP.
  Decide whether it means "neutralized" now or should go.

---

## B. Live concepts, combat-named

### B1. Damage → *(the meter verb)*

**Status: NOT DONE at the event/hook layer — and the target name changed.** The effect *classes*
were renamed, but they landed on **Opinion**, not the **Pressure** this doc proposed:
`ApplyOpinionEffect`, `ApplyOpinionBasedOnHostility`, `ModifyOutgoingOpinion`,
`ModifyIncomingOpinion`. "Pressure" survives only as the ledger verb, `OpinionLedger.ApplyPressure`.

**So do not follow the Proposed column here** — new code should say **Opinion** for effects and
status hooks, and reserve **Pressure** for what the ledger does to the meter.

| Current | Kind | Proposed (stale) | Decision | Notes |
|---|---|---|---|---|
| `DealDamageEffect` | class | `ApplyPressureEffect` | **DONE — landed as `ApplyOpinionEffect`** | |
| `DealRandomDamageEffect` | class | `ApplyRandomPressureEffect` | **DONE — landed as `ApplyRandomOpinionEffect`** | |
| `ModifyDamageDealt` | method | `ModifyOutgoingPressure` | **DONE — landed as `ModifyOutgoingOpinion`** | 2 files still name the old hook; check they aren't a second path. |
| `ModifyDamageTaken` | method | `ModifyIncomingPressure` | **DONE — landed as `ModifyIncomingOpinion`** | " |
| `DamageDealtEvent` | event | `MeterPressureEvent` | **OPEN — 11 files** | The biggest remaining hit. Also names `DamageDealtTrigger` and `DamageMinimumCondition`, both authored on assets, so this needs `[MovedFrom]`. |
| `GetDamagePreview` / `DamagePreview` | method/struct | `GetPressurePreview` / `PressurePreview` | **OPEN — 6 files** | Pure code, no assets. Cheap. |
| `LastDamageDealt` (context value) | enum | `LastPressureApplied` | **OPEN — 5 files** | `EffectContextValue`; ordinals are serialized — rename the name, never reorder. |
| `PreviewDamageDealt/Taken` | method | `PreviewOutgoing/IncomingPressure` | **OPEN** | Should follow whatever `GetDamagePreview` decides. |
| `baseDamage` / `finalDamage` | field | `basePressure` / `finalPressure` | **OPEN** | Locals — low cost. |

⚑ Pick the target word before touching any of these: `MeterOpinionEvent` and `LastOpinionApplied`
are the consistent choices now, not the Pressure names above.

### B2. Shield → the directional buffers

**Status: DONE — and the answer was neither `Shield` nor `Buffer`.** The umbrella term was
dropped entirely; everything says **Support** (player) or **Denial** (enemy) outright. Zero
`Shield` symbols survive.

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| "Shield" (umbrella term) | concept | `Buffer`? | **DONE — umbrella dropped** | Always say Support/Denial. |
| `GainShieldEffect` | class | `GainBufferEffect`? | **DONE — `GainSupportEffect`** | |
| `LoseShieldEffect` | class | `LoseBufferEffect`? | **DONE — `LoseSupportEffect`** | |
| `ConsumeAllShieldEffect` | class | `ConsumeAllBufferEffect`? | **DONE — `ConsumeAllSupportEffect`** | |
| `RaiseOpinionEqualToShieldEffect` | class | `RaiseOpinionEqualToBufferEffect`? | **DONE — `RaiseOpinionEqualToSupportEffect`** | |
| `ShieldEqualToHostilityEffect` | class | `BufferEqualToHostilityEffect`? | **DONE — `SupportEqualToHostilityEffect`** | |
| `BattleResourceType.Shield` | enum | `Buffer`? | **DONE — enum deleted** | See §A. |
| `EffectContextValue.LastSupportGained/Lost`, `CurrentSupport` | enum | KEEP | **KEEP** | Already correct. |

### B3. Attack / attacker → Voice / speaker

**Status: NOT DONE.** All still present.

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `attacker` / `attackerStats` | field | `source` / `speaker` | **OPEN — 1 file** | `StatusEffectManager.ReflectDamage`-style params. |
| `attackerName` | field | `sourceName` / `speakerName` | **OPEN — 8 files** | Carried on events, so the widest reach of the three. |
| `isAttackerPlayer` | field | `isSourcePlayer` | **OPEN — 2 files** | |
| `Attack` (verb/comments) | concept | `push` / `pressure` | **OPEN** | Note `EnemyMoveType.Attack` is section C's problem, not this one. |

Cheapest section left: no assets serialize these names, so it's a pure rename.

### B4. Heal → Raise / Rally opinion

**Status: MOSTLY DONE.** One context value left.

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Heal` (raise opinion) | concept | `Raise` / `Rally` | **DONE — `Raise`** | `RaiseOpinionEffect`, `RaiseOpinionTrackedEffect`, `OpinionLedger.RaiseDirect`. |
| `ResolveHealedTrigger` | class | `OpinionRaisedTrigger`? | **DONE — `OpinionRaisedTrigger`** | |
| `LastHealAmount` (context value) | enum | `LastOpinionRaised` | **OPEN — 7 files** | The last "heal" in the codebase. |

---

## C. Enemy intents — code categories vs. design vocabulary

**Status: NOT DONE, and blocked on a design call.** `EnemyMoveType` is unchanged across 8 files.
This is the same decision tracked as `needs-detailing.md` §4 ("Intent vocabulary") — resolve it
there, not here, then record the mapping in this table.

| Current `EnemyMoveType` | Doc intent (approx) | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Attack` | Condemn | | | Pushes the meter down. |
| `DefendOpinion` | Rebuke | | | Gains Denial. |
| `RileOthers` | Rally | | | Boosts neighbours' hostility. |
| `Buff` / `OffensiveBuff` / `DebuffAttack` / `Debuff` | — | | | Mechanical combos; may stay internal. |
| `Idle` | Murmur | | | Low impact / presence. |
| `SummonMinion` | — | | | Enemy-only summon; keep. |
| (no equivalent) | Sway | | | "convert a receptive enemy to hostile". |

---

## D. Already correct (the vocabulary to converge ON)

These are the right words — new code should match them:

- **Opinion meter** (`CurrentOpinion`, `OpinionChangedEvent`, `RaiseOpinion`)
- **Opinion, applied** (`ApplyOpinionEffect`, `ModifyOutgoing/IncomingOpinion`) — the *effect and
  status-hook* verb. **Pressure** is now narrower than this doc originally assumed: it means
  specifically what the ledger does, `OpinionLedger.ApplyPressure`.
- **Support / Denial** (`CurrentSupport`, `CurrentDenial`, `SupportChangedEvent`, `DenialChangedEvent`)
- **Hostility** (signed axis; receptive ↔ hostile)
- **Voice / Intent** (`EnemyMoveData`, intent display)
- **Attention** (archetype resource). Patronage is retired with the Nepo Baby redesign (`nepo-baby-class.md`).
- **Pacify / convert / Jaded** (Faith Leader)
- **Ally** (the persistent per-run passive holder — `AllyData`, `RunState.Allies`). The design
  docs still call this a **relic**; the code never did. Ally is the name.

---

## E. Status names — judgment call (default: KEEP)

**Status: settled as KEEP.** The StS-derived status names (`Weakened`, `Vulnerable`, `Frail`,
`Plated`, `Thorns`, `Intangible`, `Exposed`, `Rattled`…) are flavor-neutral, and all still exist
under these names. Renaming is pure churn unless you want political reskins.

| Current | Reskin idea (optional) | Decision |
|---|---|---|
| `Weakened` | | KEEP |
| `Vulnerable` | | KEEP |
| `Frail` | | KEEP |
| `Plated` | | KEEP |
| `Thorns` | | KEEP |
| `Intangible` | | KEEP |
| `Rattled` | | KEEP |
| `Smear` | (already political) | KEEP |
