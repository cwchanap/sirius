"""Build yui.glb and renji.glb from Hapadona's Tripo Studio models: the mixamorig: prefix stripped, the clips in
tools/cast_clips.py keyed on each rig, a face in tools/face_paint.py's FACES repainted, then the shared importer at cast_clips.HEIGHTS.

blender --background --factory-startup --python tools/import_tripo_cast.py -- [yui renji]
The sources, assets/source/NAME-tripo-original.blend, are her FBX downloads saved with their maps packed.
"""
from pathlib import Path
import sys
import tempfile

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cast_clips  # noqa: E402
import face_paint  # noqa: E402
import patrol_clips  # noqa: E402
from import_character import main, ROOT  # noqa: E402

if __name__ == '__main__':
    for name in sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else ['yui', 'renji']:
        bpy.ops.wm.open_mainfile(filepath=str(ROOT / f'assets/source/{name}-tripo-original.blend'))
        rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
        for bone in rig.data.bones:
            bone.name = bone.name.removeprefix('mixamorig:')
        if name in face_paint.FACES:
            face_paint.repaint_blend(name)
        unit = cast_clips.HEIGHTS[name] / patrol_clips.height(rig)
        patrol_clips.key(rig, unit, cast_clips.CLIPS[name], cast_clips.LOOPS[name], cast_clips.Cast(rig, unit, cast_clips.HEIGHTS[name]))
        keyed = Path(tempfile.mkdtemp()) / f'{name}-keyed.blend'
        bpy.ops.wm.save_as_mainfile(filepath=str(keyed))
        if code := main([str(keyed), '--name', name, '--height', str(cast_clips.HEIGHTS[name]), '--force']):
            raise SystemExit(code)
