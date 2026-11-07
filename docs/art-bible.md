# Crookedile — Art Bible & Handoff Spec

> **Kind:** Art · **Status:** Canonical · **Updated:** 2026-11-07
>
> **Summary:** Art direction, resolution spec and generation prompts (portraits, icons, card backs) for artists. Card prompts are in the card-art workbook; the Database window's tabs are the live blank-slot checker.
>
> **Source of truth:** this doc · **Related:** [`crookedile-card-art-prompts.xlsx`](crookedile-card-art-prompts.xlsx) · [`style-mock-prompt.md`](reference/style-mock-prompt.md)

*Canonical art-direction + resolution spec for artists. All sizes are measured from the real assets/prefabs or set as authoring targets; the Database window (Statuses / Intents / Enemies tabs) is the live blank-slot checker.*

**Theme:** Filipino political roguelite satire — you "work a crowd," you don't fight. Tone: glossy campaign-poster sheen over something rotten. Religious-political iconography for Faith Leader, dynastic luxury for Nepo Baby, tabloid-celebrity gloss for Celebrity.

---

## 0.1 Style reference (LOCKED — updated 2026-11-07)

> **Reference of record: [`docs/design/battle-ui-reference.png`](design/battle-ui-reference.png).** The look is this image:
> a detailed ink illustration in a dark, warm committee hall. When words and the image disagree, the image wins:
> attach it to every generation (edit, image-to-image or style reference) and describe only what changes.
>
> **Supersedes the flat *Odd Taxi* direction** (locked 2026-09-06, reference `crookedile-style-v4-oddtaxi.png`). Side by
> side, the battle reference read best, and it is not flat. Every prompt in the repo now uses the ink block (§10.1);
> the one copy outside it is listed under "Prompts in other files" below.

**Rendering style:** detailed ink illustration, like an engraved editorial cartoon. Confident dark linework with fine
hatching and cross-hatching for shadow and texture; fabric weave, wrinkles, scales and feathers drawn in, not
smoothed out. Colour is laid in warm, muted, slightly desaturated washes under the ink. No digital-oil blending, no
glossy 3D, no flat vector, no anime cel shading.

**Light and mood:** a dark, warm, sepia room. The officials and the speaker are lit from the front by warm light; the
public gallery behind them falls into brown shadow, and the corners go dark. Serious and a little grimy, never black
voids and never bright.

**Characters:** grounded, characterful anthropomorphic animals from Filipino political slang on human bodies in real
clothes (barong, suits, jackets, vestments). Faces carry attitude: smug, tired, suspicious, defiant. Satire through
expression and costume detail, not cartoon exaggeration. Not cute, not mascots.

**Composition (battle scene):** over-the-shoulder podium view — the player is IN the shot, back to camera, facing a
raised row of opponents behind a wooden panel (committee-hearing framing). **Behind them is the crowd:** a packed public
gallery in shadow with hand-painted protest signs, never a bare wall. It is the audience the Opinion Meter measures.

**Palette anchors** (sampled from the reference image, 2026-11-07):

| Role | Color |
|---|---|
| UI panels, top bar | near-black warm charcoal `#1C1B16` |
| Opinion fill, primary buttons, gold accents | mustard gold `#D39A3B` (End Turn `#C78F35`) |
| Crowd in shadow | deep brown-red `#4F291C` |
| Lower background | dark umber `#31281D` |
| Wood (desk, panel) | warm tan `#8F7B5E` |
| Card frame: Rhetoric | muted brick `#733526` |
| Card frame: Pressure | muted olive `#4B5334` |
| Card frame: Policy | muted slate teal `#344951` |
| Card text panel | warm parchment `#A3927C` |
| Hostile / danger accent | brick red (the Denial chevrons and HOSTILE tags) |
| Chrome lines / dividers | thin muted gold `#8A7448` |

These are averaged samples; re-sample against the source file before committing final UI art.

### Prompts in other files

The card, portrait, map and battle prompts all use the ink block in §10.1 (2026-11-07). One copy lives outside the
repo: the card-art workbook's style block comes from the Google Sheet (§10.5). The music prompt keeps its own jazz
direction; it borrows a soundtrack's mood, not a visual style.

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
┌─────────────────────────┐  ← all layers are 1500 × 2148, same silhouette
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
- **Card art** sits *behind* the frame and shows through the window — so author it **full-bleed 1500×2148** with the subject in the art-window safe zone (upper-center, see §3).
- **Rarity overlay** sits *on top* of the frame — mostly transparent, just adds the rarity treatment (gem, foil sheen, corner filigree).
- Text (name, description, cost number) is engine-rendered — **not** baked into art.

---

## 2. Card frames, overlays & backs

