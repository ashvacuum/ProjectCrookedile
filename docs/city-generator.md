# City generator — half-cube campaign city

> **Kind:** Design · **Status:** Partly built · **Updated:** 2026-11-26
>
> **Summary:** The campaign city: a Cebu-like city generated fresh each run from the seed, drawn with half-cube meshes on a 2:1 iso grid. Coast, radial avenues, streets, zones, landmarks and buildings; a painter for saved test maps. Replaces the sprite CityGenerator.
>
> **Source of truth:** this doc · **Related:** [`metagame-campaign.md`](metagame-campaign.md) · [`art-bible.md`](art-bible.md) · [`campaign-encounters.md`](campaign-encounters.md)

*Agreed 2026-11-23. Supersedes `metagame-campaign.md` §1.5–1.6 and the building half of `art-bible.md` §9 (sprite
buildings on a Tilemap, `CityGenerator`, `CityZoneData`). Code lives in `Assets/Scripts/Gameplay/City` (runtime) and `Assets/Scripts/Editor/City` (tools); the shader, materials,
atlases and test scenes stay in `Assets/Art/AI/Iso`.*

## 1. What the city is

- **Generated per run, rules only.** Every run builds a new city from `RunState.Seed`; the same seed gives the same
  city. There is no hand-made template: the Cebu feel comes from the rules (a coastal downtown, avenues radiating
  inland, density falling off with distance). Hand-painted maps exist only as saved test maps (§7).
- **Scenery, plus where districts stand.** The city never decides where an encounter happens: encounter scheduling
  stays a function of the pool, the day window and the travel network (`campaign-encounters.md` §6), and travel times
  stay authored on `CampaignTravelData`'s roads, never measured from the generated streets. The one thing the city
  knows is where each district's marker stands (§4.8, *ruled 2026-11-25*; this replaces the earlier "the city does not
  know where districts are" and the unbuilt `MapLocation` hotspots).
- **Close to Cebu, not a replica.** No real map data is imported.

## 2. Grid

- **Cell = 1 × 0.5 world units** (256 × 128 px at PPU 256, `art-bible.md` §9). A 4K screen shows about 15 cells across;
  the camera pans.
- **Cell to world:** cell `(row, col)` sits at `x = (col + row) · 0.5`, `y = (row − col) · 0.25`. `+row` runs up-right on
  screen, `+col` runs down-right. The half-cube prototype used 0.4 × 0.2 half extents; everything scales by 1.25.
- **Size: rows × columns, any rectangle.** The campaign city is **192 × 112**: a long coast like Metro Cebu's, and deep
  enough inland that the city is not all a short walk from the port (`CampaignCity` map size; *2026-11-26*, deepened
  from 192 × 64). Test maps default to 64 × 64.
- **Grid diagonals are screen-straight.** One step along both axes at once draws as a horizontal or vertical line on
  screen. Diagonal avenues (§4.3) use this.

## 3. Data

| Piece | Kind | Holds |
|---|---|---|
| **City map** | plain data, saved as `CityMapData` (SO) for test maps | size; per cell: ground (land, sea, park, river), road (none, street, avenue), zone; list of placed buildings (building, footprint origin, W × L, storeys) |
| ~~**`ZoneData`**~~ | — | *Superseded 2026-11-26:* zones are district neighbourhoods, a profile on `DistrictData` (§4.5). Landmark lists move to districts when landmarks are built |
| **`BuildingData`** | SO, Database tab | id from the asset GUID; face atlas material and roof / left wall / right wall cells; allowed W × L footprints; storey range and storey height |
| **Generator settings** | serialized fields on the generator | size, coast depth range, avenue count range, run-length range, block size limits, zone list ordered centre-outward |

Every number the generator uses is a serialized, tooltipped field. None are constants.

## 4. Generation, in order

Deterministic from the seed through its own `System.Random`, so generating the city never perturbs battle RNG
(`BattleRandom`) or the encounter pool's draws.

