# Needs Detailing — design questions awaiting a decision

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-11-21
>
> **Summary:** Design questions awaiting a call, ordered by how much they block. Read before authoring content.
>
> **Source of truth:** this doc · **Related:** [`core-design.md`](core-design.md)

*As of 2026-10-03. These are NOT build tasks — each needs a design call (and usually a playtest) before code. Ordered
by how much they block. Calls already made are listed at the bottom.*

---

## 1. Fanatic followers — numbers and command cards (blocks: FL deck authoring)

**Ruled 2026-10-03** (`core-design.md` §7, "Fanatics follow your lead"): a Fanatic is your loyal follower. When you
attack it attacks, when you defend it adds to your defence, and when you do neither it Silences enemies if you
converted enough that turn. Command cards can force any of the three. It follows for as long as the Fanatic buff
lasts, then is disillusioned (reverts to neutral, gains Jaded).

Still to detail, all with a placeholder in the ruling:
- **Follow amounts:** +2 Opinion / +2 Support per Fanatic per matching card. Too strong with three Fanatics and a
  cheap-attack hand? A cap per turn, or per-card instead of per-play?
- **Silence threshold:** 2 conversions this turn. Count conversions, or Fanatics present?
- **Buff length:** the Fanatic follows for as long as its buff lasts, then is disillusioned (ruled). Placeholder
  length is through the end of your next turn; which cards extend it, and by how much?
- **Command card list** for the reward pool (attack now / defend now / silence now, plus Sermon-style harvest).
- **What counts as an attack or a defend** for cards that do something else too (draw, apply a status).

**Built (2026-10-23):** follow-on-attack, follow-on-defend, the quiet-turn Silence, skipping its action against you,
and disillusion (neutral + Jaded) run in `FanaticFollowers` and `FanaticStatus`, with every placeholder in
`Resources/FaithLeaderConfig`. Conversion pays no burst. Attack/defend is read off what the card did (pushed the
meter / gained Support). **Built (2026-11-02):** over-stacking (each pacify stack a conversion consumed past its threshold adds
`FollowBonusPerExtraStack` to that Fanatic's push and Support), command cards **Lead the Charge** (`FanaticsPushEffect`)
and **Close Ranks** (`FanaticsSupportEffect`) next to the existing silence-now card **Shun the Infidels**, and buff
extension by composition (`ForEachEnemyWithStatus` Fanatic → apply Fanatic to `TriggeringEnemy`; Hallelujah adds 1
turn). A Fanatic's Strength adds to its push. Gospel and God's Chosen no longer award Jaded at conversion: it arrives once, on disillusion.

## 2. Nepo Baby open questions (blocks: Nepo build)

Redesigned again on 2026-11-21 (`nepo-baby-class.md`, "Redesign"): Scry 1 relic; pillars Inheritance, Owed, Pulling
Strings. Open: Invest numbers for The Family Name; whether its return rule gets the "Birthright" keyword; the starter
deck rebuild; which old burn-design cards survive as salvage.

## 3. Silenced after the card-type re-type

Card types now mean roles (`core-design.md` §8, resolved below): Pressure = attack, Rhetoric = skill. `Silenced` is
"no Rhetoric", which used to read as "no aggressive cards" and would now block defends, draw and calming. Decide whether
Silenced becomes "no Pressure" (can't attack) before the re-type lands.

---

## Resolved

- **CardType taxonomy** (2026-11-21): the type is the card's role. **Pressure = attack**: a card that pushes the meter is
  Pressure first and foremost, whatever else it does. **Rhetoric = skill** (defend, statuses, calming, draw, tricks).
  **Policy = persistent.** Heckle and Scandal unchanged. 18 Pressure cards that never push and 11 Rhetoric cards that do
  get re-typed once the card pools settle (`core-design.md` §8).
- **Starting relics and pillars** (2026-11-21): each class has one upfront relic and three pillars (`core-design.md`
  §7, §9). This supersedes the Celebrity starting-ally entry below: the Publicist is replaced by Last Impressions.
- **Celebrity starting ally** (2026-11-07): the **Publicist**: at battle start every enemy is Exposed, so the
  first hit on each deals double. Charm disarms the room on entrance; the old "first card upgraded" passive and the
  borrowed mulligan (now the Speechwriter, a shared ally) are both dropped for Celebrity (`celebrity-glamour-iou.md`, `allies.md` §4.1).
- **Intent vocabulary** (2026-11-02): players see the core-design words (Condemn / Rebuke / Rally / Turn / Murmur, plus
  Summon / Ward / Counter). `EnemyMoveType` is unchanged; the mapping and each word's effects are in
  `naming-glossary.md` §C. The enemy "Sway" became **Turn** so it doesn't clash with the player's Sway.
- **Card highlight** (2026-10-09, built in `CardButton`'s Condition Highlight): a card in hand glows when **a condition on it is met** right now
  (target is Hostile, Debt is owed, the enemy has a status). Boosted numbers are not a highlight; they belong on
  the card's own numbers, once damage, Aggravate and Soothe preview per target. Combos are out: there's no clear
  rule for when one is "ready".
- **Receptive enemy bonus** (2026-10-03): **+2 Support when an enemy turns receptive, +1 per receptive enemy at the
  start of your turn** (`core-design.md` §3). No taper toward the Echo Chamber.
- **Echo-chamber escape valve** (2026-10-03): the hostility card is an ordinary, removable card. The bot playtests show
  no Echo Chamber problem; revisit only if they start to.
- **Starter deck quantities:** `OriginDatabase` holds an authored deck per origin (card + count). All three classes have
  one.
- **Status database scope** (2026-10-03): `StatusEffectIconMapSO` stays icon / colour / text; sound and VFX map
  elsewhere. The concern is the statuses cards apply, which need an audit (the Database window's Statuses tab shows which used statuses lack an icon).
- **Celebrity** (2026-10-03): the Glamour / IOU build is the Celebrity (`celebrity-glamour-iou.md`); the Attention /
  Scandal / Drama King pools are dropped. Scandal cards stay as enemy-inflicted junk.
- **EncourageSides** (2026-10-03): an enemy move that angers the other enemies (raises their Hostility). That is the
  existing `RileOthers` move type (`RaiseAlliesHostilityEffect`), already used by three enemies — nothing new to build.
- **Unlocks** (2026-10-03, `unlocks.md`): everything unlocked for now; campaign 2 needs day 7 of campaign 1;
  achievements and unlocks are one system; no difficulty ladder. When locks go on, poles stay open and only pole
  improvements and meta cards lock.
- **Meta-progression shape:** in `meta-progression.md` (profiles, saves, card unlocks, achievements, Steam).
- **Campaign form** (2026-09-18, built since): a free-roam 2:1 isometric city over seven days, an hour budget, districts
  and travel (`metagame-campaign.md` §1.5, `campaign-encounters.md` §6). Still deferred, so don't detail yet: viral
  moments / News Cycle, the production resource HUD, the `EnemyConvertedEvent` flourish, and overworld art beyond
  greybox.
