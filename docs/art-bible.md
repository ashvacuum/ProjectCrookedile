# Crookedile — Art Bible & Handoff Spec

*Canonical art-direction + resolution spec for artists. Supersedes the thin `art-needed.md` checklist. All sizes are measured from the real assets/prefabs or set as authoring targets; the Content Hub (Statuses / Intents / Enemies tabs) is the live blank-slot checker.*

**Theme:** Filipino political roguelite satire — you "work a crowd," you don't fight. Tone: glossy campaign-poster sheen over something rotten. Religious-political iconography for Faith Leader, dynastic luxury for Nepo Baby, tabloid-celebrity gloss for Celebrity.

---

## 0.1 Style reference (WORKING DIRECTION — not locked)

> **Rendering direction: the anime _Odd Taxi_** — flat, muted, sophisticated, deadpan; grounded anthropomorphic cast played with dry restraint. Full generation prompt: `docs/reference/style-mock-prompt.md` (v4).
>
> **Reference of record: `docs/design/crookedile-style-v4-oddtaxi.png`** (approved 2026-07-03 — the flat muted committee-hall mock). This is the look to match: flat cel shading, dusty warm palette (mustard/teal/brick/cream/olive), thin clean lines, deadpan grounded animal-people, even soft lighting. The earlier dark-painterly mock is superseded.

**Rendering style:** dark painterly realism (digital-oil), low-key lighting, one warm key light on the speaker/subject, crowd falls into near-black vignette. No cel-shading, no flat vector. Grain/texture over gradient banding.

**Composition (battle scene):** over-the-shoulder podium view — the player is IN the shot, back to camera, facing a raised row of opponent bust-portraits behind desks (committee-hearing framing). The crowd is a dark backdrop, not individuals. This is the "work the room" fantasy rendered literally.

**Palette anchors:**
| Role | Color |
|---|---|
| Base / panels | near-black charcoal & dark walnut `#141210` / `#2A2018` |
| Parchment accents (tips, notes) | cream `#E8D9B0`, torn-paper edges, tape |
| Approval/positive | toxic green `#7FBF3F` |
| Hostile/danger | ember red-orange `#C43B2A` |
| Receptive/support | gold-amber `#D9A33B` |
| Chrome lines / dividers | thin muted gold `#8A7448` |

**UI furniture (copy these patterns):**
- **Opinion meter** = top-center horizontal bar ("Audience Approval 62/100"), faction icons capping each end (gold=yours, red=theirs), flavor line beneath.
- **Enemy slot** = nameplate w/ role icon → **signed aggression bar** (blue→green negative / red positive, numeric ±value) → portrait → **intent panel below** (icon + plain-words effect). Matches our hostility axis + revealed intents 1:1.
- **Hand** = fanned at bottom-center; card = cost gem (top-left, blue octagon), name banner, painterly art, target line ("TARGET 1" / "ALL OPPONENTS"), effect text on dark panel.
- **Card frame color = function**: red aggressive / green calm-empathy / gold religious-unity / blue neutral-facts / dark gray passive. Confirms §2a's color taxonomy.
- **Callout cards** (Risk & Reward, How It Works) = parchment scraps with handwriting + crocodile doodle — use this for tutorial/tooltip voice.
- Right rail: Turn counter, Energy (3/3 as lightning gems), Draw/Discard counts. End Turn = big gold-trimmed dark button with flavor text.

**Vocabulary note (feeds the text pass):** the mock's player-facing terms — **Audience Approval** (opinion meter), **Aggression** (hostility), **Energy** — read instantly. Consider adopting them as the display vocabulary when we purge "Resolve damage."

---

## 0. Global delivery standards (read first)

| Rule | Spec |
|---|---|
| Format | PNG-24 + straight alpha, sRGB. Deliver layered source (PSD/Clip) **and** flattened PNG. |
| Color | sRGB, no embedded ICC weirdness. Frames are color-coded — match the hex per type (below). |
| Transparency | Frames & icons need real alpha (the art/colors show through). No baked background. |
| Trim/padding | Icons: keep a ~6% transparent margin so they don't clip in the badge mask. Frames: full-bleed to the stated size. |
| Naming | Match the data slot exactly (tables below). `snake_case` or the existing `Character_NN` style. One concept per file. |
| Pivot | Center pivot unless noted. |
| Authoring scale | Author at the **target size** in each table (already ~1.5–2× display); the prefab RectTransform scales down. Don't author below target. |

---

## 0.5 Platform & resolution targets (Steam Deck → 4K)

