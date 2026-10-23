---
name: tripo-api
description: Use when working with Tripo 3D (tripo3d.ai) from this repo — signing in to the Tripo API, checking credits, listing or downloading models already generated in the account, or (only when the user explicitly asks) generating, rigging, animating or converting a character via the official tripo-cli or the raw v3 REST API, then bringing the result into Hoshikawa with tools/import_character.py. Keywords tripo, tripo-cli, tripo3d, openapi.tripo3d.ai, tsk_ key, rig, retarget, multiview, Tripo Studio.
---

# Tripo API

Tripo turns text, one image or 2–4 view images into a textured mesh, and can rig and animate it. Mio, Chiyo and KOMA-7 in `assets/models/` all began as Tripo exports. Use the official CLI (`tripo-cli`) for this work. It wraps the v3 REST API, handles uploads and polling, and writes a reproducible `task.json` next to every download.

## Credits: read this first

**Every create call spends credits.** Do not run any of these unless the user has asked, in this conversation, for that exact generation or processing step:

- `tripo make`, `tripo ai`, `tripo redo`, `tripo batch run`
- `tripo generate *`, `tripo model refine|texture|convert|import`, `tripo anim check|rig|retarget`, `tripo mesh *`
- the MCP tool `tripo_make`
- any `POST /v3/generation/*`, `/v3/models/*`, `/v3/animations/*`, `/v3/mesh/*`

These are free and safe at any time: `login`, `whoami`, `balance`, `usage`, `doctor`, `history`, `task get`, `task list`, `task watch`, `task get --download`, `view`, `docs`, `--help`, and `make … --dry-run`. A dry run plans and validates locally without uploading or submitting.

Before a paid step, run `make … --dry-run`, show the user the plan card and the cost, and wait for a yes. Headless runs auto-enable `--yes`, so the CLI will not stop to ask for you. Exit 7 (network) on a submit means the task may already exist. Check `tripo balance` (frozen credits) before retrying, so you never pay twice.

## Setup

Node 20+ is required. Run the CLI without a global install:

```sh
npx -y tripo-cli@latest --version          # v0.7.0 at the time of writing
export TRIPO_ANALYTICS_ENABLED=0           # optional: turn off anonymous telemetry (on by default since 0.7.0)
```

The examples below write `tripo` for `npx -y tripo-cli@latest`. In zsh a `T="npx -y tripo-cli@latest"` variable will not word-split, so use a shell function or the cached bin instead.

### Sign in (device flow, no key handling)

```sh
tripo login --region ov     # international account (tripo3d.ai); use cn for tripo3d.com
```

Without a TTY, the command prints a URL and a one-time code to stderr, then blocks for up to 15 minutes. Run it in the background and relay the URL and code to the user. They open `https://developers.tripo3d.ai/en/cli-auth?code=XXXX-XXXX` on any device, sign in and click Authorize. If the account has no API key yet, one is created; that costs nothing. The key is stored with mode 0600 in `~/.tripo/config.json`, and the CLI prints it only masked.

Never print, log, paste or commit the key. Do not write it into the repo. `TRIPO_API_KEY=tsk_…` overrides the stored profile for CI. Use `--profile NAME` for a second account.

```sh
tripo whoami --json    # masked key, region, balance
tripo doctor --json    # node, key, reachability, config permissions
tripo balance --json   # {"balance":N,"frozen":M}
```

## Find models already in the account

The API has **no "list all my tasks" endpoint**. `tripo task list` with no ids reads only the CLI's local history (`~/.tripo/history.jsonl`), which is empty on a fresh machine. Enumerate the account from its credit-usage log instead, then batch-query those ids:

```sh
tripo usage --limit 200 --json > usage.json                 # [{task_id,type,credits_consumed,created_at},…]
tripo task list $(jq -r '.[].task_id' usage.json) --json    # {"tasks":[…],"missed":[ids not visible to this key]}
tripo task get TASK_ID --step --json                        # one task, with pipeline step details
```

