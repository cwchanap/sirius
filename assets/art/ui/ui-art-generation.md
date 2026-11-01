# HUD icons and title logo — generation record

Generated 2026-10-24 with `codex exec` (codex-cli 0.163.0) calling its built-in image tool, on the owner's Mac, as the art task of the UI redesign (less text, more art). Each prompt asked for white shapes on flat black, so `tools/cut_ui_art.gd` could turn brightness into alpha and the HUD can tint each image by modulating it. Raw outputs, prompts and Codex logs are in the gitignored `artifacts/ui-art/` on the owner's machine.

## Runs

Each run read its prompt on stdin, with one line appended telling Codex where to put the file:

```sh
D=$PWD/artifacts/ui-art
{ cat $D/prompts/NAME.txt; printf '\nThen copy the generated image to %s/%s.png and print the path.\n' "$D" NAME-1; } \
  | codex exec --skip-git-repo-check -s workspace-write -C "$D" - > $D/NAME-1.log 2>&1
```

The two logo runs appended "unchanged: do not resize, recolour, threshold, crop or otherwise edit it with code" to that line, after the solid sheet's run had post-processed its output.

| Output | Accepted | Raw | Raw sha256 |
|---|---|---|---|
| Solid icon sheet | yes | `icons-solid-1-raw.png`, 1254×1254 RGB | `5765cdfb721ec1f94eb30d3c72ab55ef16cf0df99a4ab1c9aabd5e76c95289ac` |
| Vertical logo | yes | `logo-vertical-1.png`, 1536×1024 RGB | `7a456a65de2dca7b26b2f1e1ac10fef200f7237ecfc281942aab863b06c653de` |
| Line icon sheet | no: its strokes thin out at the HUD's 28 px | `icons-line-1.png`, 1254×1254 RGB | `5c0463c7431bf6a72e0d41141b9dec79059bff54713e885428f1d879337fbf41` |
| Horizontal logo | no: its HOSHIKAWA has a dot over the I | `logo-horizontal-1.png`, 1536×1024 RGB | `d1b695201059e2f9610a19e3ea40b5bd0ec6010c7fab4dc8f5a0c17bcb4bbf71` |

The solid sheet took three generations: the first drew four arcs instead of three on both Zero emblems, the second let the last danger icon cross into its cell's top edge, and the third is the raw used here, byte-identical to its file in `~/.codex/generated_images/`. Codex then resized it to 1024, thresholded it to pure black and white and repacked the icons on its own (`icons-solid-1.png`, sha256 `119ef442d2c09e5efd3bb71336b21ea2e405ef6580edbdb97b499f5725caa44b`); that copy is not used, because its edges lost their anti-aliasing. The horizontal logo was generated before Codex hit its usage limit and was copied unchanged from `~/.codex/generated_images/`. Neither sheet is strictly two-coloured: the edges are anti-aliased grey, which the cut keeps as soft alpha.

## Cut

```sh
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=artifacts/ui-art/icons-solid-1-raw.png --out=assets/art/ui \
  --names=zero,resonance,remedy,talk,repair,ladder,,perilous,finish,stunned,shutdown,deflect,posture_break,,sirius,
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=artifacts/ui-art/logo-vertical-1.png --out=assets/art/ui/logo.png
```

Every output is white RGBA whose alpha is the raw's brightness (8 or less clear, 247 or more opaque). Each icon is its cell's drawn part, scaled by Lanczos to fit 232 px and centred in 256 px. The logo is cut to what it draws plus 16 px, 1330×855, at its own resolution. All of them import with mipmaps, since the HUD draws them far smaller.

The sheet's cells 7, 14 and 16 were alternatives and are not used: the SIRIUS star in a hexagon (the wolf head was picked, for the heavenly-wolf star), the eye in signal arcs (the ring and dot was picked, closest to the link emblem the dialogue already draws) and the exclamation mark in a jagged diamond (危 was picked).

