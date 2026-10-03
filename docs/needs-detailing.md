# Needs Detailing — design questions awaiting a decision

*As of 2026-09-11. These are NOT build tasks — each needs a design call (and usually a playtest) before code. Ordered by how much they block.*

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
- **Scandal is currently built as its own opposite.** The design wants an anti-Curse you *want* — clogs the hand but powers your other cards per-Scandal. The authored Scandal cards are plain StS punishment curses, and they are tagged faithleader/universal rather than Celebrity. No card rewards carrying one. Decide whether the reward-per-Scandal line is real before authoring the pool; if it is, the existing curses are mis-tagged, not just unfinished.
- **Attention:** the "held too long → you become the target" penalty — auto rule or card-text-only? Currently deferred to tuning.
- **Drama King:** keep disarm framing distinct from FL debuffs (protect-while-attacking vs debuff-to-convert).

## 6. Starter deck quantities (blocks: matching the starter-decks doc)

Tag-driven starter collection gives 1 of each card; the doc wants repeats (e.g. 3× Rebuke). Detail the mechanism: quantity field on CardData? A starter-deck manifest asset per origin (probably cleaner — also solves the stale-IsStarterCard pollution)?

## 7. Status DB scope (blocks: nothing — decide during the re-key)

When re-keying `StatusEffectIconMapSO` by Id (the `StatusEffectIconMapSO` re-key): does it grow into the full "generic effects/statuses database" (SFX/VFX/category per status), or stay icon/color/text with audio-visual mapped elsewhere (BattleSoundMap pattern)? Decide once, during the re-key, to avoid touching the asset twice.

## 8. Nepo Baby open questions (blocks: Nepo build)

The class was redesigned (2026-10-03, `nepo-baby-class.md`): no Patronage, no summons. Open questions with their defaults are in that doc's section 8 (seed vs junk, "I'm Just Like You" type, whether replays raise Hostility, Hostility-scaled junk injection, Friends in High Places). Section 12 lists conflicts with current code, including what to do with the unused Patronage / summon code.

## 9. Meta-progression: achievements and unlocks (blocks: nothing yet — wanted eventually)

**Want:** persistent milestones across runs that unlock content — cards, allies, encounters.

**Already in the repo, all of it disconnected:**
- `CardData.IsUnlockable` + `CardDatabase.GetUnlockableCards()` + `CardSearchQuery.UnlockableCardsOnly`. Acquisition skips flagged cards (`CardDatabase.IsAcquirable`, `GenerateRewardOffer`), so a flagged card is never acquired until something unlocks it. Only Nepo Baby's Trust Fund is meant to start locked.
- `SaveData.unlockedCardIDs` / `unlockedLocationIDs` — written on new-save and cleared on reset, read by nothing. `unlockedLocationIDs` predates the encounter model; probably delete rather than repurpose.
- `CheatsManager.UnlockAllCards()` publishes `CheatUnlockAllCardsEvent`, which has no subscriber.

So the shape exists as three dead stubs, and the decision is what to make them mean.

**Proposed shape — three pieces, in build order:**

1. **A counter store, not an achievement engine.** Nearly every achievement is "count something that already fires on `EventBus`". A `Dictionary<string, int>` on `SaveData` plus one listener that increments named counters (`enemies_converted`, `runs_won_as_faithleader`, `day7_reached`) is the whole tracking layer. Achievements and unlocks both read it, so neither needs to know about the other.

   **Split the counting from the answering.** They look like one "UnlockManager" and aren't:
   - *Counting* needs a lifetime and `EventBus` subscriptions — a real object, `Singleton<T>` per convention, disposed the way `PassiveResolver` unsubscribes at battle end.
   - *Answering "is this unlocked?"* is a pure function over `SaveData` — a static helper with no lifetime, callable from `GetAcquirable()` **and from editor tooling with no game running**, which is what lets the Content Hub audit unlock reachability offline. Fuse the two and every query needs a live singleton.

2. **Three `UnlockCondition` subclasses, not a lot of them.** `[SerializeReference]` polymorphic, deliberately mirroring `RunRequirement`. "Convert 50 enemies", "win 3 runs as Faith Leader", "reach day 7", "beat the Incumbent" are *not* four conditions — they are one `StatAtLeast(key, n)` with four strings. Add `WonRunAs(origin)` because origin isn't a counter, and `HasUnlocked(otherId)` for chains, and the set is closed. A fifth subclass usually means a counter nobody incremented.

   So **the counter keys are the design surface, not the condition classes** — and that is the flag problem again: a string on one side, an `EventBus` listener spelling it on the other, nothing checking they match. Unlike encounter flags, designers don't invent counters (code does), so `const string` fields on the tracker beat a free-form key with an editor index.

3. **No fourth database.** Put an `UnlockCondition` field on the content asset itself — `CardData` (beside the existing `IsUnlockable`), `AllyData`, `EncounterData` — and gate at the acquisition chokepoints: `CardDatabase.GetAcquirable()` for cards, `EncounterPoolData.DrawForDay` for encounters. Cards funnel through one method, so that half is a two-line change once the condition type exists.

**Keep achievements and unlocks separate.** An achievement is a *named, displayed* milestone; an unlock condition is a *gate*. Most unlocks want no achievement card attached, and some achievements unlock nothing. Coupling them means every gate needs display copy. Both read the same counters.

**Decide before building:**
- **Does an unlock apply mid-run or from the next run?** Next-run-only is far simpler — resolve unlocks once at run start into `RunState` and nothing has to re-check mid-battle. Mid-run means every acquisition site re-reads the save.
- **Are encounters actually unlockable, or only cards and allies?** Locking encounters makes the pool a different shape per save file — the Encounter Designer's day-coverage strip and the schedule simulation both currently assume one fixed pool, and would need an "as unlocked by" toggle to stay honest.
- **What counts as a "run" for the counters** — any run started, or only ones played to a conclusion? Abandoned-run farming is the usual exploit.

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
