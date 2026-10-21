"""Deterministic tree and shrub GLBs for Hoshikawa.

Run: /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_props.py

Each GLB carries geometry, UVs, custom normals and a COLOR_0 attribute (R = wind sway weight,
G = ambient occlusion tint) plus two material *names*: bark_<species> and foliage. No images
are exported; World.Tree() assigns the shared bark textures and foliage shader by material name.
Foliage UVs index assets/art/foliage-atlas.png: keyaki (0,0), sakura (1,0), sugi (0,1), bamboo (1,1). Shrubs and the kaki
(persimmon) borrow keyaki's bark and leaves, so they add no material names.
Foliage normals follow each crown's bounding ellipsoid (Builder.volume), so a tree shades as one volume, not as separate cards.
"""
import math
import os
import random

import bpy
from mathutils import Quaternion, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "models", "props")
CELLS = {"keyaki": (0, 0), "sakura": (1, 0), "sugi": (0, 1), "bamboo": (1, 1)}
MAX_TRIS = 6000
SINK = .5  # trunks start below ground so World.Tree() placements on slopes never show a floating base


class Builder:
    def __init__(self, height, crown, shade=.62):
        self.height, self.crown, self.shade = height, crown, shade  # shade: the crown core's ambient occlusion
        self.verts, self.faces, self.mats, self.uvs, self.cols, self.nors = [], [], [], [], [], []

    def sway(self, p):
        return min(1.0, max(0.0, p.z / self.height) ** 1.5 * .6 + min(1.0, p.xy.length / self.crown) * .4)

    def face(self, pts, uvs, nors, cols, mat):
        base = len(self.verts)
        self.verts += [tuple(p) for p in pts]
        self.faces.append(list(range(base, base + len(pts))))
        self.uvs += uvs
        self.nors += [tuple(n.normalized()) for n in nors]
        self.cols += cols
        self.mats.append(mat)

    def tube(self, pts, radii, sides=8):
        n = len(pts)
        tangents = [(pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized() for i in range(n)]
        ref = Vector((1, 0, 0)) if abs(tangents[0].x) < .9 else Vector((0, 1, 0))
        u = tangents[0].cross(ref).normalized()
        lengths, rings = [0.0], []
        for i in range(1, n):
            lengths.append(lengths[-1] + (pts[i] - pts[i - 1]).length)
        for i, t in enumerate(tangents):
            u = (u - t * u.dot(t)).normalized()  # parallel transport keeps the seam from twisting
            w = t.cross(u)
            ring = []
            for k in range(sides + 1):
                a = 2 * math.pi * k / sides
                d = u * math.cos(a) + w * math.sin(a)
                ring.append((pts[i] + d * radii[i], d, (k / sides, lengths[i] / 1.5)))
            rings.append(ring)
        for i in range(n - 1):
            c0, c1 = (self.sway(pts[i]), 1, 0, 1), (self.sway(pts[i + 1]), 1, 0, 1)
            for k in range(sides):
                a, b, c, d = rings[i][k], rings[i][k + 1], rings[i + 1][k + 1], rings[i + 1][k]
                self.face([a[0], b[0], c[0], d[0]], [a[2], b[2], c[2], d[2]], [a[1], b[1], c[1], d[1]], [c0, c0, c1, c1], 0)

    def clump(self, rng, center, radius, count, species, card, flat=1.0):
        cx, cy = CELLS[species]
        u0, u1, v0, v1 = cx * .5 + .005, cx * .5 + .495, .5 - cy * .5 + .005, 1 - cy * .5 - .005
        for _ in range(count):
            z = rng.uniform(-1, 1)
            phi = rng.uniform(0, 2 * math.pi)
            ring = math.sqrt(1 - z * z)
            out = Vector((ring * math.cos(phi), ring * math.sin(phi), z))
            depth = rng.uniform(.3, 1.0) ** .5
            p = center + Vector((out.x * radius, out.y * radius, out.z * radius * flat)) * depth
            # Cards face out of the clump, jittered and rolled: random orientations left edge-on slivers and long intersection cuts.
            facing = (out + Vector((rng.uniform(-.55, .55), rng.uniform(-.55, .55), rng.uniform(-.55, .55)))).normalized()
            ax = Quaternion(facing, rng.uniform(0, 2 * math.pi)) @ facing.orthogonal().normalized()
            ay = facing.cross(ax)
            h = rng.uniform(.75, 1.15) * card / 2
            corners = [p - ax * h - ay * h, p + ax * h - ay * h, p + ax * h + ay * h, p - ax * h + ay * h]
            top = min(1, max(0, (p.z - center.z) / (2 * radius * flat) + .5))
            self.face(corners, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)], [(c - center).normalized() for c in corners],
                      [(self.sway(c), .85 + .15 * top, 0, 1) for c in corners], 1)

    def volume(self):
        """Shade the foliage as one volume: normals follow the crown's bounding ellipsoid and the inside and underside darken."""
        idx = [i for f, m in zip(self.faces, self.mats) if m == 1 for i in f]
        if not idx:
            return
        lo = Vector(tuple(min(self.verts[i][k] for i in idx) for k in range(3)))
        hi = Vector(tuple(max(self.verts[i][k] for i in idx) for k in range(3)))
        c, r = (lo + hi) / 2, Vector(tuple(max(.4, (hi[k] - lo[k]) / 2) for k in range(3)))
        for i in idx:
            e = Vector(tuple((self.verts[i][k] - c[k]) / r[k] for k in range(3)))
            grad = Vector((e.x / r.x, e.y / r.y, e.z / r.z)).normalized()
            self.nors[i] = tuple((grad * .8 + Vector(self.nors[i]) * .2).normalized())
            t = min(1, max(0, (e.length - .25) / .75))
            ao = (self.shade + (1 - self.shade) * t * t * (3 - 2 * t)) * (.86 + .14 * min(1, max(0, (e.z + 1) / 2)))
            col = self.cols[i]
            self.cols[i] = (col[0], col[1] * ao, col[2], col[3])

    def tris(self):
        return sum(len(f) - 2 for f in self.faces)


