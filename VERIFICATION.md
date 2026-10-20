# MVP verification

## Environment revamp, phase 3 pilot fix — 2026-10-20

The user reviewed the pilot and asked to fix only the teahouse's eaves camera and its floating sign and lantern. No Tripo calls; the other five buildings are unchanged.

- **Eaves camera:** the model is re-imported at 7.2 m (was 5.93 m) from the same `teahouse-tripo-2k.glb`. Its eave underside is now about 4.0 m up at the front (was 3.3 m; the Minka's was 4.1 m). In the eaves-under view the spring arm again frames Mio from above the sign, as it did under the Minka eaves. I didn't raise the camera-only stop above the 3.3 m eave instead, because the camera would then sit inside the model's roof.
- **Sign and lantern:** the sign hangs at 3.2 m on two cords up into the eave, and the chochin hangs on its own cord with dark rims. Front close-ups show both cords meeting the eave beam.
- **Fit:** the walls are 9.7 × 6.5 m and stay on the pad's level apron. The roof (12.1 m) overhangs the apron's edge, so the teahouse check now tests the model's footprint, its vertices within 10 cm of its base, against `Height()`, instead of the whole bounding box. The tea bench, umbrella and stepping stones keep their places, since the front wall did not move. All four village corridors stay clear.
- **Self-test:** **355 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). The wall check also asserts that the camera-layer eave over the front is more than 3.8 m up. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1127/1071/1216, unchanged; primitives 2.772M/2.561M/2.641M → 2.773M/2.563M/2.642M from the cords and rims. CPU median 0.79–0.89 ms.
- **Captures:** the teahouse front and oblique, the three eaves views, the dialogue background and the east row, before (7c50cdf) and after, in native Metal.

## Environment revamp, phase 3 pilot — 2026-10-20

One Tripo building, The Copper Kettle teahouse, replaces its `Minka()` version. The other five Hoshikawa buildings stay on `Minka()` for comparison. No character, terrain or pad changed.

- **Captures:** `tools/review_revamp.gd` gained a front and an oblique view of the teahouse, framed from the ground at its centre. These, the three eaves spring-camera views, the dialogue background and the `review_hamlet.gd` east row, square and overview were taken before (5bb7119's code) and after, in native Metal.
- **Model:** 16,282 triangles with 2048² maps, 5.93 m tall against the Minka's 7.2 m to its ridge. In game it stands on its pad, inside the pad's flat apron, and its front wall is where the old one was. Its eaves are lower: their underside is about 3.3 m up at the front, against the Minka's 4.1 m. In the eaves-under view the spring arm now stops right behind Mio, because it meets that lower eave sooner.
- **Self-test:** **355 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 2b had 354). For the teahouse, the archetype check is replaced by a model check: under 20k triangles, textured and tinted, grounded on the pad, taller than 5.5 m, and its bounds inside the pad's flat apron. The sign check now casts up against every array mesh under a building instead of meshes named "roof", so for the teahouse the first face above the sign must be the model's eave underside. The eaves check is unchanged and passes against the model's trimesh. A new check casts at the front wall on layer 1 and stops at the model's wall line (3.5 m). Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1139/1071/1230 → 1127/1071/1216, primitives 2.72M/2.57M/2.60M → 2.77M/2.56M/2.64M. The Minka teahouse's merged material cells and its roof mesh cost more draws than the one model. Its 16k triangles, drawn in the colour and shadow passes, add up to 2% primitives where it is in view. CPU median 0.82–0.91 ms; fps 182–197.
- **Limits:** the other five buildings are still `Minka()`. No physical keyboard/mouse session is claimed.

## Environment revamp, phase 2b — 2026-10-19

The Tripo well and the cedar props (benches, lamps, barrels) are brightened. Three more Tripo models replace box-built props: the stone lantern (all four), the handcart and the notice board. No character, building or terrain changed.

- **Captures:** `tools/review_revamp.gd` gained three views framed from the ground under the prop: a village lantern, the cart and the notice board. All 22 views were taken before (4a1de3e) and after, in native Metal.
- **Models:** 7,748 / 8,168 / 7,605 triangles with 2048² maps. Tripo previews and in-game views from front and back show no baked-texture errors. The first capture showed the notice board's blank back to the road, so it was turned 180°.
- **Self-test:** **354 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; the pilot had 348). The six new checks cover each placement: under 10k triangles, textured and tinted, standing on `Height()` within 5 cm at the expected height, and solid to a ray at hip height. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1140/1072/1230 → 1139/1071/1230, primitives 2.63M/2.48M/2.50M → 2.72M/2.57M/2.60M. Each Tripo model adds a draw, which roughly offsets the merged primitive cells it replaced. The extra ~47k triangles, drawn in both the colour and shadow passes, account for the primitives. Four lanterns did not justify a MultiMesh. CPU median 0.72–0.80 ms; fps 150–241.
- **Limits:** the plant pots, field fences and Komorebi's benches are still box-built. The lanterns' paper screens no longer glow. No physical keyboard/mouse session is claimed.

## Environment revamp, phase 2 pilot — 2026-10-19

One Tripo prop (the village well) and five Blender props (lamp, bench, barrel, bamboo fence post and bay) replace box-built versions. No character, building or terrain changed.

- **Captures:** `tools/review_revamp.gd` gained seven fixed prop close-ups (two stone lanterns, the well, a bench, a street lamp, stored barrels and the bamboo fence). Their before shots were taken on the phase-1 build from the same coordinates, in native Metal.
- **Well:** the Tripo model has no baked-texture errors (renders from four sides and of its maps). In game it stands on `Height()` within 5 cm, its posts block the player, and its roof stops the camera. It reads darker than the surrounding props because Tripo's albedo is dark.
- **Props:** all five are baked by `Merge()` (2,382 static primitives and props into 244 meshes). Node names and colliders are unchanged, so the `Plant pot` and `Fence rail` checks still pass without edits. The bench reads very dark, because the cedar atlas cell is dark yakisugi.
- **Self-test:** **348 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 1 had 345). The three new checks cover the baked props, the well's model and the well's collision. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1126–1129/1045/1212 → 1140/1072/1230, primitives 2.59M/2.48M/2.47M → 2.63M/2.48M/2.50M. That is +11 to +27 draws, from the props' three materials plus the well's own material, which is not merged. A first pass with stone and rope materials cost 1159/1092/1253, so those two were dropped. CPU median 0.74–0.82 ms; fps 119–120.
- **Limits:** the stone lanterns, plant pots, field fences and Komorebi's benches are still box-built. No physical keyboard/mouse session is claimed.

