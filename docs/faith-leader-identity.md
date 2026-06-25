# Faith Leader — identity lock + confident 20-card list

*Companion to `card-audit.md`. Everything here is authorable today with existing effects/statuses — no new code. Verified against `ApplyStatusBehaviorEffect`, `ConsumeStatusAndRaiseOpinion`, the StatusBehaviors, and the pacify-convert engine.*

---

## 1. Identity (locked)

**You don't out-shout the room. You apply quiet, accumulating pressure — Guilt, Shame, Doubt — on a dissenter until they crack and briefly become your loudest believer (a 1-turn Fanatic burst that pumps opinion), then they burn out (Jaded) and you go win someone new.**

**The loop:** stack pacify statuses → cross threshold (3 + the enemy's Jaded) → auto-convert → Fanatic burst → revert to neutral + gain Jaded. Relentlessly active; you can't coast and you can't farm one target.

**Each pacify status also does a defensive job while it sits there** (so stacking is never wasted tempo):
- **Guilt** — blunts the enemy's *push* on the meter (their offense).
- **Shame** — blunts the enemy's *shielding* of the meter (their defense).
- **Doubt** — blunts the enemy's *willingness to act* (skip chance).

**Fear:** a Hardened row (can't be converted — only silenced) and disruption before you reach 3.

### The fork that gives the class depth (free — both routes already coded)
The engine has **two payoff routes that reward opposite play**:

| Route | Effect | Rewards | Card |
|---|---|---|---|
| **Auto-burst** | `ApplyStatusBehaviorEffect` → `TryPacifyConvert` fires the instant one enemy hits threshold | **Focusing** statuses on a single target → big Fanatic burst | every stacker |
| **Board dump** | `ConsumeStatusAndRaiseOpinion` strips ALL Guilt/Shame/Doubt from ALL enemies → opinion 1:1 | **Spreading** 1-2 statuses wide, then cashing before any auto-converts | Reckoning (Absolution) |

> **Recommendation:** keep both, and make this the FL's signature decision — "do I pile on one heretic for a burst, or salt the whole room and reap it at once?" It's the FL equivalent of the core game's villain question. Tuning note: the board-dump must pay *less per status* than the focused burst, or spreading dominates (the auto-burst's Jaded escalation is its built-in tax; the dump has none, so it needs a lower rate).

### Authoring gotcha (must-know)
Pacify stackers **must apply at `Permanent` duration.** Default `DecreasePerTurn` decays the stack before it reaches 3, and the loop silently never fires. This is the single most likely reason a "correct" FL card feels dead in playtest.

---

## 2. What we keep / fix / cut (the existing 25)

| Card | Verdict | Why |
|---|---|---|
| Judgement (Guilt) | **KEEP→rename** | Real stacker; becomes "Cast Guilt." Set Permanent. |
| Excommunicate (Guilt+Shame) | **KEEP** | The multi-stacker — converts a turn faster. Core of the reward pool. |
| Absolution (ConsumeStatus) | **KEEP→rename** | The board-dump payoff. Becomes "Reckoning." |
| Gospel (on-Fanatic→draw) | **KEEP** (fix desc) | On-engine reward; just needs its text written. |
| Moral High Ground (retain Power) | **KEEP** | Protects the patient setup — on-fantasy. |
| Preach (push+shield) | **KEEP→fix** | Add `Silenced` to match the "shut them down" flavor. |
| Sermon (harvest) | **FIX (null today)** | Make it scale off `ConversionsThisTurn`. It's the harvest — must work. |
| Congregation (self-Strength Power) | **REWORK** | Strength isn't a pacify status — off-engine. Re-point to auto-apply Guilt each turn. |
| Martyrdom | **FIX** | Desc/effect fully desynced. Make it push + seed all-hostility (keep-a-villain finisher). |
| Revelation (draw, rare) | **FIX** | "Scry/free play" never built; ship it as draw 2 + retain. |
| "Rebuke" (Find Common Ground) | **KEEP→dedupe** | Basic push. Collides with the other card named Rebuke — keep one. |
| "Pray" (Gather Thoughts) | **FIX** | Drop the stray raise-all-hostility; make it Support + draw 1. |
| Stir the Flock | **KEEP→rename** | The hostility escape valve. Becomes "Call Out Sin." |
| Rebuke (RaiseOpinion) | **CUT (dup)** | Duplicate name; fold into the kept basic. |
| Accusation | **CUT** | Generic push+draw, no FL identity. |
| Blessing | **CUT from core** | Fine card, but it's a Support-payoff, not the engine. Reward-pool maybe. |
| Deflect / Pacify / Prayer | **CUT** | Generic reduce-hostility / unbuilt; Pacify's name lies (it doesn't convert). |
| Pastoral Care | **CUT (null)** | Does nothing. |
| Holy Patience / Holy Alliance | **CUT** | Unbuilt placeholders. |
| Condemnation | **CUT** | Legacy `ApplyPressureBasedOnHostility` (superseded). |
| False Prophet | **CUT/fold** | Placeholder; its push+hostility role is covered by Call Out Sin. |
| Righteous Fury | **CUT from 20** | Fine but redundant with Martyrdom; pool candidate later. |

