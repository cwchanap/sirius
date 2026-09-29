# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Hoshikawa — The Offline Valley: a Godot 4.6.2 Mono (C# / .NET 8) action-JRPG prologue. Single outdoor map, one playable character (Mio), three patrol robots, ten NPCs across two villages.

## Commands

```sh
sh run.sh                     # build C#, import assets, launch (the whole loop)
sh run.sh -- --play           # skip the title screen
dotnet build Hoshikawa.csproj # C# only
```

`run.sh` uses `GODOT_MONO`, defaulting to `/Applications/Godot_mono.app/Contents/MacOS/Godot`. The plain `godot` on PATH is a **non-Mono** build and will not run this project.

### Tests

There is no test framework. The whole suite is `scripts/IntegrationCheck.cs`, a single async method that runs inside a real Godot instance, injecting input and asserting against the actual controllers and physics:

```sh
dotnet build Hoshikawa.csproj
$GODOT_MONO --headless --editor --path . --import      # required after any asset change
$GODOT_MONO --headless --path . --fixed-fps 60 -- --self-test
```

Passing prints `SELF_TEST_PASS` and exits 0; failing prints `SELF_TEST_FAIL <exception>` and exits 1. Each assertion logs `CHECK <message>`.

To run a subset, comment out or early-return within `IntegrationCheck.Run` — assertions are sequential statements, not discoverable cases, and later ones depend on earlier player placement. Prefer adding checks next to the related ones rather than at the end.

Run it **both** headless and graphically (drop `--headless`); several past defects only appeared under the Metal renderer. Under a graphical run, OS focus changes release injected input — `Frames()` re-presses held actions and logs `TEST_INPUT_RESTORE`, so don't "fix" a flake by removing that.

A `NU1900` NuGet audit-endpoint warning on build is expected and not a regression.

### Captures and review scripts

```sh
$GODOT_MONO --path . -- --play --capture=res://artifacts/village.png
# viewpoint flags: --overview --combat-view --portrait --portrait-close --komorebi --pass-view --title-view
$GODOT_MONO --path . --script res://tools/review_valley.gd        # six landscape captures (overwrites artifacts/countryside/)
$GODOT_MONO --path . --script res://tools/review_hamlet.gd        # Hoshikawa captures; -- --out=<subdir> --only=square,overview
$GODOT_MONO --path . --script res://tools/review_arrangement.gd   # building/prop placement
$GODOT_MONO --path . --script res://tools/review_tripo.gd         # Mio model and animation poses
```

`artifacts/` is gitignored; it holds captures, logs and per-change evidence dirs. `VERIFICATION.md` records what was actually verified and the environment's limits — update it when claims change, and do not claim a physical keyboard/mouse session for engine-injected input.

## Architecture

**The entire game is built in C# at runtime.** `scenes/Main.tscn` is six lines: a `Node3D` with `scripts/Game.cs` attached. There are no other scenes, no editor-authored nodes, no autoloads, and no `.tres` resources. Do not reach for the Godot editor to add content — add it to the constructing code.

- `Game.cs` — root `Node3D`. Owns `GameMode` (Title/Playing/Dialogue/Paused/Defeated), quest stage and objective text, the camera rig (`Node3D` → `SpringArm3D` → `Camera3D`), lock-on, dialogue, toasts, effects (`Burst`/`Slash`/`FloatingNumber`), audio, screenshots, and the input map. `InputBindings()` registers every action in code — `project.godot` has no `[input]` section.
- `World.cs` — deterministic environment and both villages. Nothing here is random at runtime (`rng` is seeded 2189).
- `Architecture.cs` / `Hamlet.cs` — `partial class World`. `Architecture.cs` holds the curved-roof generator (`Roof`, `RoofY`) and the `Minka()` timber-frame kit for the six Hoshikawa styles. `Hamlet.cs` holds the brook, footbridge, terrace wall, shrine, `StoneLantern()` and the village `Dressing()`. The Komorebi buildings still use the older `House()`.
- `Player.cs` / `Enemy.cs` — `CharacterBody3D` controllers holding all action timing as float countdowns ticked in `_PhysicsProcess` (attack, dodge, guard, deflect window, posture, stun, invulnerability, cooldowns). Combat tuning lives inline in these two files; there is no data/config layer.
- `ActorVisual.cs` — thin `AnimationPlayer` wrapper. Finds the animator under an imported GLB, normalizes clip names (`some/path/run.001` → `run`), and sets loop modes. Every character goes through it; `Play(clip, speed, restart)` is the only animation entry point.
- `Hud.cs` — a single `Control` drawn entirely in `_Draw()` (immediate mode). Title screen, vitals, minimap, dialogue and perilous-attack diamond are draw calls, not nodes; `Game._Process` calls `QueueRedraw()` each frame. Menus are the one exception: `RefreshMenu()` builds real `Button` nodes.

### Invariants worth knowing before editing the world