## Environment revamp, phases 0 and 1 — 2026-10-19

Vegetation, light and the camera at the eaves, so the environment matches the Tripo characters. No character model changed.

- **Captures:** `tools/review_revamp.gd` (twelve close-ups, including the spring camera at the tea house eaves and a dialogue background) and the nine `review_hamlet.gd` views, before and after, from the same viewpoints in native Metal. One close-up viewpoint was moved after the first pass, because it sat inside the workshop; its before shot was retaken from an `origin/main` worktree. Comparisons are in `artifacts/hoshikawa-revamp/compare/`.
- **Foliage:** the new atlas's alpha is clean: corners 0, no blue-tinted opaque pixels, 77–81% of each sprite's visible pixels fully opaque. Close up, the sakura reads as small blossoms, not large torn-paper flowers. Two artifacts were traced and removed. One was a dotted pattern on the cards: shadow acne from cards self-shadowing, plus alpha-to-coverage dithering cards that were faded edge-on. The other was a brown core on backlit crowns, where shadows blocked the transmitted light. Crowns now cast but do not receive shadows, and alpha-to-coverage was dropped.
- **Seeded streams:** `countryside scatter is unchanged` still passes (first boulder pinned). Meadow seed positions keep their `rng` draws. Extra tufts and colours come from per-field generators. `villageRng` keeps its draws, and nothing draws from it after the lawn loop. The rice plants never drew from a stream.
- **Camera:** in the eaves shots the spring arm now stops under the tea house roof instead of rising through it. A new check per Hoshikawa building casts up from under the front eave: the camera layer is hit and layer 1 is not.
- **Self-test:** **345 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; main had 339 — the six new ones are the eaves checks). Both exit 0 with `SELF_TEST_PASS` and no shader errors. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1164/1098/1246 → 1126–1129/1045/1212, primitives 2.25M/2.21M/2.15M → 2.59M/2.48M/2.47M (+12–15%), from denser tree crowns and blade grass. Grass casts no shadow and its chunks drop beyond 90 m. CPU median 0.7–0.9 ms; fps is unchanged within run-to-run noise. The first viewpoint now varies by up to 3 draws between runs, likely a grass chunk at its visibility-range edge.
- **Limits:** the distant ridge mesh and the terrain textures were not changed in this phase. No physical keyboard/mouse session is claimed.

## Mio movement and combat polish — 2026-10-18

The user found the jump looked unwired, walking too fast, the dodge too short and the attack combo jerky. They also asked for a sword grip, the face strokes, the render-cost rise and attack2's timing.

- **Jump:** it was wired. Tripo's preset hops with straight legs, so once its height was removed the airborne frames looked like standing. `review_jump.gd` (its bone names updated for the new rig) now shows a crouch at takeoff, knees tucked in the air and a crouch on landing.
- **Walk and run:** walking drops from 4.3 to 2 m/s. In a game probe of planted-toe speed, the walk's foot slide fell from 3.09 to 0.36 m/s and the run's from 5.13 to 1.86 m/s. Tripo's run is a jog, so at 7.8 m/s some slide remains.
- **Dodge:** the whole 1.6 s roll used to be squeezed into 0.42 s. It now plays crouch, roll and rise in 0.6 s, at 7 m/s, so the distance is unchanged (4.2 m). Invulnerability is 0.4 s and the cooldown 0.85 s.
- **Combo:** Blender renders show all three attacks starting and ending in the same ready stance. The fastest right-hand frames are 7 of 15 for attack1 and 8 of 20 for attack3. attack2's short backhand spans frames 2–11 around its hit at about frame 7.
- **Grip:** close renders in idle, walk, guard and attack1 show the right fingers wrapped round the hilt.
- **Face:** before/after flat-texture renders show the cheek strokes, the dark band beside her left eye, the nose-bridge streak and smaller marks gone. A slightly paler patch remains beside her left eye.
- **Render cost:** primitives are unchanged from the previous build. The rise since the 65-bone rig was traced to level-of-detail choice. Old and new Mio meshes have identical LOD levels and the same mesh bounds. At the default LOD bias, Godot now draws the full-detail mesh (239,794 primitives per frame for Mio, against 121,334 before). At bias 0.01 both builds draw the same 2,030,620. Nothing is broken, and the change was left as it is.
- **Self-test:** `camera-relative walking` now expects 1.5–2.5 m in a second. **339 checks pass headless and in native Metal** (main had 339). Both exit 0 with `SELF_TEST_PASS`; the native log has no input restores. `just unit` passes 15 cases and `just e2e` 4, including Mio's steering test at the slower walk.
- **Evidence:** `artifacts/mio-polish/`.

## Zero's Japanese voice — 2026-10-18

- **Audio:** 18 Koume mp3s (eleven_v4, 44.1 kHz mono, 2.4 MB) in `assets/audio/voice/ja/Zero/`, one per Zero beat, each imported as `AudioStreamMP3`. `python3 tools/build_voice.py --check` passes for all 31 Japanese lines.
- **Self-test:** the voice-file check now covers all 31 Mio and Zero beats. Zero's opening line plays `Zero/Intro.0.mp3`, Mio's reply cuts it off and plays her own, and Ren, who has no voice files, stays silent. **339 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro); both runs exit 0 with `SELF_TEST_PASS` and no leak or error output.
- **Limits:** as for Mio's voice, the checks confirm which file plays, not how it sounds; no native-speaker review and no physical keyboard/mouse session.

## Unit and e2e test suites — 2026-10-18

- **Local runs:** in a Linux cloud container with Godot 4.6.2.stable.mono.official and .NET SDK 8.0.131: `just unit` passes 15 cases in under a second, `just e2e` passes 4 cases in about 30 s (one headless game start each), and the existing self-test passes 338 checks headless on Linux in 20 s.
- **CI:** the first GitHub Actions run of PR #8 (ubuntu-latest, Godot 4.6.2 from `chickensoft-games/setup-godot`) passed all three suites: 15 unit cases, `SELF_TEST_PASS` with every check, and 4 e2e cases in 26 s. The e2e job took under three minutes with an uncached import and then saved `.godot/imported` (480 MB) for later runs.
- **Mutation checks:** shifting a terrain-shader road constant, inserting an intro line before Mio's voiced beats, moving `NightFalls` and moving the clinic onto the road each fail the matching unit test; letting Escape only pause, and reversing Mio's forward input, fail the pause and steering e2e tests, which leave `scene_tree.json`, engine logs and process output under `test_output/`.
- **Limits:** e2e runs headless, so failure artifacts carry no screenshot. A windowed run under Xvfb with software Vulkan (llvmpipe) did not reach its first capture within four minutes, so CI does not attempt rendered runs; the Metal graphical self-test stays a local check. E2E input is engine-injected; no physical keyboard/mouse session is claimed.

