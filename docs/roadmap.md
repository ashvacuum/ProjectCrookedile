# Crookedile — 3-Month Demo Roadmap

*Budget: ~13 dev weekends (one dev, weekends only) + a parallel art track (recruited art buddies).
Written 2026-07-03. Companion docs: `card-audit.md`, `faith-leader-identity.md`, `art-bible.md`, `test-plan.md`.*

## The scope call (read this first)

**13 weekends ships a one-class demo, not a 3-class game.** The deliverable is a
**Faith Leader vertical slice**: pick FL → short run on a map (~10 encounters) → win/lose → retry.
Polished enough to put on itch.io / Steam as a demo and watch strangers play it.

**Cut from the demo (explicitly, now, so nothing half-exists):**
- Nepo Baby & Celebrity — class select shows them "coming soon". Their code stays; no content work.
- Shops (blocked on currency ⚑), viral moments / News Cycle — post-demo.
- Events: **3–5 simple ones only** (they're cheap in the M2 architecture; more post-demo).
- Boss: ONE (reuse the enemy system — bigger numbers + a gimmick status; no phase framework).
- Full test-suite rebuild — only smoke tests around the pacify engine + opinion ledger.

**Relics are IN (cheap now):** the runtime is already built (Phase R done — RunState.Relics,
PassiveResolver folding, 5 prototype relics + debug grant). Demo gets boss-victory → pick 1 of 3.

**The two massive unknowns, front-loaded on purpose:**
1. **Is the core loop fun?** (stack→convert + villain management) — answered in Phase 0, weekend 1.
2. **The campaign layer isn't built yet** (M1–M3 of `metagame-campaign.md`) — the biggest build, Phase 2.

---

## Phase 0 — Fun Check (weekend 1) 🔴 GATE

Wire what exists into a playable fight and answer "is this fun?" before building anything else.

- Assemble 2-3 `BattleSession`s from the FL starter (15 cards) + prototype roster (7 enemies).
- Play 10+ full battles. Watch for: do you ever *keep* a villain on purpose? does convert feel good?
- Text pass while playtesting: adopt the mock vocabulary — **Aggression / Audience Approval /
  Support / Denial / Energy** — purge "Resolve damage" from all card text (data-only edit).
- Tune the obvious numbers (pacify burst size, echo-chamber decay, enemy condemn values).

**Gate:** if stack→convert isn't fun after tuning, STOP and redesign the payoff before Phase 1.
A weekend spent here saves a month downstream.

## Phase 1 — Combat Content Complete (weekends 2–4)

Everything a single fight needs, done to demo quality.

- Author the 5 missing FL cards (Confession, Litany, Crusade, Penance, Zealots — all
  `ApplyStatusBehaviorEffect` re-skins) + fix the flagged keepers (Congregation rework, Martyrdom
  desync). → **FL = 20 confident cards.**
- Card upgrades authored for the 20 (`_upgradedEffects`) — rewards need them.
- Enemy roster to ~10 (add 2-3 from the audit list: Tabloid Reporter, Diehard, Rival's Plant) +
  stance-tune the existing 7 in play.
- 5-6 distinct encounter compositions (the "who's my villain today" variety lives here).
- Battle rewards flowing end-to-end: card offer screen → deck grows (already ~built; verify + polish).
- Production HUD for Support/Denial/turn (replace debug overlay usage in the battle scene).

## Phase 2 — The Campaign Layer (weekends 5–8) 🔴 the big build

Build M1–M3 of **`docs/metagame-campaign.md`** (the locked free-roam design — NOT an StS node
map). Debug-grade presentation is fine; the architecture doc already settles the type tree,
two-scene flow, and RunState handoff.

- **Wknd 5 — M1 skeleton:** `CampaignMapData`/locations, Hours/Day on RunState, campaign scene
  with a plain location-button list, HQ ends the day. Battle handoff via `PendingBattle`.
- **Wknd 6 — M2 events (mini):** `EventEncounterData` + Requirement/Outcome types + the event
  panel; author 3–5 events (one grants a relic).
- **Wknd 7 — M3 boss + reward scaling:** boss flag → pick-1-of-3 relic; `BattleResult` crowd
  stats → reward quality scales via `RewardConfig` ("win well").
- **Wknd 8 — slack + demo answers to the ⚑s:** campaign end = fixed day count ("election day",
  ~10–12 encounters' worth); HQ verb for v1 = remove-or-upgrade a card. Class select screen
  (FL playable, other two greyed).

## Phase 3 — Art Drop-in + Deck Feel (weekends 9–11)

- Land the art track's deliverables as they arrive: portraits, card frames, UI kit re-skin,
  card art. (Import settings per art bible §0.5: ASTC, mips on cards, 2048 cap.)
- Audio: the two BGM tracks (map nu-jazz / combat swagger — `music-prompt.md`), card-play +
  convert + turncoat SFX via the existing `BattleSoundMap`.
- **Steam Deck reality check:** 1280×800 layout pass + controller/keyboard navigation of the card
  hand. ⚠️ This is a real work item — current input is mouse-drag (`CardButton`). Budget a full
  weekend; if it blows up, demo ships "best with mouse/touch" and Deck-verified moves post-demo.
- Juice pass with what exists (DOTween punches, floating numbers, the convert flourish).

## Phase 4 — Ship the Demo (weekends 12–13)

- Onboarding: the parchment "How it works" panel from the mock as a first-fight overlay
  (no tutorial scripting — annotated UI + a gentle first encounter).
- Difficulty/balance pass on the full run (target: first win around run 3-5 for a new player).
- Builds: Windows + SteamOS/Proton check. itch.io page (Steam page if keys/assets ready).
- Friends-and-strangers playtest, fix the top-5 confusions, ship.

---

## Art track (parallel — brief the artists in week 1)

Their queue in priority order (full specs in `art-bible.md`; style = the approved v4 Odd Taxi mock):

| # | Deliverable | Count | Needed by |
|---|---|---|---|
| 1 | Enemy portraits (the 7 cast, 1024²) | 7 | Phase 3 start |
| 2 | Card frames: 5 type + 3 rarity overlays (1500×2148) | 8 | Phase 3 start |
| 3 | UI kit: 9-slice panels, meter, buttons, parchment scraps | ~12 pieces | Phase 3 |
| 4 | FL card art (briefs in art bible §3) | 20 | Phase 3–4, trickle |
| 5 | Status + intent icons (128², flat/tintable) | 29 + 10 | Phase 3–4 |
| 6 | Player buwaya (podium back-view + class-select bust) | 2 | Phase 4 |

**Animation: keep it OFF the critical path.** No skeletal/Spine character animation in the demo —
static portraits + the existing tween/VFX juice reads great in this flat style (Odd Taxi itself is
famously still). If the buddies *want* animation wins: animated main-menu logo, a 4-frame portrait
blink/talk cycle, card-glow loops. Nice-to-haves only.

## Weekend map at a glance

| Wknd | Focus |
|---|---|
| 1 | 🔴 Fun check + vocabulary pass |
| 2–4 | FL 20 cards + upgrades, 10 enemies, encounters, rewards, HUD |
| 5–8 | Map, RunState, ramp, class select (+ slack) |
| 9–11 | Art drop-in, audio, Deck/controller, juice |
| 12–13 | Onboarding, balance, builds, playtest, **ship demo** |

## Standing risks

- **Run layer slip** — mitigated by 3-node-type scope + slack weekend. If slipping at weekend 7: cut elite nodes, ship fight/rest only.
- **Controller support** — quarantined to one weekend with a mouse-first fallback.
- **Art latency** — the game must be *playable* gray-boxed at every phase; art lands as a skin, never a blocker. Placeholder crops from the mock cover until then.
- **Scope creep from the other classes** — Nepo/Celebrity ideas go into `docs/needs-detailing.md`, not into the build.
