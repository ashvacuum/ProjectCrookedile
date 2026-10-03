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
converted enough that turn. Command cards can force any of the three.

Still to detail, all with a placeholder in the ruling:
- **Follow amounts:** +2 Opinion / +2 Support per Fanatic per matching card. Too strong with three Fanatics and a
  cheap-attack hand? A cap per turn, or per-card instead of per-play?
- **Silence threshold:** 2 conversions this turn. Count conversions, or Fanatics present?
- **Duration:** until the end of your next turn. Long enough to feel like a follower, short enough to keep the
  stack → convert loop going?
- **Command card list** for the reward pool (attack now / defend now / silence now, plus Sermon-style harvest).
- **What counts as an attack or a defend** for cards that do something else too (draw, apply a status).

**Code gap:** `PacifyConversionEngine` still pays an instant burst on conversion (`BurstPerStack` × consumed), and
`FanaticStatus` is only a hostility flag. The follower behaviour is not built.

## 2. Intent vocabulary (blocks: enemy roster authoring)

Code uses `EnemyMoveType` (Attack/Defend/DefendOpinion/RileOthers/...); the design doc uses Rally/Rebuke/Sway/Condemn/Murmur. Pick one vocabulary before authoring 6–8 enemies, or every enemy asset gets touched twice. Also detail Sway (convert receptive→hostile) and Murmur (low-impact presence) — both specified, neither has a concrete effect list.

## 3. Celebrity sub-archetype card pools (blocks: Celebrity playtests, not FL/Nepo)

**Possibly obsolete:** Celebrity was redone as the Glamour / IOU build (`celebrity-glamour-iou.md`). If that is now
the Celebrity, this item closes and `core-design.md` §7 is rewritten to match. Until that call, the old opens:

Each of Attention / Scandal / Drama King needs enough cards to be *committable* (~8–12 each, per the "coherent mini-archetypes, not oatmeal" rule). Specific opens:
- **Scandal:** severity-when-drawn; on-draw vs in-play triggers (pool can have both — ratio?); removal beyond the spin/cash-out.
- **Scandal is currently built as its own opposite.** The design wants an anti-Curse you *want* — clogs the hand but powers your other cards per-Scandal. The authored Scandal cards are plain StS punishment curses, and they are tagged faithleader/universal rather than Celebrity. No card rewards carrying one. Decide whether the reward-per-Scandal line is real before authoring the pool; if it is, the existing curses are mis-tagged, not just unfinished.
- **Attention:** the "held too long → you become the target" penalty — auto rule or card-text-only? Currently deferred to tuning.
- **Drama King:** keep disarm framing distinct from FL debuffs (protect-while-attacking vs debuff-to-convert).

## 4. Nepo Baby open questions (blocks: Nepo build)

The class was redesigned (2026-10-03, `nepo-baby-class.md`): no Patronage, no summons. Open questions with their defaults are in that doc's section 8 (seed vs junk, "I'm Just Like You" type, whether replays raise Hostility, Hostility-scaled junk injection, Friends in High Places). Section 12 lists the conflicts with the old code and how each was resolved; Patronage is removed, and the summon effect stays for enemy content.

## 5. Meta-progression: unlocking campaigns, events and allies (blocks: campaign unlock content)

Only cards unlock today. The campaign needs campaigns (whole runs), events and allies (the game's items) to unlock as
well. The proposal is in `meta-progression.md` section 4, "Beyond cards": the same unlock condition on each asset,
grants from events, a `CompletedCampaign` condition to chain campaigns, and the run recording which campaign it is.
Open: whether a locked ally's recruit option is shown disabled (proposed) or hidden, and the run-start screen that
lists campaigns.

## 6. EncourageSides — no agreed meaning

An enemy move from an early pass on enemy behaviours ("encourage others to switch sides"), never defined: it could
turn your converts back, push receptive enemies hostile, or pull the meter. No `EnemyMoveType` value, no effect, no
asset — deliberately. Two readings fit the current design:
- **The enemy's answer to Fanatics:** ends one Fanatic early (it reverts and gains Jaded). Gives Faith Leader a real
  threat to read in the intents.
- **Fold it into Sway** (item 2), which is already specified as "convert a receptive enemy to hostile" and has no
  effect list either. Then EncourageSides stops existing as its own move.

## 7. CardType colour taxonomy carries no meaning as authored

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
- **Starter deck quantities:** `OriginDatabase` holds an authored deck per origin (card + count). Faith Leader and Nepo
  Baby have one; Celebrity's is empty, so it falls back to one of each starter-tagged card.
- **Status database scope** (2026-10-03): `StatusEffectIconMapSO` stays icon / colour / text; sound and VFX map
  elsewhere. The concern is the statuses cards apply, which need an audit (a Content Hub build task, not a design call).
- **Meta-progression shape:** in `meta-progression.md` (profiles, saves, card unlocks, achievements, Steam).
- **Campaign form** (2026-09-18, built since): a free-roam 2:1 isometric city over seven days, an hour budget, districts
  and travel (`metagame-campaign.md` §1.5, `campaign-encounters.md` §6). Still deferred, so don't detail yet: viral
  moments / News Cycle, the production resource HUD, the `EnemyConvertedEvent` flourish, and overworld art beyond
  greybox.