## Mio's Japanese voice — 2026-10-18

- **Audio:** 13 Yuki mp3s (eleven_v4, 44.1 kHz mono, 1.1 MB) in `assets/audio/voice/ja/Mio/`, one per Mio beat, each imported by Godot as `AudioStreamMP3`. `python3 tools/build_voice.py --check` confirms `ja.json` still matches `Prologue.cs`.
- **Self-test:** five new checks. Every Mio beat in a named scene has an mp3 longer than half a second. Zero's opening line plays nothing. Mio's reply plays `Intro.1.mp3` and keeps playing once its text is completed. The homecoming plays `Homecoming.6.mp3`. Esc stops the voice. **338 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro); both runs exit 0 with `SELF_TEST_PASS` and no leak or error output.
- **Limits:** the checks confirm which file plays and that the player reports playing; nobody listened to the in-game mix during the run, and the Japanese readings were not reviewed by a native speaker. No physical keyboard/mouse session is claimed.

## Mio's Tripo Studio clips — 2026-10-16

The user rigged the low-poly Mio in Tripo Studio and made her clips there. Two bridge sends and one FBX download carried no animation or an incomplete set; the third bridge send held all fifteen actions.

- **Preservation:** the bridge scene was saved unchanged with `save_as_mainfile(copy=True)`. The open session was not edited.
- **Identification:** six frames of every action were rendered and the right hand's path was traced, to match each action to its prompt. The weak first attack1 and the unused slash and standing_relax presets were set aside.
- **Same mesh:** the animated send's vertices and UVs equal the first send's exactly, so the repaired textures apply unchanged.
- **Timing:** each clip keeps the previous clip's length. Measured in the exported game copy, the fastest right-hand frame is 7 of 15 for attack1 (hit at about 6), 5–6 of 16 for attack2 (about 7), 8 of 20 for attack3 (about 8) and 12–16 of 31 for skill (about 17).
- **GLB audit:** `check_mio_export.py` passes: 65 bones, 12 clips, 47,980 triangles, maximum skin weight-sum error 9.69e-8, exact embedded base-color and normal bytes. The GLB is 8.6 MB.
- **Poses:** Blender renders of all 12 clips beside the previous ones, and close renders of the sword hand in idle, walk, guard and attack1, show the sword held in the right palm.
- **Self-test:** the player rig checks now expect 65 bones and Mixamo bone names. **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and the native log has no input restores.
- **Render cost:** draw calls are unchanged. Primitives rose by 118,460 at each of the three viewpoints compared with the dense-rig low-poly build (2,133,766 → 2,252,226 at (0, 31)). The triangle count is the same, so this is more passes over Mio, probably shadow cascades; the cause was not investigated.
- **Captures:** `tools/review_tripo.gd` saved all eleven views in the current village. They and side-by-side comparisons with the previous build are in `artifacts/mio-tripo-animation/`.
- **Limits:**
  - Tripo's hands stay open, so the sword rests in an open palm.
  - attack2's swing opens its clip, so it lands about two frames before the hit.
  - The guard loop is the held block played forward and back.

## Low-poly Mio — 2026-10-16

The user sent a 47,383-triangle Mio over Tripo DCC Bridge. It replaces the 150,600-triangle decimation of the dense import.

- **Preservation:** the live bridge scene was saved unchanged with `save_as_mainfile(copy=True)`, and the open session was not edited. All repairs ran in background Blender on copies.
- **Identification:** renders of "3d fantasy character model" match the in-game Mio's hair, coat, scarf and satchel. Its surface lies a median 5 mm from the dense import at the same scale, so it is a separate Tripo generation, not a reduction of it.
- **Texture repair:** before/after renders of the head from front, both three-quarters and back show skin and white gone from the bangs, side locks and strands behind the ears. Eyes, ears and collar are unchanged. Outside the repaired texels, the JPEG re-save changes the base color by a mean 0.35/255.
- **GLB audit:** `check_mio_export.py` passes: 41 bones, 12 clips, 47,980 triangles, maximum skin weight-sum error 1.27e-7, and embedded base-color and normal bytes equal `assets/art/mio-lowpoly-*`. The GLB is 8.0 MB, down from 17.4 MB.
- **Deformation:** Blender renders of all 12 clips beside the previous model's showed matching poses. Close renders of run, attack1, attack3, skill, guard and stagger showed no tearing or stretching.
- **Self-test:** **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro), the same count as main. Both runs exit 0 with `SELF_TEST_PASS`. The native log has the two expected `TEST_INPUT_RESTORE` lines.
- **Render cost:** `render_cost.gd` primitives fell by 69,060 at each of the three viewpoints. Draw calls did not change.

  | viewpoint | draws | primitives (before → now) |
  |---|---|---|
  | (0, 31) | 1164 | 2,202,826 → 2,133,766 |
  | (0, 16) | 1098 | 2,158,544 → 2,089,484 |
  | Shrine steps | 1246 | 2,100,611 → 2,031,551 |

- **Captures:** `tools/review_tripo.gd` saved all eleven views of both the previous and new GLB in the current village. Both are in `artifacts/mio-lowpoly-integration/`.
- **Limits:**
  - The first in-game captures showed a greyer, noisier face than the previous model. The bake's broken normals were then flattened, and Tripo's roughness/metallic maps were replaced by the previous matte values.
  - A fine dot pattern from the sun's soft shadow remains on the face in close views. The previous model shows the same pattern in the same view, and it disappears when Mio casts no shadow.
  - The dark strokes near the eye corners come from Tripo's bake and were left as they are.

## Sora, Nao and Toma Tripo integration — 2026-10-16

Three Tripo Studio characters reached a live Blender session over DCC Bridge as rigged FBX, and nothing had saved them.

- **Preservation:** the scene was saved without change through the Blender MCP (`save_as_mainfile(copy=True)`), leaving the open session's file path and state as they were. The save reopened in background Blender with three 65-bone armatures, three meshes and twelve packed textures.
- **Identification:** renders of each character were matched to the v3 four-view references. anime is Sora, female is Nao and adventurer is Toma.
- **Originals:** the per-character originals were split from that save. Its hash was unchanged afterwards.

| | Sora | Nao | Toma |
|---|---|---|---|
| Source triangles = GLB triangles | 19,591 | 19,530 | 18,215 |
| Height (Blender audit of the GLB, soles at 0) | 1.78 m | 1.64 m | 1.74 m |
| Ankle above ground (self-test) | 0.129 m | 0.093 m | 0.146 m |