1. **`World.Height(x, z)` is the single source of truth for terrain.** The visible mesh, its trimesh collider, and every placement call read the same static function, so geometry and collision cannot drift. Placing anything means calling `Height()` — never hardcode a Y. Adding a landform means editing `Height()`, which moves collision with it.
2. **`World.RoadX(z)` and `terrain.gdshader` describe the same road curve in two languages.** Change one and you must change the other, or the painted road separates from the placement guide. The same applies to `RegionAt()`'s Z thresholds and the region names used in `Game._Process` toasts and the HUD minimap.
3. **Hoshikawa's layout is data.** The `World.Hamlet` table (name, centre, size, yaw, style) drives the levelled house pads inside `Height()`, the `Minka()` buildings, the shrubs and the stepping stones. Move a house by editing the table, never one placement. `World.BrookX(z)` is shared by the `Height()` channel, the water strip, the footbridge and the minimap line.
4. **Seeded streams are pinned.** The countryside and northern valley draw from `rng` (seed 2189), and `IntegrationCheck` pins the first boulder's exact position. Hoshikawa's dressing draws only from `villageRng` (seed 3301). `Tree()` always consumes exactly three draws from whichever generator it is given, so changing a species or model never shifts later scatter.

Collision layers: **1** world/static geometry, **2** player, **4** enemies. `World.Box(..., solid: true)` is the standard way to add a static collider alongside a mesh.

### Character assets

GLBs under `assets/models/` are **pre-exported and committed**; `editor/import/blender/enabled=false`, so Godot never touches Blender. `.blend` sources in `assets/source/` are gitignored and live only on this machine.

For new models use `python3 tools/import_character.py SOURCE --name NAME --height METRES`. It accepts `.blend/.glb/.gltf/.fbx`, preserves source files, textures and bound clips, and writes the GLB, `assets/source/NAME-game.blend`, `assets/art/NAME-import/` and `artifacts/model-imports/NAME/build.json`. Existing outputs require `--force`; `--output-root` supports trial exports. Chiyo/KOMA-7 scripts are compatibility presets of this importer; Mio's combat retargeting remains specialized. See README's import section for selection/idle/reduction options and the runnable `tools/check_character_import.py` regression check. New NPCs still need explicit `World.cs` wiring and runtime checks.

- `mio.glb` — the user's Tripo import: 41 bones, 12 clips (4 supplied + 8 retargeted combat clips). Rebuilt by `blender --background --factory-startup --python tools/import_tripo_mio.py`, audited by `python3 tools/check_mio_export.py`.
- `chiyo.glb` — the user's Tripo elder: 41 bones, one authored `idle` clip (the source supplied no actions), 1.58 m tall. Built by `tools/import_tripo_chiyo.py` in background Blender; source `assets/source/chiyo-tripo-original.blend` is preserved. Only Chiyo uses this GLB; the other human NPCs retain the shared villager rig.
- `koma7.glb` — the user's Tripo gardener robot: 65 bones, one authored six-second head-turn `idle`, 1.25 m tall. Built by `tools/import_tripo_koma7.py` in background Blender; the original source and all 96,316 triangles/four textures are preserved. Only KOMA-7 uses it; hostile robots retain `patrol.glb`.
- `villager.glb` / `patrol.glb` — authored humans: 58 bones, 12 clips. Built by `tools/refine_humans.py` (+ `mio_illustrated.py`, `mio_turnaround.py`) inside Blender. `build_characters.py` is legacy and recreates the *old* Mio — running it requires re-running `import_tripo_mio.py` afterward.

`IntegrationCheck` asserts exact bone counts (Mio/Chiyo 41, KOMA-7 65, shared humans 58), specific bone names, Mio's 12 clips, Chiyo/KOMA-7's looping idles and textured materials, skeleton deformation and grounding. KOMA-7 also has checks for fitted collision, idle duration and stationary hips/feet. Any re-export must keep the corresponding character's contract or the self-test fails.

Trees and shrubs (`assets/models/props/*.glb`) have no `.blend` source. `/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_props.py` regenerates them deterministically. They carry material *names* only (`bark_<species>`, `foliage`); `World.Tree()` binds the bark textures and `foliage.gdshader` by those names. Newly imported textures default to `mipmaps/generate=false`: set it to true in their `.import` for anything that tiles or is alpha-cut.

Audio is synthesized by `python3 tools/build_audio.py` (stdlib only). Art in `assets/art/` is imagegen output used unedited (except the chroma-keyed foliage atlas and the split bark atlas); prompts and provenance are in `ASSETS.md`, which should be updated whenever an asset is regenerated.

## Code style

The C# here is deliberately dense: file-scoped `namespace Hoshikawa;`, `ImplicitUsings` and `Nullable` enabled (`null!` for Godot-initialized fields), multiple statements per line separated by `;`, and no blank lines inside methods. Match it. Comments are rare and explain a non-obvious physical or pipeline constraint (e.g. why a jump frame index is what it is), not what the code does.

`PLAN.md` tracks completed work as checklists; `README.md` documents controls, the prologue route and per-file notes for players. Both are kept current with the code.
