"""Build drone.glb from Hapadona's Tripo Studio drone: maps cut to 1024 px, its halo rotor split off as the mesh Rotor, centred on
its axis so Enemy.cs can spin it, then the shared importer, static, at 1 m (Enemy.Drone's size scales it).

blender --background --factory-startup --python tools/import_tripo_drone.py
The source, assets/source/drone-tripo.glb, is her Studio model exported from Blender as a plain mesh: Tripo's rig had laid a
biped skeleton across it, so it is dropped and the drone moves in code.
"""
from pathlib import Path
import sys
import tempfile

import bmesh
import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_character import main, ROOT  # noqa: E402
from shrink_glb_textures import shrink  # noqa: E402

# Source metres: every loose part wholly above this is the halo ring, its spokes and hub cap; the struts and mast stay below.
RING = .72

if __name__ == '__main__':
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'assets/source/drone-tripo.glb'))
    shrink(1024)
    body = next(o for o in bpy.data.objects if o.type == 'MESH')
    body.name = 'Drone'
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    mesh = bmesh.from_edit_mesh(body.data)
    seen = set()
    for start in mesh.verts:
        if start in seen:
            continue
        part, stack = [], [start]
        seen.add(start)
        while stack:
            v = stack.pop()
            part.append(v)
            for edge in v.link_edges:
                other = edge.other_vert(v)
                if other not in seen:
                    seen.add(other)
                    stack.append(other)
        if min(v.co.z for v in part) >= RING:
            for v in part:
                v.select = True
    mesh.select_flush_mode()
    bmesh.update_edit_mesh(body.data)
    bpy.ops.mesh.separate(type='SELECTED')
    bpy.ops.object.mode_set(mode='OBJECT')
    rotor = next(o for o in bpy.data.objects if o.type == 'MESH' and o != body)
    rotor.name = 'Rotor'
    points = [rotor.matrix_world @ v.co for v in rotor.data.vertices]
    low, high = (Vector([f(p[i] for p in points) for i in range(3)]) for f in (min, max))
    bpy.ops.object.select_all(action='DESELECT')
    rotor.select_set(True)
    bpy.context.view_layer.objects.active = rotor
    bpy.context.scene.cursor.location = (low + high) / 2
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    print('ROTOR', len(rotor.data.polygons), 'faces of', len(rotor.data.polygons) + len(body.data.polygons), 'about', tuple(round(c, 3) for c in (low + high) / 2))
    split = Path(tempfile.mkdtemp()) / 'drone-split.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(split))
    raise SystemExit(main([str(split), '--name', 'drone', '--height', '1', '--mesh', 'Drone', '--mesh', 'Rotor', '--force']))
