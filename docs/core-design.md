# Crookedile — Core Design Doc

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

Hostile enemies are **not purely a problem** — they are a resource. You want them present (see Echo Chamber). Turning an enemy hostile grants a card draw. Receptive enemies should also offer something (TBD — e.g. reduced card cost or amplified meter swings for "reading the room").

### Locked-state statuses
- **Hardened** — cannot be turned receptive. The permanent villain.
- **Fanatic** — cannot be turned hostile. The permanent loyalist.

Both are **statuses** (can be applied and potentially removed), not fixed enemy types. Each archetype relates differently: Faith Leader is frustrated by Hardened (conversion kit fails), Celebrity loves Hardened (free permanent villain), Nepo Baby may be able to break Hardened ("daddy knows people").

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
| **Nepo Baby** | "Who can I bring in?" | **Before** | Change who is even in the room (summon bodies) | Its own imported allies betraying it |
| **Celebrity** | "What deck am I building this run?" | **flexible** | Draft into multiple archetypes the locked classes can't (open canvas) | Committing wrong / an incoherent pile; weakest before it commits |
| **Faith Leader** | "Who can I pacify into a follower?" | **After** | Stack statuses to convert enemies into 1-turn meter-pumping Fanatics | Disruption before reaching 3 stacks; a Hardened room (can't pacify) |

> A distinctiveness test: each archetype must have a **unique capability** AND a **unique fear**. Overlapping fears are what make archetypes feel samey. Watch especially that Celebrity fears *self-overreach* while Faith Leader fears *opponent disruption* — if Celebrity's risk becomes "opponent breaks my setup," they've merged.

### Nepo Baby — *the schemer*
Controls room composition; imports allies via daddy's connections (necromancer-like — summons bodies). Can also **Plant** a hostile to break the echo chamber. **Leash:** summoned allies are the *most* corruptible — their own people turning Turncoat is the signature nightmare.

### Celebrity — *the open canvas* (archetype-flexible)
**Spine: deliberately none — and that absence IS the identity.** The other two are *locked* into one engine (Nepo Baby always Patronage; Faith Leader always stack-to-convert). Celebrity is the class whose final identity is **assembled during the run** — you draft into whichever sub-archetype the reward pool offers, and the class bends to support it. On-fantasy: a celebrity politician has *no fixed substance*, reinventing themselves for the moment (action star one cycle, tearful family man the next). "Empty vessel that becomes whatever the run shapes" is the sharpest expression of the celebrity-politician satire in the cast.

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
**Spine (as concrete as Nepo Baby's Patronage):**

> **Stack statuses (Guilt / Shame / Doubt, any mix) on an enemy to the pacify threshold. This consumes the statuses and converts them. Normal enemy → becomes a Fanatic for 1 turn (a one-turn burst pumping the opinion meter), then reverts to neutral. Hardened enemy → silenced instead (can't be converted, but can be shut up).**
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

**Lifecycle:** stack to threshold (3 + Jaded) → consume pacify statuses → **Fanatic for 1 turn** (meter burst) → **revert to neutral** + gain a **Jaded** stack. No permanent emitters. The class is **relentlessly active** — every turn is spent either stacking toward the next conversion or cashing one in. Kills auto-pilot; you can never coast on a built board, and you can't infinitely milk one target (Jaded escalation).

**Echo-chamber immunity (emergent):** because converts revert to *neutral* (not receptive), Faith Leader's core engine can't accidentally fill the row with permanent receptives — so playing their identity doesn't self-trigger the all-receptive penalty. The other two classes make *lasting* allies and genuinely risk the echo chamber; Faith Leader's allies are momentary, so they're naturally immune. (On non-converting turns they still want a hostile present — the **Hardened-enemy-as-permanent-villain** interaction handles this: can't convert them, so they reliably keep you out of the echo chamber; silence them only if too loud.)

**Hard counter / fear:** a row of **Hardened** enemies starves Faith Leader (can't be pacified). On-fantasy: the preacher is powerless against true non-believers. **Leash:** disruption — losing setup or being rushed before reaching 3 stacks.

**Tuning flags (key balance levers):**
- Payoff math must be **generous** — 3 status-applications for a *1-turn* burst is a steep trade; the Fanatic burst (or a harvest card scaling off it) must pay well or it feels bad.
- Needs cards that apply **multiple statuses at once**, so conversion isn't always a full 3 turns of setup.
- *(Open: can you over-stack past 3 for a bigger burst? Likely reward-pool, not core.)*

- Loop: **Stack → Convert → Burst → (revert) → repeat**

---

## 8. Starter Decks

> **Canonical starter decks live in `crookedile-starter-decks.md`** (kept current there). Summary of the teaching goal per class:
> - **Nepo Baby** — teaches the *Patronage* loop (burn a card → bank Patronage → summon). Two-currency economy; the heaviest to learn.
> - **Faith Leader** — teaches *stack-to-convert* (3 + Jaded statuses → Fanatic burst → revert neutral). All three pacify statuses present.
> - **Celebrity** — teaches *fundamentals + a taste of each draftable direction* (Attention / Scandal / Drama King). Deliberately the most vanilla starter, hinting at breadth rather than one loop, with a sturdy floor.

Design rule: starters teach the **core verb** in its simplest form, mostly via repeats, with as few distinct cards as a first-timer can hold. Excitement comes from the reward pool, not the starter. See the starter-decks doc for full lists, the legibility pass, and the per-class "potential" (reward-pool) sketches.

---

## 9. Starter Passives (innate ability, StS-style)

Two layers, like StS: **starter passives** = simple innate per-battle ability defining the baseline; **relics** = accumulated persistent passives that warp strategy (the real depth layer — TBD). Starter passives should be humble and reinforce the fantasy via their **trigger timing** (before / during / after).

- **Nepo Baby (before)** — *start of battle:* discard any number of cards and redraw that many. (The mulligan — privilege, starts ahead.)
- **Celebrity (start of battle)** — *the first card you play each battle is played upgraded.* "Mastering his craft" — his opening move is always the polished, rehearsed best-take. Uses the existing upgrade system (nothing new to build); adds a small *which card do I open with?* decision. Note: colorless/generic benefit — Celebrity's identity comes from the card pool, not this passive. *(Playtest watch: don't let any single card's upgraded version be a blowout, since this guarantees it turn one.)*
- **Faith Leader (after)** — *(candidate, unresolved)* leaning toward something that protects the patient setup or engages hostility:
  - Option A: first opinion shield each battle gets a bonus (shelters the setup) — risk: a bit generic.
  - Option B: start of battle, reduce all enemy hostility by 1 (on-theme; does *not* make all receptive, so doesn't auto-trigger echo chamber) — risk: softens a villain you may want.
  - **Decide via playtest**, depends on how harsh the echo chamber feels in practice.

---

## 10. Metagame (early thoughts — not yet solid)

The game is a card game wrapped in an overworld metagame (StS map structure, not a full campaign RPG). The card battles ARE the game; the overworld is connective tissue.

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