### 4.1 Coast
Exactly one map edge is sea; the other three are land. On a rectangular map it is a **long side**, and of those the one
**facing the camera** (the bottom of the screen: the last column on a map with more rows), so the city strings along a
shore the player looks across (both rules tunable on the randomizer, *2026-11-26*). **The coastline waves** *(2026-11-26)*: its depth is two
sine waves with seeded counts and phases, broad bays (1.5–3.5 along the coast) plus a shorter ripple riding on them,
spanning a depth range of 4–20 cells so bays and headlands read at city scale. Small random steps (at most two cells
off the wave) roughen it so it never looks drawn with a ruler. All tunable on the randomizer. Sea cells are ground =
sea; nothing else is placed on them.

### 4.1b Rivers
*Built 2026-11-26.* The mountains are taken to be at the back of the map (the land edge opposite the sea), so rivers run
from there down to the coast: 2–3 rivers, 1–2 cells wide, each starting at its own share of the back edge and
meandering on the way (count, width and meander tunable on the randomizer). They are carved right after the coast and
before any road, so the highway and avenues cross them as **bridges** (a road on a river cell). Streets and lots stop
at the bank. River is its own ground type: it draws as water, takes roads where sea does not, and the painter has a
River brush.

### 4.2 Downtown
A point on the coastline, near its middle with a seeded offset. Zones and avenues are measured from it.

### 4.3 Radial avenues
A seeded number of avenues (default 3–5) start at downtown and head inland to the map edge, spread across the inland
half-plane. Each avenue is **two cells wide** and moves in **eight directions** (both grid axes and the two
diagonals): long straight runs (a tunable length range) joined by 45° bends, so it reads as a winding boulevard.
Sideways moves are short (a separate, smaller length range); long sideways runs read as a staircase, not a road.
Avenues may cross each other; a crossing is an avenue junction.

**The highway** *(built 2026-11-26)*: one straight two-cell avenue runs the whole length of the land, parallel to the
coast, like Cebu's main north–south road. Its position across the land (0 = just inland of the coast, 1 = the far
edge, default the middle) and whether it exists are randomizer settings. Streets sprout off it as off any avenue.

### 4.4 Streets
*Grids since 2026-11-26.* Each neighbourhood type (§4.5) lays its streets out as a **grid** or **organically**:

- **Grid:** regular streets every block in both directions across the area, anchored on its centre (the town fallback's
  on downtown), spacing rolled once per area from the type's block length. A grid line that would run beside an
  existing road is skipped, so it joins the road instead of doubling it. Business districts, the capitol, the market,
  reclamation, HQ and every tier of ordinary housing are grids, so planned town reads as blocks.
- **Organic:** the marching streets below. Only **informal housing** is organic: the tangle of lanes where nothing was
  planned. Marching streets stop at a grid area's edge, so grids stay clean.

**Open patches** *(2026-11-26)*: before streets and lots, the randomizer scatters ragged patches of **park** ground
(fields, plazas, empty land; 18–28 patches of 2.5–7 cells, tunable), kept clear of neighbourhood centres. Nothing is
paved or built there; the highway and avenues run through them. Park draws as a green placeholder until grass art
exists, and the painter has a Park brush.

Organic streets **march**. Each is a walker, one cell wide, that starts beside an avenue (a tunable chance per avenue-side cell)
and steps straight along a grid axis:

- **Blocks.** Every block length (a tunable range) it reaches a corner. There it may sprout a side street on each side,
  and rarely turns. The side-street chance falls from a downtown value to an edge value with distance from downtown,
  so downtown is a dense grid and the edge of town thins to a few long roads.
- **Closing.** Meeting another road ends the walker and closes a block.
- **Dying.** It also stops at the sea, the map edge, beside a parallel road (so roads never merge two cells wide), or
  at random (a small chance per cell), which leaves a dead end. A walker that stops short of one block without meeting
  a road is erased, so there are no stubs.

Blocks are whatever the walkers close off. Buildings only go on lots that touch a road (§4.7), so block edges fill,
deep block interiors stay open yards, and open land past the last street stays empty.

*Superseded 2026-11-24:* recursive cuts of the regions between avenues. They made an even grid with no growth,
dead ends or thinning; marching was chosen instead.