def polyline(rng, origin, direction, length, segments, wander=.15, lift=0.0):
    pts, d = [origin], direction.normalized()
    for _ in range(segments):
        d = (d + Vector((rng.uniform(-wander, wander), rng.uniform(-wander, wander), rng.uniform(-wander * .4, wander * .4) + lift))).normalized()
        pts.append(pts[-1] + d * length / segments)
    return pts


def branch(b, rng, origin, direction, length, radius, depth, tips, lift=.05, spread=.8):
    pts = polyline(rng, origin, direction, length, 4, lift=lift)
    radii = [radius * (1 - .7 * i / 4) for i in range(5)]
    b.tube(pts, radii, 8 if radius > .1 else 6)
    if depth > 0:
        for _ in range(rng.randint(2, 3)):
            i = rng.randint(2, 3)
            side = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-.2, .6))).normalized()
            branch(b, rng, pts[i], (pts[-1] - pts[0]).normalized() + side * spread, length * rng.uniform(.45, .65), radii[i] * .75, depth - 1, tips, lift, spread)
    else:
        tips.append(pts[-1])


def keyaki(seed):
    rng = random.Random(seed)
    b = Builder(rng.uniform(6.5, 7.5), 4.0)
    top = Vector((rng.uniform(-.15, .15), rng.uniform(-.15, .15), 2.2))
    b.tube(polyline(rng, Vector((0, 0, -SINK)), top - Vector((0, 0, -SINK)), top.length + SINK, 3, .05), [.36, .3, .26, .23])
    tips, limbs = [], rng.randint(4, 5)
    for i in range(limbs):
        ang, tilt = 2 * math.pi * i / limbs + rng.uniform(-.3, .3), math.radians(rng.uniform(28, 42))
        d = Vector((math.sin(tilt) * math.cos(ang), math.sin(tilt) * math.sin(ang), math.cos(tilt)))
        branch(b, rng, top, d, rng.uniform(3.0, 3.8), .16, 1, tips)
    rng.shuffle(tips)
    for tip in tips[:14]:
        b.clump(rng, tip + Vector((0, 0, .3)), rng.uniform(1.1, 1.6), 120, "keyaki", .62, .8)
    return b