**Tripo Studio models are not visible to the API.** This was checked on 2026-10-16 against Hapadona's account. Characters they had generated in the Studio web app (studio.tripo3d.ai) did not appear anywhere through the API key: `usage` returned `[]`, `balance` returned 0 and `task list` was empty. tripo-cli 0.7.0 has no Studio or library command, and the v3 API has no endpoint for one.

Official sources from 2026-10-16 agree on this:

- The Help Center says Studio and the API each have "independent account benefits and billing". Studio credits cannot pay for API calls.
- None of the 36 API doc pages mentions Studio or a task-list endpoint.

There is no API route to Studio assets. These are the official ways out of Studio:

| route | plan | what the user does | notes |
| --- | --- | --- | --- |
| Studio Export/Download | Free: 15 exports, H2.5 models only, 10 stored models. Pro/Max/Team: unlimited | Export each model to GLB (FBX if rigged) | Not listed as a credit cost in Studio's pricing table |
| DCC Bridge → Blender (add-on 1.0.32, Blender 4.1+) | Pro, Max or Team only | Install the add-on ZIP (from `tripo-public.tripo3d.ai/plugins/blender-bridge/Tripo3d_Blender_Bridge-latest.zip`) in Blender. In Studio, using Chrome, Edge or Opera on the same Mac, click **DCC Bridge**, turn on Blender, pick the animations in Export, then choose Send To → Blender | Studio-made animations come across too. Then save or export from Blender into the importer |
| DCC Bridge → Godot (4.6+) | Pro, Max or Team only | Copy the addon into a project's `addons/` | **Do not use it in this repo.** It adds an editor addon and auto-places assets into the open scene, and Hoshikawa builds everything in C# at runtime. A throwaway Godot project works if needed |

Paid Studio subscribers can claim 1,000 API credits once at `studio.tripo3d.ai/cli`. The API account must use the same email as Studio. Those credits never expire and are not Studio credits. Free Studio plans keep only 1 day of edit history. Pro keeps 7 days, and Max and Team keep it permanently.

The unofficial `Ador-able/tripo-studio-mcp` scrapes Studio's private web API. Do not use it: it is a Codex-only plugin under a proprietary licence, it wants the Studio password, and its credential helper is Windows-only.

**DCC Bridge drops models into the live Blender scene only.** The add-on writes each received FBX to `bpy.app.tempdir`, imports it, and the file is gone after that. The log is `$TMPDIR/tripo3d_blender_bridge.log` and records each model's name and time. Until someone saves, the models exist only in Blender's memory, with textures packed.

Rescue them without touching the user's session. Use the Blender MCP socket (`127.0.0.1:9876`, `execute_code`) to run `bpy.ops.wm.save_as_mainfile(filepath=…, copy=True)`. Then split per character in background Blender. Sora, Nao and Toma came in this way on 2026-10-16; see ASSETS.md.

After export, pass the file straight to the importer below (free). Use `tripo model import FILE` (paid) only when it has to become an API task, for example for API rig or retarget.

## Download an existing result (free)

```sh
tripo task get TASK_ID --download -o artifacts/tripo-account/TASK_ID --json
```

Output URLs expire about 5 minutes after a query. `--download` re-queries the task first, so never cache a URL. The folder gets every output URL field (for example `pbr_model`, `model`, `base_model`, `rendered_image`), a `preview.png` and `task.json`. Always download into `artifacts/`, which is gitignored. Never download straight into `assets/`.

## Generating and processing (paid; only on an explicit request)

```sh
# multiview → model: filename hints front|back|left|right win, otherwise order is front,left,back,right; front is required
tripo generate multiview-to-model front.png left.png back.png right.png [--model tripo-v3.1|tripo-p1|tripo-p2] --json
tripo make front.png left.png back.png right.png --dry-run      # free preview of the plan and cost

# check riggability first (cheap gate), then rig, then animate
tripo anim check TASK_ID --json                                  # {"riggable":true,"rig_type":"biped"}; GLB only
tripo anim rig TASK_ID --rig-type biped --spec tripo|mixamo --out-format glb|fbx --json
tripo anim retarget RIG_TASK_ID --animation preset:idle preset:walk preset:run --animate-in-place --out-format glb --json
```