- **GLB contents:** each GLB holds one mesh, a 65-joint skin and a closed four-second `idle`.
  - The maximum skin weight-sum error is 9.69e-8.
  - Embedded base-color and normal bytes equal the source textures. Roughness and metallic are combined by Blender.
  - Godot uses the default scene import settings, matching `koma7.glb`.
- **Code:** `World.TripoResidents` now holds the asset, height and label height of all five Tripo residents, replacing the Chiyo/KOMA-7 conditionals. Chiyo and KOMA-7 keep their exact previous values.
- **New checks:** IntegrationCheck runs the same set over all five residents.
  - rig contract, own GLB, looping idle that moves the rig, planted foot, textured material, terrain placement
  - collision capsule fitted to the table height
  - idle length (six seconds for KOMA-7, four for the others)
  - ankle 0–0.2 m above ground
  - hips and feet fixed at five points through the idle
- **Results:** before merging main, 323 checks passed: 272 plus 51 new. After merging main's visual-novel prologue (282 checks on its own), **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings, errors or input restores. The existing Komorebi walk-to-service checks still reach Nao and Toma with their new capsules.
- **Render cost:** `render_cost.gd` draws fell at all three viewpoints, because each model is one surface where the shared villager had several. Primitives rose 0.4–4.4% at the same viewpoints.

  | viewpoint | draws (main → now) | primitives (main → now) |
  |---|---|---|
  | (0, 31) | 1229 → 1164 | 2,110,640 → 2,202,826 |
  | (0, 16) | 1154 → 1098 | 2,149,466 → 2,158,544 |
  | Shrine steps | 1311 → 1246 | 2,054,133 → 2,100,611 |

- **Captures:** `tools/review_villagers.gd` saved front, face, idle, side, back and real-dialogue captures for each, under `artifacts/{sora,nao,toma}-integration/`. All 18 were inspected. After the merge, the dialogue capture first finishes the typed-out line, because the capture stops processing. It was recaptured: the visual-novel layout shows each resident's portrait opposite Mio, with the full line and the 3D model behind. Hashes, the GLB audit, and the import, test, review and render-cost logs are in `artifacts/villagers-integration/`.
- **Limits:**
  - The heights are judgement calls; the references give ages and builds but no heights.
  - The idles are authored, because Tripo supplied no animation.
  - All input was engine-injected.

## Scene 1 visual-novel dialogue — 2026-10-16

The prologue's conversations now play as visual-novel scenes: portraits, name plates, typed-out lines, narration and Zero's link emblem.

- **New checks:** ten. Beginning the journey opens on Zero. Mio has a portrait and Zero has none. A line types out, the first press completes it, and the intro takes exactly two presses per line. Chiyo's first meeting mentions Shio, and talking to her again plays only her usual line. The homecoming's night falls before Mio mentions her mother, and Zero's next line changes the subject. Esc skips the rest of a scene, and restarting the prologue from the pause menu replays the opening. Keys go in through `Input.ParseInputEvent`; no physical keyboard or mouse was used.
- **Headless:** 282 checks pass (was 272), exit 0 with `SELF_TEST_PASS`, and the log has no warnings or errors.
- **Graphical:** this run was in a Linux container with no GPU, under xvfb with Mesa's software OpenGL and `--rendering-method gl_compatibility`, not native Metal on the Mobile renderer. All 281 checks pass at 640×400 on commit `3ead5f1` (before the resident portraits), exit 0 with `SELF_TEST_PASS`. The log's only errors and warnings are the container's: no audio device and no V-Sync control. Software rendering made the run take about two hours; an earlier attempt at 1440×900 was stopped after 142 passing checks.
- **Captures:** `tools/review_dialogue.gd` under the same xvfb setup, inspected by eye: the intro (both speakers and a line mid-typing), Ren's briefing, Zero's line in it, Hana with KOMA-7, Chiyo's closing narration, nightfall and the line about Mio's mother. Recaptured after the Codex portraits: Mio and the resident she speaks with stand at either side, head to hip above the text box, the listener dimmed, with Zero's emblem between them. The night overlay darkens the frame from "Night falls" onward.
- **Portraits:** the portrait check covers Mio and all ten residents. With `yui.png` moved away it fails with `SELF_TEST_FAIL`. How the eleven were generated, cut and checked is in `assets/art/portraits/portraits-generation.md`.
- **Native Metal:** on the owner's Mac, `tools/review_dialogue.gd` ran graphically on commit `8b4a35d` under Metal and printed `DIALOGUE_CAPTURE_OK`; the nine captures show the Codex portraits placed as above, with Mio left facing right and the resident right facing left. That Mac's headless self-test also passed.
- **Not covered:** the full graphical self-test under native Metal.

## Static geometry merge — 2026-10-14

Draw calls fell by 70–76%, and the frame looks the same. Before the change, World's static boxes, spheres and cylinders were 2,675 separate meshes. Each cost a draw call, and most cost a second one in the sun's shadow pass. `World.Merge()` now bakes them into 233 meshes, one per material and 32 m cell, after `_Ready` builds the world. The originals stay hidden, so colliders and every name-based check still work.

| `render_cost.gd` viewpoint | Draws before → after | Primitives before → after | Median CPU render ms before → after |
|---|---|---|---|
| (0, 31) | 4666 → 1229 | 2,024,204 → 2,110,640 | 1.92–1.94 → 0.77 |
| (0, 16) | 3897 → 1154 | 2,074,466 → 2,149,466 | 1.64 → 0.71 |
| Shrine steps | 5389 → 1311 | 1,996,473 → 2,054,133 | 2.16–2.30 → 0.79–0.80 |

- **Measurement:** draws and primitives repeat exactly under `--fixed-fps 60`. The CPU times come from back-to-back runs on the same machine, at load average about 5. fps read 118–120 both before and after, so a cap limits it, not the render.
- **Primitives:** they rise about 4%, because a merged 32 m cell is drawn whole when any part of it is in view.
- **Materials:** `Mat()` now returns one shared material per colour, emission and `twoSided`, which cut its materials from 916 to 101. `Merge()` groups by material reference. Grouping by serialized material content gave the same draws but took about 300 ms at startup; the merge now takes 80–93 ms. Five call sites used to set `CullMode` on a `Mat()` result, and they now pass `twoSided` instead. World clears the cache in `_ExitTree`, and six `--verbose` quit runs report no leaked resources.
- **Exclusions:** transparent materials, `PlaneMesh`, meshes with visual children and meshes under any non-`Node3D` parent are not merged.
- **Pixel diff:** frames captured at the three viewpoints match the pre-change frames. The (0, 31) view has no pixel off by more than 4/255. The other two views have 1 and 2 pixels over 25/255, at triangle edges. Normals use the inverse transpose, because many spheres are scaled unevenly.
- **New check:** the merged meshes must hold exactly the hidden originals' 165,084 triangles, with the same world-space bounds. Removing the per-object transform from `Merge()` makes it fail.
- **Results:** 272 checks pass headless and in native Metal. Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings or errors.

