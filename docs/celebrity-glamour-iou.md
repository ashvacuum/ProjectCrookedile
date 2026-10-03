# Celebrity: Glamour / IOU

> **Kind:** Design · **Status:** Built · **Updated:** 2026-10-03
>
> **Summary:** The Celebrity class: Glamour and IOU poles with Soundbite tokens between them, all its cards and rules. Numbers are placeholders.
>
> **Source of truth:** this doc; built in [`Data/Cards/Celebrity/GlamourIou/`](../Assets/Data/Cards/Celebrity/GlamourIou/) and [`GlamourIou.cs`](../Assets/Scripts/Gameplay/Battle/Celebrity/GlamourIou.cs) · **Related:** [`core-design.md`](core-design.md) · [`needs-detailing.md`](needs-detailing.md)

This is the Celebrity (canonical since 2026-10-03; it replaced the Attention / Scandal / Drama King design summarised
in `core-design.md` §7). All numbers are placeholders and live on the card assets; shared engine rules are in `CelebrityRules` (`Assets/Scripts/Gameplay/Battle/Celebrity/GlamourIou.cs`).

Vocabulary: **Composure** = Support (the shield). **Sway** = a push on the Opinion meter. Statuses, not keywords.

## Fantasy
A guy who knows nothing, talks the talk, and never walks the walk. Every promise is empty and it is all for the press.
He does not convert enemies. He wins by spectacle and borrowed momentum.

## Structure (Silent-style: base + poles + connector)
- **Base:** plain, defensive, good alone. Playable with no pole online.
- **Glamour pole:** slow, defensive, long-fight engine.
- **IOU pole:** fast, aggressive overcharge engine (borrow now, pay next turn).
- **Connector: Soundbite tokens.** Cheap generated cards that glue the poles together.

