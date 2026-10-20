# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Hoshikawa — The Offline Valley: a Godot 4.6.2 Mono (C# / .NET 8) action-JRPG prologue. Single outdoor map, one playable character (Mio), three patrol robots, ten NPCs across two villages.

## Commands

```sh
just run                      # build C#, import assets, launch (the whole loop)
just run -- --play            # skip the title screen
just unit                     # gdUnit4Net unit tests, plain .NET
just self-test                # build, import, in-engine self-test
just e2e                      # build, import, godot-e2e tests
dotnet build Hoshikawa.csproj # C# only
```

`just run` uses `GODOT_MONO`, defaulting to `/Applications/Godot_mono.app/Contents/MacOS/Godot`. The plain `godot` on PATH is a **non-Mono** build and will not run this project. `GODOT_MONO` is not exported in the shell, so set it before running the raw `$GODOT_MONO` commands below: `export GODOT_MONO=/Applications/Godot_mono.app/Contents/MacOS/Godot`. Likewise `blender` is not on PATH; use `/Applications/Blender.app/Contents/MacOS/Blender`.

`.claude/worktrees/` holds full checkouts of other branches (excluded via `.git/info/exclude`). Ripgrep skips them; plain `grep -r`/`find` will return duplicate hits from them.

### Tests

Three suites, all C#. CI (`.github/workflows/`) runs the unit tests on every push and PR, and the self-test plus e2e tests on PRs and by manual dispatch. CI is headless Linux only: GitHub's runners have no GPU and software Vulkan draws the valley at well under 1 fps, so graphical runs stay a local Metal job.

- **Unit** (`tests/unit/`, `just unit`): gdUnit4Net (`[TestSuite]`, `[TestCase]`, `AssertThat`) on plain .NET against the game assembly, with no engine running. Only static data and pure functions work there (`World.Height`, `RoadX`, the `Hamlet` table, `Prologue`, files under `assets/`); nodes, `GD`, and native singletons such as `Geometry2D` or `RandomNumberGenerator` need the engine, so behaviour that touches them belongs in the self-test or e2e.
- **E2E** (`tests/e2e/`, `just e2e`): gdUnit4Net tests that launch the game as a separate headless Godot process through godot-e2e (`addons/gdunit_e2e/`, not versioned — CI downloads the pinned cwchanap/godot-e2e release zip (`GODOT_E2E_VERSION` in `e2e.yml`, mirrored by `e2e_addon_version` in the justfile) and `just e2e` fetches it when missing or stale; never edit the copy here, fix upstream and bump the pin). Tests reach nodes by path (`/root/Hoshikawa`, `/root/Hoshikawa/Mio`) and read C# fields by name (`Mode`, `QuestStage`, `Speaker`); keys, frame waits and vectors go through the raw commands in `tests/e2e/Hoshikawa.cs`. The child finds Godot through `GODOT_BIN`, which `just e2e` sets from `GODOT_MONO`. A failing test leaves its scene tree and logs in `test_output/csharp/<suite>/<test>/` (no screenshot headless). Each test pays a full game start, about eight seconds.
- **Self-test** (`just self-test`): `scripts/IntegrationCheck.cs`, the main suite, a single async method that runs inside a real Godot instance, injecting input and asserting against the actual controllers and physics:

```sh
dotnet build Hoshikawa.csproj
$GODOT_MONO --headless --editor --path . --import      # required after any asset change
$GODOT_MONO --headless --path . --fixed-fps 60 -- --self-test
```

Passing prints `SELF_TEST_PASS` and exits 0; failing prints `SELF_TEST_FAIL <exception>` and exits 1. Each assertion logs `CHECK <message>`.

To run a subset, comment out or early-return within `IntegrationCheck.Run` — assertions are sequential statements, not discoverable cases, and later ones depend on earlier player placement. Prefer adding checks next to the related ones rather than at the end.

Run it **both** headless and graphically (drop `--headless`); several past defects only appeared under the Metal renderer. `project.godot` selects the **Mobile** rendering method on Metal, not Forward+, so check a rendering feature is supported there before relying on it. Under a graphical run, OS focus changes release injected input — `Frames()` re-presses held actions and logs `TEST_INPUT_RESTORE`, so don't "fix" a flake by removing that.

A `NU1900` NuGet audit-endpoint warning on build is expected and not a regression.

### Captures and review scripts

