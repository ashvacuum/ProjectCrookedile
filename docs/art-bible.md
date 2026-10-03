# Crookedile — Art Bible & Handoff Spec

*Canonical art-direction + resolution spec for artists. Supersedes the thin `art-needed.md` checklist. All sizes are measured from the real assets/prefabs or set as authoring targets; the Content Hub (Statuses / Intents / Enemies tabs) is the live blank-slot checker.*

**Theme:** Filipino political roguelite satire — you "work a crowd," you don't fight. Tone: glossy campaign-poster sheen over something rotten. Religious-political iconography for Faith Leader, dynastic luxury for Nepo Baby, tabloid-celebrity gloss for Celebrity.

---

## 0.1 Style reference (LOCKED — confirmed 2026-09-06)

> **Rendering direction: the anime _Odd Taxi_, played livelier** — flat, muted, sophisticated, deadpan; grounded anthropomorphic cast with dry restraint. *Lively* is the one deliberate departure: Odd Taxi's stillness is the wrong read for a room you are actively working, so keep its flatness and its palette but not its inertia — more colour saturation in the accents, more posture and gesture in the cast, faces that are reacting rather than observing. Full generation prompt: `docs/reference/style-mock-prompt.md` (v4).
>
> **Reference of record: `docs/design/crookedile-style-v4-oddtaxi.png`** (approved 2026-07-03 — the flat muted committee-hall mock). This is the look to match: flat cel shading, dusty warm palette (mustard/teal/brick/cream/olive), thin clean lines, deadpan grounded animal-people, even soft lighting. The earlier dark-painterly mock is superseded.
>
> ⚑ **That file is not in the repo.** `docs/design/` holds two PNGs under GUID filenames — rename whichever is the approved v4 mock to the path above, and delete or clearly label the other, or this pointer stays dead for anyone the file is handed to.
>
> **The campaign map wears this same look, in 2:1 isometric.** Same palette, same flat cel shading, same thin lines — only the projection differs. Spec: §9. Prompt: `docs/reference/iso-tile-prompt.md`.

> **Card illustration is the exception (2026-10-02).** Card art is read at hand size, where Odd Taxi detail turns to clutter. Cards use the **icon style** in §3: one object, bold silhouette, flat colour blocks, one accent colour, plain background. Portraits, battle scene, map and UI keep the Odd Taxi direction above. `docs/reference/style-mock-prompt.md` still governs those; it no longer governs cards. Prompts live in `docs/crookedile-card-art-prompts.xlsx`.

**Rendering style:** flat cel shading, even soft lighting, thin clean line work. Dusty warm palette over grey neutrals. No digital-oil rendering, no low-key vignette, no grain — flatness is the point, and the liveliness comes from colour and posture, not from lighting drama.

**Composition (battle scene):** over-the-shoulder podium view — the player is IN the shot, back to camera, facing a raised row of opponent bust-portraits behind desks (committee-hearing framing). The crowd reads as a flat mass of shapes rather than rendered individuals. This is the "work the room" fantasy rendered literally.

**Palette anchors:**

> ⚑ **These values are still the superseded dark-painterly palette** — a near-black base is the
> opposite of the dusty mustard/teal/brick/cream/olive the locked direction calls for. The accent
> hues below (green/red/gold) survive the change in role; the base and chrome rows do not.
> Pull real values off the approved mock rather than guessing them here.

| Role | Color |
|---|---|
| Base / panels | ⚑ near-black charcoal & dark walnut `#141210` / `#2A2018` — **stale, needs the flat warm base** |
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

### Card art style: icon, not scene

The art carries the card's meaning so the description text can stay small. Reference point: Deadlock's ability icons.