The frame-rate recheck below predates this change; its draw counts are now the "before" column. Probes, frames, diffs and logs are in `artifacts/perf/`.

## Brook mouth joint — 2026-10-14

The dark line where the brook strip met the river sheet is gone.

- **Cause:** there were two height steps at z −4. The brook sat 2 cm below the river (−.40 against −.38). And the water shader's 3.5 cm wave is computed per vertex. The river's edge has a vertex every 1.26 m, so it followed the sine. The brook's mouth row had only its two side vertices, 4.2 m apart, which is almost exactly one wave period (4.19 m), so that edge rose and fell flat. The gap between the edges reached about 7 cm and showed the dark channel bed through it.
- **Fix:** both sheets now sit at `World.WaterLevel` (−.38). The brook's mouth row reuses the river sheet's own edge vertices and fans into the strip's first full row at z −3.
- **Checks:** a new check requires every mouth vertex to lie on the river's vertex grid at the river's level, spanning the brook's full width. The brook-edge check now takes the strip's sides as the lengthwise edges that only one triangle uses. Its old rule, edges that don't cross the brook's centreline, also caught the mouth fan's inner edges over open water. On the old two-vertex strip both rules pick the same edges. The highest brook edge is now −0.168 m (was −0.188), still beyond the 4 cm margin.
- **Results:** 271 checks pass headless and in native Metal. Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings or errors.
- **Captures:** a throwaway probe filmed the joint from two angles at eight moments across one 2π s wave period, on the old and new code. The line shows in most old frames and in no new ones. In a pixel diff of a whole frame, only the joint and the brook's west waterline change. The waterline moves because the brook is 2 cm higher.
- **Side effect:** part of the mouth now falls outside a shadow that used to cover it. The patch has stair-stepped shadow-map edges like those already on the river. It is not the water shadowing itself: turning off shadow casting on both water meshes changes only 311 pixels.

Evidence is in `artifacts/hamlet/joint/`: the probe, the before/after frames, the contact sheets, `review/` recaptures and both self-test logs.

## Shutdown audio leak — 2026-10-14

The intermittent shutdown error is fixed. It was "1 resources still in use at exit" plus an ObjectDB leak warning, printed after `SELF_TEST_PASS`.

- **What leaked:** `--verbose` names `res://assets/audio/valley.wav` and its `AudioStreamPlaybackWAV`, both at reference count 1. This matches the earlier verbose logs in `artifacts/tripo-jump/`.
- **Cause:** the ambience player stops its playback when it leaves the tree at quit. The AudioServer only drops a stopped playback, and the stream it holds, on the audio thread's next mix step. When the engine shut the AudioServer down first, both leaked.
- **Fix:** `Game._ExitTree` stops the ambience and blocks for 100 ms, so that mix step runs first. Every quit path goes through it: the self-test, the HUD Quit buttons, closing the window and `--quit-after`.
- **Evidence:** the repro is `Godot --headless --verbose --path . --quit-after 120 -- --play`. Without the fix it leaked in 6 of 20 runs, and in 20 of 20 in a control rerun just after the fixed runs. With the fix it leaked in 0 of 40.
- 270 checks pass headless and in native Metal, both runs with `--verbose`, and neither log reports a leak.
- The verbose graphical log also shows 15 "Image format RGB8 not supported by hardware" warnings. These appear only under `--verbose`; the non-verbose graphical log of the same tree has none.

Evidence, including every repro run log and both self-test logs, is in `artifacts/shutdown-leak/`.

## Frame-rate recheck — 2026-10-14

The minor fixes and the river widening cost nothing measurable. `main` (`d78ff65`) and `3670287` were measured back to back with `tools/render_cost.gd`, at the same three viewpoints as before, with vsync off and `--fixed-fps 60`. Repeat runs gave identical draw and primitive counts.

| Viewpoint | Draw calls (main / base) | Primitives (main / base) |
|---|---|---|
| (0, 31) | 4666 / 4665 | 2,024,184 / 2,023,554 |
| (0, 16) | 3897 / 3898 | 2,074,446 / 2,074,692 |
| Shrine steps | 5389 / 5389 | 1,996,457 / 1,996,771 |

- Primitives differ by at most 0.03%.
- Median CPU render time is 1.75–2.43 ms on both commits, and uncapped fps is 123–156 on both (Apple M1 Pro, Metal, Forward Mobile). The run-to-run spread is larger than any difference between the commits.
- Absolute fps is not comparable with the 118–120 fps recorded below. Other apps put the machine's load average between 9 and 56 during these runs, and vsync no longer caps at 60 Hz: a vsync-on run reached 110–144 fps.
- GPU render time reads 0 under Metal Forward Mobile, so the probe does not sample it.
- The old `fps.gd` probe left the mouse captured after `Start()`, so moving the real mouse turned the camera, and runs varied 6× in draw calls. `render_cost.gd` releases the mouse. Without `--fixed-fps`, walking actors still shift the counts by a few draws between runs.

Evidence, including `summary.txt` and every run log, is in `artifacts/hamlet/fps-recheck/`.

## River banks — 2026-09-30

The Hoshikawa river sheet now ends inside its banks. **270 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors.

