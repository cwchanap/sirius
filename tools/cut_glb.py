"""Delete the faces of a generated GLB that lie wholly inside a box, e.g. a stray fragment Tripo left floating beside a model.

/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/cut_glb.py -- IN.glb OUT.glb XMIN YMIN ZMIN XMAX YMAX ZMAX

The box is in the source's own glTF units and axes (Y up), as the GLB stores them. Everything else, the UVs, normals,
materials and textures included, passes through.
"""
import os
import sys

import bmesh
import bpy

source, target, *box = sys.argv[sys.argv.index('--') + 1:]
lo, hi = [float(v) for v in box[:3]], [float(v) for v in box[3:]]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(source))
removed = 0
for o in [o for o in bpy.data.objects if o.type == 'MESH']:
    bm = bmesh.new()
    bm.from_mesh(o.data)
    def inside(v):
        w = o.matrix_world @ v.co
        p = (w.x, w.z, -w.y)  # Blender Z up back to glTF Y up
        return all(lo[i] <= p[i] <= hi[i] for i in range(3))
    faces = [f for f in bm.faces if all(inside(v) for v in f.verts)]
    removed += len(faces)
    bmesh.ops.delete(bm, geom=faces, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(o.data)
    bm.free()
bpy.ops.export_scene.gltf(filepath=os.path.abspath(target), export_format='GLB', export_image_format='AUTO')
print(f"CUT_GLB_OK removed {removed} faces")
