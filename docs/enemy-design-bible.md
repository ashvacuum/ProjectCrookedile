# Crookedile — Enemy Design Bible (v2, shared-meter model)

> Source: `crookedile_enemies.xlsx` (2026-07-08). Corrected to match locked core design: ONE shared Opinion Meter per room; enemies are conditions to manage, not HP bars to delete.
>
> **Session notes (2026-07-08):** Credibility (P2) is a META/campaign resource, not a battle loss axis — parked as a goal. Ward redesigned: consumable status, not untargetability (see Roles). Echo Chamber needs its own design pass. Target types (below) not finalized — free to redo.

## 1. Design Pillars

| # | Pillar | Detail | Status |
|---|--------|--------|--------|
| P1 | Win axis: the shared Opinion Meter | ONE meter per room. Player pushes it up (Sway); enemies push it down / block it. Fill it = win the room. Directional shields: enemies place up-block shields, player places down-block shields. | LOCKED (core design) |
| P2 | Loss axis: player Credibility | Player Credibility hits 0 = run over. Enemy attacks (Smears/Exposés/Red-tags) damage Credibility. Composure = the block analog. | ~~LOCKED~~ → META resource, parked (2026-07-08) |
| P3 | Enemies are conditions, not HP bars | Not "killed." Managed: statused (Guilt/Shame/Doubt), Silenced, state-flipped, converted (Fanatic), corrupted. Room ends when the METER fills. | LOCKED (core design) |
| P4 | Hostile / Receptive states + Echo Chamber | Per-enemy states modify behavior and card effectiveness. ECHO CHAMBER: if ALL enemies are Receptive, meter gains are HALVED and the meter decays. Keep ≥1 non-receptive enemy — managing the mix IS the game. | LOCKED (needs design pass on UI/holdout) |
| P5 | Role × Stance variety engine | 7 roles: Pusher, Shielder, Amplifier, Converter, Inflictor, Escalator, Protector. Variety from role combos + stances, not bespoke stat blocks. | LOCKED |
| P6 | One question per encounter | Each encounter composition asks one deck question — usually in the COMBO of roles, not a single enemy. | LOCKED |
| P7 | Every room has a clock | Meter decay, enemy ramp (Escalator), shield stacking, Credibility chip, deck pollution. No free turtling. | LOCKED |
| P8 | Readable intents (voice intents) | Telegraphed via the 21-glyph set. Conditional intents allowed ("Counter-Argues IF you play Rhetoric"). Constrained randomness in move pools. | LOCKED |
| P9 | Bosses = rigged rooms | Rival candidates contesting the same meter. Phases trigger via ROOM CHANGES (new enemies, shields, rules), not HP thresholds. Incumbent = final boss. | LOCKED (core design) |
| P10 | Archetype asymmetry | Rate encounters vs Nepo Baby (Patronage/summons), Faith Leader (Guilt/Shame/Doubt→Fanatic engine, Jaded escalator), Celebrity (Scandal-fuel subarchetypes). No encounter uniformly hard. | IN PROGRESS |
| P11 | Theme & act framing | Anthro Filipino animals + mythological creatures, Spiritfarer-adjacent painterly register. Wardrobe = class satire. Acts: Barangay → City → National (proposed). Metagame: 30-day campaign, Debate milestone boss. | LOCKED / PROPOSED |

## 2. Room Anatomy — shared meter model

| Element | What it is | Player interaction | Enemy interaction |
|---|---|---|---|
| Opinion Meter | Single shared per-room win track. Fill = win. | Sway pushes (Pressure cards), Fanatic bursts (Faith Leader), Scandal payoffs (Celebrity). | Pushers drag it down; Shielders block upward movement. |
| Directional shields | Blockers on the meter. Enemy shields block UP; player shields block DOWN. | Player places down-block shields (Composure/Rhetoric tools); can strip enemy up-shields. | Shielder role places/refreshes up-shields. |
| Player Credibility | *(META resource — campaign layer, parked)* | Guard with Composure; heal rarely. | Smear/Exposé/Red-tag moves chip it. Which roles carry Credibility attacks = D8. |
| Enemy states (Hostile/Receptive) | Per-enemy behavior modes. | Flip states via cards/statuses; MANAGE the mix — all-Receptive triggers Echo Chamber penalty. | Converters drag Receptives back to Hostile; some (Televangelist) weaponize Echo Chamber deliberately. |
| Statuses on enemies | Guilt, Shame, Doubt (pacify trio), Silence, Devotion, Jaded, Hardened, Fanatic, Turncoat, Warded. | FL engine: stack G/S/D to 3+Jaded → consume → 1-turn Fanatic bursts meter → reverts. Jaded = permanent per-enemy escalator (anti-milking). | Hardened resists statuses; Protectors guard allies via Warded stacks. |
| The row | Ordered enemy positions. Adjacency matters (Praise). | Target selection, positional cards, Nepo summons occupy slots (most corruptible). | Amplifier/Converter range limits (proposed D10), summon insertion (bosses). |
| Clocks | Anti-stall pressure. | Race or control. | Escalator ramps, meter decay, shield stacking, pollution, Credibility chip. |

