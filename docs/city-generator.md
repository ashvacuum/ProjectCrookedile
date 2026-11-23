# City generator — half-cube campaign city

> **Kind:** Design · **Status:** Proposal · **Updated:** 2026-11-23
>
> **Summary:** The campaign city: a Cebu-like city generated fresh each run from the seed, drawn with half-cube meshes on a 2:1 iso grid. Coast, radial avenues, streets, zones, landmarks and buildings; a painter for saved test maps. Replaces the sprite CityGenerator.
>
> **Source of truth:** this doc · **Related:** [`metagame-campaign.md`](metagame-campaign.md) · [`art-bible.md`](art-bible.md) · [`campaign-encounters.md`](campaign-encounters.md)

*Agreed 2026-11-23. Supersedes `metagame-campaign.md` §1.5–1.6 and the building half of `art-bible.md` §9 (sprite
buildings on a Tilemap, `CityGenerator`, `CityZoneData`). Nothing here is built yet; the half-cube prototype in
`Assets/Art/AI/Iso` is the starting point.*

## 1. What the city is

- **Generated per run, rules only.** Every run builds a new city from `RunState.Seed`; the same seed gives the same
  city. There is no hand-made template: the Cebu feel comes from the rules (a coastal downtown, avenues radiating
  inland, density falling off with distance). Hand-painted maps exist only as saved test maps (§7).
- **Scenery only.** The city does not know where districts are and never decides where an encounter happens.
  Encounter scheduling stays a function of the pool, the day window and the travel network
  (`campaign-encounters.md` §6). Location hotspots stay uGUI placed from `MapLocation` data over the map.
- **Close to Cebu, not a replica.** No real map data is imported.

## 2. Grid

- **Cell = 1 × 0.5 world units** (256 × 128 px at PPU 256, `art-bible.md` §9). A 4K screen shows about 15 cells across;
  the camera pans.
- **Cell to world:** cell `(row, col)` sits at `x = (col + row) · 0.5`, `y = (row − col) · 0.25`. `+row` runs up-right on
  screen, `+col` runs down-right. The half-cube prototype used 0.4 × 0.2 half extents; everything scales by 1.25.
- **Size: 64 × 64 cells**, a tunable field.
- **Grid diagonals are screen-straight.** One step along both axes at once draws as a horizontal or vertical line on
  screen. Diagonal avenues (§4.3) use this.

## 3. Data

| Piece | Kind | Holds |
|---|---|---|
| **City map** | plain data, saved as `CityMapData` (SO) for test maps | size; per cell: ground (land, sea), road (none, street, avenue), zone; list of placed buildings (building, footprint origin, W × L, storeys) |
| **`ZoneData`** | SO, Database tab | name, distance band (§4.4), placeholder colour, weighted building list, landmark list |
| **`BuildingData`** | SO, Database tab | id from the asset GUID; face atlas material and roof / left wall / right wall cells; allowed W × L footprints; storey range and storey height |
| **Generator settings** | serialized fields on the generator | size, coast depth range, avenue count range, run-length range, block size limits, zone list ordered centre-outward |

Every number the generator uses is a serialized, tooltipped field. None are constants.

## 4. Generation, in order

Deterministic from the seed through its own `System.Random`, so generating the city never perturbs battle RNG
(`BattleRandom`) or the encounter pool's draws.

### 4.1 Coast
The seed picks one map edge as the sea. The coastline runs along it with a seeded, jagged depth inside a tunable
range. Sea cells are ground = sea; nothing else is placed on them.

### 4.2 Downtown
A point on the coastline, near its middle with a seeded offset. Zones and avenues are measured from it.

### 4.3 Radial avenues
A seeded number of avenues (default 3–5) start at downtown and head inland to the map edge, spread across the inland
half-plane. Each avenue is **two cells wide** and moves in **eight directions** (both grid axes and the two
diagonals): long straight runs (a tunable length range) joined by 45° bends, so it reads as a winding boulevard.
Avenues may cross each other; a crossing is an avenue junction.

### 4.4 Streets
Land between the avenues splits into regions. Each region is cut recursively by **one-cell streets, along the grid
axes only**, at a seeded position across its longer side, until no block is wider than the max block size; a cut
that would leave a block thinner than the min block size is refused. Streets stop where they meet another road, so
every block touches a road.

