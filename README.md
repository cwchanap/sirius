# Hoshikawa — The Offline Valley

A playable, original Godot 4.6.2 **C# / .NET 8** action JRPG prologue. Mio explores a rural human settlement in 22nd-century Japan; Zero remains in the occupied machine city and speaks over their sensory link.

## Play

Open `project.godot` in **Godot Mono** and press **F6** on `scenes/Main.tscn`, or **F5** to run the project.

On this Mac:

```sh
sh run.sh
```

`run.sh` builds C#, imports the included assets, and launches the game. Set `GODOT_MONO` to your Mono engine executable on another machine. The project defaults to the Mobile renderer with native Metal on macOS. For older graphics hardware, launch with `--rendering-method gl_compatibility --rendering-driver opengl3`. The plain `godot` command on this machine points to a different, non-Mono engine; use the Mono application.

## Controls

| Action | Input |
|---|---|
| Move / camera | WASD / mouse |
| Run / jump | Shift / Space |
| Sword combo / finish broken enemy | Left mouse or J |
| Guard / timed deflect | Hold / tap right mouse or K |
| Dodge | Ctrl or Alt |
| Lock target | Tab |
| Resonance Cut | Q |
| Healing remedy | H |
| Talk / use relay | E |
| Continue dialogue | E, Enter or left mouse |
| Skip the rest of a scene | Esc |
| Pause / release pointer | Esc |
| Zoom camera | Mouse wheel |
| Save screenshot | F8 (`artifacts/gameplay.png`) |

Tap guard within **190 ms** of a frontal normal strike to deflect it. Deflection damages enemy posture. Holding guard builds your own posture; reaching its limit staggers you. A **red diamond** warns of an unblockable low sweep: dodge or jump. Fill an enemy's posture bar, then attack nearby during its **4.5-second** opening to finish it. Resonance Cut hits nearby robots for health and posture damage, with a **12-second** cooldown. Three remedies restore 45 health each; Aki replenishes health and remedies.

## Prologue route

1. Speak to **Ren** outside Kisaragi Workshop, at the south end of the village.
2. Cross the bridge and follow the lanterns up the old relay road.
3. Disable all **three** patrol robots.
4. Interact with the manual relay below the broken viaduct; Zero responds from the city.
5. Return to Ren to secure the evacuation route.

The story plays as visual-novel scenes over the paused game: portraits, a name plate and typed-out lines. Beginning the journey, or restarting the prologue from the pause menu, opens with Zero waking Mio through the sensory link. Ren's briefing, the relay restart and the homecoming at nightfall are scenes of their own, and each Hoshikawa resident has a short first conversation before falling back to their usual line. Zero speaks from Mio's wrist terminal, so it appears as a link emblem rather than a portrait. The first press finishes a line that is still typing; the next moves on.

Hoshikawa is a small Japanese hamlet with **six buildings**: a workshop with a lean-to, a kura granary, an irimoya assembly hall, a teahouse, a clinic with an engawa and a thatched farmhouse. It has **six named residents** (Ren, Chiyo, Aki, Hana, Sora, KOMA-7). A brook with a plank footbridge runs past the rice fields, a raised terrace behind a stone wall edges the west side, and the road climbs through a torii to a small shrine in a cedar grove. Countryside includes textured rolling terrain, a river and bridge, rice beds, forest, rocks, meadow vegetation, wildflowers, and a distant machine-city skyline.

Continue north through the viaduct opening to **Cedar Pass** and **Komorebi**, the second village. The valley now spans 128 × 280 metres, with 120 metres of additional northern terrain. A winding lantern road crosses the Cedar River on a second bridge, with a rain shelter and overlook on side paths. Komorebi has five buildings, persimmon orchards, vegetable gardens, a village square and four residents: Nao, Emi, Toma and Yui. **Emi refills health and remedies.** Region banners and a local minimap follow your journey; the original relay mission remains playable.

## Verification

The integration check runs inside Godot, exercising the actual C# controllers and physics:

```sh
dotnet build Hoshikawa.csproj
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --editor --path . --import
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path . --fixed-fps 60 -- --self-test
```

