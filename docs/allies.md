# Allies — the run's build-warping layer

> **Kind:** Design · **Status:** Proposal · **Updated:** 2026-11-09
>
> **Summary:** What allies are for, where a run gets them (a starting ally per class, events, elite wins, boss wins), how they're lost, class pools, and a starter roster. Proposal: the sources, loss rules and roster below are not built yet.
>
> **Source of truth:** this doc · **Related:** [`core-design.md`](core-design.md) · [`metagame-campaign.md`](metagame-campaign.md) · [`campaign-encounters.md`](campaign-encounters.md) · [`naming-glossary.md`](naming-glossary.md)

Allies are what the design docs call **relics**; the code name is Ally (`AllyData`, `RunState.Allies`,
`RecruitAllyOutcome`, `HasAlly`; see `naming-glossary.md`). `core-design.md` §9 names them "the real depth layer" and
leaves the design space open. This doc fills it.

---

## 1. What an ally is for

An ally is a person in your campaign's orbit (a fixer, a driver, a pollster, a pastor) who changes **how you play
for the rest of the run**. That is the build-warping layer, not a flat stat bump. "Draw 1 at battle start" is the
weakest kind of ally and most of today's roster is that kind.

Every ally does one of three jobs:

1. **Pays off a setup.** It rewards a plan the player builds: "the first conversion each battle gives that Fanatic
   +1 stack", "your first Policy each battle costs 0". Battles are judged by how well a setup pays off, so allies
   should widen that gap, not shrink it.
2. **Shapes the map.** Travel, encounter duration, Funds, entry windows. Traffic is meant to shape choices
   (`metagame-campaign.md`), and these allies are the counterplay.
3. **A deal with a cost.** A strong upside with a political downside: "+1 energy each turn, but each battle starts
   with one enemy at +2 Hostility". Only boss rewards carry costs (§2).

**Constraints**

- Every number is tunable data on the ally asset, never a constant.
- No ally hands out cheap or repeatable Hostility reduction: reducers are scarce by design.
- The pool is justified by Faith Leader first. Nepo Baby and Celebrity are provisional; their pools follow their
  designs.
- Each ally has an authored description. The generated text is a fallback, not the card.

## 2. Where allies come from

| Source | What you get | Pool | Status |
|---|---|---|---|
| Story events | One named ally, fixed by the event (`RecruitAllyOutcome`) | Hand-wired per event | **Built** |
| Elite win | Pick 1 of 2 | Elite pool for your class, minus allies you hold | Not built |
| Boss win | Pick 1 of 3, on top of the full Credibility restore | Boss pool for your class: a **mix** of pure and costly allies | Not built |

- **Elites** are the hard optional fights: today the Televangelist, the Dynast and Barangay Tanod, marked by
  `BattleEncounterData.IsElite` (next to `IsBoss`; the encounter CSV's `is elite` column sets it).
- **Boss offers mix pure and costly allies.** Of the three offered, at least one is pure and at least one carries a
  cost when the pool allows it, so the pick is a real decision rather than an obvious one.
- Offers never repeat an ally the run holds. An empty pool offers fewer choices, never a duplicate.
- Story allies stay hand-wired: they're narrative, and the event's text names them.
- Ordinary fights keep rewarding cards only.

## 3. Losing allies

Any ally can be lost. Politics runs on loyalty, and losing a friend should hurt the way a Turncoat does in battle:
painful, memorable, recoverable.

- **Event outcomes** can take an ally away: a named one ("Sell out your source" loses Anonymous Tip) or a random
  one. This needs a `LoseAllyOutcome` next to `RecruitAllyOutcome`.
- **A run-wide rule** may also take one. Candidate: a crushing loss (Opinion collapsed to 0) makes your most recently
  recruited ally walk. Open call, §6.
- A lost ally returns to its pool, so a run can meet them again.
- The map and the run summary say who left and why.

## 4. Class pools

Pools are **mostly class-specific**. Each ally carries an origin tag, the way class cards do; an untagged ally is
shared. Roughly two thirds of each class's elite and boss pools are its own, plus a small shared set (map and economy
allies, mostly).

### 4.1 Starting allies (built)

Each class starts the run with one **starting ally**, its innate passive, the way Slay the Spire's starter passive
is a relic. The old origin passives were converted in place (2026-11-06). One system holds every persistent passive: it shows in the same ally bar, follows the
same save path, and a boss ally can be designed to upgrade or replace it.

| Class | Starting ally | Note |
|---|---|---|
| Faith Leader | The Deacon (+1 energy on your first turn) | |
| Nepo Baby | Personal Assistant (redraw the opening hand) | |
| Celebrity | Publicist (every enemy starts Exposed: the first hit on each deals double) | Charm lowers the room's defenses on entrance |