### 4.5 Zones
Each block takes the first zone whose distance band contains the block's distance from downtown (zones are ordered
centre-outward: element 0 is downtown, the last is the edge of town).

### 4.6 Landmarks
Each zone lists must-have landmarks. The generator places each once, before ordinary buildings, in the largest block
of that zone that fits it, fronting a road. A landmark that fits nowhere is skipped with a `GameLogger` warning.

### 4.7 Buildings
Each block is packed largest footprint first from its zone's weighted list; storeys roll within the building's range.

## 5. Roads and tiles

- **Neighbour-picked art.** A road cell's art and rotation come from its **eight** neighbours (eight because of
  diagonal avenues). Streets use the one-cell road set (straight, corner, T, cross, dead end, plus straight variants:
  crosswalk, manhole, storm drain). Avenue cells are half sidewalk, half carriageway; the two cells of an avenue mirror
  each other.
- **Placeholders first.** Any piece without art draws as a flat placeholder colour, so the generator and renderer can
  be finished before the art. The renderer reports which pieces fell back to placeholders.
- **Art needed (backlog):**
  - sea and coast edges (straight, inner and outer corners);
  - avenue pieces: corners, avenue-meets-street junctions, avenue crossings (the road atlas has edge and corner pieces;
    which of them fit is an art-audit task);
  - the diagonal set: diagonal straight, the half pieces on the cells flanking a diagonal, axis-to-diagonal bends, and
    diagonal-meets-street junctions;
  - a face atlas per building type (only `CampaignBlock` exists).

## 6. Rendering

- **Ground and roads never overlap**, so they draw beneath everything and do not sort against buildings. They are baked
  into chunk meshes (16 × 16 cells), with each cell's atlas cell and rotation carried in the vertex data, so a 64 × 64
  map is 16 meshes rather than 4,096 objects.
- **Buildings are individual half-cube meshes** (`HalfCubeBuilding`: footprint in tiles, wall height). A building's base
  is exactly its footprint's diamonds. Buildings sort by their front cell's grid depth.
  - *Known limit:* long footprints beside each other can still sort wrong (the classic iso case). Measure it on real
    generated maps before adding splitting.

### 6.1 Lighting: one 3D sun

Buildings are lit as the 3D boxes they stand for, not as flat 2D shapes.

- **Face normals are true 3D directions in view space** (camera elevation 26.565°, plan rotated 45°):

  | Face | Normal (x, y, z; −z faces the camera) |
  |---|---|
  | Roof | (0, 0.894, −0.447) |
  | Left wall | (−0.707, −0.316, −0.632) |
  | Right wall | (0.707, −0.316, −0.632) |

  Ground and road cells use the roof normal. With these, a sun behind a building lights its roof and leaves both
  front walls in shade. The prototype's roof faces the camera instead, which is why its back-lighting looks wrong.
- **One sun, set as shader globals.** A world-space direction (azimuth and elevation) plus colour, ambient colour and
  window emission, set once per frame by one lighting component and converted to view space for the shader. It
  replaces the Light2D key light, URP's 2D normal-map lighting on city meshes, the shader's second key-light term,
  the per-object `HalfCubeLightLink` and the per-renderer loop in `CityDayNightCycle`.
- **The sun follows the campaign clock.** The day runs 08:00 to 16:00 (`campaign-encounters.md` §6): low morning sun
  from one side at the start, rising, then late-afternoon sun from the other side at the end. Azimuth, elevation and
  colour over the day are tunable curves and gradients. Night lighting only applies if a day ever runs past dusk.

### 6.2 Shadows: height-aware, from a height texture

URP's `ShadowCaster2D` is not used: it projects each building's whole outline across the screen with no idea of
height, so a short building darkens a tall neighbour's roof.

- **Height texture.** The renderer writes one texel per cell (64 × 64 for the default map) holding the height of
  whatever stands there: 0 for ground and roads, the roof height for a building's footprint cells. It is rebuilt only
  when the map changes (generation, painter edits, loading a test map).
- **Every lit pixel knows its 3D position.** Ground, wall and roof vertices carry their grid position and height, so
  each pixel of the city knows where it is in the 3D city, not just on screen.