- A new check samples both long edges of the river sheet against the 1 m terrain mesh. It skips only the stretch where the brook strip carries on from the edge. Before the fix, the edges were up to 0.86 m above the ground (x = 36 on the village bank). Now they are at least 10.6 cm below it.
- The sheet was 4.8 m wide, narrower than its dip, so both edges hung over the dry hollow. It is now 8 m wide (z −12 to −4).
- Widening alone could not fix the village bank at x ≈ 34–38 and x ≈ −58 to −62. There the terrain's swell troughs left the ground within 1 cm of the water for over 10 m. `BaseHeight` now adds a 0.3 m village-side bank centred on z = −4. The countryside bank already met the water within 3.25 m and is unchanged. The brook's bed is still carved to −0.95 m.
- The brook strip now starts at the sheet's new edge (z −4). The brook-edge check no longer excludes the mouth's first metre: every brook edge is at least 18.8 cm below the bank.
- The first-boulder pin, the terrain continuity grid and the 31 village trees are unchanged.
- After the self-review, both edge checks require at least 4 cm of burial, up from 2 cm. Water's vertex wave lifts edges by up to 3.5 cm, so a 2 cm limit could pass an edge that still breaks the surface at a crest. The measured margins are unchanged (18.8 cm brook, 10.6 cm river), and 270 checks pass again headless and in native Metal. Those logs are `selfreview-headless.log` and `selfreview-graphical.log`. One headless run printed a resource-in-use error and an ObjectDB leak warning at shutdown, after `SELF_TEST_PASS`. A headless rerun was clean, matching the intermittent shutdown report recorded under the 81-check run below.
- The self-review's remaining nits are now written down and the behaviour is unchanged. The sign check fails when no roof is overhead, and its message now says so; all six signs hang under their eaves. A probe confirmed the brook-mouth boulder skip removes exactly one boulder, at (19.1, −4.2) on the −0.95 m channel bed, so its z > −9 window stays. `CLAUDE.md` now notes that the water sheets depend on the dip, bank and channel in `Height()`. 270 checks pass headless and in native Metal, both runs clean (`nits-headless.log`, `nits-graphical.log`).

Frame rate was not re-measured. Evidence is in `artifacts/hamlet/river/`: both test logs, and `review_hamlet.gd` captures including a new `river` view. `before/` holds the same `river` and `brook-mouth` views from the previous commit.

## Hamlet review minors — 2026-09-29

The ten minor findings deferred from the hamlet's whole-branch review are fixed. **269 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors. Each new check below failed before its fix:

- Brook water edges stay under the bank's 1 m mesh triangles. They were up to 0.46 m above the mesh; from z −4.5 south they are now at least 8 cm below it. The strip's half-width is 2.1 m, and it starts flush with the river sheet, which removes the dark seam where the two met. The mouth's first metre still lies over the river's dip, as the river sheet's own edges always have.
- No countryside boulder sits in the brook mouth. The boulder there is skipped only after its random draws, so the first-boulder pin still passes.
- Potted plants stand clear of decks and steps. The hall's pots now flank its porch step, and the clinic's stand on the engawa.
- The shrine mound's mesh faces are at most 25°. They were 32.4°; the mound's falloff radius grew from 11 to 14 m.
- Village grass keeps off the footpaths, which are now the shared `World.Footpaths` data.
- Fence rails follow the ground. A rail's two ends were up to 0.81 m apart in height above ground. Rails now run post to post, in every fence including Komorebi's.
- Every Hoshikawa sign hangs clear of its roof. The hall's sign had clipped its porch roof; it now hangs 0.25 m lower.
- The terrain continuity grid now extends north to z −8.9. This caught a 0.38 m step at z = 0 in `BaseHeight` that predates the hamlet; the village flattening now fades in over 3 m. The old bridge causeway (|x| < 4.5, z < −3) is skipped because its banks are steep by design.
- Each resident's talk spot is standable: the dialogue check now lets physics settle the player for 10 frames before `Interact()`. This check passed on its first run. It hardens the old teleport-only dialogue check rather than reproducing a defect.

The foliage back-face shading was checked with a render probe, since headless runs have no renderer. A leaf card lit from above measured luminance 1.000 from above and 0.627 from below. With `cull_disabled`, Godot flips `NORMAL` on back faces. After `foliage.gdshader` undoes that flip, both sides read 1.000.

Frame rate was not re-measured for these changes. Evidence is in `artifacts/hamlet/minors/`: both test logs, `foliage-probe.txt`, and eight `review_hamlet.gd` captures. These include a new `brook-mouth` view. The `terrace` camera was also moved, because the earlier capture had it inside a keyaki crown.

## Organic hamlet — 2026-09-29

Hoshikawa was rebuilt as an organic hamlet. The changes are new terrain terms in `Height()`, six Japanese building archetypes, a brook and footbridge, a terrace, a shrine, generated tree models and village dressing. The C# build passes with the expected NU1900 warning, and so does the Godot import. **242 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Each run exits 0 with `SELF_TEST_PASS`, and neither log contains runtime warnings or errors. The new checks cover:

- every house corner on a level pad within 5 cm;
- the carved brook, the footbridge carrying the player, and climbing out of the brook channel;
- walking up the torii steps onto the shrine plateau;
- reaching the west terrace by its ramp;
- ray-cast clearance of the spawn → rice-field route;
- archetype roofs, with Komorebi's buildings unchanged;
- each tree species loading with game materials and standing grounded (31 village trees and shrubs);
- the first countryside boulder's exact position, so the seeded scatter stream stays fixed.

Three more checks were added after the whole-branch review:
- `Height()` has no step larger than 0.1 m across a 5 cm grid of the village. This caught a 0.48 m step where two house aprons overlapped; the pad blend is now continuous.
- Every face of the 1 m terrain mesh along the brook banks is at most 40°. The banks were 44°; the widened channel brings them to 37.4°.
- Each village trunk's base meets the ground on every side, so no base floats on a slope. Trunks now start 0.5 m below ground.

All previous route, dialogue, combat and quest checks still pass. These are engine-injected checks, not a physical keyboard/mouse session.

Frame rate: at the 60 Hz vsync cap the village holds 59–60 fps at three viewpoints. With vsync off it reaches 118–120 fps, against 144–160 fps at the base commit, so the new trees and buildings cost about 1.5–2 ms per frame. No visibility ranges were needed.

Evidence is in `artifacts/hamlet/`:

- `headless-test.log` and `graphical-test.log`;
- `fps.txt`;
- `before-after.png`, comparing `--overview`, `--portrait` and the `original-village` landscape view;
- `final/`: seven `review_hamlet.gd` captures plus the six `review_valley.gd` captures;
- `task1`–`task5/`: per-step captures;
- `before/valley/`.

Running `review_valley.gd` during this work overwrote the gitignored `artifacts/countryside/` renders. They were regenerated from base commit `7c3af66` in a scratch worktree and restored, but they are re-renders, not the original files. Generated textures are used unedited, with two exceptions: the foliage atlas had its blue key removed, and the bark atlas was split into quadrants. Provenance is in `ASSETS.md`.

## Shared character importer — 2026-09-20

