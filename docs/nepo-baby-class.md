# Nepo Baby: Burn / Return / Calm class spec (v0.1)

**Status: ideation locked, not built. Numbers are placeholders.** This spec replaces the Patronage / summon design in
`core-design.md` section 7 and `crookedile-starter-decks.md` ("Nepo Baby: the schemer"). Those sections stay until this
is built; section 12 lists every place they and the code disagree with it.

Vocabulary: **Composure** = Support (the shield). **Sway** = a push on the Opinion meter. Statuses, not keywords.

## 0. How to use this doc

Implement the Nepo Baby class as data-driven content on top of the existing combat system. Every number marked
(placeholder) lives in the config asset (section 9), never hardcoded. Anything under "Open questions" (section 8) is NOT
decided: implement the stated default behind a config flag. Before changing any existing behavior, list conflicts with
current code (section 12).

## 1. Existing system to respect (do not change)

- Combat is single-axis: one shared **Opinion meter** per encounter (enemies push down, the player pushes up). Zero =
  loss, max = win. No player HP in combat. Credibility lives on the metagame layer.
- **Sway** = push on the Opinion meter in the player's favor. **Composure** = defensive resource that absorbs incoming
  Opinion damage (existing Composure rules).
- Each enemy has **Hostility** and a state: Hostile / Neutral / Receptive. Hostile = stronger (see 12.A2), Receptive =
  weaker moveset. If ALL enemies are Receptive at once, the echo chamber decays Opinion. Every encounter has more than
  one enemy.
- Card types: Pressure, Rhetoric, Policy, Scandal, Heckle. Rarity: Basic / Enhanced / Rare. **Policies are pole
  anchors**, Rares are reserved for build-declaring cards.
- Starter decks are standardized: 3 pressure + 3 shield + 3 hostility-reducer + 1 class seed card = 10 cards. Do not
  touch the Faith Leader or Celebrity starters.
- Encounter length targets: standard 3-5 turns, elite 7-8, boss 9-10. Enemy-side summoning exists, player-side summoning
  does not. Statuses are the only state-guard language.
- Roster relevant to Nepo: Encounter 1 "First Impressions" (teaches convert early or bleed Opinion), Encounter 2
  "Heckler", Encounter 3 "The Receptive Trap" (Bodyguard punishes Hostility changes), elite "Ardent Fan". (See 12.A5.)

## 2. Class fantasy

Someone who has never been told no. He relishes spending the family's resources (his own deck) to get exactly what he
wants, and the whole room pays the bill (Hostility). He is a **glass cannon**: best-in-class damage and access, thin
defense, and a clock set by the room's anger. Design goal: the **hardest class and the most mechanically different**.
His resource is the deck itself, not a status.

Contrast with Celebrity (do not blur): Celebrity **creates** cards (Soundbites, tokens, empty promises). Nepo
**consumes** cards (burns, pulls, replays).

## 3. Class structure

Three lanes plus a valve package. Each lane has its own verb and its own brake, so the class is not "exhaust
everything".

- **Burn (exhaust from hand):** the high-ceiling engine. His cards are mostly cost 2-3, so burning one is a big swing
  and a double play is a worthy opener. It thins the deck, so it is the only lane that Heckle/Scandal rot punishes.
- **Return (cards come back to hand):** powerful but sparse. Only a small slice of the pool returns to hand (at most
  ~15% of the pool, see config). Brake: in-turn cost increase.
- **Calm (Hostility control):** the low-damage slow burn. Small hits, steady Hostility reduction, payoffs for a room
  with no Hostile enemies. Wins elites and bosses by outlasting. Nothing in this lane should burst.
- **Valves (the old "Fixer" pole, now a package, not a pole):** insurance against bad hands. Scry, skip, cycle,
  redirect. **Every valve costs something** (Hostility, energy, or deck cleanliness) so he is recoverable, never safe.

### Cost model
- **Burning is paid in cards** (the deck thins).
- **Pulling and scanning is paid in Hostility** (the room gets angrier).
- Hostility is a **pure price and clock**. Do NOT add payoffs that reward Hostility (that is the Faith Leader's
  Martyr/Agitator pole).