- **Shadow test by stepping toward the sun.** For each pixel, the shader steps from that 3D position toward the sun
  across the height texture. If any step lands in a cell taller than the ray at that point, the pixel is in shadow.
  So shadows fall on streets, on walls and on lower roofs, with length set by building height and sun elevation, and a
  shorter building never shades a taller one's roof.
- **Tunables:** step count and maximum distance (a long low sun needs more steps), shadow strength and colour, and a
  small bias so a surface never shadows itself.
- *Known limits:* each cell has one height, so a building's shape is its box; anything finer (awnings, rooftop
  props) casts no shadow. Edges are as sharp as one cell of the height texture unless the steps are blended; soften
  with a few jittered samples if they read as stair-stepped.

### 6.3 Rain

Rain is a gameplay travel modifier on some districts for a whole day (`campaign-encounters.md` §6.2). The map does not
know districts, so whenever any district is rainy **the whole map shows rain**; the travel UI marks which roads it
slows.

- **Two shader globals.** *Rain intensity* (is it raining) drives the light and the falling streaks. *Wetness* follows
  it with a lag: surfaces soak in over a few seconds when rain starts and dry slowly when it stops, puddles last.
- **Overcast light**, through the §6.1 sun: dim the sun, shift ambient to a cool grey, weaken and soften the §6.2
  shadows, raise window emission. A slight desaturation and haze come from the URP Volume (settings, no code).
- **Wet surfaces in the half-cube shader:**
  - albedo darkens with wetness, more on pavement and concrete than on glass or metal;
  - surfaces reflect the **overcast sky colour**, not a sun highlight, strongest on flat surfaces (roofs, roads, ground)
    and weak on walls;
  - **puddles** on roads and flat roofs come from a puddle-shape texture whose cut-off rises with wetness, starting in
    crevices (the occlusion map); inside a puddle the surface turns smooth and mirror-like.
- **Falling rain:** two or three scrolling layers of slanted streaks over the whole screen at different speeds,
  angled by wind. Cheaper than particles and works at any zoom.
- **Polish, last:** ripples in puddles, sparse splash particles, window glow smearing into wet roads.
- Rain visuals are cosmetic, so their animation may use `UnityEngine.Random`; whether it rains comes only from the
  seeded travel roll.

## 7. Painter and test maps

A Scene-view tool on the city view, for testing art, tiles and packing:

- **Generate** from any seed in the editor.
- **Paint** ground (land, sea), streets, avenues and zones; **place and remove** buildings; Undo works.
- **Save** the current map as a `CityMapData` test map, and **load** one in place of the generated city: an editor
  field on the city view, plus a dev-console command. Loading a test map only changes scenery, so the command is
  `Safe = true`.
- Runs always generate; a test map is never used by a normal run.

## 8. Removed

The sprite system is deleted, not migrated: `CityGenerator.cs`, `CityZoneData.cs` and its one asset `Factory.asset`,
`IsoFootprintGizmo.cs`, the sprite building prefabs in `Assets/Art/AI/Iso/Buildings`, the `CityZoneData` entries in
`ContentAssetNaming.cs`, and the gizmo's row in the readme's data-file table.

## 9. Build order

1. **Foundations:** the 3D sun and corrected face normals (§6.1), height-aware shadows (§6.2), the 1 × 0.5 grid, chunked ground meshes, building sorting by grid depth.
2. **City map and renderer** with placeholder tiles and neighbour-picked road art.
3. **Generator:** coast, downtown, avenues, streets, zones, landmarks, buildings.
4. **Painter** and saved test maps.
5. **Remove** the old system (§8).
6. **Lighting from the campaign clock**, then **rain** (§6.3; the travel rule in `campaign-encounters.md` §6.2).

## 10. Choices made without a separate decision

These follow from the answers above; change them here if they are wrong.

- Streets are axis-aligned only; only avenues run diagonally.
- The sea edge is picked by the seed rather than fixed.
- Avenue count defaults to 3–5.
- A landmark that does not fit is skipped with a warning, not forced.
- Chunk size 16 × 16.
- Elevation is out of scope for now; the cell data leaves room for a height level later.