## 3. Role Taxonomy + Targeting

| Role | Targets | Core moves | Counterplay | Notes / open questions |
|---|---|---|---|---|
| Pusher | The Meter (down) | Sway-down pushes; heavy variants telegraphed | Down-block shields; Silence; flip to Receptive weakens pushes | Baseline threat. Credibility-attack variant? (D8) |
| Shielder | The Meter (up-block shields) | Place/refresh up-shields; shield-stack ramp | Shield-strip cards; overwhelm with burst (Fanatic) | Shield HP vs count — D9, leaning count (code = count ✓) |
| Amplifier | Other enemies (buff) | +Sway to allies, +shield value, haste | Priority target: Silence or pacify first; positional range limits it | The Tita (gossip). Range = adjacent only? (D10) |
| Converter | Other enemies (states) | Drag Receptive → Hostile; strip player-applied statuses | Time your all-in around his cooldown; Silence | Also the anti-Echo-Chamber relief valve — deliberate tension! |
| Inflictor | Player deck / statuses | Inject Scandal (permanent) or Heckle (temporary); player debuffs | Purge tools; Celebrity WANTS the Scandals (inversion) | Tabloid Reporter. Celebrity rating flips on these fights. |
| Escalator | Self | Ramp stacks (Airtime): +push, +resist | Race; or pacify early before stacks matter | The clock role. |
| Protector | Other enemies (guard) | **Warded stacks on allies (REDESIGNED 2026-07-08):** each stack absorbs the next hostility change or incoming debuff on the carrier, then is consumed. Ally stays targetable. | Burn the stacks with cheap effects; pacify/Silence the Protector so wards stop refreshing | Supersedes the old "untargetable, no redirect" lock; D11 (AoE bypass) is MOOT. |

### Player-side target types (NOT finalized — free to redesign)

| Code | Target type | Examples | Rules |
|---|---|---|---|
| MTR | The Meter | Pressure pushes, Fanatic burst | No enemy selection. Blocked by enemy up-shields unless [pierce]. |
| ST | Single enemy | Insinuate Sins, Cast Out, Silence | Receptive enemies REMAIN legal targets; Jaded punishes milking (LOCKED). |
| POS | Positional (row-relative) | Praise (adjacency) | Row is ORDERED; enemies do not move; Nepo summons choose slot (LOCKED). |
| AOE | All enemies | Preach-type wide status | — |
| RND | Random enemy | (none yet) | D12: leaning NONE — deterministic fits debate-puzzle identity. |
| SLF | Self / player | Composure, draw, Patronage, purge | — |
| SHD | Shields (meter objects) | Shield-strip | Shields as targetable objects distinct from enemies. |
| ALY | Own summons (Nepo) | Buff/sacrifice allies | Allies occupy row slots; enemies may target them (most corruptible). |

## 4. Act 1 — Barangay Politics (encounter compositions)

Enemies are conditions → the QUESTION lives in the role combo.

