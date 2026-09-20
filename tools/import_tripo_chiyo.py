"""Compatibility preset for the shared character importer."""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_character import main, ROOT

if __name__ == "__main__":
    extra = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [] if "bpy" in sys.modules else sys.argv[1:]
    raise SystemExit(main([str(ROOT / "assets/source/chiyo-tripo-original.blend"),
                          '--name', 'chiyo', '--height', '1.58', '--idle', 'breathing', '--force', '--max-triangles', '90000'] + extra))
