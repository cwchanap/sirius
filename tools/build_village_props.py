"""Deterministic village prop GLBs for Hoshikawa: street lamp, bench, barrel and the bamboo fence post and panel.

Run: /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python tools/build_village_props.py

Built to real-world scale for 1.6-1.8 m characters, origin on the ground, Z up in Blender (Y up once exported).
Each GLB carries geometry, UVs and material *names* only; World.Prop() binds them to the existing art:
cedar = village-atlas.png boards (one tile = TILE metres, grain along V), bark_bamboo = bark-bamboo.png (wraps once
round a culm, a node every half tile) and paper = the lit lantern colour. Three materials only: Merge() bakes one mesh per
material per 32 m cell, so each extra material costs a draw call (and its shadow passes) in every cell a prop stands in.
Nothing is random, so a rebuild reproduces the committed files.
"""
import math
import os

import bpy
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "models", "props")
TILE = 1.2
MATERIALS = ("cedar", "paper", "bark_bamboo")


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
    for name, build, limit in (("lamp", lamp, 600), ("bench", bench, 600), ("barrel", barrel, 900), ("fence_post", fence_post, 200), ("fence_panel", fence_panel, 600)):
        export(build(), name, limit)
    print("VILLAGE_PROPS_OK")


main()
