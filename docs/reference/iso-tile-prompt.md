# Isometric map-asset generation prompt (2:1 dimetric)

> **Kind:** Reference · **Status:** Canonical · **Updated:** 2026-11-07
>
> **Summary:** Campaign-map generation prompt: 2:1 dimetric tiles and buildings, plus the acceptance check every sprite has to pass.
>
> **Source of truth:** this doc; grid guide [`iso-grid-guide-256x128.png`](iso-grid-guide-256x128.png) · **Related:** [`metagame-campaign.md`](../metagame-campaign.md) · [`art-bible.md`](../art-bible.md) · [`style-mock-prompt.md`](style-mock-prompt.md)

*Companion to `style-mock-prompt.md` (v6, the current look; `art-bible.md` §0.1). That prompt makes battle
art; this one makes the campaign map's tiles and buildings — `metagame-campaign.md` §1.5,
sizes and import settings in `art-bible.md` §9. Drop generated images beside this file.*

---

## 0. Read this before you generate anything

**Image models do not respect exact projection geometry.** You will not prompt your way to a
mathematically true 2:1 dimetric base. Prompts get you the *style, silhouette and palette*;
the base diamond gets fixed afterwards. Plan for that instead of re-rolling forty times.

The workflow that actually converges:

1. **Generate one anchor building** (recommended: the Campaign HQ). Iterate until the style
   is right. This image is now the style reference for everything else.
2. **Correct its geometry by hand** — snap the base to a true 256×128 diamond, straighten the
   verticals. Save as `_anchor_hq.png`. Everything downstream inherits this.
3. **Generate the rest with the anchor attached as an image reference**, not from the text
   prompt alone. Forty independent text-only generations produce forty slightly different
   light angles and line weights, and that reads worse than four repeated buildings.
4. **Ship the sprite only after the base-diamond check** (§3).

**When hand-correcting gets expensive** (roughly: past the first dozen buildings, or the
moment roofs start disagreeing), switch to a 3D blockout rendered orthographically at 26.565°
and stylize over the render. Guaranteed-correct geometry and a single consistent light for
free; it is a standard 2D-iso production route, not a betrayal of the sprite look.

---

## 1. The technical block (paste verbatim into every generation)

> True isometric game asset, **2:1 dimetric projection** — the object's square ground footprint
> reads as a diamond exactly **twice as wide as it is tall**, plan rotated 45°, camera elevation
> 26.57°. **Strict orthographic: parallel edges, no perspective, no vanishing point, no
> foreshortening** — the far edge of the roof is exactly as long as the near edge. Vertical
> edges are perfectly vertical in the image. Single object, centered, complete and unclipped,
> on a **fully transparent background**. Fixed key light from the **upper left**, soft and even;
> thick ink outlines and at most two painted tones per surface; no cast shadow on the ground, no ambient
> occlusion pooling outside the footprint. Confident thick ink outline. Sprite-sheet asset, not
> a scene.

## 2. The style block (paste verbatim, unchanged — this is the locked look)

> Hand-drawn 2D: confident thick ink outlines with varied weight and clear silhouettes, softly
> painted surfaces with visible brushwork, warm lighting, rich but slightly muted colour: mustard,
> teal, brick red, cream, olive. Appealing and lived-in. Filipino urban flavor kept understated and lived-in: corrugated
> tin roofing, hand-painted signage, a campaign tarpaulin zip-tied to a fence, tangled overhead
> wires, a sari-sari store's hanging sachets — texture, not clutter.

## 3. The negative block

> NOT perspective, NOT three-point or two-point perspective, NOT a tilted or rotated camera,
> NOT top-down, NOT a front elevation, NOT a drop shadow on the canvas, NOT a ground plane or
> grass or pavement extending past the building's own footprint, NOT multiple views or a
> turnaround sheet, NOT a checkerboard or white or colored background, NOT text, labels,
> watermarks, rulers or grid overlays, NOT characters or people, NOT oil painting, NOT grimdark,
> NOT bright slapstick caricature, NOT cute, NOT photoreal, NOT glowing outlines or rim light.

## 4. The subject slot

Assemble as: **`{SUBJECT}` + §1 technical + §2 style + §3 negative.** Keep the subject to one
sentence and name its footprint, so the model sizes the mass correctly:

> A two-storey **{SUBJECT}** occupying a **{N}×{N}** tile footprint.

### Subject list — the campaign city (target ≈20–40 for a non-repetitive map)

| Footprint | Subjects |
|---|---|
| 1×1 filler (bulk of the set) | sari-sari store · tricycle-terminal shed · concrete row house · shanty with tin roof · corner eatery (carinderia) · bus-stop waiting shed · vulcanizing shop · water-refilling station · barangay outpost · empty lot with tarpaulin fence |
| 2×2 character | wet market stall row · basketball covered court · public school block · jeepney terminal · funeral parlour · videoke bar · gated-subdivision house · fast-food branch · cockpit arena (sabungan) · pawnshop-and-remittance corner |
| 3×3 landmark | barangay hall · parish church · TV/radio station · municipal plaza with monument · public market hall |
| 4×4 hero | **Campaign HQ** (the anchor asset — generate this first) · city hall |

### Ground and road tiles (RuleTile / RandomTile fodder)

Same three blocks, with the subject replaced by:

> A single flat **{SURFACE}** ground tile — the whole image is one **256×128** diamond, filling
> the frame corner to corner, with nothing above ground level.

Surfaces: cracked asphalt · concrete slab · patched dirt · painted road with faded centre line
· pedestrian crossing · plaza tile · grass verge · flooded gutter. Roads additionally need the
straight / corner / T / cross / dead-end variants — generate the straight first and derive the
rest from it so the line weights match.

---

## 5. Acceptance check (run on every sprite before import)

1. **Base diamond** — drop a 256×128 diamond guide on the footprint. Width must be exactly
   twice the height. This is the one that silently ruins a map; check it first.
2. **Verticals** — walls are vertical in the image, not leaning.
3. **Light** — key from upper left, same as `_anchor_hq.png`. Hold two side by side.
4. **Alpha** — background fully transparent, no white fringe, no baked cast shadow.
5. **Pivot** — sits at the base diamond's centre (`art-bible.md` §9), not the image centre.
6. **Tiling** — placed next to its neighbours on the real grid, the roofs do not disagree
   about where the sun is.