| ID | Slot | Encounter | Members (animal — roles — key moves & targets) | The Question | Clock | vs Nepo | vs Faith | vs Celeb |
|---|---|---|---|---|---|---|---|---|
| E01 | Easy | Tambay | 1x Askal — Pusher — Sway-down [MTR], light Smear | Push vs block on the meter? | Push +1 every 2 turns | Easy | Easy | Easy |
| E02 | Easy | Chismosa Pair | 2x Maya — Pusher + Amplifier — Tita-lite: one pushes, one buffs her | Do you see priority (buffer first)? | Amplified pushes stack | Easy | Easy | Easy |
| E03 | Easy | Barangay Tanod | 1x Carabao — Pusher/Escalator — ramps while Hostile; flipping Receptive stops ramp | Do you use states, not just pushes? | +1 push/turn while Hostile | Easy | Easy (his engine) | Med |
| E04 | Easy | Vlogger | 1x Parrot — Inflictor — telegraphs big Heckle dump [deck], avoidable if answered | Do you read voice intents? | Heckles accumulate if ignored | Easy | Med | Easy (fuel-ish) |
| N01 | Normal | Troll Farm | 4x Rat — all Pushers, weak — swarm math | Do you have wide answers (AoE status / wide flips)? | Each un-pacified rat = +1 push/turn | Hard (slots contested) | Med | Easy (Attention AoE) |
| N02 | Normal | The Tita | 1x Cat Tita — Amplifier/Converter + 1x Askal Pusher — Tita buffs AND drags Receptives back Hostile | Silence/pacify the engine or race the output? | Amplified pushes ramp | Med | Med | Med |
| N03 | Normal | Tabloid Reporter | 1x Musang — Inflictor/Escalator — Scandal injection [deck] + self-ramp | Can your deck absorb permanent pollution? | 1 Scandal every 2 turns | Hard | Hard | EASY (fuel!) |
| N04 | Normal | Fence-Sitters | 3x Butiki — Pushers with meter DECAY aura | Can you commit and close (tempo)? | Meter decays 15%/turn | Med | Hard (setup-heavy) | Easy |
| N05 | Normal | Bouncer & Handler | Bayawak Pusher/heavy Credibility hits + Ahas PROTECTOR warding him | Do you answer wards (burn stacks / pacify Protector)? | Bayawak Smears ramp | Med | Med | Hard |
| N06 | Normal | Radio Commentator | 1x Tandang — Shielder/Escalator — stacks up-shields + Airtime | Can you strip shields or burst through? | Airtime +1/turn | Med | Easy (Fanatic burst) | Med |
| EL1 | Elite | The Televangelist | 1x Peacock — Converter(inverted)/Shielder — pushes YOUR enemies Receptive to force Echo Chamber penalty on you | Manage the state MIX, not just flip everything friendly? | Echo Chamber decay while all-Receptive | Med | Hard (mirrors yours) | Med |
| EL2 | Elite | The Dynast | 1x Agila — Pusher/Amplifier + summons proxy allies (Nepo mirror) — summons occupy row, warded | Handle summon pressure + wards? | New proxy every 3 turns | Hard (mirror, slot war) | Med | Med |
| EL3 | Elite | The Comment Section | 3x Uwak — Inflictors, CHAIN-adjacent — alternate Heckle floods + Smears | Purge/cycle through temporary pollution? | Heckle flood scales | Med | Hard | Easy |
| B01 | Boss | The Kagawad Machine | Rigged room: Shielder core + rotating Pusher adds; PHASE = room change swaps the add roster | Full exam: shields + priority + tempo | Room change every N turns | Med | Med | Med |
| B02 | Boss | The Debate (milestone) | Rival candidate (croc-adjacent?) contests the SAME meter — pushes it toward HIS side; audience enemies as conditions | Out-tempo a mirror who uses your win axis? | Meter is tug-of-war; stale after N turns | Med | Med | Med |
| B03 | Boss | The Network Executive | Gagamba — Inflictor/Shielder rigged room — News Cycle staleness + scheduled Primetime Exposé Credibility nukes | Burst windows + Credibility survival | Gains halve every 4 turns (stacking) | Med | Med | Hard |

All Draft status.

## 5. Hostile/Receptive Mix — the group-fight system

v1 wrongly modeled per-enemy Opinion ripple links. Actual system: the group puzzle is the STATE MIX under the Echo Chamber rule.

| Dynamic | How it works | Puzzle created | Enemies that exploit it |
|---|---|---|---|
| Echo Chamber penalty | All Receptive → meter gains halved + decay. You WANT a managed mix. | Anti-snowball: the "winning" state is a trap. Deciding WHO stays Hostile (weakest pusher) is the skill. | Televangelist force-feeds Receptives; converters removing Hostiles can accidentally trigger it. |
| Hostile ramps | Some escalate while Hostile (Tanod). | Flip the ramping ones, keep a harmless one Hostile as "designated holdout." | Escalator-Pushers. |
| Receptive vulnerabilities | Receptives take amplified statuses / enable payoffs, but count toward Echo Chamber. | Milking tension — Jaded escalates per-enemy to cap it. | FL target-milking (Jaded exists for this). |
| Converter tension | Enemy Converters drag Receptives back Hostile — HURTS their Echo Chamber defense, HELPS you avoid the penalty. | Sometimes you LET the Converter act. When to Silence him vs use him. | The Tita, Televangelist counterplay. |
| Protector wards | Warded stacks absorb hostility shifts / debuffs on allies. | Burn stacks vs remove Protector; composition ordering. | Handler (N05), Dynast proxies. |
| Row adjacency | Ordered row; adjacency cards (Praise); summons pick slots. | Positional value; Amplifier range limits (proposed D10). | Comment Section (chain-adjacent), Nepo slot war vs Dynast. |

