# Asset provenance

All game assets were produced for this project. Art direction uses original warm, painterly anime JRPG imagery; combat references posture and timed-deflection mechanics.

## Current Mio: user-provided Tripo import

The user generated and imported this replacement through Tripo. It supersedes the authored Mio described in the historical sections below. The exact Tripo generation settings are not recorded here.

- Original: `assets/source/mio-tripo-original.blend`, 942,240 vertices / 1,884,692 faces, 41 bones, four supplied clips: idle, walk, run, jump.
- Runtime: `assets/models/mio.glb`; editable game copy: `assets/source/mio-tripo-game.blend`. 150,600 triangles including the retained sword, 1.84 m tall, four supplied clips plus eight retargeted combat clips.
- Textures: `assets/art/mio-tripo-basecolor.jpg` and `mio-tripo-normal.png`, copied byte-for-byte from the import's packed 4096×4096 textures. Both are embedded in the GLB.
- Build: `tools/import_tripo_mio.py`; audit: `tools/check_mio_export.py`. Horizontal hip offsets are removed to keep the model aligned with the C# controller. Reduced-mesh corner normals are recalculated; no texture painting was performed.
- Previous model backup: `artifacts/tripo-integration/before/mio.glb`; its editable source remains `assets/source/characters.blend`.
- The imported rig has no finger bones or independently rigged coat/hair. Open-hand sword contact and cloth deformation in extreme combat poses remain limitations of this integration.

## Chiyo: user-provided Tripo NPC — 2026-09-16

- Original import preserved from live Blender: `assets/source/chiyo-tripo-original.blend` (41 bones, 968,939 vertices, 1,937,931 faces, no supplied actions). The default cube/camera/light remain only in that untouched source backup.
- Runtime: `assets/models/chiyo.glb`, 89,999 triangles, 1.58 m tall; current editable copy: `assets/source/chiyo-game.blend`. Build with `python3 tools/import_tripo_chiyo.py`, a preset of the shared background-Blender importer. The earlier `chiyo-tripo-game.blend` is retained.
- Four original packed 4096×4096 JPEGs are copied unchanged to `assets/art/chiyo-tripo-{basecolor,normal,roughness,metallic}.jpg`. The GLB embeds unchanged base color and normal images; Blender combines roughness/metallic channels for glTF material use. Mesh corner normals are recalculated after reduction, and the exporter normalizes the four strongest joint weights.
- A new four-second breathing `idle` uses the existing villager idle's small spine rotations and a slight counter-motion of the head. Feet and hips stay still. This is an authored idle, not an animation supplied by Tripo; Chiyo does not require the playable character's twelve combat clips.
- Chiyo alone uses this model, facing the village road at her existing position. Her dialogue remains unchanged; label and collision height fit her smaller stature. Mio, the shared villager and patrol GLBs retain their previous hashes.
- Evidence: `artifacts/chiyo-integration/` contains source hashes, build/export reports, headless and native test logs, and five native captures.

## KOMA-7: user-provided Tripo NPC — 2026-09-20

- Original live Blender import preserved as `assets/source/koma7-tripo-original.blend`: 48,138 vertices, 96,316 triangles, a 65-bone Mixamo rig and no supplied actions. The source backup retains the original scene, including the default cube/camera/light.
- Runtime `assets/models/koma7.glb` and current editable `assets/source/koma7-game.blend` are built by `python3 tools/import_tripo_koma7.py`, a preset of the shared background-Blender importer. The earlier `koma7-tripo-game.blend` is retained. All mesh geometry is retained, scaled to 1.25 m with soles at ground level. The game copy removes the `mixamorig:` bone-name prefix; skin weights remain bound to the same bones.
- Four packed 4096×4096 textures are copied unchanged to `assets/art/koma7-tripo-{basecolor,roughness,metallic}.jpg` and `koma7-tripo-normal.png`. The GLB embeds the exact base-color and normal bytes; Blender combines the roughness and metallic maps for glTF.
- A new six-second `idle` gently turns the head by up to four degrees. The torso, reservoir, hips and feet stay still. This idle is authored for the stationary village NPC; the import supplied no animations.
- KOMA-7 alone uses the new model, original materials, fitted collision capsule and lower name label. His existing position, gardener dialogue and village role are retained. The three hostile robots still use `patrol.glb`; Mio, Chiyo and the shared villager GLBs are unchanged.
- Export audit, source hashes, 138 passing headless/native checks and six Godot captures are in `artifacts/koma7-integration/`.

The shared importer rebuilds these two assets from their original sources. Current byte-preserved texture copies live under `assets/art/chiyo-import/` and `assets/art/koma7-import/`, with content hashes in filenames; the previous `*-tripo-*` texture files remain retained. Current export reports are `artifacts/model-imports/{chiyo,koma7}/build.json`; importer regression and Godot revalidation evidence are under `artifacts/generic-import/`. No new image art is generated by this refactor.

