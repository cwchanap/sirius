#!/usr/bin/env python3
"""Cuts the Noto CJK subsets under assets/fonts/ that the Japanese and Traditional Chinese text is drawn with.

    python3 tools/build_fonts.py [--noto-dir /usr/share/fonts/opentype/noto]

Needs fontTools and the Noto CJK collections (Debian/Ubuntu: apt install fonts-noto-cjk; others: the .ttc files from
https://github.com/notofonts/noto-cjk). Each output keeps the kana, symbols and punctuation plus every character that
appears in assets/dialogue/*.json or in a language's own name in Localization.cs, so adding a line with a new kanji means re-running this. Han glyphs differ by locale
(JP and TC forms), which is why there is one file per language rather than one pan-CJK font."""
import argparse, json, pathlib, re, sys
from fontTools import subset
from fontTools.ttLib import TTCollection

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = ROOT / "assets/fonts"
FACES = {"Sans": "NotoSansCJK-Regular.ttc", "Serif": "NotoSerifCJK-Regular.ttc"}
LOCALES = {"JP": "JP", "TC": "TC"}
BASELINE = [(0x20, 0x7E), (0xA0, 0xFF), (0x2000, 0x206F), (0x2190, 0x21FF), (0x25A0, 0x25FF), (0x3000, 0x303F),
            (0x3040, 0x309F), (0x30A0, 0x30FF), (0xFF00, 0xFFEF)]


def characters():
    chars = {c for lo, hi in BASELINE for c in map(chr, range(lo, hi + 1))}
    def walk(node):
        if isinstance(node, str): chars.update(node)
        elif isinstance(node, dict):
            for k, v in node.items(): walk(k); walk(v)
        elif isinstance(node, list):
            for v in node: walk(v)
    for path in sorted((ROOT / "assets/dialogue").glob("*.json")): walk(json.loads(path.read_text(encoding="utf-8")))
    languages = re.search(r"Languages=\{(.*?)\};", (ROOT / "scripts/Localization.cs").read_text(encoding="utf-8"), re.S)[1]
    for name in re.findall(r'\("[^"]+","([^"]+)"\)', languages): chars.update(name)
    return chars


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--noto-dir", default="/usr/share/fonts/opentype/noto")
    args = ap.parse_args()
    unicodes = sorted(ord(c) for c in characters() if ord(c) > 0x1F)
    OUT.mkdir(exist_ok=True)
    for face, collection in FACES.items():
        path = pathlib.Path(args.noto_dir) / collection
        if not path.exists(): sys.exit(f"missing {path}")
        fonts = TTCollection(str(path)).fonts
        for locale in LOCALES:
            font = next((f for f in fonts if f["name"].getDebugName(1) == f"Noto {face} CJK {locale}"), None)
            if font is None: sys.exit(f"{collection} has no {locale} font")
            options = subset.Options()
            options.layout_features = ["kern", "vert", "vrt2", "palt"]
            options.notdef_outline = True
            options.name_IDs = ["*"]
            options.hinting = False
            sub = subset.Subsetter(options)
            sub.populate(unicodes=unicodes)
            sub.subset(font)
            target = OUT / f"Noto{face}{locale}-Regular.otf"
            font.flavor = None
            font.save(str(target))
            print(f"{target.relative_to(ROOT)}  {target.stat().st_size // 1024} KB  {len(font.getBestCmap())} characters")


if __name__ == "__main__":
    main()
