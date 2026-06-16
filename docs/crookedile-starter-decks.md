# Crookedile — Starter Decks (working draft)

*Built on the StS model: each deck is mostly basic offense + basic defense (heavy repeats), one shared-role hostility card, and as many identity cards as the verb needs — in moderation, legibility over count. All numbers are placeholder; design the **relationships**, tune the magnitudes in play.*

## Shared basics (the "Strike / Defend" layer)

Every class has its own basic pair, mechanically near-identical, no flavor riders (StS approach). Across all three the two basics are functionally:

- **Basic offense** — 1e. Push the opinion meter in your favor (small). The "Strike."
- **Basic defense** — 1e. Gain opinion shield (small). The "Defend."

And every class carries **one default hostility card** (the universal echo-chamber escape valve, established in core doc §4):

- **Hostility card** — 1e. Make one receptive enemy non-receptive (seed hostility). Breaks/ prevents the echo chamber. *(Open: whether this is removable.)*

So each deck = ~4 offense + ~3 defense + 1 hostility + identity cards.

---

## Nepo Baby — *the schemer*
**Verb:** burn the hand you were handed to fund borrowed power. **Distinct economy** — see below. Fantasy: unearned privilege; you don't build value, you spend down an inheritance of favors.

### The Patronage economy *(this class only)*
Nepo Baby is the **odd one out by design**. The other two classes are *energy-gated* (3 energy/turn is the throttle). Nepo Baby is **hand-gated**:

- **Most cards cost 0 energy** but cost **Patronage** instead. Energy is no object — doors open freely (the privilege fantasy). A few big plays still cost energy and/or extra Patronage to stay rare (double-gated).
- **Patronage** is generated *only* by a dedicated **generate card** that **sacrifices a card from your hand** (the "burn what you're handed" cost, à la Prime Monster's exploit-for-capital). **No free baseline burn** — generation is gated to drawing the card, so Patronage is something you plan around, not a panic lever.
- Therefore your real resource is **your hand itself**. Over-summon and you empty your hand: a big board, nothing left to play. The overload trap is baked into the economy, not bolted on.
- **Hand size is lifeblood.** Card draw matters more for this class than any other → draw/refill is a reward-pool priority. *(The mulligan starter passive — discard any number, redraw — reinforces this perfectly: it's "reshuffle the hand I was dealt," the whole class in one ability.)*

**Patronage buys three things:**
1. **Summon** — bodies in the row (one-time)
2. **Manipulate** — bend the room now: reduce hostility, flip a stance, shield (one-time)
3. **Install** — persistent engines that pay out every turn (ongoing) — *reward-pool material, see potential layer (Troll Farm etc.)*

### Starter deck

| Card | Qty | Energy | Patronage | Effect | Role |
|---|---|---|---|---|---|
| Push (offense) | 4 | 1e | — | Small opinion push | basic |
| Cover (defense) | 3 | 1e | — | Small opinion shield | basic |
| **Call in Patronage** | 2 | 0e | — | **Sacrifice a card from hand → gain Patronage** | **identity (generate)** |
| **Call a Favor** | 2 | 0e | spend P | **Summon a receptive ally** into the row | **identity (summon)** |
| Plant | 1 | 0e | spend P | Summon a **hostile** body | hostility / echo-chamber escape *(paid opposition — on-fantasy)* |

**Why this set:** the loop **burn → bank Patronage → summon** is the whole class, shown in three cards (generate, summon, the hostile-summon escape). Basics still cost energy so the player learns Patronage *gradually* against a familiar baseline rather than all at once. Plant doubles as echo-chamber escape AND "I bring my own villain." No manipulate card in the starter — keep it to the summon verb first; manipulation enters via rewards.

**Known tuning risk (playtest):** the death-spiral. Burning a card for Patronage *then* spending a card to summon = two cards spent per body. Without enough draw, the hand hollows out and stalls. Thematically perfect ("blew through daddy's favors") but needs the summoned board to generate enough value to justify the trade — especially since allies are fragile/corruptible. Tune: Patronage per generate, number of generate cards, draw availability.

---

## Celebrity — *the open canvas* (archetype-flexible)
**No fixed verb — and that's the point.** Celebrity's identity is *assembled during the run* by drafting into one of three sub-archetypes (Attention / Scandal / Drama King — see design doc §7). So the **starter can't teach one loop** — its job is to teach the **core fundamentals competently** (push, shield, manipulate) with a **sturdy floor** (never bricks), while planting **one seed of each direction** so the player feels the breadth and understands "this class becomes what I draft." Plays like the Silent (flow/adapt) with Ironclad's floor.

**This is the advanced/expressive class, NOT the beginner on-ramp** — versatility = maximum rope. (See §7.)

### Starter deck

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

---

## Faith Leader — *the converter*
**Verb:** stack statuses on an enemy to **pacify** them. **Engine (this class's spine):** stack statuses (Guilt/Shame/Doubt, any mix) to the **pacify threshold = 3 + Jaded stacks** → consumes them → converts. Normal enemy → **Fanatic for 1 turn** (pumps the opinion meter), then **reverts to neutral** and gains a **Jaded** stack. Hardened enemy → **silenced** instead. Relentlessly active — every turn you're stacking toward a conversion or cashing one in; no permanent emitters, and **Jaded** stops you milking one target (each re-conversion costs +1).

**Two status categories:** *pacify statuses* (Guilt/Shame/Doubt — you apply, count toward threshold, consumed on convert) vs. *threshold status* (**Jaded** — auto-applied on fanaticization, permanent, stacks, raises future cost, never consumed, doesn't count toward its own threshold). Jaded is visible on the enemy so conversion cost reads directly off the stack.

### The status kit
Each status blunts a specific enemy behavior *and* counts toward the 3-stack pacify threshold (the climb is self-protecting):

| Status | Blunts |
|---|---|
| **Guilt** (≈Weakened) | enemy's *push* on the meter (offense) |
| **Shame** | enemy's *shielding* (Rebuke/defense) |
| **Doubt** | enemy's *willingness to act* (soft, partial — distinct from Preach's hard silence) |

### Starter deck

| Card | Qty | Cost | Effect | Role |
|---|---|---|---|---|
| Rebuke (offense) | 3 | 1e | Small opinion push | basic |
| Pray (defense) | 2 | 1e | Small opinion shield **+ draw a card** | basic *(elevated — setup class needs hand fuel)* |
| Call Out Sin | 1 | 1e | Push one receptive enemy toward hostile | hostility / echo-chamber escape |
| **Guilt** | 2 | 1e | Apply Guilt (weakens push) + counts toward pacify | **identity (stacker)** |
| **Shame** | 2 | 1e | Apply Shame (drops shield) + counts toward pacify | **identity (stacker)** |
| **Doubt** | 1 | 1e | Apply Doubt (soft reluctance) + counts toward pacify | **identity (stacker)** |
| **Sermon** | 1 | 2e | Harvest: scales with Fanatic bursts / status converted this turn | **identity (payoff + villain-wanting)** |

**Why this set:** all three status types are present so the player learns the **any-3-to-convert** rule directly, and each status visibly does a *defensive* job too (so stacking never feels wasted). A first-timer stacks two statuses, sees the enemy isn't converting yet, adds a third, watches them flip to a Fanatic burst — the whole engine taught in one sequence. Sermon shows the harvest/payoff and wants a villain present.

**Echo-chamber immunity:** converts revert to *neutral*, so Faith Leader's engine never floods the row with permanent receptives → can't self-trigger the all-receptive penalty. (Still wants a hostile present on non-converting turns; a Hardened enemy is the ideal permanent villain.)

**Tuning flags:** payoff must be **generous** (3 setups → 1-turn burst is steep); needs **multi-status-per-card** cards in rewards so conversion isn't always 3 full turns. Hard counter: a Hardened-heavy row starves the class.

*(Note: dropped the old "Absolution requires Guilt" card — the pacification engine now teaches "payoff requires setup" structurally, so a dedicated gated-payoff card is redundant in the starter.)*

---

## Legibility pass (the trim check)

The discipline isn't a card count — it's whether a new player can read the opening hand and know what to do. Quick self-check per deck:

- **Nepo Baby** — now the **heaviest to learn**, not the easiest: it carries a unique two-currency economy (energy + Patronage) and a sacrifice mechanic. Mitigated by teaching gradually (basics stay energy-only; the loop is just burn→summon). Still legible as three clean steps, but flag it as the class most likely to confuse a true first-timer. ⚠️ watch (was "easiest" under the old summon-only model)
- **Celebrity** — basics + Read the Room are dead simple; the three single-copy seed cards each gesture at a direction without forcing a decision. Risk isn't legibility of the *starter* — it's that the *class* asks the player to eventually commit to a direction, which a true beginner won't know to do. ⚠️ advanced class by nature, not the on-ramp (see §7).
- **Faith Leader** — the **any-3-statuses-to-convert** rule is countable and visible (player sees each enemy's stack climb), and each status does an obvious defensive job, so stacking never feels opaque. Legible. ✅ (watch: is the 3-setups-for-1-turn payoff *felt* as worth it? — tuning, not legibility)

**Rule going forward:** design each deck to its fantasy first, then run this legibility pass and trim only what a first-timer can't hold. Count follows from the verb, not the reverse.

---

## Next: the "potential" layer (kept separate from starters)

Once starters feel right, sketch the **subset of directions** each class's *reward pool* opens — explicitly NOT in the starter, so the two don't bleed:

- **Nepo Baby** — Patronage-funded **installations** (signature category — *"buy a corrupt institution that works for you"*): **Troll Farm** (each turn: push the meter / suppress a hostile voice — manufactured online consensus), plus a captured news outlet, a bought official, a fake grassroots movement, etc. Persistent engines need a **leash** so they don't trivialize encounters — preferred: **upkeep Patronage each turn** to keep running (a troll farm needs funding — on-fantasy), or limited duration, or shut-down by enemy "exposé" intents. Also: ally protection (Cover Story), ally-payoff scaling, breaking Hardened, **draw/refill effects** (hand is lifeblood — high priority), manipulate cards (spend Patronage to bend the room). *Note: installations may render damage as opinion-meter pressure / voice suppression, since there is no enemy HP.*
- **Celebrity** — the reward pool is the **widest in the game**, organized into three draftable sub-archetypes the player commits to over a run (see §7):
  - **Attention/Aggro** — cards that draw aggro and bank it, payoffs that spend banked attention as big meter hits (build-and-spend, tempo risk).
  - **Scandal** — Scandal-generators (and synergy with enemy-inflicted Scandals), per-Scandal-drawn payoffs (+shield etc.), per-Scandal-in-play payoffs (end-of-turn meter damage), and a **spin/cash-out** to clear Scandals for a burst (anti-Curse snowball, consistency risk, all-in). *Tuning: draw-severity gentle not punishing; on-draw vs in-play triggers.*
  - **Drama King** — sympathy/shield generators, disarm/enemy-weaken tools, grind payoffs (control, low risk). *Watch: disarm vs Faith Leader weaken — frame as self-protection, not conversion.*
  - Each line should be *coherent enough to commit to*; flexibility is **between** drafted archetypes, not mush within every card.
- **Faith Leader** — **multi-status-per-card** cards (apply 2 statuses at once, so conversion isn't always 3 turns — a priority), bigger **harvest payoffs** that scale off Fanatic bursts (Sermon, Crusade), over-stacking past 3 for a bigger burst, Preach-style hard-silence tools, cards that exploit the *defensive* side of statuses (e.g. punish a Shamed enemy harder). Status-interaction *texture* (Guilt+Shame combos differently) lives here, not in core.

This is where each class's *potential* lives. Starters only teach the verb; rewards reveal the ceiling.
