# Needs Detailing — design questions awaiting a decision

> **Kind:** Tracking · **Status:** Living · **Updated:** 2026-10-03
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

**Code gap:** `PacifyConversionEngine` still pays an instant burst on conversion (`BurstPerStack` × consumed), and
`FanaticStatus` is only a hostility flag. The follower behaviour is not built.

## 2. Intent vocabulary (blocks: enemy roster authoring)

Code uses `EnemyMoveType` (Attack/Defend/DefendOpinion/RileOthers/...); the design doc uses Rally/Rebuke/Sway/Condemn/Murmur. Pick one vocabulary before authoring 6–8 enemies, or every enemy asset gets touched twice. Also detail Sway (convert receptive→hostile) and Murmur (low-impact presence) — both specified, neither has a concrete effect list.

## 3. Celebrity class passive (blocks: nothing — the class plays without one)

Glamour / IOU is the Celebrity (2026-10-03). The "first card each battle is played upgraded" passive was locked for
the old Celebrity and nobody has ruled whether it carries over; the code points the Actor origin at a Nepo-style
mulligan placeholder (`Daddy's Gifts`). Decide: keep the upgraded first card, or a passive that touches Glamour or Debt.

## 4. Nepo Baby open questions (blocks: Nepo build)

The class was redesigned (2026-10-03, `nepo-baby-class.md`): no Patronage, no summons. Open questions with their defaults are in that doc's section 8 (seed vs junk, "I'm Just Like You" type, whether replays raise Hostility, Hostility-scaled junk injection, Friends in High Places). Section 12 lists the conflicts with the old code and how each was resolved; Patronage is removed, and the summon effect stays for enemy content.

## 5. CardType colour taxonomy carries no meaning as authored

Pressure (green, persuade) vs Rhetoric (red, aggressive) is applied at random: draw-only cards
sit in Rhetoric, and pressure is split across both. The colours are load-bearing for the player
read and for `Silenced` (= "no Rhetoric"), so they need a consistent rule — or the split needs to
stop pretending to be one.

---

## Resolved

- **Receptive enemy bonus** (2026-10-03): **+2 Support when an enemy turns receptive, +1 per receptive enemy at the
  start of your turn** (`core-design.md` §3). No taper toward the Echo Chamber.
- **Echo-chamber escape valve** (2026-10-03): the hostility card is an ordinary, removable card. The bot playtests show
  no Echo Chamber problem; revisit only if they start to.
- **Starter deck quantities:** `OriginDatabase` holds an authored deck per origin (card + count). All three classes have
  one.
- **Status database scope** (2026-10-03): `StatusEffectIconMapSO` stays icon / colour / text; sound and VFX map
  elsewhere. The concern is the statuses cards apply, which need an audit (a Content Hub build task, not a design call).
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
