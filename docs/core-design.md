# Crookedile — Core Design Doc

> **Kind:** Design · **Status:** Canonical · **Updated:** 2026-10-03
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
Guilt · Shame · Doubt · Silence · Devotion · Jaded · Hardened · Fanatic · Turncoat · Scandal (Celebrity)

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
| **Celebrity** | "What deck am I building this run?" | **flexible** | Draft into multiple archetypes the locked classes can't (open canvas) | Committing wrong / an incoherent pile; weakest before it commits |
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

### Celebrity — *the open canvas* (archetype-flexible)
**Spine: deliberately none — and that absence IS the identity.** The other two are *locked* into one engine (Nepo Baby always consumes his deck; Faith Leader always stack-to-convert). Celebrity is the class whose final identity is **assembled during the run** — you draft into whichever sub-archetype the reward pool offers, and the class bends to support it. On-fantasy: a celebrity politician has *no fixed substance*, reinventing themselves for the moment (action star one cycle, tearful family man the next). "Empty vessel that becomes whatever the run shapes" is the sharpest expression of the celebrity-politician satire in the cast.

Closest StS reference: **Silent's deep philosophy** (draft into poison/shiv/discard) taken to the extreme, with an **Ironclad-style reliable floor** so it never bricks before committing. *Plays like the Silent (card flow, adapt), with Ironclad's floor (sturdy fundamentals).* **High floor early, high ceiling late.**

**NOT the beginner on-ramp.** "Versatile" sounds gentle but the open-canvas class is the *hardest to balance and least beginner-friendly* — maximum freedom = maximum rope. A new player can build an incoherent pile; an expert expresses themselves. **Celebrity is the advanced / expressive class.** (Corrects earlier "accessible starter" framing.) Depth comes from engaging the *core game* (hostility, villain balance, echo chamber) directly + assembling an archetype, rather than a personal engine layered on top.

**Design history:** an earlier "Credibility" resource (Overload → depleting pool w/ exposure cliff → fabricated-tag + collapse) was **cut as over-engineered** — it made Celebrity play a different game (bespoke fake-trackers/collapse rules), hurt accessibility, cost a lot to build for 1/3 of the roster, and kept resembling StS2's **Regent** (Stars). Resolution: no unique resource; identity = breadth of card pool + draftable sub-archetypes below.