```sh
$GODOT_MONO --path . -- --play --capture=res://artifacts/village.png
# viewpoint flags: --overview --combat-view --portrait --portrait-close --komorebi --pass-view --title-view
$GODOT_MONO --path . --script res://tools/review_valley.gd        # six landscape captures (overwrites artifacts/countryside/)
$GODOT_MONO --path . --script res://tools/review_revamp.gd        # environment close-ups, eaves spring camera, dialogue background; -- --out=<subdir> --only=sakura-close
$GODOT_MONO --path . --script res://tools/review_hamlet.gd        # Hoshikawa captures; -- --out=<subdir> --only=square,overview
$GODOT_MONO --path . --script res://tools/review_arrangement.gd   # building/prop placement
$GODOT_MONO --path . --script res://tools/review_tripo.gd         # Mio model and animation poses
$GODOT_MONO --path . --script res://tools/review_jump.gd          # Mio jump frame sequences (artifacts/tripo-jump/)
$GODOT_MONO --path . --script res://tools/review_chiyo.gd         # Chiyo poses and dialogue (artifacts/chiyo-integration/)
$GODOT_MONO --path . --script res://tools/review_koma7.gd         # KOMA-7 views (artifacts/koma7-integration/)
$GODOT_MONO --path . --script res://tools/review_villagers.gd      # Sora/Nao/Toma views; -- --only=sora (artifacts/NAME-integration/)
$GODOT_MONO --path . --script res://tools/review_dialogue.gd      # prologue visual-novel scenes (artifacts/dialogue/)
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=<png> --out=<png> [--rect=x,y,w,h] [--flip] [--ground=rows]   # dialogue portrait cut-out
$GODOT_MONO --path . --disable-vsync --fixed-fps 60 --script res://tools/render_cost.gd   # draw calls/primitives/CPU ms at three village viewpoints
```

To compare render cost between commits, compare `render_cost.gd`'s draw and primitive counts, which repeat exactly under `--fixed-fps`. Its fps and CPU times drift with whatever else the machine is running.

`artifacts/` is gitignored; it holds captures, logs and per-change evidence dirs. `VERIFICATION.md` records what was actually verified and the environment's limits — update it when claims change, and do not claim a physical keyboard/mouse session for engine-injected input.

## Architecture

**The entire game is built in C# at runtime.** `scenes/Main.tscn` is six lines: a `Node3D` with `scripts/Game.cs` attached. There are no other game scenes, no editor-authored nodes, no autoloads, and no `.tres` resources; godot-e2e's `addons/gdunit_e2e/runtime/bootstrap.tscn` is loaded only by e2e test processes. Do not reach for the Godot editor to add content — add it to the constructing code.

- `Game.cs` — root `Node3D`. Owns `GameMode` (Title/Playing/Dialogue/Paused/Defeated), quest stage and objective text, the camera rig (`Node3D` → `SpringArm3D` → `Camera3D`), lock-on, dialogue, toasts, effects (`Burst`/`Slash`/`FloatingNumber`), audio, screenshots, and the input map. `InputBindings()` registers every action in code — `project.godot` has no `[input]` section.
- `World.cs` — deterministic environment and both villages. Nothing here is random at runtime (`rng` is seeded 2189).
- `Architecture.cs` / `Hamlet.cs` — `partial class World`. `Architecture.cs` holds the curved-roof generator (`Roof`, `RoofY`) and the `Minka()` timber-frame kit for the six Hoshikawa styles. `Hamlet.cs` holds the brook, footbridge, terrace wall, shrine, `StoneLantern()` and the village `Dressing()`. The Komorebi buildings still use the older `House()`.
- `Player.cs` / `Enemy.cs` — `CharacterBody3D` controllers holding all action timing as float countdowns ticked in `_PhysicsProcess` (attack, dodge, guard, deflect window, posture, stun, invulnerability, cooldowns). Combat tuning lives inline in these two files; there is no data/config layer.
- `Prologue.cs` — every line of the prologue's visual-novel scenes as `(Speaker, Text)` beats; a blank speaker is narration. `Game.Play(beats)` starts a scene, `Advance()` finishes a typing line or moves on, `EndDialogue()` returns to play. Quest stages still change on `Interact()`, before the scene plays. `Begin()` (title screen) plays the intro; `Start()`, which the self-test calls, does not.
- `ActorVisual.cs` — thin `AnimationPlayer` wrapper. Finds the animator under an imported GLB, normalizes clip names (`some/path/run.001` → `run`), and sets loop modes. Every character goes through it; `Play(clip, speed, restart)` is the only animation entry point.
- `Hud.cs` — a single `Control` drawn entirely in `_Draw()` (immediate mode). Title screen, vitals, minimap, dialogue and perilous-attack diamond are draw calls, not nodes; `Game._Process` calls `QueueRedraw()` each frame. Menus are the one exception: `RefreshMenu()` builds real `Button` nodes. In `GameMode.Dialogue` the HUD draws only the visual-novel layer: portraits from `assets/art/portraits/<name>.png` (lower case, no hyphen; a missing file just leaves the slot empty), Zero's link emblem, and the text box.

