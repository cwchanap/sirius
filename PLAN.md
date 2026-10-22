# Hoshikawa — The Offline Valley

Playable Godot C# countryside slice of the user's dual-protagonist JRPG concept.

## Story baseline
Based on the latest revision of “Story Main line” (6a75647c-8d84-83e8-b467-45bf0fb6b41e): 22nd-century Japan; humans live in rural offline settlements while AI/robots inhabit cities and imprison humans. Mio is a 22-year-old scavenger/repairer. Zero has his own mechanical body in the city; their voluntary sensory link does not transfer bodily control. This slice follows Mio. Zero contributes remotely, not as a village resident.

## Work and acceptance
- [x] Generate original painted terrain/material atlas and Mio key art with imagegen; retain prompts/provenance.
- [x] Model Mio, a villager, and a patrol robot using Blender MCP; export animated GLBs and editable Blender source.
- [x] Build six distinct buildings, six named talkable NPCs, rolling countryside, bridge, farmland, forest, and a relay landmark.
- [x] Implement camera-relative movement/run/jump/dodge, sword combo, timed deflect/guard, posture breaks and execution, one cooldown skill, three patrol enemies, health/recovery.
- [x] Add a complete objective: meet Ren, clear patrols, activate manual relay, return to Ren. Add title, pause, controls, dialogue, combat feedback, ambient audio, and restart.
- [x] Build with installed Godot Mono/.NET; run a deterministic integration self-check; inspect actual rendered gameplay and fix issues.

## Scope
Single outdoor map, one playable human character, one remote AI partner, no city chapter or building interiors. Original stylized anime-inspired characters and warm painterly landscapes; deliberate MVP geometry and animation, not franchise assets.

## Countryside and second settlement

- [x] Extend the playable northern terrain, scenery, collision boundaries and road beyond the viaduct.
- [x] Add Cedar Pass, a second river bridge, roadside shelter, overlook, orchards and fields.
- [x] Build Komorebi with five buildings, four talkable residents and a medic; update region labels and local minimap.
- [x] Traverse both directions with the real controller, verify all residents and the original mission, and inspect fresh Godot captures. Both headless and native Metal runs passed 71 checks.

## Validation result
C# build succeeded. The final graphical integration run passed 57 checks with `SELF_TEST_PASS`, exit 0, and no runtime errors or warnings. Village, countryside combat, dialogue completion, and title renders were inspected. See `VERIFICATION.md` for evidence and remaining environment limitations.

## Asset arrangement review follow-up

- [x] Grade shelter and overlook approaches to meet their floors and keep furniture clear of entry routes.
- [x] Fit garden and rice-bed surfaces to terrain, with paths and openings through physical fences.
- [x] Angle Komorebi buildings around the courtyard, cluster services and props, and orient the overlook toward Cedar River.
- [x] Extend integration coverage onto the actual platforms, through the fields, and from the street to all four Komorebi services. Isolate injected test input from desktop focus changes.

## Human model refinement

- [x] Integrate the user's Chiyo Tripo NPC with preserved source/textures, a fitted game copy, authored idle, existing dialogue and 124 passing headless/native checks.
- [x] Integrate the user's KOMA-7 Tripo gardener with preserved source/geometry/textures, a 1.25 m game copy, authored head-turn idle, existing dialogue and 138 passing headless/native checks.
- [x] Share the character import pipeline across Blender/GLB/glTF/FBX sources, retain Chiyo/KOMA-7 presets, document future imports, pass the importer regression check and rebuild both NPCs with 138 passing headless/native checks and fresh visual review.
- [x] Integrate the user's Sora, Nao and Toma Tripo models from a DCC Bridge Blender session: preserved per-character originals, full-geometry game copies, breathing idles, existing dialogue and roles, a shared `World.TripoResidents` table, and 333 passing headless/native checks after merging the visual-novel prologue.