## Generated image art

Generated with the built-in `image_gen` tool, then copied into the project without raster edits.

- `artifacts/npc-image-to-3d-v1/`: new Ren (engineer), Aki (medic), and Chiyo (elder) references. Each has a 1024×1536 single front image and a 1774×887 front/profile/back sheet generated from that front. Original PNGs, exact prompts, hashes, and a downloadable ZIP are retained in the pack. These are proposed designs for user-run 3D conversion; the runtime NPC models are unchanged.
- `artifacts/npc-image-to-3d-v2/`: separate front/left/right/back PNGs for each of those three NPCs, grouped in character folders and individual ZIPs. Fronts are preserved from v1; nine new single-angle images were generated with built-in imagegen. All twelve images are 1024×1536. Exact prompts and hashes are retained alongside the files.
- `artifacts/npc-image-to-3d-v3/`: four separate views each for the seven remaining village residents: Hana (farmer), Sora (courier), KOMA-7 (gardener robot), Nao (lodge keeper), Emi (mountain medic), Toma (orchard keeper), and Yui (pass courier). Generated on 2026-09-16 with built-in imagegen: seven front masters followed by profiles and rear views using the earlier images as references. All 28 original 1024×1536 RGBA PNGs are preserved without raster edits, with one four-PNG ZIP per character and a combined ZIP. `README.md` links every image; `generation-records.json` records exact prompts, inputs, original paths, dimensions and SHA-256 hashes. These illustrated references retain minor cross-view variations and have not been converted into runtime models.

- `assets/art/material-atlas.png`: 2×2 grass / earth / masonry / roof atlas; sampled by the terrain and building shaders.
- `assets/art/mio-keyart.png`: Mio and the valley; title background and HUD portrait region.
- `assets/art/amber-iris.png`: generated painted iris artwork, UV-mapped into the shared villager model and embedded in its GLB.

- `assets/art/mio-turnaround.png`: preserved front/back design illustration; the current garment geometry uses a fabric atlas.
- `assets/art/mio-face-diffuse.png`: preserved original face atlas with painted eyes, cheeks, nose and lips.
- `assets/art/mio-skin-painted-v3.png`: preserved skin-only atlas, generated from the original face atlas with the head sheet as a complexion reference. Painted eyes and brows were removed for continuous upper-face mapping.
- `assets/art/mio-skin-painted-v5.png`: preserved previous skin atlas. Built-in imagegen removed the pointed lip outline, retained a faint lip tint, and added restrained nose-plane shading while preserving the UV layout. Exact prompt: `artifacts/face-finish/imagegen.json`. Generated pixels are preserved unchanged.
- `assets/art/mio-skin-painted-v4.png`: previous skin atlas. Built-in imagegen removed the vertical nose mark and softened lip color; modeled nasal wings, nostril marks and lip cushions now carry those features. Exact prompt and input/output paths: `assets/art/mio-skin-painted-v4-generation.json`. Generated pixels are preserved unchanged.
- `assets/art/mio-hair-diffuse.png`: preserved earlier hair texture.
- `assets/art/mio-hair-soft.png`: preserved earlier midnight hair texture.
- `assets/art/mio-fabric-soft.png`: preserved previous teal, ivory, crimson and leather atlas. Revision references and direction are recorded in `assets/art/mio-shape-materials.md`.

The three original Mio texture prompts are recorded verbatim in [mio-generation-prompts.md](assets/art/mio-generation-prompts.md). Each generation referenced the original `mio-keyart.png`; the resulting files are 1254×1254 PNGs. Textures are embedded in the GLB and packed into the editable Blender file. UV layout changes do not alter the raster source art.

### Material atlas prompt

Use case: stylized-concept. Asset type: game material texture atlas for a 3D anime JRPG rural Japanese village. Create one square 2048x2048 texture atlas divided into four perfectly equal square quadrants, edge to edge, no margins, no text, no borders. Top left: seamless richly detailed hand-painted meadow grass, tiny green blades, clover, subtle dark moss, varied sage and olive greens, no large flowers. Top right: seamless warm ochre packed earth footpath, small pebbles, worn granular soil and pale dusty mottling. Bottom left: seamless old warm grey irregular cut stone masonry with fine cracks and soft moss in seams, subtle surface grain. Bottom right: seamless terracotta Japanese ceramic roof tiles, repeating orderly overlapping rows, fine handmade variation, weathering and muted red brown glazed edges. Each quadrant is a flat orthographic PBR base color texture, lighting neutral and evenly diffuse, no perspective, no cast shadows. Painterly detailed game texture finish, attractive restrained natural palette, readable fine organic detail. No objects, logos, labels, watermarks.

