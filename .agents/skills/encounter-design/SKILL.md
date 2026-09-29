---
name: encounter-design
description: Author or review a Crookedile campaign encounter — event or battle — including its pool row, options, outcomes, requirements, and day scheduling. Use when adding an encounter, tuning when one appears, wiring event choices, gating content behind flags or prior encounters, or diagnosing why an encounter never shows up.
---

# Encounter design

An encounter is a location on the campaign map. The player spends Hours to enter it. A
seven-day run offers `CampaignFlow._locationsPerDay` locations per day, drawn from an
`EncounterPoolData`.

Two halves, authored in different places, and most confusion comes from mixing them up:

| | Lives on | Answers |
|---|---|---|
| **The encounter** | `EncounterData` asset | What happens when you go there |
| **The pool row** | `EncounterPoolEntry` in the pool | When and how often it's offered |

An encounter with no pool row never appears. A pool row is not part of the encounter — the same
encounter can hold several rows with different windows.

## Making one

Use **New Event** / **New Battle** on the pool asset. That creates the asset beside the pool,
adds its row, and selects it. Don't right-click-create and wire it by hand.

- **Battle** — press **New** beside the Session field. It creates the session as a child of the
  encounter. Author exactly one round; see "Sequencing" below.
- **Event** — write Body (the panel text) and at least one option. **An event with zero options
  traps the player in a panel they cannot leave.**

Blurb is the one line on the map button; Body is the scene text after committing.

## Options, outcomes, requirements

An option is a label, its outcomes, and the result text shown after. Requirements gate it — an
unmet option renders **disabled with its reason, never hidden**, because seeing the door you
can't open is most of what makes a gate interesting.

Outcomes: `AdjustFunds`, `AdjustCredibility`, `AdjustCredibilityPercent`, `GainCard`,
`GainRandomCard`, `RemoveCard`, `RemoveRandomCard`, `UpgradeRandomCard`, `UpgradeChosenCard`,
`RemoveChosenCard`, `GoToEncounter`, `SetFlag`, `RecruitAlly`, `AdvanceDay`.

Requirements: `HasVisitedEncounter`, `HasFlag`, `FundsAtLeast`, `CredibilityAtLeast`, `HasAlly`,
`DayAtLeast`. All negatable. Requirements are ANDed.

Every outcome applies, in order. There is no branching within an option — a different
consequence is a different option, or a chain into a different encounter.

## Scheduling (the pool row)

- **First/Last Day** — the window. Last Day `0` means no end.
- **Weight** — *relative*, not a probability. A day's chance is this weight over the total weight
  of everything eligible that day, so the same weight is common on a thin day and rare on a busy
  one. `-1` inherits the encounter's own `DropWeight`.
- **Guaranteed** — appears every day in its window, ahead of the random picks. This is the only
  way to make structure certain; a weight alone gives you "likely", which you cannot design
  around. **It still consumes one of the day's slots.**
- **Once Per Run** — retires after being visited. This is **per encounter, not per row**: if one
  row is once-per-run, visiting it retires every row for that encounter.
- **Requirements** — hard gate, ANDed. **Boost If** + **Boost x** — soft nudge, multiplies weight
  when all hold; the encounter stays available either way.

## Sequencing

An encounter has no "and then" of its own. Sequences are a property of a **choice**:
`GoToEncounterOutcome` on an option, which can point at a battle — so "refuse him and fight now"
is one option on one event. Later availability is a dependency instead: `HasVisitedEncounter` on
a pool row.

Multi-round `BattleSession`s only chain battle → battle with nothing possible between. Prefer one
round per encounter.

## Flags

Free-form, case-sensitive strings. `SetFlagOutcome` writes, `HasFlag` reads. Nothing validates
the spelling — a typo means the gate or boost silently never fires. Keep them lowercase
snake_case and grep for the writer before adding a reader.

## Before calling it done

1. **Content Hub** — catches no session, no options, no body, no display name, unreachable rows.
2. **Encounter Designer → Timeline** — day windows; `w2` is the weight, `w2*` means inherited,
   `ALWAYS` means guaranteed.
3. **→ Dependencies** — gates and boosts as a graph.
4. **→ Simulate** — many seeds at once. The line that matters is **offered vs. affordable**: if
   the hour budget covers everything offered, the player never actually chooses, and no amount of
   pool breadth fixes that. Also names encounters drawn in no run, and ones in 80%+ of runs.

## Traps

- Weight `0` and not Guaranteed can never be drawn. Same for a row inheriting a `DropWeight` of 0.
- Requirements referencing an encounter outside this pool can never be satisfied by playing.
- Overlapping windows on two rows for one encounter double its weight that day; it still appears
  at most once.
- `HasVisitedEncounter` cannot tell two rows of the same encounter apart.
- Credibility and Funds gates currently fail always — every origin starts at 0 in
  `OriginDatabase`. Fix the origin before gating on either.
