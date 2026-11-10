# Hoshikawa — The Offline Valley

Playable Godot C# countryside slice of the user's dual-protagonist JRPG concept.

## Story baseline
Based on the latest revision of “Story Main line” (6a75647c-8d84-83e8-b467-45bf0fb6b41e): 22nd-century Japan; humans live in rural offline settlements while AI/robots inhabit cities and imprison humans. Mio is a 22-year-old scavenger/repairer. Zero has his own mechanical body in the city: a caring robot, not the railway's AI, whom the city's master AI lets know only what his work needs; their voluntary sensory link does not transfer bodily control. This slice follows Mio. Zero contributes remotely, not as a village resident.

## Work and acceptance
- [x] Generate original painted terrain/material atlas and Mio key art with imagegen; retain prompts/provenance.
- [x] Model Mio, a villager, and a patrol robot using Blender MCP; export animated GLBs and editable Blender source.
- [x] Build six distinct buildings, six named talkable NPCs, rolling countryside, bridge, farmland, forest, and a relay landmark.
- [x] Implement camera-relative movement/run/jump/dodge, sword combo, timed deflect/guard, posture breaks and execution, one cooldown skill, three patrol enemies, health/recovery.
- [x] Add a complete objective: meet Ren, clear patrols, activate manual relay, return to Ren. Add title, pause, controls, dialogue, combat feedback, ambient audio, and restart.
- [x] Build with installed Godot Mono/.NET; run a deterministic integration self-check; inspect actual rendered gameplay and fix issues.