### Key art prompt

Use case: stylized-concept. Asset type: polished illustrated title screen artwork for an original 3D action JRPG. Wide landscape image. Japanese anime RPG key visual with graceful detailed linework and luminous painterly colors. On the right third: Mio, an adult 22 year old Japanese woman with amber eyes, dark navy black hair tied in a short ponytail with layered bangs, a fitted petrol teal traveling jacket with brass piping and ivory shirt, vermilion red scarf, practical dark trousers and brown knee high boots, a worn utility satchel, antique gold wrist terminal with turquoise glow, holding a slim single-edged sword lowered at her side. Full upper body and head visible, looking thoughtfully toward a sunlit Japanese countryside village. Background: mossy stone bridge over bright river, timber and plaster houses with terracotta roofs, rice fields, rolling green forest mountains, distant abandoned rail viaduct and one tiny futuristic city tower suggesting 22nd century. Soft wind, floating leaves, late afternoon sun rays. Composition leaves left 45 percent gently shaded scenic empty space for title UI. Art style is refined detailed anime JRPG promotional illustration, original character, warm natural colors contrasting jade technology, inviting pastoral wonder tinged with melancholy. No text, logos, watermarks, interface, or lettering.

### Amber iris prompt

Use case: stylized-concept. Asset type: UV texture for the iris of an original anime-style 3D JRPG human character. Create a square flat texture containing exactly ONE perfectly centered large circular golden amber iris, taking up 92 percent of the square width. Pure warm ivory outside the circle. The iris has deep brown outer limbal ring, rich amber-orange and honey-gold radial streaks, a dark near-black perfectly round pupil at the center around 30 percent of iris diameter, delicate layered cel-painted gradients, subtle warm ochre starburst fibers. Two small ivory reflected light glints in upper left of iris, one small secondary glint lower right. Front orthographic view, perfect radial circle, no perspective, no eyeball shading. Elegant detailed hand-painted Japanese anime game eye artwork, crisp at small scale, beautifully colored. No eyelids, skin, eyebrows, eyelashes, face, text, border, logo, watermarks, or multiple eyes. This will be mapped onto a circular 3D iris surface so the circle must be centered precisely.

## Hoshikawa hamlet textures — 2026-09-29

Generated with `codex exec` gpt-image; exact prompts and raw-output hashes are in `assets/art/hamlet-textures-generation.md`.

- `assets/art/village-atlas.png`: 2×2 thatch / dark cedar boards / earthen plaster / grey kawara atlas used by the Minka kit in `scripts/Architecture.cs`. Red roofs are the kawara cell tinted in the shader; the pixels are unchanged.
- `assets/art/foliage-atlas.png`: 2×2 keyaki / sakura / sugi / bamboo leaf-cluster sprites generated on a #0000FF key; the key was removed with `remove_chroma_key.py --auto-key border --soft-matte --despill --spill-cleanup` (RGBA, corners alpha 0). Sampled by `assets/shaders/foliage.gdshader` on the tree leaf cards.
- `assets/art/bark-{keyaki,sugi,sakura,bamboo}.png`: the four unchanged 627×627 quadrants of one generated bark atlas.

## Tree models — 2026-09-29

- `assets/models/props/{keyaki_a,b,c, sugi_a,b,c, sakura_a,b, bamboo_a, shrub_a,b}.glb`, 400–2,920 triangles each, built procedurally and deterministically by `tools/build_props.py` in background Blender 5.2 (no source .blend). They carry geometry, custom normals, UVs into the foliage atlas, and vertex colour (R wind sway, G ambient); materials are names only (`bark_<species>`, `foliage`) and `World.Tree()` binds the textures above.

## Blender models

Created and exported through the connected Blender MCP, using `tools/build_characters.py` and the updated human authoring pipeline in `tools/refine_humans.py` and `tools/mio_illustrated.py`. Editable source: `assets/source/characters.blend`. Runtime exports: `mio.glb`, `villager.glb`, `patrol.glb`. Meshes, materials, skinning and animation keys are original authored procedural Blender geometry. Human NPCs share the villager base with palette/scale variants; KOMA is a friendly recolor of the robot base. Each rig includes idle, walk, run, three attacks, guard, dodge, jump, skill, stagger and death clips.

Mio now has fuller middle/lower cheeks and a softer jaw transition while retaining the chin height, eye width, recessed sockets and small pupil footprint. The closed crown, gathered ponytail, tapered fringe and painted face accents from the prior refinement remain. Original Mio artwork is the identity baseline. [Comparison](artifacts/cheek-jaw-volume/comparison.html) · [Review](artifacts/cheek-jaw-volume/REVIEW.md).

## Dialogue portraits — 2026-10-16

