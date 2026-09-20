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