- **One object + one verb.** One focal object whose shape or motion shows the effect. Two elements maximum, nothing behind them.
- **Silhouette first.** It must read as a solid shape at hand size. If it needs detail to be understood, simplify it.
- **Flat colour blocks** with a clean thick inked outline. No texture, gradients, glows or soft shadows.
- **Palette:** three base colours plus **one saturated accent** per card (gold for Faith Leader, hot pink for Celebrity). Frame colour already encodes type; the art does not repeat it.
- **Plain solid background**, centred subject.
- **Effect to shape:** damage = the object moving toward a target (thrust, motion arcs); defense = an enclosing or blocking shape (shield, ring, closed hand); draw = a fan of cards or one card sliding out; status = the object that stands for it; Flock / group = repeated identical silhouettes (3 or more); delayed = a still, waiting object.
- **Avoid:** scenes, crowds of distinct people, faces carrying the meaning, fine detail that vanishes at card size.

### Art briefs — Faith Leader (all 38 built cards)

One brief per card, from `Assets/Data/Cards/FaithLeader`. The mechanic summary comes from each card's effect list; the Midjourney-ready version of each brief is in `docs/crookedile-card-art-prompts.xlsx`.

| Card | Rarity | Type | Brief |
|---|---|---|---|
| Sermon | Basic | Pressure | a single raised open palm thrust forward with three motion arcs, one gold accent |
| Pressure | Basic | Pressure | one clenched fist pressing down onto a flat slab, one gold accent |
| Guilt | Basic | Pressure | one heavy weight hanging from a thin chain, one gold accent |
| Sow Discord | Basic | Pressure | one pointing finger above a single question mark, one gold accent |
| Drown out the Haters | Basic | Pressure | one megaphone with thick sound arcs pushing outward, one gold accent |
| Holier Than Thou | Basic | Pressure | one raised chin and pointing nose above a halo ring, one gold accent |
| Impenetrable Belief | Basic | Pressure | one thick round shield with a cross cut into its face, one gold accent |
| Quiet Reflection | Basic | Rhetoric | one closed eye above a still ripple ring, one gold accent |
| Shepherd's Joy | Basic | Rhetoric | one shepherd's crook with a single smiling sheep, one gold accent |
| Silent Majority | Basic | Rhetoric | one closed mouth with a finger pressed to its lips, one gold accent |
| Stir the Flock | Basic | Rhetoric | one wooden spoon stirring a swirl of identical sheep, one gold accent |
| Support | Basic | Rhetoric | one open hand holding up a single gold star |
| Traitor to the Cause | Basic | Rhetoric | one broken chain link with a dagger through it, one gold accent |
| Absolve Sins | Enhanced | Pressure | one hand releasing a chain of broken links upward, one gold accent |
| Blessed are the Persecuted | Enhanced | Pressure | one thorn crown glowing with a halo, one gold accent |
| I am speaking | Enhanced | Pressure | one raised palm in front of a crossed-out mouth, one gold accent |
| Name and Shame | Enhanced | Pressure | one pointing finger with a single speech bubble cracked in half, one gold accent |
| Preach | Enhanced | Pressure | one lectern with a megaphone fused on top, one gold accent |
| Quiet the Haters | Enhanced | Pressure | one hand over a row of three silenced mouths, one gold accent |
| Retribution | Enhanced | Pressure | one gavel striking down with impact lines, one gold accent |
| Sacrificial Lamb | Enhanced | Pressure | one lamb on an altar, one gold glow |
| The Other Cheek | Enhanced | Pressure | one cheek turned toward a stopped slapping hand, one gold accent |
| United Faith | Enhanced | Pressure | three identical hands clasped in a ring, one gold accent |
| Word is Law | Enhanced | Pressure | one open book stamped by a single gavel, one gold accent |
| Call out the Wicked | Enhanced | Rhetoric | one pointing finger aimed at a single dark silhouette, one gold accent |
| Congregation | Enhanced | Rhetoric | five identical silhouettes filing toward one gold doorway |
| Faith | Enhanced | Rhetoric | one steady flame inside a lantern, one gold accent |
| God's Chosen | Enhanced | Rhetoric | one gold crown floating above a single raised hand |
| Hallelujah | Enhanced | Rhetoric | two arms thrown up with gold light above |
| Insinuate | Enhanced | Rhetoric | one raised eyebrow above a sideways glance, one gold accent |
| Persecution Complex | Enhanced | Rhetoric | one pointing finger turned back at its own chest, one gold accent |
| Praise | Enhanced | Rhetoric | two hands clapping with a gold spark between them |
| Fruits of Conversion | Enhanced | Policy | one fruit branch with a single gold fruit |
| Gospel | Enhanced | Policy | one open book radiating a single beam of light, one gold accent |
| Have you no Shame? | Enhanced | Policy | one pointing finger above a bowed head, one gold accent |
| The World Is Against Us | Rare | Rhetoric | one small circle of figures facing out, shielded by a thick ring, one gold accent |
| Us vs Them | Rare | Policy | one thick vertical line dividing two identical silhouettes, one gold accent |
| Zealotry | Rare | Policy | one burning torch held aloft, one gold accent |