`assets/art/portraits/` holds the visual-novel portraits, cut out of existing art with `tools/cut_portrait.gd` rather than generated anew. The tool removes the flat white background, keeps any transparency the source already had, and crops to the character.

- `mio.png`: the front three-quarter view of `assets/art/mio-turnaround-body-v1.png`, cut with `--rect=1070,0,350,660 --flip` so she faces right. 318×636.
- Residents: to be cut from the front views in `artifacts/npc-image-to-3d-v2/` and `-v3/`. `portraits-sources.md` will record each source file, its hash and any image that had to be generated.

Zero has no portrait by design: nobody in the valley has seen its body.

## Audio and fonts

Original synthesized audio generated by `tools/build_audio.py`; no sampled commercial recordings. UI uses Godot's bundled fallback font and available system serif fonts; no system font binaries are redistributed.

## Narrative source

The user's “Story Main line” conversation, latest dual-body countryside/city revision: 6a75647c-8d84-83e8-b467-45bf0fb6b41e. The village name, residents, localized prologue dialogue and relay mission details are MVP adaptations. Zero is not the village gardening robot and does not possess Mio.


## Turnaround-led Mio revision

- `assets/art/mio-turnaround-body-v1.png`: front, left profile, back and three-quarter full-body construction sheet.
- `assets/art/mio-turnaround-head-v1.png`: matching head views and the reference for Mio's eye design.
- `assets/art/mio-iris-painted-v1.png`: preserved first dedicated amber iris texture, replacing the former roughly 28×30-pixel head-sheet crop. Exact prompt: `artifacts/lid-jaw/iris-prompt.txt`.
- `assets/art/mio-iris-painted-v2.png`: preserved previous 1254×1254 amber-brown iris. Exact prompt: `artifacts/mouth-chin/iris-prompt.txt`.
- `assets/art/mio-iris-painted-v4.png`: preserved previous 1254×1254 iris, generated with imagegen from the original head guide. Smaller, softly rounded pupil, dark upper iris, subdued amber lower half and a small glint. Original generated PNG bytes are preserved. Exact prompt and provenance: `assets/art/mio-iris-painted-v4-generation.json`. The intervening v3 trial is retained but unused.
- `assets/art/mio-iris-painted-v5.png`: preserved previous 1254×1254 amber iris, generated with imagegen using v4 and the official Elaine screenshot as references. Dark upper band and pupil, pale honey lower crescent, clear primary catchlight. Generated pixels are unchanged; UVs stay inside the painted circle. Exact prompt, inputs and hash: `assets/art/mio-iris-painted-v5-generation.json`.
- `assets/art/mio-iris-painted-v6.png`: preserved previous amber iris, edited with built-in imagegen using v5 and the official Elaine screenshot. More compact pupil and broader upper-left reflection. Generated PNG bytes are unchanged. Exact prompt: `artifacts/lid-structure/iris-prompt.txt`; inputs and hash: `assets/art/mio-iris-painted-v6-generation.json`.
- `assets/art/mio-iris-painted-v7.png`: preserved previous amber iris, edited with built-in imagegen from v6. Smaller, rounder pupil and reduced surrounding upper shadow; iris dimensions, UVs and geometry are unchanged. Generated PNG bytes are preserved. Exact prompt: `artifacts/pupil-balance/iris-prompt.txt`; provenance: `assets/art/mio-iris-painted-v7-generation.json`.
- `assets/art/mio-fabric-turnaround-v1.png`: current coat, blouse, scarf and leather atlas.
- `assets/art/mio-turnaround-landmarks.json`: approximate manually identified pixel landmarks and calibration notes.
- `assets/art/mio-turnaround-generation.json`: exact built-in imagegen prompts, input baseline and asset purposes.

The sheets were generated first from the original Mio key art. They have small cross-view inconsistencies; the head sheet governs the face and the body sheet governs costume placement. They are not exact scans or a finished 3D result. The previous skin texture is retained.

`assets/art/mio-hair-painted-v2.png` is a preserved previous hair base-color texture, generated with the built-in imagegen tool from the original hair map and head-sheet reference. It replaces fine photographic strand noise with grouped dark violet/navy painted highlights. Its exact prompt and input paths are recorded in `assets/art/mio-hair-painted-v2-generation.json`; the original `mio-hair-soft.png` is preserved.


Current texture refinement uses built-in imagegen edits, copied unchanged: `assets/art/mio-hair-painted-v3.png` removes repeated violet bands; `assets/art/mio-iris-painted-v8.png` adds finer amber shading while preserving the small pupil footprint; `assets/art/mio-skin-painted-v6.png` adds restrained nose-plane tint. Exact prompts, inputs, original generated paths and SHA-256 hashes are recorded in [imagegen provenance](artifacts/hair-refinement/imagegen.json). Fabric remains `mio-fabric-turnaround-v1.png`.