### 4.5 Zones: district neighbourhoods
*Ruled and built 2026-11-26; replaces the distance-band `ZoneData`.* A city's character comes from **neighbourhood
types** (`CityNeighbourhoodData`, `Assets/Data/City Neighbourhoods`), not from bands around downtown. Each district
points at one, and several can share a type. A type's fields, all tunable on the asset:

| Field | Sets |
|---|---|
| Placement | where the area is centred (§4.8) |
| Area radius | how far the area reaches, in cells |
| Storeys | building height range |
| Lot size | lot side range, so building footprint |
| Lot fill | share of lots built; the rest stay yards, plazas and gaps |
| Block cells, street branch chance | street density: short blocks and high branching give a web of small roads |
| Scenery areas | extra copies of the type scattered around town beyond the districts that use it |
| Town fallback | builds all land outside every area (one type; ordinary housing) |

**Housing is everywhere.** Besides the districts, each type the network's districts use scatters its scenery areas,
and the town-fallback type builds whatever land no area covers, so there is housing all over the city, not only
where there is somewhere to visit.

Generation order: coast, downtown, highway and avenues; then every **area centre**, districts first (§4.8), then
scenery; then open patches; then grid streets, organic streets and lots, each built to the type of the nearest area
covering it, else the town fallback.

| Type | Districts | Storeys | Lots | Fill | Streets | Placement | Scenery |
|---|---|---|---|---|---|---|---|
| Business Towers | Business District | 5–10 | 2–3 | 85% | blocks 4–6 | Downtown | — |
| Capitol | Civic Center (the political district) | 3–5 | 3–4 | 50% | blocks 5–8, sparse | Avenue | — |
| Market | Public Market and Plaza | 1–2 | 1–2 | 80% | blocks 3–5 | Avenue | — |
| Reclamation | Reclamation (Port and Malls) | 2–4 | 3–5 | 65% | blocks 5–8, sparse | Coast | — |
| Informal Housing | Riverside Community | 1 | 1 | 85% | blocks 2–3, dense lanes | River | 2 |
| Middle-Class Housing | Residential Barangay | 1–2 | 1–2 | 55% | blocks 3–5, small roads | Anywhere | 3, town fallback |
| Affluent Housing | Affluent Subdivision | 2–3 | 2–3 | 55% | blocks 5–7 | Edge | 1 |
| Exclusive Village | Exclusive Village | 2 | 3–4 | 40% | blocks 6–9, few roads | Edge | 1 |
| HQ Grounds | Campaign Headquarters | 2–3 | 2 | 70% | blocks 4–7 | Avenue | — |

Fill is kept well below full (*retuned 2026-11-26*): the town fallback covers every unclaimed cell, so its fill and
street grain set how packed the whole city feels.

**Roofs** *(built 2026-11-26)*: each type also has a gable chance, a pitch range and a roof colour palette. In the
Philippines gables mark affluent houses, so only **Affluent Housing** (85%) and **Exclusive Village** (90%) roll gables;
every other type is flat. Every type with a palette tints its roofs, one colour per building: concrete and pale greys
for middle-class housing, rust and blue tin for informal housing, glass greys for the towers, terracotta and dark
tile for the affluent gables. The roof always takes the same three random draws, so retuning roofs never moves the
city's lots or heights.

Every building is still the one `CampaignBlock` half-cube, so for now a district reads through height, lot size, gaps,
street grain and roofs. House and tower art arrives with `BuildingData` (§3).

### 4.6 Landmarks
Each zone lists must-have landmarks. The generator places each once, before ordinary buildings, in the largest block
of that zone that fits it, fronting a road. A landmark that fits nowhere is skipped with a `GameLogger` warning.

### 4.7 Buildings
Each block is packed largest footprint first from its zone's weighted list; storeys roll within the building's range.
**Every lot touches a road** (a road cell beside one of its four sides): a building nobody can reach makes no sense, so a
lot that touches none is not built.

**Building gap** *(2026-11-26)*: each built lot keeps a ring of open land around it, so buildings don't stand wall to
wall. It is set per neighbourhood type: 1 cell for most, 2 for the Exclusive Village, 0 for Informal Housing and the
Market, which really are packed. A lot whose fill roll leaves it empty holds no ring.

