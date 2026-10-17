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