### Invariants worth knowing before editing the world

1. **`World.Height(x, z)` is the single source of truth for terrain.** The visible mesh, its trimesh collider, and every placement call read the same static function, so geometry and collision cannot drift. Placing anything means calling `Height()` — never hardcode a Y. Adding a landform means editing `Height()`, which moves collision with it. The water is the exception: the `Hoshikawa river` sheet (z −12…−4) and the brook strip are fixed planes at `World.WaterLevel` whose edges must stay buried in the river dip and bank in `BaseHeight()` and the brook channel in `HamletGround()`. Reshape those and the edges surface; `IntegrationCheck` requires ≥ 4 cm of burial, just above the water shader's 3.5 cm wave. The brook's mouth row reuses the river sheet's edge vertices, because the wave is computed per vertex and any other spacing opens a step at the joint.
2. **`World.RoadX(z)` and `assets/shaders/terrain.gdshader` describe the same road curve in two languages.** Change one and you must change the other, or the painted road separates from the placement guide; `TerrainTest` reads the shader's constants and fails when they drift. The same applies to `RegionAt()`'s Z thresholds and the region names used in `Game._Process` toasts and the HUD minimap.
3. **Hoshikawa's layout is data.** The `World.Hamlet` table (name, centre, size, yaw, style) drives the levelled house pads inside `Height()`, the `Minka()` buildings, the shrubs and the stepping stones. Move a house by editing the table, never one placement. `World.BrookX(z)` is shared by the `Height()` channel, the water strip, the footbridge and the minimap line; `World.Footpaths` by the footpath strips, the shrub clearance and the grass exclusion.
4. **Seeded streams are pinned.** The countryside and northern valley draw from `rng` (seed 2189), and `IntegrationCheck` pins the first boulder's exact position. Hoshikawa's dressing draws only from `villageRng` (seed 3301). `Tree()` always consumes exactly three draws from whichever generator it is given, so changing a species or model never shifts later scatter. `World.Grass()` draws a field's extra tufts, blades and colours from its own seeded generator, so lawns and meadows never shift the streams their seed positions come from.
5. **Static primitives are baked.** At the end of `World._Ready`, `Merge()` bakes every box, sphere and cylinder, and every Blender prop placed with `Prop()`, under `World`'s plain `Node3D` containers into one mesh per material and 32 m cell. The originals stay, hidden, for their colliders, names and `IntegrationCheck`'s lookups. Moving or recolouring an original after `_Ready` changes nothing on screen. Anything that must change at runtime goes under a non-`Node3D` parent or is added after `_Ready`. `Mat()` returns one shared material per colour, emission and `twoSided`, which is what lets `Merge()` group by reference: never change a material `Mat()` returns. Pass `twoSided: true`, or build your own material.

Collision layers: **1** world/static geometry, **2** player, **4** enemies, **8** (`World.CameraLayer`) camera-only: roofs carry a `CameraStop()` body there so the spring arm (mask 1|8) stays under the eaves while the player and robots, which never collide with 8, walk beneath them. `World.Box(..., solid: true)` is the standard way to add a static collider alongside a mesh.

### Character assets

GLBs under `assets/models/` are **pre-exported and committed**; `editor/import/blender/enabled=false`, so Godot never touches Blender. `.blend` sources in `assets/source/` are gitignored and live only on this machine.

For new models use `python3 tools/import_character.py SOURCE --name NAME --height METRES`. It accepts `.blend/.glb/.gltf/.fbx`, preserves source files, textures and bound clips, and writes the GLB, `assets/source/NAME-game.blend`, `assets/art/NAME-import/` and `artifacts/model-imports/NAME/build.json`. Existing outputs require `--force`; `--output-root` supports trial exports. Chiyo/KOMA-7 scripts and `tools/import_tripo_villagers.py` (Sora/Nao/Toma) are presets of this importer; Mio's importer, which cuts and retimes her Tripo clips and fits the sword, remains specialized. See README's import section for selection/idle/reduction options and the runnable `tools/check_character_import.py` regression check. New NPCs still need an entry in `World.TripoResidents` (asset, height, label height) and runtime checks.

