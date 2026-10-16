# Dialogue portrait sources

Every portrait is cut from existing art with `tools/cut_portrait.gd`; none was generated, so there is no Codex image to record. Cut on 2026-10-16.

The resident sources are the unchanged front views from the gitignored reference packs, which live only in the main checkout's `artifacts/` on the user's machine. Each source hash matches the one its pack recorded (`npc-image-to-3d-v2/files.json`, `npc-image-to-3d-v3/generation-records.json`). The v2 fronts are byte-identical copies of the v1 fronts.

The resident sources have a painted ground shadow under the feet. `--ground=170` clears that shadow. The seven v3 sources are already transparent, so the cutter skips its pure-white pocket pass for them; that pass would otherwise clear highlights such as KOMA-7's visor rim. Mio's cut is unchanged: it recuts byte for byte without `--ground`.

| Portrait | Source | Source sha256 | Portrait sha256 |
|---|---|---|---|
| `mio.png` | `assets/art/mio-turnaround-body-v1.png` | `bf718b8bd3eb528e50cea7d6a3e4758d267ef3685afa76ce15919da76740a789` | `8bc49059d023f0db88022153bb1187f7b61700734a9482b715ecd30b8c86d323` |
| `ren.png` | `artifacts/npc-image-to-3d-v2/ren-engineer/front.png` | `afafc8fad1e67e216a8924736b1eec1e5bfa11b846125789f636d98f9a6feba5` | `e20080e90693c90b814a88423b7496c778033e28a086ff5cebbd5bd96148039c` |
| `chiyo.png` | `artifacts/npc-image-to-3d-v2/chiyo-elder/front.png` | `9a740cb6cc3d40cb65cc62ae0720d53b18fc01ae877d54bed460fb69b9968d10` | `9307016994b1168cdae1a3aaa19dc854e6912587e0ef7633fe95d864cec10027` |
| `aki.png` | `artifacts/npc-image-to-3d-v2/aki-medic/front.png` | `2db311bac57f391808356a02366e8e6df22077c90bc620fbac198fa65e727f3d` | `0d144423a032dda0fd07714816cec8fdbedf464fd57f5c3e0ee72802f6e93f10` |
| `hana.png` | `artifacts/npc-image-to-3d-v3/hana-farmer/front.png` | `a08193f85930a36c3a8e99c8c84ea75003d9dd197f917b7c82f874e3873655ac` | `29746128e26f46b083f5bf42c50b1bed862839f87b0cf4c34addd7fe562444b8` |
| `sora.png` | `artifacts/npc-image-to-3d-v3/sora-courier/front.png` | `817a4f2b75da10ff6d57c2b0224c2781abea6b06e554d19ee298d15529a6be2f` | `1a6629970cd7755c728849e11ab118a6d1b42c9fb6d6de7826fb2aa9c74abeb8` |
| `koma7.png` | `artifacts/npc-image-to-3d-v3/koma7-gardener/front.png` | `5b92db7720d008699be14dfcc6f7d6e2e69adefb7ccb250912478a0841bf6dd2` | `53af518f7158a571c4cf3abd19543dd6be5763d5f33859f03fe5836f41a416ad` |
| `nao.png` | `artifacts/npc-image-to-3d-v3/nao-lodge-keeper/front.png` | `e9fd23e38846bed41d0841e9d296351e9f3523ad7f3ee2807dee46d9b38a8a0d` | `8270612720b965d83071becdcdf7f2a60ec085f709cd4cc6e1bf2d146a17dbd0` |
| `emi.png` | `artifacts/npc-image-to-3d-v3/emi-mountain-medic/front.png` | `0e3c739c411562e3b5351180e2ea634d19b25fdb0ff824c552be543eb94e0815` | `c56a4040d50bbddde5e11dc68300df9e16fd07d32ca290ae49712ec024520fc9` |
| `toma.png` | `artifacts/npc-image-to-3d-v3/toma-orchard-keeper/front.png` | `cad785cb91ef260ef10ee355c750a27922f8be2d909d668b205f21d77cfae83d` | `b8c77b13e4ac4c75a337e758f41508c8093bf32d36993061fccdc943ae7a782e` |
| `yui.png` | `artifacts/npc-image-to-3d-v3/yui-pass-courier/front.png` | `ca8681bd37b66620f6d34e71b46844fd805df2f0d362ea2f91ab0f0f8afb893f` | `87b814c930bb078080469045dffce7f6ba7ecfef22bd00d87a9979fe5a51221e` |

## Commands

Run from the repository root, with `GODOT_MONO=/Applications/Godot_mono.app/Contents/MacOS/Godot` and `A=/Users/chanwaichan/workspace/sirius/artifacts` (the main checkout's `artifacts/`):

```sh
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=res://assets/art/mio-turnaround-body-v1.png --rect=1070,0,350,660 --flip --out=res://assets/art/portraits/mio.png
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v2/ren-engineer/front.png --out=res://assets/art/portraits/ren.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v2/chiyo-elder/front.png --out=res://assets/art/portraits/chiyo.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v2/aki-medic/front.png --out=res://assets/art/portraits/aki.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/hana-farmer/front.png --out=res://assets/art/portraits/hana.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/sora-courier/front.png --out=res://assets/art/portraits/sora.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/koma7-gardener/front.png --out=res://assets/art/portraits/koma7.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/nao-lodge-keeper/front.png --out=res://assets/art/portraits/nao.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/emi-mountain-medic/front.png --out=res://assets/art/portraits/emi.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/toma-orchard-keeper/front.png --out=res://assets/art/portraits/toma.png --ground=170
$GODOT_MONO --headless --path . --script res://tools/cut_portrait.gd -- --src=$A/npc-image-to-3d-v3/yui-pass-courier/front.png --out=res://assets/art/portraits/yui.png --ground=170
$GODOT_MONO --headless --editor --path . --import
```

## Checks

Each cut was composited over a checkerboard and a dark backdrop and compared with its source. Each portrait holds one character, with no leftover background and no ground shadow; the soles are intact. Where a cut is opaque, its RGB matches the source exactly at the aligned offset. Above the bottom 200 rows of each resident portrait, the only pixels cleared are white background showing through gaps: between Chiyo's sleeve and torso, and between Aki's hair strands. Nothing on a character was made transparent.