**Display range we must cover:** Steam Deck native **1280×800 (16:10)** up to docked **3840×2160 (16:9)** — a 3× linear span *and* an aspect-ratio change. Also expect 1080p/1440p docked.

**DPI is not the lever.** A Unity game renders to the framebuffer pixel count, ignoring OS DPI scaling. UI size is driven by the **CanvasScaler**, not DPI. Physical PPI (Deck ~206–226, a 4K 27" monitor ~163) only affects perceived sharpness and minimum legible text — it does **not** change how assets are authored.

**The one rule that sets every target: author for the top of the range (4K).** Downscaling is crisp; upscaling is soft. Size each asset to its **largest on-screen pixel size at 4K**, and the Deck simply downsamples it cleanly.

| Asset | Largest 4K display (approx) | → Author target |
|---|---|---|
| Inspected/zoomed card | ~75% of 2160 = ~1620px tall | **1500×2148** (was 1000×1432) |
| Hand card | ~30% = ~650px tall | covered by the same 1500×2148 |
| Enemy portrait (inspect) | ~500–700px | **1024×1024** (was 512) |
| Status/intent badge | ~55–90px even at 4K | **128×128 stays fine** (256 only if shown large) |
| Resource/cost icon | small | **128×128** fine |

**Unity import settings (so 4K assets don't bloat the Deck):** Max Size 2048, compress (ASTC on the Deck's APU / BC7 fallback), **mipmaps ON for card frames + art** (they scale a lot — mips kill shimmer when downsampled to 800p). Icons can keep mips off.

**CanvasScaler:** Scale With Screen Size · Reference **1920×1080** (or 2560×1440) · Match Width-Or-Height **0.5**. Sprites then scale with the canvas across the whole 1280×800 → 4K range; you never touch per-device sizes.

**Aspect ratio is the real gotcha, not resolution.** 16:10 (Deck) is taller than 16:9 (docked). Design every screen inside a **16:9 safe zone**, let 16:10 reveal a little extra top/bottom (don't hard-code 16:9). Anchor HUD to edges, the card hand to bottom-center — never to absolute pixels.

**Steam Deck "Verified" art-adjacent must-haves:** native 16:10 (no forced letterbox), text legible at arm's length on 7" (keep body text ≳ 24px at the 1080p reference — test on-device), 60fps at 1280×800 (watch **alpha overdraw** from the stacked transparent card layers), and full controller navigation.

---

## 1. Card anatomy (how the layers compose)

A card is **4 stacked UI Images + text**, bottom to top. Understanding this tells the artist what must be transparent where.

```
┌─────────────────────────┐  ← all layers are 1000 × 1432, same silhouette
│  ① TYPE FRAME (chrome)  │     color-coded border + nameplate + cost orb + textbox,
│   ┌─────────────────┐   │     with a TRANSPARENT ART WINDOW cut out of the middle
│   │ ② CARD ART      │   │  ← per-card illustration shows through the window
│   │   (full-bleed)  │   │
│   └─────────────────┘   │
│  ③ RARITY OVERLAY       │  ← gems/foil/corner flourish ON TOP of the frame (mostly transparent)
│  name · cost · textbox  │  ← ④ text rendered by the engine (not art)
└─────────────────────────┘
   (card BACK ④ is a separate full sprite, shown when the card is face-down)
```

- **Type frame** = the colored chrome. Same silhouette across all 5 types, recolored + light motif change. Has the transparent art window.
- **Card art** sits *behind* the frame and shows through the window — so author it **full-bleed 1000×1432** with the subject in the art-window safe zone (upper-center, see §3).
- **Rarity overlay** sits *on top* of the frame — mostly transparent, just adds the rarity treatment (gem, foil sheen, corner filigree).
- Text (name, description, cost number) is engine-rendered — **not** baked into art.

---

## 2. Card frames, overlays & backs

### 2a. Type frames — 5 — `1000 × 1432` — `CardVisualSettings._*Frame`
Same frame silhouette, color + motif per type. Transparent art window (~upper 55% of card), a nameplate strip near the top, a cost orb (top-left), and a lower description panel (semi-opaque so engine text reads on it).

| Slot | Type | Color (intent) | Hex anchor | Motif |
|---|---|---|---|---|
| `_pressureFrame` | Pressure | Green — persuade/de-escalate | `#3FА86B`* | calm laurel / handshake |
| `_rhetoricFrame` | Rhetoric | Red — aggressive | `#C0392B` | sharp, jagged, megaphone |
| `_policyFrame` | Policy | Blue — policy/lean | `#2C6FB5` | ledger / seal / document |
| `_statusFrame` | Heckle (status) | Purple — temporary junk | `#7D4CA8` | dashed/ephemeral border |
| `_curseFrame` | Scandal | Dark crimson — unplayable clog | `#6E1B2E` | torn tabloid / redacted bars |

\* tune in engine; these are direction, not law. **Current gap:** all 5 point at placeholder `CardFront_01–03`; only 3 distinct frames exist.

### 2b. Rarity overlays — 3 — `1000 × 1432` (mostly transparent) — `CardVisualSettings._*Frame`
Drawn over the type frame. Keep the art window + textbox clear.

| Slot | Rarity | Treatment |
|---|---|---|
| `_basicFrame` | Basic | None or a thin matte border. The plain floor. |
| `_enhancedFrame` | Enhanced | Silver/bronze corner filigree + subtle inner bevel. |
| `_rareFrame` | Rare | Gold frame accents + foil sheen + corner gems. Should read as "rare" at a glance. |

**Current gap:** all 3 reuse the pressure-frame placeholder — no visual rarity difference exists yet. This is the highest-value frame work.

### 2c. Card backs — 4 — `1000 × 1432` — `CardVisualSettings._*CardBack`
Per origin, shown face-down. `_defaultCardBack`, `_faithLeaderCardBack` (religious seal), `_nepoBabyCardBack` (dynastic crest), `_actorCardBack` (celebrity monogram/star). These exist (`CardBack_01/02`) but only 2 distinct.

---

## 3. Per-card illustration (the art window)

The `Character_NN` sprites are **placeholders** (random assignment). Real per-card art is authored **full-bleed 1000×1432, portrait**, subject framed in the **art-window safe zone**: roughly `x: 70–930, y: 110–800` (upper-center). Keep critical detail out of the bottom ~45% (textbox) and the top ~8% (nameplate). Confirm the exact window against the frame PSD before final crops.

### Art briefs — Faith Leader (the 15 built cards)
One-line direction each; satire = Filipino megachurch-politician. Tie the image to the mechanic.

| Card | Rarity | Brief |
|---|---|---|
| Rebuke | Basic | Preacher jabbing a finger mid-sermon — a sharp verbal correction. |
| Pray | Basic | Hands clasped, eyes closed, a faint halo — calm gathering of strength. |
| Call Out Sin | Basic | Finger leveled at one face in the crowd; the accused recoils (seeding a villain). |
| Guilt Trip | Basic | A parishioner head bowed, shoulders sagging under an unseen weight. |
| Name and Shame | Basic | Public square pillory vibe — someone hiding their face from pointed phones/cameras. |
| Sow Doubt | Basic | A whispered word; a question-mark thought curling over a wavering listener. |
| Sermon | Basic | Pulpit wide-shot, rapt crowd, light from above — the payoff moment. |
| Moral High Ground | Basic | The leader literally elevated on a marble step, serenely looking down. |
| Preach | Basic | Megaphone fused with a lectern; words as a physical force pushing the crowd. |
| Excommunicate | Enhanced | A heavy church door slamming on a cast-out figure; banishment. |
| Congregation | Enhanced | A swelling flock filing in, candlelight — an engine that builds each turn. |
| Gospel | Enhanced | An open holy book radiating light, pages turning on their own. |
| Absolution | Rare | Mass absolution — arms raised over a whole kneeling crowd; scales/judgment overtone. |
| Martyrdom | Rare | A figure arms outstretched, sacrificial glow, the crowd around them inflamed (riled). |
| Revelation | Rare | A single shaft of light splitting clouds — sudden clarity/vision. |

*(Nepo Baby & Celebrity briefs are deferred — those card lists aren't locked yet; speccing art for cards that may be cut is premature. Same template applies once their 20-lists are nailed.)*

---

## 4. Status icons — 29 — `128 × 128` — `StatusEffectIconMapSO`

Flat / single-color silhouette style, readable at a ~32px badge. Author white-on-transparent or single-hue; the map can tint. Source of truth = `StatusRegistry`.

- **Player debuffs:** Weakened, Vulnerable, Frail, Entangled, Exposed, Confused, Silenced, Stunned, Rattled, Smear
- **Player buffs:** Strength, Dexterity, Focus, Energized, Plated, Regeneration, Intangible, Thorns, Ritual, Momentum, Echo
- **Faith Leader pacify:** Guilt, Shame, Doubt, Jaded
- **Hostility flags (on enemies):** Hardened, Fanatic, Devotion, Turncoat

Suggested motifs for the FL-relevant ones (these drive readability of our lead class): **Guilt** = downcast weight/chain; **Shame** = face-cover/blush mask; **Doubt** = wavering question mark; **Jaded** = cracked halo / rolling eyes; **Hardened** = stone wall; **Fanatic** = wide-eyed flame; **Turncoat** = flipped/two-face.

---

## 5. Enemy intent icons — 10 — `128 × 128` — `EnemyIntentTheme`
Author neutral **white**; the theme recolors per intent. One per `EnemyMoveType`.

| Icon | Means | Motif |
|---|---|---|
| Attack | Pressure/debuff to the player | downward fist / shout |
| Defend | Gains shield / self-heal | raised guard |
| Buff | Self-buff only | up-arrow aura |
| Debuff | Debuffs player, no damage | tangling hand |
| OffensiveBuff | Attacks AND self-buffs | fist + aura |
| DebuffAttack | Debuffs AND deals damage | fist + tangle |
| SummonMinion | Spawns an enemy | beckoning hand / +figure |
| Idle | Does nothing | zzz / folded arms |
| DefendOpinion | Gains Denial (shields meter) | shield over a meter bar |
| RileOthers | Raises other enemies' hostility | shout radiating to neighbors |

---

## 6. Enemy portraits — `512 × 512` square bust — `EnemyData._portrait`

**Updated to the current prototype roster** (`Resources/Enemies/Prototype/Enemies/`) — the old `art-needed.md` list is stale. Filipino-political-satire busts; the stance/role should read on the face.

| Enemy | Role / Stance | Portrait direction |
|---|---|---|
| Loyal Partisan | Aggressive / Hostile | Snarling diehard in a campaign shirt, fist up. The baseline heckler. |
| Spin Doctor | Defensive / Neutral | Slick PR operative, phone + earpiece, unbothered smirk (raises Denial). |
| Heckler | Disruptive / Neutral | Loudmouth mid-jeer, mouth wide (Silences you). |
| Firebrand | Amplifier / Hostile | Charismatic agitator mid-shout, rallying the row. |
| The Bishop | Protector / Hostile (**Hardened**) | Stone-faced prelate, gold vestments, immovable — absolves allies of your statuses. |
| Swing Voter | Passive / **Receptive** | Uncertain ordinary citizen, hopeful but wary (will Turncoat if provoked). |
| The Fixer | Summoner / Neutral | Shadowy operator on a phone, summoning muscle. |

*(Elites — Televangelist, Dynast — come with the boss pass; not yet built.)*

---

## 7. Resource / cost icons — `128 × 128`
Small icons rendered next to the cost number. Author white/tintable.
- **Action Points** (energy) — the universal cost. Lightning/peso-spark.
- **Patronage** (₱) — Nepo Baby's banked favor currency. Coin/envelope-of-cash.
- **Attention** — Celebrity's spotlight resource. Camera-flash/spotlight.

---

## 8. Priority order for the artist (highest leverage first)
1. **3 rarity overlays** — currently identical; biggest readability win, tiny scope.
2. **5 distinct type frames** — only 3 exist; the color taxonomy is core to reading a hand.
3. **FL pacify status icons** (Guilt/Shame/Doubt/Jaded) + the 26 others — gameplay legibility for our lead class.
4. **10 intent icons** — needed to read the enemy turn.
5. **7 prototype enemy portraits.**
6. **Per-card illustrations** — Faith Leader 15 first (briefs in §3), others as their lists lock.

---

## Quick resolution reference

Sizes are **4K-ready** (author at the top of the Steam-Deck→4K range; see §0.5).

| Asset | Size | Format |
|---|---|---|
| Card type frame | 1500 × 2148 | PNG + alpha, transparent art window, mipmaps on |
| Rarity overlay | 1500 × 2148 | PNG + alpha, mostly transparent, mipmaps on |
| Card back | 1500 × 2148 | PNG, mipmaps on |
| Per-card illustration | 1500 × 2148 (full-bleed) | PNG, subject in art-window safe zone, mipmaps on |
| Status icon | 128 × 128 | PNG + alpha, flat/tintable |
| Intent icon | 128 × 128 | PNG + alpha, white/tintable |
| Enemy portrait | 1024 × 1024 | PNG, square bust |
| Resource/cost icon | 128 × 128 | PNG + alpha, white/tintable |

*(Note: the existing placeholder frames/backs are 1000×1432. New art should be authored at 1500×2148 — same 1:1.43 ratio, just 4K-capable. The current per-card `Character_` placeholders are 2048² source, already plenty.)*
