# Mio's room — generation record

Generated 2026-10-30 (PDT) with `codex exec` (codex-cli 0.166.0) calling its built-in image tool, on the owner's Mac, for the opening's first map (`World.Room()`). One try each; every raw output below is byte-identical to its file in `~/.codex/generated_images/`. Raw outputs and Codex logs are in the gitignored `artifacts/room-art/` on the owner's machine.

Each run read its prompt on stdin, with one line appended telling Codex where to put the file:

```sh
D=$PWD/artifacts/room-art
{ cat $D/prompts/NAME.txt; printf '\nThen copy the generated image unchanged to %s/%s-1.png and print the path. Do not resize, recolour, crop, tile or otherwise edit it with code.\n' "$D" NAME; } \
  | codex exec --skip-git-repo-check -s workspace-write -C "$D" - > $D/NAME-1.log 2>&1
```

| File | Size | Raw sha256 | Edits |
|---|---|---|---|
| `tatami.png` | 1254×1254 | `7e0a1246290be54d463ffe1a857e5fa0ea0f08069d7b8709573ae73562a1bd1a` | none |
| `plaster.png` | 1254×1254 | `7848af6f44e1c6bb7cc00296e07c4fc5524dee046c0513c9b338e4b4e23eb86c` | none |
| `cedar.png` | 1254×1254 | `2c57d7c9eaba414cbab10d590195b94ba7a44a273056c9ecd02fb742a82bf513` | none |
| `shoji.png` | 1254×1254 | `340743ec7068b035a65cb3fbe39534fee3e34b4afdab10d587598e15a1223c20` | none |
| `futon.png` | 1072×1135 | `e3eb335340c84cd6f49ce3b909ae82ca67dc486a46734185eb1c9a16455217b2` | cropped, see below |
| `view.png` | 1536×1024 | `648a095a2f76e4777ed893c76790925fdcb5791da4c5d088fb7fcb0a8050a0b8` | none |
| `photo.png` | 1024×1536 | `4d0dd311074b7ea6efb751fc78661ffac10a489a7814baf785eb776238102cbb` | none |

The five textures were checked tiled 2×2 at full size around the corner where four copies meet. Tatami, plaster, cedar and shoji repeat without a visible seam as drawn. The futon's asanoha pattern did not: the raw's width and height are not whole repeats of it, so its stars broke at both edges. `futon.png` is the raw cropped to x 54–1126, y 112–1247 (four repeats across, seven down, found by matching the columns and rows that differ least), with no resampling; its sha256 is `3b5c3c5b35307ce518c57d752ff51eed465855cb98102f749971bf3215285c07`.

All seven import Lossless with mipmaps: the five because they tile, the photograph because the wall shows it about 25 cm tall.

`photo.png` is Shio and Mio at about seven on a station platform, for the frame on the north wall (`Prologue.Photo`). Shio wears the gold wrist terminal with the jade lamp that Chiyo remembers. The station sign is blank on purpose.

## Prompts

### tatami.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Square image. A single seamless tileable texture for a 3D interior: the left edge continues the right edge and the top edge continues the bottom edge with no visible seam. Flat orthographic, fills the whole square edge to edge, lighting neutral and evenly diffuse, no perspective, no cast shadows, no vignette, no text, no borders, no logos, no objects. Hand-painted stylized material for a warm painterly anime JRPG set in rural Japan, readable fine detail, restrained natural palette.
Subject: tatami mat surface, close woven igusa rush straw running in fine parallel rows from top to bottom, tight even weave with the faint cross-stitch lines of the warp every few centimetres, warm sun-faded green-gold to pale straw colour with gentle variation between rows. Only the woven rush: no cloth edge binding, no mat borders, no seams between mats.
```

### plaster.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Square image. A single seamless tileable texture for a 3D interior: the left edge continues the right edge and the top edge continues the bottom edge with no visible seam. Flat orthographic, fills the whole square edge to edge, lighting neutral and evenly diffuse, no perspective, no cast shadows, no vignette, no text, no borders, no logos, no objects. Hand-painted stylized material for a warm painterly anime JRPG set in rural Japan, readable fine detail, restrained natural palette.
Subject: traditional Japanese earthen plaster wall (tsuchikabe), smooth troweled clay with soft trowel sweeps, fine sand grain and sparse short straw fibres, warm pale cream-beige with faint ochre mottling, matte. No cracks, no stains, no timber.
```