### 2a. Type frames — 5 — `1500 × 2148` — `CardVisualSettings._*Frame`
Same frame silhouette, color + motif per type. Transparent art window (~upper 55% of card), a nameplate strip near the top, a cost orb (top-left), and a lower description panel (semi-opaque so engine text reads on it).

| Slot | Type | Color (intent) | Hex anchor | Motif |
|---|---|---|---|---|
| `_pressureFrame` | Pressure | Green — persuade/de-escalate | `#3FА86B`* | calm laurel / handshake |
| `_rhetoricFrame` | Rhetoric | Red — aggressive | `#C0392B` | sharp, jagged, megaphone |
| `_policyFrame` | Policy | Blue — policy/lean | `#2C6FB5` | ledger / seal / document |
| `_statusFrame` | Heckle (status) | Purple — temporary junk | `#7D4CA8` | dashed/ephemeral border |
| `_curseFrame` | Scandal | Dark crimson — unplayable clog | `#6E1B2E` | torn tabloid / redacted bars |

\* tune in engine; these are direction, not law. **Current gap:** all 5 point at placeholder `CardFront_01–03`; only 3 distinct frames exist.

### 2b. Rarity overlays — 3 — `1500 × 2148` (mostly transparent) — `CardVisualSettings._*Frame`
Drawn over the type frame. Keep the art window + textbox clear.

| Slot | Rarity | Treatment |
|---|---|---|
| `_basicFrame` | Basic | None or a thin matte border. The plain floor. |
| `_enhancedFrame` | Enhanced | Silver/bronze corner filigree + subtle inner bevel. |
| `_rareFrame` | Rare | Gold frame accents + foil sheen + corner gems. Should read as "rare" at a glance. |

**Current gap:** all 3 reuse the pressure-frame placeholder — no visual rarity difference exists yet. This is the highest-value frame work.

### 2c. Card backs — 4 — `1500 × 2148` — `CardVisualSettings._*CardBack`
Per origin, shown face-down. `_defaultCardBack`, `_faithLeaderCardBack` (religious seal), `_nepoBabyCardBack` (dynastic crest), `_actorCardBack` (celebrity monogram/star). These exist (`CardBack_01/02`) but only 2 distinct.

---

## 3. Per-card illustration (the art window)

The `Character_NN` sprites are **placeholders** (random assignment). Real per-card art is authored **full-bleed 1500×2148, portrait**, subject framed in the **art-window safe zone**: roughly `x: 105–1395, y: 165–1200` (upper-center; the old 1000×1432 window scaled ×1.5). Keep critical detail out of the bottom ~45% (textbox) and the top ~8% (nameplate). Confirm the exact window against the frame PSD before final crops.

### Card art style: ink still life

The art carries the card's meaning so the description text can stay small. Reference prompt that set the direction: *an empty church pew, a single lit candle, a neatly folded barong draped over the backrest, nobody in the room.*

- **Still life.** Two to four ordinary objects that imply a moment: someone just left, something is about to happen. The room tells the joke; nothing explains it.
- **One quiet twist** on about a third of cards: one impossible detail played straight (a shadow that kneels, steam shaped like sheep). Magical realism, never fantasy: no wands, sparkles, runes or creatures.
- **Figures** on about a third of cards, one or two at most: plain silhouettes, hands at the frame edge, or grounded anthropomorphic animals in ordinary clothes. Never a crowd, never the focus.
- **Ink rendering:** the §0.1 look: detailed ink linework with hatching, warm muted washes, low warm light. Objects and
  silhouettes must still read as bold shapes at hand size, like the reference image's cards (a pointing hand, a candle
  between praying hands, figures silhouetted in a doorway).
- **Accent:** the only saturated colour, per class: gold Faith Leader, hot pink Celebrity, emerald green Nepo Baby, purple Heckle, crimson Scandal.
- **Framing:** subject in the upper centre two thirds; the lower 45% stays quiet for the textbox.
- **Check at hand size** (~650px tall). If the moment doesn't read there, cut an object.

Per-card subjects, the style block and the assembled prompts are in `docs/crookedile-card-art-prompts.xlsx` (Settings, Art Rules, one tab per class).

---

## 4. Status icons — 29 — `128 × 128` — `StatusEffectIconMapSO`

Flat / single-color silhouette style, readable at a ~32px badge. Author white-on-transparent or single-hue; the map can tint. Source of truth = `StatusRegistry`.

- **Player debuffs:** Weakened, Vulnerable, Frail, Entangled, Exposed, Confused, Silenced, Stunned, Rattled, Smear
- **Player buffs:** Strength, Dexterity, Focus, Energized, Plated, Regeneration, Intangible, Thorns, Ritual, Momentum, Echo
- **Faith Leader pacify:** Guilt, Jaded (Shame and Doubt retired 2026-10-31; no icons needed)
- **Hostility flags (on enemies):** Hardened, Fanatic, Devotion, Turncoat

