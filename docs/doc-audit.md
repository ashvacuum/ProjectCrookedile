# Docs Audit — what's still true

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-10-03
>
> **Summary:** Standing audit of the docs against the code: what was stale and what was done about it.
>
> **Source of truth:** this doc · **Related:** [`needs-detailing.md`](needs-detailing.md)

*Run 2026-09-06 against the code as it stands. Every claim below was checked against an asset or
a source file, not against another doc. This file records **findings**, not decisions: where a
doc and the code disagree about **intent**, the doc still wins (per `CLAUDE.md`) and the row says
so rather than assuming the code is right.*

---

## Verdict per file

| Doc | Last touched | Verdict |
|---|---|---|
| `core-design.md` | Jun 6 | **Canonical, holds up.** The Faith Leader spine (§7) matches `PacifyConversionEngine` exactly — threshold `3 + Jaded`, fuel consumed, Hardened silenced instead. The items it flags as open are still open. |
| `metagame-campaign.md` | Jul 2 | **Canonical, terminology stale.** See "Relic → Ally" below. Phase R is described as pending; it shipped under a different name. |
| `campaign-encounters.md` | Jul 29 | **Canonical, mostly true.** Origin start table is stale (below). Outcome table predates three new outcomes. |
| `enemy-design-bible.md` | Jul 8 | **Canonical.** Roster question tracked separately; not re-audited here. |
| `encounter-authoring-reference.md` | Aug 28 | **Accurate except the tables**, which are the part people actually read. §3.2/§3.3 name two types that were renamed and miss four that exist. Highest-value fix in the folder. |
| `needs-detailing.md` | Sep 6 | **Current.** Header still says "As of 2026-06-10" and points at a file that now lives in `deprecated/`. |
| `naming-glossary.md` | Jun 7 | **Half-executed, no decisions recorded.** See below. |
| `campaign-build-checklist.md` | Jul 29 | **Stale checkboxes.** 9 unchecked, at least four of which are done. |
| `roadmap.md` | Jul 3 | **Superseded by events.** Targets already met or overshot; the relic paragraph describes a system under a name the code doesn't use. |
| `card-audit.md` + `card-audit.csv` | Jun 25 / Jul 12 | **Obsolete.** Audits a card set at a path that no longer exists. Some of its *design* findings are still live — see below. |
| `faith-leader-identity.md` | Jun 25 | **Overtaken.** "Confident 20-card list"; there are 38 authored FL cards. |
| `crookedile-starter-decks.md` | Jun 6 | Unverified against the rebuilt card set. Worth a pass once the deck list settles. |
| `art-bible.md` | Jul 3 | **Internally contradictory** — §0.1 locks a flat Odd-Taxi direction, then the next paragraph specifies "dark painterly realism, no cel-shading". One of those is dead. |
| `opinion-meter-passes.md` | May 30 | Oldest live doc. Not audited. |
| `ui-vfx.md` | Jul 28 | Not audited. |
| `guides/` | — | Empty directory. |

---

## The findings, in order of how much they mislead

### 1. "Relic" does not exist in the codebase

42 mentions across 7 docs; **zero** occurrences in `Assets/Scripts/`. The concept shipped as
**Ally**: `AllyData`, `AllyDatabase`, `RunState.Allies`, `RecruitAllyOutcome`, `HasAlly`.
`AllyData`'s own summary calls it "the StS relic slot, framed as a person," so the rename was
deliberate — the docs simply never followed.

Affected: `metagame-campaign.md` (14), `campaign-build-checklist.md` (9),
`encounter-authoring-reference.md` (6), `campaign-encounters.md` (5), `roadmap.md` (4),
`core-design.md` (2), `needs-detailing.md` (2).

Two of those are *wrong type names in an authoring reference*, which is the kind of error that
costs someone twenty minutes in a type-picker: `GrantRelicOutcome` → `RecruitAllyOutcome`,
`HasRelic` → `HasAlly`.

**Not a pure find-and-replace.** `roadmap.md` claims "Phase R done — RunState.Relics,
PassiveResolver folding, 5 prototype relics + debug grant". `RunState.Allies` and the
`PassiveResolver` folding exist; **two** allies are authored (Crowd Control, The Beef), not five,
and there is no debug-grant cheat. That paragraph needs rewriting, not renaming.

### 2. `encounter-authoring-reference.md` §3.2 / §3.3 miss six types

Present in code, wrong or absent in the tables:

- `GrantRelicOutcome` is listed; the real type is `RecruitAllyOutcome`.
- `HasRelic` is listed; the real type is `HasAlly`.
- `AdjustCredibilityPercentOutcome` — missing.
- `AdvanceDayOutcome` — missing.
- `GainCardFromPoolOutcome` — missing *(added 2026-09-06)*.
- `OriginIs` requirement — missing *(added 2026-09-06)*.

`GainRandomCardOutcome`'s row is also out of date: it now carries `Scope`
(Any / PlayerOrigin / Colorless), `RestrictType` + `Type`, `RestrictRarity` + `Rarity`, and
`Count`, where the table lists only the rarity pair.

### 3. Origin starting values are stale in two places

`campaign-encounters.md:246` and `encounter-authoring-reference.md:304` both print:

| Origin | Funds | Credibility |
|---|---|---|
| Faith Leader | 20 | 40 |
| Nepo Baby | 120 | 10 |
| Celebrity | 60 | 30 |

`OriginDatabase.asset` now holds **150/90, 600/65, 250/79**. Both docs say these are "seeded
defaults, not balance", so tuning is expected — but they print them as fact, and the *reads-as*
column ("Faith Leader: broke but trusted") no longer describes 150 Funds.

**Also a design question, not just a doc fix.** Both docs state day length is 3 Hours and build
the hour-budget argument on it; the asset sets `MaxHours: 24` for all three origins. A 24-hour
day is a different game from a 3-hour day — decide which is intended before either file is
edited. `CLAUDE.md`'s "Known soft spots" entry, which says these three fields are unset for all
origins, is stale too.

### 4. `card-audit.md` audits a card library that was deleted

It scopes itself to `Assets/Resources/Cards/`. That directory no longer exists. Cards now live in
`Assets/Data/Cards/FaithLeader/{Basic,Enhanced,Rare}` — 38 Faith Leader cards, plus Status and
Curses.

Checked against the current assets, its headline findings resolve as:

| Finding | Now |
|---|---|
| §1 "Deal X Resolve damage" everywhere | **Fixed.** Zero card assets contain the phrase. |
| §2 FL spine not built (no Doubt applier, Sermon null, one rare convert payoff) | **Fixed.** Four Doubt appliers; Sermon has effects; `ConvertPacifiedEffect` is authored on Gospel. |
| §3 Nepo Baby is generic goodstuff (7 cards) | **Changed shape.** Zero `nepobaby` cards exist — the pool is empty, not generic. |
| §4 Celebrity has zero cards | **Still true.** Zero `celebrity` cards. |
| §5 Scandal built backwards | Unverified against the rebuilt Curses. |
| §6–§8 desyncs, null effects, duplicate asset names | Unverified against the rebuild. |
| §10 CardType colour taxonomy carries no meaning | Still an open design question, independent of the rebuild. |

Recommend retiring the file to `deprecated/` and lifting §5 and §10 into `needs-detailing.md` as
live design questions, rather than editing an audit of assets that are gone.

### 5. `naming-glossary.md` is half-executed with an empty Decision column

- **B2 (Shield → the directional buffers)** — **executed**, and not as proposed. The doc offers
  `Buffer` with a question mark; the code went to Support/Denial outright (`GainSupportEffect`,
  `ConsumeAllSupportEffect`, `SupportEqualToHostilityEffect`). No `Shield` symbol survives.
- **B1 (Damage → Pressure)** — **not executed.** `DamageDealtEvent`, `DamageDealtTrigger` and
  `DamageMinimumCondition` are still there across 11 files. Note the *player-facing* purge did
  happen — no card text says "Resolve damage" — it's the code symbols that didn't move.

So the file reads as an open proposal when half of it is settled. Fill in the Decision column, or
split the executed rows out.

### 6. Broken cross-references into `deprecated/`

- `needs-detailing.md:3` — "Execution tasks live in `work-now.md`". That file is in `deprecated/`.
- `needs-detailing.md:50` — "(work-now §1)".
- `metagame-campaign.md:139` — "closes `work-now.md` §6 item".
- `roadmap.md:4` — companion doc `test-plan.md`, also in `deprecated/`.

Either those files come back out of `deprecated/`, or the pointers need to name whatever replaced
them. `needs-detailing.md`'s header also still says "As of 2026-06-10" despite being edited today.

### 7. `campaign-build-checklist.md` — unchecked boxes that are done

Of 9 unchecked items, at least four have shipped:

- `RunRequirement` base + subclasses (line 195) — built, six subclasses, used in three slots.
- Promote HQ to a real `EventEncounterData` (line 223) — `Assets/Data/Encounters/HQ/Headquarters.asset`.
- Author 2–3 test events (line 228) — nine encounters authored.
- Content Hub events provider (line 226) — needs confirming, but the Hub audits encounters today.