### Hard guardrails
- **vs Faith Leader:** no stacking statuses on enemies, no deep conversion. Calming caps at Neutral (cannot push an
  enemy to Receptive). He may convert incidentally via reducers but is never rewarded for the all-Receptive state.
- **vs Celebrity:** no token generators, no "add X cards to hand" engine. Nepo fetches, burns, replays, and retrieves.
- **Policy cards cannot be exhausted from hand as a cost, anywhere.** One flat rule, no per-card exceptions.
- No new card keywords beyond existing vocabulary plus the terms in section 4.

## 4. Terms and rules

- **Burn:** exhaust a card from hand as a cost. Policies cannot be burned.
- **Pull:** take a chosen card from the draw pile into hand.
- **Scry N:** look at the top N cards of the draw pile, discard any, reorder the rest.
- **Replay:** play a card an extra time from one play (Executive Privilege, Dynasty, Encore). A replay is not a second
  play for any "first card each turn" effect.
- **Printed cost:** a card's base energy cost, ignoring discounts. Burn effects read printed cost.
- **Heckle / Scandal (enemy junk):** Heckles are light (exhaust normally, burnable as fuel). Scandals are heavy and stick
  in the deck until removed by dedicated cleanup. Proposal, flagged in config.

## 5. Starter deck (10 cards, placeholders)

| Qty | Card | Type | Cost | Effect |
|---|---|---|---|---|
| 2 | Name Drop | Pressure | 1 | Sway attack. Pitched above Faith Leader/Celebrity baseline (config multiplier) |
| 1 | **Pull Rank** (aggravator) | Pressure | 0 | Deal 4 Sway to target, then raise that enemy's Hostility by 1. Single target |
| 3 | Daddy's Lawyer | Shield | 1 | Composure, light. Pitched below other classes' (config multiplier) |
| 3 | I'm Just Like You | Hostility-reducer | 1 | Reduce target Hostility by a flat amount. If it converts the target to Receptive, draw a card, otherwise reduce a random card in hand's cost by 1 (floor 1) |
| 1 | **Blow the Allowance** (seed) | Rhetoric | 1 | Burn a non-Policy card from hand. Play it for free, and deal Sway equal to its printed energy cost. Enhanced: cost 0 and damage doubled |

- **Passive:** once per battle, a full-hand Mulligan (discard hand, redraw fresh).
- Seed rulings: printed cost, not current cost. The seed goes to discard normally so it recurs. A burned card played
  this way is a play for triggers (Old Boys' Club) but does NOT add Hostility by itself. A card with no valid play (some
  junk) cannot be targeted (open question 8.1).
- Pull Rank teaches the class: free damage now, enemy anger later, thin shields to absorb it. It is a poor burn target
  (printed cost 0), which is intended.
- "I Know a Guy" moves OUT of the starter and into the pool (section 6).

## 6. Reward pool (first batch, all placeholders)

### Burn lane and extra plays
- **Executive Privilege** (Policy, persistent): once per turn, burn a card to play your next card twice.
- **Dynasty** (Policy, Rare, persistent): after a double play, burn another card to play it a third time. The third
  play raises Hostility on all enemies, escalating (reuse the escalating scale from "I Have All the Cards").
- **Legacy Admission** (Policy, one-shot, 0, exhaust): burn a card; your next two cards cost 0 this turn.
- **Trust Fund** (Policy, Rare, persistent, 3): whenever a replay of a Rhetoric card resolves, gain 1 energy and raise
  Hostility on all enemies by 1. Locked behind a future unlock system (section 7).
- **Encore** (Rhetoric, 1, exhaust): replay the last non-Policy card you played; raise Hostility on all enemies by 1.
- **Born Into It** (Pressure, 0, exhaust): deal Sway per card exhausted this combat, with a config cap.
- **Inheritance** (Rhetoric, 1): burn up to 2 cards from hand; gain Composure per card burned.
- **Spin Doctor** (Rhetoric, 1): burn a Heckle or Scandal from hand and lower target Hostility by 1.
- **Golden Parachute** (Policy, one-shot, 1, exhaust): burn a Heckle or Scandal from hand and draw 2.
- **Silver Spoon** (Policy, persistent): at the start of your turn, if your hand has no Heckles or Scandals, gain 1
  energy.
