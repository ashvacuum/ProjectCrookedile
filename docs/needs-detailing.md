# Needs Detailing — design questions awaiting a decision

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-10-03
>
> **Summary:** Design questions awaiting a call, ordered by how much they block. Read before authoring content.
>
> **Source of truth:** this doc · **Related:** [`core-design.md`](core-design.md)

*As of 2026-10-03. These are NOT build tasks — each needs a design call (and usually a playtest) before code. Ordered by how much they block.*

---

## 1. Receptive enemy bonus (blocks: nothing structural)

**Ruled 2026-09-29** (`core-design.md` §3): receptive enemies pay in Support — **+2 Support the moment an enemy turns
receptive, +1 per receptive enemy at the start of your turn** (both on `BattleManager`). The concern below still
stands and is worth a playtest before more is built on it.

**Before the ruling:** +1 Support per receptive enemy at player turn start. **Problem:** passive, invisible during play, and it rewards stacking receptives with the same defensive currency the Echo Chamber then punishes — the reward and the trap pull on the same axis, muddying the "controlled tension" read.

Directions from the core doc (§3) worth detailing:
- **Reduced card cost** while ≥N receptives present ("reading the room") — active, felt every hand.
- **Amplified meter swings** — receptives as a megaphone; pairs dangerously (interestingly?) with echo-chamber halving.
- Per-archetype flavors instead of one global rule.

Decide: global vs per-archetype; turn-start vs continuous; and whether the bonus should *deliberately* taper as the room approaches all-receptive (telegraphing the chamber).

## 2. Fanatic burst — what does a Fanatic actually DO? (blocks: FL deck authoring)

**Locked intent:** FL cards apply statuses and deal little/no direct pressure; the *Fanatics deal the pressure*. The converted enemy is the damage dealer for 1 turn, then reverts.

**Current code:** conversion pays an instant `RaiseDirect` burst (consumedStacks × 3) at the moment of conversion. That's "conversion deals the damage," not "the Fanatic deals it." To match intent, detail:
- Does the Fanatic get a **turn** — an intent that fires during the enemy phase, pumping the meter (visible, on-fantasy, but delayed and disruptable)?
- Or keep the instant burst and treat "Fanatic for 1 turn" as flavor + the Fanatic hostility-flag window?
- If the Fanatic acts in the enemy phase: can enemies interfere (silence/stun your own convert)? Does Guilt-on-the-convert weaken its push *for* you?
- Burst math: with cards applying 1–2 statuses each, conversion costs ~2–3 plays. Define the target payoff vs. a plain pressure card per energy, and how over-stacking scales.
- Sermon/harvest cards scaling off `ConversionsThisTurn` — pool sketches.

## 3. Echo-chamber escape valve (blocks: starter deck finalization)

Every starter must include a default hostility card. Open: is it **un-removable** so deck-thinning can't re-create the trap? Decide before reward/removal systems are built, since removal UI needs to know.

## 4. Intent vocabulary (blocks: enemy roster authoring)

Code uses `EnemyMoveType` (Attack/Defend/DefendOpinion/RileOthers/...); the design doc uses Rally/Rebuke/Sway/Condemn/Murmur. Pick one vocabulary before authoring 6–8 enemies, or every enemy asset gets touched twice. Also detail Sway (convert receptive→hostile) and Murmur (low-impact presence) — both specified, neither has a concrete effect list.

## 5. Celebrity sub-archetype card pools (blocks: Celebrity playtests, not FL/Nepo)

Each of Attention / Scandal / Drama King needs enough cards to be *committable* (~8–12 each, per the "coherent mini-archetypes, not oatmeal" rule). Specific opens:
- **Scandal:** severity-when-drawn; on-draw vs in-play triggers (pool can have both — ratio?); removal beyond the spin/cash-out.
- **Scandal is currently built as its own opposite.** The design wants an anti-Curse you *want* — clogs the hand but powers your other cards per-Scandal. The authored Scandal cards are plain StS punishment curses, and they are tagged faithleader/universal rather than Celebrity. No card rewards carrying one. Decide whether the reward-per-Scandal line is real before authoring the pool; if it is, the existing curses are mis-tagged, not just unfinished.
- **Attention:** the "held too long → you become the target" penalty — auto rule or card-text-only? Currently deferred to tuning.
- **Drama King:** keep disarm framing distinct from FL debuffs (protect-while-attacking vs debuff-to-convert).

A separate **Glamour / IOU** build exists as a playable variant (`celebrity-glamour-iou.md`, 25 card assets), to test an
alternative against these three directions rather than to replace them.

## 6. Starter deck quantities — resolved

`OriginDatabase` holds an authored starter deck per origin (card + count), and `CardDatabase.GetStarterDeck` uses it
before falling back to tag collection. Faith Leader and Nepo Baby use authored decks; Celebrity's entry is still empty,
so it falls back to tagged starters (one of each).

## 7. Status DB scope (blocks: nothing — decide during the re-key)

When re-keying `StatusEffectIconMapSO` by Id (the `StatusEffectIconMapSO` re-key): does it grow into the full "generic effects/statuses database" (SFX/VFX/category per status), or stay icon/color/text with audio-visual mapped elsewhere (BattleSoundMap pattern)? Decide once, during the re-key, to avoid touching the asset twice.

## 8. Nepo Baby open questions (blocks: Nepo build)

The class was redesigned (2026-10-03, `nepo-baby-class.md`): no Patronage, no summons. Open questions with their defaults are in that doc's section 8 (seed vs junk, "I'm Just Like You" type, whether replays raise Hostility, Hostility-scaled junk injection, Friends in High Places). Section 12 lists the conflicts with the old code and how each was resolved; Patronage is removed, and the summon effect stays for enemy content.

## 9. Meta-progression: achievements and unlocks

Folded into **`meta-progression.md`** (profiles, saves, unlocks, achievements, campaign hooks, Steam). The shape
proposed here (counters split from answering, unlock conditions on the content asset, achievements separate from
unlocks, next-run unlocks) carried over.

## 10. EncourageSides — no agreed meaning

Specced as a move ("encourage others to switch sides") and never defined: does it defect an ally
toward the player, flip hostility, or pull the meter directly? No `EnemyMoveType` value, no
effect, no asset — deliberately. Decide the rule before anything is built; each reading needs a
different effect.

## 11. CardType colour taxonomy carries no meaning as authored

Pressure (green, persuade) vs Rhetoric (red, aggressive) is applied at random: draw-only cards
sit in Rhetoric, and pressure is split across both. The colours are load-bearing for the player
read and for `Silenced` (= "no Rhetoric"), so they need a consistent rule — or the split needs to
stop pretending to be one.

## 12. Deferred wholesale (don't detail yet)

**(2026-07-02) Partially un-deferred:** the campaign metagame + relic runtime are now planned
in `metagame-campaign.md` (Potionomics-style free-roam map, campaign HQ, hour budget, event
nodes; relics from bosses + events; reward-quality scaling in v1). Its ⚑ open questions live
there.

**(2026-09-18) Map form decided:** the overworld is a **2:1 isometric sprite city**
(`metagame-campaign.md` §1.5, asset spec + generation prompt in `art-bible.md` §9).
Production overworld art is still deferred — greybox tiles are fine for playtests — but the
*shape* is no longer an open question, so don't re-litigate it here. Still deferred:
- Viral moments / News Cycle track.
- Production resource HUD (debug overlay suffices for playtesting).
- `EnemyConvertedEvent` bespoke flourish/animation.
- Overworld building art beyond greybox (the ≈20–40 sprites §1.5 calls for).