`CampaignMapData` also appears twice (lines 146 and 172), the second marked "deferred, possibly
dead". Resolve the duplicate.

### 8. `art-bible.md` contradicts itself in adjacent paragraphs

§0.1 declares the flat, muted Odd-Taxi direction the "reference of record (approved 2026-07-03)"
and says the dark painterly mock is superseded. The **Rendering style** line immediately below
still specifies "dark painterly realism (digital-oil) … No cel-shading, no flat vector."

An artist reading top to bottom gets opposite instructions two paragraphs apart. Deleting the
stale line is a one-line fix, and there's an art track reading this file.

---

## Suggested order

1. `encounter-authoring-reference.md` §3.2/§3.3 — wrong type names cost time immediately.
2. `art-bible.md` — one contradictory line.
3. Relic → Ally across the seven docs, with `roadmap.md`'s paragraph rewritten rather than renamed.
4. Decide the 3-vs-24 Hours question, then fix both origin tables together.
5. Retire `card-audit.md`, `card-audit.csv` and `faith-leader-identity.md` to `deprecated/`,
   lifting their still-live design questions into `needs-detailing.md`.
6. `naming-glossary.md` Decision column; `campaign-build-checklist.md` checkboxes; the four broken
   `deprecated/` pointers.

---

## Acted on 2026-09-11

Step 5 of the order above is **done**, plus two files this audit had not reached:

| File | Action |
|---|---|
| `card-audit.md`, `card-audit.csv` | → `deprecated/`. §5 (Scandal backwards) and §10 (CardType taxonomy) lifted into `needs-detailing.md` as items 5 and 11. |
| `faith-leader-identity.md` | → `deprecated/`. Identity lock lives in `core-design.md` §7; the 20-card list is history. |
| `roadmap.md` | → `deprecated/`. Superseded by events; the Phase R paragraph was wrong twice over (finding 1). |
| `campaign-build-checklist.md` | → `deprecated/`. Tracker for a build that shipped — resolves finding 7 without editing 9 checkboxes. |
| `opinion-meter-passes.md` | → `deprecated/`. **Verified executed:** Pass 2 in `EnemySlotUI`, Pass 3 in `OpinionMeterUI`, Pass 4 in `EnemyMoveType`. EncourageSides lifted to `needs-detailing.md` item 10. |
| `guides/` | Deleted — empty and untracked. |

Finding 6 (broken `deprecated/` pointers) is closed: `needs-detailing.md`'s two `work-now`
references and `metagame-campaign.md:139` now name the path. `needs-detailing.md`'s header date
is current. `readme.md`'s documentation map was rebuilt around the survivors, and picked up
`encounter-authoring-reference.md`, which it had never listed.

### Still open, in order

1. `encounter-authoring-reference.md` §3.2/§3.3 — six wrong or missing type names (finding 2).
2. `art-bible.md` — the contradictory rendering line (finding 8).
3. Relic → Ally across the four remaining docs (finding 1). `roadmap.md`, the worst offender,
   left with its deprecation; `metagame-campaign.md` (14 mentions) is now the top one.
4. The 3-vs-24 Hours question, then both origin tables (finding 3). Still a design call.
5. `naming-glossary.md`'s empty Decision column (finding 5).

---

## Acted on 2026-10-03

All five "still open" items above are closed:

| Item | Resolution |
|---|---|
| 1. Authoring-reference type names | §3.2/§3.3 rebuilt from the code: every `RunOutcome` and `RunRequirement` subclass listed with its fields, including `GainRandomCardOutcome`'s full field set and the new `UnlockContentOutcome` / `HasUnlocked`. |
| 2. `art-bible.md` rendering line | Already fixed before this pass: the Rendering style line now matches §0.1 (flat). |
| 3. Relic → Ally | Code-reference docs (`encounter-authoring-reference.md`, `campaign-encounters.md`) use the real names. `metagame-campaign.md` keeps "relic" as the design term with a note pointing at the Ally code names; `naming-glossary.md` records the decision. |
| 4. Hours and origin tables | The asset now uses `MaxHours: 8` for all origins. Both origin tables print the current `OriginDatabase` values (150/90, 600/65, 250/79) and the run default of 8. |
| 5. Glossary Decision column | Already filled before this pass (status line dated 2026-09-06). |

Also brought up to date in this pass: `readme.md` (docs map, Resources list, editor tools, save
system, status line), `AGENTS.md` (re-synced with `CLAUDE.md`), `campaign-encounters.md` (dead
checklist links, "blocked staples" section, encounter database note), and the save system in
`metagame-campaign.md` and `meta-progression.md`.

