# HPA-1252 — Hoshikawa Station: Outside map

**Status:** Proposed design and implementation plan only. Implement and verify in this same draft PR, not in another PR.
**Linear:** [HPA-1252](https://linear.app/cwchanap/issue/HPA-1252/hoshikawa-station-outside-map-from-tunnel-mountain-to-the-parcels-door)
**Baseline:** main at `4e7c96ffa9e1dede11763c3fd0704f118d7c128d` (station 2D and audio asset deliveries already merged).
**Design authority:** approved station map v9 (2026-11-24), Linear attachments 1 (Outside aerial), 2 (world placement) and 7 (`station-maps-v9-source.zip`). Both mockups were inspected during planning; retrieve and unpack the source archive at implementation time, inspect its README and run `python3 -I walk-model/report9.py` from the extracted root with numpy. The walk graph, not the drawing polylines, is the authority for route distances. Do not invent missing post locations or replace the approved layout with a new one.

## 1. Goal and scope

Extend the existing playable valley eastward from the Signal yard in Tunnel Mountain to the station's parcels door. Deliver the entire outside graybox in one PR: its closed exploration loop, walkable terrain and collision, two-way tunnel transition, Forecourt Starwell, one earned gate shortcut, locked future entrances, key pickup, 17 enemies, two traps, three supply pickups, regional HUD and localized objective.

This is a gameplay/geometry pass. Reuse primitives and existing models, UI, enemies, sounds and terrain helpers. HPA-1256 and HPA-1258 delivered assets, but **HPA-1257** owns the station's Tripo models and art/audio integration. HPA-1253 through HPA-1255 own the inside floors; HPA-1259 owns the boss. Do not generate or wire new art/SFX here, implement the inside, B2, companion AI, new voiced scenes, or build a general scene-streaming/quest/save framework. HPA-1303 (cross-session saves) is a separate unresolved product question; checkpoint persistence below means within the current run. The old buried path below the Shelf remains buried.

## 2. Layout and progression contract

The station's coordinate frame is x east, z north, metres. Convert once at the layout boundary: world x = 430 + local x, world z = -120 - local z; height is the runtime terrain height unless an authored deck/stair overrides it. Use the graph and maps for the exact footprint, routes and posts; coordinates below are anchors, not substitutes for the archive.

| Anchor | Station frame (x,z) | World (x,z), approximate |
| --- | --- | --- |
| West tunnel mouth (existing Signal yard) | outside frame | (134, -176) |
| East portal landing | (-53, 29.5) | (377, -149.5) |
| Forecourt Starwell | (78, -50) | (508, -70) |
| S2 main doors | x 58–74, z -36 | x 488–504, z -84 |
| Arcade pouch | (98, -88.6) | (528, -31.4) |
| Garage cell | (135, -90) | (565, -30) |
| S1 gate | x 132, z -28..-12 | x 562, z -108..-92 |
| Southeast fence gap | x 191–196, z -12 | x 621–626, z -108 |
| Crane cell | (163.5, 18.75) | (593.5, -138.75) |
| Old signal room | x 182–194, z 31–39 | x 612–624, z -159..-151 |
| Parcels door | (128, 10) | (558, -130) |
| S5 old line gate | (-9, 44.5) | (421, -164.5) |

The first route (about 555 m):

1. Reach the existing Signal yard after the day-three rockfall; interact at the cleared west tunnel mouth. Fade across the unwalked 250 m tunnel to the east portal.
2. Descend the zigzag lineman's path past the oil-drum trap. The old line is visibly intact to S5, but cannot be used to enter platform 1. Reach the Forecourt Starwell about 217 m from the portal, around 20 m from the barred main doors.
3. Service road runs to locked S1; the fence separates this road from the outer route. Dead buses block the depot's direct route. Walk through the garage, then around the outer fence to the far southeast gap. Optional forecourt–arcade–lane–depot loop contains a remedy pouch and two sleeping scouts.
4. Enter freight yard through the gap; include four sidings, warehouses, hump and wagon trap, gantry with a climbable cell platform and one-way drop. Reach the old signal room and collect the parcels-door key. The duty roster is represented by a placeholder prop only; its voiced/readable story scene is deferred.
5. Return via the north siding to the loading dock, unbar S1 from the yard side, then use the key at the parcels door. Show TO BE CONTINUED there, never at the tunnel. The door remains a collidable terminal boundary until HPA-1253 adds the interior.

| Door | Initial state / behavior |
| --- | --- |
| Station tunnel | Two-way, available in Quest.Beyond; no trigger before the rockfall/progression or from outside the reachable mouth. Use deliberate E interaction, grounded destinations facing away from portals, no physical 250 m tunnel. |
| S1 yard gate | Locked from service road; open only from freight-yard side using existing gate semantics. Remains open through rest, defeat and return travel; resets on Restart. |
| Parcels door | No key: localized locked feedback. Key held: interaction unlocks once, emits terminal toast, no interior load. Keep key as a non-consumable key item; unlocked state survives rest/defeat; Restart clears both. |
| S2 main doors | Barred from inside; no entry/open action in this PR. |
| S5 old line gate | Shut; later signal cabin lever controls it. |
| Mall stair | Shutter jammed from both sides; never opens. |
| B2 yard ramp | Sealed, city-power appearance via existing material/placeholder only; no city key or B2 gameplay. |

Use the stable name Hoshikawa Station (English), 星川駅 (Japanese), 星川站 (Traditional Chinese; verify glossary) for the region, distinct from the village and from Zero's station in Nagoya. Continue using the current HUD and E interaction affordance, not new screens.

**Objectives (proposed detail):** keep Quest.Beyond and existing story gates. Before entering, marker points to the Signal yard mouth, then the old signal room when on the Outside and key not held, then the parcels door. If returning to Tunnel Mountain before completion, marker points back to its mouth. After terminal interaction, keep the marker at the parcels door while on the Outside. Use existing `Objective`/`ObjectivePosition` and localized text; no separate quest manager. The player can use the Starwell, S1, optional loop and pickups independently of the marker. S1 is not a second key gate; prove its one-way rule, but do not require opening it merely to use the parcels key.

### Distances and topology

Measure actual walks, with physical obstacles, against the v9 graph (use the existing MazeTest practice of roughly ±10% for authored walking distances, document any departure; do not silently relax the graph or move a gate to make tests pass).

- Portal → Forecourt: 217 m. First intended route to finish ≈555 m.
- Forecourt → S1 yard side with gate shut: ≈317 m. Forecourt → parcels door before S1: ≈339 m; after opening S1: ≈112 m. The gate saves ~227 m, no adjacent redundant shortcuts.
- Worst return walk before Forecourt rest ≈277 m from Court via Great stair/tunnel transition; after Forecourt, ≈311 m before S1, ≈116 m after. Treat the tunnel as a transition edge (not 250 m of walking); distinguish intended itinerary, shortest route and checkpoint return. Compare each number using the graph's start/end/door state.
- Flood both directions with intended gate states, one-way crane drop and real obstacle footprints. There is no alternate entry to the yard while S1 is closed; every reachable point has a valid return to a Starwell, including the portal back to the earlier map. Do not use bidirectional test links for a one-way drop.

## 3. Implementation design

### A. Terrain, layout and world construction

Add `scripts/StationOutside.cs` as a partial World, parallel to Maze.cs, containing station-local layout tables, coordinate helper, `StationTrails`/floor pads, obstacle footprints, door/landmark anchors, station enemy/pickup/trap rows and a single build method. Compose it from the existing World constructor before `Merge()`. Use the same data for collision, visuals and tests; no new editor scene or runtime Python dependency. Keep the layout as C# literals derived from the v9 source; archive references and graph results in documentation.

The approved playable footprint is approximately 200 × 140 m, with full map extents x 368–634, z -200..-20 and building x 430–558, z -156..-84. Extend Far east from x=440 to about 640 (provide enough margin for the closed boundary), preserve the existing valley bounds. MazeFine ends at x=372; station extents overlap it by 4 m. Build a non-overlapping union of fine cells/chunks (split the new rect at the old fine boundary or equivalent), not duplicate meshes/colliders. Audit every Fine/MazeFine reference including `IsFine`, `Terrain()`, `Ground()` rim normals, far-cell seam fans, water grid selection, tests and render captures. Keep 1 m near and 4 m far resolution, aligned seam cells, single ground/collision source `Height()`; no overlapping fine/far triangles or collider cracks.

Use existing `Carve()` and its per-16 m reach cache where appropriate for the hillside and road; author flat pads for forecourt, garage, yard, station shell and tracks. Avoid blending station floors into distant Tunnel Mountain floors or changing old trail heights. Existing `AllTrails` is used by the minimap, while `Cut` is a separate table and `Squares` currently ends at x=448; explicitly wire station paint, floor calculations, reach filtering/cache extent and `RoadGap()`, so the minimap and terrain agree. Keep old `MazeEdge`/`InMaze` semantics (including old test assumptions); station region must take precedence over the broad current maze predicate without reclassifying old paths. Profile the new east extent; no full-table scan for every sample. Keep seeded streams pinned and use a separate deterministic station scatter stream.

Lay the shell, platforms and track bed, forecourt/clock tower, arcade, depot/garage/dead buses, perimeter fences and gap, lineman's hut, freight yard/warehouse/sidings/crane/hump, signal room and loading dock using existing builders and placeholder geometry. Use collidable walls/obstacles and natural mountain boundaries; no walk through the station shell or around the yard fence. Reuse collision layers and camera-only eaves policy where applicable; static primitives are baked by Merge, moving parts/interactive pickups stay out of it. Explicitly verify jump/dodge/climb cannot bypass locked gates; crane climb and drop are physically traversable. The city freight line is scenery, separate from the valley's intact old line; do not connect the two or add trains.

Move the machine-city silhouette from (x392–416,z-120) to the north-west ridge; pick the exact coordinates by automated captures, not by speculative story edits. Update `Cutscene.Shots["city"]` and check all city/overlook views (including camera far plane/fog: current gameplay far=260 m, scene far=320 m). Keep the existing opening's shot timing, make the city visible in the expected direction from the valley, Cedar Pass and east portal, and the station visible from the Station overlook. Preserve the story's right/left composition at the east portal. List any direction-sensitive voiced lines needing revision instead of changing them.

### B. Game interaction and recovery

In `Game.cs` and, if useful, a small partial Game alongside the new map, add station-specific interaction lookup shared by `Interaction()` and `Interact()`. Keep current proximity and mode checks; consume input only at a valid nearby entrance and stage. Use `Cutscene.Place()` and `Blackout`/HUD fade pattern from room exits (same World instance; don't call EnterRoom/LeaveRoom or hide/free the valley). Set landing/heading/camera rig immediately during blackout, clear velocity/lock-on/stale danger, prevent immediate bounce or reentrant interactions, resume controls when safe. Return to the west landing without resetting inventory, enemies, shortcuts, or the last checkpoint. If the quest is not Beyond the existing rockfall access restriction remains.

Append the Forecourt row to `World.SavePoints` without reordering the current four rows. Use existing glow/rest UI and Game.RestedAt. `Player.Respawn()` currently applies an offset and `Game.Retry()` uses InMaze for the toast; ensure the Forecourt landing is free, correctly grounded and returns the Starwell toast (not village) via explicit place/region logic. Court remains the last rest until the player rests at Forecourt; no auto-rest on portal crossing. Rest/Retry rearm traps, restore scouts/drones, preserve defeated patrols and earned S1/key/unlock/pickups. Restart resets all station runtime progress and returns to the existing opening. Do not write a disk save.

S1 uses a `World.Part` of kind gate with From on the yard side and To on the service-road side; interaction distances should make the side unambiguous and prevent through-fence operation. Existing `Game.Opened` is the source of truth for earned gates; reuse it for the parcels terminal unlock only if its stable id is distinct and never appears as a swinging gate. `arrived` currently fires at MazeEnd; remove/move that trigger so completion requires the key and interaction. Reset terminal state on Restart; do not clear on Retry or repeated portal use.

Add `parcels_key` (Key, Max=1) to Items, use an existing icon, and use the World pickup pipeline (visible until successfully collected) and the current inventory to hold it. Increase pouch max from 3 to 7 as the ticket's default, allowing later inside pouches; the existing base remedy count is 3, so the fourth pouch raises capacity to 7 and all seven allow 10, not 7. Rewrite the pouch text in the three languages, preserve fill semantics, and check all HUD icon/slot layout at max capacity. Two cells use existing capacity (leave pickup available if full). Do not add new art.

### C. Encounters, HUD and region

Place exactly 3 patrols, 8 scouts (4 sleeping), 6 drones at the v9 posts. Reuse Enemy setup, reach constraints, culling and rewards; use station post data, not random spawns. Existing Enemy.Maze flag both excludes later patrols from the day-one mission and is used in tests that expect exactly 81 mountain enemies. Proposed minimal change: preserve its existing non-relay semantics (or rename if needed), set for station enemies, and partition by home region in old mountain-specific assertions; do not accidentally count station patrols toward Quest.Road or apply mountain lock-yard lookup to station enemies. No new AI. Ensure patrol routes avoid walls/decks/fences, and sleeping scouts only wake under current rules.

Add oil drums and wagon to the existing World.Parts rolling-load pipeline with map-specific stable IDs and runs. Reuse Traps.cs for physics/damage/rearm; make sure positions follow height where the authored route is on ground, and stationary parts remain collidable before trigger. Test no cull/return side effects. Station's default background is relay until HPA-1257; map `Music.Track(StationName, danger)` accordingly; battle music priority unchanged.

`World.RegionAt`, `Game.Region` toast, `Hud.Minimap`, objective marker and `Music.Track` agree on region. Use existing `AllTrails` painted path entries and north-up projection; no minimap redesign or indoor minimap. Localize region/toast/objectives, portal, door and key prompts, item text and sign labels in `assets/dialogue/ui.ja.json` and `ui.zh_TW.json`, use existing font pipeline for new glyphs. Verify naming against glossary and Localization.Missing; no voiced dialogue edits.

## 4. Ordered implementation checklist (one PR)

- [ ] Retrieve v9 source; run graph report and record reference distances, intended route, collision boundaries, coordinates and spawn counts. Run baseline `just unit`, `just self-test`, `just e2e`, and `tools/render_cost.gd` on pinned main; retain evidence, not generated files in git.
- [ ] Create station layout/height/paint data, fine/far union and cache extension; update old terrain helpers/tests and demonstrate unchanged old walkability. Build placeholder terrain/shell/fences/decks/rail geometry, verify closed flood and capture overhead.
- [ ] Move city and adjust opening shot; add a reusable station-specific aerial and ground-view capture to review_map.gd (or small review_station.gd); add Outside views to render_cost.gd.
- [ ] Wire two-way tunnel, checkpoints, S1, key/terminal door, objectives, restart and HUD; build physical blockers and ensure old stage gating.
- [ ] Add 17 robots, two traps, three supply pickups and key; implement region/music fallback and translations; preserve day-one mission counts and old enemy behavior.
- [ ] Add tests and update sequential IntegrationCheck assertions where scope legitimately changed (4 → 5 Starwells, old MazeEnd terminal, enemy counts, pouch cap); never merely remove coverage. Run all tests and MCP validation, fix issues in this PR.
- [ ] Update CLAUDE.md, README.md, PLAN.md and VERIFICATION.md (actual evidence/limits). Link actual captures/logs in the draft PR before marking ready; no generated game artifacts committed unless small intentional documentation evidence.

## 5. Acceptance criteria — automated, end to end

All criteria apply to the implementation commit(s), not this documentation-only draft.

### Pure .NET / headless engine

- [ ] `just unit` passes, including new `tests/unit/StationOutsideTest.cs` and existing Wilds/Maze/localization tests. Assert coordinate transform, region precedence, exactly one fine-cell owner at overlaps and seam continuity, deterministic heights/paint, old trail/pad heights and region classification unchanged except intended station/city placement; reachability and shortest walks for closed/open S1, gap as only entrance, no sealed entrance bypass, directed crane drop, returnability and no disconnected floor. Extend WildsTest reach bounds and obstacle/one-way traversal model rather than using an inaccurate open rectangle. Verify spawn/trap/pickup counts, stable names, inventory cap and music selection.
- [ ] `just self-test` reports SELF_TEST_PASS in English, Japanese and Traditional Chinese. Existing progression from the title/prologue remains intact; extend the end of IntegrationCheck to drive from Signal yard via E to east portal, actually walk the approved waypoints using existing input-driven Walk/WalkTo, trigger/rest at Forecourt, attempt S1 from wrong side, traverse garage/gap/yard, interact with key and S1 and unlock parcels door. For route coverage do not replace walking with position teleports; isolated setup for defeat/collision probes is fine. Verify no earlier terminal toast, correct prompt/marker before/after key and S1, old line/S2/mall/B2 stay sealed, traps trigger/rearm, pickups nonduplicating and robots/rewards work.
- [ ] Add a focused `tests/e2e/` godot-e2e scenario using the existing Hoshikawa helper against the real launched Godot process: advance/arrange a legitimate Beyond session (existing test setup may position Mio before the station, not fake completion), E transit both ways, test keyless rejection, wrong-side S1 rejection and successful far-side open, key collection and terminal toast, then actual defeat/Rise again before and after resting at Forecourt. Assert Court/Forecourt respawn, retained key/S1/unlock/pickup state, trap reset and patrol/scout/drone semantics, then Restart resets station state. Include a stage-before-Beyond rejection and repeat transit to catch camera/velocity problems. Input-driven interactions via godot-e2e; read actual properties/position, don't set the resulting state. `just e2e` and CI unit/e2e workflows pass.

### MCP gameplay and screenshot visual parity (single platform)

- [ ] Run the above end-to-end checks and a rendered playthrough via Godot MCP (or the project's godot-e2e MCP using its supported game input/inspection/capture operations; use existing repo commands through the connected agent environment where appropriate). Exercise tunnel traversal, both checkpoint recoveries, the complete route, key gating and S1 shortcut in a running game. Capture screenshots and machine-readable logs; no manual/human sign-off as a test.
- [ ] Compare a north-up overhead screenshot against Linear attachment 1: purple route's shape/order, the side loop, the fence gap, S1/S2/S5, station footprint, freight yard sidings/crane/hump, Forecourt Starwell, parcels door, enemy/pickup placements and the topology are visually aligned. Compare wider overhead/world view against attachment 2: portal, station east of Tunnel Mountain, visible overlook and relocated city. Check map orientation and labeled feature coordinates; allow a few metres of ground-fit adjustment only if the v9 topology and measured walks still hold. Placeholder aesthetics are expected; do not compare final texture/model polish.
- [ ] MCP screenshot checks at the east portal, lineman path, forecourt/HUD, garage, fence gap, crane/drop, signal room, locked/open S1, locked/unlocked parcels door, plus the old valley and opening city shot. Inspect readable three-language region/prompt/objective, minimap route, inventory key/pouch and HUD at up to ten remedy capacity (may seed a test inventory for that visual check); no clipping, overlap, broken camera, terrain gaps or unreachable pickup. Save evidence paths and actual findings in VERIFICATION.md.
- [ ] Use the project's Mac/Metal reference for the graphical comparison and performance capture, as required by the ticket and repo guidelines; one platform is sufficient. Run `$GODOT_MONO --path . --disable-vsync --fixed-fps 60 --script res://tools/render_cost.gd` at identical pre/post old viewpoints and add at least east portal/forecourt/yard on the new map. Report draw calls, primitives, CPU/render time and frame rate, same device/settings; compare deterministic counts, record expected additional terrain versus unintended old-view regressions. Proposed guardrail: investigate and fix material regression in existing viewpoints (e.g. >10% deterministic draw/primitive increase not explained by visible new ground) or visibly poor playability before ready; do not claim GPU performance from headless/Mesa. If Mac/MCP renderer isn't available, report that missing acceptance explicitly and keep it unchecked rather than treating an unverified capture as a pass.

## 6. Decisions / risks

- Follow approved v9 and the ticket defaults: relay music, existing reward costs, 7-pouch max, north-west city location chosen by framing. Proposed local code names, objective granularity, retaining the key, terminal interaction and 10% perf investigation threshold are implementation details, not additional story scope.
- Fine mesh overlap, reach cache ending at x448, camera clipping and the broad InMaze predicate are concrete current-main hazards. Address before art integration. Current checkpoint and patrol behavior are reused rather than rewritten.
- Source archive was retrieved as binary in planning but not unpacked/run; use the attachment as the implementation preflight. Mockups and detailed Linear notes were reviewed. No gameplay, build, screenshot or performance run has been performed in this planning-only PR.
- No need for a new general multi-floor system yet; expose stable portal/door IDs and positions for HPA-1253/1255. They can replace the parcels terminal behavior and unlock S2/S5 without duplicating Outside state. Keep HPA-1257 independent from geometry implementation except the existing Linear dependency.