**The card pool must contain multiple *coherent* mini-archetypes** (not a pile of random good cards — that's oatmeal). Each is a distinct **risk posture**; the run decides which you assemble. The Scandal line in particular hard-commits (it sacrifices flexibility), so committing to one direction can shut others off — spiky, replayable, expert.

#### Three draftable sub-archetypes

**1. Attention / Aggro — *build-and-spend* (tempo risk).** Court attention / provoke the room to bank a resource, then spend it as a big opinion-meter hit. Fantasy: the celebrity who *wants* the spotlight — all publicity is good publicity — and converts "everyone's talking about me" into political gain. **Risk:** drawing aggro means the room focuses on attacking *you*; you take heat to build the payoff, and holding too long makes you a target. The easiest/cleanest of the three.

**2. Scandal — *anti-Curse snowball* (consistency risk, all-in).** Scandals are manufactured cards (by your own cards *or* inflicted by enemies — a tabloid/paparazzi enemy is a *threat to others but a gift here*: all publicity is good publicity). They **clog the hand** (fewer playable slots = the "fewer outs" downside), **but that clog IS the engine**: your other cards pay off **per Scandal drawn** (e.g. +1 shield each) and/or **per Scandal in play** (e.g. deal X at end of turn each). The more you carry, the harder you hit and the more cramped you play — reward and cost are *the same cards*, so it's **self-limiting** (no cap needed; eventually you can't draw a workable hand). A **"spin"/cash-out** outlet clears Scandals for a burst, letting you detonate at the peak before you choke. This is the one Celebrity line with a real spine-like loop — fine, since the open-canvas class can have one deep draftable direction among simpler ones.
  - *Inversion of StS Curses:* you WANT Scandals (they power you); they only hurt by occupying hand slots — not by active punishment. Keep the draw-downside **gentle** (clog, not stab) since the scaling reward is already the reason to stop hoarding (don't double-punish).
  - *Tuning/open:* exact severity when drawn; whether removal beyond the cash-out exists; on-draw triggers (swingy) vs. in-play triggers (steady) — pool can have both.

**3. Drama King — *sympathy + disarm* (low risk, control).** Manufactured victimhood: fetch **sympathy** (shields / opinion defense) and **disarm** enemies (reduce their ability to hurt the meter) while still chipping the meter. The outlast/grind line — survive, neutralize, win on accumulated pressure, never let your standing drop. *"Look what they're doing to me."*
  - *Watch-flag:* disarm ≈ Faith Leader's weaken/debuffs. Keep framing distinct — Faith Leader debuffs to **convert**; Celebrity disarms to **protect itself while attacking**. Similar tools, different intent.

**Why the set works:** three different *risk postures*, not three flavors of one thing — **Attention** = tempo risk (fast, snowbally), **Scandal** = consistency risk (all-in, sacrifices flexibility), **Drama King** = low risk (grind, control). Drafting Celebrity = choosing "fast and loud / all-in on spectacle / slow and safe." Three genuinely different runs from one class — the open canvas delivering.

### Faith Leader — *the converter* (status specialist)
**Spine (as concrete as Nepo Baby's burn):**

> **Stack statuses (Guilt / Shame / Doubt, any mix) on an enemy to the pacify threshold. This consumes the statuses and converts them. Normal enemy → becomes a Fanatic, a loyal follower that copies your attacks and defends until the end of your next turn, then reverts to neutral. Hardened enemy → silenced instead (can't be converted, but can be shut up).**
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

**Lifecycle:** stack to threshold (3 + Jaded) → consume pacify statuses → the enemy becomes a **Fanatic**, your loyal follower → at the end of your next turn it **reverts to neutral** + gains a **Jaded** stack. No permanent emitters. The class is **relentlessly active** — every turn is spent either stacking toward the next conversion or using the followers you have. Kills auto-pilot; you can never coast on a built board, and you can't infinitely milk one target (Jaded escalation).

#### Fanatics follow your lead *(ruled 2026-10-03)*

A Fanatic is not a one-off burst. It is a follower that copies what you do, and cards can order it around. Conversion
itself pays nothing; the value is in what the Fanatic does while it lasts. *All numbers are placeholders for the
config.*

- **Duration:** from conversion until the **end of your next turn**, so a Fanatic converted by your last play still
  follows a full turn. Then it reverts to neutral and gains Jaded.
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
count). Where its list differs from the tables below, these tables are the design. Celebrity has no authored entry yet,
so it falls back to one of each starter-tagged card.

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

### Celebrity — *fundamentals plus a taste of each direction*

**No fixed verb — and that's the point.** Celebrity's identity is *assembled during the run* by drafting into one of three sub-archetypes (Attention / Scandal / Drama King — see §7). So the **starter can't teach one loop** — its job is to teach the **core fundamentals competently** (push, shield, manipulate) with a **sturdy floor** (never bricks), while planting **one seed of each direction** so the player feels the breadth and understands "this class becomes what I draft." Plays like the Silent (flow/adapt) with Ironclad's floor.

**This is the advanced/expressive class, NOT the beginner on-ramp** — versatility = maximum rope. (See §7.)

#### Starter deck

| Card | Qty | Cost | Effect | Role |
|---|---|---|---|---|
| Soundbite (offense) | 4 | 1e | Small opinion push | basic (the reliable floor) |
| Spin Control (defense) | 3 | 1e | Small opinion shield | basic (the reliable floor) |
| Read the Room | 2 | 1e | Draw 2 | flow/dig (Silent-style adaptability) |
| Manufactured Drama | 1 | 1e | Flip one enemy to **hostile** (cast a villain) | hostility / echo-chamber escape |
| **Court Attention** | 1 | 1e | Draw aggro to yourself; bank it → spend later as meter damage | **seed of Attention/aggro line** |
| **Hit Piece** | 1 | 1e | Generate 1 **Scandal**; small bonus per Scandal you're carrying | **seed of Scandal line** |
| **Woe Is Me** | 1 | 1e | Gain **sympathy** (shield) + disarm-lite on one enemy while chipping the meter | **seed of Drama King line** |

**Why this shape:** the basics + Read the Room give a forgiving, flexible floor (you always have a competent play). The three single-copy seed cards each *gesture* at a draftable direction without committing — the player tries each, notices "huh, if I picked up more Scandal cards this could be a whole thing," and that curiosity *is* the open-canvas lesson. Deliberately the most vanilla starter; the magic is in the reward pool where you commit.

*No resource system* — directions are expressed purely through cards (see §7 design history: the Credibility system was cut).

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
- **Celebrity** — basics + Read the Room are dead simple; the three single-copy seed cards each gesture at a direction without forcing a decision. Risk isn't legibility of the *starter* — it's that the *class* asks the player to eventually commit to a direction, which a true beginner won't know to do. ⚠️ advanced class by nature, not the on-ramp (see §7).
- **Faith Leader** — the **any-3-statuses-to-convert** rule is countable and visible (player sees each enemy's stack climb), and each status does an obvious defensive job, so stacking never feels opaque. Legible. ✅ (watch: is the 3-setups-for-1-turn payoff *felt* as worth it? — tuning, not legibility)

**Rule going forward:** design each deck to its fantasy first, then run this legibility pass and trim only what a first-timer can't hold. Count follows from the verb, not the reverse.

### The "potential" layer (kept separate from starters)

Once starters feel right, sketch the **subset of directions** each class's *reward pool* opens — explicitly NOT in the starter, so the two don't bleed:

- **Nepo Baby** — three lanes plus valves (full card list in `nepo-baby-class.md` section 6): **Burn** (Executive Privilege, Dynasty, Legacy Admission, Encore, Born Into It, Bail Out; Trust Fund as an unlock-gated Rare), **Pull and scan** paid in Hostility (Inside Information, Background Check, Call in a Favor, Special Order), a deliberately sparse **Return** lane (I Know a Guy, Heirloom, Family Seat, Hand-Me-Downs), a slow **Calm** lane capped at Neutral (Apology Tour, Smooth Things Over, Smooth Operator), and **valves** that each cost something (Skip the Line, VIP Access, Do-Over, Not My Problem). Cap valves per reward screen so he stays a glass cannon.
- **Celebrity** — the reward pool is the **widest in the game**, organized into three draftable sub-archetypes the player commits to over a run (see §7):
  - **Attention/Aggro** — cards that draw aggro and bank it, payoffs that spend banked attention as big meter hits (build-and-spend, tempo risk).
  - **Scandal** — Scandal-generators (and synergy with enemy-inflicted Scandals), per-Scandal-drawn payoffs (+shield etc.), per-Scandal-in-play payoffs (end-of-turn meter damage), and a **spin/cash-out** to clear Scandals for a burst (anti-Curse snowball, consistency risk, all-in). *Tuning: draw-severity gentle not punishing; on-draw vs in-play triggers.*
  - **Drama King** — sympathy/shield generators, disarm/enemy-weaken tools, grind payoffs (control, low risk). *Watch: disarm vs Faith Leader weaken — frame as self-protection, not conversion.*
  - Each line should be *coherent enough to commit to*; flexibility is **between** drafted archetypes, not mush within every card.
- **Faith Leader** — **multi-status-per-card** cards (apply 2 statuses at once, so conversion isn't always 3 turns — a priority), bigger **harvest payoffs** that scale off Fanatics present (Sermon, Crusade), over-stacking past 3 for a bigger burst, Preach-style hard-silence tools, cards that exploit the *defensive* side of statuses (e.g. punish a Shamed enemy harder). Status-interaction *texture* (Guilt+Shame combos differently) lives here, not in core.

This is where each class's *potential* lives. Starters only teach the verb; rewards reveal the ceiling.

---

## 9. Starter Passives (innate ability, StS-style)

Two layers, like StS: **starter passives** = simple innate per-battle ability defining the baseline; **relics** = accumulated persistent passives that warp strategy (the real depth layer — TBD). Starter passives should be humble and reinforce the fantasy via their **trigger timing** (before / during / after).

- **Nepo Baby (now)** — *once per battle, on the opening hand:* full-hand Mulligan (discard your hand, draw a fresh one). Privilege: he never has to live with a bad hand. Do-Over is the paid, weaker version in the pool.
- **Celebrity (start of battle)** — *the first card you play each battle is played upgraded.* "Mastering his craft" — his opening move is always the polished, rehearsed best-take. Uses the existing upgrade system (nothing new to build); adds a small *which card do I open with?* decision. Note: colorless/generic benefit — Celebrity's identity comes from the card pool, not this passive. *(Playtest watch: don't let any single card's upgraded version be a blowout, since this guarantees it turn one.)*
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
- Celebrity: flesh out each of the three sub-archetype card pools (Attention / Scandal / Drama King) deeply enough that each is committable; settle Scandal tuning (draw-severity, on-draw vs in-play triggers, removal beyond cash-out)

### Recently resolved
- ~~Celebrity's resource system~~ → **CUT.** No unique resource; manufacturing fantasy lives in the card pool (see §7). Over-engineered and made Celebrity inaccessible / too close to StS2's Regent.
- ~~Celebrity's starter passive~~ → **LOCKED.** First card played each battle is upgraded ("mastering his craft").