### cedar.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Square image. A single seamless tileable texture for a 3D interior: the left edge continues the right edge and the top edge continues the bottom edge with no visible seam. Flat orthographic, fills the whole square edge to edge, lighting neutral and evenly diffuse, no perspective, no cast shadows, no vignette, no text, no borders, no logos, no objects. Hand-painted stylized material for a warm painterly anime JRPG set in rural Japan, readable fine detail, restrained natural palette.
Subject: dark aged Japanese cedar (sugi) wood, straight vertical grain running top to bottom with long gently wavering growth lines, soft darker latewood streaks, a satin patina of old oiled timber, deep warm brown with reddish undertones. One continuous board surface: no plank seams, no knots, no nails, no joints.
```

### shoji.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Square image. A single seamless tileable texture for a 3D interior: the left edge continues the right edge and the top edge continues the bottom edge with no visible seam. Flat orthographic, fills the whole square edge to edge, lighting neutral and evenly diffuse, no perspective, no cast shadows, no vignette, no text, no borders, no logos, no objects. Hand-painted stylized material for a warm painterly anime JRPG set in rural Japan, readable fine detail, restrained natural palette.
Subject: handmade Japanese washi paper for shoji screens, warm off-white with soft cloudy fibre density, long thin kozo fibres scattered through the sheet, faint uneven translucency as if lit softly from behind, very subtle. Only the paper: no lattice, no wooden frame, no shadows of the frame.
```

### futon.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Square image. A single seamless tileable texture for a 3D interior: the left edge continues the right edge and the top edge continues the bottom edge with no visible seam. Flat orthographic, fills the whole square edge to edge, lighting neutral and evenly diffuse, no perspective, no cast shadows, no vignette, no text, no borders, no logos, no objects. Hand-painted stylized material for a warm painterly anime JRPG set in rural Japan, readable fine detail, restrained natural palette.
Subject: cotton futon quilt cover fabric, soft faded indigo-blue hand-dyed cotton with a small repeating traditional asanoha (hemp-leaf) star pattern in pale off-white, the pattern slightly uneven like hand stencilling (katazome), visible cotton weave texture and gentle softness of a well-used quilt. Even overall pattern, no folds, no stitching lines, no hems.
```

### view.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Wide landscape painting, 3:2, for a backdrop seen through the open sliding paper doors of a farmhouse room in a 3D anime JRPG. Early summer morning just after rain, seen from a raised wooden veranda at the edge of a rural Japanese village: in the foreground a strip of wet garden grass and a few hydrangea shrubs in blue and violet bloom, then flooded rice paddies with young green rice in neat rows, their still water mirroring a pale washed sky, narrow raised earth paths (aze) between them glistening wet; in the middle distance a few low farmhouses with dark tiled roofs half hidden by trees, and a line of tall cedars; behind, layered wooded hills of deep and fresh greens fading into soft white morning mist, the farthest ridges almost lost in haze. Soft low sunlight breaking through the clouds from the left, glints on the water, a few droplets in the air, calm and fresh. Warm painterly anime JRPG background art, luminous soft colours, delicate atmospheric perspective. Eye level a little above the paddies, horizon about two fifths from the top. No people, no animals, no text, no frame, no veranda, no door frames, no interior.
```

### photo.png

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Portrait 2:3. An old faded colour photograph from about fifteen years ago, a little warm-shifted and soft, with faint film grain and slight fading at the corners, as kept in a small wooden frame on a farmhouse wall. A Japanese woman in her early thirties, Shio, gentle confident smile, dark navy-black hair tied back loosely with a few strands loose, wearing a practical dark green railway work jacket over an ivory shirt; on her left wrist an antique gold wrist terminal with a tiny glowing jade-green lamp. She crouches on a small rural railway station platform in summer, one arm around a little girl of about seven with dark navy-black hair in a short ponytail and amber eyes, who grins at the camera. Behind them a wooden station sign post without readable letters, a weathered platform edge, rails and a green wooded hillside. Anime JRPG illustration style matching painted game portraits, natural soft daylight. No readable text, no logos, no watermark, no border, no frame drawn around the photo.
```