def sugi(seed):
    rng = random.Random(seed)
    height = rng.uniform(10.5, 11.5)
    b = Builder(height, 2.0)
    b.tube(polyline(rng, Vector((0, 0, -SINK)), Vector((0, 0, 1)), height + SINK, 6, .02), [.36, .3, .24, .18, .12, .07, .03])
    z, levels = 2.4, 0
    while z < height - 1:
        f = (z - 2.4) / (height - 2.4)
        for k in range(4):
            ang = 2 * math.pi * k / 4 + levels * .7 + rng.uniform(-.2, .2)
            d = Vector((math.cos(ang), math.sin(ang), rng.uniform(-.25, -.05)))
            pts = polyline(rng, Vector((0, 0, z)), d, 1.35 * (1 - f) + .3, 2, .1)
            b.tube(pts, [.06 * (1 - f) + .02, .03, .012], 5)
        z += .75
        levels += 1
    count = 10
    for i in range(count):
        f = i / (count - 1)
        zc = 3.1 + f * (height - 3.6)
        r = 1.55 * (1 - f) + .45
        b.clump(rng, Vector((rng.uniform(-.15, .15), rng.uniform(-.15, .15), zc)), r, 64, "sugi", .62, .7)
        if f < .6:
            for _ in range(2):
                ang = rng.uniform(0, 2 * math.pi)
                b.clump(rng, Vector((math.cos(ang) * r * .75, math.sin(ang) * r * .75, zc - .2)), r * .55, 30, "sugi", .55, .7)
    return b


def sakura(seed):
    rng = random.Random(seed)
    b = Builder(5.5, 4.5, .76)  # blossom is pale and translucent; a deep core reads as brown
    top = Vector((rng.uniform(-.2, .2), rng.uniform(-.2, .2), 1.4))
    b.tube(polyline(rng, Vector((0, 0, -SINK)), top - Vector((0, 0, -SINK)), top.length + SINK, 2, .08), [.32, .26, .22])
    tips, limbs = [], rng.randint(3, 4)
    for i in range(limbs):
        ang, tilt = 2 * math.pi * i / limbs + rng.uniform(-.35, .35), math.radians(rng.uniform(50, 66))
        d = Vector((math.sin(tilt) * math.cos(ang), math.sin(tilt) * math.sin(ang), math.cos(tilt)))
        branch(b, rng, top, d, rng.uniform(2.8, 3.4), .15, 1, tips, lift=-.02, spread=.6)
    rng.shuffle(tips)
    for tip in tips[:10]:
        b.clump(rng, tip + Vector((0, 0, .15)), rng.uniform(1.0, 1.35), 140, "sakura", .56, .6)
    return b


def bamboo(seed):
    rng = random.Random(seed)
    b = Builder(7.5, 2.5)
    for _ in range(rng.randint(12, 16)):
        a, r = rng.uniform(0, 2 * math.pi), rng.uniform(0, 1.0) ** .7
        base = Vector((math.cos(a) * r, math.sin(a) * r, -SINK))
        lean = Vector((math.cos(a), math.sin(a), 0)) * rng.uniform(.04, .12)
        h = rng.uniform(5, 8)
        pts, d = [base], Vector((0, 0, 1))
        for i in range(5):
            d = (d + lean * (1 + i * .6)).normalized()
            pts.append(pts[-1] + d * h / 5)
        radius = rng.uniform(.04, .06)
        b.tube(pts, [radius] * 5 + [radius * .6], 6)
        for i in range(rng.randint(3, 4)):
            t = rng.uniform(.62, 1.0)
            k = min(4, int(t * 5))
            p = pts[k].lerp(pts[k + 1], t * 5 - k)
            b.clump(rng, p + lean * 4, rng.uniform(.5, .8), 30, "bamboo", .55, .8)
    return b


