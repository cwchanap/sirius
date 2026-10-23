"""Switch model textures to VRAM compression with mipmaps in their Godot .import files, flagging normal maps.

python3 tools/compress_textures.py           # every model texture: assets/models/* and assets/art/*-import/*
python3 tools/compress_textures.py IMAGE...  # only these images
Re-import afterwards: $GODOT_MONO --headless --editor --path . --import

Godot extracts a GLB's maps as Lossless (uncompressed in video memory) and never re-extracts one whose .import exists, so the
settings can only be applied after a first import; import_character.py does both imports around this.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
IMAGES = ('.png', '.jpg', '.jpeg', '.webp')
SETTINGS = {'compress/mode': '2', 'compress/high_quality': 'false', 'mipmaps/generate': 'true'}


def model_textures(root=ROOT):
    return sorted(p for pattern in ('assets/models/*', 'assets/art/*-import/*') for p in root.glob(pattern) if p.suffix.lower() in IMAGES)


def compress(images):
    changed = []
    for image in images:
        path = image.with_name(image.name + '.import')
        if not path.exists():
            continue
        text = new = path.read_text()
        settings = {**SETTINGS, 'compress/normal_map': '1' if re.search('normal', image.name, re.I) else '0'}
        for key, value in settings.items():
            line = f'{key}={value}'
            new = re.sub(rf'^{re.escape(key)}=.*$', line, new, flags=re.M) if re.search(rf'^{re.escape(key)}=', new, re.M) else new.replace('[params]\n', f'[params]\n\n{line}', 1)
        if new != text:
            path.write_text(new)
            changed.append(path)
    return changed


if __name__ == '__main__':
    changed = compress([Path(a).resolve() for a in sys.argv[1:]] or model_textures())
    print('COMPRESS_TEXTURES', len(changed), 'changed')
