# Crookedile — Core Design Doc

> **Kind:** Design · **Status:** Canonical · **Updated:** 2026-10-04
>
> **Summary:** The combat model: Opinion Meter, hostility, the Echo Chamber rule, voice intents, turn structure, the three archetypes, their starter decks and reward-pool directions.
>
> **Source of truth:** this doc · **Related:** [`needs-detailing.md`](needs-detailing.md) · [`naming-glossary.md`](naming-glossary.md) · [`enemy-design-bible.md`](enemy-design-bible.md)

*A Filipino political roguelite deckbuilder. Working title: **Crookedile** (formerly Palakasan).*

> You are not winning a fight. You are working a crowd — managing who speaks, how loudly, and in what direction they push public opinion.

---

## 1. Core Fantasy

You stand before a room and manage the conversation. The goal is never to defeat or eliminate enemies — it's to control their **voice** so the public ends up on your side. Crucially, you never want the room fully agreeable (an echo chamber) and you never want it fully against you. The sweet spot is **controlled tension**: keep at least one villain present so you look brave for engaging them, while the crowd stays behind you.

**The central decision every encounter:** who is my villain today, and what do I do with everyone else?

---

## 2. The Opinion Meter & Shields

The **opinion meter** is public sentiment — the actual battleground. It is *not* a "push as high as possible" resource; it's a balance you manage.

There are **no enemy HP pools**. Shields exist purely to guard the opinion meter, and they are **directional**:

- **Upward shield** — enemies raise this to block the meter from rising in your favor. You must break through to gain sentiment.
- **Downward shield** — you raise this to protect your current standing from being pushed down.

The meter is **live** — it adjusts in real time during both the player action phase and the enemy action phase, so every card play and every enemy intent visibly moves it.

---

## 3. Hostility

Hostility is the heart of the game. It exists as both:

1. **An enemy stance** — enemies start outwardly hostile, neutral, or meek/receptive.
2. **A card element** — cards can seed, manage, redirect, or amplify hostility deliberately.

**Hostility sets behaviour, never numbers (ruling 2026-09-29).** An enemy's hostility decides its stance, and the stance decides which of its authored move lists it draws from (hostile / neutral / receptive). A hostile enemy hits harder only because its hostile moves are authored harder — hostility is never a damage multiplier. (An earlier `1 + 0.5 × hostility` multiplier on enemy pushes was removed: it turned every riling card into a compounding damage spiral.)

Hostile enemies are **not purely a problem** — they are a resource. You want them present (see Echo Chamber). Turning an enemy hostile grants a card draw. **Receptive enemies pay in Support (2026-09-29):** +2 Support the moment an enemy turns receptive, and +1 Support per receptive enemy at the start of each of your turns. Hostile = more cards; receptive = more cushion — and an all-receptive room still triggers the echo chamber.

### Locked-state statuses
- **Hardened** — cannot be turned receptive. The permanent villain.
- **Fanatic** — cannot be turned hostile. The permanent loyalist.

Both are **statuses** (can be applied and potentially removed), not fixed enemy types. Each archetype relates differently: Faith Leader is frustrated by Hardened (conversion kit fails), Celebrity loves Hardened (free permanent villain), Nepo Baby barely notices it (his calming stops at Neutral anyway, so Hardened only blocks reducers that convert incidentally).

### Status list (working)
Guilt · Shame · Doubt · Silence · Devotion · Jaded · Hardened · Fanatic · Turncoat · Glamour (Celebrity, on the player)

*Faith Leader's core statuses:* **Guilt** (weakens enemy push), **Shame** (drops enemy shield), **Doubt** (soft reluctance to act) are *pacify statuses* (count toward conversion, consumed on convert). **Jaded** is a *threshold status* (permanent, stacks, raises pacify cost, never consumed). See §7 Faith Leader.

---

## 4. The Echo Chamber Rule *(LOCKED)*

