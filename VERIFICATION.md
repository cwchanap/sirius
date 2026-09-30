# MVP verification

## River banks — 2026-09-30

The Hoshikawa river sheet now ends inside its banks. **270 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors.

- A new check samples both long edges of the river sheet against the 1 m terrain mesh. It skips only the stretch where the brook strip carries on from the edge. Before the fix, the edges were up to 0.86 m above the ground (x = 36 on the village bank). Now they are at least 10.6 cm below it.
- The sheet was 4.8 m wide, narrower than its dip, so both edges hung over the dry hollow. It is now 8 m wide (z −12 to −4).
- Widening alone could not fix the village bank at x ≈ 34–38 and x ≈ −58 to −62. There the terrain's swell troughs left the ground within 1 cm of the water for over 10 m. `BaseHeight` now adds a 0.3 m village-side bank centred on z = −4. The countryside bank already met the water within 3.25 m and is unchanged. The brook's bed is still carved to −0.95 m.
- The brook strip now starts at the sheet's new edge (z −4). The brook-edge check no longer excludes the mouth's first metre: every brook edge is at least 18.8 cm below the bank.
- The first-boulder pin, the terrain continuity grid and the 31 village trees are unchanged.

Frame rate was not re-measured. Evidence is in `artifacts/hamlet/river/`: both test logs, and `review_hamlet.gd` captures including a new `river` view. `before/` holds the same `river` and `brook-mouth` views from the previous commit.

## Hamlet review minors — 2026-09-29

The ten minor findings deferred from the hamlet's whole-branch review are fixed. **269 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors. Each new check below failed before its fix:

- Brook water edges stay under the bank's 1 m mesh triangles. They were up to 0.46 m above the mesh; from z −4.5 north they are now at least 8 cm below it. The strip's half-width is 2.1 m, and it starts flush with the river sheet, which removes the dark seam where the two met. The mouth's first metre still lies over the river's dip, as the river sheet's own edges always have.
- No countryside boulder sits in the brook mouth. The boulder there is skipped only after its random draws, so the first-boulder pin still passes.
- Potted plants stand clear of decks and steps. The hall's pots now flank its porch step, and the clinic's stand on the engawa.
- The shrine mound's mesh faces are at most 25°. They were 32.4°; the mound's falloff radius grew from 11 to 14 m.
- Village grass keeps off the footpaths, which are now the shared `World.Footpaths` data.
- Fence rails follow the ground. A rail's two ends were up to 0.81 m apart in height above ground. Rails now run post to post, in every fence including Komorebi's.
- Every Hoshikawa sign hangs clear of its roof. The hall's sign had clipped its porch roof; it now hangs 0.25 m lower.
- The terrain continuity grid now extends south to z −8.9. This caught a 0.38 m step at z = 0 in `BaseHeight` that predates the hamlet; the village flattening now fades in over 3 m. The old bridge causeway (|x| < 4.5, z < −3) is skipped because its banks are steep by design.
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
