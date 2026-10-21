"""Deterministic village prop GLBs for Hoshikawa: street lamp, bench, barrel, the bamboo fence post and panel, and the
water and field props: brook pebbles and rocks, the paddy sluice, water-wheel trough and hasa rack, field fence post and rail, cabbage and daikon.

Run: /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_village_props.py

Built to real-world scale for 1.6-1.8 m characters, origin on the ground, Z up in Blender (Y up once exported).
Each GLB carries geometry, UVs and material *names* only; World.Prop() binds them to the existing art:
cedar = village-atlas.png boards (one tile = TILE metres, grain along V), bark_bamboo = bark-bamboo.png (wraps once
round a culm, a node every half tile) and paper = the lit lantern colour. The water and field props add stone (the masonry
atlas cell, two tiles per UV unit), straw, leaf, leaf_dark and root, flat colours. Merge() bakes one mesh per material per
32 m cell, so each material costs a draw call (and its shadow passes) in every cell a prop stands in: stone and straw are
the materials the primitives there already use, and the three crop colours stand only in Komorebi's garden beds.
Nothing is random, so a rebuild reproduces the committed files.
"""
import math
import os

import bpy
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "models", "props")
TILE = 1.2
MATERIALS = ("cedar", "paper", "bark_bamboo", "stone", "straw", "leaf", "leaf_dark", "root")


def jitter(i, k):
    """Deterministic noise in 0..1."""
    return math.sin(i * 12.9898 + k * 78.233) * 43758.5453 % 1.0


class Builder:
    def __init__(self):
        self.verts, self.faces, self.uvs, self.mats, self.smooth = [], [], [], [], []

    def face(self, pts, uvs, mat, smooth=False, ids=None):
        if ids is None:
            ids = list(range(len(self.verts), len(self.verts) + len(pts)))
            self.verts += [tuple(p) for p in pts]
        self.faces.append(ids)
        self.uvs += uvs
        self.mats.append(MATERIALS.index(mat))
        self.smooth.append(smooth)

    def box(self, centre, size, mat, grain=2, shift=(0, 0), rot=Matrix.Identity(3)):
        """Axis-aligned in its own frame; each face maps the grain axis to V when the face contains it."""
        c, h = Vector(centre), Vector(size) / 2
        for axis in range(3):
            j, k = [a for a in range(3) if a != axis]
            v_axis = grain if grain in (j, k) else (j if size[j] >= size[k] else k)
            u_axis = k if v_axis == j else j
            for sign in (-1, 1):
                corners = []
                for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                    p = Vector((0, 0, 0))
                    p[axis], p[j], p[k] = sign * h[axis], a * h[j], b * h[k]
                    corners.append(p)
                normal = Vector((0, 0, 0))
                normal[axis] = sign
                if (corners[1] - corners[0]).cross(corners[2] - corners[0]).dot(normal) < 0:
                    corners.reverse()
                uvs = [(p[u_axis] / TILE + shift[0], p[v_axis] / TILE + shift[1]) for p in corners]
                self.face([c + rot @ p for p in corners], uvs, mat)

    def ring(self, centre, axis, radius, sides, phase=0.0):
        axis = Vector(axis).normalized()
        u = axis.orthogonal().normalized()
        w = axis.cross(u)
        return [Vector(centre) + (u * math.cos(phase + 2 * math.pi * i / sides) + w * math.sin(phase + 2 * math.pi * i / sides)) * radius
                for i in range(sides)]

    def tube(self, a, b, r0, r1, sides, mat, u_tiles=1.0, v_scale=1.0, smooth=True, cap_top=True, cap_bottom=True, inward=False, swap=False):
        """Frustum from a to b. U wraps u_tiles times round it, V runs along it at v_scale tiles per metre; swap exchanges them."""
        a, b = Vector(a), Vector(b)
        lo, hi = self.ring(a, b - a, r0, sides), self.ring(b, b - a, r1, sides)
        base = len(self.verts)
        self.verts += [tuple(p) for p in lo + hi]
        length = (b - a).length
        for i in range(sides):
            n = (i + 1) % sides
            ids = [base + i, base + n, base + sides + n, base + sides + i]
            uvs = [(i / sides * u_tiles, 0), ((i + 1) / sides * u_tiles, 0), ((i + 1) / sides * u_tiles, length * v_scale), (i / sides * u_tiles, length * v_scale)]
            if inward:
                ids.reverse()
                uvs.reverse()
            self.face(None, [(v, u) for u, v in uvs] if swap else uvs, mat, smooth, ids)
        for ring, flip, on in ((lo, True, cap_bottom), (hi, False, cap_top)):
            if on:
                pts = list(reversed(ring)) if flip else ring
                self.face(pts, [(p.x / TILE, p.y / TILE) for p in pts], mat)

    def rock(self, centre, radii, mat, sides=8, rings=5, seed=0, rough=.18, rot=Matrix.Identity(3)):
        """A lumpy smooth ellipsoid, its vertex radii jittered from seed; UVs are planar, one unit per metre."""
        c = Vector(centre)
        base = len(self.verts)
        def point(r, s):
            t, p = math.pi * r / rings, 2 * math.pi * s / sides
            k = 1 + rough * (jitter(seed, r * sides + s) - .5)
            return c + rot @ Vector((math.sin(t) * math.cos(p) * radii[0] * k, math.sin(t) * math.sin(p) * radii[1] * k, math.cos(t) * radii[2] * k))
        pts = [point(0, 0)] + [point(r, s) for r in range(1, rings) for s in range(sides)] + [point(rings, 0)]
        self.verts += [tuple(p) for p in pts]
        def vid(r, s):
            return base if r == 0 else base + len(pts) - 1 if r == rings else base + 1 + (r - 1) * sides + s % sides
        for r in range(rings):
            for s in range(sides):
                # Down a meridian, then on round it: the winding faces out. The poles collapse their quads to triangles.
                ids = list(dict.fromkeys([vid(r, s), vid(r + 1, s), vid(r + 1, s + 1), vid(r, s + 1)]))
                self.face(None, [(self.verts[i][0] + .5 * self.verts[i][1], self.verts[i][2] + .5 * self.verts[i][1]) for i in ids], mat, True, ids)

    def tris(self):
        return sum(len(f) - 2 for f in self.faces)


