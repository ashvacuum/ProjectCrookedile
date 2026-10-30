---
name: playtest-analysis
description: Analyse a Crookedile playtest bot run (a Playtests/<stamp>/ folder with report.md, battles.csv, campaigns.csv, turns.csv, card_plays.csv) and explain what it means for balance, the skill gap, progression, bosses and cards. Use whenever the user shares or names a playtest folder or stamp, says "playtest", "the bot ran", "new run", "final run", asks how an origin, card, encounter or boss is doing, whether a change worked, or compares two runs, even if they only paste a path.
---

# Playtest analysis

The Playtest Bot (Crookedile → Playtest Bot) plays standalone battles (every encounter × origin × battle bot,
a few seeds each) and whole seven-day campaign runs, then writes `Playtests/<stamp>/`. `report.md` is the
summary; the CSVs hold the raw rows. Your job is to turn that into a short, honest read for a designer.

**Analysis only.** Do not change code, assets or docs while analysing, however obvious the fix looks. End with
options and let the user decide; they give explicit permission before anything is built.

## How to judge a run

The design goal is **a good setup pays off and a bad one fails**, not raw difficulty. So:

- **Skill gap is the headline**: best of Scored/Greedy minus Random. The target is **40+ points**; around 20
  reads as a coin toss. A high win rate for careful play is fine; a high Random win rate means careless play
  isn't failing; 0% for every bot means even good play doesn't pay.
- **The win line is 60% of the meter** at the turn limit (tunable on `BattleManager`). Battles start at half,
  so doing nothing loses. "Narrow loss" (within 10 of the line) should belong to careless play.
- **Bots are heuristics, not players.** Greedy and Scored chase Opinion each turn; none plans a stance mix.
  RandomRun's battles are played by the Tempo bot. Say this when a conclusion leans on bot behaviour.
- **Hostility reducers are scarce on purpose.** Never propose more or bigger reducers to "balance" raisers.
- **Mechanic numbers must stay tunable data.** When you recommend a change, name the asset or field to tune.

## Workflow

1. **Coverage first.** Which origins, bots and how many seeds? Single-origin runs can't be compared across
   origins. Standalone cells are usually 5 battles (±20 points), so don't over-read one encounter × bot cell.
   Standalone results repeat exactly across runs when seeds and starter decks are unchanged; the new information
   is then in the campaigns.
2. **Read `report.md`** for the headline sections: Bugs and hangs (long turns of 8+ cards can be free-play
   loops), Encounter difficulty with the Skill gap column and Win margins, Card scores, Utility, Campaign runs
   with Credibility (run HP) and Progression, and Bosses.
3. **Run the bundled script** for the cuts the report doesn't make, passing the previous run to get deltas:

   ```
   py -3 -I .Codex/skills/playtest-analysis/scripts/analyze.py Playtests/<stamp> Playtests/<previous-stamp>
   ```

   It prints coverage, the skill gap now against before, a **Classes** block, per-encounter careful/Random gaps,
   a **Cards** block, **decisive bursts** (one 30+ play without which the win is lost), each boss's roster, meter
   and turn-by-turn room, how every boss win happened, and prior campaign losses against the boss result.
4. **Check that recent changes actually loaded.** Assets edited outside Unity don't apply until Unity reimports
   them. Compare the script's boss rosters, start Opinion and meter (and any card's cost in `card_plays.csv`)
   with what the assets now say. A mismatch means the run tested the old version: report it and ask the user to
   reimport (right-click the folder → Reimport) rather than drawing conclusions about the change.
5. **Dig where the numbers point**, using the CSVs:
   - a card's plays, Opinion per play and room hostility change (`card_plays.csv`), and its asset under
     `Assets/Data/Cards/` to see what it does;
   - an encounter's roster and its enemies' move assets under `Assets/Data/Enemies/` (aggressive moves are the
     damage);
   - a boss's bundles in its `BossData` under `Assets/Data/Bosses/` when one bundle shows ~100%.
   Compare like with like: a card's win-rate swing is confounded by when it gets played (late campaign fights
   and the final boss lose more), and passive cards credit their effects through the Utility table, not their
   own play.