**Remoteness** *(2026-11-26)*: the city concentrates around downtown, and going up or down the coast gets remote.
Each cell's remoteness is 0 within the **town core** (by default the 30% of the way from downtown to either end of the
coast) and rises smoothly to 1 at the far ends; it is measured along the coast only, so inland never counts as remote.
Toward the ends the **ordinary town** (the fallback and any land outside neighbourhoods) loses lot fill (down to 20%),
side streets (down to 25%), and past a remoteness of 0.55 its grid stops, leaving sparse wandering roads. Open patches
favour the ends, so the countryside gathers the fields. Districts and scenery areas are places and keep their own look
wherever they stand. All of it is tunable in the randomizer's Remoteness section.

### 4.8 Districts
*Built 2026-11-25; centres before buildings since 2026-11-26.* Each neighbourhood type has a **placement** rule. Once
the coast, highway and avenues are down, each district on the travel network, then each scenery area, takes the centre
of its area, an open land cell, by its type's rule (a district with no type: Anywhere):

| Rule | Centres on |
|---|---|
| Downtown | the cell nearest downtown |
| Coast | the cell nearest the sea |
| Avenue | a cell beside an avenue, about halfway out |
| Edge | the cell farthest from downtown |
| River | the cell nearest a riverbank (informal settlements line the rivers) |
| Anywhere | any cell |

- HQ goes first, then road endpoints in road order, then scenery areas; each takes the best cell left.
- **Reach** *(2026-11-26)*: centres stay within 60% of the way from downtown to the farthest land, so districts stay
  reachable and the city concentrates around its centre; Edge means the edge of that reach, and Avenue its halfway point.
- **Spacing is a soft cost** *(2026-11-26)*: a spot inside 24 cells of a centre already placed pays 2 score per cell of
  crowding, so an area shifts a little off its ideal spot rather than jumping across the map, which a hard spacing rule
  used to do. A small seeded jitter picks among the few best spots. All tunable on `CampaignCity`
  (`CityDistrictPlacer`, Area placement box).
- After the buildings are placed, the district's marker goes on the **tallest building within its area**, the nearest on
  a tie, else the nearest building anywhere; no two districts share one.
- Seeded from the run seed on its own stream, so a run always places its districts the same way and nothing is saved.
- A district with no building left gets no marker and a `GameLogger` warning.
- The district's marker stands on its building's roof (§7).

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
  into chunk meshes (16 × 16 cells, tunable on the view), with each cell's atlas cell and rotation carried in the vertex
  data (`TEXCOORD2`, read by the half-cube shader when its w is 2; a mesh without the channel can read w = 1 on D3D), so a 192 × 112 map is 84 meshes rather than 21,504
  objects and the ground batches. Sea and avenue cells are each chunk's other two submeshes. Painting rebuilds only the
  chunks a change touches. *Built 2026-11-26.*
- **Water** *(built 2026-11-26)*. Sea, rivers and the open sea past the coast share one painted look in the half-cube
  shader, in the city's ink-and-wash style rather than realistic water: stepped teal bands by distance from the shore,
  their edges wandering so they read as washes; a dark ink line at the waterline at a fixed pixel width, like the
  buildings' outlines; a thin cream foam band inside it that slowly swells and thins; sparse cream wave strokes
  offshore that drift and fade in and out; and a faint slow mottle for brushwork. It is lit flat like the ground, so the
  clock, building shadows and cloud shade apply, and at night window light falls on it with a faint smear. Distance
  from the waterline comes from a per-cell shore map (steps to the nearest land, bilinear-filtered so the 0.5 contour
  sits on the cell edge), which gives a one-cell river a real middle instead of turning it all to shore; it keeps
  growing past the map edge so the open sea carries on from the coast. Water vertices are marked `atlasData.w = 3`.
  Colours, band distances, ink and foam widths, stroke density, size and weight, and one calm speed for all the motion
  are in the City Map view's Water foldout.
