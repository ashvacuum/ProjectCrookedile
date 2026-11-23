# Crookedile — Core Design Doc

> **Kind:** Design · **Status:** Canonical · **Updated:** 2026-11-21
>
> **Summary:** The combat model: Opinion Meter, hostility, the Echo Chamber rule, voice intents, turn structure, the three archetypes (each a starting relic plus three pillars), card types, starter decks.
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

**Winning (2026-10-27):** filling the meter wins immediately and emptying it loses immediately. At the turn limit
(Judgment) you win only at or above the **win line, 60% of the meter** (`BattleManager` Judgment Win Percent).
Edging past half is a loss: the room wants a clear verdict, so a good setup pays off and a careless one fails.
Margin also shapes the reward: a win at **80%+** draws from a better card pool, and a **maxed meter guarantees at
least one Rare** (see `campaign-encounters.md` §10).

---

## 3. Hostility

Hostility is the heart of the game. It exists as both:

1. **An enemy stance** — enemies start outwardly hostile, neutral, or meek/receptive.
2. **A card element** — cards can seed, manage, redirect, or amplify hostility deliberately. Playing a card never
   shifts Hostility by itself (ruling 2026-11-02): every raise or drop is an explicit effect on the card.

**Hostility sets behaviour, never numbers (ruling 2026-09-29).** An enemy's hostility decides its stance, and the stance decides which of its authored move lists it draws from (hostile / neutral / receptive). A hostile enemy hits harder only because its hostile moves are authored harder — hostility is never a damage multiplier. (An earlier `1 + 0.5 × hostility` multiplier on enemy pushes was removed: it turned every riling card into a compounding damage spiral.)

Hostile enemies are **not purely a problem** — they are a resource. You want them present (see Echo Chamber). Turning an enemy hostile grants a card draw. **Receptive enemies pay in Support (2026-09-29):** +2 Support the moment an enemy turns receptive, and +1 Support per receptive enemy at the start of each of your turns. Hostile = more cards; receptive = more cushion — and an all-receptive room still triggers the echo chamber.

### Locked-state statuses
- **Hardened** — cannot be turned receptive. The permanent villain.
- **Fanatic** — cannot be turned hostile. The permanent loyalist.

Both are **statuses** (can be applied and potentially removed), not fixed enemy types. Each archetype relates differently: Faith Leader is frustrated by Hardened (conversion kit fails), Celebrity loves Hardened (free permanent villain), Nepo Baby barely notices it (his calming stops at Neutral anyway, so Hardened only blocks reducers that convert incidentally).

### Status list (working)
Guilt · Silence · Devotion · Jaded · Hardened · Fanatic · Turncoat · Glamour (Celebrity, on the player)

*Faith Leader's core status:* **Guilt** (weakens enemy push) is her one *pacify status* (counts toward conversion, consumed on convert). Shame and Doubt were retired on 2026-10-31. **Jaded** is a *threshold status* (permanent, stacks, raises pacify cost, never consumed). See §7 Faith Leader.

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