Success prints `SELF_TEST_PASS` and exits 0. Current evidence and environment limitations are recorded in `VERIFICATION.md`. Checks cover imported animations, both villages and all ten residents, walking, running, jumping, evasion, both bridge crossings, the entire northern route in both directions, shelter and overlook access, both medics, dialogue, deflection, guard, perilous attacks, damage timing, skill cooldown, finishers, enemy patrol/aggro, pause, the full mission, and restart.

For reproducible rendered evidence, append `-- --play --capture=res://artifacts/village.png` to a graphical launch. Optional `--overview`, `--combat-view`, `--portrait`, and `--portrait-close` select useful inspection viewpoints. These options are development conveniences; normal play begins at the illustrated title screen.

Run Godot with `--script res://tools/review_dialogue.gd` to capture the prologue's dialogue scenes under `artifacts/dialogue/`. Run Godot with `--script res://tools/review_hamlet.gd` for Hoshikawa captures under `artifacts/hamlet/` (`-- --out=<subdir> --only=square,overview` narrows them). Use `--komorebi` or `--pass-view` with `--play` to inspect the new areas. Run Godot with `--script res://tools/review_valley.gd` for six landscape captures under `artifacts/countryside/`.

Komorebi now has angled service frontages, a smaller courtyard, an orchard packing station, and paths through the garden and orchard fence openings. Graded approaches meet the shelter and river-facing overlook floors; garden soil follows the hillside. Run Godot with `--script res://tools/review_arrangement.gd` for the placement review captures in `artifacts/arrangement-after/`.

## Import another character

Save the model as `.blend`, `.glb`, `.gltf` or `.fbx`, then run from the project root:

```sh
python3 tools/import_character.py /path/to/hana.glb --name hana --height 1.65
```

The shared importer launches a separate background Blender, keeps the input unchanged, preserves textures and supplied active/NLA-bound animation clips, and centers/grounds the model at the requested height in metres. It handles a single deform rig with multiple meshes, or an unrigged static model. Blender uses the installed macOS application by default; override it with `--blender /path/to/blender` or `BLENDER_BIN`.

Outputs are `assets/models/hana.glb`, the packed editable copy `assets/source/hana-game.blend`, original texture copies under `assets/art/hana-import/`, and an audit report at `artifacts/model-imports/hana/build.json`. The report records source/output hashes, bone/triangle counts, clips and texture hashes. Existing outputs require `--force`; use `--output-root artifacts/hana-trial` to review a separate trial first. Keep the input and any external texture/buffer files together and outside these output paths.

Useful options:

- `--armature RigName` selects a rig when the source contains several; repeat `--mesh MeshName` to explicitly choose its meshes.
- `--clip Rest=idle` renames a supplied clip to a game animation name. Clips must be assigned as an active action or NLA strip in Blender.
- `--idle breathing` or `--idle head-turn` adds a simple idle only when the rig has no supplied animations. Use `--head-bone` and `--spine-bones` for unfamiliar bone names.
- `--strip-bone-prefix mixamorig:` optionally removes a bone namespace.
- `--max-triangles 90000` optionally reduces geometry. The default retains it; meshes with shape keys cannot be reduced.

Use export-ready deform rigs and image-based materials: bake procedural materials and rig constraints before import. This tool preserves supplied motion; it does not rig a static mesh, retarget combat, remove root motion or correct a model's facing direction. Mio retains its specialized `tools/import_tripo_mio.py` pipeline for those combat adjustments.

Chiyo and KOMA-7 now use small presets of the shared importer:

```sh
python3 tools/import_tripo_chiyo.py
python3 tools/import_tripo_koma7.py
```

These presets rebuild their existing outputs automatically. For a new NPC, wire the GLB into `scripts/World.cs`, fit its capsule/name label, and use `scripts/ActorVisual.cs` for playback. Then build C#, import assets and run the Godot integration checks above, both headless and graphically; importing a file alone does not add a resident to the village.