- Rig model: the CLI defaults to rig v2.0 (wire `v2.5-20260210`), which covers all seven body types (biped, quadruped, hexapod, octopod, avian, serpentine, aquatic). Its presets are `preset:idle walk run dive climb jump slash shoot hurt fall turn`, plus body-specific walks.
- Rig v1.0 (`-p model=v1.0-20240301`, biped only) has 90+ `preset:biped:*` clips: dance, emotes, combat and more. Which presets you can use depends on the rig model of the upstream rig task.
- `retarget` takes a **rig task id only**. It accepts at most 5 animations per task and bills per animation.
- Game code moves Mio, so use `--animate-in-place` for anything that replaces a locomotion clip.
- Any API parameter without a flag goes through `-p key=value` (JSON values allowed).
- The full parameter tables are in `tripo docs --topic commands/generate` and `tripo docs --topic commands/process`.
- Every subcommand's help is captured in `artifacts/tripo-account/cli-help.txt` when that directory exists.

## Raw REST API (when the CLI is not an option)

| | |
| --- | --- |
| Base URL | `https://openapi.tripo3d.ai` (ov) · `https://openapi.tripo3d.com` (cn) |
| Auth | `Authorization: Bearer tsk_…` |
| Envelope | `{"code":0,"data":…,"message":…,"request_id":…}`; any non-zero `code` is an error |
| Free reads | `GET /v3/account/balance` · `GET /v3/account/usage` · `GET /v3/tasks/{id}[?step=1]` · `POST /v3/tasks/list {"task_ids":[…]}` |
| Paid creates | `POST /v3/generation/{text-to-model,image-to-model,multiview-to-model,text-to-image,image-to-image,image-to-multiview,edit-multiview,image-to-splat}` · `/v3/models/{refine,texture,convert,import}` · `/v3/animations/{rig-check,rig,retarget}` · `/v3/mesh/{segment,complete,decimate,uv,smartsegment,edit}` |
| Uploads | `POST /v3/files` returns a `file_…` token; images ≤ 20 MB, models ≤ 150 MB |

`/v3/tasks/list` returns `{"tasks":{id: detail}, "missed":[…]}`, a map rather than an array, despite what the public docs say. Poll `GET /v3/tasks/{id}` until `status` is `success`, `failed` or `banned`. A failed task refunds its frozen credits.

## MCP server for Claude Code

```sh
claude mcp add tripo -- npx -y tripo-cli@latest mcp
```

It runs over stdio using the stored login, or set `TRIPO_API_KEY` in the server env. It exposes the tools `tripo_make` (**paid**), `tripo_task_get`, `tripo_task_wait`, `tripo_balance` and `tripo_history`. Treat `tripo_make` under the credit rules above.

## Into the game

A downloaded GLB/FBX goes through the shared importer, never straight into `assets/models/`:

```sh
python3 tools/import_character.py artifacts/tripo-account/TASK_ID/model.glb --name NAME --height 1.60 \
    --output-root artifacts/NAME-trial            # review a trial first; drop it (and add --force if needed) for the real import
```

Then follow CLAUDE.md: wire the NPC in `World.cs`, play it through `ActorVisual`, record provenance in `ASSETS.md`, and run `IntegrationCheck` both headless and graphically. Tripo rigs come back with their own bone set and count, so a new character needs its own bone-count and clip contract in `IntegrationCheck`. Mio's 41-bone rig and 12 clips are not interchangeable with a fresh Tripo rig. Audit a download first with background Blender: `artifacts/tripo-account/audit_models.py` reports triangles, bounds, bones, clips and textures.