- **Old Boys' Club** (Policy, persistent, Enhanced, 1): the first Rhetoric card you play each turn refunds 1 energy, up
  to the energy actually paid (0-cost cards give nothing). Replays do not count and never trigger it.

### Pull and scan (paid in Hostility)
- **I Have All the Cards** (Pressure/Arsenal capstone): pull any number of cards from the deck; each pull raises
  Hostility on all enemies, with per-pull cost scaled by Fibonacci. (Not in the repo yet, see 12.A6.)
- **Inside Information** (Policy, persistent): at the start of each turn, look at the top 5 of your draw pile and take
  one; raises Hostility slightly each turn.
- **Background Check** (Policy, one-shot, 0, exhaust): look at the top 5, take one, put the rest back in any order.
- **Call in a Favor** (Policy, one-shot, 1, exhaust): take any card from the draw pile; it costs 0 this turn; raises
  Hostility.
- **Special Order** (Rhetoric, 1): take any card with printed cost 2+ from the draw pile; raise Hostility on all enemies
  by 1.
- **Fast Track** (Pressure, 1): 8 Sway, or 12 if you pulled a card this turn.
- **Hush Money** (Pressure, 2): 16 Sway; raise Hostility on all enemies by 1.

### Return lane (sparse by design)
- **I Know a Guy** (Rhetoric, 1): retrieve the last Rhetoric card played from the discard. Enhanced: any Rhetoric card,
  player's choice. (No extra exhaust on the retrieved card. The in-turn cost rule is the brake.)
- **Heirloom** (Pressure, 2): deal 10 Sway, then return this card to hand. It costs 1 more for the rest of the turn.
- **Family Seat** (Shield, 2): gain 8 Composure, then return this card to hand at +1 cost this turn.
- **Hand-Me-Downs** (Policy, persistent): once per turn, a card with printed cost 2+ that would go to discard returns to
  hand instead; raise Hostility on all enemies by 1.
- **Stacked Deck** (Rhetoric, 1): put up to 2 cards from hand on top of the draw pile.
- **Hand Me Down** (Pressure, 1): deal Sway per card discarded this turn.

### Calm lane (low damage, slow burn)
- **Apology Tour** (Rhetoric, 1): lower target Hostility by 2, not below Neutral.
- **Smooth Things Over** (Rhetoric, 2): lower ALL enemies' Hostility by 1, not below Neutral. Expensive on purpose;
  best against full Hostile rooms.
- **Smooth Operator** (Policy, persistent, Enhanced, 1): at the start of your turn, if no enemy is Hostile, draw 1 extra
  card and the first card you play costs 1 less.

### Valves (every valve costs something)
- **Skip the Line** (0, exhaust): scry 3, discard any of them, draw 1; raise Hostility on all enemies by 1.
- **VIP Access** (Policy, persistent, Enhanced, 2): at the start of each turn, scry 1.
- **Pull Strings** (0): draw 2, then discard 1. Not exhaust.
- **Do-Over** (2, not exhaust): discard your hand and draw that many cards; raise Hostility on all enemies by 1. A paid,
  weaker Mulligan, so the passive stays valuable.
- **Bail Out** (0, exhaust): burn up to 3 cards from hand; gain 2 energy per burned card with printed cost 2+.
- **That Was Supposed to Be Mine** (2): remove a buff from target enemy.
- **Not My Problem** (1, once per turn): move up to 2 Hostility from one enemy to another.
- **I Know You Are But What Am I** (1): this turn, the first time Opinion damage gets through Composure, gain Sway equal
  to that damage.
- **Hush Fund** (1, exhaust): gain 3 Composure per card in your exhaust pile, capped (config).
- **Damage Control** (2, exhaust): set all Hostile enemies to Neutral; add a Heckle to your discard pile.