| File | Cell | Drawn | For |
|---|---|---|---|
| `zero.png` | 1 | a dot inside three arcs | Zero's link, on the minimap and in dialogue |
| `resonance.png` | 2 | a katana through sound-wave rings | Resonance Cut |
| `remedy.png` | 3 | a drawstring pouch with a leaf | remedies, the remedy pouch |
| `talk.png` | 4 | a speech bubble | talking to a resident |
| `repair.png` | 5 | a wrench over a cog | the manual relay |
| `ladder.png` | 6 | a ladder | the teahouse ladder |
| `perilous.png` | 8 | 危 in a spiked circle | a perilous attack |
| `finish.png` | 9 | a sword through a diamond | a robot whose posture is broken |
| `stunned.png` | 10 | a spiral with sparks | a stunned robot |
| `shutdown.png` | 11 | a power symbol split by a dagger | Mio behind a robot |
| `deflect.png` | 12 | a spark where two blades cross | a deflect |
| `posture_break.png` | 13 | a shattered diamond | Mio's posture broken |
| `sirius.png` | 15 | a wolf head with a star | SIRIUS robots |
| `logo.png` | — | 星川 in brush strokes, HOSHIKAWA, THE OFFLINE VALLEY | the title screen |

## Prompts

### icons-solid (accepted)

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1024 square image: a game UI icon sheet of 16 icons arranged in an exact 4 by 4 grid. Each icon sits centred in its own equal 256x256 cell and fills about 70% of the cell, with clear black space around it; icons never touch or cross into neighbouring cells. No grid lines, no cell borders, no labels, no numbers and no text anywhere, except the single kanji inside icon 8.
Colours: pure white (#FFFFFF) shapes on a flat pure black (#000000) background. Strictly two colours: no grey, no gradients, no shading, no glow, no texture, no drop shadow.
Style: Japanese family-crest (kamon) inspired SOLID icons for the HUD of an action JRPG set in a rural Japanese valley under watch of a hostile AI. Filled white silhouettes with inner details cut out as black negative space, the way a kamon crest is carved; the same visual weight across every icon; simple and bold enough to read at 32 px. No circle or badge behind an icon unless its description asks for one.
The 16 icons, row by row, left to right:
Row 1: (1) Zero's link emblem: a small solid dot at the centre surrounded by three separate curved arc segments evenly spaced on a ring, like a pulsing signal. (2) Resonance Cut: a straight katana blade slashing diagonally through two concentric sound-wave arcs. (3) Remedy: a small drawstring cloth medicine pouch with a single leaf mark on it. (4) Talk: a rounded speech bubble with a small tail at the lower left.
Row 2: (5) Repair: a wrench crossed over a cog wheel. (6) Ladder: a short wooden ladder seen straight on, two rails and four rungs. (7) SIRIUS crest: a sharp four-pointed star inside a hexagonal mechanical frame, cold and machine-like. (8) Danger: the kanji 危 inside a circle with a jagged, spiky outer edge.
Row 3: (9) Finishing blow: a sword pointing straight down, piercing a diamond shape. (10) Stunned: a spiral swirl with three small sparks around it. (11) Shut down: a power symbol (a circle open at the top with a vertical bar through the gap) split by a small dagger blade. (12) Deflect: a starburst spark where two crossing blades meet.
Row 4: (13) Posture broken: a diamond cracked into shards with small gaps between them. (14) Zero emblem, alternative: an eye shape inside a ring of three signal arcs. (15) SIRIUS crest, alternative: a stylised wolf head in profile facing left with a four-pointed star on its brow (SIRIUS is the heavenly-wolf star). (16) Danger, alternative: a bold exclamation mark inside a jagged diamond, no kanji.
```

### logo-vertical (accepted)

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1536x1024 landscape image: the title logo of a Japanese action JRPG. Pure white (#FFFFFF) on a flat pure black (#000000) background, strictly two colours: no grey, no gradient, no glow, no texture, no border, no watermark, no red seal stamp.
Layout: the two kanji 星川 written large in expressive Japanese brush calligraphy (sumi brush, confident strokes with dry-brush streaks at the stroke ends), stacked vertically (星 above 川) on the left half, together about 75% of the image height. To their right, vertically centred against the kanji and left-aligned: the word HOSHIKAWA in widely letter-spaced elegant serif capitals, a thin rule with a tiny diamond under it, and under that THE OFFLINE VALLEY in much smaller widely letter-spaced serif capitals. A generous black margin on every side. No other text. The kanji must be exactly 星 and 川, correctly written.
```

### icons-line (not used)

The same as icons-solid except its style paragraph:

```
Style: Japanese family-crest (kamon) inspired LINE icons for the HUD of an action JRPG set in a rural Japanese valley under watch of a hostile AI. Clean geometric construction, outlines only, one consistent stroke weight (about 14 px at this size) across every icon, rounded stroke ends, simple and bold enough to read at 32 px. No circle or badge behind an icon unless its description asks for one.
```

### logo-horizontal (not used)

The same as logo-vertical except its layout paragraph:

```
Layout: the two kanji 星川 written large in expressive Japanese brush calligraphy (sumi brush, confident strokes with dry-brush streaks at the stroke ends), side by side horizontally, together about 55% of the image width, centred. Under them a thin horizontal rule with a tiny diamond at its centre. Under the rule the word HOSHIKAWA in widely letter-spaced elegant serif capitals, and under that THE OFFLINE VALLEY in much smaller widely letter-spaced serif capitals, all centred. A generous black margin on every side. No other text. The kanji must be exactly 星 and 川, correctly written.
```

# Dialogue box and Starwell art — generation record, 2026-11-01

Generated with `codex exec` (codex-cli 0.166.0, `--model gpt-6.1-terra -c model_reasoning_effort=xhigh`) calling its built-in image tool on the owner's Mac, after she found the lacquer dialogue box and Starwell screen too plain and asked for more image art and less text. Each prompt was run the same way as above, its output copied unchanged from `~/.codex/generated_images/`. Raw outputs, prompts and logs are in the gitignored `artifacts/ui-art-2/` of a worktree on the owner's machine.

| Output | Accepted | Raw | Raw sha256 |
|---|---|---|---|
| Starwell painting | yes | `starwell-1.png`, 1536×1024 RGB | `b5317be038571025cd80b55ac10051eab0eba73ceeddcf25871c08797e6ee926` |
| Lacquer surface | yes | `lacquer-1.png`, 1536×1024 RGB | `a371e8f7aad94deea6c830c5b04dfdcd68034ca0e28e6142fba2b222d75b5132` |
| Maki-e corner | yes | `corner-1.png`, 1254×1254 RGB | `aff5d4660d026337005b03c87f7d80ed35fdf3f6b83235c8992283d4bdae07c2` |
| Crest ring | yes | `crest-1.png`, 1254×1254 RGB | `dbfb6441ecf5fa354106d8cd2ef419c88c5d62bb1e92373e7d4d10a32c6df35d` |
| Divider | yes | `divider-1.png`, 1536×1024 RGB | `ac083e85fb70d323f2fd1443d0e25e314202de9cf63a2cdff6ede5188c2499bf` |
| Icon sheet | yes | `icons-2.png`, 1254×1254 RGB | `d6938a626e9d6994d587c2a00700eabdcf217d4ecaa8d13aa7c0f279ac3bb454` |
| Icon sheet | no: the sandal's speed line crossed into the next cell and the staff touched its cell's top | `icons-1.png`, 1254×1254 RGB | `0ca6da1e954559faaef87057790f31ff870ddd992bad2eb4d3f24002ab5f9a13` |

The two paintings are committed as generated (`starwell.png`, `lacquer.png`); the HUD crops each to the panel it fills. The gold pieces were drawn on black and cut keeping their colour, the icons as before:

```sh
R=artifacts/ui-art-2
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=$R/icons-2.png --out=assets/art/ui \
  --names=hp,sp,atk,def,raise,buy,level,runner,engineer,elder,medic,farmer,courier,gardener,lodge,orchard
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=$R/corner-1.png --out=assets/art/ui/corner.png --colour --fit=512
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=$R/crest-1.png --out=assets/art/ui/crest.png --colour --whole --fit=384
$GODOT_MONO --headless --path . --script res://tools/cut_ui_art.gd -- --src=$R/divider-1.png --out=assets/art/ui/divider.png --colour --fit=1024
```

`--colour` keeps each pixel's hue at full strength with its brightness as alpha, so the gold composites over the dark panels as it was painted over black. All of them import with mipmaps.

| File | Drawn | For |
|---|---|---|
| `starwell.png` | the stone well at night, stardust spiralling up into the Milky Way, valley paddies behind | the Starwell screen's panel |
| `lacquer.png` | blue-black urushi with kirigane flakes and gold dust gathering at the edges | every lacquered panel's body (the dialogue box) |
| `corner.png` | a pine, seigaiha waves, stars and gold flakes hugging a top-left corner | two opposite corners of each lacquered panel, mirrored |
| `crest.png` | a gold ring with eight four-pointed stars and cloud scrolls | round the speaker's face in the dialogue |
| `divider.png` | a tapering double rule with a star between two wave scrolls | under the Starwell's stardust |
| `hp.png` `sp.png` `atk.png` `def.png` | a heart, wind curls, a katana, a shield with a star | the stats on the Starwell screen |
| `raise.png` `buy.png` `level.png` | a chevron under a star, a holed coin, a star in a rice wreath | Raise and Buy buttons, Mio's level |
| `runner.png` `engineer.png` `elder.png` `medic.png` `farmer.png` `courier.png` `gardener.png` `lodge.png` `orchard.png` | a straw sandal, a spanner and screwdriver, a staff and gourd, a mortar and leaf, a rice sheaf, a tied letter, a watering can, a paper lantern, a persimmon | a speaker's role on their dialogue ribbon |

## Prompts (2026-11-01)

Each began with the same line as the sheets above: "Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop."

### starwell

```
Create one 1536x1024 landscape image: a painted illustration for the save-point screen of a Japanese action JRPG, in polished anime game key-art style (soft painterly light, clean shapes), to sit beside character portraits drawn in modern anime style.
Subject: the Starwell, an old round stone well with a small wooden roof and a hanging wooden bucket, in the square of a quiet rural Japanese mountain village at night. A slender column of gentle golden stardust rises out of the well and spirals up into a deep blue starry sky with a faint Milky Way, warmly lighting the well's stones, a nearby stone lantern and the eaves of one old wooden house. Terraced rice paddies and soft mountain silhouettes far behind.
Composition: the well stands in the left third of the image. The right two thirds are darker open night sky over soft shadowed ground, calm and low in detail, because interface text and cards will be laid over them.
Palette: deep indigo and blue-black with warm gold light. No people, no characters, no animals, no text, no letters, no logo, no frame, no border, no watermark.
```

### lacquer

```
Create one 1536x1024 landscape image: a flat, straight-on, evenly lit surface texture of polished Japanese urushi lacquer, to fill the background of a game interface window.
Deep blue-black lacquer (between #0b1822 and #142636) with a faint soft sheen, very sparsely sprinkled with tiny flakes of gold leaf (kirigane) and fine gold dust (nashiji). The gold thins out toward the middle of the image so text laid over it stays readable, with a few slightly denser drifts of gold dust near the edges and corners.
Fill the whole image edge to edge. No objects, no motifs, no pattern, no border, no frame, no vignette, no text, no perspective, no room reflections.
```

### corner

```
Create one 1024x1024 square image: a Japanese maki-e lacquer ornament for the TOP-LEFT corner of a game interface window frame, gold on a flat pure black (#000000) background.
The ornament hugs the top-left corner of the image: one arm runs along the top edge and one down the left edge, each about 65% of the image long, thinning into a trail of fine gold dust at its end. In the corner itself a compact cluster of motifs: a gnarled pine branch with needle tufts, stylised seigaiha wave crests, and three small four-pointed stars among a scatter of gold flakes.
Raised gold maki-e look: warm metallic gold (midtones around #e7c873, pale highlights around #fff2c0, deep shadows around #8a6a2a) with fine engraved detail.
Everything else, including the whole lower-right half, is empty pure black. The background must be strictly flat #000000: no lacquer texture, no vignette, no shadow, no glow spilling onto the black. No text, no border, no frame lines.
```

### crest

```
Create one 1024x1024 square image: a round medallion frame for a character's face portrait in a JRPG dialogue box, gold on a flat pure black (#000000) background.
An ornate gold ring about 88% of the image wide, centred. Its centre is an empty pure black circle (about 70% of the image wide) where the face will be shown. The ring has a fine engraved double rim, eight small four-pointed stars spaced evenly around it (the one at the top larger than the others) and delicate cloud scrolls between the stars.
Raised gold maki-e look: warm metallic gold (midtones around #e7c873, pale highlights around #fff2c0, deep shadows around #8a6a2a) with fine engraved detail.
Inside and outside the ring, strictly flat pure black #000000: no texture, no glow, no shadow. No text, no face, no figure.
```

### divider

```
Create one 1536x1024 landscape image: one ornamental horizontal divider for a game interface, gold maki-e on a flat pure black (#000000) background.
A thin double gold rule runs across 90% of the image width along its exact vertical centre, tapering to fine points at both ends, with fine gold dust along it. At its centre a small ornament: a four-pointed star flanked on each side by a curling seigaiha wave scroll. The whole divider, ornament included, is no taller than 12% of the image height.
Warm metallic gold (midtones around #e7c873, pale highlights around #fff2c0, deep shadows around #8a6a2a). Everything else strictly flat pure black #000000: no texture, no glow, no shadow, no text.
```

### icons

```
Create one 1024x1024 square image: a game UI icon sheet of 16 icons arranged in an exact 4 by 4 grid. Each icon sits centred in its own equal 256x256 cell and fills about 70% of the cell, with clear black space around it; icons never touch or cross into neighbouring cells. No grid lines, no cell borders, no labels, no numbers and no text anywhere.
Colours: pure white (#FFFFFF) shapes on a flat pure black (#000000) background. Strictly two colours: no grey, no gradients, no shading, no glow, no texture, no drop shadow.
Style: Japanese family-crest (kamon) inspired SOLID icons for an action JRPG set in a rural Japanese mountain valley. Filled white silhouettes with inner details cut out as black negative space, the way a kamon crest is carved; the same visual weight across every icon; simple and bold enough to read at 32 px. No circle or badge behind an icon unless its description asks for one.
The 16 icons, row by row, left to right:
Row 1: (1) Vitality: a plump heart. (2) Stamina: three flowing curls of wind, one above another. (3) Attack: a straight katana pointing up and to the right, with a round tsuba guard. (4) Defense: a heater shield with a four-pointed star cut out of its centre.
Row 2: (5) Raise: a bold upward chevron arrow with a small four-pointed star above its tip. (6) Buy: an old Japanese coin with a square hole (mon) and a small four-pointed sparkle at its upper right. (7) Level: a four-pointed star inside a wreath of two rice ears. (8) Runner: a straw sandal (waraji) seen from the side with three speed lines behind it.
Row 3: (9) Engineer: a spanner and a screwdriver crossed in an X. (10) Elder: a curved wooden walking staff with a small gourd hanging from it. (11) Medic: a mortar and pestle with a single leaf. (12) Farmer: a sheaf of three drooping rice ears tied with a band.
Row 4: (13) Courier: a folded letter tied with a cord bow. (14) Gardener: a watering can with three droplets falling from its spout. (15) Lodge keeper: a round Japanese paper lantern (chochin) with horizontal ribs. (16) Orchard keeper: a persimmon fruit with its four-leaf calyx and one stem leaf.
```