---

## 3. The confident 20

CardType: P=Pressure, R=Rhetoric, Pol=Policy(Power). Stacker statuses are **Permanent**. Energy budget = 3/turn, so stackers are 1e (apply 2-3/turn).

### Starter set (8 distinct — teaches the whole loop in one hand)
| # | Card | Type | Cost | Effect (real classes) | Role | Source |
|---|---|---|---|---|---|---|
| 1 | **Rebuke** | P | 1 | `RaiseOpinionEffect` 5 | basic push / floor | keep+dedupe |
| 2 | **Pray** | P | 1 | `GainBufferShieldEffect` 5 + `DrawCardsEffect` 1 | basic defense + hand fuel | fix |
| 3 | **Cast Guilt** | R | 1 | `ApplyStatusBehaviorEffect` Guilt 1, Permanent, Opponent | stacker (blunt push) | keep (Judgement) |
| 4 | **Cast Shame** | R | 1 | `ApplyStatusBehaviorEffect` Shame 1, Permanent, Opponent | stacker (blunt shield) | new |
| 5 | **Sow Doubt** | R | 1 | `ApplyStatusBehaviorEffect` Doubt 1, Permanent, Opponent | stacker (blunt willingness) | **new — no playable Doubt applier exists today** |
| 6 | **Call Out Sin** | R | 1 | `RaiseTargetHostilityEffect` 2 + `ApplyPressureEffect` 2 | echo-chamber escape / seed a villain | keep (Stir the Flock) |
| 7 | **Sermon** | P | 1 | `RaiseOpinionEffect` amountSource=`ConversionsThisTurn` ×N | harvest payoff (wants a convert this turn) | **fix (null today)** |
| 8 | **Confession** | P | 1 | `GainBufferShieldEffect` 4 + `ApplyStatusBehaviorEffect` Guilt 1 Permanent | defensive stacker — bridges survive↔engine | new |

*Teaching arc in one hand: play Cast Guilt + Cast Shame + Sow Doubt on one enemy → it converts → Fanatic burst → Sermon cashes the conversion. The any-3 rule taught by doing.*

### Reward pool (12 — deepens, branches the fork)
| # | Card | Type | Cost | Effect (real classes) | Role | Source |
|---|---|---|---|---|---|---|
| 9 | **Excommunicate** | R | 1 | Guilt 1 + Shame 1, Permanent | multi-stacker (convert a turn faster) | keep |
| 10 | **Litany** | R | 2 | `ApplyStatusBehaviorEffect` Guilt 1, **AllOpponents**, Permanent | board-wide setup → feeds the dump | new |
| 11 | **Crusade** | R | 1 | `ApplyStatusBehaviorEffect` (chosen) **2 stacks**, Opponent, Permanent | fast focused convert | new |
| 12 | **Reckoning** | P | 2 | `ConsumeStatusAndRaiseOpinion` | **board-dump payoff** (the spread route) | keep (Absolution) |
| 13 | **Gospel** | Pol | 1 | Power: on Fanatic applied → `DrawCardsEffect` 1 | convert→draw engine | keep (write desc) |
| 14 | **Congregation** | Pol | 2 | Power: TurnStart `ApplyStatusBehaviorEffect` Guilt 1 to a hostile enemy | passive auto-stacker | rework |
| 15 | **Penance** | P | 2 | Doubt 1 AllOpponents + `GainBufferShieldEffect` per enemy | defensive board setup | new |
| 16 | **Martyrdom** | R | 2 | `ApplyPressureEffect` 8 + `RaiseAllOpponentsHostilityEffect` 2 | keep-a-villain finisher | fix |
| 17 | **Moral High Ground** | R(Power) | 1 | Power: drawn cards retain + Support | protects patient setup | keep |
| 18 | **Revelation** | P | 1 | `DrawCardsEffect` 2 + `MakeCardRetainEffect` | hand fuel / dig | fix |
| 19 | **Preach** | P | 2 | `ApplyPressureEffect` 6 + `ApplyStatusBehaviorEffect` Silenced 1 | hard-silence a loud enemy | fix |
| 20 | **Zealots** | R | 2 | `ApplyStatusBehaviorEffect` (chosen) 3 stacks one enemy → over-threshold = bigger burst | over-stack payoff (per design doc) | new |

### What the 20 covers (sanity check)
- **6 stackers** (3 single-status, 2 multi/burst, 1 board-wide) — the engine has fuel for both routes.
- **3 distinct payoffs** — Sermon (harvest the auto-burst), Reckoning (board dump), Zealots (over-stack) — each rewards a different stacking pattern.
- **2 passive engines** (Gospel, Congregation) for the build-around.
- **4 defense/hand-fuel** (Pray, Confession, Moral High Ground, Revelation) — the setup class survives its slow turns.
- **1 hostility escape** (Call Out Sin) + **1 finisher** (Martyrdom) keep a villain present so the room never echo-chambers.

### New designs needed (5): Cast Shame, Sow Doubt, Confession, Litany, Crusade, Penance, Zealots — all are just `ApplyStatusBehaviorEffect` re-skins (target/stacks/status vary). Cheapest content in the game to author.