Suggested motifs for the FL-relevant ones (these drive readability of our lead class): **Guilt** = downcast weight/chain; **Jaded** = cracked halo / rolling eyes; **Hardened** = stone wall; **Fanatic** = wide-eyed flame; **Turncoat** = flipped/two-face.

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

## 6. Enemy portraits — `1024 × 1024` square bust — `EnemyData._portrait`

Square bust, head-and-shoulders, facing camera from slightly below (they sit at a raised committee panel). The role reads on the face. Animal casting is locked in style-mock v4 for the first seven; the prompt block is §10.1.

| Enemy | Stance | Behaviour | Subject line |
|---|---|---|---|
| Loyal Partisan | hostile (2, range −7…5) | The baseline attacker | A stocky bulldog *(tuta)* in a plain screen-printed campaign shirt, flat tired stare, one fist resting on the desk — loyal out of habit, not passion |
| Spin Doctor | neutral | Shields the meter (Denial) | A lean snake *(ahas)* in an unremarkable grey suit, half-lidded eyes, earpiece, phone face-down, entirely unbothered |
| Heckler | neutral | Silences you | A rooster mid-jeer, beak open, permanent smirk, leaning back with one arm hooked over the chair back |
| Firebrand | hostile | Rallies the row | A wiry fighting cock *(sabungero)* in a sleeveless shirt, coiled forward but calm-faced, about to stand |
| The Bishop | hostile, Hardened | Absolves allies of your statuses | A heavy-set carabao in modest vestments and a plain pectoral cross, serene, immovable, hands folded on the desk |
| Swing Voter | receptive | Turncoats if provoked | A soft-spoken chameleon in an ordinary polo, eyes drifting to one side, hopeful and wary at once |
| The Fixer | neutral | Summons muscle | A watchful monitor lizard *(bayawak)* holding a phone low under the desk, eyes up on you the whole time |
| Curious Student ⚑ | — *(no casting yet)* | — | **Proposed:** a young maya (sparrow) in a university lanyard and plain shirt, notebook open, actually listening — the only face in the row that isn't performing |
| The Incumbent ⚑ | rival boss above the row · final boss | Rigged room: his machine (2 Loyal Partisans, Swing Voter, Tandang); moves in `boss-brain.md` | **Proposed:** an old, heavy crocodile in a perfectly tailored barong with a sash, three decades of campaign posters behind him; your own species thirty years further in — flat, patient, utterly unimpressed |

`docs/enemy-design-bible.md` §4 specifies a **different, animal-named roster** (Askal, Maya, Carabao, Parrot, Rat, Cat Tita, Musang, Butiki, Bayawak, Ahas, Tandang, Peacock, Agila, Uwak, Gagamba) across 15 encounters. The nine assets above are the older generic set. Docs are canonical, so **do not commission the nine as final** until the roster question is settled — otherwise you pay for portraits the encounter tables never call. See §10.5.

*(Elites — Televangelist, Dynast — come with the boss pass; not yet built.)*

---

## 7. Resource / cost icons — `128 × 128`
Small icons rendered next to the cost number. Author white/tintable.
- **Action Points** (energy) — the universal cost. Lightning/peso-spark.
- **Glamour** — Celebrity's status. Camera-flash/spotlight.
- **Debt** — Celebrity's IOU. A torn IOU slip.

---

## 8. Priority order for the artist (highest leverage first)
1. **3 rarity overlays** — currently identical; biggest readability win, tiny scope.
2. **5 distinct type frames** — only 3 exist; the color taxonomy is core to reading a hand.
3. **FL pacify status icons** (Guilt/Jaded) + the 26 others — gameplay legibility for our lead class.
4. **10 intent icons** — needed to read the enemy turn.
5. **7 prototype enemy portraits.**
6. **Per-card illustrations** — Faith Leader 15 first (subjects in the card-art workbook), others as their lists lock.
7. **Campaign-map iso set** (§9) — starts when the campaign layer needs to look like anything; the HQ anchor first, then ground/road tiles, then buildings. Greybox is acceptable until then, so this stays last.

---

## 9. Campaign map — 2:1 isometric tiles & buildings *(LOCKED 2026-09-18)*

The campaign overworld is a 2D sprite city on a 2:1 isometric grid (`metagame-campaign.md`
§1.5 for why; this section is the authoring spec). It wears the §0.1 ink look: linework with light hatching and
warm muted washes, drawn to the one projection below. Generation prompt + acceptance check:
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

## 10. Generation prompts

Paste the style block first, then the subject line. Card illustrations are not here: their style block and every card's
subject line are in [`crookedile-card-art-prompts.xlsx`](crookedile-card-art-prompts.xlsx) (Settings tab + one tab per
class), following the rules in §3.

### 10.1 Portraits (ink block)