- `mio.glb` — the user's low-poly Tripo model with the 65-bone Mixamo rig (prefix stripped) and 12 clips the user made in Tripo Studio (`assets/source/tripo-bridge-20261016-mio-anim.blend`), textures from the repaired `mio-lowpoly-fixed-face.blend`, 47,980 triangles with the sword. `import_tripo_mio.py`'s `CLIPS` table cuts each Tripo action and retimes it to the game length Player.cs is tuned against; change a clip there, not in Godot. The same script adds the attack ready-stance blends, the jump crouch/tuck and the right-hand grip; Player.cs plays walk and run at ground-matched rates. Rebuilt by `/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/import_tripo_mio.py`, audited by `python3 tools/check_mio_export.py`.
- `chiyo.glb` — the user's Tripo elder: 41 bones, one authored `idle` clip (the source supplied no actions), 1.58 m tall. Built by `tools/import_tripo_chiyo.py` in background Blender; source `assets/source/chiyo-tripo-original.blend` is preserved. Only Chiyo uses this GLB.
- `koma7.glb` — the user's Tripo gardener robot: 65 bones, one authored six-second head-turn `idle`, 1.25 m tall. Built by `tools/import_tripo_koma7.py` in background Blender; the original source and all 96,316 triangles/four textures are preserved. Only KOMA-7 uses it; hostile robots retain `patrol.glb`.
- `sora.glb` / `nao.glb` / `toma.glb` — the user's Tripo villagers, sent from Tripo Studio over DCC Bridge: 65 Mixamo bones (prefix stripped), one authored four-second breathing `idle` each, 1.78 / 1.64 / 1.74 m, all source triangles kept. Built by `tools/import_tripo_villagers.py` from `assets/source/{sora,nao,toma}-tripo-original.blend`. The other human NPCs (Ren, Aki, Hana, Emi, Yui) retain the shared villager rig.
- `villager.glb` / `patrol.glb` — authored humans: 58 bones, 12 clips. Built by `tools/refine_humans.py` (+ `mio_illustrated.py`, `mio_turnaround.py`) inside Blender. `build_characters.py` is legacy and recreates the *old* Mio — running it requires re-running `import_tripo_mio.py` afterward. Likewise `check_mio_{eye_cheek,eye_rim,hair,lids,orbit_contour}.py` and `render_human_review.py` inspect the authored `HumanV2_*` Blender scenes, not the shipped Tripo Mio.

`IntegrationCheck` asserts exact bone counts (Chiyo 41, Mio/KOMA-7/Sora/Nao/Toma 65, shared humans 58), specific bone names and Mio's 12 clips. For every `World.TripoResidents` entry it also asserts the looping idle, textured material, skeleton deformation, grounding, fitted collision, idle duration and stationary hips/feet. Any re-export must keep the corresponding character's contract or the self-test fails.

Trees and shrubs (`assets/models/props/*.glb`) have no `.blend` source. `/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_props.py` regenerates them deterministically. They carry material *names* only (`bark_<species>`, `foliage`); `World.Tree()` binds the bark textures and `foliage.gdshader` by those names. Village props (lamp, bench, barrel, bamboo fence post and bay) come the same way from `tools/build_village_props.py`. They carry material names `cedar`, `paper` and `bark_bamboo`, which `World.Prop()` binds to the existing atlases. Keep them to those three: each extra material adds a merged draw in every cell a prop stands in. Complex props come from the Tripo API instead. Shrink their 4096 px maps with `tools/shrink_glb_textures.py`, then import them static with `import_character.py` under 10k triangles. Place them whole, unmerged, with fitted colliders, through `World.Model()`, like `well`, `lantern`, `cart` and `notice` (ASSETS.md records task ids, prompts and credits). `Model()` gives each model one shared, tinted copy of its material, because Tripo's albedo reads darker than the village atlas; the tint is the brightness knob. Newly imported textures default to `mipmaps/generate=false`: set it to true in their `.import` for anything that tiles or is alpha-cut.

Audio is synthesized by `python3 tools/build_audio.py` (stdlib only). Voice is different: Japanese dialogue lines come from ElevenLabs into `assets/audio/voice/ja/<Speaker>/<scene>.<beat>.mp3`, and `Game.Advance()` plays whichever file matches the beat. Follow `.claude/skills/voicing-dialogue/SKILL.md` to translate, audition or voice lines; editing a voiced line in `Prologue.cs` makes its Japanese and audio stale. Art in `assets/art/` is imagegen output used unedited (except the chroma-keyed foliage atlas and the split bark atlas); prompts and provenance are in `ASSETS.md`, which should be updated whenever an asset is regenerated.

## Code style

The C# here is deliberately dense: file-scoped `namespace Hoshikawa;`, `ImplicitUsings` and `Nullable` enabled (`null!` for Godot-initialized fields), multiple statements per line separated by `;`, and no blank lines inside methods. Match it. Comments are rare and explain a non-obvious physical or pipeline constraint (e.g. why a jump frame index is what it is), not what the code does.

`AGENTS.md` is a symlink to this file so Codex reads the same instructions; edit `CLAUDE.md` and keep the link. `PLAN.md` tracks completed work as checklists; `README.md` documents controls, the prologue route and per-file notes for players. Both are kept current with the code.