- [x] Replace the rigid primitive humans with shaped anime faces, continuous garments, tapered limbs, five-finger hands and refined hair.
- [x] Generate and UV-map amber iris art; preserve the original Mio palette and clothing identity.
- [x] Replace 12-bone rigs with 58-bone anatomy and normalized blended weights; export exactly 12 runtime clips per human.
- [x] Verify both human exports, all six in-game human skeletons, animated poses and the complete graphical gameplay check.

## Original-illustration Mio pass

- [x] Keep the first generated Mio key art as the identity and costume authority.
- [x] Generate a matching front/back sheet and face/hair diffuse art; preserve the prompts.
- [x] Rebuild Mio's visible face, hair, open coat, cuffs, scarf, boots, satchel and wrist terminal in Blender MCP.
- [x] Keep the 58-bone / 12-clip runtime contract and mirror the combat poses for the illustration's sword hand.
- [x] Inspect real Blender front, three-quarter, rear, profile, run, guard and attack renders; inspect the Godot export and rerun integration checks.

## Proportion correction

- [x] Shorten the lower face and exposed neck; round the chin and join the neck into the jaw.
- [x] Join the coat body and sleeves into sloping shoulders; add ribcage, hip and limb volume, lengthen the forearms, and connect the wrists beneath the cuffs.
- [x] Adjust the rig and idle pose; compare identical Godot camera views and inspect untextured geometry plus animated poses.
- [x] Re-export the 58-bone, 12-clip model and pass all 57 graphical gameplay checks.

## Sculpt rebuild after proportion review

- [x] Preserve the rejected model, source and matching renders under `artifacts/sculpt-before/`.
- [x] Rebuild the skull/jaw/profile and ribcage/waist/pelvis/leg forms; inspect clay previews before export.
- [x] Refit the crown/fringe/ponytail, collar, strap and costume UVs; correct torso-to-arm skinning and shoulder pivots.
- [x] Export the reviewed Mio, preserve the 58-bone/12-clip contract, pass 57 graphical checks and save final comparisons.

## Organic hamlet (Hoshikawa rebuild)

- [x] Reshape Hoshikawa's terrain in `World.Height()`: gentle swells, a raised west terrace with an ishigaki wall, a flattened square, level pads under every house, a brook channel (`BrookX`) and a shrine mound.
- [x] Relayout the six buildings from one `World.Hamlet` table as angled Japanese archetypes (workshop, kura, irimoya hall, teahouse, engawa clinic, thatched farmhouse) with generated curved kawara/thatch roofs.
- [x] Add the brook water, arched footbridge, torii, stone steps, shrine, stone lanterns and minimap brook.
- [x] Replace sphere trees game-wide with Blender-generated keyaki, sugi, sakura, bamboo and shrub models, using Codex-generated foliage and bark textures and a wind-sway foliage shader; Komorebi's orchard keeps its persimmon trees.
- [x] Dress the village: square furniture, lamps along the curving road, gardens, household props, stepping stones and grass. The countryside's seeded scatter is unchanged, and this is pinned by a test.
- [x] 242 checks pass headless and in native Metal, including terrain continuity, brook bank slope and trunk-base checks added after the whole-branch review. The frame rate stays at the 60 Hz vsync cap (118–120 fps uncapped).
- [x] Fix the review's ten deferred minors. The brook edges are buried and the brook joins the river flush, no boulder sits in the brook, pots stand clear of the decks, and the shrine mound is at most 25°. Grass keeps off the footpaths, fence rails follow the ground, and the hall sign hangs under its porch roof. The z = 0 terrain step is gone, dialogue spots are settled under physics, and foliage back faces keep their normals. 269 checks pass headless and in native Metal.
- [x] Widen the river sheet to 8 m and give it a low village-side bank, so both of its edges end inside the ground and the brook runs straight on from it. 270 checks pass headless and in native Metal.
- [x] Seal the brook/river joint. Both sheets share `World.WaterLevel`, and the brook's mouth row reuses the river sheet's edge vertices, so the per-vertex wave matches across the joint. 271 checks pass headless and in native Metal.

## Performance and stability