Paste this base block, then the portrait ending below it.

```
Detailed ink illustration in the manner of a dark editorial cartoon: confident dark linework with
fine hatching and cross-hatching for shadow and texture; fabric weave, wrinkles, scales and feathers
drawn in. Colour laid in warm, muted, slightly desaturated washes under the ink: mustard gold, brick
red, olive, slate teal, parchment, deep umber. Warm low light from the front; edges fall into brown
shadow, never pure black. Filipino political satire: grounded, characterful anthropomorphic animals
on human bodies in real clothes (barongs, suits, jackets, vestments), faces with attitude: smug,
tired, suspicious, defiant. Not cute, not mascots, not anime, not flat vector, not glossy 3D, not oil
painting.
Portrait composition, subject in the upper-centre two-thirds, lower 45% quiet and uncluttered
(a textbox sits there). No text, no letters, no logos, no watermark, no UI frame.
```

Attach `docs/design/battle-ui-reference.png` as a style reference with it wherever the tool allows.

*Midjourney tail:* `--ar 7:10 --style raw --s 150 --no text,letters,watermark,frame`
*Gemini / ChatGPT tail:* `Render at 1500x2148 px, portrait, PNG.`

Swap its last two sentences (composition + "No text...") for:

```
Square bust portrait, head-and-shoulders, facing camera from slightly below (they sit at a
raised committee panel). Plain warm umber background fading to shadow, no scenery. The role reads on the face.
```

*Midjourney tail:* `--ar 1:1 --style raw --s 150` · *target 1024x1024.*

### 10.2 Ally, status and intent icons

```
Flat single-colour silhouette icon, pure white on transparent, no outline weight variation,
no gradient, no text. Must read clearly at 32 px. Centred with ~6% margin. Simple bold shape.
SUBJECT: <paste subject line here>
```

*Target 128x128 (ally icons 256x256 — see §10.4).*

### 10.3 Player origins and card backs

All three are the same crocodile (*buwaya*) — the wardrobe is the satire. Composition: back to camera at a podium for scene art; three-quarter bust for portraits.

| Origin | Subject line |
|---|---|
| Faith Leader | A composed crocodile politician in a plain white barong with a large wooden cross over it, sleeves pressed, a worn bible under one arm; calm, patient, unreadable |
| Nepo Baby | A young crocodile in an expensive slim-cut barong with a heavy gold family signet ring and a watch too big for him, bored posture, phone face-down on the lectern |
| Celebrity | A crocodile in designer sunglasses indoors, barong worn open over a tee, practised half-smile aimed slightly off-camera at where the photographers are |

**Card backs** (`1500x2148`, 4 needed — default + one per origin): Faith Leader = an embossed religious seal; Nepo Baby = a dynastic family crest; Celebrity = a monogram-and-star; default = the crocodile silhouette. Flat, symmetrical, single motif, muted two-tone.

### 10.4 Ally icons

Allies are passive relics, not row bodies. Suggest **256×256**, flat single-colour silhouette (block §10.2), so they
read in a relic tray at ~48 px.

| Ally | Rarity | Mechanic | Subject line |
|---|---|---|---|
| The Beef | Basic | Every 3rd time an enemy's hostility rises, gain 3 Support | Two crossed forearms braced against each other, shoulders squared |
| Crowd Control | Basic | When an enemy turns receptive (first 3 turns), meter +2 | A rope-barrier stanchion with the rope looped neatly, a crowd shape behind it |
| Devil's Advocate | Enhanced | When the room tips receptive, re-radicalise a random hostile by 10 | A single raised hand in a room of lowered ones |
| Fixer's Cousin | Enhanced | Every 5th card played, one card becomes free | A folded envelope of cash passed under a table edge |
| Hail Mary | Rare | Once per battle, while the meter is critically low, meter +10 | A thrown basketball frozen at the top of its arc, the hoop far off at the edge of frame |
| Anonymous Tip | — | see the asset | *not written yet* |
| Burner Phone | — | see the asset | *not written yet* |
| Opposition File | — | see the asset | *not written yet* |
| The Driver | — | see the asset | *not written yet* |

### 10.5 Settle before commissioning

1. **The enemy roster is two different sets** (§6). It decides whether you commission 9 portraits or ~15.
2. **Three "Enhanced" cards are authored as Basic rarity**: Holier Than Thou, Impenetrable Belief and Sermon sit in
   `Cards/FaithLeader/Enhanced/` with `_rarity: 0`, so they'd get the plain overlay.
3. **The Incumbent** (final boss) has moves and a room now (`boss-brain.md`) but no portrait or casting yet.
4. **The card-art workbook's style block** (Settings and Art Rules tabs) still describes the flat look. It is pulled
   from the Google Sheet, so change it there: replace it with the §10.1 ink block, and the rendering rule with §3's.

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
