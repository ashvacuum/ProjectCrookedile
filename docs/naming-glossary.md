# Crookedile — Naming Glossary (combat → "working a crowd")

*The codebase grew out of an HP/combat prototype, so it's full of fight vocabulary
(`damage`, `Resolve`, `attacker`, `Shield`, `heal`). The game is **"work a crowd, not win a
fight"** — managing the **opinion meter** with **pressure**, **Support/Denial**, and **Voice intents**.*

**How to use this doc:** fill the **Decision** column with the final name (or `KEEP` / `DELETE`).
Once decisions are locked, we rename per this glossary (serialized classes/enums need `[MovedFrom]`
+ asset handling). New code should already follow the **Proposed** column.

Legend for **Kind**: `class` = effect/SO class · `enum` = enum or value · `event` = EventBus struct ·
`method` = method/hook · `field` = field/property · `concept` = general vocabulary.

---

## A. Dead / vestigial — leftover HP-game code (recommend DELETE)

The game has **no HP** (`BattleStats.IsDefeated` is hardwired false). These are pure leftovers.

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Resolve` (the HP noun) | concept | DELETE | | Not the verb `Resolve` in `EffectResolver`/`ResolveCardEffects` — those stay. |
| `BattleResourceType.Resolve` | enum | DELETE | | No HP resource. |
| `HealResolveEffect` | class | DELETE | | Healed HP. |
| `RestoreResolveEffect` | class | DELETE | | Restored HP. |
| `BattleEffectType.ResolveDamage` | enum | DELETE | | Legacy enum (polymorphic BattleEffect replaced it). |
| `BattleEffectType.ResolveHeal` | enum | DELETE | | " |
| `BattleEffectType.RandomDamage` | enum | DELETE | | " |
| `BattleEffectType` (whole enum) | enum | DELETE? | | Verify nothing still reads it; the new system is polymorphic classes. |

---

## B. Live concepts, combat-named — rename to the opinion vocabulary

### B1. Damage → Pressure (the core verb: pushing the meter)

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `DealDamageEffect` | class | `ApplyPressureEffect` | | `OpinionLedger.ApplyPressure` already uses "pressure". |
| `DealRandomDamageEffect` | class | `ApplyRandomPressureEffect` | | |
| `DamageDealtEvent` | event | `MeterPressureEvent` | | Notification only. |
| `ModifyDamageDealt` | method | `ModifyOutgoingPressure` | | Status hooks already use this name. |
| `ModifyDamageTaken` | method | `ModifyIncomingPressure` | | " |
| `PreviewDamageDealt/Taken` | method | `PreviewOutgoing/IncomingPressure` | | |
| `GetDamagePreview` / `DamagePreview` | method/struct | `GetPressurePreview` / `PressurePreview` | | |
| `LastDamageDealt` (context value) | enum | `LastPressureApplied` | | `EffectContextValue`. |
| `baseDamage` / `finalDamage` | field | `basePressure` / `finalPressure` | | Locals — low cost. |

### B2. Shield → the directional buffers (Support / Denial)

The two shields are already **Support** (player, absorbs drops) and **Denial** (enemy, absorbs rises).
Only the generic word "Shield" in class/method names is stale. Pick a neutral umbrella term:

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| "Shield" (umbrella term) | concept | `Buffer`? | | Or just always say Support/Denial and drop the umbrella. |
| `GainShieldEffect` | class | `GainBufferEffect`? | | Routes to Support (player) / Denial (enemy) by caster. |
| `LoseShieldEffect` | class | `LoseBufferEffect`? | | |
| `ConsumeAllShieldEffect` | class | `ConsumeAllBufferEffect`? | | |
| `RaiseOpinionEqualToShieldEffect` | class | `RaiseOpinionEqualToBufferEffect`? | | |
| `ShieldEqualToHostilityEffect` | class | `BufferEqualToHostilityEffect`? | | |
| `BattleResourceType.Shield` | enum | `Buffer`? | | |
| `EffectContextValue.LastSupportGained/Lost`, `CurrentSupport` | enum | KEEP | | Already Support — fine. |

### B3. Attack / attacker → Voice / speaker

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `attacker` / `attackerStats` | field | `source` / `speaker` | | Whoever applied the pressure. |
| `attackerName` | field | `sourceName` / `speakerName` | | Used in events. |
| `isAttackerPlayer` | field | `isSourcePlayer` | | |
| `Attack` (verb/comments) | concept | `push` / `pressure` | | |

### B4. Heal → Raise / Rally opinion

| Current | Kind | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Heal` (raise opinion) | concept | `Raise` / `Rally` | | `RaiseOpinion` already exists. |
| `LastHealAmount` (context value) | enum | `LastOpinionRaised` | | |
| `ResolveHealedTrigger` | class | `OpinionRaisedTrigger`? | | Passive trigger. |

---

## C. Enemy intents — code categories vs. design vocabulary

The doc (§5) names voice intents **Rally / Rebuke / Sway / Condemn / Murmur**; the code uses mechanical
categories. Decide whether to unify (and how they map).

| Current `EnemyMoveType` | Doc intent (approx) | Proposed | Decision | Notes |
|---|---|---|---|---|
| `Attack` | Condemn | | | Pushes the meter down. |
| `DefendOpinion` | Rebuke | | | Gains Denial. |
| `RileOthers` | Rally | | | Boosts neighbours' hostility. |
| `Buff` / `OffensiveBuff` / `DebuffAttack` / `Debuff` | — | | | Mechanical combos; may stay internal. |
| `Idle` | Murmur | | | Low impact / presence. |
| `SummonMinion` | — | | | Nepo-style; keep. |
| (no equivalent) | Sway | | | "convert a receptive enemy to hostile". |

---

## D. Already correct (the vocabulary to converge ON)

These are the right words — new code should match them:

- **Opinion meter** (`CurrentOpinion`, `OpinionChangedEvent`, `RaiseOpinion`)
- **Pressure** (`ApplyPressure`, `ModifyOutgoing/IncomingPressure`)
- **Support / Denial** (`CurrentSupport`, `CurrentDenial`, `SupportChangedEvent`, `DenialChangedEvent`)
- **Hostility** (signed axis; receptive ↔ hostile)
- **Voice / Intent** (`EnemyMoveData`, intent display)
- **Patronage / Attention** (archetype resources)
- **Pacify / convert / Jaded** (Faith Leader)

---

## E. Status names — judgment call (default: KEEP)

The StS-derived status names (`Weakened`, `Vulnerable`, `Frail`, `Plated`, `Thorns`, `Intangible`,
`Exposed`, `Rattled`…) are flavor-neutral. Renaming is pure churn unless you want political reskins.
Mark any you'd like to reskin; otherwise these stay.

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
