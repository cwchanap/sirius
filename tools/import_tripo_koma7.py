"""Compatibility preset for the shared character importer."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_character import main, ROOT

if __name__ == "__main__":
    extra = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [] if "bpy" in sys.modules else sys.argv[1:]
    raise SystemExit(main([str(ROOT / "assets/source/koma7-tripo-original.blend"),
                          '--name', 'koma7', '--height', '1.25', '--idle', 'head-turn', '--force', '--strip-bone-prefix', 'mixamorig:'] + extra))