## Scope
Single outdoor map and one interior (Mio's room, where the prologue opens), one playable human character, one remote AI partner, no city chapter. Original stylized anime-inspired characters and warm painterly landscapes; deliberate MVP geometry and animation, not franchise assets.

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
- [x] Fast-forward and history: holding Ctrl runs a scene on a line every 0.08 s with no voice; L or the wheel opens a lacquered history of every line heard (`Game.Log`, kept in English and translated as drawn), over the scene, which stops typing and pauses its voice until it closes. The dialogue box shows the four keys as keycaps. In the history, E (the bottom line) or a click replays a voiced line on its own player; closing stops it. Gamepad bindings wait for the gamepad pass (`skip` is an input action so it only needs an event). Not yet seen under Metal.

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
- [x] Tripo batch 1, the relay road (250 credits): a boulder for the rockfall and the cliff-foot scatter (40 in all, each on the lowest ground under it with no air beneath), the relay cabinet on its plinth with the Jade terminal behind its window, the viaduct's piers and deck spans, and three barricades across the station road. 3,881–7,710 triangles each, with 1024² (boulder, barricade) or 2048² PBR maps and fitted colliders. 521 checks pass headless and in native Metal.
- [x] Tripo batch 2, Cedar Pass and the roadside (140 credits, Codex references free): the Cedar Pass shelter and the valley overlook (detailed textures, 2048² maps), the signpost on all five wayfinders and as the overlook's plaque, and the teahouse ladder's mesh (standard textures, 1024² maps), 4,392–7,998 triangles each. The shelter and overlook keep Mio's walk-on surfaces 3 cm proud with fitted box colliders; the ladder keeps its walk box, hinge and swing. Free reuse in the same areas: the batch 1 boulder for the Cedar Pass and canyon-choke boulders, the handcart as the quarry sled, and the remedy pouch on a barrel. 521 checks pass headless in all three languages and in native Metal.
- [x] Tripo batches 3-4 from the owner's own Tripo Studio models (no API credits, Codex references): the shrine hall and torii, one timber bridge for both rivers, the ishigaki terrace wall, Komorebi's festival masts, street lamps, laundry, woodpile, the teahouse's chochin and tea bench, tomato plants, benches and barrels; the Blender lamp, bench, barrel and bridge plank props and the shrine's roof generator are gone.
- [ ] The brook footbridge and the clinic's herb rack (not yet sent over DCC Bridge).
- [x] Texture compression (no credits): every model texture imports VRAM Compressed (S3TC, BC5 normal maps) with mipmaps, through `tools/compress_textures.py`, which `import_character.py` now runs between two Godot imports. Video memory in the village square fell from 2.9 GB to 854 MB with Tripo batches 1 and 2 in place (2.5 GB to 779 MB before them); draws and primitives are unchanged.
- [ ] Halve the eleven 4096² character maps to 2048², saving another 118 MB.

## Combat: robots are shut down from behind

- [x] Robots have 500 health (scouts 300) and Mio's sword only scratches one from the front (2 health, 5 posture, no stagger); attacking within 60° of its back shuts it down outright. After a swing a robot neither moves nor turns while it recovers, the opening to get round it.
- [x] Robot strikes are heavy: 50 vitality, 75 for the perilous sweep, scouts 60%.
- [x] Resonance Cut no longer damages: it stuns robots in reach for 4 s and has 3 charges (`Player.MaxSkillCharges`) instead of a cooldown, refilled by resting with Aki or Emi or recovering at the village (and, since the items, one at a time by a resonance cell). The HUD shows the charges, and the robot panel says when Mio is behind one or it is stunned.
- [x] Deflect, posture break and the frontal finisher are unchanged.

## Combat: Mio's combo and Resonance Cut animations

- [x] The combo and Resonance Cut keyed in Blender by `tools/mio_combat.py` (IK on the Tripo rig) in place of Tripo's clips: a diagonal cut, a rising backhand and a leaping overhead slam that each start from the last one's link pose, and Resonance Cut as a crouch, a leap spinning twice and a slam.
- [x] `Player.Swings`: per-swing hit, link, end and lunge; a queued press chains at the link; each swing aims at the locked or nearest robot and lunges no closer than 1.3 m.
- [x] A sword trail, hit-stop on a landed blow, a ground ring under the third swing, Resonance Cut's shockwave ring from the slam, and a camera punch.
- [x] Effects (`scripts/Effects.cs`, `glow.gdshader`, `sword_trail.gdshader`): a glowing trail with a white-hot edge and no corners in the spin, gold on the third swing, the whole blade through Resonance Cut's spin; a landed blow flashes, throws sparks past the robot, cuts a streak across it along the blade's path and flares its plating; the third swing throws dust; Resonance Cut gathers motes as she crouches, and its slam sends two rings, a column of light, dust, sparks and a jade tint across the screen; a stunned robot crackles with jade sparks.
- [x] Combat sounds from ElevenLabs (the owner asked for them with Mio's voice): new swing, impact, guard and deflect, a slam for Resonance Cut and a short-circuit for the robots it stuns; Mio shouts on every swing and the finisher (at first only half the first two swings shouted; the owner missed them in the recording), calls 共鳴斬 as she casts and cries out when hit, in her cast voice Yuki, through one player so a chained swing cuts her last shout off. `build_audio.py` no longer writes the four cues it replaced.

## Music

- [x] Background music by area and danger (`scripts/Music.cs`): the valley's loop in the villages and Cedar Pass, the relay road's own track there, and a battle track while any robot is out of its patrol and for 3 s after; 1.5 s crossfades, and a faded track pauses where it is. The relay and battle tracks started as synthesized placeholders from `tools/build_audio.py`.
- [x] The relay road and battle tracks from ElevenLabs Music in Falcom style (the owner's pick): `relay.ogg` and `battle.ogg`, each one loop with no intro (the owner's rule: fights start at once, and the files stay small), replacing the placeholders.

## Map redesign (地圖重新設計)

Plan: the design doc in the project's Claude Docs (GoW / Dark Souls-style layout, one PR per phase, art and sound as separate tasks).

- [x] Phase 1, terrain foundation, natural boundary and water (layout unchanged, no credits). The four box walls are gone: beyond the play area `World.Outside()` raises 8–12 m cliffs into the mountains on the north, east and north-west, and lays a wandering beach down into the sea on the south-west, behind the shrine and the west terrace, broken by four rocky capes. The river now rises high on the eastern range, runs down its slope and over two falls of about 6 m into its gorge, and flows west to its mouth in the sea, at sea level across the valley. The Cedar River rises further north, falls 9 m and 5 m into Cedar Pass, runs across it in a slot cut through the hill that used to bury it, and leaves west through a canyon, closed to walkers by a boulder choke, down its own valley to its mouth on the bay. Beyond the edge the ground eases to a level upland under the cliffs, so the valley's channels do not run on as trenches. `World.Rivers` holds each course with its water level, never rising downstream; the sea and gorge water are laid on the terrain's own grid so every shore is buried. The only invisible walls are off the beach and behind the choke. The 1 m terrain now covers a 40 m margin in 64 m chunks, with 4 m mountains beyond. The moored boat turned its bow to the new upstream, and the machine city moved behind the eastern ridge. `WildsTest` checks that water runs downhill, every river bed is under its water, and walking from the square cannot leave the valley. After the second Metal review the headwaters bend with the range, wander in width and open into mountain valleys whose walls carry no creases at bends or falls; each spring's ravine starts as a fold in the hillside; steep reaches break into white water; the 4 m terrain is stitched to the 1 m edge; and the far sea reaches past any view. After the third, a reach's water fills only ground joined to its course, and the wet band at every waterline is painted per pixel from the ground's height above the water, so it no longer zigzags across the terrain's triangles. After the fourth, water is joined to its course a metre cell at a time even under the 4 m ground, the band is painted only where water lies (and stays narrow on the 4 m ground), and the white water's streaks run along each reach.
- [ ] Art A1: a cliff rock texture (Codex) in place of the atlas stone on the cliffs and gorges.
- [x] Phase 2, the relay road enlarged with a branch and a shortcut (placeholders, no credits). The relay road now runs 164 m from the river to the viaduct, and Cedar Pass, the Cedar River and its valley, and Komorebi move 100 m north. Its ground is a block of mountain cut by a table of trails (`World.Trails`), each a level floor that meets the next at a cliff or leaves a ridge: the junction just over the bridge, the east switchback up to a ruined teahouse on the cliff above it, the high road, the relay plaza under the broken viaduct, and a forest valley branching west from the junction to a dead end at an old quarry. The three patrols guard the junction, the teahouse terrace and the plaza; four smaller, weaker scouts on `patrol.glb` watch the road and the valley and don't count toward the mission. The quarry's remedy pouch raises the remedies Mio carries from three to four. The teahouse ladder is the first shortcut, lowered only from the terrace to link it to the junction; it stays down through a recovery and is drawn up by a restart. The road's paint now comes from C# (`World.RoadGap()` in the terrain's UV2.y), so the terrain shader no longer copies the route. `WildsTest` floods the road and checks that she keeps to its trails, that the terrace is reached only by the switchback or the ladder, and that the quarry is a dead end. 521 checks pass headless in each language. The first Metal review passed, and its five findings are fixed: the drawn-up ladder now lies back on a trestle, high edges no longer dip into trenches before the cliff, the forest valley's bends leave no step across the mountain, and a quarry block and a cedar no longer stand in the walls. A second Metal review confirmed those fixes; the forest valley's cedars now line its path. The relay road extension toward the station leaves from the relay plaza, not Hoshikawa: a station road runs east from the plaza and ends at a barrier, a sign and a rockfall.
- [ ] Phase 3: Hoshikawa expanded: Mio's house, the old Hoshikawa halt at the east gate, the shrine hill's sea-cliff lookout.
- [ ] Phase 4: the Cedar gorge and Komorebi's bowl.

## UI redesign: less text, more art

Plan: the proposal doc in the project's Claude Docs (audit, per-screen keep/remove/icon table, new HUD wireframe); art and code are separate tasks.

- [x] Art: thirteen solid kamon-style HUD icons and the 星川 title logo, drawn by Codex and cut by `tools/cut_ui_art.gd` into white images the HUD tints (`assets/art/ui/`).
- [x] Code: the HUD, combat markers, toasts, title, pause, defeated and dialogue screens use the icons and keycaps instead of text; the always-on controls panel moves to the pause screen; the translation tables go from 134 interface strings to 80. 534 checks pass headless and in native Metal in all three languages.
- [x] Dialogue box and Starwell screen as lacquer and stardust (the owner's pick of three mocked directions): a chamfered gold-rimmed panel with a hairline and corner diamonds; the speaker's face on a crest before a gold ribbon with their name and role, stardust drifting off its end; the Starwell as a card per stat and per priced item, a vignette behind it. No new art; every string unchanged.
- [x] Owner found it still plain: Codex art on her Mac (the Starwell painting, an urushi lacquer surface, a gold maki-e corner, a gold crest ring, a divider, sixteen icons). The dialogue box is lacquer with two maki-e corners and the face in the gold ring, the role an icon on the ribbon; the Starwell screen is its painting with the cards over its darkened right side, the stats and Mio's level as icons, Raise and Buy as icon buttons. Dropped the Level, Trade, Raise, Buy and long stat-name strings. The crest no longer covers the dialogue's first line.
- [ ] Metal check of both screens on the Mac.

## Items

- [x] An item table (`scripts/Items.cs`) and `Inventory`; remedies and the remedy pouch move into it, joined by the wrist terminal (key item) and two resonance cells on the relay road (each restores one Resonance Cut charge, not refilled by resting). An items screen opens with I or from the pause menu: supplies and key items in slots, the selected one's icon, count and text, E or Use to spend a supply. Icons reuse the HUD set.
- [ ] Art: item icons of their own (Codex), in place of the remedy, resonance and Zero icons the items borrow.

## Settings

- [x] A settings screen (`GameMode.Settings`, from the title screen and the pause menu) with a Voice and a Music slider (`scripts/Volume.cs`): ten steps each, arrow keys, d-pad or mouse, saved in `user://settings.cfg` beside the language. Dialogue voice lines and Mio's shouts play on a Voice bus, the music on a Music bus, the sound effects stay on Master; the Voice slider plays one of Mio's shouts so its level can be judged.
- [ ] Art: icons for the two sliders in place of the words Voice and Music (Codex).
- [ ] A third slider for the sound effects, if the owner asks for one.

## Stardust, levels and the save point

- [x] One currency, stardust, from robots shut down, kept through a defeat. The Starwell (Hoshikawa's well) is the one save point: rest, raise HP / SP / ATK / DEF a point each for a rising cost, and buy resonance cells. Running spends SP. All numbers in `scripts/Stats.cs`.
- [ ] Art: a stardust icon and the save point's own glow or model (Codex); the HUD draws a star in code for now.
- [ ] Sound: a stardust pickup and a level-up cue (ElevenLabs); both borrow `heal` for now.
- [ ] More stats, more shop items and more save points as the map grows; each is a table row.

## Drones

- [x] A third kind of robot to level on: five small flying drones (`Enemy.Drone`, a `RobotKind` row beside the patrol and the scout) over the relay road's switchback, forest valley and high road. Its own model (below); 6 health, so a full combo shuts one down from the front, each hit knocks it off its swing, strikes at 30% of a patrol's and never perilous; 25 stardust; back after a rest like the scouts.
- [x] Art: a drone model of its own: the owner's Tripo Studio model from Codex four-views (`drone.glb`, static), its halo split off as a `Rotor` that spins in flight; struck it tips back, shut down it drops onto its prong.
- [x] A new robot for the patrols and scouts: the owner's rigged Tripo Studio model from Codex four-views (`patrol.glb`), its seven clips keyed by `tools/patrol_clips.py`; the authored Blender robot and `tools/build_characters.py` are gone.
- [ ] Sound: a rotor hum (ElevenLabs).
- [ ] Number tuning after hands-on play.

## Languages: Japanese and Traditional Chinese

- [x] Phase 0: `assets/dialogue/glossary.md`, with the names, places, terms and tone for all three languages; kanji names for Chinese settled as proposals for the owner to confirm.
- [x] Phase 1: `scripts/Localization.cs` and the language setting (`--lang=`, `user://settings.cfg`, operating system, English in the self-test); every HUD string, toast, sign, name label, role and dialogue line goes through a translation table; the Hud wraps with `TextParagraph`; language buttons on the title screen and pause menu; Noto CJK subsets from `tools/build_fonts.py`.
- [x] Phase 2: the Japanese text (39 new lines plus the 31 voiced ones, 121 interface strings).
- [x] Phase 3: the Traditional Chinese text (70 lines, 121 interface strings).
- [x] Self-test in all three languages, and unit tests that each translation covers every line and string, keeps its placeholders and has no dead entries.
- [ ] A native reader checks the Japanese and Traditional Chinese; the owner confirms the Chinese names and 天狼 / シリウス in the glossary.
- [ ] Voice Zero's six new relay reminders in Japanese.
- [ ] Export presets, once they exist, need `*.json` in the resource filter so `assets/dialogue/` ships.

## Before setting out: the second and third day (出發之前)

- [x] Plan (Notion, 序章後半：出發之前): platform two's departure signal answers only to the station's signal box lever, so someone must go in; Renji holds the relay plaza; Sota and Yui go with Mio; three days, two day changes, all on the existing maps.
- [x] Twelve new scenes in `Prologue.cs` (the second morning, Renji's news, Sota joining, the clinic and Yui's crossbow, KOMA-7 and Grandpa Tetsu's unseen voice, the telephone, Chiyo's lantern, the evening, the second night, the third morning, the rockfall) and three repeats, in Japanese and Traditional Chinese; `RenjiRest` and the homecoming's "prologue complete" card are gone.
- [x] Quest stages after the relay (`Quest`): Renji in the morning, four errands in any order counted on the objective and marked nearest first, the telephone to Renji, home to sleep, the rockfall, the station road.
- [x] Day changes: a title card over black (`Prologue.Cards`) opens each morning in Mio's room; her door takes her home from the second day; night tints the evening and the second night (`Prologue.Nightfall`).
- [x] Residents move with the story (`Game.Spot`): Renji and KOMA-7 up to the rockfall on the second evening, Sota and Yui on the third morning, KOMA-7 home once it has cleared the road (`World.ClearRockfall`, under a moment's black until KOMA-7 has a lifting clip).
- [x] A second Starwell at Komorebi's well (`World.SavePoints`).
- [x] Self-test (737 checks) in all three languages, unit tests.
- [x] Everyone knows the name Zero, as Mio's terminal's name, and Mio calls it Zero anywhere: the villagers say "ask Zero" and Mio asks "Zero, ...". The name test is gone.
- [x] Zero answers only Mio's questions, and the villagers know it: Yui, Renji, Chiaki, KOMA-7 and Sota ask Mio, and she asks the terminal (`PrologueTest.ZeroAnswersOnlyMio`). 13 cast lines changed or added, the 7 recorded ones' takes removed for re-voicing and the unchanged takes after them renamed to their new keys.
- [x] Second script review: platform two's departure signal (not the home signal) is the one only the station's lever turns, the old freight system logs each load by its category code, and the way home is an old track trolley while the crawlers keep off the rails.
- [x] A read for natural lines: Mio no longer reminds Zero of the rule on her way out (`Intro.17`, so Zero's `Intro.18` opens "Wait, Mio." and waits for a new take), she asks "how are the signals" rather than "report" (`HalfDone.1`), `Evening.5` reads straight, the narration says Mio, not "you", and the Chinese loses its stiffest wording. Two lines the last change cut short in Japanese and Chinese (`Briefing.7`, `Homecoming.0`) are whole again.
- [x] A second read, for logic as well: Renji says what kept him up before he asks about the station's signals (`HalfDone.0`) and sends Mio to KOMA about the rockfall, which only his reminder named (`HalfDone.11`); Yui says she is coming when she hands over her crossbow (`Crossbow.5`); Mio answers Renji at the rockfall, where she said nothing (a new beat, `Rockfall.8`); and she no longer says "Renji has to hear this" twice in one morning (`Gate.12`, English and Chinese only).
- [x] A third read, of the Chinese alone: a bare 線 no longer stands for the railway, a wire and "online" by turns (`Evening.4`, `Evening.5`, `HalfDone.10` and others say 鐵路, 鐵軌 or 電線), and twenty-odd lines lose a calque. No Japanese or English changed, so no take is affected.
- [ ] Voice the 66 unrecorded cast lines (`Prologue.Unrecorded`) once the owner has read them.
- [ ] Separate tasks: KOMA-7 lifting rock, Yui's crossbow, the city skyline on the north-west ridge, the relay road's extension (next map).

## A longer opening (開場劇情)

- [x] Rename the cast to the 2026-10-23 name table (Renji, Chiaki, Haruka, Sota, Naomi, Emiko; Mio's family name Minase) in code, both translations, the glossary, voice files and portraits.
- [x] `Opening` narration over the city and the valley, then the `Intro` in Mio's room: Zero wakes her, the smell of the paddies, the freight train with medicine that will not stop at Hoshikawa Station.
- [x] Mio's room as its own small map (`World.Room()`), with her mother's photograph and a door that loads the valley at the new Minase House.
- [x] Dress the room in Codex art: tiling tatami, plaster, cedar, washi and quilt textures, Shio's photograph and the painted view through the shoji.
- [x] Realistic bedding from Blender (`tools/build_bedding.py`): a quilted cover draped as cloth over her sleeping pose, pushed into her lap as she sits up and folded at the futon's foot when she rises; a rounded, tufted futon and a domed buckwheat pillow.
- [x] Tripo Studio models for the opened line telephone, the tools on the floor, the wire spool and screw tin, and the desk lamp (the owner made them), imported static.
- [x] Calm the staged camera: a scene cuts only where it starts and moves between its few held shots, with a self-test measuring every frame's step and turn.
- [x] End the waking scene on Mio deciding to take the news to Renji (Zero minds her missed meal), and swing the camera round her to the follow camera, facing the door, instead of cutting.
- [x] Zero is a caring robot in the city that knows only what its work needs: it packed the medicine onto the train, rather than reading a timetable.
- [x] A garden under the engawa: the house on posts and stones, lawn, a step and stepping stones, an eave, hedged sides and a terrace edge over the painted paddies.
- [x] Yui, now happy, energetic and talkative, waits at the gate with Emiko's letter (`Gate`), races Mio to the workshop, joins Renji's briefing and walks on to the clinic; she lives in Hoshikawa now, not Komorebi.
- [x] Staged scenes: a scene camera per beat and the characters acting each line (`Prologue.Stage`, `scripts/Cutscene.cs`).
- [x] Import Hapadona's Tripo Studio Yui and Renji; key the opening's clips in Blender (`tools/cast_clips.py`): Mio's sleep, wake, sit, terminal and rise, rebuilt into `mio.glb` on the Mac; Yui's and Renji's idle, walk and gestures.
- [x] Self-test (671 checks) in English, Japanese and Traditional Chinese, unit tests and e2e.
- [x] Voice the 31 new or changed Mio and Zero lines in Japanese (ElevenLabs, Yuki and Koume, one take each); `Prologue.Unrecorded` is empty again.
- [x] Give Yui and Renji Japanese voices: auditions, the owner's picks (kano for Yui, her own Sho for Renji), all 27 lines voiced and `Prologue.Cast` naming the voiced speakers.
- [x] Keep Zero a secret: the villagers know the voice on Mio's wrist, but she passes the terminal off as an old line terminal and tells nobody, Renji included, that it is a robot in the city (the gate, the briefing, Renji's rest line, the homecoming, Chiyo, KOMA-7 and Sota rewritten); Sota, the AI's spy, gets a first hint: he asks who is on the other end and wants to know first when the train is due, and at the homecoming one signal by the village goes dark for a moment.
- [x] Zero never speaks first when anyone but Mio is there: Mio calls him an AI terminal that only answers queries (she reminds him on the way out of her room), so Yui, Renji, KOMA-7 and Sota ask and he answers in polite terminal Japanese. The train carries food as well as medicine and runs through Hoshikawa Station on platform two; Renji decides to take only the medicine and leave the food for people who need it more.
- [x] Voice the 27 rewritten cast lines (9 Mio, 8 Zero, 5 Yui, 5 Renji).
- [x] Consistency pass: Renji no longer knows the terminal reaches the city, the clinic has a week of medicine everywhere, KOMA-7 and Mio talk like old neighbours, Zero's city platform is the loading platform.
- [x] Second pass: the patrol count is state-neutral in the briefing and reminder, Chiaki doesn't assume the train, Yui doesn't claim the race, Zero names the city's freight terminal.
- [x] Voice the eight lines those passes changed; `Prologue.Unrecorded` is empty.
- [x] Nobody but Mio says Zero's name (Chiyo knows it too, and Mio and Zero don't know she does): Yui calls it the terminal at the gate and the clinic, Mio stops naming it in front of KOMA-7, and `PrologueTest` checks all three languages. Renji tells Mio that he and Sota will keep watch over the village, since they fight as well as she does.
- [x] Voice the five lines that changed (`Gate.3`, `Gate.5`, `Briefing.13`, `FirstMeeting.KOMA-7.4`, `FirstMeeting.Yui.2`); `Prologue.Unrecorded` is empty.
- [x] Repaint Mio's and Yui's faces where Tripo's bake smeared hair onto skin and skin onto hair (`tools/face_paint.py`): clean brows, eyelids, forehead and temples, hair-coloured bangs and side locks, Yui's forehead under her bangs in the hair's colour, the eyes, nose and mouth kept, the brows redrawn; Yui's importer repaints her source map, Mio's importer packs the committed map.
- [x] Dialogue acting that doesn't look like a puppet (`tools/cast_clips.py`): keys flow through a spline, each joint trails its parent (chest, head, wrist, fingers), gesturing hands placed by two-bone IK (Mio's terminal raised across her chest with the elbow in, Yui's open-handed wave from the elbow, a palm-up give, an index point), fingers spread from index to pinky, and every held gesture breathing in a `_hold` loop that `ActorVisual` queues; Renji's shared gestures come along. Swapped into the committed GLBs with `cast_clips.py --swap` and `tools/glb_swap.py`.
- [x] Posture review: Yui and Renji stand at ease instead of in the rest pose (weight on the left leg, its hip out and the hips riding as high as it reaches, the right knee eased forward, feet closer and turned out, arms close), and their gestures grow out of that stance with the feet planted; Mio talks in `stand`, her idle's own feet with the sword lowered, which her terminal and rise share, so cues no longer slide her feet or swing her blade; feet are planted by leg IK in `tools/cast_clips.py`.
- [x] Hands: fingers curl toward the palm about an axis read off the rest pose (the old axis vanished for a hanging finger and swung big curls sideways into claws), the thumbs tuck per gesture instead of staying splayed, the curls retuned, Mio's sword wrist straightened; Mio's thighs and coat freed from her pinkies, which Tripo's skinning had tied them to (`tools/hand_weights.py`); the dark fingertips and smears on both characters' hands repainted.
- [x] Hands out of the body (Godot skins without self-collision): `tools/self_contact.py` measures each hand and forearm inside the rest of the body and turns the upper arm out, run on every cast clip and on Mio's Tripo idle, walk, guard and jump; Mio's forearm no longer goes through her satchel, Yui's arms hang clear of her hip, her folded forearms stack instead of crossing through each other, and she reads with her hands apart.
- [x] The rest of the cast's self-contact: Mio's sword kept out of her legs (her run, stagger and lying death turn the wrist instead of passing the blade through a thigh), her eye rub's fist at her eye rather than in her cheek, her seated terminal raised over her thigh, the patrol robot's arm armour out of its thighs in its run, stagger and death, and every turn eased over neighbouring frames so none flicks.
- [x] Mio's combat clips kept out of her body: her left hand eases onto the hilt as a mirrored fist instead of jumping there open (in Resonance Cut it no longer swings through her chest and right arm mid-spin), the overhead windups raise the sword before her face rather than through her hair, and the stance's left hand is off her thigh.
- [ ] Listen to the new takes (Mio's, Zero's, Yui's and Renji's) in the game and redo any that read wrong.
- [ ] New dialogue portraits for Yui and Renji from their Tripo models, the photograph's image, door and photograph icons (Codex).
- [ ] Graphical check under Metal on the Mac.
