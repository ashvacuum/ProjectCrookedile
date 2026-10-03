# Unlocks — what a player earns between runs

> **Kind:** Design · **Status:** Canonical · **Updated:** 2026-10-03
>
> **Summary:** What unlocks (classes, cards, allies, events, campaigns), what earns each one, how fast, and how the player sees it. Achievements and unlocks are one system. Everything is unlocked for now.
>
> **Source of truth:** this doc · **Related:** [`meta-progression.md`](meta-progression.md) · [`metagame-campaign.md`](metagame-campaign.md) · [`core-design.md`](core-design.md)

> **For now everything is unlocked** (decided 2026-10-03). Locks are off in code (`UnlockRules.LocksEnabled`); type
> `locks true` in the dev console to play with them on. Conditions and counters still record, so turning locks on
> later loses nothing. This doc is the structure that switches on once the unlock content is authored.

Every number here is a placeholder. The point of this doc is the shape: what is locked, what opens it, and in what
order a new player meets things. The data and save side already exist for cards and are described in
[`meta-progression.md`](meta-progression.md) section 4.

---

## 1. Rules

1. **Unlocks widen, they never power up.** A fresh profile can win. An unlock adds an option (a card in the pool, an
   ally you can meet, an event on the map, a campaign to pick), never a stat bonus.
2. **Earned by playing, and it says why.** No meta-currency and no shop. Every unlock names what earned it.
3. **It arrives next run.** A run keeps the content it started with, so a seed replays the same.
4. **Start small.** The first runs have fewer things in them, so the core (the meter, Hostility, the Echo Chamber,
   your class's verb) is readable. The pool fills as the player shows they understand it.
5. **The city remembers you.** Some events and allies unlock because of something you did in an earlier run.
6. **Nothing is missable.** Every unlock can still be earned later; nothing is lost on a defeat.
7. **Achievements are unlocks.** One system, not two: each feat that unlocks something *is* an achievement, and
   Steam achievements mirror them. No achievement exists just for display.

## 2. What unlocks

| Kind | A new profile has | The rest unlocks by |
|---|---|---|
| **Classes** | Faith Leader | Run milestones (§3) |
| **Cards** | Every card that plays a pole | Pole improvements and meta cards unlock by class feats (below) |
| **Allies** (the relics) | The Basic allies: The Beef, Crowd Control, Opposition File | Meeting them in story events, or feats |
| **Events** | The base campaign pool | Story callbacks from earlier runs |
| **Campaigns** | Campaign 1 (the current seven-day pool) | Reaching day 7 of the one before |

### Classes

Faith Leader is the teaching class: its loop (stack, convert, lead your Fanatics) shows the room and the Echo Chamber
directly. Celebrity is next because its base plays fine without understanding either pole. Nepo Baby, the hardest
class by design, comes last.

### Which cards lock: poles open, meta cards earned

**Every pole is open from the start.** A pole is a class's lane: Faith Leader's convert loop, Nepo Baby's Burn, Return
and Calm, Celebrity's Glamour and IOU. All the cards that let you *play* a pole are in the pool on a fresh profile,
so any build a class is designed around can be tried from its first run.

Only two kinds of card ever lock:

- **Pole improvements:** cards that make a pole you can already play stronger, rather than letting you play it at
  all. The persistent Policy that turbocharges a lane is the typical one (Photo Op for Glamour, Bailout for IOU,
  Executive Privilege for Burn).
- **Meta cards:** the extremely strong cards a run gets built around, usually the Rares (Trust Fund already works
  this way: win a run as Nepo Baby).

Each unlocks through a **class feat**, one card or a small group per feat. Which cards count as improvements or meta
is a per-card call made when locks go on; most of each class stays open.

Class feats count what the class does, so they reward playing it the intended way:

| Class | Example feats |
|---|---|
| Faith Leader | Convert 25 enemies (lifetime) · have 3 Fanatics at once · win a run |
| Nepo Baby | Burn 50 cards (lifetime) · play one card three times in a turn · win a run |
| Celebrity | Reach 10 Glamour in one battle · settle 30 Debt (lifetime) · win a run |

### Allies

Allies are the game's items, so finding a new one should feel like an event, not a counter ticking over.

- **Met in the story:** most Enhanced and Rare allies unlock by an event grant ("the Fixer's cousin owes you one").
  You meet them in a run, and from the next run on they can be recruited.
- **Earned by a feat:** a few unlock from a feat that fits them (Hail Mary: win a battle from below 10% Opinion).

### Events

The base pool is always there. **Callback events** unlock because of an earlier run: help the Fixer once and *The
Favour* can show up in later runs; lose to the Incumbent and a rematch thread opens. This is how the city remembers you
across runs, and it is the main source of new events.

### Campaigns

Campaign 1 is the current seven-day race. Reaching day 7 of it unlocks campaign 2 (a new pool, a new boss, and room for
a longer schedule), and so on. There is no difficulty ladder (no Ascension); not planned for now.

## 3. Pacing: what a new player sees

| Moment | Unlocks |
|---|---|
| First run | Faith Leader, campaign 1, every pole's cards, Basic allies |
| End of the first run (any result) | **Celebrity** |
| Reach day 4 in any run | Two allies |
| 3 runs finished | **Nepo Baby** |
| A class feat | That feat's pole improvement or meta card(s) |
| A story beat | Its callback event or ally, next run |
| Reach day 7 of campaign 1 | **Campaign 2** |
| First win as a class | That class's "win a run" card |

Target: everything in campaign 1 is open after roughly 15–20 runs, and no single run ends without at least one thing
having moved (a counter, a feat, or an unlock).

## 4. How the player sees it

- **End of run:** a "New next run" list, each item with the reason ("Converted your 25th enemy").
- **Collection:** every card, ally, event and campaign. Locked ones show as a silhouette with how to unlock them, so
  there is always a visible next goal.
- **Class select:** a newly unlocked class is marked until it has been played once.
- **In the run:** locked cards and events never appear. An event option that recruits a locked ally shows disabled
  with how to unlock it, the same as any gated option, so the player learns the ally exists.

## 5. Decisions (2026-10-03)

1. **Everything is unlocked for now.** Locks switch on once unlock content is authored and playtested.
2. **Campaign 2 needs day 7 of campaign 1**, not a win.
3. **Achievements and unlocks are the same system** (rule 7).
4. **No difficulty ladder** (no Ascension) for now.

5. **Poles are open from the start; only pole improvements and meta cards lock** (when locks go on).

Still to decide when locks go on: which classes start locked, and which cards count as pole improvements or meta. The
tables above are the proposal.

## 6. What the code needs

Cards already unlock this way. The same condition and grant go onto allies, encounters and campaign pools, plus a
run-start screen (class + campaign) and the end-of-run and collection screens. Details in
[`meta-progression.md`](meta-progression.md) section 4.