`tools/import_character.py` now handles saved Blender, GLB, glTF and FBX models; Chiyo and KOMA-7 rebuild through small compatibility presets. `tools/check_character_import.py` passes in Blender 5.2.1, exercising all four formats, transformed multi-mesh skinning, source/texture preservation, grounding, clip renaming and actual animated samples, optional authored idle, static meshes and invalid-input/overwrite guards. This is a local export-ready-model workflow, not automatic rigging or combat retargeting; Mio's specialized importer remains unchanged.

Both production NPC GLBs were rebuilt with the shared importer. Chiyo retains 41 bones, 89,999 triangles, a 1.58 m height and breathing idle; KOMA-7 retains 65 bones, 96,316 triangles, a 1.25 m height and head-turn idle. Every embedded image matches the preceding exports byte for byte, including combined roughness/metallic maps. Original source hashes and Mio/villager/patrol GLB hashes are unchanged. The exporter still reports the existing sampler-combination warning and Chiyo's four-influence skin-weight normalization warning.

C# build and Godot asset import pass. **138 checks pass both headless and in native Metal**, each exiting 0 with `SELF_TEST_PASS` and no runtime warnings/errors. The build retains the expected NU1900 audit-feed warning. Fresh front, face/idle, side, back and dialogue captures were inspected, with front views compared directly to the preceding exports; no visible import regression was found. This uses engine-injected input, not physical keyboard/mouse testing. Evidence: `artifacts/generic-import/`; per-asset source hashes/options/export reports: `artifacts/model-imports/{chiyo,koma7}/build.json`. Usage and the reproducible importer check are in README's import section.

## KOMA-7 Tripo integration — 2026-09-20

KOMA-7 now uses the supplied cream-and-jade gardener model instead of the patrol robot variant. The source has 65 bones and no actions. The game copy retains all 48,138 source vertices / 96,316 triangles, scales to 1.25 m, and includes one authored six-second head-turn idle. His capsule and label fit the smaller model; village position, dialogue and role are unchanged.

C# build and Godot asset import pass. **138 checks pass in both headless and native Metal runs**, each exiting 0 with `SELF_TEST_PASS` and no runtime warnings/errors in the final logs. Checks cover the exact KOMA-7 model and rig, textured material, idle playback/deformation/duration, fitted collision, stationary hips/both feet, dialogue, and the existing movement/combat/quest/restart behavior. The build retains the expected NU1900 vulnerability-feed warning. These are engine-injected integration checks, not a physical keyboard/mouse acceptance session.

The 24,144,100-byte GLB audit confirms 65 bones, 96,316 triangles, a closed six-second animation and normalized skin weights (maximum sum error 9.78e-8). Embedded base-color and normal images match the original packed bytes. Four original 4K texture files are preserved; Blender combines roughness and metallic for glTF. The original `.blend` and Mio/Chiyo/villager/patrol GLB hashes are unchanged. Blender reports a sampler-selection warning while combining texture nodes; the exported material and textures were inspected in Godot.

Six native captures were inspected: front, face, idle, side, back and dialogue. Front/face/idle/dialogue use the actual village placement. Side/back temporarily place KOMA-7 on the street, grounded by a physics ray, for an unobstructed view; review close-ups hide his name label. Evidence and reproducible capture script: `artifacts/koma7-integration/` and `tools/review_koma7.gd`. Sandbox-only Blender startup/editor-directory failures were resolved by running the installed applications with their normal permissions.

## Chiyo Tripo integration — 2026-09-16

Chiyo's supplied 41-bone model replaces her shared villager instance. The original contained no actions; the game copy has a four-second authored breathing idle with stationary hips/feet, a 1.58 m height, and 89,999 triangles. Her placement uses the existing terrain height, with her label and capsule fitted to the new model. She faces the village road and retains her existing dialogue.

C# build and Godot import pass. **124 checks pass in both headless and native Metal runs**, each printing `SELF_TEST_PASS`, exiting 0, and showing no runtime warnings/errors. New checks verify Chiyo's exact rig and source scene, idle playback/loop/deformation, planted foot, textured material and terrain placement. The existing dialogue, player movement/jump, combat, other residents and full quest/restart checks still pass. The expected NU1900 audit-feed warning remains in the build.

The exported GLB audit verifies normalized four-joint skinning (maximum weight-sum error 1.42e-7), 89,999 triangles, the closed four-second animation loop, and unchanged embedded base-color/normal bytes. Roughness and metallic maps are combined by the glTF exporter. The original Chiyo source and Mio/villager/patrol runtime hashes are unchanged. Native front, face, side, back and dialogue captures were inspected. The front/face/dialogue show her actual village placement; side/back temporarily move her to the street in the capture process to avoid building occlusion. No separate physical keyboard/mouse acceptance session is claimed. Evidence: `artifacts/chiyo-integration/`.

## Tripo jump correction — 2026-09-13

The source jump contains five hops. The runtime export now retains frames 1–13, removes the extra vertical travel and maps ascent/descent to player velocity. Contact recovery starts only after a physical landing; long falls hold the descent pose. The original Tripo source remains unchanged, and all eleven other animation channel payloads match the preceding export byte for byte.

C# build and Godot import pass. All **118 gameplay checks pass**, including standing, running, consecutive jumps and long falls. Native before/after recordings and sampled bone motion confirm the repeated bouncing is gone. These checks use engine-injected input, not a separate physical keyboard session. The build retains the NU1900 audit-feed warning. Verbose self-test shutdown identifies the outstanding resource warning as `assets/audio/valley.wav` and its audio playback instance; it is not an animation assertion failure. Evidence and reproduction commands are in `artifacts/tripo-jump/README.md`.

## Second arrangement review fixes — 2026-09-13

Moved the lodge/store signs onto their awnings, supported all three workshop tables, placed the clinic marker clear of its window planter, and hung orchard fruit below the canopy with stems. Also moved Ren clear of the workshop table after the close-up exposed his overlap.

The C# build passes with the existing NU1900 audit-endpoint warning. All **81 headless integration checks pass**; the final run exits 0 without warnings or errors. Thirteen native Metal captures were inspected, with passing checks for both signs' visibility past the awnings and Ren's table clearance. Evidence and the retained first-run certificate-store error are documented in [the fix results](/Users/chanwaichan/workspace/sirius/artifacts/arrangement-fixes-2/RESULT.md). No new physical keyboard/mouse session is claimed.

## Asset arrangement follow-up — 2026-09-13

All **81 integration checks pass** in headless Godot and the native Metal renderer. Both final runs exit 0 without runtime errors or warnings. Earlier headless runs intermittently reported one resource still in use during shutdown; the original occurrence remains in `headless-test-first.log`, and its intermittent cause is not established. The C# build succeeds with the existing NU1900 vulnerability-audit endpoint warning.