### Design rules (v2)
- **R1** Every encounter has at least one "safe holdout" candidate (low-threat enemy you can afford to leave Hostile) OR deliberately denies one (elite/boss pressure).
- **R2** Echo Chamber UI: show the penalty state loudly BEFORE triggering (warning at all-but-one Receptive). *(Needs design pass.)*
- **R3** ~~Protector: ward untargetable by ST~~ → superseded by Warded-stacks redesign.
- **R4** Row is ordered; enemies never reposition (v1); Nepo summons choose slot on entry.
- **R5** No random-target player cards in v1 (pending D12) — deterministic debate-puzzle identity.
- **R6** Receptive enemies stay legal targets; Jaded is the anti-milking valve, not targeting rules.

## 6. Voice Intents — 21-glyph mapping

| Intent category | Meaning | Example moves | Conditional? | Code mapping (EnemyMoveType) |
|---|---|---|---|---|
| Push (meter) | Sways the meter down by shown amount | Rally the Base, Block Walk | No | Attack / DebuffAttack |
| Shield (meter) | Places/refreshes up-block shields | Stonewall, Talking Points | No | DefendOpinion |
| Smear (Credibility) | Damages player Credibility | Callout, Exposé, Red-tag | No | *(meta layer, parked)* |
| Buff (ally) | Amplifies an ally (show WHO via row arrow) | Chika, Endorsement | No | RileOthers / Buff |
| Convert (state) | Drags a Receptive back to Hostile (show WHO) | Sermon, Guilt Trip | No | RileOthers |
| Inflict (deck) | Injects Scandal (permanent) or Heckle (temp) | Fake News, Front Page | No | Debuff + AddCardToDeckEffect |
| Ramp (self) | Escalator stack incoming | Airtime | No | Buff + turn-gated condition |
| Ward (guard) | Wards an ally (Warded stacks) | Human Shield | No | **Ward = 10** (built) |
| Counter (conditional) | Punishes IF player does X this turn | "Counter-Argue if Rhetoric played" | YES | **Counter = 11** (built; fizzles to idle) |
| Threshold (conditional) | Triggers at meter % / turn count | Phase/room changes, Padrino wake | YES | **OpinionAtOrAbove/Below conditions** (built) |
| Unknown (?) | Hidden; boss openers only | — | Sparingly (P8) | not built |

Pattern rules: move pools with constraints (no repeat x2 unless scripted; threshold/room-change moves override pool; bosses get scripted openers).

## 7. Open Decisions (v2)

| # | Decision | Options | Leaning | Blocks |
|---|---|---|---|---|
| D8 | Which roles carry Credibility attacks? | (a) Pusher variants only (b) any role can carry a Smear move (c) dedicated 8th role | (b) — moves, not roles | Enemy kit writing *(meta layer, parked)* |
| D9 | Shield model: HP vs count | (a) shields have HP (b) discrete charges stripped 1/card | (b) count — code already does this | Shielder kits, shield-strip cards |
| D10 | Amplifier/Converter range | (a) global (b) adjacent-only | (b) adjacent-only — makes row order matter | Row/positional design |
| D11 | ~~AoE bypass Protector wards?~~ | — | MOOT — ward redesigned to consumable stacks | — |
| D12 | Random-target cards allowed? | (a) none (b) commons-only | (a) none for v1 | Card audit |
| D13 | Heckle exit rule | (a) exhaust on draw/play (b) end-of-encounter purge | TBD | Inflictor tuning, EL3 |
| D14 | Credibility recovery economy | Rest slots? Cards? Post-room partial? | TBD — campaign-layer pass *(parked with Credibility)* | Difficulty tuning |
| D15 | Numbers pass | Meter size, push values, shield counts, status thresholds | AFTER kits + targeting locked | Everything downstream |
| D16 | Boss 2 "The Debate" tug-of-war | Rival pushes same meter negative vs own meter | Tug-of-war — same meter, purest mirror | B02 design |
