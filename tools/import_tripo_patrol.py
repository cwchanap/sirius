"""Build patrol.glb, the patrols' and scouts' robot, from Hapadona's Tripo Studio model: maps halved to 2048 px, the clips in
tools/patrol_clips.py keyed on its rig, then the shared importer at patrol_clips.HEIGHT.

blender --background --factory-startup --python tools/import_tripo_patrol.py
The source, assets/source/patrol-tripo.glb, is her rigged Studio model exported from Blender with the mixamorig: prefix stripped.
"""
from pathlib import Path
import sys
import tempfile

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import patrol_clips  # noqa: E402
from import_character import main, ROOT  # noqa: E402
from shrink_glb_textures import shrink  # noqa: E402

if __name__ == '__main__':
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'assets/source/patrol-tripo.glb'))
    for obj in list(bpy.data.objects):
        # The glTF importer's bone display shape.
        if obj.type == 'MESH' and obj.parent is None:
            bpy.data.objects.remove(obj)
    shrink(2048)
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    patrol_clips.key(rig, patrol_clips.HEIGHT / patrol_clips.height(rig))
    keyed = Path(tempfile.mkdtemp()) / 'patrol-keyed.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(keyed))
    raise SystemExit(main([str(keyed), '--name', 'patrol', '--height', str(patrol_clips.HEIGHT), '--force']))