The controller now walks onto the shelter and overlook floors without jumping, reaches each Komorebi service from the street, enters both growing areas through fence openings, and reaches Hoshikawa's rice fields. Closed fence sections have collision. Raycasts against the real terrain collider measure a maximum 0.051 m separation for the new garden surfaces. The original combat, dialogue, bridge route, mission and restart checks still pass.

Early graphical attempts were interrupted by OS focus changes releasing injected input. `graphical-focus-check.log` records focus loss at physics frame 116 and the held movement action disappearing. The test now maintains its intended held actions across those resets; the late-guard check waits until the actual deflect window closes. The final graphical log records restored run, forward and guard actions and still passes every check. This is engine-injected input validation, not a physical keyboard/mouse acceptance session. Gameplay input handling is unchanged.

Evidence is under `artifacts/arrangement-after/`: `headless-test.log`, `graphical-test.log`, `audit.log`, and the rendered arrangement captures. `tools/review_arrangement.gd` reproduces the inspection views, including rice-plant contact in `rice-grounding.png`. The original review is retained in `artifacts/arrangement-review/REVIEW.md`; pre-edit source copies are text files in `artifacts/arrangement-before/`.

## Countryside expansion — 2026-09-12

C# build passes with the existing NU1900 audit-endpoint warning. The expanded map passes **71 checks** in both headless Godot and the native Metal renderer, with `SELF_TEST_PASS`, exit 0 and no runtime errors or warnings in the final logs.

The real player controller traverses the new route from the relay through the viaduct and across Cedar Bridge into Komorebi, then returns via the overlook and shelter. Checks cover all 11 buildings and 10 residents, Emi's recovery service, the northern collision boundary, and the original combat and complete relay mission. This is engine-driven input validation; no separate physical keyboard/mouse session is claimed.

Evidence: `artifacts/countryside-headless-test.log`, `artifacts/countryside-graphical-test.log`, and six inspected landscape renders under `artifacts/countryside/`. Regenerate the renders with `--script res://tools/review_valley.gd`. The pre-expansion world sources are saved as text under `artifacts/countryside-before/`.

## Earlier character validation

Latest character import and graphical gameplay check validated locally on 2026-09-09 with Godot **4.6.2 Mono**, .NET 8 target, and Apple M1 Pro / native Metal rendering.

| Gate | Result |
|---|---|
| C# build | Pass, 0 errors. One NU1900 warning: NuGet vulnerability-audit endpoint unreachable. The required Godot packages were available and restored. |
| Asset import | Pass: generated PNGs, audio and three animated GLBs. |
| In-engine gameplay integration | **57 checks passed**, `SELF_TEST_PASS`, exit 0. |
| Final graphical integration runtime log | No errors or warnings. |
| Visual inspection | Village, terrain, character rig, combat feedback, dialogue and title screen inspected using actual Godot-rendered screenshots. |
| Physical keyboard/mouse session | Not repeated for this model revision. Engine input actions were exercised by the integration check. |

The integration check covers actual physics movement, running, jumping, landing, dodge displacement and invulnerability, walking from the village across the bridge, all six NPC conversations, mission acceptance, deflection timing, guard posture, unblockable strikes, one-hit-per-swing behavior, skill range/cooldown, posture break/finisher, healing, enemy patrol and pursuit, an enemy's own timed attack, pause, relay activation, mission completion and fresh restart.

Final graphical command:

```sh
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --fixed-fps 60 --log-file artifacts/human-graphical-test.log -- --self-test
```

Evidence:

- `artifacts/village.png`: final normal gameplay view.
- `artifacts/village-overview.png`: village layout.
- `artifacts/combat-deflect.png`: timed-deflect feedback during the integration check.
- `artifacts/mission-complete.png`: completed prologue dialogue with Ren in the village.
- `artifacts/title.png`: illustrated entry screen.
- `artifacts/human-graphical-test.log`: detailed local results; runtime logs are git-ignored.

Frame-rate readings in screenshot logs are brief local samples, not a sustained benchmark. No hosted build, export-package testing, or physical input validation is claimed.

## Mio model from turnarounds — 2026-09-09

| Asset | Skinned meshes | Triangles | Vertices with significant blended weights |
|---|---:|---:|---:|
| Mio | 15 | 157,979 | 27,023 |
| Shared human villager | 14 | 133,500 | 24,063 |

The full-body and head sheets were generated before this revision and retained in `assets/art/`. Five calibrated reference planes are stored in the `MioTurnaroundGuides` Blender scene, outside the runtime export. The existing 58-bone, 12-clip rig and deformable base are retained; both mesh coordinates and bone rest positions use the same reference fit. Sleeve weights were corrected at the authored cross-sections.

`python3 tools/check_mio_export.py` passes against the final GLB: finite geometry, normalized four-weight skinning, valid bone indices, all named clips, polygon budget, and byte-for-byte identity of all four embedded source textures. Godot's extracted new costume and head-sheet PNGs also match the source bytes. `artifacts/mio-export-check.json` records the exact asset hash.

Godot Mono import passes. The latest Blender facial volume revision passes all **57 checks**, reports `SELF_TEST_PASS`, exits 0 and has no errors or warnings. An earlier turnaround revision had an intermittent timed-deflect failure while Blender was rendering; its failed log remains at `artifacts/turnaround-first-graphical-test.log`. No C# gameplay changes were made.

The latest Godot integration log and inspected Blender front, profile, three-quarter, clay, full-body, running and raised-arm renders are collected in `artifacts/MIO_BLENDER_REFINEMENT.md`. The preceding model, source and renders are preserved in `artifacts/face-volume-before/`. The model still simplifies the reference's fine cloth folds, ornaments and facial nuance; passing functional checks does not establish visual parity.

The preceding satchel revision initially failed the running-speed check; the unchanged rerun passed all 57 checks. The failure is preserved in `artifacts/satchel-first-graphical-test.log`; its intermittent cause remains unresolved.
# Tripo player replacement — 2026-09-13

Mio now uses the user-provided Tripo model. Final GLB validation passed with 41 bones, 12 clips, 150,600 triangles, normalized skin weights and exact matches to both packed 4K textures. The four supplied clips deform the imported skeleton and remain centered on the physical controller. C# build, Godot import and the final in-engine self-test passed (105 checks); native village captures verify the model and animation poses. The build reported one NU1900 warning because NuGet vulnerability metadata was unavailable. Details, backups, the recovered Blender session and remaining hand/cloth limitations are in `artifacts/tripo-integration/README.md`. Earlier model-specific counts below are historical.
