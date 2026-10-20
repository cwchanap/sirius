# Hoshikawa hamlet textures — generation record

Generated 2026-09-29 with `codex exec` (codex-cli 0.154.0) calling its built-in gpt-image tool; the orchestration model never drew pixels with code. Outputs were 1254×1254 opaque RGB PNGs.

## village-atlas.png

Copied unchanged to `assets/art/village-atlas.png`.

Raw output sha256: `40afa3350bca0c08be61e2dbdb955cf797d1f357da4625efccd13b47355ab891`

Prompt:

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
A square 2x2 texture atlas, four equal quadrants, each a seamless tileable hand-painted stylized material for a warm painterly anime JRPG countryside (Ghibli-like brushwork, soft light, no text, no borders, no gutters, no perspective, flat top-down orthographic swatches that fill each quadrant edge to edge):
top-left: golden-grey weathered rice-straw thatch, layered bundles running vertically;
top-right: dark weathered Japanese cedar boards (yakisugi-inspired), vertical planks;
bottom-left: warm ochre earthen plaster wall with subtle straw flecks;
bottom-right: blue-grey Japanese kawara clay roof tiles, rows running horizontally.
```

## bark-atlas.png

Split into four unchanged 627×627 quadrants: `bark-keyaki.png` (top-left), `bark-sugi.png` (top-right), `bark-sakura.png` (bottom-left), `bark-bamboo.png` (bottom-right).

Raw output sha256: `e78ddf43ddaaed273a70bbcf9da049f941ca2202b60b626ab4fefb27922a27dc`

Prompt:

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
A square 2x2 texture atlas, four equal quadrants, each a seamless tileable stylized hand-painted bark swatch for a warm painterly anime JRPG (Ghibli-like brushwork), flat orthographic, filling each quadrant edge to edge, no text, no borders, no gutters:
top-left: grey mottled zelkova (keyaki) bark with soft flaking patches;
top-right: red-brown fibrous Japanese cedar (sugi) bark in long vertical strips;
bottom-left: dark purple-brown cherry bark with horizontal lenticel bands;
bottom-right: green bamboo culm with pale node rings running horizontally.
```

## foliage-raw.png (2026-10-19, current)

Generated with `codex exec` (codex-cli 0.163.0) calling its built-in image tool; 1254×1254 opaque RGB. The blue key was removed with `remove_chroma_key.py --auto-key border --soft-matte --despill --spill-cleanup` (sampled key `#0002fd`) to make `foliage-atlas.png`; no other edits.

Raw output sha256: `d460d9cb9abac868941cf8733014f0bbd71567ed482e37f511f9fb45fd781eed`
`foliage-atlas.png` sha256: `63646041c7aa65b691bb2288c819c84f8fe1f51ccf7e63598d9cc7e87caab593`

Prompt:

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
A square 2x2 atlas of hand-painted foliage cluster sprites for alpha-tested leaf cards on semi-realistic 3D trees (naturalistic painterly game texture, soft even daylight from above, no cel shading, no outlines). Four equal square quadrants. Each quadrant holds one loose, airy, roughly round spray of MANY SMALL leaves or blossoms filling about 80% of its cell, centred. The silhouette is irregular and broken into many small leaf tips and blossoms with gaps where the background shows through: densest and slightly darker in the middle, thinning to scattered single leaves at the rim. Everything outside the foliage is a perfectly flat pure #0000FF blue: no shadows, gradients or vignette on the background, no text, no borders, no gutters, no dividing lines.
top-left: keyaki (Japanese zelkova) twig spray: about 80 small serrated leaves, each roughly 1/20 of the quadrant width, mixed fresh and mid greens with a few olive leaves.
top-right: somei-yoshino cherry blossom spray in full bloom: a cloud of about 200 very small pale pink to white five-petal blossoms, each roughly 1/30 of the quadrant width, on thin dark twigs, with a few tiny bronze-green young leaves. No large individual flowers.
bottom-left: sugi (Japanese cedar) foliage: many small overlapping dark blue-green needle sprays.
bottom-right: bamboo: many slender small green lanceolate leaves on thin twigs, drooping.
```

The prompt then asked Codex to copy the image to a scratch path.

## foliage-raw.png (2026-09-29, replaced)

Blue key removed with `remove_chroma_key.py` to make `foliage-atlas.png` (RGBA); no other edits.

Raw output sha256: `3b81a411dcfc1831a363871f1bc17eedb746ad3028577f58e498f3e0cebaa4f5`

Prompt:

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
A square 2x2 atlas of stylized hand-painted foliage cluster sprites for alpha-cut leaf cards in a warm painterly anime JRPG (Ghibli-like brushwork). Each quadrant holds one dense leaf cluster filling about 85% of its cell, centred, with clean crisp silhouettes, on a perfectly flat pure #0000FF blue background everywhere else, no shadows or gradients on the background, no text, no borders:
top-left: fresh green zelkova (keyaki) leaves;
top-right: pale pink cherry blossoms with a few small green leaves;
bottom-left: dark blue-green Japanese cedar (sugi) needle sprays;
bottom-right: slender green bamboo leaves.
```
