"""Presets of the shared character importer for Sota, Naomi and Toma (assets sora, nao, toma), sent from Tripo Studio over DCC Bridge.

python3 tools/import_tripo_villagers.py [sora nao toma]
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_character import main, ROOT

HEIGHTS = {'sora': '1.78', 'nao': '1.64', 'toma': '1.74'}

if __name__ == "__main__":
    names = sys.argv[1:] or list(HEIGHTS)
    for name in names:
        code = main([str(ROOT / f"assets/source/{name}-tripo-original.blend"), '--name', name, '--height', HEIGHTS[name],
                     '--idle', 'breathing', '--force', '--strip-bone-prefix', 'mixamorig:'])
        if code:
            raise SystemExit(code)
