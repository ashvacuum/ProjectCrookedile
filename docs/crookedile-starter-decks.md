# Crookedile — Starter Decks (working draft)

> **Kind:** Design · **Status:** Draft · **Updated:** 2026-10-03
>
> **Summary:** Per-class starter decks and the reward-pool "potential" layer.
>
> **Source of truth:** this doc; built decks in [`OriginDatabase`](../Assets/Resources/Databases/OriginDatabase.asset) · **Related:** [`core-design.md`](core-design.md) · [`nepo-baby-class.md`](nepo-baby-class.md) · [`celebrity-glamour-iou.md`](celebrity-glamour-iou.md)

*Built on the StS model: each deck is mostly basic offense + basic defense (heavy repeats), one shared-role hostility card, and as many identity cards as the verb needs — in moderation, legibility over count. All numbers are placeholder; design the **relationships**, tune the magnitudes in play.*

## Shared basics (the "Strike / Defend" layer)

Every class has its own basic pair, mechanically near-identical, no flavor riders (StS approach). Across all three the two basics are functionally:

- **Basic offense** — 1e. Push the opinion meter in your favor (small). The "Strike."
- **Basic defense** — 1e. Gain opinion shield (small). The "Defend."

And every class carries **one default hostility card** (the universal echo-chamber escape valve, established in core doc §4):

- **Hostility card** — 1e. Make one receptive enemy non-receptive (seed hostility). Breaks/ prevents the echo chamber. *(Open: whether this is removable.)*

So each deck = ~4 offense + ~3 defense + 1 hostility + identity cards.

---

## Nepo Baby — *the glass cannon*
> **Full spec: `nepo-baby-class.md`.** Numbers are placeholders and live in the Nepo Baby config asset.

**Verb:** burn your own deck to get exactly what you want, now. The room pays the bill in Hostility. His resource is the deck itself: no Patronage, no summons, no second currency.

Follows the standard 3 pressure + 3 shield + 3 hostility-reducer + 1 seed shape, with a glass-cannon tilt: pressure pitched above the other classes, shields below.

### Starter deck

| Card | Qty | Type | Cost | Effect | Role |
|---|---|---|---|---|---|
| Name Drop | 2 | Pressure | 1e | Sway attack, above the Faith Leader/Celebrity baseline | basic offense |
| **Pull Rank** | 1 | Pressure | 0e | 4 Sway to target, then +1 Hostility on that enemy | **aggravator** (also the echo-chamber escape) |
| Daddy's Lawyer | 3 | Shield | 1e | Light Composure, below the other classes | basic defense |
| I'm Just Like You | 3 | Rhetoric | 1e | Reduce target Hostility. Converts to Receptive: draw 1. Otherwise: a random card in hand costs 1 less (floor 1) | hostility reducer |
| **Blow the Allowance** | 1 | Rhetoric | 1e | Burn a non-Policy card from hand, play it free, and deal Sway equal to its printed cost. Enhanced: 0e, damage doubled | **identity (seed)** |

**Passive:** once per battle, a full-hand Mulligan (discard hand, draw fresh).

**Why this set:** Blow the Allowance teaches the burn (the bigger the card, the bigger the swing). Pull Rank teaches the price: free damage now, enemy anger later, and thin shields to absorb it. It is a poor burn target (printed cost 0), which is intended. "I'm Just Like You" is Rhetoric so it feeds Old Boys' Club from fight one (open question 8.2).

**Known tuning risk (playtest):** Pull Rank against encounters that punish Hostility changes, and how fast a burn-heavy deck thins into Heckle/Scandal rot.

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

- **Nepo Baby** — the **hardest class by design**, but the starter is legible: three familiar basics plus one seed whose payoff is printed on the card (Sway = the burned card's cost). The difficulty is in the reward pool and the Hostility clock, not the opening hand. ⚠️ watch Pull Rank: a first-timer may spam it and not connect the angrier room to it.
- **Celebrity** — basics + Read the Room are dead simple; the three single-copy seed cards each gesture at a direction without forcing a decision. Risk isn't legibility of the *starter* — it's that the *class* asks the player to eventually commit to a direction, which a true beginner won't know to do. ⚠️ advanced class by nature, not the on-ramp (see §7).
- **Faith Leader** — the **any-3-statuses-to-convert** rule is countable and visible (player sees each enemy's stack climb), and each status does an obvious defensive job, so stacking never feels opaque. Legible. ✅ (watch: is the 3-setups-for-1-turn payoff *felt* as worth it? — tuning, not legibility)

**Rule going forward:** design each deck to its fantasy first, then run this legibility pass and trim only what a first-timer can't hold. Count follows from the verb, not the reverse.

---

## Next: the "potential" layer (kept separate from starters)

Once starters feel right, sketch the **subset of directions** each class's *reward pool* opens — explicitly NOT in the starter, so the two don't bleed:

- **Nepo Baby** — three lanes plus valves (full card list in `nepo-baby-class.md` section 6): **Burn** (Executive Privilege, Dynasty, Legacy Admission, Encore, Born Into It, Bail Out; Trust Fund as an unlock-gated Rare), **Pull and scan** paid in Hostility (Inside Information, Background Check, Call in a Favor, Special Order), a deliberately sparse **Return** lane (I Know a Guy, Heirloom, Family Seat, Hand-Me-Downs), a slow **Calm** lane capped at Neutral (Apology Tour, Smooth Things Over, Smooth Operator), and **valves** that each cost something (Skip the Line, VIP Access, Do-Over, Not My Problem). Cap valves per reward screen so he stays a glass cannon.
- **Celebrity** — the reward pool is the **widest in the game**, organized into three draftable sub-archetypes the player commits to over a run (see §7):
  - **Attention/Aggro** — cards that draw aggro and bank it, payoffs that spend banked attention as big meter hits (build-and-spend, tempo risk).
  - **Scandal** — Scandal-generators (and synergy with enemy-inflicted Scandals), per-Scandal-drawn payoffs (+shield etc.), per-Scandal-in-play payoffs (end-of-turn meter damage), and a **spin/cash-out** to clear Scandals for a burst (anti-Curse snowball, consistency risk, all-in). *Tuning: draw-severity gentle not punishing; on-draw vs in-play triggers.*
  - **Drama King** — sympathy/shield generators, disarm/enemy-weaken tools, grind payoffs (control, low risk). *Watch: disarm vs Faith Leader weaken — frame as self-protection, not conversion.*
  - Each line should be *coherent enough to commit to*; flexibility is **between** drafted archetypes, not mush within every card.
- **Faith Leader** — **multi-status-per-card** cards (apply 2 statuses at once, so conversion isn't always 3 turns — a priority), bigger **harvest payoffs** that scale off Fanatic bursts (Sermon, Crusade), over-stacking past 3 for a bigger burst, Preach-style hard-silence tools, cards that exploit the *defensive* side of statuses (e.g. punish a Shamed enemy harder). Status-interaction *texture* (Guilt+Shame combos differently) lives here, not in core.

This is where each class's *potential* lives. Starters only teach the verb; rewards reveal the ceiling.