`OriginDatabase` names each class's `StartingAlly`; `RunState.Create` recruits it, so it sits in `RunState.Allies`
with every other ally and saves the same way. A battle with no run (the playtest bot's standalone fights) uses the
origin's starting ally; `BattleManager.ConfigureForSimulation` can pass other allies or none (play-mode tests pass
none). The ally assets live in `Assets/Data/Allies/` and kept their old asset GUIDs. A run saved before the change
resumes without its starting ally.

## 5. Starter roster

Faith Leader first. Names and numbers are placeholders to tune.

### Faith Leader — elite pool

| Ally | Effect | Job |
|---|---|---|
| Choir Director | Each conversion gives that Fanatic +1 stack, so it follows a turn longer | Setup payoff |
| Prayer Circle | At battle start, the most hostile enemy gains 1 Guilt | Setup payoff |
| Confessor | Applying Guilt to a hostile enemy gains you 1 Support | Setup payoff |
| Altar Server | Your first Policy each battle costs 0 | Setup payoff |

### Faith Leader — boss pool (mixed)

| Ally | Effect | Cost |
|---|---|---|
| Megachurch Board | The conversion Policy starts in play every battle | Your Guilt cards cost +1 |
| Firebrand Preacher | Fanatics push +3 | A disillusioned Fanatic goes to +3 Hostility instead of neutral |
| Revival Tent | The first enemy you convert each battle needs no conversion Policy | none |
| Tithe Collector | +Funds per conversion, paid at day end | none |

Revival Tent deliberately bends the rule that conversion happens only while a conversion Policy is in play. It is a
boss reward, so the exception is earned; drop it if the rule should hold everywhere.

### Celebrity

| Ally | Pool | Effect | Job |
|---|---|---|---|
| Paparazzi | Elite | Battle start: +3 Glamour | Setup payoff: the Glamour engine starts warm |
| Manager | Elite | Battle start: draw 1 | Setup payoff: a wider opening hand |

Both compose from existing blocks: `BattleStartTrigger` with `GainGlamourEffect` or `DrawCardsEffect`.

### Shared pool

| Ally | Pool | Effect | Cost |
|---|---|---|---|
| The Driver | Elite | −25% travel time | none |
| Chief of Staff | Elite | −25% encounter time | none |
| Campaign Manager | Boss | Card rewards show 4 choices instead of 3 | none |
| Party Machine | Boss | +1 energy each turn | Each battle starts with one random enemy at +2 Hostility |
| Dark Money Donor | Boss | +100 Funds now | −10 max Credibility |
| Spin Doctor (built) | Unassigned | HQ's heal restores 20% of max Credibility instead of 10% | none |
| Speechwriter (built) | Unassigned | Once per battle, at your turn start, discard any number of cards and draw that many | none |

### Existing story allies

These keep their events and gain a class tag.

| Ally | Effect today | Granted by |
|---|---|---|
| Anonymous Tip | Battle start: draw + Support | Leaked Group Chat, Whistleblower |
| Burner Phone | Battle start: draw | Leaked Group Chat |
| Opposition File | Battle start: draw | Data Leak Offer |
| Crowd Control | Opinion when an enemy turns receptive | Endorsement Offer |
| The Beef | Support on Hostility changes | Endorsement Offer |
| Fixer's Cousin | Makes a card free every Nth play | Old Friend, New Ask |
| Hail Mary | Opinion boost while you're behind | Old Friend, New Ask |
| Devil's Advocate | Riles the room when enemies turn receptive | **nothing** (needs an event) |
| The Driver | −25% travel time | **nothing** (moves to the shared elite pool) |

Demo target: about 15–18 allies across the pools.

## 6. Open calls

1. **Run-wide loss rule:** does a crushing loss take your newest ally, or do only event outcomes take allies?
2. **Fanatic tweaks** (Choir Director, Firebrand Preacher) depend on the Fanatic numbers still open in
   `needs-detailing.md` §1.
3. **Revival Tent:** keep the conversion-without-Policy exception, or not?
4. **Elite list:** which encounters are elites, beyond the three named above?
5. **Can a starting ally be lost?** Proposed: exempt from random loss, but a boss ally may replace it by name (the
   Slay the Spire starter-relic upgrade).

## 7. What building it takes

- **Built:** `AllyData.Pool` (Starting, Story, Elite, Boss), `AllyData.Origin` (null = shared) and `HasCost`, which the
  boss offer reads; `BattleEncounterData.IsElite`. Story allies await their class tags, and Spin Doctor and
  Speechwriter sit in Story until they get a source.
- An ally pick after elite and boss wins, reusing the card-pick screen; the bot harness mirrors it.
- `LoseAllyOutcome` (named or random); saves already store allies by ID, so a removal needs no schema change.
- New effects only where the building blocks fall short: "a Policy starts in play", "card rewards show +N", the two
  Fanatic tweaks. Every other ally above composes from existing triggers, conditions and effects.
- **Built:** the Database window's Allies tab shows pool, class and cost columns, filters by pool, and warns on an
  empty authored description.
