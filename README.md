# Hoshikawa — The Offline Valley

A playable, original Godot 4.6.2 **C# / .NET 8** action JRPG prologue. Mio explores a rural human settlement in 22nd-century Japan; Zero remains in the occupied machine city and speaks over their sensory link.

## Play

Open `project.godot` in **Godot Mono** and press **F6** on `scenes/Main.tscn`, or **F5** to run the project.

On this Mac:

```sh
just run
```

`just run` builds C#, imports the included assets, and launches the game. Set `GODOT_MONO` to your Mono engine executable on another machine. The project defaults to the Mobile renderer with native Metal on macOS. For older graphics hardware, launch with `--rendering-method gl_compatibility --rendering-driver opengl3`. The plain `godot` command on this machine points to a different, non-Mono engine; use the Mono application.

## Controls

| Action | Input |
|---|---|
| Move / camera | WASD / mouse |
| Run / jump | Shift / Space |
| Sword combo / shut down a robot from behind / finish a broken one | Left mouse or J |
| Guard / timed deflect | Hold / tap right mouse or K |
| Dodge | Ctrl or Alt |
| Lock target | Tab |
| Resonance Cut | Q |
| Healing remedy | H |
| Items | I, or Items on the pause menu |
| Talk / use relay / pick up / open a door | E |
| Continue dialogue | E, Enter or left mouse |
| Skip the rest of a scene | Esc |
| Pause / release pointer | Esc |
| Zoom camera | Mouse wheel |
| Save screenshot | F8 (`artifacts/gameplay.png`) |
| Language | The English / 日本語 / 繁體中文 buttons on the title screen and pause menu |
| Volume | Settings on the title screen and pause menu: a Voice and a Music slider (arrow keys, d-pad or mouse); Esc or Back closes it |

The HUD carries little text: the objective, what E would act on, and an occasional banner. Everything else is icons, bars and keycaps drawn beside what they do (E, Q, H, Tab, Esc), and this key list is on the pause screen.

The items screen (I, or Items on the pause menu) freezes play and lists what Mio carries: supplies on one row, key items on the next, the selected one's icon, count and description beside them. E or its Use button spends a supply; I, Esc or Back closes it to wherever it opened from. She sets out with three remedies (H uses one without opening the screen) and the wrist terminal Zero speaks through. On the relay road she finds the remedy pouch, a key item that lets her carry a fourth remedy, and two resonance cells, each restoring one charge of Resonance Cut; resting refills her remedies but not the cells. Items and pickups are the table in `scripts/Items.cs` and `World.Pickups`.

