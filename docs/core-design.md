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
Guilt · Shame · Silence · Devotion · Hardened · Fanatic · Turncoat

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
| **Celebrity** | "What drama do I set up?" | **During** | Fabricate cards & room conditions on demand | Its own credibility collapsing / exposure |
| **Faith Leader** | "Do I have the right cards in the right order?" | **After** | Chain compounding combos into one explosive turn | Disruption of the sequence |

> A distinctiveness test: each archetype must have a **unique capability** AND a **unique fear**. Overlapping fears are what make archetypes feel samey. Watch especially that Celebrity fears *self-overreach* while Faith Leader fears *opponent disruption* — if Celebrity's risk becomes "opponent breaks my setup," they've merged.

### Nepo Baby — *the schemer*
Controls room composition; imports allies via daddy's connections (necromancer-like — summons bodies). Can also **Plant** a hostile to break the echo chamber. **Leash:** summoned allies are the *most* corruptible — their own people turning Turncoat is the signature nightmare.

### Celebrity — *the director*
Doesn't manage the room, **directs** it. Manufactures cards and crowd states (staging) then cashes in with conditional payoff cards. Think teleserye: scripted drama, timed tears, a villain cast on purpose. **Credibility** = an overload-style resource (Hearthstone Overload): manufacture now, pay later. **Leash:** exposure — overreach while low on credibility is catastrophic.
- Loop: **Stage → Perform → Cash in**
- Two starter cards scaling off *opposite* room states teaches "curate, don't farm."

### Faith Leader — *the combo player*
Incremental gains that compound into massive payoffs; everything interconnects (prayer → blessing → miracle). Status specialist (guilt, shame, devotion). Weakest individual card stats; most vulnerable if caught without setup. Highest skill ceiling. **Leash:** disruption unravels the chain.
- Loop: **Seed → Tend → Harvest** (Suffer → Build → Deliver)

---

## 8. Starter Decks (first pass — values are placeholder)

> Starter decks teach the **core loop** in its simplest form. Be deliberately humble; excitement comes from rewards/relics. Each starter has exactly **one** card teaching "you want a villain."

### Faith Leader — teaches *sequencing*
- **Guilt** (x3) — 1e. Reduce one enemy's hostility, apply Guilt. (seed)
- **Pray** (x3) — 1e. Small opinion shield, draw a card. (engine)
- **Preach** (x2) — 1e. Silence one hostile enemy this turn. (defense)
- **Absolution** (x1) — 2e. Convert an enemy to ally — *only if they have Guilt*. (payoff requiring prior play — teaches the whole archetype)
- **Sermon** (x1) — 2e. Whole row; scales with total statuses on field. (villain-wanting card)

### Celebrity — teaches *stage-then-cash-in*
- **Spin** (x3) — 1e. Manufacture: flip one enemy's apparent stance. (staging)
- **Woe Is Me** (x2) — 1e. Opinion shield + meter gain scaling with **receptive** enemies. (payoff)
- **Read the Room** (x2) — 1e. Draw 2, costs Credibility. (teaches debt gently)
- **Crocodile Tears** (x2) — 1e. Meter surge scaling with **hostile** enemies. (villain-wanting card)
- **Big Reveal** (x1) — 2e. Strong manufactured card, adds Credibility debt. (high-risk taste)

### Nepo Baby — teaches *import-and-protect*
- **Call a Favor** (x3) — 1e. Summon a receptive ally. (signature verb)
- **Cover Story** (x2) — 1e. Protect one ally from Turncoat this turn. (teaches fragility immediately)
- **Connections** (x2) — 1e. Reduce one enemy's hostility, gain small resource. (glue)
- **Plant** (x2) — 1e. Summon a **hostile** body. (villain-wanting card + echo-chamber breaker)
- **Daddy Knows People** (x1) — 2e. Summon an ally AND shield the meter. (import + payoff)

---

## 9. Starter Passives (innate ability, StS-style)

Two layers, like StS: **starter passives** = simple innate per-battle ability defining the baseline; **relics** = accumulated persistent passives that warp strategy (the real depth layer — TBD). Starter passives should be humble and reinforce the fantasy via their **trigger timing** (before / during / after).

- **Nepo Baby (before)** — *start of battle:* discard any number of cards and redraw that many. (The mulligan — privilege, starts ahead.)
- **Celebrity (during)** — *on first card each turn:* its room-scaling effect counts one extra enemy. (Performance — best in the moment.)
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