def kaki(seed):
    """A persimmon: a short trunk and gnarled limbs spreading wide and low, the crown thin enough for World.cs to hang fruit on it."""
    rng = random.Random(seed)
    b = Builder(4.6, 3.2, .66)
    top = Vector((rng.uniform(-.2, .2), rng.uniform(-.2, .2), 1.2))
    b.tube(polyline(rng, Vector((0, 0, -SINK)), top - Vector((0, 0, -SINK)), top.length + SINK, 2, .1), [.3, .25, .2])
    tips, limbs = [], rng.randint(4, 5)
    for i in range(limbs):
        ang, tilt = 2 * math.pi * i / limbs + rng.uniform(-.3, .3), math.radians(rng.uniform(42, 58))
        d = Vector((math.sin(tilt) * math.cos(ang), math.sin(tilt) * math.sin(ang), math.cos(tilt)))
        branch(b, rng, top, d, rng.uniform(2.0, 2.5), .13, 1, tips, lift=.0, spread=.7)
    rng.shuffle(tips)
    for tip in tips[:12]:
        b.clump(rng, tip + Vector((0, 0, .15)), rng.uniform(.8, 1.05), 80, "keyaki", .55, .7)
    return b


def shrub(seed):
    rng = random.Random(seed)
    b = Builder(1.4, 1.5)
    for _ in range(rng.randint(3, 5)):
        a, r = rng.uniform(0, 2 * math.pi), rng.uniform(0, .7)
        c = Vector((math.cos(a) * r, math.sin(a) * r, rng.uniform(.5, .8)))
        b.tube([Vector((c.x * .3, c.y * .3, -.2)), c * .6, c], [.035, .025, .015], 5)
        b.clump(rng, c, rng.uniform(.5, .75), 60, "keyaki", .42, .85)
    return b


def export(builder, name, species):
    builder.volume()
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(builder.verts, [], builder.faces)
    uv = mesh.uv_layers.new(name="UVMap")
    for i, value in enumerate(builder.uvs):
        uv.data[i].uv = value
    col = mesh.color_attributes.new("Col", 'FLOAT_COLOR', 'CORNER')  # float colors are exported linear, unconverted
    for i, value in enumerate(builder.cols):
        col.data[i].color = value
    mesh.color_attributes.active_color = col
    for mat_name in (f"bark_{species}", "foliage"):
        mesh.materials.append(bpy.data.materials.get(mat_name) or bpy.data.materials.new(mat_name))
    for poly, m in zip(mesh.polygons, builder.mats):
        poly.material_index = m
        poly.use_smooth = True
    mesh.normals_split_custom_set(builder.nors)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for o in bpy.context.scene.objects:
        o.select_set(o == obj)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True, export_vertex_color='ACTIVE',
                              export_normals=True, export_materials='EXPORT', export_image_format='NONE', export_yup=True)
    tris = builder.tris()
    assert tris <= MAX_TRIS, f"{name}: {tris} triangles exceeds {MAX_TRIS}"
    print(f"PROP {name} triangles={tris} height={max(v[2] for v in builder.verts):.2f}")


def main():
    os.makedirs(OUT, exist_ok=True)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    plan = [("keyaki", keyaki, "abc"), ("sugi", sugi, "abc"), ("sakura", sakura, "ab"), ("bamboo", bamboo, "a"), ("shrub", shrub, "ab"), ("kaki", kaki, "ab")]
    for species, build, variants in plan:
        for i, v in enumerate(variants):
            export(build(1000 * (len(species) + i) + ord(v)), f"{species}_{v}", "keyaki" if species in ("shrub", "kaki") else species)
    print("PROPS_OK")


main()