**Stardust and levelling.** A robot shut down leaves Mio stardust (150 for a patrol, 60 for a scout, 25 for a drone). Her purse is at the bottom right. Hoshikawa's well, the **Starwell**, glows in the square and is the valley's one save point: press E beside it to rest (health, stamina, remedies and Resonance Cut's charges refill, and the scouts and drones she shut down come back, though the patrols stay down) and open its screen, where stardust raises a stat by a point (each level costs more than the last: 100, 150, 200 and on) or buys resonance cells (80 each). HP and SP gain 10 a point; ATK makes the sword and its posture damage bite 10% harder per point, and each strike's damage is divided by DEF / 10, so the first point takes about a tenth off. Being defeated, or slipping out of the map, sends her back to the village with her purse: dying never costs stardust. Running spends SP (the bar over her posture) and, run dry, she walks until it has refilled to 30%. The numbers are the constants in `scripts/Stats.cs`.

Robots shrug off the sword: each robot has 500 health (scouts 300), and from the front each hit only scratches its plating for 2. **Drones**, the small flying robots on the relay road, are the exception and the easy stardust: they have 6 health, so three hits (one full combo) shut one down from the front, each hit knocks one off its swing, and their strikes take 15 and are never the perilous sweep. Get behind one, within 60° of its back, and attack to **shut it down** outright. After each swing a robot holds still and cannot turn for about a second, which is the way round it. Their strikes are heavy: **50** vitality, **75** for the perilous sweep (scouts 60% of that), so dodge rather than trade blows. Tap guard within **190 ms** of a frontal normal strike to deflect it. Deflection damages enemy posture. Holding guard builds your own posture; reaching its limit staggers you. A red **危** crest over a robot warns of an unblockable low sweep: dodge or jump. A gold icon over it means attack now (its posture is broken, or Mio stands behind it), a jade one that it is stunned. Fill an enemy's posture bar, then attack nearby during its **4.5-second** opening to finish it. Resonance Cut does no damage: it locks nearby robots in place for **4 seconds**, long enough to get behind them. It holds **3 charges**, all refilled by resting (talking to Chiaki or Emiko, or recovering at the village) and one at a time by a resonance cell. Three remedies restore 45 health each; Chiaki and Emiko replenish health, remedies and charges. The robots' numbers are the constants at the top of `scripts/Enemy.cs`; the charges are `Player.MaxSkillCharges`.

## Languages

The game is playable in English, Japanese (日本語) and Traditional Chinese (繁體中文, Taiwan usage). The first launch follows the operating system's language, the language buttons on the title screen and the pause menu switch it at any time, and the choice is kept in `user://settings.cfg`. `--lang=en|ja|zh_TW` after `--` overrides both for one run. The Japanese voice plays under every language. Everything shown is translated: the HUD, menus, toasts, signs, name labels and every line of dialogue. `assets/dialogue/glossary.md` fixes the names and terms; `assets/dialogue/*.json` hold the text.

## Prologue route

1. Wake in Mio's room. Her mother's photograph hangs on the north wall (E beside it); the door in the east wall (E) leads out to the Minase House's gate, where **Yui** is waiting with Renji's message and races Mio to the workshop.
2. Speak to **Renji** outside Kisaragi Workshop, at the south end of the village.
3. Cross the bridge to the junction on the old relay road and follow the lanterns up the switchback, past the ruined teahouse and along the high road.
4. Disable all **three** patrol robots: one at the junction, one on the teahouse terrace, one in the relay plaza. The four smaller scouts and five flying drones on the road don't count.
5. Interact with the manual relay below the broken viaduct; Zero responds from the city.
6. Return to Renji to secure the evacuation route.

The story plays as visual-novel scenes over the paused game: portraits, a name plate and typed-out lines. Beginning the journey, or restarting the prologue from the pause menu, opens on narration over the machine city and the valley, then fades in on Mio asleep in her room, where Zero wakes her through the sensory link and tells her what it read in the city timetable overnight: in three days a freight train carrying medicine crosses the valley, and it will not stop at Hoshikawa Station. Those scenes, Yui's at the gate and Renji's briefing are staged: a scene camera frames each line, and the characters act it (Mio wakes, sits up to answer her wrist terminal, rises and crosses to the open shoji; Yui waves, points, hands over Emiko's letter and folds her arms; Renji reads the letter and points up the road). Renji's briefing, the relay restart and the homecoming at nightfall are scenes of their own, and each Hoshikawa resident has a short first conversation before falling back to their usual line. Zero speaks from Mio's wrist terminal, so it appears as a link emblem rather than a portrait. The first press finishes a line that is still typing; the next moves on. Mio and Zero speak their lines in Japanese (the English text stays on screen); the voice stops when the scene moves on or is skipped.

Hoshikawa is a small Japanese hamlet with **seven buildings**, each a Tripo model: a workshop with a lean-to, a kura granary, an irimoya assembly hall with a porch, a teahouse, a clinic with an engawa, a thatched farmhouse and the Minase House, Mio's home on the east side, in the farmhouse style. It has **seven named residents** (Renji, Chiyo, Chiaki, Haruka, Sota, Yui, KOMA-7). A reed-lined brook with a plank footbridge runs past four terraced rice paddies, walled by grass-topped aze and fed through a sluice board by a turning water wheel, a scarecrow keeping watch and a hasa rack drying straw beside them; a raised terrace behind a stone wall edges the west side, and the road climbs through a torii to a small shrine in a cedar grove. Countryside includes textured rolling terrain, a reed-lined river under a timber bridge on stone piers, with a boat moored off the village bank and stone washing steps down to the water, rice beds, forest, rocks, meadow vegetation, wildflowers, and a distant machine-city skyline.

The old relay road is a ravine of trails cut into the mountain rather than an open meadow, and you keep to them: their walls are too steep to climb. From the junction just over the bridge, the lantern road climbs east up a switchback to the terrace of a ruined teahouse on the cliff above the junction, runs north along the high road and drops into the relay plaza under the broken viaduct. East of the plaza a road toward the station, the way on for a later chapter, ends at a barrier before a rockfall. A side branch leaves the junction west into a wooded valley and ends at an old quarry, where a remedy pouch lets Mio carry a fourth remedy. A resonance cell lies behind the terrace's patrol and another at the forest valley's eastern bend. A ladder lies drawn up on the teahouse terrace; lower it from there and it becomes a shortcut between the terrace and the junction. The ladder stays down and the pouch and cells stay taken when you recover at the village; restarting the prologue resets them and Mio's items.

The valley is closed by its own landscape rather than invisible walls. Mountains with 8–12 m rock cliffs rise on the north, east and north-west, and the sea lies to the south-west: behind the shrine and beyond the west terrace a beach runs down to it. Rocky capes run out into the sea from the shore. The river rises high on the eastern range, runs down the mountainside and over two waterfalls into a gorge, flows west past the village at sea level and enters the sea between the beach and a headland. The Cedar River rises further north on the same range, falls twice into Cedar Pass, crosses it in a channel cut through the hill, and leaves through a canyon in the western range, down its own valley to its mouth on the bay; a boulder choke closes the canyon's mouth.

Continue north through the viaduct opening to **Cedar Pass** and **Komorebi**, the second village. The valley now spans 128 × 380 metres: the relay road takes 164 of them, and Cedar Pass and Komorebi lie beyond it. A winding lantern road crosses the reed-lined Cedar River on a second timber bridge, with a rain shelter and overlook on side paths. Komorebi has five buildings, persimmon orchards, vegetable gardens, a village square and three residents: Naomi, Emiko and Toma. **Emiko refills health and remedies.** Region banners and a local minimap follow your journey; the original relay mission remains playable.

## Verification

The integration check runs inside Godot, exercising the actual C# controllers and physics:

```sh
dotnet build Hoshikawa.csproj
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --editor --path . --import
/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path . --fixed-fps 60 -- --self-test
```

Success prints `SELF_TEST_PASS` and exits 0. Append `--lang=ja` or `--lang=zh_TW` to run the same flow with that language on screen, which also fails on any string drawn without a translation; `just self-test` runs all three. Current evidence and environment limitations are recorded in `VERIFICATION.md`. Checks cover imported animations, both villages and all ten residents, walking, running, jumping, evasion, both bridge crossings, the entire northern route in both directions, shelter and overlook access, both medics, dialogue, deflection, guard, perilous attacks, damage timing, the combo's chaining, lunge, sword trail, hit-stop, sparks and cut streak, Resonance Cut's charges, wave and stun, shutting robots down from behind, finishers, enemy patrol/aggro, pause, the full mission, and restart.

Two more suites run on plain `dotnet test`, and CI runs all three on every pull request (`.github/workflows/`; the unit tests also run on every push):

```sh
just unit   # gdUnit4Net unit tests of the terrain, road, house pads and prologue data; no Godot needed
just e2e    # gdUnit4Net tests that drive a headless game through godot-e2e: title, the opening and Mio's room, pause, steering, Renji's briefing
```

For reproducible rendered evidence, append `-- --play --capture=res://artifacts/village.png` to a graphical launch. Optional `--overview`, `--combat-view`, `--portrait`, and `--portrait-close` select useful inspection viewpoints. These options are development conveniences; normal play begins at the illustrated title screen.

Run Godot with `--script res://tools/review_dialogue.gd` to capture the prologue's dialogue scenes under `artifacts/dialogue/`. Run it with `--script res://tools/review_languages.gd -- --lang=ja` (or `zh_TW`, `en`) for the title, HUD, menus and scenes in one language under `artifacts/i18n/<lang>/`. Run Godot with `--script res://tools/review_hamlet.gd` for Hoshikawa captures under `artifacts/hamlet/` (`-- --out=<subdir> --only=square,overview` narrows them). Use `--komorebi` or `--pass-view` with `--play` to inspect the new areas. Run Godot with `--script res://tools/review_valley.gd` for six landscape captures under `artifacts/countryside/`, and with `--script res://tools/review_revamp.gd` for the environment close-ups (sakura, lawn, rice, the tea house eaves through the gameplay camera, a dialogue background, Komorebi's square, orchard, packing station and gardens, and each house in both villages from the front, at 45° and under its eaves) under `artifacts/revamp/`.

Komorebi reuses Hoshikawa's Tripo buildings: the thatched farmhouse is the lodge, and the clinic, weavers' workshop and orchard kura face the road, with the meeting hall at its north end. The square has a Tripo well, notice board and benches, and stone lanterns flank the hall. Persimmon trees bear fruit in the orchard, and a handcart, barrels and a hasa rack stand at its packing station. Hoshigaki hang under the store's eave, and a scarecrow stands in the cabbage beds. Paths run through the garden and orchard fence openings. Graded approaches meet the shelter and river-facing overlook floors; garden soil follows the hillside. Run Godot with `--script res://tools/review_arrangement.gd` for the placement review captures in `artifacts/arrangement-after/`.

## Import another character

Save the model as `.blend`, `.glb`, `.gltf` or `.fbx`, then run from the project root:

```sh
python3 tools/import_character.py /path/to/hana.glb --name hana --height 1.65
```

The shared importer launches a separate background Blender, keeps the input unchanged, preserves textures and supplied active/NLA-bound animation clips, and centers/grounds the model at the requested height in metres. It handles a single deform rig with multiple meshes, or an unrigged static model. Blender uses the installed macOS application by default; override it with `--blender /path/to/blender` or `BLENDER_BIN`.

Outputs are `assets/models/hana.glb`, the packed editable copy `assets/source/hana-game.blend`, original texture copies under `assets/art/hana-import/` (a static model's go to the uncommitted `assets/source/NAME-textures/` instead, since Godot extracts its textures from the GLB), and an audit report at `artifacts/model-imports/hana/build.json`. The report records source/output hashes, bone/triangle counts, clips and texture hashes. Existing outputs require `--force`; use `--output-root artifacts/hana-trial` to review a separate trial first. Keep the input and any external texture/buffer files together and outside these output paths.

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

- `scripts/World.cs`: deterministic environment and village construction; terrain and collision share the same surface, a 1 m mesh in 64 m chunks over the play area and its 40 m margin, and 4 m mountains and seabed beyond. Static boxes, spheres, cylinders and the Blender village props are baked into one mesh per material and 32 m cell after construction. The village wells, stone lanterns, handcarts, notice boards, water wheel, scarecrows, moored boat and washing steps are Tripo models (`assets/models/{well,lantern,cart,notice,waterwheel,scarecrow,boat,kawabata}.glb`), placed whole by `Model()` with their own fitted colliders and a brightening albedo tint. Warm afternoon light: a low south-west sun, sky ambient and distance-only depth fog. `Grass()` lays lawn, meadow, reeds and rice as swaying blade tufts in 32 m chunks that drop out beyond 90 m. `Height()` levels the four paddies in `World.Paddies`; `Shore()` gives the terrain shader its wet banks. The river, brook and Cedar River share the depth-aware `water.gdshader`; the paddies' still water is `paddy.gdshader`. Field fences are weathered-wood post and rail props with their own colliders. `TimberBridge()` builds both river bridges from the same props and a `plank` deck on stone piers, keeping a slab and rail colliders. Roofs carry camera-only colliders (`CameraLayer`) so the camera stays under the eaves.
- `scripts/RelayRoad.cs`: the old relay road. Its trails (`World.Trails`) are level floors cut into a block of mountain, which also lay the road's paint; the file places the scatter, lanterns, ruined teahouse, relay, viaduct, the closed station road, quarry, remedy pouch and the teahouse ladder shortcut, and the homes of the patrols, scouts and drones.
- `scripts/Wilds.cs`: the land beyond the play area. Cliffs and mountains, the south-west beach, sea and capes, the rivers' headwaters, gorges, falls and canyon (`World.Rivers`, each course's water level falling downstream), and the only invisible walls, off the beach and behind the canyon's boulder choke. `waterfall.gdshader` streaks the falls' curtains. `tools/review_map.gd` captures the sea, both river mouths, the falls, the Hoshikawa's headwater, the canyon, the mountain walls and two aerials under `artifacts/map/`.
- `scripts/Architecture.cs`: Hoshikawa's Japanese buildings. `Roof()` generates curved roofs (now only the shrine's); `Minka()` places each building's Tripo model on its `World.Hamlet` pad, or Komorebi's level ground from `World.KomorebiHouses`, from the `TripoHouses` table, with fitted wall colliders, its own mesh as camera-only eaves, the shop sign on cords from the eave, and each style's extras (the teahouse's lantern, bench and umbrella; the clinic's medic banner and herb rack).
- `scripts/Hamlet.cs`: the brook with its pebbles and rocks, the reeds along the brook and both rivers, the rivers' pebbles and rocks, the rice fields (aze levees, paddy water, rice, irrigation channel and sluice, hasa rack, scarecrow), the water wheel with its legs and trough, the bobbing boat moored to its post and the walkable washing steps on the river's village bank (`Landings()`), footbridge, terrace wall, torii and shrine, stone lanterns, the well, notice board, handcart, scarecrow and hasa rack both villages share, and village dressing. All village randomness uses its own seeded generators.
- `tools/build_props.py`: deterministic background-Blender generator for the tree, shrub and persimmon GLBs in `assets/models/props/`. Small outward-facing leaf cards carry normals from the whole crown's ellipsoid, so a crown shades as one volume. `assets/shaders/foliage.gdshader` gives them alpha cut-out, a fade for cards seen edge-on, backlight and wind sway; `grass.gdshader` sways the grass and rice blades.
- `tools/build_village_props.py`: deterministic background-Blender generator for the lamp, bench, barrel, bamboo and field fence, brook stone, sluice, water-wheel trough, hasa rack, bridge plank, cabbage and daikon GLBs in `assets/models/props/`. They carry material names only (`cedar`, `paper`, `bark_bamboo`, `stone`, `straw`, `leaf`, `leaf_dark`, `root`); `World.Prop()` binds them to the existing atlases and colours.
- `tools/build_bedding.py`: background-Blender generator for Mio's futon, buckwheat pillow and quilted cover (`assets/models/props/bedding.glb`). The cover is cloth dropped onto her own body from `mio.glb`, asleep and sat up, with a `sit` and a `folded` shape key; `--preview=DIR` renders the three states and `--cache=FILE` keeps the simulated cloth for reruns.
- `tools/shrink_glb_textures.py`: halves a generated GLB's oversized textures in background Blender before `tools/import_character.py`, keeping each map's encoding. Used for the Tripo props' 4096² maps.
- `tools/compress_textures.py`: switches model textures' `.import` files to VRAM compression with mipmaps, flagging normal maps. `tools/import_character.py` runs it between two Godot imports; run it by hand, then re-import, after any other model import.
- `tools/cut_glb.py`: deletes the faces of a generated GLB that lie wholly inside a box, in background Blender, passing everything else through. Used to cut a stray fragment off the Tripo water wheel.
- `scripts/Player.cs`, `Enemy.cs`: action timing, movement, and robot behavior.
- `scripts/Items.cs`: the item table (name, text, icon, kind, how many she can carry, what using one does) and `Inventory`, what Mio carries.
- `scripts/Game.cs`, `Hud.cs`: quest, camera, dialogue, presentation and audio.
- `scripts/Effects.cs`, `SwordTrail.cs`: combat effects (flashes, sparks, cut streaks, dust, rings, Resonance Cut's slam) and the sword trail, lit by `assets/shaders/glow.gdshader` and `sword_trail.gdshader`.
- `scripts/Prologue.cs`: every line of the prologue's visual-novel scenes, kept apart from the code that plays them, including each resident's repeated line and Zero's relay reminders. `SceneNames` gives each scene the key its voice files and translations use; `Stage` gives a beat its camera shot and each character's clip; `Unrecorded` lists the Mio and Zero lines still waiting for a voice.
- `scripts/Cutscene.cs`: the opening's staging: the scene camera and its shots, each beat's acts, the characters turning to each other, Yui's runs and walks between the gate, the workshop and the clinic, the fade from black and the trip from Mio's room to the valley.
- `scripts/Room.cs`: Mio's room, the prologue's first map, built while she is in it and freed when she leaves, with the valley hidden. Its tatami, walls, timber, shoji paper, quilt, Shio's photograph and the painted view out of the shoji are Codex art (`assets/art/room/`); `assets/shaders/view.gdshader` draws the view. The futon, pillow and cover are Blender models: the cover lies draped over Mio as she sleeps, slides into her lap as she sits up and is folded at the futon's foot when she gets up.
- `scripts/Localization.cs`: the three languages. UI text is keyed by its English wording and registered with Godot's `TranslationServer`, so `Label3D` signs and `Button`s follow a switch by themselves and the HUD calls `T()`/`F()`; dialogue is looked up by scene position (`Line`). Also picks the language at launch, saves the choice, and sets the CJK fallback faces. Strings it cannot find are collected in `Localization.Missing`, which the self-test requires to stay empty.
- `assets/audio/voice/ja/Mio/`, `Zero/`: Mio's and Zero's Japanese voices, one mp3 per line named by its key (`Intro.3.mp3`). `Game.Advance()` plays the file for the beat it shows.
- `assets/art/ui/`: the HUD's thirteen icons and the title logo, white on clear so the HUD tints each where it draws it (`Hud.Icon()`); a robot's icon is a billboard `Sprite3D`. Cut from Codex's sheets by `tools/cut_ui_art.gd`; prompts in `assets/art/ui/ui-art-generation.md`.
- `assets/art/portraits/`: dialogue portraits generated with Codex from each character's reference art and cut out with `tools/cut_portrait.gd`. A speaker without a file there shows only a name plate.
- `assets/models/mio.glb`: the user's low-poly Tripo character with Tripo's 65-bone rig and eighteen clips: eight made in Tripo Studio, the three attacks and Resonance Cut keyed in Blender by `tools/mio_combat.py`, and the opening's sleep, wake, sit, terminal and rise keyed by `tools/cast_clips.py`.
- `assets/models/chiyo.glb`: Chiyo's Tripo model with a 41-bone rig and a gentle authored idle. Her original import is preserved in `assets/source/chiyo-tripo-original.blend`; rebuild the game copy with `tools/import_tripo_chiyo.py` in background Blender.
- `assets/models/koma7.glb`: KOMA-7's Tripo robot with a 65-bone rig and an authored head-turn idle. Preserved source: `assets/source/koma7-tripo-original.blend`; rebuild with `tools/import_tripo_koma7.py` in background Blender.
- `assets/source/tripo-bridge-20261016-mio-anim.blend`: the low-poly model with its Tripo rig and clips, as sent over DCC Bridge. `mio-lowpoly-fixed-face2.blend` holds its repaired hair colour, face and normal map; `mio-tripo-game.blend` is the game copy. `mio-tripo-original.blend` is the earlier full-resolution import.
- `assets/source/characters.blend`: the authored Mio scene, kept only because `tools/import_tripo_mio.py` takes the sword from it.
- `tools/import_tripo_mio.py`: rebuilds the current Mio GLB from those Tripo sources in a separate background Blender process, cutting and retiming each clip to the game's timing.
- `tools/mio_combat.py`: keys the three-swing combo and Resonance Cut on Mio's rig from a few poses per clip, by inverse kinematics. `tools/preview_mio_combat.py` renders them as contact sheets in any Blender, without the Tripo sources; `tools/review_combat.gd` records them in the game.
- `assets/models/patrol.glb`: the patrols' and scouts' robot, the user's Tripo Studio model on its 65-bone Mixamo rig, with seven clips (idle, walk, run, the guard windup, the strike, stagger and death) keyed in Blender by `tools/patrol_clips.py`. Rebuild with `tools/import_tripo_patrol.py` from `assets/source/patrol-tripo.glb`; `tools/patrol_clips.py` on its own renders the clips as contact sheets from the committed model, and `tools/review_patrol.gd` captures them in the game.
- `assets/models/drone.glb`: the drones' own Tripo Studio model, static, its halo split off as a `Rotor` mesh that spins in flight. Rebuild with `tools/import_tripo_drone.py` from `assets/source/drone-tripo.glb`.
- `tools/refine_humans.py`: the shared villager (`villager.glb`): 58-bone rig, normalized skinning and all 12 action clips. Saves `assets/source/villager.blend`.
- `assets/art/`: original imagegen art used directly in the game; prompts in `ASSETS.md`.
- `scripts/Volume.cs`: the Voice and Music mixer buses and their ten-step volumes, which the settings screen sets and `user://settings.cfg` keeps beside the language. Dialogue voice lines and Mio's shouts play on Voice, the music on Music; the sound effects stay on Master.
- `scripts/Music.cs`: background music. The valley's loop plays in the villages and Cedar Pass, the relay road has its own, and a battle track takes over while a robot is after Mio; each change crossfades over 1.5 s.
- `tools/build_audio.py`: original synthesized ambient loop and the remaining interface and movement cues using Python's standard library.
- `assets/audio/`: the sword, slam and short-circuit sounds, Mio's combat shouts (`mio_*.wav`, Japanese, her voice Yuki) and the relay road and battle music (`relay.ogg`, `battle.ogg`, one loop each) are ElevenLabs takes; ASSETS.md lists their prompts.
- `assets/dialogue/ja.json`, `zh_TW.json`: every prologue line in Japanese and Traditional Chinese, keyed like `Intro.1` by scene and beat in `Prologue.cs`; `ja.json` is also the script for the Japanese voice. `ui.ja.json`, `ui.zh_TW.json`: the interface strings, keyed by their English text. `glossary.md`: names, places, game terms and each character's tone in all three languages.
- `assets/fonts/`: Noto Sans and Serif CJK (JP and TC), cut down to the characters the translations use by `tools/build_fonts.py` (needs fontTools and the Noto CJK collections; re-run it after adding a character). OFL licence in `OFL.txt`.
- `tests/unit/`: gdUnit4Net unit tests on plain .NET against the game assembly: the road against the terrain shader, house pads and clearances, the brook bed, level paddy floors, regions, Tripo resident assets, the prologue's voice files and portraits, and that both translations cover every line and string with matching placeholders and no dead entries.
- `tests/e2e/`, `addons/gdunit_e2e/`: gdUnit4Net end-to-end tests and the godot-e2e addon they use to launch and drive a headless game process. The addon is not versioned — CI downloads the pinned [cwchanap/godot-e2e](https://github.com/cwchanap/godot-e2e) release zip each run, and `just e2e` fetches the same release locally.
- `.github/workflows/`: `unit.yml` runs the unit tests on every push and pull request; `e2e.yml` installs Godot 4.6.2 .NET and runs the self-test and e2e tests on pull requests and by manual dispatch.
- `tools/build_voice.py`: Japanese voice lines from ElevenLabs text-to-speech, standard library only. `--check` validates `ja.json` against `Prologue.cs` (`zh_TW.json` is checked by the unit tests); `--audition` renders candidate voices to `artifacts/voice-audition/`. The procedure is `.claude/skills/voicing-dialogue/SKILL.md`.

Mio uses the user's low-poly Tripo model. Eight of her clips come from Tripo Studio: the idle, walk, run and jump presets, and the guard, dodge, stagger and death generated from text prompts, each cut and retimed to the game's timing, with the original sword in her closed right hand. The combo and Resonance Cut are keyed in Blender by `tools/mio_combat.py`. The combo is a diagonal cut, a rising backhand and a leaping overhead slam, each starting from the pose the last one ends its follow-through in: press attack again during a swing and the next one starts there, well before the first has recovered. Each swing turns to the locked robot or the nearest one in reach and lunges up to it; a landed blow holds the valley in a few frames of hit-stop, flashes on the robot's plating and throws sparks, and the blade leaves a glowing jade trail (gold on the third swing, which also throws up dust). Resonance Cut draws back as jade motes gather round her, leaps spinning twice so the whole blade draws a full moon of light, and slams down; two rings, a column of light and a cloud of dust go out from the slam, and the robots it stuns crackle with jade sparks. Movement stays under the C# collision controller: walking is 2 m/s and running 7.8 m/s, with both cycles played at a matching rate. The dodge is a 0.6 s roll over the same distance as before. The game jump uses the first hop from Tripo's repeated sequence with an added crouch and knee tuck, removes its extra vertical travel, follows physical ascent/descent and starts landing recovery on ground contact.

Chiyo uses her own Tripo elder model beside the Village Assembly. She faces the road, has a quiet breathing idle and retains her existing conversation. Her 1.58 m game copy has 89,999 triangles and the original four 4K material maps. The full-resolution import remains preserved. `tools/review_chiyo.gd` captures her appearance and village dialogue.

KOMA-7 uses his own cream-and-jade Tripo gardener model in Hoshikawa. The 1.25 m model retains all 96,316 triangles and four original 4K maps, with a six-second head-turn idle and fitted collision/label. His conversation is unchanged. `tools/review_koma7.gd` captures his appearance, idle and dialogue.

Sota (Hoshikawa), Naomi and Toma (Komorebi) use their own Tripo models (`sora.glb`, `nao.glb`, `toma.glb`), sent from Tripo Studio over DCC Bridge. Each keeps all of its geometry (18,215–19,591 triangles), a 4K base-colour map and 2K normal/roughness/metallic maps, and a four-second breathing idle. Sota is 1.78 m, Naomi 1.64 m and Toma 1.74 m, with fitted collision and labels. Their positions, conversations and Komorebi services are unchanged. Rebuild them with `python3 tools/import_tripo_villagers.py [sora nao toma]`. `tools/review_villagers.gd` (`-- --only=sora,toma` narrows it) captures each one's appearance, idle and village dialogue under `artifacts/{sota,naomi,toma,yui,renji}-integration/`.

Yui and Renji use the user's Tripo Studio models, downloaded as FBX and saved with their maps packed as `assets/source/{yui,renji}-tripo-original.blend`: 65 Mixamo bones, about 19,300 triangles, 4K maps, Yui 1.60 m and Renji 1.78 m. Their clips (idle, walk and the gestures the opening plays: Yui's run, wave, give, point, read and fold, Renji's work at his bench, give, point and read) are keyed in Blender by `tools/cast_clips.py`; rebuild both with `blender --background --factory-startup --python tools/import_tripo_cast.py`, or run `tools/cast_clips.py` alone for contact sheets from the committed model.

The game copy has 47,980 triangles including the sword (the decimated dense import had 150,600), a height of 1.84 m, Tripo's 65-bone rig, and repaired 4096×4096 base-color and normal textures. The original 1,884,692-face import remains intact. Her right hand closes around the sword hilt in every clip.

Run `python3 tools/check_mio_export.py` to verify the runtime rig, all 18 clips, skin weights and exact texture bytes. `tools/review_tripo.gd` captures the model and animation poses inside the village; results and the pre-replacement backup are in `artifacts/tripo-integration/`, the low-poly replacement's evidence is in `artifacts/mio-lowpoly-integration/`, and the Tripo clips' evidence is in `artifacts/mio-tripo-animation/`. Earlier face review tools and `MioControlCages` apply only to the preserved authored model.
