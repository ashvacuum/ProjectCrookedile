# Needs Detailing — design questions awaiting a decision

*As of 2026-06-10. These are NOT build tasks — each needs a design call (and usually a playtest) before code. Execution tasks live in `work-now.md`. Ordered by how much they block.*

---

## 1. Receptive enemy bonus (blocks: nothing structural, but current one is wrong)

**Current:** +1 Support per receptive enemy at player turn start. **Problem:** passive, invisible during play, and it rewards stacking receptives with the same defensive currency the Echo Chamber then punishes — the reward and the trap pull on the same axis, muddying the "controlled tension" read.

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
- **Attention:** the "held too long → you become the target" penalty — auto rule or card-text-only? Currently deferred to tuning.
- **Drama King:** keep disarm framing distinct from FL debuffs (protect-while-attacking vs debuff-to-convert).

## 6. Starter deck quantities (blocks: matching the starter-decks doc)

Tag-driven starter collection gives 1 of each card; the doc wants repeats (e.g. 3× Rebuke). Detail the mechanism: quantity field on CardData? A starter-deck manifest asset per origin (probably cleaner — also solves the stale-IsStarterCard pollution)?

## 7. Status DB scope (blocks: nothing — decide during the re-key)

When re-keying `StatusEffectIconMapSO` by Id (work-now §1): does it grow into the full "generic effects/statuses database" (SFX/VFX/category per status), or stay icon/color/text with audio-visual mapped elsewhere (BattleSoundMap pattern)? Decide once, during the re-key, to avoid touching the asset twice.

## 8. Nepo Baby leash (blocks: Nepo roster/deck depth)

"Summoned allies are the *most* corruptible" is the signature fear, but nothing detailed: are summons extra-vulnerable to Sway? Higher Turncoat damage? Do Plants (hostile summons) count as your villain for echo-chamber purposes (they should — confirm)? The Hardened-breaking "daddy knows people" card — core or reward pool?

## 9. Deferred wholesale (don't detail yet)

**(2026-07-02) Partially un-deferred:** the campaign metagame + relic runtime are now planned
in `metagame-campaign.md` (Potionomics-style free-roam map, campaign HQ, hour budget, event
nodes; relics from bosses + events; reward-quality scaling in v1). Its ⚑ open questions live
there. Still deferred:
- Viral moments / News Cycle track.
- Production resource HUD (debug overlay suffices for playtesting).
- `EnemyConvertedEvent` bespoke flourish/animation.
