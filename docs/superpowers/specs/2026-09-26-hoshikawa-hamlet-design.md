# Hoshikawa organic hamlet — design

Approved in chat 2026-09-26. Scope: Hoshikawa (z ≥ 0) layout, terrain, buildings and decoration, plus shared tree models. Out of scope: human models, Komorebi layout (its `House()` calls keep today's look; its orchard keeps the legacy persimmon tree).

## Constraints carried from the existing game

- `World.Height(x,z)` stays the single source of truth; every new landform (swells, terrace, pads, brook, shrine mound) is a term in it.
- `RoadX()` and `terrain.gdshader` are unchanged, so the painted road and square (plaza at (0,21)) stay in sync.
- `Buildings` keeps exactly six Hoshikawa entries; the shrine is not a building.
- The global `rng` (seed 2189) stream is not disturbed: `Village()` uses its own generator and `Tree()` still consumes exactly three values from whichever generator it is given.
- Self-test routes stay clear: spawn (0,31) → (0,15) → (23,15) → (34,15) → (34,20); each resident + (0,0,2) is open ground.

## Layout (world coordinates, yaw in degrees, door faces local +Z)

| Building | Archetype | Centre | Size w×h×d | Yaw | Resident |
|---|---|---|---|---|---|
| Kisaragi Workshop | gable + open lean-to shed, stovepipe, solar collector | (-11, 9) | 8×4×7 | 75 | Ren (-5.2, 11) |
| Seed & Grain | kura storehouse (plaster over namako base, grey tiles) | (12.5, 6) | 6×4.6×5.5 | -95 | KOMA-7 (8.3, 11.5) |
| Village Assembly | large irimoya hall with gable porch | (-12.5, 21) | 9×5×7 | 82 | Chiyo (-6, 20) |
| The Copper Kettle | irimoya tea house, noren, red bench + umbrella | (12, 24) | 8×4.5×7 | -80 | Sora (6.8, 22) |
| Hoshikawa Clinic | gable with engawa veranda, herb racks, medic banner | (-11.5, 37) | 7×3.5×6 | 98 | Aki (-5.5, 37.5) |
| Mori Farmhouse | thatched irimoya minka, firewood, straw | (11, 41.5) | 8.5×3.4×6.5 | -70 | Hana (6, 37.5) |

- **Square**: widened road around the existing well (-3.2, 24) gets a small tiled well roof, notice board, two stone lanterns, benches and a sakura at (-7, 27.5). Corridor x∈[-1.5, 4.5] stays clear.
- **Side lanes / stepping stones** from the road to each door.
- **Shrine** on a mound centred (-1, 63): torii across the road at z=52, stone steps z 53–58.5 (visual, non-solid, over a walkable ≤25° terrain ramp), small gabled shrine with shimenawa, two stone lanterns, sakura, sugi grove behind.
- **Brook** `BrookX(z) = 19.3 + 0.6·sin(0.16z)` from z≈58 to the river; flat water strip at y=-0.40; arched plank footbridge on the z=15 rice path (planks flush with terrain at both ends, solid like the main bridge).
- **West terrace**: +1.3 m plateau west of x≈-20 for z≈4–48, faced by a solid ishigaki stone wall, ramping to ground at both ends; holds keyaki, bamboo and shrubs.
- Dressing: kitchen gardens (tomato stakes near KOMA), bamboo fences, laundry pole, firewood stack, handcart by the kura, straw rolls, potted plants, village grass tufts. Paper lamps repositioned along the curving road.

## Terrain additions to `Height()` (applied only for z > -2, in order)

base → gentle swells (±0.35 m, faded to 0 within 3.5 m of `RoadX`) → west terrace → square flatten → house pads (level at pad centre, 2.2 m blend, rotated rectangles from the shared layout table) → brook channel (0.95 m deep, flat bed ±0.6 m, banks to ±1.8 m) → shrine mound (2.4 m plateau r 4.5, slope to r 10). The east side of the brook and the rice terraces keep today's terrain.

## Buildings (`scripts/Architecture.cs`, partial `World`)

A `Minka(...)` kit separate from the legacy `House()`:
- Roof mesh built as a height-field per style — gable (kirizuma), hip-and-gable (irimoya, with vertical tsuma triangles) and thatch — with a concave *sori* profile, lifted eave corners, underside and fascia.
- Timber-frame walls: corner/interval posts, nuki beams, plaster panels, lower wainscot boards; lattice windows, shoji sliding doors, noren, engawa, rafter tails, stone base, entry step, hanging name sign.
- New 2×2 village material atlas (thatch, dark cedar boards, earthen plaster, grey kawara) generated with Codex gpt-image, recorded in `ASSETS.md`.

## Trees (`tools/build_props.py`, background Blender → `assets/models/props/*.glb`)

- Species / variants: keyaki ×3, sugi ×3, sakura ×2, bamboo clump ×1, shrub ×2.
- Deterministic branching trunks (bmesh tapered tubes), foliage clumps of alpha-cut leaf cards with outward (spherised) normals, vertex colour R = wind sway weight, G = ambient tint.
- GLBs carry geometry and material *names* only (`bark_<species>`, `foliage`); C# assigns shared materials: per-species bark textures and a `foliage.gdshader` (alpha scissor, double-sided, backlight, wind sway, per-instance tint).
- Textures from Codex: 2×2 foliage atlas on a blue chroma key (keyed to alpha) and a 2×2 bark atlas split into four files; recorded in `ASSETS.md`.
- `Tree(p, scale, species, rng)` keeps its three rng draws (variant, tint, yaw) and its C# trunk collider sized per species. Countryside picks keyaki/sugi mixes, the northern valley mostly sugi, Komorebi orchard keeps `OrchardTree` (the legacy model).

## Verification

- `IntegrationCheck` additions: every Hoshikawa foundation corner within 5 cm of `Height()`; walking across the brook footbridge (existing rice route) stays on floor; walking up to the shrine plateau; tree GLBs load with their expected meshes; village trees and props keep the spawn/rice/resident corridors clear (existing route + dialogue checks).
- Headless and graphical `--self-test`; `tools/review_hamlet.gd` before/after captures in `artifacts/hamlet/`.
- Update `README.md`, `PLAN.md`, `VERIFICATION.md`, `ASSETS.md`, `CLAUDE.md` (brook invariant, new files).