*(Card-art prompts for all three classes are in `crookedile-card-art-prompts.xlsx` (Nepo Baby tab added 2026-10-03, emerald green accent). Nepo Baby & Celebrity briefs are not written yet. Their card lists now exist — Nepo Baby's 42 cards in `nepo-baby-class.md` §5–6, Celebrity's Glamour/IOU set in `celebrity-glamour-iou.md` — so both are ready for the same template. Until art lands, Nepo cards can't appear in reward offers: cards without artwork are skipped.)*

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
- **Attention** — Celebrity's spotlight resource. Camera-flash/spotlight.

---

## 8. Priority order for the artist (highest leverage first)
1. **3 rarity overlays** — currently identical; biggest readability win, tiny scope.
2. **5 distinct type frames** — only 3 exist; the color taxonomy is core to reading a hand.
3. **FL pacify status icons** (Guilt/Shame/Doubt/Jaded) + the 26 others — gameplay legibility for our lead class.
4. **10 intent icons** — needed to read the enemy turn.
5. **7 prototype enemy portraits.**
6. **Per-card illustrations** — Faith Leader 15 first (briefs in §3), others as their lists lock.
7. **Campaign-map iso set** (§9) — starts when the campaign layer needs to look like anything; the HQ anchor first, then ground/road tiles, then buildings. Greybox is acceptable until then, so this stays last.

---

## 9. Campaign map — 2:1 isometric tiles & buildings *(LOCKED 2026-09-18)*

The campaign overworld is a 2D sprite city on a 2:1 isometric grid (`metagame-campaign.md`
§1.5 for why; this section is the authoring spec). Generation prompt + acceptance check:
`docs/reference/iso-tile-prompt.md`.

### Projection — one angle, no exceptions
**True 2:1 dimetric:** plan rotated 45°, camera elevation **26.565°** (`atan(0.5)`),
**orthographic**. A square ground footprint therefore draws as a diamond exactly twice as wide
as tall. Every tile, road and building uses this same projection and the **same key-light
direction (upper left)**. Mixed angles or mixed light are the one defect that cannot be fixed
by re-arranging the city — it has to be redrawn. Check both before import.

### Sizes (4K-ready, per §0.5 — author at the top of the range)
One grid cell = **256 × 128 px** at **PPU 256**, so one cell is one Unity world unit and a 4K
screen reads ~15 cells across. Building width is fixed by footprint; height is a ceiling, not
a target — draw only as tall as the building is.

**Footprints are W×L, not square.** A market row is 3×1, a covered court 2×3, a bridge longer
still. Both axes contribute half a cell to each dimension, so a W×L footprint's **base** is
`(W+L)·128` wide by `(W+L)·64` tall. Sprite height is that base plus however tall the building
stands — a ceiling, not a target.

| Asset | Base size | Notes |
|---|---|---|
| Ground / road tile (1×1) | **256 × 128** | the bare diamond, nothing above ground level |
| Building 1×1 | **256 × 128** base, ≤512 tall | the filler bulk of the set |
| Building 2×1 / 1×2 | **384 × 192** base | row houses, stalls |
| Building 2×2 | **512 × 256** base, ≤1024 tall | character buildings |
| Building 3×1 | **512 × 256** base | market stall rows |
| Building 3×3 | **768 × 384** base, ≤1280 tall | landmarks |
| Building 4×4 | **1024 × 512** base, ≤1536 tall | hero (Campaign HQ, city hall) |
| Zone prop (tree, pole, jeepney, tarpaulin) | ≤ **256 × 384** | scatter, sits inside one cell |

Note a square footprint draws as a diamond and a rectangular one as a **parallelogram** — 3×1
and 2×2 share a 512×256 base but are different shapes. The `IsoFootprintGizmo` draws the real
one; don't eyeball it from the numbers.

All PNG + alpha, transparent background, no baked cast shadow (a soft contact ellipse inside
the footprint is fine — long shadows break when the placer re-arranges the block).

### Pivot — the setting everyone gets wrong
The pivot sits at the **centre of the base diamond**, not the centre of the image. For an
N×N footprint the base diamond is `256N × 128N`, so its centre is **64N px up from the bottom
edge**: set a Custom pivot of `x = 0.5`, `y = 64N / spriteHeight`. A 1×1 building drawn
256×512 gets `y = 64/512 = 0.125`. Get this wrong and buildings float or sink by half a cell.

### Unity import & scene settings
- **Import:** PPU **256**, Filter Bilinear, Compression as §0.5, **mipmaps ON** (the map
  scales across the Deck→4K range), Max Size 2048.
- **Grid:** Cell Layout **Isometric Z as Y**, Cell Size `(1, 0.5, 1)`.
- **Sorting:** project-wide **Transparency Sort Mode = Custom Axis (0, 1, 0)**. Without it
  buildings render through each other — this is the classic iso failure, and it is a graphics
  setting, not an art problem.
- **Tilemap Renderer Mode:** `Chunk` for flat ground/roads (faster), `Individual` for anything
  with height, or tall sprites sort wrong against each other.
- Roads use **`RuleTile`**, ground variation uses **`RandomTile`** (both from
  `com.unity.2d.tilemap.extras`, already in the project).

### Checking a sprite
Two tools, same geometry:
- **In an image editor:** `docs/reference/iso-grid-guide-256x128.png` — a 2:1 lattice with 1×1 /
  2×2 / 4×4 markers. Drop it in as a top layer. In Krita you can instead use Grid and Guides →
  Type: Isometric with **both angles set to 26.57** (its default is 30°, which is a *different*
  projection and will quietly put every asset slightly wrong).
- **In the scene view:** the `IsoFootprintGizmo` component. Drop it on a candidate sprite and
  cycle the footprint until the diamond matches the base — that number goes in the zone list. It
  draws at the transform position, so a diamond that does not sit under the building's base means
  the **pivot** is wrong, which matters more than the art being a few percent off (pivots decide
  sort order; geometry does not).

### Naming
`iso_ground_<surface>`, `iso_road_<variant>`, `iso_bldg_<name>_<N>x<N>`, `iso_prop_<name>`.
One concept per file, per §0 delivery standards.

### Scope
≈**20–40 distinct buildings** before a generated city stops reading as four assets
copy-pasted. That count, not the tooling, is the campaign map's schedule. Generate the
Campaign HQ first and treat it as the style anchor every other asset is referenced against.

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
| Iso ground / road tile | 256 × 128 | PNG + alpha, PPU 256, mipmaps on (§9) |
| Iso building (1×1 … 4×4) | 256×≤512 … 1024×≤1536 | PNG + alpha, pivot at base-diamond centre (§9) |
| Iso zone prop | ≤ 256 × 384 | PNG + alpha (§9) |

*(Note: the existing placeholder frames/backs are 1000×1432. New art should be authored at 1500×2148 — same 1:1.43 ratio, just 4K-capable. The current per-card `Character_` placeholders are 2048² source, already plenty.)*