- [x] Add `tools/render_cost.gd`, which reports draw calls, primitives and CPU render time at three village viewpoints. The hamlet rebuild's minors and the river widening cost nothing measurable.
- [x] Fix the intermittent audio leak at shutdown: `Game._ExitTree` stops the ambience and lets the audio thread release it before the AudioServer shuts down. The leak went from 6 in 20 quits to 0 in 40.
- [x] Bake World's 2,675 static boxes, spheres and cylinders into 233 meshes, one per material and 32 m cell, with `Mat()` sharing one material per look. Draw calls fall from 4666/3897/5389 to 1229/1154/1311, and captured frames match the unmerged ones. 272 checks pass headless and in native Metal.

## Scene 1 visual-novel prologue

- [x] Write the prologue as visual-novel scenes in `scripts/Prologue.cs`: Zero waking Mio, Ren's briefing, the relay restart, the homecoming at nightfall, and a first conversation with each Hoshikawa resident. Zero stays a voice on Mio's wrist terminal, curious like a child; the homecoming's change of subject is the first hint that it is hiding something about Mio's mother.
- [x] Replace the single dialogue box with a scene player: portraits, name plates, typed-out lines, narration, a night overlay, and Zero's link emblem. E, Enter or a click finish a line and then move on; Esc skips the rest of a scene.
- [x] Cut first portraits from the existing turnaround and reference sheets with `tools/cut_portrait.gd`; their front-on stance read as unnatural in dialogue.
- [x] Generate all eleven portraits with Codex as three-quarter dialogue poses from each character's reference art, and fit the scene layout to their head-to-thigh framing.
- [x] 282 checks pass headless; the graphical run used xvfb and OpenGL, not native Metal.

## Low-poly Mio

- [x] Save the user's low-poly Tripo Mio from the live DCC Bridge session without changing it.
- [x] Repaint the skin and white that Tripo's bake printed onto her bangs, side locks and the strands behind her ears, and flatten the bake's broken normals.
- [x] Bind the 47,380-triangle body to the dense import's 41-bone rig and weights, so all twelve clips and the sword carry over. Keep the previous matte material.
- [x] 333 checks pass headless and in native Metal; Mio's triangles drop from 150,600 to 47,980 and the GLB from 17.4 MB to 8.0 MB.
- [x] Replace the dense import's rig and clips with Tripo's 65-bone rig and twelve clips the user made in Tripo Studio, each cut and retimed to the game's timing. 333 checks pass headless and in native Metal.
- [x] Polish Mio after play-testing: an authored jump crouch and knee tuck, a 2 m/s walk, gait-matched walk and run playback, a full 0.6 s dodge roll, a combo that chains through one ready stance, a closed right-hand grip, and the bake's face strokes repainted. 339 checks pass headless and in native Metal.
- [x] Polish Mio again from the review image: refill the face's pale patches, smudges and brow strokes from the surrounding skin, seat the hilt across her closed fist, lower the ready stance's left hand to her waist, and shorten the dodge's time lying on her back. 385 checks pass headless and in native Metal.

## Mio's Japanese voice

- [x] Translate Mio's thirteen prologue lines into `assets/dialogue/ja.json`, keyed by scene and beat so they double as localization text.
- [x] Add `tools/build_voice.py`, which refuses to voice a line whose English changed in `Prologue.cs`, and the `voicing-dialogue` skill as the procedure.
- [x] Audition three Japanese ElevenLabs voices (Kurea, Haruno, Yuki) on five of Mio's lines.
- [x] The user picked Yuki for Mio.
- [x] Voice all of Mio's lines with Yuki into `assets/audio/voice/ja/Mio/`: thirteen mp3s, 1.1 MB.
- [x] Play each beat's voice line in dialogue, looked up by `Prologue.SceneNames`; stop it on the next beat, Esc or the end of the scene. 338 checks pass headless and in native Metal.

## Zero's Japanese voice

- [x] Translate Zero's eighteen prologue lines into `assets/dialogue/ja.json`: no first-person pronoun, counting and railway words.
- [x] Audition three Japanese ElevenLabs voices (Kei, Koume, Toru) on five of Zero's lines.
- [x] The user picked Koume. Voice all eighteen of Zero's lines into `assets/audio/voice/ja/Zero/` (2.4 MB); 339 checks pass headless and in native Metal.