The opponent phase resolves the rival's moves in their displayed order, then audience modifier intents, then audience direct intents. Reaching full Opinion wins immediately; reaching zero loses immediately. Timed Judgment uses the win line (60% of the meter), after the final opponent response in a boss debate. Standard fights retain their existing Judgment timing.

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
- **Turn** — try to turn a receptive enemy back to hostile (named Sway in older notes; the player's push is Sway)
- **Condemn** — heavy downward push on the meter
- **Murmur** — low impact, maintains presence: no meter push, at most a light debuff

Plus **Summon**, **Ward** and **Counter** for the enemies that need them. The code-name to label mapping is in
[`naming-glossary.md`](naming-glossary.md) §C.

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
3. **Enemy action phase** — remaining intents execute; meter stays **live**. Modifier intents (Rally/Turn) resolve **first**, then direct intents (Condemn/Rebuke) **left to right**.
4. **End of turn** — statuses tick/compound; new intents revealed; viral-moment check.

Energy is **3 per turn**.

---

## 7. The Three Archetypes

**Class shape *(2026-11-21)*.** Like most Slay the Spire classes, each archetype has **one upfront mechanic** that runs
from the first turn, carried by its **starting relic** (a starting ally) and taught by the starter deck, and **three
pillars** in the reward pool: three ways to bend that mechanic, each strong on its own, so a run's picks decide which
version of the class you play. Shared **glue** cards serve any pillar. The relics act on different things, so the
classes stay apart: Faith Leader acts on **enemies**, Celebrity on **his own cards**, Nepo Baby on **his draw pile**.

**Why:** the 2026-11-06 playtest showed every class playing as its basics (Faith Leader's basic attack and defend were
60% of plays; each reward card about 1-2%) and Faith Leader's conversion never firing. The identity showed too late.

**No buttons.** Every relic fires on its own or through normal card play. Nothing asks the player to press a button or
choose to spend a resource.

| Archetype | Starting relic | Acts on | Pillars |
|---|---|---|---|
| **Faith Leader** | The first attack each turn applies 1 Guilt to its target (untargeted: a random enemy that plans to attack you, else any random enemy) | Enemies | **Congregation**, **Inquisition**, **Martyr** |
| **Celebrity** | **Last Impressions:** the first Pressure card you play each turn uses its upgraded effect | His own cards | **Star Power**, **Leverage**, **Content Machine** |
| **Nepo Baby** | At the start of your turn, **Scry 1** | His draw pile | **Inheritance**, **Owed**, **Pulling Strings** |

> A distinctiveness test: each archetype must have a **unique capability** AND a **unique fear**. Overlapping fears are what make archetypes feel samey.

### Nepo Baby — *his power isn't his own* *(redesigned 2026-11-21)*
> **Full spec: [`nepo-baby-class.md`](nepo-baby-class.md).** Burn (exhaust as a cost) is no longer his identity: it was
> undefined and hard to grasp.

Someone handed everything. He never has to live with a bad draw, and his strongest card is his surname. His mechanic is
**deck filtering**: he only ever gets the good cards, because someone tipped him off.

- **Relic: Scry 1** at the start of your turn. One light choice: keep the top card or discard it.
- **Inheritance:** **The Family Name**, a starter Pressure card, grows through **Invest** cards and returns to the
  **top of your draw pile** after play, so the relic's Scry decides each turn whether you draw it again. ("Birthright" is
  reserved as a possible keyword for its return rule.)
- **Owed** (keyword): "When discarded, play it." Only discards caused by an effect count (Scry, discard-and-draw,
  choose-to-discard), never the end-of-turn hand discard. Owed pays him for throwing cards away.
- **Pulling Strings:** deck advantage. Return cards from the discard pile to hand, into the draw pile or onto its top;
  "Scry X; if a Policy is among them, play it"; and similar. Brake: a card returned from the discard pile to hand can't be
  returned again that battle (closes the Owed loop).
- **Fear:** a draw pile full of junk the filter can't keep up with.

### Celebrity — *the empty promise* *(pillars set 2026-11-21)*
> **Full spec: [`celebrity-glamour-iou.md`](celebrity-glamour-iou.md)** (rules, all cards, open questions).

A guy who knows nothing, talks the talk, and never walks the walk. Every promise is empty and it is all for the press.
He does not convert enemies. Glamour and Debt are **pillars**, not his identity: his identity is the polish.