## Classes

Origins are Faith Leader, Nepo Baby and Actor (the Celebrity cards). Compare them only on runs that played all
three on the same seeds. For each, read the script's Classes block:

- **Win rate and its own skill gap.** An origin can be balanced on win rate yet forgiving (Random wins a lot) or
  brittle (only careful play wins). Parity means within ~10 points of each other, each with a healthy gap.
- **Damage taken against output.** Damage taken per battle is defence; Opinion per AP is offence; room hostility
  per play is how much the class riles the room (a riling class takes more damage later). A class losing on
  defence wants shields or calming, not more damage, and the reverse.
- **Worst encounters against the other origins** show the matchup a class can't answer (Denial walls, swarms,
  heavy hitters). Look at its starter deck and pool for the missing answer rather than nerfing the encounter.
- **Campaigns**: completion, Credibility at the end and final deck. Low Credibility with a big deck means the
  class fights a lot and bleeds.
- **Class multipliers** (`NepoBabyConfig` Sway/Composure, `FaithLeaderConfig`) scale everything at once; note
  their current values, because a multiplier change moves every number for that class.
- The starter deck lives on the origin's row in `Assets/Resources/Databases/OriginDatabase.asset`; its mix of
  attacks to shields decides early-fight output.

## Cards

The Cards block measures each card against its class's **Basic attack curve** (the median Opinion per AP of that
class's Basic cards that deal Opinion) and joins the report's Utility table:

- **OVER CURVE** (1.6× the curve or more): fine for a Rare or a card with a real cost (hostility per play, an
  exhaust, a setup condition); a problem for a Basic or a starter, which every run sees. Cross-check decisive
  bursts: an over-curve card that also decides wins on one play is the first to look at.
- **WEAK RARITY**: an Enhanced or Rare below the Basic curve with no utility; players will skip it.
- **NOTHING MEASURED**: no Opinion, Support or energy. Read its hostility per play first (calming cards show a
  negative number and are doing their job), then its asset: draws, statuses, card moves and setup effects aren't
  measured, so open it before calling it dead.
- **UTILITY**: Support that blocked real hits (prevented), energy and the extra plays it bought, and passive
  Opinion. This is how shields, energy cards and persistent Policies are judged.
- **NEVER PLAYED**: not offered or never picked. Remember the reward rules: a card with no upgrade authored is
  never offered (Heckles, Scandals and generated cards exempt); art doesn't gate offers.

Per-play numbers include every context; a persistent Policy is played once, so its numbers are per battle. Open
the card's asset under `Assets/Data/Cards/<class>/` before recommending a change, and name the field to tune.

## Patterns worth knowing

- **One-play wins**: a decisive burst is fine as a rare combo payoff, a problem when a common card does it often
  (Holier Than Thou's flat 30) or a free action feeds it (Last Season counting Nepo Baby's free mulligan).
- **Bosses**: if the room is all hostile by turn 3 and Opinion falls every turn, the audience's aggressive moves
  are winning the fight, not the rival. A boss loss ends the run, so it dominates "What ends runs".
- **Credibility**: a steady slide means losses outpace HQ heals; a flat line at the cap means losses don't bite.
- **Progression**: decks and Rares should grow while the campaign win rate holds; a cliff on day 7 is the boss.

## Write-up

Lead with the answer to what the user asked, then the evidence:

1. One or two sentences: the most important finding.
2. Tables for the numbers that support it: before and after, careful against careless.
3. What looks wrong, ranked by impact, each with its cause (the asset, move or rule) and a fix to consider.
4. Caveats in one short list: sample size, bot behaviour, stale imports.
5. A closing question offering the most valuable next step. Don't build it until the user says so.

Keep it skimmable: lead lines bold, plain words, no wall of numbers. Name the playtest folder you read.