def lamp():
    b = Builder()
    b.box((0, 0, -.08), (.2, .2, .36), "cedar", grain=0, shift=(.5, .5))
    b.box((0, 0, .88), (.1, .1, 1.56), "cedar")
    b.box((0, 0, 1.635), (.36, .36, .03), "cedar", grain=0, shift=(.3, .1))
    b.box((0, 0, 1.84), (.3, .3, .38), "paper")
    for x in (-.165, .165):
        for y in (-.165, .165):
            b.box((x, y, 1.855), (.035, .035, .42), "cedar", shift=(x, y))
    for s in (-1, 1):
        b.box((0, s * .165, 2.04), (.36, .035, .03), "cedar", grain=0, shift=(.1, s * .2))
        b.box((s * .165, 0, 2.04), (.035, .36, .03), "cedar", grain=1, shift=(s * .2, .1))
        b.box((0, s * .155, 1.84), (.3, .02, .02), "cedar", grain=0, shift=(.4, s * .3))
        b.box((s * .155, 0, 1.84), (.02, .3, .02), "cedar", grain=1, shift=(s * .3, .4))
    # A shallow hipped cap: four slopes to a small flat top, a flat soffit and thin eave edges.
    eave, top, z0, z1 = .29, .04, 2.075, 2.2
    lo = [Vector((x * eave, y * eave, z0)) for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    hi = [Vector((x * top, y * top, z1)) for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    for i in range(4):
        n = (i + 1) % 4
        quad = [lo[i], lo[n], hi[n], hi[i]]
        b.face(quad, [(p.x / TILE + i * .2, (p - lo[i]).length / TILE) for p in quad], "cedar")
        edge = [lo[i] - Vector((0, 0, .025)), lo[n] - Vector((0, 0, .025)), lo[n], lo[i]]
        b.face(edge, [(p.x / TILE + p.y / TILE, p.z / TILE) for p in edge], "cedar")
    b.face(hi, [(p.x / TILE, p.y / TILE) for p in hi], "cedar")
    soffit = [p - Vector((0, 0, .025)) for p in reversed(lo)]
    b.face(soffit, [(p.x / TILE, p.y / TILE) for p in soffit], "cedar")
    b.tube((0, 0, z1), (0, 0, z1 + .07), .028, .008, 8, "cedar", smooth=True, cap_bottom=False)
    return b


def bench():
    b = Builder()
    for i, y in enumerate((-.14, 0, .14)):
        b.box((0, y, .47), (1.6, .13, .04), "cedar", grain=0, shift=(i * .37, i * .23))
    for x in (-.62, .62):
        b.box((x, 0, .425), (.07, .4, .05), "cedar", grain=1, shift=(x, .5))
        b.box((x, 0, .12), (.05, .3, .05), "cedar", grain=1, shift=(x, .7))
        for y in (-.15, .15):
            b.box((x, y, .17), (.06, .06, .46), "cedar", shift=(x + y, 0))
    for y in (-.15, .15):
        b.box((0, y, .12), (1.24, .04, .05), "cedar", grain=0, shift=(.2, y))
    return b


def barrel():
    b = Builder()
    # Staves stay faceted and the planks run up them; the chime stands 4 cm proud of the lid.
    b.tube((0, 0, 0), (0, 0, .8), .29, .315, 18, "cedar", u_tiles=2, v_scale=1 / TILE, smooth=False, cap_top=False, cap_bottom=False)
    b.tube((0, 0, .76), (0, 0, .8), .296, .3, 18, "cedar", u_tiles=2, v_scale=1 / TILE, smooth=False, cap_top=False, cap_bottom=False, inward=True)
    lid = b.ring((0, 0, .76), (0, 0, 1), .296, 18)
    b.face(lid, [(p.x / TILE + .3, p.y / TILE) for p in lid], "cedar")
    outer, inner = b.ring((0, 0, .8), (0, 0, 1), .315, 18), b.ring((0, 0, .8), (0, 0, 1), .3, 18)
    for i in range(18):
        n = (i + 1) % 18
        quad = [inner[i], inner[n], outer[n], outer[i]]
        b.face(quad, [(i / 9, 0), ((i + 1) / 9, 0), ((i + 1) / 9, .02), (i / 9, .02)], "cedar")
    for z in (.1, .42, .7):
        r = .29 + .025 * z / .8 + .011
        # Split-bamboo hoops: the culm runs round the barrel, so the texture's V follows the hoop.
        b.tube((0, 0, z - .024), (0, 0, z + .024), r, r, 24, "bark_bamboo", u_tiles=2 * math.pi * r / .6, v_scale=1 / .6, swap=True)
    return b


def fence_post():
    b = Builder()
    b.tube((0, 0, -.2), (0, 0, .9), .035, .032, 10, "bark_bamboo", v_scale=1 / .6, cap_bottom=False)
    return b


def fence_panel():
    """One 1.2 m bay of yotsume-gaki along Y (the game's Z): three rails, three canes alternating sides, dark ties."""
    b = Builder()
    for z in (.25, .5, .75):
        b.tube((0, -.63, z), (0, .63, z), .018, .017, 8, "bark_bamboo", v_scale=1 / .6)
    for i, y in enumerate((-.3, 0, .3)):
        x = .03 if i % 2 else -.03
        b.tube((x, y, .02), (x, y, .82), .012, .011, 6, "bark_bamboo", v_scale=1 / .6)
        for z in (.25, .5, .75):
            b.box((x / 2, y, z), (.07, .028, .028), "cedar", grain=0, rot=Matrix.Rotation(math.radians(45), 3, 'X'))
    return b


def pebbles():
    """A spill of seven flattened river pebbles within half a metre, each a third below the origin to bed into a sloping bank."""
    b = Builder()
    for i in range(7):
        a, d, r = i * 2.4, .1 + .3 * jitter(i, 1), .05 + .07 * jitter(i, 2)
        b.rock((math.cos(a) * d, math.sin(a) * d, r * .15), (r * (1 + .3 * jitter(i, 3)), r, r * .5), "stone", sides=7, rings=4, seed=i + 10)
    return b


def river_rock():
    """One water-worn rock, origin at its middle: World sinks it to leave its upper third above the brook."""
    b = Builder()
    b.rock((0, 0, 0), (.48, .36, .3), "stone", sides=10, rings=6, seed=3, rough=.22)
    return b


def sluice():
    """A sluice gate for a 0.9 m channel across Y (the game's Z): grooved posts, the board in its slots, a beam across the top."""
    b = Builder()
    for y in (-.5, .5):
        b.box((0, y, .2), (.1, .1, .9), "cedar", shift=(y, 0))
        for x in (-.035, .035):
            b.box((x, y - math.copysign(.065, y), .2), (.02, .03, .9), "cedar", shift=(x, y))
    b.box((0, 0, .17), (.03, .92, .36), "cedar", grain=1, shift=(.3, .2))
    b.box((0, 0, .41), (.03, .14, .12), "cedar", shift=(.6, .1))
    b.box((0, 0, .69), (.12, 1.16, .08), "cedar", grain=1, shift=(.1, .7))
    return b


def trough():
    """A board water trough (toi) 2 m along Y (the game's Z), open at both ends: World stretches it from the water wheel to the channel."""
    b = Builder()
    b.box((0, 0, .015), (.32, 2, .03), "cedar", grain=1)
    for x in (-.145, .145):
        b.box((x, 0, .1), (.03, 2, .17), "cedar", grain=1, shift=(x, .4))
    return b


def hasa():
    """A hasa-gake drying rack 5.4 m along X: crossed bamboo legs holding two rails, straw sheaves split over the top one."""
    b = Builder()
    legs = (-2.55, -.85, .85, 2.55)
    for x in legs:
        for s in (-1, 1):
            b.tube((x, s * .42, -.15), (x, -s * .12, 1.62), .025, .02, 6, "bark_bamboo", v_scale=1 / .6)
    for z, r in ((1.42, .026), (.92, .022)):
        b.tube((-2.75, 0, z), (2.75, 0, z), r, r, 6, "bark_bamboo", v_scale=1 / .6)
    # Each sheaf is bound at the rail and split over it, its two halves flaring to a brush; open-ended, as only their sides show.
    for i in range(30):
        x = -2.32 + i * .16
        if min(abs(x - leg) for leg in legs) < .1:
            continue
        drop = .55 + .15 * jitter(i, 4)
        for s in (-1, 1):
            b.tube((x, s * .035, 1.44), (x + .03 * (jitter(i, 5) - .5), s * (.13 + .04 * jitter(i, 6)), 1.44 - drop), .03, .075, 5, "straw",
                   cap_top=False, cap_bottom=False)
    return b


def field_post():
    """A weathered field-fence post, seven rough faces, 0.2 m in the ground and cut to a shallow point 1.31 m up."""
    b = Builder()
    b.tube((0, 0, -.2), (0, 0, 1.24), .075, .068, 7, "cedar", u_tiles=.4, v_scale=1 / TILE, smooth=False, cap_top=False)
    top, apex = b.ring((0, 0, 1.24), (0, 0, 1), .068, 7), Vector((0, 0, 1.31))
    for i in range(7):
        tri = [top[i], top[(i + 1) % 7], apex]
        b.face(tri, [(p.x / TILE + i * .1, p.z / TILE) for p in tri], "cedar")
    return b


def field_rail():
    """A split field-fence rail, 2 m along Y (the game's Z): World stretches it to each post-to-post span."""
    b = Builder()
    b.tube((0, -1, 0), (0, 1, 0), .06, .055, 6, "cedar", u_tiles=.3, v_scale=1 / TILE, smooth=False)
    return b


def cabbage():
    """A cabbage head cupped by five outer leaves, half a metre across."""
    b = Builder()
    b.rock((0, 0, .13), (.15, .15, .13), "leaf", seed=21, rough=.08)
    for i in range(5):
        a = i * 2 * math.pi / 5 + .3
        rot = Matrix.Rotation(a, 3, 'Z') @ Matrix.Rotation(math.radians(-35), 3, 'Y')
        b.rock((math.cos(a) * .15, math.sin(a) * .15, .08), (.15, .11, .025), "leaf_dark", sides=7, rings=4, seed=30 + i, rough=.12, rot=rot)
    return b


def daikon():
    """A daikon's white shoulder above the soil under a rosette of six long leaves."""
    b = Builder()
    b.tube((0, 0, -.06), (0, 0, .1), .045, .04, 8, "root", cap_bottom=False)
    for i in range(6):
        a, lift = i * math.pi / 3 + .2, math.radians(50 + 8 * (i % 2))
        rot = Matrix.Rotation(a, 3, 'Z') @ Matrix.Rotation(-lift, 3, 'Y')
        b.rock((math.cos(a) * .2 * math.cos(lift), math.sin(a) * .2 * math.cos(lift), .1 + .2 * math.sin(lift)), (.2, .05, .015), "leaf_dark",
               sides=6, rings=4, seed=40 + i, rough=.15, rot=rot)
    return b


def export(builder, name, limit):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(builder.verts, [], builder.faces)
    uv = mesh.uv_layers.new(name="UVMap")
    for i, value in enumerate(builder.uvs):
        uv.data[i].uv = value
    for mat_name in MATERIALS:
        mesh.materials.append(bpy.data.materials.get(mat_name) or bpy.data.materials.new(mat_name))
    for poly, m, smooth in zip(mesh.polygons, builder.mats, builder.smooth):
        poly.material_index = m
        poly.use_smooth = smooth
    mesh.validate()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for o in bpy.context.scene.objects:
        o.select_set(o == obj)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True, export_vertex_color='NONE',
                              export_normals=True, export_materials='EXPORT', export_image_format='NONE', export_yup=True)
    tris = builder.tris()
    assert tris <= limit, f"{name}: {tris} triangles exceeds {limit}"
    size = [max(v[i] for v in builder.verts) - min(v[i] for v in builder.verts) for i in range(3)]
    print(f"PROP {name} triangles={tris} size={size[0]:.2f}x{size[1]:.2f}x{size[2]:.2f} top={max(v[2] for v in builder.verts):.2f}")


def main():
    os.makedirs(OUT, exist_ok=True)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)
    for name, build, limit in (("lamp", lamp, 600), ("bench", bench, 600), ("barrel", barrel, 900), ("fence_post", fence_post, 200), ("fence_panel", fence_panel, 600),
                              ("pebbles", pebbles, 600), ("river_rock", river_rock, 300), ("sluice", sluice, 300), ("hasa", hasa, 1200),
                              ("field_post", field_post, 100), ("field_rail", field_rail, 60), ("cabbage", cabbage, 400), ("daikon", daikon, 400),
                              ("trough", trough, 60)):
        export(build(), name, limit)
    print("VILLAGE_PROPS_OK")


main()