The importer regression check exercises all four input formats, transformed multi-mesh skinning, preserved animations/textures, grounding and invalid-input guards:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python-exit-code 1 --python tools/check_character_import.py
```

## Project notes

- `scripts/World.cs`: deterministic environment and village construction; terrain and collision share the same surface. Static boxes, spheres and cylinders are baked into one mesh per material and 32 m cell after construction.
- `scripts/Architecture.cs`: Hoshikawa's Japanese buildings. `Roof()` generates curved gable, irimoya and thatch roofs; `Minka()` builds the timber frame and each style's extras from the `World.Hamlet` table.
- `scripts/Hamlet.cs`: the brook, footbridge, terrace wall, torii and shrine, stone lanterns, and village dressing. All village randomness uses its own seeded generator.
- `tools/build_props.py`: deterministic background-Blender generator for the tree and shrub GLBs in `assets/models/props/`; `assets/shaders/foliage.gdshader` gives their leaf cards alpha cut-out, backlight and wind sway.
- `scripts/Player.cs`, `Enemy.cs`: action timing, movement, and robot behavior.
- `scripts/Game.cs`, `Hud.cs`: quest, camera, dialogue, presentation and audio.
- `scripts/Prologue.cs`: every line of the prologue's visual-novel scenes, kept apart from the code that plays them.
- `assets/art/portraits/`: dialogue portraits generated with Codex from each character's reference art and cut out with `tools/cut_portrait.gd`. A speaker without a file there shows only a name plate.
- `assets/models/mio.glb`: the user's Tripo character, with a 41-bone rig, its four supplied animations and eight adapted combat clips.
- `assets/models/chiyo.glb`: Chiyo's Tripo model with a 41-bone rig and a gentle authored idle. Her original import is preserved in `assets/source/chiyo-tripo-original.blend`; rebuild the game copy with `tools/import_tripo_chiyo.py` in background Blender.
- `assets/models/koma7.glb`: KOMA-7's Tripo robot with a 65-bone rig and an authored head-turn idle. Preserved source: `assets/source/koma7-tripo-original.blend`; rebuild with `tools/import_tripo_koma7.py` in background Blender.
- `assets/source/mio-tripo-original.blend`: preserved full-resolution Tripo import and packed textures; `mio-tripo-game.blend` is the game copy.
- `assets/source/characters.blend`: previous authored Mio, villagers and original sword, retained for reference and combat retargeting.
- `tools/import_tripo_mio.py`: rebuilds the current Mio GLB from the Tripo source in a separate background Blender process.
- `tools/build_characters.py`: legacy full character regeneration. It recreates the previous Mio; run `tools/import_tripo_mio.py` afterward to restore the Tripo character.
- `tools/refine_humans.py`: shared human helpers, 58-bone rigs, normalized skinning and all 12 action clips.
- `tools/mio_illustrated.py`: Mio facial geometry, garment UVs, layered hair and key-art costume.
- `tools/render_human_review.py`: temporary Blender studio renders of faces, idle, running and guard poses.
- `assets/art/`: original imagegen art used directly in the game; prompts in `ASSETS.md`.
- `tools/build_audio.py`: original synthesized ambient loop and combat sounds using Python's standard library.

Mio now uses the user's Tripo model derived from the generated reference artwork. Idle, walk, run and jump come from Tripo. The existing attack combo, guard, dodge, skill, stagger and death clips are adapted to its skeleton, and the original sword follows the right hand. Movement stays under the C# collision controller. The game jump uses the first hop from Tripo's repeated sequence, removes its extra vertical travel, follows physical ascent/descent and starts landing recovery on ground contact.

Chiyo uses her own Tripo elder model beside the Village Assembly. She faces the road, has a quiet breathing idle and retains her existing conversation. Her 1.58 m game copy has 89,999 triangles and the original four 4K material maps. The full-resolution import remains preserved. `tools/review_chiyo.gd` captures her appearance and village dialogue.

KOMA-7 uses his own cream-and-jade Tripo gardener model in Hoshikawa. The 1.25 m model retains all 96,316 triangles and four original 4K maps, with a six-second head-turn idle and fitted collision/label. His conversation is unchanged. `tools/review_koma7.gd` captures his appearance, idle and dialogue; the three enemy patrol robots retain their original model.

The game copy has 150,600 triangles including the sword, a height of 1.84 m, and unchanged 4096×4096 Tripo base-color and normal textures. The original 1,884,692-face import remains intact. The imported rig has no finger bones; combat uses the existing motions and the open-hand sword grip still needs a dedicated animation pass.

Run `python3 tools/check_mio_export.py` to verify the runtime rig, all 12 clips, skin weights and exact texture bytes. `tools/review_tripo.gd` captures the model and animation poses inside the village; results and the pre-replacement backup are in `artifacts/tripo-integration/`. Earlier face review tools and `MioControlCages` apply only to the preserved authored model.
