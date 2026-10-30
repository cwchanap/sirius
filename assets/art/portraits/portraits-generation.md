# Dialogue portraits — generation record

Generated 2026-10-15 (PDT) with `codex exec` (codex-cli 0.159.2) calling its built-in image-generation tool. The orchestration model never drew pixels with code: its only Python opened the result to check size and background colour, and each output was copied unchanged from `~/.codex/generated_images/`. Every output is a 1024×1536 opaque RGB PNG on white.

On 2026-10-30 the files were renamed with the residents (the story's name table), unchanged: `ren.png` is now `renji.png`, `aki.png` `chiaki.png`, `hana.png` `haruka.png`, `sora.png` `sota.png`, `nao.png` `naomi.png` and `emi.png` `emiko.png`. The record below keeps the old names. Yui's and Renji's portraits predate their Tripo models and do not match them yet.

They replace the earlier cuts from the character reference sheets, which stood front-on with arms at their sides. Each portrait is a three-quarter dialogue pose cut from mid-thigh up, KOMA-7 from the knees up. Mio faces the viewer's right, because she stands on the left of the dialogue scene. Every resident faces the viewer's left. Zero still has no portrait.

Raw outputs, prompt files and Codex logs are in the gitignored `artifacts/portraits-v2/` (`<key>-<try>.png`, `prompts/<key>-<try>.txt`, `logs/<key>-<try>.log`), in the worktree that made them. `$A` below is the main checkout's `artifacts/` (`/Users/chanwaichan/workspace/sirius/artifacts`), where the residents' reference packs live on the user's machine only.

## Runs

Each run read its prompt on stdin, with one line appended telling Codex where to put the file, and attached the references in the order listed:

```sh
D=$PWD/artifacts/portraits-v2
{ cat $D/prompts/<key>-<try>.txt; printf '\nThen copy the generated image to %s/%s.png and print the path.\n' "$D" <key>-<try>; } \
  | codex exec --skip-git-repo-check -s workspace-write -C "$D" - -i <ref1> -i <ref2> ...
```

Mio's references are her key art and both turnaround sheets. Each resident's references are the front, left and right views from their pack, then Mio's accepted raw output `artifacts/portraits-v2/mio-2.png` as the style, framing and scale reference. The resident prompts share one brief; only the identity, gesture and expression lines differ.

| Portrait | Tries | Accepted raw | Raw sha256 |
|---|---|---|---|
| `mio.png` | 2 | `mio-2.png` | `ad4d6d997c6085689505c65a59662507c00768b54c26d71edef305cf022b54c2` |
| `ren.png` | 1 | `ren-1.png` | `8395add2d2c3fa25fe361b0f6a4cd141cb4b5c9579236126e6233010c433a39f` |
| `chiyo.png` | 1 | `chiyo-1.png` | `d7ab19c7597b74aea518004df68b7ba499ddcb73d1092b89ecf932c3b31b360a` |
| `aki.png` | 1 | `aki-1.png` | `d9e5ff455b6eb4ea719d5f61a3bf4353be2df96c83de4a54362a078a4e8208fa` |
| `hana.png` | 1 | `hana-1.png` | `b7326a243511ce06742dbea27bd23fee8888eb8e5b62672898ec873fc98b6496` |
| `sora.png` | 1 | `sora-1.png` | `45d2f96ad1f657ae5953c5d8a94f2da80e75c19244a8d07919efb5556e28f9c3` |
| `koma7.png` | 1 | `koma7-1.png` | `1b2c7a4070cf11fc9db8e9291479d779573c391869dc77546d4c43bf2abc00e1` |
| `nao.png` | 1 | `nao-1.png` | `e2d838c89c71fd2351c9e44f7e359cb740966c191effc720fabe61406048be73` |
| `emi.png` | 1 | `emi-1.png` | `f727f390c4493c5df3acec51e8e132677664ce644925b1c942d0332451b19438` |
| `toma.png` | 1 | `toma-1.png` | `4886b450ddadb44278c1c2f7dd12c4ec1da8df471d654e4407b53e2c4348b0e1` |
| `yui.png` | 2 | `yui-2.png` | `556e4a249668be75feadb3f11ed235f4813283cde55d9e0a2e7f4fd9062dcb17` |

Rejected raws: `mio-1.png` `2be8a97fe77940215d380b839cc6b01893ae350a1cabe4a9f5e51c09d13dc085`; `yui-1.png` `86dc9515d8aa30a2672cf772d9b6988b335632a3a717c0e1be16c055dcb3fad9`.

In eight runs (Ren, Chiyo, Hana, KOMA-7, Emi, Toma, and both of Yui's) Codex generated two or three candidates and copied the one it judged best. Every raw named here is byte-identical to its file in `~/.codex/generated_images/`. Yui's first retry stopped at Codex's usage limit before generating anything. The same `yui-2` prompt ran again after the reset.

## mio.png — Mio

References: `assets/art/mio-keyart.png`, `assets/art/mio-turnaround-body-v1.png`, `assets/art/mio-turnaround-head-v1.png`.

Try 1, `mio-1.png`. Rejected: her body stayed almost front-on and her open hand gestured to the left, away from the listener on the right. Try 2 adds the explicit three-quarter turn, the gesture toward the listener and the mid-thigh crop above the boots.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Mio, the 22-year-old traveller swordswoman in the attached references (key art, full-body turnaround, head turnaround). Reproduce her identity faithfully from them: the same face and large amber-gold eyes, dark navy-black hair in a messy high ponytail held by a bronze hair cuff with loose strands and side bangs; dark teal long coat with a tall stand collar, brass studs and gold piping, sleeves turned back to ivory cuffs; ivory button-up shirt; red scarf knotted at the throat; thin chain necklace; brown leather crossbody satchel on its shoulder strap; layered brown leather belts with a small red tassel; brass bracer with a glowing turquoise gem on one wrist; dark brown trousers with leather thigh straps. Same colours as the references.
Style: polished anime JRPG visual-novel standing portrait matching the attached key art and turnaround: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height. The top of the head and ponytail are fully inside the frame; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: body turned three-quarter toward the viewer's RIGHT, head and eyes turned toward that person on the right side of the image, shoulders loose. One hand rests on her satchel's shoulder strap at her chest; the other is raised a little at waist height in a small open, friendly gesture. Correct, natural hands with five fingers. Calm, friendly expression with a faint warm smile and a hint of quiet determination. No T-pose, no arms hanging straight down, no front-facing turnaround stance, no sword drawn.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no colour swatches, no border, no watermark.
```

Try 2, `mio-2.png`, accepted.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Mio, the 22-year-old traveller swordswoman in the attached references (key art, full-body turnaround, head turnaround). Reproduce her identity faithfully from them: the same face and large amber-gold eyes, dark navy-black hair in a messy high ponytail held by a bronze hair cuff with loose strands and side bangs; dark teal long coat with a tall stand collar, brass studs and gold piping, sleeves turned back to ivory cuffs; ivory button-up shirt; red scarf knotted at the throat; thin chain necklace; brown leather crossbody satchel on its shoulder strap; layered brown leather belts with a small red tassel; brass bracer with a glowing turquoise gem on one wrist; dark brown trousers with leather thigh straps. Same colours as the references.
Style: polished anime JRPG visual-novel standing portrait matching the attached key art and turnaround: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height. The top of the head and ponytail are fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs, above the tops of her boots.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: her chest and hips angled toward the right side of the image in a clear three-quarter view, head and eyes turned toward that person on the right side of the image, shoulders loose. One hand rests on her satchel's shoulder strap at her chest; the other is raised a little in front of her waist, palm up, in a small open, friendly gesture toward the person on the right. Both hands stay well inside the frame. Correct, natural hands with five fingers. Calm, friendly expression with a faint warm smile and a hint of quiet determination. No T-pose, no arms hanging straight down, no front-facing turnaround stance, no sword drawn.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no colour swatches, no border, no watermark.
```

## ren.png — Ren

References: `$A/npc-image-to-3d-v2/ren-engineer/front.png`, `$A/npc-image-to-3d-v2/ren-engineer/left.png`, `$A/npc-image-to-3d-v2/ren-engineer/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `ren-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Ren, the village engineer, a man in his thirties, shown in the first three attached reference images (front, left and right views). Reproduce his identity faithfully from them: tousled dark-brown hair, light stubble, warm tanned face and brown eyes; olive-green canvas work jacket with two flap chest pockets, sleeves rolled to the forearms showing a pale lining; cream henley undershirt; dark-brown leather belt with a turquoise-inlaid buckle and a small leather pouch at the hip; brown leather work apron tied at the waist; dark charcoal work trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of his head down to mid-thigh, filling the image height; his head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across his mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside him: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. He holds a well-worn steel spanner in one hand, resting it loosely against his shoulder; his other thumb is hooked in his belt beside the apron. Both hands stay well inside the frame. Correct, natural hands with five fingers. Tired but kind expression with a small reassuring smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## chiyo.png — Chiyo

References: `$A/npc-image-to-3d-v2/chiyo-elder/front.png`, `$A/npc-image-to-3d-v2/chiyo-elder/left.png`, `$A/npc-image-to-3d-v2/chiyo-elder/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `chiyo-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Chiyo, the village elder, a woman in her seventies, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: silver-grey hair in a low bun with soft loose strands, gentle lined face, kind dark eyes; plum-red wrap-front jacket with wide three-quarter sleeves, woven ochre-and-brown patterned trim and a few small mended patches; cream high-collared undershirt with cuffs showing at the wrists; wide ochre sash tied at the waist with a small brown leather pouch; dark blue-grey trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One hand rests at her sash; the other is raised gently in front of her, palm up, reaching a little toward the person on the left as if asking to look at something. Both hands stay well inside the frame. Correct, natural hands with five fingers. Warm grandmotherly smile, a little wistful. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## aki.png — Aki

References: `$A/npc-image-to-3d-v2/aki-medic/front.png`, `$A/npc-image-to-3d-v2/aki-medic/left.png`, `$A/npc-image-to-3d-v2/aki-medic/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `aki-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Aki, the village medic, a young woman, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: short wavy auburn bob with a silver hair clip, hazel eyes; ivory stand-collar medic tunic with sage-green piping and a small sage leaf emblem on the chest, sleeves turned up and buttoned with sage-green tabs; sage-green belt with a brass buckle and a brown leather pouch on the hip; slate-blue trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One hand rests on the leather pouch at her belt; the other holds a small corked glass remedy vial up at chest height. Both hands stay well inside the frame. Correct, natural hands with five fingers. Brisk, capable and kind, with a confident little smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## hana.png — Hana

References: `$A/npc-image-to-3d-v3/hana-farmer/front.png`, `$A/npc-image-to-3d-v3/hana-farmer/left.png`, `$A/npc-image-to-3d-v3/hana-farmer/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `hana-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Hana, the rice farmer, a young woman with sun-tanned skin, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: dark-brown hair in a crown braid with a low bun and a few loose strands, brown eyes; olive-green work shirt with the sleeves rolled up; cream undershirt; rust-red canvas overalls with brass buckles on the straps and a small embroidered sprout on the bib pocket; a small mustard burlap seed pouch hanging from the overalls on a brass ring. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. She holds the little burlap seed pouch up in one hand as if about to show it; her other hand rests on her hip. Both hands stay well inside the frame. Correct, natural hands with five fingers. Cheerful and open, with a bright grin. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## sora.png — Sora

References: `$A/npc-image-to-3d-v3/sora-courier/front.png`, `$A/npc-image-to-3d-v3/sora-courier/left.png`, `$A/npc-image-to-3d-v3/sora-courier/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `sora-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Sora, the courier, a young man, shown in the first three attached reference images (front, left and right views). Reproduce his identity faithfully from them: spiky windswept dark navy hair, dark eyes; navy denim jacket with brass buttons, grey chevron patches on the sleeves and leather cuff straps; cream shirt with a burnt-orange neckerchief; brown leather messenger bag with a brass clasp set with a green stone, worn on a diagonal strap across his chest; grey-brown trousers with a brown belt. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of his head down to mid-thigh, filling the image height; his head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across his mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside him: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One hand rests on the flap of his messenger bag at his hip; the other is raised casually near his shoulder in an easy half-wave. Both hands stay well inside the frame. Correct, natural hands with five fingers. Easy-going and alert, with a friendly half-smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## koma7.png — KOMA-7

References: `$A/npc-image-to-3d-v3/koma7-gardener/front.png`, `$A/npc-image-to-3d-v3/koma7-gardener/left.png`, `$A/npc-image-to-3d-v3/koma7-gardener/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `koma7-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of KOMA-7, a small 1.25-metre gardener robot, shown in the first three attached reference images (front, left and right views). Reproduce his identity faithfully from them: rounded cream-white ceramic head with a black visor screen showing two glowing cyan circle eyes and a small smile line, brass ear discs and a small brass cap on top; cream-white rounded body panels edged with jade-green plates and brass rivets, dark-brown flexible joints; a jade sprout emblem on the chest; a jade-green water tank worn as a backpack with a brass cap; a small brown leather pouch on the hip; jointed cream fingers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of his head down to his knees, filling the image height; his head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across his knees.
Pose: natural and relaxed, as if mid-conversation with someone standing beside him: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One jointed hand is raised in a small friendly wave; the other rests on the leather pouch at his hip. His head tilts a little in curiosity. Both hands stay well inside the frame. Correct, well-formed jointed robot hands. The visor shows a cheerful face: two glowing cyan circle eyes and a small smile line. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## nao.png — Nao

References: `$A/npc-image-to-3d-v3/nao-lodge-keeper/front.png`, `$A/npc-image-to-3d-v3/nao-lodge-keeper/left.png`, `$A/npc-image-to-3d-v3/nao-lodge-keeper/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `nao-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Nao, the lodge keeper, a sturdy middle-aged woman, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: brown hair with grey streaks pulled up in a loose bun with loose strands, warm brown eyes, round friendly face; cream shirt with a pointed collar and rolled sleeves; rust-red buttoned waistcoat with brass buttons; slate-blue apron tied with a long mustard sash knotted at the side, a small brown leather pouch on the sash; dark-brown trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One fist is planted on her hip; the other hand is raised in a warm welcoming gesture, palm open toward the person on the left. Both hands stay well inside the frame. Correct, natural hands with five fingers. Motherly, with a hearty smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## emi.png — Emi

References: `$A/npc-image-to-3d-v3/emi-mountain-medic/front.png`, `$A/npc-image-to-3d-v3/emi-mountain-medic/left.png`, `$A/npc-image-to-3d-v3/emi-mountain-medic/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `emi-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Emi, the mountain medic, a woman in her late thirties, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: dark hair in a low ponytail with a single silver-white streak at the front, brown eyes; teal-green stand-collar medic coat with tan stitching, a cream leaf emblem on the chest and a small green gem clasp at the collar, sleeves rolled to the elbows with cream cuffs and button tabs; cream inner shirt; brown leather belt with a cream canvas-and-leather medical pouch; mauve-grey trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One hand rests on the flap of the medical pouch at her hip; the other is raised at chest height in a calm, reassuring gesture. Both hands stay well inside the frame. Correct, natural hands with five fingers. Composed and kind, faintly amused. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## toma.png — Toma

References: `$A/npc-image-to-3d-v3/toma-orchard-keeper/front.png`, `$A/npc-image-to-3d-v3/toma-orchard-keeper/left.png`, `$A/npc-image-to-3d-v3/toma-orchard-keeper/right.png`, `artifacts/portraits-v2/mio-2.png`.

Accepted on the first try, `toma-1.png`.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Toma, the orchard keeper, a broad-shouldered man in his fifties, shown in the first three attached reference images (front, left and right views). Reproduce his identity faithfully from them: short swept-back grey hair, a grey moustache, weathered tanned face; burnt-orange canvas work vest with four flap pockets and a darker shoulder yoke; cream henley shirt with the sleeves rolled up and round ochre elbow patches; brown leather belt with a brass buckle and a brown leather tool pouch; olive-green work trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of his head down to mid-thigh, filling the image height; his head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across his mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside him: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. He holds a ripe orange persimmon in one big hand at chest height, as if offering it; his other thumb is hooked in his belt. Both hands stay well inside the frame. Correct, natural hands with five fingers. Gruff but warm, a crinkle-eyed smile under the moustache. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## yui.png — Yui

References: `$A/npc-image-to-3d-v3/yui-pass-courier/front.png`, `$A/npc-image-to-3d-v3/yui-pass-courier/left.png`, `$A/npc-image-to-3d-v3/yui-pass-courier/right.png`, `artifacts/portraits-v2/mio-2.png`.

Try 1, `yui-1.png`. Rejected: her body turned toward the right while only her head looked left, and the letter was held out to the right, away from the listener. Try 2 spells out a whole-body turn to the left and offers the letter toward the listener.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Yui, the mountain-pass courier, a young woman, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: dark plum-brown hair with side-swept bangs and two long braids falling over the front of her shoulders, dark eyes; cropped grey-blue jacket with brass buttons, a sleeve pocket and buttoned cuffs; mustard-ochre waistcoat with brass buttons over a cream ruffled-collar blouse; brown leather satchel with a jade disc clasp on a diagonal strap across her chest; dark plum-brown trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: chest and hips angled toward the LEFT side of the image in a clear three-quarter view, head and eyes turned toward that person on the left side of the image, shoulders loose. One hand holds the strap of her satchel; the other holds up a folded, sealed letter at chest height. Both hands stay well inside the frame. Correct, natural hands with five fingers. Bright and a little shy, with a soft smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

Try 2, `yui-2.png`, accepted.

```
Use your built-in image-generation tool to create a genuine AI raster. Do NOT draw it with Python/PIL/SVG/code; if you have no image tool print NO IMAGE TOOL and stop.
Create one 1024x1536 portrait-orientation image: a visual-novel dialogue standing portrait (tachie) of Yui, the mountain-pass courier, a young woman, shown in the first three attached reference images (front, left and right views). Reproduce her identity faithfully from them: dark plum-brown hair with side-swept bangs and two long braids falling over the front of her shoulders, dark eyes; cropped grey-blue jacket with brass buttons, a sleeve pocket and buttoned cuffs; mustard-ochre waistcoat with brass buttons over a cream ruffled-collar blouse; brown leather satchel with a jade disc clasp on a diagonal strap across her chest; dark plum-brown trousers. Same colours as the references.
The last attached image is Mio's finished portrait from the same game: match its art style, line weight, shading, framing and head size so the two read as one cast, but do not copy her face, outfit or pose.
Style: polished anime JRPG visual-novel standing portrait: clean line art, soft cel shading, painterly detail, warm natural light.
Framing: one character only, from just above the top of her head down to mid-thigh, filling the image height; her head is about one fifth of the image height, the same head size as Mio's in the attached finished portrait. The top of the head is fully inside the frame with a little white space above; the bottom edge cuts across her mid-thighs.
Pose: natural and relaxed, as if mid-conversation with someone standing beside her: her whole body faces the LEFT side of the image in a clear three-quarter view (chest, hips and legs all angled to the left, not twisted away), head and eyes turned toward that person on the left side of the image, shoulders loose. One hand holds the strap of her satchel at her chest; the other holds up a folded, sealed letter at chest height in front of her, offering it toward the person on the left. Both hands stay well inside the frame. Correct, natural hands with five fingers. Bright and a little shy, with a soft smile. No T-pose, no arms hanging straight down, no front-facing turnaround stance.
Background: flat pure white #FFFFFF everywhere, no floor, no ground shadow, no text, no labels, no border, no watermark.
```

## Cutting

Run from the repository root, with `GODOT_MONO=/Applications/Godot_mono.app/Contents/MacOS/Godot`. No cut needed `--ground` (there is no painted shadow) and none uses `--flip`:

```sh
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/mio-2.png --out=res://assets/art/portraits/mio.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/ren-1.png --out=res://assets/art/portraits/ren.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/chiyo-1.png --out=res://assets/art/portraits/chiyo.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/aki-1.png --out=res://assets/art/portraits/aki.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/hana-1.png --out=res://assets/art/portraits/hana.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/sora-1.png --out=res://assets/art/portraits/sora.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/koma7-1.png --out=res://assets/art/portraits/koma7.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/nao-1.png --out=res://assets/art/portraits/nao.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/emi-1.png --out=res://assets/art/portraits/emi.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/toma-1.png --out=res://assets/art/portraits/toma.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$PWD/artifacts/portraits-v2/yui-2.png --out=res://assets/art/portraits/yui.png
```

## Highlight restore

Because the raw outputs are opaque, the cutter runs its pocket pass, which clears every enclosed pure-white region of 8+ pixels. That is right for the background showing between hair strands and between an arm and the body. On these paintings it also cleared specular highlights inside the figure: Aki's tunic shoulder, KOMA-7's cream panels and rivets, Sora's collar, the stones in Mio's bracer, Ren's buckle and Sora's bag, Aki's vial, the top edge of Yui's envelope. Over the game those read as holes.

Every enclosed clear region of every cut was reviewed against its raw. The highlights were then restored with the script below, run once on the fresh cut. A region is restored when it contains a listed seed (a raw-image pixel), or, with `--auto`, when pale colour rather than line art surrounds it. The script writes back the raw pixels, fully opaque, along with the 2 px edge the cutter un-mixed. Every other pixel is exactly as the cutter wrote it. `--auto` is used only where every region it picked was checked to be a highlight. On Mio's and Chiyo's hair it would also fill real gaps, so those two cuts use seeds only. The other cuts needed no restore.

| Portrait | Restore arguments | What it restores |
|---|---|---|
| `mio.png` | `--seed=263,774` | the white highlight in her bracer gem |
| `ren.png` | `--seed=397,962` | the highlight in his belt-buckle stone |
| `aki.png` | `--auto --seed=213,450 --seed=196,464 --seed=240,729 --seed=346,931` | highlights on her tunic shoulder and sleeve (automatic), plus the liquid and glass highlights in the vial and two small tunic highlights (seeds) |
| `sora.png` | `--auto --seed=448,410 --seed=95,586 --seed=789,989 --seed=832,1185` | slits along his shirt and collar (automatic), plus a lapel highlight, the inside of the collar, a cuff highlight and the stone on his bag clasp (seeds) |
| `koma7.png` | `--auto --seed=633,474 --seed=491,589 --seed=709,598 --seed=150,679` | slits in his cream panels, visor rim and fingers (automatic), plus two brass-rivet highlights and two panel-edge highlights (seeds) |
| `toma.png` | `--auto` | specks along his rolled sleeve edge (automatic) |
| `yui.png` | `--auto` | a highlight strip along the top edge of her envelope (automatic) |

```sh
python3 restore_highlights.py artifacts/portraits-v2/<raw>.png assets/art/portraits/<key>.png <arguments>
```

```python
import sys
from collections import deque
from PIL import Image
# restore_highlights.py RAW CUT [--auto] [--seed=x,y ...]   (rewrites CUT in place; seeds are RAW pixel coordinates)
# cut_portrait.gd also clears pure-white highlights that sit inside the figure (pale cloth, gems, glass, brass).
# Each clear region the figure encloses is either real background (gaps between hair strands, arm and body) or such
# a highlight. Regions containing a seed, and with --auto those mostly ringed by pale colour (under 40% of the pixels
# 3-5 px outside it have a channel below 150), get their raw pixels back, fully opaque, with the 2 px edge the cutter
# un-mixed from white. Every other pixel stays exactly as the cutter wrote it.
raw=Image.open(sys.argv[1]).convert("RGB"); cut=Image.open(sys.argv[2]).convert("RGBA")
auto="--auto" in sys.argv; seeds=[tuple(int(v) for v in a[7:].split(",")) for a in sys.argv[3:] if a.startswith("--seed=")]
W,H=raw.size; w,h=cut.size; rp=raw.load(); cp=cut.load()
ops=[(x,y) for y in range(0,h,37) for x in range(0,w,29) if cp[x,y][3]==255][:400]
ox,oy=next((dx,dy) for dy in range(H-h+1) for dx in range(W-w+1) if all(rp[x+dx,y+dy]==cp[x,y][:3] for x,y in ops))
clear=[[cp[x,y][3]<8 for x in range(w)] for y in range(h)]
def nb(x,y):
    for X,Y in ((x+1,y),(x-1,y),(x,y+1),(x,y-1)):
        if 0<=X<w and 0<=Y<h: yield X,Y
seen=[[False]*w for _ in range(h)]; q=deque((x,y) for x in range(w) for y in (0,h-1) if clear[y][x])
q.extend((x,y) for y in range(h) for x in (0,w-1) if clear[y][x])
for x,y in q: seen[y][x]=True
while q:
    for X,Y in nb(*q.popleft()):
        if clear[Y][X] and not seen[Y][X]: seen[Y][X]=True; q.append((X,Y))
fix=[]; hit=set()
for y in range(h):
    for x in range(w):
        if not clear[y][x] or seen[y][x]: continue
        c=[(x,y)]; seen[y][x]=True; k=0
        while k<len(c):
            for X,Y in nb(*c[k]):
                if clear[Y][X] and not seen[Y][X]: seen[Y][X]=True; c.append((X,Y))
            k+=1
        cs=set(c); s=[p for p in seeds if (p[0]-ox,p[1]-oy) in cs]; hit.update(s)
        ring=[min(rp[X+ox,Y+oy]) for X,Y in {(x+dx,y+dy) for x,y in c for dx in range(-5,6) for dy in range(-5,6) if max(abs(dx),abs(dy))>=3}
              if 0<=X<w and 0<=Y<h and (X,Y) not in cs and cp[X,Y][3]==255]
        if s or (auto and ring and sum(v<150 for v in ring)/len(ring)<0.4): fix.append(c)
assert hit==set(seeds), f"seeds outside any enclosed clear region: {set(seeds)-hit}"
for c in fix:
    for x,y in c: clear[y][x]=False
out=cut.copy(); op=out.load()
for c in fix:
    for x,y in {(x+dx,y+dy) for x,y in c for dx in range(-2,3) for dy in range(-2,3)}:
        if 0<=x<w and 0<=y<h and not any(clear[Y][X] for X in range(max(0,x-2),min(w,x+3)) for Y in range(max(0,y-2),min(h,y+3))):
            op[x,y]=rp[x+ox,y+oy]+(255,)
out.save(sys.argv[2])
print("RESTORED",len(fix),"regions",sum(map(len,fix)),"px",sys.argv[2])
```

Then `$GODOT_MONO --headless --editor --path . --import`.

## Checks

Each raw was checked against the brief and its references: identity, facing direction, framing, a single figure, hands and fingers, and the white background. Each cut was composited over a dark backdrop at full size. Every enclosed clear region was inspected against the raw, before and after the restore. The cuts keep only real background clear: the white between hair strands, and between an arm and the body or a strap.

Still imperfect:

- Ren's raw has one strand of hair touching the top edge (2 px wide), so his cut starts at the tip of that strand.
- Nao's and Hana's crops end nearer the knee than mid-thigh.
- KOMA-7 is longer in the leg than his stubby reference.
- Mio has a second red tassel on her satchel; her reference has one, on the belt.
- Hana's seed pouch carries a printed leaf that her reference lacks.
- Head height runs from about 20% to 24% of the raw image height, not exactly one fifth.
- The raw backgrounds are near-white (251–255), not exactly #FFFFFF. The cutter's white tolerance clears them.

| Portrait | Portrait sha256 |
|---|---|
| `mio.png` | `9847959fe98710daa320244819614ec9b5cb084ea43e562971d823bbe709a2d8` |
| `ren.png` | `d32768fa1827732851864cd1d250e4850f84a7189d2e8caa64b4b80aec6bfe37` |
| `chiyo.png` | `e29320f32122378e573bcd4975bf6a0894bffaa6a5475558488c31b50d196b62` |
| `aki.png` | `3d195ebebcc6233ce6b911151b66270b698d8caecfb05d7eb3aeae7592932a94` |
| `hana.png` | `5b635c7f339e282c6f62b6dc75f6982e8ad20eb59f28b1a9c05c051a70c7f26e` |
| `sora.png` | `7e299bb5feb6cf4d63f34b44288740ecbf4757a89ce8e35a9d9128b9e4c2e9c8` |
| `koma7.png` | `c558112a0e486e839072e5d4c17dcd64c0478ae7ad1bc0bbd153f1c329e4e9f6` |
| `nao.png` | `8d4860b6811dbd02154d81e291c89ff728d154e0547ff5672c68aa2e347f77cd` |
| `emi.png` | `44ef219b4e39c3d3022f7283c8fb70f9171f39c9ae8dad625bc3e34849843620` |
| `toma.png` | `a761c7b82a5271d020bf3cea52f0178364b7691d70f1cf8a6286a141acf8375a` |
| `yui.png` | `e05c7eaaf79b4cf78ad570e410a87ed1a62a36933ec0cebf097fc09b49b66625` |