Guardrails: nothing stacks on enemies and nothing moves Hostility toward Receptive (Faith Leader's lane; v0.2 below proposes Pacifier cards); no fetch,
tutor, search or choose-from-pile effects and no redirect or reflect (Nepo Baby's lane); no new keywords; no class
passive designed for this build. Persistent Policies declare the build, one-shot Policies are tactical.

## v0.2 direction (proposal, 2026-10-03)

**Why:** the built set is defense-heavy. 9 of 25 cards produce Composure and only 4 push the meter; Glamour is
mostly built *from* Composure and spent *on* Composure; IOU has one Borrow card and six cards that only cushion
Debt. The class defends instead of playing its engines.

**The loop:** *Borrow* (or plain energy) pays for a *Headliner*, a high-cost card that dumps a lot of Glamour.
Glamour then does damage three ways: it lifts the meter every round (as now), it makes every Sway hit harder
(*Star Power*), and *Cash-outs* consume it for a burst. Composure is the Celebrity's willpower: it absorbs hits so
Scrutiny can't strip the Glamour. The Debt bill lands next turn; Debt payoffs turn the bill into more Glamour or Sway.

**The room:** a real mix of *Aggressors* and *Pacifiers*. Aggressors are controversy: they raise Hostility and
earn Glamour ("all publicity is good publicity"). Pacifiers are charm: they lower Hostility and scale with Glamour.
Faith Leader still owns stacking statuses on enemies and conversion; Celebrity never does either.

### Rules added

1. **Star Power:** every Sway you deal gets +1 per 3 Glamour (`GlamourStatus.ModifyOutgoingOpinion`, number in
   `CelebrityRules`). Glamour is offense first.
2. **Encore round:** some cards make Glamour tick twice this round (new `CelebrityState` flag).
3. Scrutiny, Debt settlement and Soundbites stay as they are. Composure protecting Glamour is the point of defense.

### Card roles (target mix for the 31 cards)

| Role | Cards | What it does |
|---|---|---|
| Headliner (high cost) | Red Carpet *(new)*, Sold-Out Show *(new)*, Trending, Press Release | 2–4 energy: big Glamour or doubling. The reason to Borrow. |
| Cash-out | Standing Ovation, Spotlight *(new)*, Mic Drop *(new)* | Spend Glamour as Sway |
| Amplifier | Photo Op, Going Viral | More Glamour, faster |
| Aggressor | Hot Take, Roast *(new)*, Calling It In | Sway, raise Hostility, earn Glamour |
| Pacifier | Autograph, Meet and Greet *(new)*, Fan Mail | Lower Hostility, stronger with Glamour |
| Willpower | No Comment, Smile and Wave, Thumbs Up, Behind the Podium, Prepared Remarks, Media Training *(new)* | Composure that guards Glamour |
| Borrow | Cash Advance, Fine Print, Overdraft, Open Tab, Line of Credit | Energy now, Debt next turn |
| Debt payoff / insurance | Campaign Donors, Settle Up, Calling It In, Too Big to Fail, Rain Check, Bailout | Turn the bill into Glamour or Sway; soften it |

Roughly 12 cards push the meter or spend Glamour on it, 6 defend, 5 manage the room, 9 run the IOU engine.

### Card list (numbers are placeholders)

**Starter (10):** 3 **Hot Take** (Pressure 1: 6 Sway, +1 target Hostility, +1 Glamour) · 2 **Autograph** (Pressure
1: −2 target Hostility, +1 Glamour) · 3 **No Comment** (Pressure 1: 5 Composure) · 1 **Smile and Wave** (Rhetoric 1:
4 Composure, 3 Glamour) · 1 **Cash Advance** (Rhetoric 1, Borrow: +2 energy, +2 Debt). One seed per engine, two
room tools, three shields.

| Card | Change | Type / rarity / cost | Effect |
|---|---|---|---|
| Red Carpet | new | Rhetoric enhanced 3 | Gain 6 Glamour |
| Sold-Out Show | new | Rhetoric rare 4 | 10 Sway. Double your Glamour |
| Trending | rework (cost 1 → 3) | Rhetoric rare 3, exhaust | Double your Glamour |
| Press Release | rework | Rhetoric enhanced 2 | 4 Glamour, 4 Composure (build or protect, either way) |
| Standing Ovation | keep | Rhetoric enhanced 2 | Sway equal to Glamour |
| Spotlight | new | Rhetoric enhanced 1 | Consume all Glamour: Sway equal to twice what you consumed |
| Mic Drop | new | Rhetoric rare 2, exhaust | Consume half your Glamour: that much Sway, and −1 Hostility on every enemy |
| Photo Op | rework | Policy enhanced 1, persistent | Whenever you play a Rhetoric card, gain 1 Glamour |
| Going Viral | rework | Rhetoric rare 1 | Glamour ticks twice this round |
| Roast | new | Pressure enhanced 1 | 8 Sway, +2 target Hostility, +2 Glamour |
| Calling It In | keep | Rhetoric enhanced 1 | Sway equal to 4 × Debt |
| Meet and Greet | new | Pressure enhanced 2 | −1 Hostility on every enemy, −1 more per 5 Glamour |
| Fan Mail | rework | Rhetoric enhanced 1 | Draw 1 (2 at 5+ Glamour), −1 target Hostility |
| Thumbs Up | keep | Rhetoric basic 0 | 3 Composure, 1 Glamour |
| Behind the Podium | keep | Rhetoric enhanced 1 | Composure equal to Glamour |
| Prepared Remarks | keep | Rhetoric basic 1 | 4 Composure, add 2 Soundbites |
| Media Training | new | Policy enhanced 2, persistent | Scrutiny only strips Glamour from hits that leak 5+ |
| Fine Print | rework | Policy basic 0, Borrow | +1 energy, +1 Debt, draw 1 |
| Overdraft, Open Tab, Line of Credit | keep | | Borrow support |
| Campaign Donors | rework | Policy enhanced 1 | Gain Glamour equal to Debt gained this turn |
| Settle Up | rework | Rhetoric enhanced 1, exhaust | Cancel all Debt; gain 1 Glamour per Debt cancelled |
| Too Big to Fail, Rain Check, Bailout | keep | | Debt insurance |
| Soundbite (token) | rework | Rhetoric 0, exhaust | 2 Sway, +1 Glamour |

### What it takes to build

- **Data only (existing effects):** every Sway, Hostility, Composure, Glamour gain, Glamour doubling (gain scaled by
  current Glamour), Borrow and draw change above; the starter deck; the new Red Carpet, Sold-Out Show, Roast and
  Meet and Greet.
- **Small code:** Star Power (one override on `GlamourStatus`), a consume-Glamour effect (all or half, × multiplier)
  for Spotlight and Mic Drop, the Encore-round flag for Going Viral, the Media Training Scrutiny threshold, and
  Settle Up's "Glamour instead of Composure" option on `ForgiveDebtEffect`.
- **Then:** the Play Mode card smoke test covers the new cards; a playtest-bot run compares Celebrity win rates
  before and after (the bots don't value Glamour yet, so a human pass matters most).

## Rules
**Glamour** (player status, stacking, never decays on its own)
1. Gained from card effects (`GainGlamourEffect`).
2. After all enemy actions, Opinion rises by the stack count, then 1 stack is removed (`BattleManager.TickGlamour`).
3. Scrutiny: each Opinion hit that leaks past Composure strips 1 Glamour, flat. A fully blocked hit strips nothing.
4. Enemy hits resolve before the tick. Unpaid-Debt damage is not a hit.

**Debt** (player number, `CelebrityState`)
1. Gained from Borrow cards.
2. Settled at the start of the player's next turn, after energy refreshes: Debt reduces energy (floor 0), the remainder
   becomes Opinion damage 1:1. Debt resets to 0.
3. Default: unpaid-Debt damage ignores Composure (`CelebrityRules.UnpaidDebtBlockable`).
4. Debt outstanding when the fight ends is never collected.

**Soundbite** (token): cost 0, Exhaust, Sway 2, generated only (Rhetoric).

## Starter deck (10)
3 Hot Take (Pressure, 1: Sway 6), 3 No Comment (Pressure, 1: 5 Composure), 3 Autograph (Pressure, 1: -2 Hostility),
1 **Smile and Wave** (Rhetoric, 1: 5 Composure + 3 Glamour; upgraded 7 + 4).

## Cards
| Pole | Card | Type / rarity / cost | Effect |
|---|---|---|---|
| Glamour | Thumbs Up | Rhetoric basic 0 | 3 Composure, 1 Glamour |
| Glamour | Press Release | Rhetoric basic 1 | 6 Composure, 2 Glamour |
| Glamour | Prepared Remarks | Rhetoric basic 1 | 4 Composure, add 2 Soundbites |
| Glamour | **Photo Op** | Policy enhanced 1 | Persistent: whenever you gain Composure, gain 1 Glamour |
| Glamour | Behind the Podium | Rhetoric enhanced 1 | Composure equal to Glamour |
| Glamour | Trending | Rhetoric rare 1, exhaust | Double Glamour |
| Glamour | Going Viral | Rhetoric rare 1 | Glamour equal to half current Composure |
| Glamour | Standing Ovation | Rhetoric enhanced 2 | Sway equal to Glamour |
| Glamour | Fan Mail | Rhetoric enhanced 1 | Draw 1; draw 1 more at 5+ Glamour |
| IOU | Cash Advance (Borrow) | Rhetoric basic 1, tag `borrow` | +2 energy, +2 Debt, add 2 Soundbites |
| IOU | Calling It In (Leverage) | Rhetoric enhanced 1 | Sway = 4 x Debt |
| IOU | Settle Up (Forgive) | Rhetoric enhanced 1, exhaust | Cancel all Debt, 2 Composure per Debt |
| IOU | **Line of Credit** | Policy enhanced 1 | Persistent: first Debt gain each turn is 1 less |
| IOU | **Open Tab** | Policy enhanced 1 | Persistent: first Borrow card each turn costs 0 |
| IOU | **Bailout** | Policy rare 2 | Persistent: unpaid-Debt damage adds that many Soundbites (cap 3/turn) |
| IOU | Too Big to Fail | Policy enhanced 2, exhaust | First unpaid-Debt damage this fight deals none |
| IOU | Campaign Donors | Policy enhanced 1 | Composure per Debt gained this turn |
| IOU | Overdraft | Policy basic 1, exhaust | This turn Borrow gives double energy and double Debt |
| IOU | Rain Check | Policy basic 1, exhaust | Delay Debt settlement one turn |
| IOU | Fine Print | Policy basic 0, exhaust | Composure equal to current Debt |

Rarities and costs not given in the original spec (Trending, Going Viral, Standing Ovation, Overdraft, Rain Check,
Fine Print, Calling It In, Settle Up, Cash Advance) are my placeholders.

## Open questions (default implemented)
1. Which Policies persist vs one-shot: assignment above is a proposal. Persistence is a battle flag set when the Policy
   is played, so it lasts the fight regardless of where the card goes.
2. Unpaid-Debt damage: unblockable (flip `UnpaidDebtBlockable`).
3. Soundbite card type: Rhetoric.
4. Photo Op: enhanced, cost 1.
5. Class passive: none designed for this build. The `Actor` origin currently points at `Daddy's Gifts.asset`, a
   once-per-battle mulligan that duplicates Nepo Baby's — a placeholder to replace, not a design.

## Authoring and testing
- The cards live in `Assets/Data/Cards/Celebrity/GlamourIou/` and are tuned on the assets (the one-shot builder that
  first wrote them is gone). The starter deck is authored in `OriginDatabase`. `Tests/EditMode/CelebrityDebtTests`
  covers settlement, Line of Credit, Rain Check and the waiver.
- Bot run: use the existing playtest runner with origin `Actor`. Snowball combos to read first: Photo Op + Open Tab +
  Campaign Donors; Too Big to Fail + Bailout; Overdraft + Rain Check; Trending on a Photo Op deck.
- Known gaps: no Glamour/Debt HUD, no status icon, cards have no artwork (so they read as in-development),
  and the bots do not value Glamour or Debt (they rank cards by Opinion preview).