Spread valves across card types. Do not make them mostly Rhetoric (it would make Old Boys' Club trigger trivially).

## 7. Rare gating and unlocks

Rarity ladder stays Basic / Enhanced / Rare. **Trust Fund** is a plain Rare. A future unlock system will gate it. The
card asset carries a locked-until-unlocked flag so the reward pool skips locked cards (see 12.B4: `CardData` already has
`_isUnlockable`). No new rarity tier, no unique flag.

## 8. Open questions (implement default, flag in config)

1. **Can the seed target Heckle/Scandal?** Default: no (must have a valid play). Alternative: yes, as a free play with
   its downside, which would make the seed answer rot alone.
2. **"I'm Just Like You" card type:** default Rhetoric (feeds Old Boys' Club from fight one). Alternative: Pressure.
3. **Skip the Line:** valve card only (default) vs also a persistent Policy (VIP Access is the persistent version,
   include both).
4. **Does a replay also raise Hostility by default?** Default: no, except cards that say so (Encore, Dynasty, Trust
   Fund).
5. **Rot link to Hostility:** Hostile enemies inject Heckles faster and calmed ones slower. Default off, flag
   `junkInjectionScalesWithHostility`.
6. **"Friends in High Places"** (on Hostile-to-Receptive conversion, discount the next card) has no home in the new
   structure. Default: unimplemented. Decide whether it becomes an optional pool Policy.
7. **Scry discarding junk for free:** default free within Skip the Line (the Hostility price is the cost). Paid removal
   stays on Spin Doctor and Golden Parachute.

## 9. Config (ScriptableObject)

The tunable config is a ScriptableObject asset, editable in the Inspector with no code change. Follow the project's
conventions when building it (`_camelCase` private `[SerializeField]` fields with `[Tooltip]`, read-only properties,
`Crookedile/...` menu path). All values are placeholders.

| Group | Field | Default |
|---|---|---|
| Glass-cannon asymmetry (vs Faith Leader baseline) | pressureSwayMult | 1.15 |
| | shieldComposureMult | 0.8 |
| Starter | pullRankSway | 4 |
| | pullRankHostilityGain | 1 |
| | seedBasicCost | 1 |
| | seedEnhancedCost | 0 |
| | seedSwayPerPrintedCost | 1 |
| | seedEnhancedDamageMult | 2 |
| | seedCanTargetJunk | false (8.1) |
| | imJustLikeYouIsRhetoric | true (8.2) |
| Rules | policiesBurnable | false (decided) |
| | replaysRaiseHostilityByDefault | false (8.4) |
| | junkInjectionScalesWithHostility | false (8.5) |
| | scandalsStickInDeck | true |
| Old Boys' Club | oldBoysClubRefund | 1 |
| | refundCappedAtEnergyPaid | true |
| Burn / payoffs | bornIntoItSwayPerExhausted | 3 |
| | bornIntoItSwayCap | 24 |
| | hushFundComposurePerExhausted | 3 |
| | hushFundComposureCap | 15 |
| | bailOutEnergyPerCostlyCard | 2 |
| | bailOutMinPrintedCost | 2 |
| | bailOutMaxCards | 3 |
| Trust Fund (Rare, unlock-gated) | trustFundEnergyPerReplay | 1 |
| | trustFundHostilityPerTrigger | 1 |
| Return lane | returnInTurnCostIncrease | 1 |
| | maxReturnShareOfPool | 0.15 |
| Valves | skipTheLineScry | 3 |
| | skipTheLineHostilityGain | 1 |
| | doOverHostilityGain | 1 |

Card definitions are data-driven and read these values (see 12.B1 on how that fits the card-asset model).

## 10. Testing

The auto-play bots (`Editor/Playtest/PlaytestRunner`) lean heavily on Hostility and rarely play for hand control, so
they are a poor fit for this class. Use smarter bots or human playtests, and set Nepo's target win rate **lower** than
the other classes (hardest class by design).

- **Encounter 1:** Pull Rank pushes against "convert early or bleed Opinion". Human-playtest it first.
- **Encounter 3:** the Bodyguard punishes Hostility changes, and Pull Rank is a Hostility change. Confirm it teaches
  restraint and does not feel unfair.
- **Combo checks, in order:** Executive Privilege + Dynasty + Trust Fund + I Know a Guy; Encore + Executive Privilege;
  Special Order + Old Boys' Club; Born Into It in long fights; Pull Rank spam with replays.
- **Rot:** measure deck cleanliness over a long elite or boss fight with the Burn lane. Burn decks should be hurt by
  Heckle/Scandal rot, Return and Calm decks much less.
- **Valves:** cap how many are offered per reward screen. With three or four valves in the deck he stops being a glass
  cannon.
- **Trust Fund:** test in a separate run with it forced into the pool, so the combo is measured without distorting the
  baseline win rate.

## 11. Deliverables

1. Nepo Baby config ScriptableObject and the card assets for sections 5-6, all reading the config.
2. New effect implementations: Burn (exhaust from hand as cost), Pull, Scry, Replay, Return-to-hand, top-of-draw
   placement, "once per turn" Policy gating.
3. The Policy-cannot-be-burned rule enforced at the Burn cost check.
4. The locked-until-unlocked flag and reward-pool filtering.
5. Nepo Baby starter deck and the once-per-battle Mulligan passive.
6. A test report per section 10.
7. The conflict list below, resolved **before** changing existing behavior.

## 12. Conflicts with existing docs and code

Audited 2026-10-03. Nothing below has been changed; each item needs a ruling before implementation touches it.

### A. Design docs (docs win over code, so these need a decision first)

1. **The whole class identity.** `core-design.md` section 7 and `crookedile-starter-decks.md` define Nepo Baby as the
   Patronage / summon class ("Who can I bring in?", summons receptive allies, Plant summons a hostile, fear = his own
   allies turning Turncoat). This spec says player-side summoning does not exist and makes the deck itself the resource.
   `core-design.md` section 8 ("teaches the Patronage loop") and `needs-detailing.md` section 8 ("Nepo Baby leash")
   are built on the old design. Ruling needed: this spec supersedes them, and those sections get rewritten.
2. **Hostile = Strength buff.** Section 1 says Hostile grants a passive Strength buff. `core-design.md` section 3
   (ruling 2026-09-29) says hostility sets behaviour, never numbers: stance picks which authored move list an enemy
   draws from, and the old hostility damage multiplier was removed. The spec's "Hostility is a clock" still works under
   the ruling, but no Strength buff should be added.
3. **Echo chamber.** Section 1 says all-Receptive decays Opinion. Canon (`core-design.md` section 4, `CrowdReactions`)
   is decay **and** halved gains. The spec's wording is incomplete, not a change.
4. **Starter passive.** `core-design.md` section 9: start of battle, discard any number of cards and redraw that many.
   This spec: once per battle, a full-hand Mulligan at any time. The existing `Nepotism` passive asset is neither: the
   first card played each battle reduces a random card's cost by 1.
5. **Roster names.** "First Impressions", "The Receptive Trap", the Bodyguard and "Ardent Fan" are not in
   `enemy-design-bible.md`, `campaign-encounters.md` or `Assets/Data`. Only the Heckler exists. Section 10's encounter
   tests cannot run until these exist or are mapped onto current encounter IDs.
6. **"I Have All the Cards" is called existing.** No card, effect or doc by that name exists, and no Fibonacci cost
   scale exists. Dynasty reuses that scale, so it has to be built first.
7. **Hardened.** `core-design.md` section 3 says Nepo Baby "may be able to break Hardened". This spec doesn't mention
   it. Treat as dropped unless re-added.
8. **Heckle exit rule.** Section 4 says Heckles "exhaust normally". `enemy-design-bible.md` D13 leaves the Heckle exit
   rule open (exhaust on draw/play vs end-of-encounter purge). Today a Heckle is just a card: it discards unless
   authored with an exhaust effect.

### B. Code

1. **Config ScriptableObject vs card-asset numbers.** Card numbers live on each `CardData` asset, inside its
   `[SerializeReference]` `BattleEffect` list (e.g. `ApplyOpinionEffect` amount). Neither existing class has a
   per-class tuning asset: Faith Leader is pure card assets plus `PacifyConversionEngine`; Celebrity's shared rules are
   static constants in `CelebrityRules`. Having cards read a config asset means either effects that look values up by
   key, or an editor step that writes config values into the card assets. Proposal: the config holds the class rules
   and multipliers (flags, caps, `policiesBurnable`, `returnInTurnCostIncrease`), and per-card numbers stay on the card
   assets where the Content Hub and Authoring Catalog already see them.
2. **Field naming.** The section 9 snippet uses public camelCase fields. Project convention is private `_camelCase`
   `[SerializeField]` with `[Tooltip]` and read-only properties. The table above keeps the names; the class should
   follow convention.
3. **Patronage is already built.** `CostType.Patronage`, `ArchetypeResource.Patronage`, `BankedResource`,
   `GeneratePatronageEffect` ("sacrifice cards from hand"), `SummonBodyEffect`, and Patronage display in
   `CardButton`, `BattleStatsOverlay`, `BattleLogPanel`, `BattleScreen`, `BattleInspectorWindow`, `PlaytestRunner` and
   `KeywordGlossary`. No Nepo card assets use them (`Assets/Data/Cards` has no Nepo folder). Ruling needed: delete,
   or keep for enemies (`SummonBodyEffect` may serve enemy-side summoning).
4. **Unlock flag already exists.** `CardData._isUnlockable` ("Must this card be unlocked through progression?") plus
   `CardDatabase.GetUnlockableCards()` and `CardSearchQuery.UnlockableCardsOnly`. Nothing reads it at reward time
   (`needs-detailing.md` section 9). Proposal: reuse `_isUnlockable` and add it to the exclusions in
   `CardDatabase.IsAcquirable` and `GenerateRewardOffer` instead of adding a second `lockedUntilUnlocked` bool. That
   filter applies to every class: no card is flagged today, so nothing currently offered disappears.
5. **Every Policy already exhausts on play.** Policies are activated passives (`CardData.IsActivatedPassive`;
   `CardPlayController` exhausts them after their effects resolve). "Persistent" vs "one-shot" is then whether the card
   has passives or only effects. That matches the spec. Whether a Policy can be burned from hand is new: no
   burn-from-hand cost exists yet.
6. **Burn already exists in one place.** `GeneratePatronageEffect` exhausts cards from hand with player choice and
   already special-cases Heckle/Scandal. The new Burn cost generalises this, adding the Policy exclusion and printed
   cost readout.
7. **Scandals are unplayable.** `CardData.IsUnplayable` is true for every Scandal, and `DeckManager` reacts to Scandal
   draws (Celebrity). That matches "heavy, sticks in the deck", and it means "play it for free" (Blow the Allowance)
   has no valid target in a Scandal, which supports default 8.1 = no.
8. **Mulligan.** `MulliganEffect` exists. The once-per-battle limit fits `FirstTimeCondition` on an origin passive, as
   `Nepotism` already uses.
9. **Calm caps at Neutral.** `ReduceHostilityEffect` and `ShiftHostilityEffect` have no floor at the NeutralZone
   (`BattleStats.IsReceptive` is hostility < -NeutralZone). Apology Tour and Smooth Things Over need an
   "not below Neutral" option.
10. **Missing effects.** Not in `Effects/`: scry, pull from draw pile, replay / play-twice, return-to-hand after
    resolution (only choose-from-discard), put chosen cards from hand on top of the draw pile (`MoveOwnerCardEffect`
    only moves the card a passive lives on),
    in-turn cost increase, move Hostility between enemies, remove an enemy buff, "per card exhausted this combat" and
    "per card discarded this turn" context values. All are new `[SerializeReference]` effects or `EffectContextValue`
    entries; no existing behavior changes.
11. **Starter deck slot.** `OriginDatabase` builds starters from each entry's `StarterDeck`, falling back to cards
    tagged as starters. `OriginType.NepoBaby`'s entry is where the 10 cards and the new passive go. Its
    `StartingFunds` / `StartingCredibility` / `MaxHours` are unset like every other origin (CLAUDE.md, known soft
    spots).
12. **Cards without art are never offered.** `CardData.IsInDevelopment` is true while `_artwork` is null, and both
    `IsAcquirable` and `GenerateRewardOffer` skip those cards. The new Nepo cards need placeholder art, or a dev
    override, before section 10's reward-pool tests can see them.
