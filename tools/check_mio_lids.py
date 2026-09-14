"""Run in Blender on the neutral model; lashes must hug skin or the open lid edge."""
import bpy
import bmesh
from mathutils import Vector

scene = bpy.data.scenes['HumanV2_mio']
skin = next(o for o in scene.objects if o.name.startswith('Mio painted skin'))
lashes = next(o for o in scene.objects if o.name.startswith('Mio fine lashes'))
bm = bmesh.new()
bm.from_mesh(skin.data)
edges = [(skin.matrix_world @ e.verts[0].co, skin.matrix_world @ e.verts[1].co)
         for e in bm.edges if e.is_boundary and all(1.63 < v.co.z < 1.69 for v in e.verts)]
bm.free()
assert edges, 'Missing open eyelid boundaries'
inverse = skin.matrix_world.inverted()
gaps, edge_gaps = [], []
points = [v.co for v in lashes.data.vertices] + [p.center for p in lashes.data.polygons]
for point in points:
    p = lashes.matrix_world @ point
    hit, surface, _, _ = skin.ray_cast(inverse @ Vector((p.x, -.3, p.z)),
                                      inverse.to_3x3() @ Vector((0, 1, 0)))
    surface = skin.matrix_world @ surface
    if hit and surface.y < 0:
        gap = surface.y - p.y
        assert -.0001 <= gap <= .0006, ('Lash intersects or floats above skin', tuple(p), gap)
        gaps.append(gap)
    else:
        distance = min((p - (a + (b - a) * max(0, min(1, (p - a).dot(b - a) /
                                        (b - a).length_squared)))).length for a, b in edges)
        assert distance <= .0004, ('Lash misses both skin and eyelid edge', tuple(p), distance)
        edge_gaps.append(distance)
assert len(gaps) > 500
RESULT = dict(skin_samples=len(gaps), edge_samples=len(edge_gaps),
              skin_gap_mm=[min(gaps) * 1000, max(gaps) * 1000],
              maximum_edge_distance_mm=max(edge_gaps, default=0) * 1000)
print('MIO_LIDS_OK:', RESULT)