## Automated tests and CI

- [x] Add gdUnit4Net unit tests (`tests/unit/`) on plain .NET: the road against the terrain shader, house pads and clearances, the brook bed, region order, Tripo resident assets, and the prologue's voice files, Japanese lines, portraits and night beat. 15 cases.
- [x] Vendor godot-e2e v0.1.1 (`addons/gdunit_e2e/`) and add gdUnit4Net e2e tests (`tests/e2e/`) against a headless game: title to intro, pause and resume, steering Mio, Ren's briefing. 4 cases.
- [x] Add `just unit`, `just e2e` and `just self-test`.
- [x] Add GitHub Actions: unit tests on every push and pull request; self-test and e2e on pull requests and by manual dispatch, headless with Godot 4.6.2 .NET.

## Environment revamp (星川村翻新計畫)

Bring the non-character environment up to the Tripo characters' detail: semi-realistic, painterly textures, normal lighting, no cel shading. Characters and the shared villager rig stay as they are.

- [x] Phase 0: fixed before shots (`tools/review_revamp.gd` close-ups plus the nine `review_hamlet.gd` views) and a `render_cost.gd` baseline.
- [x] Phase 1a: regenerate the foliage atlas with small leaves and blossoms; rebuild the trees with smaller, outward-facing cards, crown-ellipsoid normals and crown occlusion; fade edge-on cards; crowns no longer receive shadows.
- [x] Phase 1b: lawn and meadow grass as swaying, colour-varied blade tufts in culled 32 m chunks, keeping every scatter stream's draws.
- [x] Phase 1c: rice paddies with still water over wet soil and 374 upright rice clumps in place of 240 boxes.
- [x] Phase 1d: warm afternoon light with sky ambient, distance-only depth fog, glow and a contrast/saturation lift.
- [x] Phase 1e: camera-only roof colliders on layer 8, so the spring arm stays under the eaves. 345 checks pass headless and in native Metal.
- [x] Phase 2 pilot, Tripo: the village well, generated with the Tripo API (55 credits), imported static at 8,182 triangles with 2048² PBR maps, and given fitted collision and a camera-only roof.
- [x] Phase 2 pilot, Blender: `tools/build_village_props.py` builds the lamp, bench, barrel and bamboo fence on the existing atlases. `World.Prop()` swaps the box-built versions, keeping positions, node names and colliders, and `Merge()` bakes them. 348 checks pass headless and in native Metal.
- [x] Phase 2b: brighten the Tripo well (albedo tint) and the cedar props (atlas tint, no new material).
- [x] Phase 2b, Tripo: the stone lantern (all four), the handcart with its rice bales and the notice board (150 credits), each under 8,200 triangles with 2048² PBR maps and fitted colliders. 354 checks pass headless and in native Metal.
- [ ] Phase 2, remaining box-built props (plant pots; Komorebi's benches are bench props since the Komorebi revamp).
- [x] Phase 3 pilot, Tripo: The Copper Kettle teahouse (Codex reference, one 40-credit image-to-model), 16,282 triangles with 2048² PBR maps, on its table pad with the shop sign, fitted wall colliders and its own mesh as the camera-only eaves. The other five stay on `Minka()` for comparison. 355 checks pass headless and in native Metal.
- [x] Phase 3 pilot fix: the teahouse scaled to 7.2 m so its eaves are 4 m up and the spring arm under them frames Mio as the Minka eaves did; the shop sign and the chochin hang on cords from the eave.
- [x] Phase 3: the other five Hoshikawa buildings from Tripo (200 credits): workshop, kura, hall, clinic and thatched farmhouse, 15,764–16,866 triangles each with 2048² PBR maps. One `Minka()` places all six from `TripoHouses`, with a walls box, a low trimesh for porches, decks and steps, camera-only eaves and the shop sign on cords. 360 checks pass headless and in native Metal.
- [x] Phase 4a, water and fields (no Tripo credits): a depth-aware `water.gdshader` for the river, brook and Cedar River (shallow olive to deep blue-green, shore fade and foam, flowing ripples, sky fresnel, no stripes); wet earth and gravel banks in the terrain shader; reeds along both banks, pebbles and half-sunk rocks in the brook; four terraced paddies levelled in `Height()` with walkable aze levees, still water (`paddy.gdshader`), jittered rice hills in two tones, an irrigation channel and sluice board, and a hasa rack; weathered-wood field fences; cabbage and daikon in Komorebi's beds. 368 checks pass headless and in native Metal.
- [x] Phase 4b, Tripo: a water wheel turning with the brook beside the sluice, on crossed timber legs with a trough to the channel, and a scarecrow in paddy 1 (80 credits), each under 8,500 triangles with 2048² PBR maps and fitted colliders. 375 checks pass headless and in native Metal.
- [x] Phase 4c, bridges and banks (no Tripo credits): both river bridges rebuilt as weathered timber, a new `plank` prop deck on log stringers and pier caps over stone piers, `field_post` posts and top and knee rails, the colliders unchanged. The stone bridge's earth causeway is opened between stone abutments, so the river runs under it. Reeds, pebbles and half-sunk rocks line the Cedar River's wet reach, and pebbles and rocks the river's banks. 379 checks pass headless and in native Metal.
- [x] Phase 4d, Tripo: a flat-bottomed river boat bobbing at its mooring off the village bank, tied by rope to a post, and stone washing steps cut into the bank down to a slab at the water, walkable both ways (80 credits), each under 9,000 triangles with 2048² PBR maps and fitted colliders. 385 checks pass headless and in native Metal.
- [x] Komorebi revamp (no Tripo credits). Its five buildings reuse Hoshikawa's Tripo houses through `Minka()` from `World.KomorebiHouses`, facing the road, with the meeting hall at its north end. The square's basin and box benches become the Tripo well, a notice board and bench props, and stone lanterns stand at the hall. A new Blender `kaki` persimmon tree bears fruit in the orchard. A handcart, barrels and a hasa rack make the packing station, with hoshigaki under the store's eave, and a scarecrow stands in the cabbage beds. The legacy `House()` and `OrchardTree()` are gone. 500 checks pass headless in each language; the last native Metal run (462 checks) was before the rebase onto the language PR and has not been repeated.
- [x] Old-model cleanup (no credits), keeping `villager.glb` and `patrol.glb` on purpose. Removed 28 textures Godot had extracted from old GLB exports, every authored-Mio texture and its build and QA scripts (`mio_illustrated.py`, `mio_turnaround.py`, the `check_mio_*` face checks, `render_human_review.py`) and the earlier dense Tripo Mio's textures. `refine_humans.py` now builds only the villager, triangle-for-triangle the same as before. 500 checks pass headless in each language; the last native Metal run (462 checks) was before the rebase onto the language PR and has not been repeated.

## Map redesign (地圖重新設計)

Plan: the design doc in the project's Claude Docs (GoW / Dark Souls-style layout, one PR per phase, art and sound as separate tasks).

- [x] Phase 1, terrain foundation, natural boundary and water (layout unchanged, no credits). The four box walls are gone: beyond the play area `World.Outside()` raises 8–12 m cliffs into the mountains on the north, east and north-west, and lays a wandering beach down into the sea on the south-west, behind the shrine and the west terrace, broken by four rocky capes. The river now rises high on the eastern range, runs down its slope and over two falls of about 6 m into its gorge, and flows west to its mouth in the sea, at sea level across the valley. The Cedar River rises further north, falls 9 m and 5 m into Cedar Pass, runs across it in a slot cut through the hill that used to bury it, and leaves west through a canyon, closed to walkers by a boulder choke, down its own valley to its mouth on the bay. Beyond the edge the ground eases to a level upland under the cliffs, so the valley's channels do not run on as trenches. `World.Rivers` holds each course with its water level, never rising downstream; the sea and gorge water are laid on the terrain's own grid so every shore is buried. The only invisible walls are off the beach and behind the choke. The 1 m terrain now covers a 40 m margin in 64 m chunks, with 4 m mountains beyond. The moored boat turned its bow to the new upstream, and the machine city moved behind the eastern ridge. `WildsTest` checks that water runs downhill, every river bed is under its water, and walking from the square cannot leave the valley. After the second Metal review the headwaters bend with the range, wander in width and open into mountain valleys whose walls carry no creases at bends or falls; each spring's ravine starts as a fold in the hillside; steep reaches break into white water; the 4 m terrain is stitched to the 1 m edge; and the far sea reaches past any view. After the third, a reach's water fills only ground joined to its course, and the wet band at every waterline is painted per pixel from the ground's height above the water, so it no longer zigzags across the terrain's triangles. After the fourth, water is joined to its course a metre cell at a time even under the 4 m ground, the band is painted only where water lies (and stays narrow on the 4 m ground), and the white water's streaks run along each reach.
- [ ] Art A1: a cliff rock texture (Codex) in place of the atlas stone on the cliffs and gorges.
- [x] Phase 2, the relay road enlarged with a branch and a shortcut (placeholders, no credits). The relay road now runs 164 m from the river to the viaduct, and Cedar Pass, the Cedar River and its valley, and Komorebi move 100 m north. Its ground is a block of mountain cut by a table of trails (`World.Trails`), each a level floor that meets the next at a cliff or leaves a ridge: the junction just over the bridge, the east switchback up to a ruined teahouse on the cliff above it, the high road, the relay plaza under the broken viaduct, and a forest valley branching west from the junction to a dead end at an old quarry. The three patrols guard the junction, the teahouse terrace and the plaza; four smaller, weaker scouts on `patrol.glb` watch the road and the valley and don't count toward the mission. The quarry's remedy pouch raises the remedies Mio carries from three to four. The teahouse ladder is the first shortcut, lowered only from the terrace to link it to the junction; it stays down through a recovery and is drawn up by a restart. The road's paint now comes from C# (`World.RoadGap()` in the terrain's UV2.y), so the terrain shader no longer copies the route. `WildsTest` floods the road and checks that she keeps to its trails, that the terrace is reached only by the switchback or the ladder, and that the quarry is a dead end. 520 checks pass headless in each language. The first Metal review passed, and its five findings are fixed: the drawn-up ladder now lies back on a trestle, high edges no longer dip into trenches before the cliff, the forest valley's bends leave no step across the mountain, and a quarry block and a cedar no longer stand in the walls. A second Metal review confirmed those fixes; the forest valley's cedars now line its path.
- [ ] Phase 3: Hoshikawa expanded: Mio's house, the old Hoshikawa halt at the east gate, the shrine hill's sea-cliff lookout.
- [ ] Phase 4: the Cedar gorge and Komorebi's bowl.

## Languages: Japanese and Traditional Chinese

- [x] Phase 0: `assets/dialogue/glossary.md`, with the names, places, terms and tone for all three languages; kanji names for Chinese settled as proposals for the owner to confirm.
- [x] Phase 1: `scripts/Localization.cs` and the language setting (`--lang=`, `user://settings.cfg`, operating system, English in the self-test); every HUD string, toast, sign, name label, role and dialogue line goes through a translation table; the Hud wraps with `TextParagraph`; language buttons on the title screen and pause menu; Noto CJK subsets from `tools/build_fonts.py`.
- [x] Phase 2: the Japanese text (39 new lines plus the 31 voiced ones, 121 interface strings).
- [x] Phase 3: the Traditional Chinese text (70 lines, 121 interface strings).
- [x] Self-test in all three languages, and unit tests that each translation covers every line and string, keeps its placeholders and has no dead entries.
- [ ] A native reader checks the Japanese and Traditional Chinese; the owner confirms the Chinese names and 天狼 / シリウス in the glossary.
- [ ] Voice Zero's six new relay reminders in Japanese.
- [ ] Export presets, once they exist, need `*.json` in the resource filter so `assets/dialogue/` ships.