- **Relic: Last Impressions.** The first Pressure card you play each turn uses its upgraded effect ("he practised that
  line"). That play only: the card itself never changes. Generated cards (Soundbites) don't count.
- **Star Power (Glamour):** after the enemy acts, the meter rises by your Glamour, then Glamour drops by 1; every hit
  that leaks past Support strips 1 (Scrutiny). Bank it for ticks or spend it.
- **Leverage (Debt):** borrow energy now; Debt settles at the start of your next turn, energy first, then the meter.
  Promises and card recovery/booking fold in here.
- **Content Machine:** Soundbites and other generated cards; payoffs for volume.
- **Glue:** the core defensive cards; the villain cards (Aggravate, Soothe, Manufactured Outrage) sit beside Star Power.
  Feud is not a pillar (it acts on an enemy, which is Faith Leader's side). Starstruck is proposed for cutting.

**Design history:** the open-canvas Celebrity (Attention / Scandal / Drama King) was replaced by the Glamour / IOU build
on 2026-10-03; an earlier "Credibility" resource was cut as over-engineered. The 2026-11-21 redesign gave him a relic
and three pillars, and replaced the Publicist (enemies start Exposed) as his starting ally.

### Faith Leader — *the converter* (status specialist)

**2026-11-21 redesign.** The old engine needed many cards and much preparation and leaned on luck: Guilt drains a stack
every turn, so three stacks had to land on one enemy within a turn. Now:

- **Relic:** the first attack each turn applies **1 Guilt to its target**: the enemy the speech is aimed at, whatever it
  intends. An untargeted first attack guilts a random enemy that plans to attack you; with no attackers, any random
  enemy. A guilty Fanatic gets the Guilt too.
- **Guilt keeps draining** (1 per turn), so the relic alone never builds to a conversion. Guilt blunts the push of
  whoever carries it (a Fanatic's follow included) and feeds the payoffs that count guilty enemies (Searching Souls,
  Public Penance, Heavy Conscience): that is the base class.
- **Inquisition:** amplify Guilt past the drain (more per application, more triggers, gather it onto one enemy, spread
  it) and **earn conversion**. **Gospel** (conversion only while a conversion Policy is in play) is this pillar's
  keystone, not a card every Faith Leader needs. The Fanatic rules below are Inquisition's engine.
- **Martyr** (Inquisition's extension): Fanatics push harder per hostile enemy. Only while Fanatics exist; converting a
  hostile enemy lowers the count it scales on, so the choice is more followers or angrier ones.
- **Congregation:** calm the room into the echo chamber and **outrun it** with much bigger output; the echo chamber still
  applies. Keystone **Tithe**: while the echo chamber is active, gain 1 energy at the start of your turn. Also a cheap
  Policy: draw 1 extra card at turn start while every enemy is receptive.
- **Rulings:** a Hardened enemy at the threshold is Silenced, not converted. A Fanatic that reaches the threshold while
  still following converts again on the spot: +1 Fanatic stack, no Jaded (Jaded comes on disillusion). Guilt on a Fanatic
  primes it to be converted again once disillusioned.

**Inquisition's engine (as concrete as Nepo Baby's filter):**

> **Stack Guilt on an enemy to the pacify threshold. This consumes the Guilt and converts them. Normal enemy → becomes a Fanatic, a loyal follower that copies your attacks and defends for as long as the Fanatic buff lasts, then is disillusioned and reverts to neutral. Hardened enemy → silenced instead (can't be converted, but can be shut up).**
>
> **Pacify threshold = 3 + the enemy's Jaded stacks.** Each time an enemy is fanaticized they gain a stack of **Jaded** (permanent for the fight, never consumed), raising their future conversion cost by 1. So a fresh enemy converts at 3; a once-burned backslider at 4; twice at 5; etc. **This is the anti-milking brake** — re-converting the same person hits diminishing returns, pushing you to win *new* converts rather than farm one target. On-fantasy: a believer who's already lapsed is harder to inspire again.

**Two status categories (keep distinct):**
- **Pacify status** (Guilt) — *you* apply it, it counts toward the threshold, **consumed on conversion**.
- **Threshold status** (Jaded) — applied *automatically* on each fanaticization, **modifies** the requirement, **permanent & never consumed**, does **NOT** count toward its own threshold. Stacks. Visible on the enemy so the player reads conversion cost directly off the Jaded count.

**One status: Guilt *(2026-10-31)*.** Guilt (≈Weakened) blunts the enemy's *push* on the meter *while* counting
toward the pacify threshold, so the climb is self-protecting: **3 Guilt = pacify** (plus Jaded). Shame and Doubt were
retired: three interchangeable statuses split her payoff cards three ways (draw the wrong setup and the payoff was
dead) and crowded the enemy with markers. Every setup card now feeds every payoff card; variety lives in what the
payoffs give, not in which status they need. Guilt was kept because blunting the push answers her real weakness,
damage taken.

**Lifecycle:** stack to threshold (3 + Jaded) → consume pacify statuses → the enemy becomes a **Fanatic**, your loyal follower, for as long as the Fanatic buff lasts → when it runs out the follower is **disillusioned**: it **reverts to neutral** + gains a **Jaded** stack. No permanent emitters. The class is **relentlessly active** — every turn is spent either stacking toward the next conversion or using the followers you have. Kills auto-pilot; you can never coast on a built board, and you can't infinitely milk one target (Jaded escalation).

#### Fanatics follow your lead *(ruled 2026-10-03)*

A Fanatic is not a one-off burst. It is a follower that copies what you do, and cards can order it around. Conversion
itself pays nothing; the value is in what the Fanatic does while it lasts. *All numbers are placeholders for the
config.*

- **Duration:** a follower for **as long as the Fanatic buff lasts**. The buff counts down each turn (placeholder
  length: through the end of your next turn, so a Fanatic converted by your last play still follows a full turn), and
  cards can extend it. When it runs out the follower is **disillusioned**: it reverts to neutral and gains Jaded.
- **It follows your plays.** Each time you play a card:
  - that pushes the meter (an **attack**), every Fanatic also pushes: **+4 Opinion each** (`FaithLeaderConfig`, raised from 2 on 2026-10-30);
  - that gains Support (a **defend**), every Fanatic adds to it: **+3 Support each** (raised from 2);
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

**Status engine *(2026-10-30, Guilt-only 2026-10-31)*.** Placeholder cards, all keyed to Guilt:

- *Setup.* **Confession** (Rhetoric, Basic, 1): 2 Guilt on a target; upgraded 3, a full conversion in one card. **Force
  Majeure** (Rhetoric, Rare, 2): 1 Guilt on every hostile enemy (2 upgraded); the angrier the room, the wider it lands.
  **Still Small Voice** (Policy, Enhanced, 1; upgraded 0): whenever an enemy turns hostile, it gains 1 Guilt, so riling
  the room feeds conversion.
- *Payoff while Guilt sits on enemies,* so setup turns aren't dead turns: **Searching Souls** (Rhetoric, Basic, 1;
  upgraded 0): draw 1 per guilty enemy. **Public Penance** (Pressure, Basic, 1): 4 Opinion against each guilty enemy
  (6 upgraded). **Heavy Conscience** (Rhetoric, Basic, 1): 4 Support per guilty enemy (6 upgraded). **Fire and
  Brimstone** (Rhetoric, Enhanced, 2; upgraded 1): for each guilty enemy, gain 1 Strength until end of turn, so every
  Opinion hit that turn deals +1 per guilty enemy.
- *Conversion payoff.* Her 3-cost payoffs (Us vs Them, Zealotry, The World Is Against Us, Gospel, Hallelujah, Faith) cost
  1 less per enemy converted this turn, so a conversion turn is a payoff turn.

The starter swaps one Support for Confession (Pressure ×5, Support ×4, Guilt, Traitor to the Cause, Confession).

**Full house: energy for courting the echo chamber *(2026-10-30)*.** A second, deliberately risky lane beside
conversion. Calming (not converting) can turn the *whole* room receptive, which is the echo chamber: gains halved and the
meter decaying. Faith Leader is paid for taking that risk in **energy**, the resource she otherwise lacks, so a full
house funds the big plays that break it again. Placeholder cards: **Tithe** (Policy, Enhanced, 1; upgraded 0): at the
start of your turn, if every enemy is receptive, gain 2 energy (3 upgraded). **Altar Call** (Rhetoric, Basic, 1): reduce
a target's Hostility by 3 (4 upgraded); then if every enemy is receptive, gain 1 energy. **Answered Prayer** (Pressure,
Basic, 1): 4 Opinion (6 upgraded); then if every enemy is receptive, gain 1 energy. The check is the room's state when
the card resolves (`RoomFullyReceptive`), so the energy comes once per play.

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

**2026-11-21: starters now teach the relic.** Each starter is to be rebuilt so the class's relic (§7) does something
from turn 1, with a few cards that hint at each pillar. The decks below are the pre-redesign versions and are kept until
the rebuild.

### Card types *(2026-11-21)*

The type is the card's role, so relics and pillars can key off it and the frame colour tells the player what counts:

- **Pressure = attack.** If a card pushes the meter, it is Pressure first and foremost, whatever else it does.
- **Rhetoric = skill:** defend, statuses, calming, draw and tricks.
- **Policy = persistent:** it stays in play.
- **Heckle** and **Scandal:** unchanged, the junk cards.

An audit on 2026-11-21 found 18 Pressure cards that never push the meter and 11 Rhetoric cards that do. They get
re-typed once the class card pools settle.

### Shared basics (the "Strike / Defend" layer)

Every class has its own basic pair, mechanically near-identical, no flavor riders (StS approach). Across all three the two basics are functionally:

- **Basic offense** — 1e. Push the opinion meter in your favor (small). The "Strike."
- **Basic defense** — 1e. Gain opinion shield (small). The "Defend."

And every class carries **one default hostility card** (the universal echo-chamber escape valve, established in core doc §4):

- **Hostility card** — 1e. Make one receptive enemy non-receptive (seed hostility). Breaks/ prevents the echo chamber. *(Open: whether this is removable.)*

So each deck = ~4 offense + ~3 defense + 1 hostility + identity cards.

### Nepo Baby — *to be rebuilt*

The current starter (Name Drop ×4, Pull Rank, Daddy's Lawyer ×2, I'm Just Like You ×2, Blow the Allowance) teaches the
retired burn design. The rebuild centres on **The Family Name** (Inheritance) with the Scry relic, plus a card or two
hinting at Owed and Pulling Strings. See [`nepo-baby-class.md`](nepo-baby-class.md).

### Celebrity — *spectacle on credit*

Starter (10), from [`celebrity-glamour-iou.md`](celebrity-glamour-iou.md): 3 **Hot Take** (Pressure, 1: Sway 6),
3 **No Comment** (Pressure, 1: 7 Support), 3 **Autograph** (Pressure, 1: -2 Hostility), 1 **Smile and Wave**
(Rhetoric, 1: 5 Support + 3 Glamour). The base plays fine alone; Smile and Wave is the one seed, showing Glamour
lifting the meter on its own. The IOU pole arrives through rewards. Under the card-type rule, No Comment (Support) and
Autograph (calming) become Rhetoric, so Hot Take is the starter's Pressure card that Last Impressions upgrades.

### Faith Leader — *stack to convert*

The engine and status kit are in §7.

#### Starter deck

| Card | Qty | Cost | Effect | Role |
|---|---|---|---|---|
| Rebuke (offense) | 3 | 1e | Small opinion push | basic |
| Pray (defense) | 2 | 1e | Small opinion shield **+ draw a card** | basic *(elevated — setup class needs hand fuel)* |
| Call Out Sin | 1 | 1e | Push one receptive enemy toward hostile | hostility / echo-chamber escape |
| **Guilt** | 5 | 1e | Apply Guilt (weakens push) + counts toward pacify | **identity (stacker)** |
| **Sermon** | 1 | 2e | Harvest: scales with Fanatics present / conversions this turn | **identity (payoff + villain-wanting)** |

**Why this set:** Guilt is present enough that the player learns the **3-Guilt-to-convert** rule directly, and it visibly does a *defensive* job too (so stacking never feels wasted). A first-timer stacks two Guilt, sees the enemy isn't converting yet, adds a third, watches them flip to a Fanatic that joins their next attack — the whole engine taught in one sequence. Sermon shows the harvest/payoff and wants a villain present.

**Echo-chamber immunity:** converts revert to *neutral*, so Faith Leader's engine never floods the row with permanent receptives → can't self-trigger the all-receptive penalty. (Still wants a hostile present on non-converting turns; a Hardened enemy is the ideal permanent villain.)

**Rebuild note (2026-11-21):** five Guilt cards compensated for having no Guilt source. With the relic supplying
Guilt, two or three are enough, and the freed slots can hint at Congregation and Inquisition.

Tuning flags are in §7.

### Legibility pass (the trim check)

The discipline isn't a card count — it's whether a new player can read the opening hand and know what to do. Quick self-check per deck:

- **Nepo Baby** — the **hardest class by design**, but the starter is legible: three familiar basics plus one seed whose payoff is printed on the card (Sway = the burned card's cost). The difficulty is in the reward pool and the Hostility clock, not the opening hand. ⚠️ watch Pull Rank: a first-timer may spam it and not connect the angrier room to it.
- **Celebrity** — three plain basics plus one Glamour seed whose effect shows on the meter every round. Legible. ⚠️ watch: does a first-timer notice Scrutiny stripping Glamour when hits leak?
- **Faith Leader** — the **any-3-statuses-to-convert** rule is countable and visible (player sees each enemy's stack climb), and each status does an obvious defensive job, so stacking never feels opaque. Legible. ✅ (watch: is the 3-setups-for-1-turn payoff *felt* as worth it? — tuning, not legibility)

**Rule going forward:** design each deck to its fantasy first, then run this legibility pass and trim only what a first-timer can't hold. Count follows from the verb, not the reverse.

### The "potential" layer (kept separate from starters)

Each class's reward pool is its three pillars plus shared glue (§7): **Faith Leader** Congregation, Inquisition, Martyr;
**Celebrity** Star Power, Leverage, Content Machine; **Nepo Baby** Inheritance, Owed, Pulling Strings. Starters only
teach the relic; rewards reveal the ceiling.

---

## 9. Starting relics *(2026-11-21)*

Each class's starting ally is its **starting relic**: the upfront mechanic from §7, active from the first turn. Allies
collected during a run are the accumulated relics that warp strategy (`allies.md`).

- **Faith Leader:** the first attack each turn applies 1 Guilt to its target (untargeted: a random enemy that plans to
  attack you, else any random enemy). Replaces the unresolved shield-bonus and hostility-reduction candidates.
- **Celebrity: Last Impressions.** The first Pressure card you play each turn uses its upgraded effect, that play only;
  generated cards don't count. Replaces the Publicist (enemies start Exposed) and the retired "first card each battle is
  played upgraded" passive, which is still hardcoded in `CardPlayController` and goes when this is built.
- **Nepo Baby:** Scry 1 at the start of your turn. Replaces the once-per-battle opening mulligan, which was judged too
  weak next to the per-turn relics.

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
- Relic design space (where build variety lives) → proposal in [`allies.md`](allies.md) (2026-11-06)
- ~~Finalizing Faith Leader's starter passive trigger~~ → her starting relic (§9, 2026-11-21)
- What receptive enemies grant (mirror to the hostile-draw reward)
- Whether the default hostility card is removable
- ~~Celebrity sub-archetype pools~~ → replaced by the Glamour / IOU build (2026-10-03).

### Recently resolved
- ~~Celebrity's resource system~~ → **CUT.** No unique resource; manufacturing fantasy lives in the card pool (see §7). Over-engineered and made Celebrity inaccessible / too close to StS2's Regent.
- ~~Celebrity's starter passive~~ → **LOCKED** for the old Celebrity: first card played each battle is upgraded. Reopened by the Glamour / IOU redesign; resolved 2026-11-07 as the Publicist (§9).