> **If all enemies in the row are receptive, you are in an echo chamber: opinion meter increases are halved, AND the meter decays each turn until the chamber is broken.**

- **Halved gains** punishes the player still climbing — converting everyone stops being efficient.
- **Decay** punishes the player already ahead — you can't turtle on a won board; your lead bleeds.

Together, converting the *whole* room is a mistake at every stage. You must always leave someone non-receptive.

**Counterplay:** Every archetype's starting deck includes a default **hostility card** (an echo-chamber escape valve), so no one can be locked out. The chamber should be **breakable on the same turn it's noticed** — playing a seed-hostility card immediately stops the decay rather than eating a forced turn of bleed.

*(Open: whether the default hostility card should be un-removable so deck-thinning can't re-create the trap.)*

---

## 5. Enemies & Voice Intents

### Boss debates (2026-10-04)

For tree authoring, task settings, cooldown examples and runtime debugging, see [Boss Brain — Authoring and Runtime](boss-brain.md).

A rival candidate is a separate entity above the audience row. The rival has no HP, cannot convert, and is not a player card target. The rival is always adversarial but does not contribute to Echo Chamber, hostile bonus draws, audience counts, Fanatic counts, wide audience effects, or adjacency. Cards still work against a populated audience; the rival manipulates that same audience and the shared Opinion Meter.

At each player-turn start, Behaviour Designer selects one named bundle containing **two or three moves**. All moves reveal together in numbered execution order. The bundle stays committed throughout card play; randomly selected hostile/receptive audience targets also reveal and remain committed even if their stance changes. A target absent at reveal stays absent for that move. Group effects resolve against the live audience.

The opponent phase resolves the rival's moves in their displayed order, then audience modifier intents, then audience direct intents. Reaching full Opinion wins immediately; reaching zero loses immediately. Timed Judgment uses the existing majority rule, after the final opponent response in a boss debate. Standard fights retain their existing Judgment timing.

The planning tree runs only during intent declaration and stops before player input. Its conditions can inspect Opinion, audience stance counts, player turn, Support and Denial. Bundle cooldowns prevent repetitive plans; a valid two/three-move bundle with no cooldown is the fallback. Effects use the existing move/effect system. Player delayed-effect wrappers are unsupported for rival moves; represent later threats with future planning bundles.

Author through **Crookedile → Database → Encounters**: campaign scheduling/rewards on the encounter, audience and turn/Opinion limits in its inline Battle Session, rival/tree/bundles in the round's inline Boss asset, and effects in the bundle's inline move assets. An assigned rival makes the scenario read **Boss Debate**; an empty rival makes it **Standard Fight**. The campaign Boss flag must agree with that assignment and is checked by Database audits. Shared boss/move assets affect every referring encounter; use the Bosses tab's duplicate action for an independent boss, tree and move set, then assign the variant to the desired round.

The prototype encounter is `Assets/Data/Bosses/Debate Prototype/Debate Prototype.asset`. Its session contains four audience members and an eight-turn limit. The rival chooses defensive pressure at 75% Opinion, audience disruption with at least two receptive members, and otherwise an opening bundle. Use the Encounters preview's selected-encounter bot playtest with an origin and seed to iterate.

Enemies have **no HP**. Instead they have:
- **Voice** — their revealed intent for the turn (what they plan to do).
- **Hostility** — the direction of their influence.
- **Statuses** — guilt, shame, silence, devotion, etc., which modify their voice.

### Intents *(LOCKED: revealed)*
Intents are **revealed and visible from the start of the turn** so the player can assess the room before acting. Working set of intents:
- **Rally** — boost hostility of adjacent enemies
- **Rebuke** — place upward shield on the meter
- **Sway** — try to convert a receptive enemy to hostile
- **Condemn** — heavy downward push on the meter
- **Murmur** — low impact, maintains presence

### Row & adjacency
Enemies sit in a **single static row** (no positioning/movement — moving people around breaks the rally-crowd fantasy). Adjacency still matters: rally/influence effects ripple to the **closest** enemies first. Instead of moving people, cards manipulate **targeting patterns** and **reach**.

### Card targeting patterns
- **Single target** — precise, low cost
- **Adjacent** — target + neighbors
- **All hostile** — address the dissenters
- **All receptive** — rally supporters
- **Whole row** — full crowd address; big swing, higher cost, less control

### Variety
Need enough enemies that encounters feel distinct — think StS scale (~60-70 total), not hundreds. Variety comes from combining **hostility stance** × **intent pattern** (aggressive / defensive / disruptive / passive-amplifier). Prototype target: ~6-8 enemies covering different role combos.

### Receptive → Hostile turn (Turncoat)
When a receptive enemy flips hostile, it should **cascade**, not just flip a stat:
- Their next intent becomes aggressive (Rebuke/Condemn)
- Adjacent enemies get a hostility nudge (betrayal is contagious)
- Any ally buff they gave you drops
- Small opinion meter hit — the crowd noticed the betrayal
- **Turncoat** status: freshly-turned enemies hit harder than a natural hostile for a turn or two (they knew your strategy)

Losing a receptive ally should *hurt* — painful, memorable, but recoverable. Political betrayal, not run-ending.

---

## 6. Turn Structure *(LOCKED)*

1. **Start of turn** — all enemy intents visible; player assesses the room.
2. **Player action phase** — 3 energy; play cards in any order (order matters, esp. Faith Leader); meter is **live** and reacts as you play. Silencing an enemy removes its intent immediately.
3. **Enemy action phase** — remaining intents execute; meter stays **live**. Modifier intents (Rally/Sway) resolve **first**, then direct intents (Condemn/Rebuke) **left to right**.
4. **End of turn** — statuses tick/compound; new intents revealed; viral-moment check.

Energy is **3 per turn**.

---

## 7. The Three Archetypes

Each asks a different question every turn and lives at a different point in time.

| Archetype | Question | Time | Can uniquely... | Fears... |
|---|---|---|---|---|
| **Nepo Baby** | "What do I burn to get it now?" | **Now** | Consume his own deck: burn, pull, replay, retrieve | The room's anger (Hostility clock) and junk rotting a thinned deck |
| **Celebrity** | "What can I promise now and pay for later?" | **Borrowed time** | Build Glamour that lifts the meter every round, and borrow energy against next turn (Debt) | Hits that leak past Composure (Scrutiny strips Glamour) and the bill coming due (unpaid Debt hits the meter) |
| **Faith Leader** | "Who can I pacify into a follower?" | **After** | Stack statuses to convert enemies into Fanatics who follow your attacks and defends for a turn | Disruption before reaching 3 stacks; a Hardened room (can't pacify) |

> A distinctiveness test: each archetype must have a **unique capability** AND a **unique fear**. Overlapping fears are what make archetypes feel samey. Watch especially that Celebrity fears *self-overreach* while Faith Leader fears *opponent disruption* — if Celebrity's risk becomes "opponent breaks my setup," they've merged.

### Nepo Baby — *the glass cannon*
> **Full spec: `nepo-baby-class.md`** (cards, config, open questions, conflicts with current code).

Someone who has never been told no. He spends the family's resources (his own deck) to get exactly what he wants, and the whole room pays the bill (Hostility). Best-in-class damage and access, thin defense, a clock set by the room's anger. **The hardest class and the most mechanically different: his resource is the deck itself, not a status.** Celebrity *creates* cards; Nepo *consumes* them.

**Three lanes plus a valve package**, each lane with its own verb and its own brake:
- **Burn** (exhaust from hand as a cost): the high-ceiling engine. His cards are mostly cost 2-3, so burning one is a big swing. Thins the deck, so it is the lane Heckle/Scandal rot punishes.
- **Return** (cards come back to hand): powerful but sparse (at most ~15% of the pool). Brake: in-turn cost increase.
- **Calm** (Hostility control): small hits, steady Hostility reduction, payoffs for a room with no Hostile enemies. Wins elites and bosses by outlasting.
- **Valves:** scry, skip, cycle, redirect. Every valve costs something (Hostility, energy or deck cleanliness).

**Cost model:** burning is paid in cards; pulling and scanning is paid in Hostility. Hostility is a **pure price and clock**, never rewarded (rewarding it is Faith Leader's lane).

**Guardrails:** no stacking statuses on enemies and no deep conversion (calming caps at Neutral; never rewarded for all-Receptive). No token generators or "add X cards to hand" engines (Celebrity's lane). **Policies can never be burned.** No player-side summoning.

**Fear:** a thinned deck rotting with Heckles and Scandals while an angry room hits through thin shields.

### Celebrity — *the empty promise* (Glamour / IOU)
> **Full spec: [`celebrity-glamour-iou.md`](celebrity-glamour-iou.md)** (rules, all cards, open questions). Canonical
> since 2026-10-03.

A guy who knows nothing, talks the talk, and never walks the walk. Every promise is empty and it is all for the press.
He does not convert enemies. He wins by spectacle and borrowed momentum.

- **Glamour** (a status on the player): after the enemy acts, the meter rises by your Glamour, then Glamour drops by 1.
  **Scrutiny:** every hit that leaks past Composure strips 1 Glamour. The slow, defensive, long-fight engine.
- **IOU / Debt:** Borrow cards give energy now and Debt that is settled at the start of your next turn, out of that
  turn's energy first and the meter second. The fast, aggressive overcharge engine.
- **Soundbites:** cheap generated token cards that glue the two together.
- **Structure:** a plain defensive base that works alone, plus the two poles; persistent Policies declare the build,
  one-shot Policies are tactical.
- **Guardrails:** nothing stacks on enemies and nothing moves Hostility toward Receptive (Faith Leader's lane); no
  fetch, tutor, search or redirect (Nepo Baby's lane).

**Design history:** the open-canvas Celebrity (draft into Attention / Scandal / Drama King) was replaced by this build
on 2026-10-03. An earlier "Credibility" resource was cut before that as over-engineered. Glamour and Debt are a
status and a number that live on cards, not a separate class meter.

### Faith Leader — *the converter* (status specialist)
**Spine (as concrete as Nepo Baby's burn):**

> **Stack statuses (Guilt / Shame / Doubt, any mix) on an enemy to the pacify threshold. This consumes the statuses and converts them. Normal enemy → becomes a Fanatic, a loyal follower that copies your attacks and defends for as long as the Fanatic buff lasts, then is disillusioned and reverts to neutral. Hardened enemy → silenced instead (can't be converted, but can be shut up).**
>
> **Pacify threshold = 3 + the enemy's Jaded stacks.** Each time an enemy is fanaticized they gain a stack of **Jaded** (permanent for the fight, never consumed), raising their future conversion cost by 1. So a fresh enemy converts at 3; a once-burned backslider at 4; twice at 5; etc. **This is the anti-milking brake** — re-converting the same person hits diminishing returns, pushing you to win *new* converts rather than farm one target. On-fantasy: a believer who's already lapsed is harder to inspire again.

**Two status categories (keep distinct):**
- **Pacify statuses** (Guilt / Shame / Doubt) — *you* apply them, they count toward the threshold, **consumed on conversion**.
- **Threshold status** (Jaded) — applied *automatically* on each fanaticization, **modifies** the requirement, **permanent & never consumed**, does **NOT** count toward its own threshold. Stacks. Visible on the enemy so the player reads conversion cost directly off the Jaded count.

**The status kit** — each status blunts one specific enemy behavior *while* counting toward the pacify threshold (so the climb is self-protecting):

| Status | Blunts | Note |
|---|---|---|
| **Guilt** (≈Weakened) | Enemy's *push* on the meter (offense) | softens their Condemn |
| **Shame** | Enemy's *shielding* of the meter (Rebuke/defense) | they can't defend opinion |
| **Doubt** | Enemy's *willingness to act* (soft, partial reluctance) | distinct from Preach's hard guaranteed silence |

All three count equally toward conversion: **any 3 = pacify.**

**Lifecycle:** stack to threshold (3 + Jaded) → consume pacify statuses → the enemy becomes a **Fanatic**, your loyal follower, for as long as the Fanatic buff lasts → when it runs out the follower is **disillusioned**: it **reverts to neutral** + gains a **Jaded** stack. No permanent emitters. The class is **relentlessly active** — every turn is spent either stacking toward the next conversion or using the followers you have. Kills auto-pilot; you can never coast on a built board, and you can't infinitely milk one target (Jaded escalation).

#### Fanatics follow your lead *(ruled 2026-10-03)*

A Fanatic is not a one-off burst. It is a follower that copies what you do, and cards can order it around. Conversion
itself pays nothing; the value is in what the Fanatic does while it lasts. *All numbers are placeholders for the
config.*

- **Duration:** a follower for **as long as the Fanatic buff lasts**. The buff counts down each turn (placeholder
  length: through the end of your next turn, so a Fanatic converted by your last play still follows a full turn), and
  cards can extend it. When it runs out the follower is **disillusioned**: it reverts to neutral and gains Jaded.
- **It follows your plays.** Each time you play a card:
  - that pushes the meter (an **attack**), every Fanatic also pushes: **+2 Opinion each**;
  - that gains Support (a **defend**), every Fanatic adds to it: **+2 Support each**;
  - that does both, both happen.
- **If you do neither, they shout the room down.** At the end of your turn, if you played no attack and no defend card
  and converted **2 or more** enemies this turn, each Fanatic **Silences** one non-Fanatic enemy (most hostile first)
  for its next action.
- **Command cards force it** whatever you played: *attack now* (each Fanatic pushes), *defend now* (each Fanatic adds
  Support), *silence now* (each Fanatic Silences an enemy, no conversion count needed). These live in the reward pool.
- **Over-stacking:** each pacify stack consumed above the threshold adds +1 to that Fanatic's follow amount.
- **It is on your side in the enemy phase:** a Fanatic takes no action against you. Enemies can still interfere: a
  Silenced or Stunned Fanatic doesn't follow.
- **Harvest cards** (Sermon) scale off Fanatics present or conversions this turn.

**Echo-chamber immunity (emergent):** because converts revert to *neutral* (not receptive), Faith Leader's core engine can't accidentally fill the row with permanent receptives — so playing their identity doesn't self-trigger the all-receptive penalty. The other two classes make *lasting* allies and genuinely risk the echo chamber; Faith Leader's allies are momentary, so they're naturally immune. (On non-converting turns they still want a hostile present — the **Hardened-enemy-as-permanent-villain** interaction handles this: can't convert them, so they reliably keep you out of the echo chamber; silence them only if too loud.)

**Hard counter / fear:** a row of **Hardened** enemies starves Faith Leader (can't be pacified). On-fantasy: the preacher is powerless against true non-believers. **Leash:** disruption — losing setup or being rushed before reaching 3 stacks.

**Tuning flags (key balance levers):**
- Payoff math must be **generous** — 3 status-applications for a follower that lasts about one turn is a steep trade; the follow amounts (or a harvest card scaling off them) must pay well or it feels bad.
- Needs cards that apply **multiple statuses at once**, so conversion isn't always a full 3 turns of setup.
- Over-stacking is ruled: each stack past the threshold strengthens that Fanatic's follow by 1.

- Loop: **Stack → Convert → Lead your Fanatics → (revert) → repeat**

---

## 8. Starter Decks

Design rule: starters teach the **core verb** in its simplest form, mostly via repeats, with as few distinct cards as a
first-timer can hold. Excitement comes from the reward pool, not the starter. Each deck is mostly basic offense + basic
defense (heavy repeats), one shared-role hostility card, and as many identity cards as the verb needs. All numbers are
placeholders: design the **relationships**, tune the magnitudes in play.

The built decks are authored per origin in [`OriginDatabase`](../Assets/Resources/Databases/OriginDatabase.asset) (card +
count). Where its list differs from the tables below, these tables are the design. All three classes have an authored deck.

### Shared basics (the "Strike / Defend" layer)

Every class has its own basic pair, mechanically near-identical, no flavor riders (StS approach). Across all three the two basics are functionally:

- **Basic offense** — 1e. Push the opinion meter in your favor (small). The "Strike."
- **Basic defense** — 1e. Gain opinion shield (small). The "Defend."

And every class carries **one default hostility card** (the universal echo-chamber escape valve, established in core doc §4):

- **Hostility card** — 1e. Make one receptive enemy non-receptive (seed hostility). Breaks/ prevents the echo chamber. *(Open: whether this is removable.)*

So each deck = ~4 offense + ~3 defense + 1 hostility + identity cards.

### Nepo Baby — *burn for a big swing*

The full starter (Name Drop ×2, Pull Rank, Daddy's Lawyer ×3, I'm Just Like You ×3, Blow the Allowance) and its rulings
are in [`nepo-baby-class.md`](nepo-baby-class.md) §5. It follows the standard shape with a glass-cannon tilt: pressure
pitched above the other classes, shields below.

**Why this set:** Blow the Allowance teaches the burn (the bigger the card, the bigger the swing). Pull Rank teaches the price: free damage now, enemy anger later, and thin shields to absorb it. It is a poor burn target (printed cost 0), which is intended. "I'm Just Like You" is Rhetoric so it feeds Old Boys' Club from fight one (open question 8.2).

**Known tuning risk (playtest):** Pull Rank against encounters that punish Hostility changes, and how fast a burn-heavy deck thins into Heckle/Scandal rot.

### Celebrity — *spectacle on credit*

Starter (10), from [`celebrity-glamour-iou.md`](celebrity-glamour-iou.md): 3 **Hot Take** (Pressure, 1: Sway 6),
3 **No Comment** (Pressure, 1: 5 Composure), 3 **Autograph** (Pressure, 1: -2 Hostility), 1 **Smile and Wave**
(Rhetoric, 1: 5 Composure + 3 Glamour). The base plays fine alone; Smile and Wave is the one seed, showing Glamour
lifting the meter on its own. The IOU pole arrives through rewards.

### Faith Leader — *stack to convert*

The engine and status kit are in §7.

#### Starter deck

| Card | Qty | Cost | Effect | Role |
|---|---|---|---|---|
| Rebuke (offense) | 3 | 1e | Small opinion push | basic |
| Pray (defense) | 2 | 1e | Small opinion shield **+ draw a card** | basic *(elevated — setup class needs hand fuel)* |
| Call Out Sin | 1 | 1e | Push one receptive enemy toward hostile | hostility / echo-chamber escape |
| **Guilt** | 2 | 1e | Apply Guilt (weakens push) + counts toward pacify | **identity (stacker)** |
| **Shame** | 2 | 1e | Apply Shame (drops shield) + counts toward pacify | **identity (stacker)** |
| **Doubt** | 1 | 1e | Apply Doubt (soft reluctance) + counts toward pacify | **identity (stacker)** |
| **Sermon** | 1 | 2e | Harvest: scales with Fanatics present / conversions this turn | **identity (payoff + villain-wanting)** |

**Why this set:** all three status types are present so the player learns the **any-3-to-convert** rule directly, and each status visibly does a *defensive* job too (so stacking never feels wasted). A first-timer stacks two statuses, sees the enemy isn't converting yet, adds a third, watches them flip to a Fanatic that joins their next attack — the whole engine taught in one sequence. Sermon shows the harvest/payoff and wants a villain present.

**Echo-chamber immunity:** converts revert to *neutral*, so Faith Leader's engine never floods the row with permanent receptives → can't self-trigger the all-receptive penalty. (Still wants a hostile present on non-converting turns; a Hardened enemy is the ideal permanent villain.)

Tuning flags are in §7.

### Legibility pass (the trim check)

The discipline isn't a card count — it's whether a new player can read the opening hand and know what to do. Quick self-check per deck:

- **Nepo Baby** — the **hardest class by design**, but the starter is legible: three familiar basics plus one seed whose payoff is printed on the card (Sway = the burned card's cost). The difficulty is in the reward pool and the Hostility clock, not the opening hand. ⚠️ watch Pull Rank: a first-timer may spam it and not connect the angrier room to it.
- **Celebrity** — three plain basics plus one Glamour seed whose effect shows on the meter every round. Legible. ⚠️ watch: does a first-timer notice Scrutiny stripping Glamour when hits leak?
- **Faith Leader** — the **any-3-statuses-to-convert** rule is countable and visible (player sees each enemy's stack climb), and each status does an obvious defensive job, so stacking never feels opaque. Legible. ✅ (watch: is the 3-setups-for-1-turn payoff *felt* as worth it? — tuning, not legibility)

**Rule going forward:** design each deck to its fantasy first, then run this legibility pass and trim only what a first-timer can't hold. Count follows from the verb, not the reverse.

### The "potential" layer (kept separate from starters)

Once starters feel right, sketch the **subset of directions** each class's *reward pool* opens — explicitly NOT in the starter, so the two don't bleed:

- **Nepo Baby** — three lanes plus valves (full card list in `nepo-baby-class.md` section 6): **Burn** (Executive Privilege, Dynasty, Legacy Admission, Encore, Born Into It, Bail Out; Trust Fund as an unlock-gated Rare), **Pull and scan** paid in Hostility (Inside Information, Background Check, Call in a Favor, Special Order), a deliberately sparse **Return** lane (I Know a Guy, Heirloom, Family Seat, Hand-Me-Downs), a slow **Calm** lane capped at Neutral (Apology Tour, Smooth Things Over, Smooth Operator), and **valves** that each cost something (Skip the Line, VIP Access, Do-Over, Not My Problem). Cap valves per reward screen so he stays a glass cannon.
- **Celebrity** — the two poles and their connector (full list in `celebrity-glamour-iou.md`): **Glamour** (Photo Op, Behind the Podium, Standing Ovation, Trending, Going Viral, Fan Mail), **IOU** (Cash Advance, Calling It In, Settle Up, Line of Credit, Open Tab, Bailout, Too Big to Fail, Campaign Donors, Overdraft, Rain Check), and **Soundbite** tokens between them. Persistent Policies declare the build.
- **Faith Leader** — **multi-status-per-card** cards (apply 2 statuses at once, so conversion isn't always 3 turns — a priority), bigger **harvest payoffs** that scale off Fanatics present (Sermon, Crusade), over-stacking past 3 for a bigger burst, Preach-style hard-silence tools, cards that exploit the *defensive* side of statuses (e.g. punish a Shamed enemy harder). Status-interaction *texture* (Guilt+Shame combos differently) lives here, not in core.

This is where each class's *potential* lives. Starters only teach the verb; rewards reveal the ceiling.

---

## 9. Starter Passives (innate ability, StS-style)

Two layers, like StS: **starter passives** = simple innate per-battle ability defining the baseline; **relics** = accumulated persistent passives that warp strategy (the real depth layer — TBD). Starter passives should be humble and reinforce the fantasy via their **trigger timing** (before / during / after).

- **Nepo Baby (now)** — *once per battle, on the opening hand:* full-hand Mulligan (discard your hand, draw a fresh one). Privilege: he never has to live with a bad hand. Do-Over is the paid, weaker version in the pool.
- **Celebrity (start of battle)** — *(open since the Glamour / IOU redesign: this passive was designed for the old Celebrity; the code currently points at a Nepo-style mulligan placeholder)* *the first card you play each battle is played upgraded.* "Mastering his craft" — his opening move is always the polished, rehearsed best-take. Uses the existing upgrade system (nothing new to build); adds a small *which card do I open with?* decision. Note: colorless/generic benefit — Celebrity's identity comes from the card pool, not this passive. *(Playtest watch: don't let any single card's upgraded version be a blowout, since this guarantees it turn one.)*
- **Faith Leader (after)** — *(candidate, unresolved)* leaning toward something that protects the patient setup or engages hostility:
  - Option A: first opinion shield each battle gets a bonus (shelters the setup) — risk: a bit generic.
  - Option B: start of battle, reduce all enemy hostility by 1 (on-theme; does *not* make all receptive, so doesn't auto-trigger echo chamber) — risk: softens a villain you may want.
  - **Decide via playtest**, depends on how harsh the echo chamber feels in practice.

---

## 10. Metagame (early thoughts — not yet solid)

The game is a card game wrapped in an overworld metagame, not a full campaign RPG. The card battles ARE the game; the overworld is connective tissue.

> **Superseded — see `metagame-campaign.md`.** The old "StS map structure" line here was wrong on both counts: the map is Potionomics-style free roam paced by Hours (not a branching node chain), and it is drawn as a **2:1 isometric sprite city** (locked 2026-09-18, `metagame-campaign.md` §1.5). That doc is canonical for the campaign layer; this section keeps only the two ideas below, which it inherits.

- **Viral moments** — exceptional good/bad encounters get "remembered" and ripple beyond the battle. To feel meaningful they should spawn **concrete things** (a new ally approaches, a hostile journalist hunts you, a door opens/closes) — not just hidden stat modifiers. A visible "News Cycle" track logging the last few moments could make aftereffects tangible. Compounding moments could build toward momentum bonuses or crisis encounters.
- **Reward scaling** — winning isn't binary; you want to *win well*. Reward quality scales with how many you converted, how many you left hostile, and enemy hostility levels going in. A sloppy win where the meter barely held = scraps.

> **Caution noted:** the metagame is exciting to design but is decoration on a foundation still being built. Lock the single-encounter loop first.

---

## 11. Playtesting Triggers to Watch

1. **Does the core decision show up every turn?** Do players agonize over keeping a villain, or default to converting everyone? If the clean sweep always works, the echo chamber isn't biting.
2. **Is hostility a resource or a problem?** Do players ever *deliberately seed* hostility? If never, half the design is decorative.
3. **Do the three archetypes actually play differently?** Hide the archetype — can you still guess it from the *decisions*? If not, identities aren't deep enough.
4. **Does the read-react loop create real choices?** Or is the optimal response to an intent layout always obvious?

**Designer habits:** separate "what I intended" from "what happened" (confusion is data, not user failure) · watch hands not faces (unplayed cards are as informative as played ones) · instrument everything (log every card play & end-of-turn meter value) · kill darlings (cut statuses that don't earn their complexity) · assume a dominant strategy exists and try to break the game yourself.

---

## Open Threads
- Full enemy roster & complete voice-intent set
- Relic design space (where build variety lives)
- Finalizing Faith Leader's starter passive trigger
- What receptive enemies grant (mirror to the hostile-draw reward)
- Whether the default hostility card is removable
- ~~Celebrity sub-archetype pools~~ → replaced by the Glamour / IOU build (2026-10-03).

### Recently resolved
- ~~Celebrity's resource system~~ → **CUT.** No unique resource; manufacturing fantasy lives in the card pool (see §7). Over-engineered and made Celebrity inaccessible / too close to StS2's Regent.
- ~~Celebrity's starter passive~~ → **LOCKED** for the old Celebrity: first card played each battle is upgraded. Reopened by the Glamour / IOU redesign (§9).
