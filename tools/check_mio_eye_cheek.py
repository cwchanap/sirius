"""Run in Blender after loading characters.blend: eyes must stay behind adjacent skin."""
import bpy
import math
from mathutils import Vector

scene = bpy.data.scenes['HumanV2_mio']
skin = next(o for o in scene.objects if o.name.startswith('Mio painted skin'))
eye = next(o for o in scene.objects if o.name.startswith('Mio warm sclera'))
samples = 0
for point in [v.co for v in eye.data.vertices] + [p.center for p in eye.data.polygons]:
    p = eye.matrix_world @ point
    # Below the visible aperture in the fitted, neutral model.
    if p.z > 1.644 or p.y > -.03:
        continue
    hit, surface, _, _ = skin.ray_cast(Vector((p.x, -.3, p.z)), Vector((0, 1, 0)))
    assert hit and surface.y < 0, ('Missing cheek surface', tuple(p))
    assert p.y >= surface.y - .0001, ('Eyeball breaks through cheek', tuple(p))
    samples += 1
assert samples > 100
print(f'MIO_EYE_CHEEK_OK: {samples} lower-globe samples remain behind the cheek.')

# A forward or taller iris can escape above the lid even when the globe is safe.
for prefix in ['Mio warm sclera', 'Mio illustrated amber eyes']:
    eye = next(o for o in scene.objects if o.name.startswith(prefix))
    gaps = []
    for point in [v.co for v in eye.data.vertices] + [p.center for p in eye.data.polygons]:
        p = eye.matrix_world @ point
        if not 1.63 < p.z < 1.685 or p.y > -.03:
            continue
        hit, surface, _, _ = skin.ray_cast(Vector((p.x, -.3, p.z)), Vector((0, 1, 0)))
        if hit and surface.y < 0:
            gaps.append(p.y - surface.y)
    assert len(gaps) > 50 and min(gaps) >= -.0001, (prefix, 'breaks through skin', min(gaps, default=0))
    print(f'MIO_EYE_OCCLUSION_OK: {prefix}, {len(gaps)} hidden samples, {min(gaps)*1000:.3f} mm minimum clearance.')

# Curved layers can cross between their vertices and leave white flecks on the iris.
iris = next(o for o in scene.objects if o.name.startswith('Mio illustrated amber eyes'))
sclera = next(o for o in scene.objects if o.name.startswith('Mio warm sclera'))
samples = 0
for angle in [-45, 0, 45]:
    a = math.radians(angle)
    outward = Vector((math.sin(a), -math.cos(a), 0))
    for polygon in iris.data.polygons:
        point = iris.matrix_world @ polygon.center
        origin = point + outward * .3
        distances = {}
        for name, obj in [('skin', skin), ('iris', iris), ('sclera', sclera)]:
            inv = obj.matrix_world.inverted()
            hit, surface, _, _ = obj.ray_cast(inv @ origin, inv.to_3x3() @ -outward)
            if hit: distances[name] = (obj.matrix_world @ surface - origin).length
        if 'iris' not in distances or distances.get('skin', 1) < distances['iris']:
            continue
        assert distances.get('sclera', 1) >= distances['iris'] - .00005, ('Sclera crosses visible iris', angle, tuple(point), distances)
        samples += 1
assert samples > 100
print(f'MIO_IRIS_LAYER_OK: {samples} visible iris samples at front and both 45-degree views.')