- **Open sea past the coast** *(built 2026-11-26)*. The view finds the coast (the map edge that is mostly sea) and
  draws one flat sea quad past it, 32 cells by default (tunable on the view), so the city looks out over water.
- **Culling by chunk** *(built 2026-11-26)*. Buildings are grouped under the chunk their front cell is in. In Play mode
  each chunk whose bounds (ground plus its buildings' roofs) miss the camera's view and a margin turns its renderers off,
  so the cost follows what is on screen, not the map's size. Renderers, not GameObjects, so buildings keep their meshes.
  Edit mode draws everything. Shadows and window glow read the city's textures, so a culled building still shades and
  lights what is on screen.
- **Buildings are individual half-cube meshes** (`HalfCubeBuilding`: footprint in tiles, wall height, roof). A building's
  base is exactly its footprint's diamonds. Buildings sort by their front cell's grid depth.
- **Roofs** are flat or **gable**: two slopes up to a ridge along the lot's longer side, with the gable-end triangle on
  the visible short wall. Slopes carry their true 3D normals, so the sun lights them apart and they read as pitched.
  Pitch stays under 30°: from the 30° camera a steeper back slope faces away and vanishes. The ridge rise is half
  the short side at the pitch; the shadow test uses each lot's average roof height (eaves plus half the rise). Roof
  faces take the building's roof tint. The gable triangle samples the wall art's top row until facades get trim art.
  - *Known limit:* long footprints beside each other can still sort wrong (the classic iso case). Measure it on real
    generated maps before adding splitting.

### 6.1 Lighting: one 3D sun

*Built 2026-11-23:* `CitySky` sets the globals; the half-cube shader no longer uses URP's
2D lights. On the campaign map the sky follows the run's clock (§9); elsewhere it follows its own minute-of-day
field (scrub it, or set seconds-per-day for a Play-mode loop).

Buildings are lit as the 3D boxes they stand for, not as flat 2D shapes.

- **Face normals are true 3D directions in view space** (camera elevation 30°, plan rotated 45°; 30° is the only
  elevation that draws a square cell twice as wide as tall, and 26.565° is the angle of the diamond's edges on screen):

  | Face | Normal (x, y, z; −z faces the camera) |
  |---|---|
  | Roof | (0, 0.866, −0.5) |
  | Left wall | (−0.707, −0.354, −0.612) |
  | Right wall | (0.707, −0.354, −0.612) |

  Ground and road cells use the roof normal. With these, a sun behind a building lights its roof and leaves both
  front walls in shade. The prototype's roof faces the camera instead, which is why its back-lighting looks wrong.
- **One sun, set as shader globals.** A world-space direction (azimuth and elevation) plus colour, ambient colour and
  window emission, set once per frame by one lighting component and converted to view space for the shader. It
  replaces the Light2D key light, URP's 2D normal-map lighting on city meshes, the shader's second key-light term,
  the per-object `HalfCubeLightLink` and the per-renderer loop in `CityDayNightCycle`.
- **The sun follows the campaign clock.** The day runs 08:00 to 16:00 (`campaign-encounters.md` §6): low morning sun
  from one side at the start, rising, then late-afternoon sun from the other side at the end. Azimuth, elevation and
  colour over the day are tunable curves and gradients. Night lighting only applies if a day ever runs past dusk.
- **Moonlight** *(built for testing, 2026-11-24)*. While the sun is down, `CityMoon` on `CitySky` (Moon tab) takes
  its place as the one light: its own rise and set minutes (wrapping past midnight), peak elevation, sweep between two
  azimuths, a dim blue-white colour and a phase (0 = new moon, 1 = full). Shadows and cloud shade then work by moonlight
  with no shader change. One light at a time: if the moon is up before sunset, only the sun shows.

### 6.2 Shadows: height-aware, from a height texture

*Built 2026-11-23:* `CityMapView` publishes the height texture; `CitySky` holds the strength, step, reach and bias.
Scenes without a city map (the previews) get sunlight but no shadows.

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

*Overcast light, wet surfaces and puddles built for testing, 2026-11-24:* `CityRain` on `CitySky` (Rain tab) holds an
intensity the campaign will set from the day's rain roll; scrub it to preview. Overcast light blends the sun's clouds
toward an overcast `CityClouds` and dims the sun, tints ambient grey, fades shadows and boosts window emission.
Puddles use grid noise rather than a puddle texture, shifted by a puddle seed the campaign sets from the run seed, and wetness only fades with the intensity (no slower drying,
since rain lasts whole days). Wet flat surfaces also smear window glow down the screen. Not built: falling streaks,
ripples, splashes, and wiring the intensity and puddle seed to the campaign.

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

### 6.4 Clouds: shadows only

*Built for testing, 2026-11-24.* No cloud is drawn. Two layers of drifting Perlin-style noise over the grid dim
**direct sunlight only** (diffuse, specular and edge light; ambient and window glow are untouched), so a cloud reads as
a soft passing shade over roofs and roads:

- **Coverage** (how much sky is cloud), **softness** (edge width), **density** (share of sun a thick cloud blocks;
  kept below the building shadow strength so buildings still read through it), **size** in cells, **wind** direction
  and speed. The second layer is smaller and slower, so shapes change as they drift.
- Each point reads the clouds along its ray to the sun, so a roof and the ground it shades agree and the pattern leans
  as the sun moves. Sunlight is building shadow x cloud shade.
- The values live in a `CityClouds` object on `CitySky` (Clouds tab). **The forecast hook:** a future weather
  system authors one `CityClouds` per forecast (clear, partly cloudy, overcast, rain) and assigns `CitySky.Clouds`.
  Not built yet: the forecast itself, an overcast tint on sun and ambient colour, and a fade between forecasts.
- **City bounce** *(2026-11-24)*. Clouds also give a little light back: city light off their undersides, a warm
  sodium colour (per `CityClouds`, so overcast rain nights glow more) read straight overhead and scaled by the night
  emission, so it vanishes by day.
- Cost: four noise evaluations per pixel (two for shade, two for bounce), small next to the shadow march.

### 6.5 Window glow: baked light pools

*Built for testing, 2026-11-24.* Window light spilling onto streets and nearby walls at night, without real lights:

- **Bake.** `CityGlow` (tuned on the city view) paints a soft RGB texture over the grid, 4 texels per cell by default,
  rebuilt with the buildings and never per frame. Each lit building pours its window colour out from all four walls
  (back walls too, as with shadows). Light walks only through open ground, so it never passes through a building to
  the street behind; it fades with distance over a reach in cells. More storeys give brighter light.
- **Shader.** One filtered read at the pixel's grid position, faded out with height so it lights streets and low
  walls rather than roofs, added as albedo x glow x the sun's night emission scale, so it vanishes by day for free. The emission reaches its day value at a
  tunable daylight (**Lights Off By** on the sky, default 0.2, about 45 minutes after sunrise), so windows are dark
  by the 08:00 start of a campaign day.
- **Test variety.** Each building rolls lit or dark and warm (tungsten) or cool (fluorescent), seeded by its lot, so
  it keeps its lights when others change. Building types (`BuildingData`) take over colour and window density later.
- **Lamp posts (later).** A lamp is the same spread from a one-texel source with its own colour and reach, plus a
  small post mesh; the bake, shader read and no-bleed rule are shared. Not built: no lamps on the map yet.
- **Limits.** Static light only: moving or individually flickering lights need a per-frame overlay. Lit surfaces cast
  no shadows of their own. URP Bloom can add the halo on the windows themselves; that is a post-processing setting.

## 7. Painter and test maps

A Scene-view tool on the city view, for testing art, tiles and packing:

*Built (initial, 2026-11-23):* **Crookedile → Art → Open City Map Editor** opens a scene with a `CityMapView` on a 64 × 64
`CityMapData` test map (`Assets/Art/AI/Iso/CityMap`). Select the view and use the **City Map Painter** tool: brushes for
street, avenue, clear road, land, sea, building lot (drag a rectangle, with a wall height) and remove building; Undo works.
Streets pick their art from four neighbours; sea and avenues are placeholder colours. Duplicating the asset saves a test
map and assigning another asset loads one. The toolbar's **Randomize** (new seed) and **Regenerate** (the seed in
the field) replace the map with a random test city from `CityMapRandomizer`, tuned on the view: coast, axis-only two-cell
avenues from downtown with short jogs, §4.4's marching streets, and lots that each touch a road, taller toward downtown. It stands in for §4
(no zones, landmarks, diagonals or `BuildingData`). Not built yet: the §4 generator, zone painting, the console command,
diagonal avenues and eight-neighbour art. *Built 2026-11-25:* `campaign.unity` draws the run's city. `CampaignCity` fills a fresh `CityMapData` from
`RunState.Seed` with the randomizer (tuned on the scene's City Map view), so the same run always draws the same city
and nothing extra is saved; it sets the puddle seed from the run seed and places the districts (§4.8). It reads the run and never changes it.
`CampaignMapView` (on the `CampaignFlow` object) opens on the current district, pans by dragging (left mouse by
default) and zooms toward the cursor with the scroll wheel, with the whole view kept on the city: every corner on a map cell or on the open sea past the coast, so the three land
edges are hard limits and the coast side opens onto the water; zoom-out is capped by the city's narrow side, sea
included. It is ignored over `CampaignFlow`'s
panels. Each district shows a name marker on its building, and the current district's is highlighted. Districts with a
location still on today's list are outlined: the half-cube shader's **selection outline**, a pulsing line of fixed
screen width around the building's silhouette except its two ground edges (on a flat roof its two back edges and the
walls' two outer verticals; on a gable the back eave, both roof ends and the walls' outer verticals). Each face marks
its own outline edges in `TEXCOORD3`, so any roof shape outlines correctly. Set per building (`CityMapView.SetBuildingOutline`) and tuned on the material (width in
pixels, pulse speed) and on `CampaignMapView` (colour and strength; *2026-11-26*, replacing a glow on every face). Markers are not clickable yet: visits still
start from the panel.

- **Generate** from any seed in the editor.
- **Paint** ground (land, sea, park, river), streets, avenues and zones; **place and remove** buildings; Undo works.
- **Save** the current map as a `CityMapData` test map, and **load** one in place of the generated city: an editor
  field on the city view, plus a dev-console command. Loading a test map only changes scenery, so the command is
  `Safe = true`.
- Runs always generate; a test map is never used by a normal run.

## 8. Removed

*Done 2026-11-25.* The sprite system is deleted, not migrated: `CityGenerator.cs`, `CityZoneData.cs` and its one asset `Factory.asset`,
`IsoFootprintGizmo.cs`, the sprite building prefabs in `Assets/Art/AI/Iso/Buildings`, the `CityZoneData` entries in
`ContentAssetNaming.cs`, and the campaign scene's leftover `Grid` of sprite prefabs. The readme's iso-grid guide row
points at `IsoGrid.cs` instead of the gizmo.

## 9. Build order

1. **Foundations:** the 3D sun and corrected face normals (§6.1), height-aware shadows (§6.2), the 1 × 0.5 grid, chunked ground meshes, building sorting by grid depth.
2. **City map and renderer** with placeholder tiles and neighbour-picked road art.
3. **Generator:** coast, downtown, avenues, streets, zones, landmarks, buildings.
4. **Painter** and saved test maps.
5. **Remove** the old system (§8).
6. **Lighting from the campaign clock** *(built 2026-11-25: `CampaignCity` sets `CitySky.MinuteOfDay` from
   `RunState.ClockMinute` every frame)*, then **rain** (§6.3; the travel rule in `campaign-encounters.md` §6.2).

## 10. Choices made without a separate decision

These follow from the answers above; change them here if they are wrong.

- Streets are axis-aligned only; only avenues run diagonally.
- The sea is one edge: a long side on a rectangular map, and one facing the camera (row 0 or the last column), both
  tunable on the randomizer; the seed picks only when the rules leave a choice.
- Avenue count defaults to 3–5.
- A landmark that does not fit is skipped with a warning, not forced.
- Chunk size 16 × 16.
- Elevation is out of scope for now; the cell data leaves room for a height level later.
