"""Mio's bedding for her room (World.Room): the shikibuton, a buckwheat pillow, and the kakebuton draped over her as she sleeps, with
two shape keys: 'sit', the cover once the wake clip has sat her up, and 'folded', folded in half at the futon's foot as she leaves it
when she rises (Game.Cue() blends them). The cover is cloth dropped onto her own body from mio.glb and settled, once in the sleep
clip's pose and once sat up at the wake clip's end, so it lies on her rather than through her; the folded cover is worked out.

Run: xvfb-run -a blender --background --factory-startup --python tools/build_bedding.py [-- --preview=DIR --cache=FILE]   (any Blender 4; about a minute)
--preview renders her asleep, sitting up and the folded cover from the side and above, to tune the shapes without the game, and
--cache=FILE keeps the cloth's simulated points there, so a rerun only rebuilds the quilt.

Built about her origin on the futon (World.Bed): Z up in Blender (Y up once exported), her head toward +Y, her feet toward -Y (the
shoji, +Z in Godot). Each part is a quilted shell round a sheet: the sheet is what the cloth solver moves, and its thickness puffs
up between the stitch lines and rounds over at the edges. The GLB carries geometry, UVs in metres and material *names* only (cotton,
pillow, cover), which Room() binds.
"""
import json
import math
import os
import sys
import time

import bpy
from mathutils import Matrix, Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "assets", "models", "props", "bedding.glb")
FPS = 24
# The futon, the cover's sheet and the pillow (centre, width along X, length along Y), metres about World.Bed.
FUTON = ((0, -.1), 1.0, 2.2, .06)
COVER = ((0, -.575), 1.4, 1.55)
PILLOW = ((.02, .8), .5, .3, .1)
STEP = .03
# Her two poses under the cover (clip, frame): asleep, and sat up at the end of the wake clip, where the cover lies SHOVE metres
# further toward her feet; and the frames each drop of the cover settles over.
POSES = {"asleep": ("sleep_Mio", 1), "sit": ("wake_Mio", 90)}
SHOVE, SETTLE = .4, 30


def jitter(i, k):
    """Deterministic noise in 0..1."""
    return math.sin(i * 12.9898 + k * 78.233) * 43758.5453 % 1.0


def smooth(a, b, x):
    x = min(max((x - a) / (b - a), 0), 1)
    return x * x * (3 - 2 * x)


class Sheet:
    """A grid of nx by ny points, i across X and j from the head end (+Y) toward the feet, with (u, v) in metres on the flat sheet."""

    def __init__(self, width, length, step):
        self.nx, self.ny = round(width / step) + 1, round(length / step) + 1
        self.width, self.length = width, length
        self.uv = [((i / (self.nx - 1) - .5) * width, j / (self.ny - 1) * length) for j in range(self.ny) for i in range(self.nx)]

    def at(self, i, j):
        return j * self.nx + i

    def faces(self):
        return [(self.at(i, j), self.at(i, j + 1), self.at(i + 1, j + 1), self.at(i + 1, j)) for j in range(self.ny - 1) for i in range(self.nx - 1)]

    def border(self):
        """The edge points in order round the sheet, each with its outward direction in (u, v)."""
        loop = [(i, 0, (0, -1)) for i in range(self.nx - 1)] + [(self.nx - 1, j, (1, 0)) for j in range(self.ny - 1)]
        loop += [(i, self.ny - 1, (0, 1)) for i in range(self.nx - 1, 0, -1)] + [(0, j, (-1, 0)) for j in range(self.ny - 1, 0, -1)]
        out = []
        for i, j, d in loop:
            corner_u = -1 if i == 0 else 1 if i == self.nx - 1 else 0
            corner_v = -1 if j == 0 else 1 if j == self.ny - 1 else 0
            out.append((i, j, (corner_u, corner_v) if corner_u and corner_v else d))
        return out

    def frame(self, points, i, j):
        """The sheet's normal and its (u, v) directions at a point of a state."""
        p = lambda a, b: Vector(points[self.at(min(max(a, 0), self.nx - 1), min(max(b, 0), self.ny - 1))])
        du, dv = p(i + 1, j) - p(i - 1, j), p(i, j + 1) - p(i, j - 1)
        return dv.cross(du).normalized(), du.normalized(), dv.normalized()


def shell(name, sheet, states, thickness, material, rim=4):
    """A closed quilt round each state of the sheet, offset half its thickness either side, the edges rounded over; the first state
    is the mesh, the rest shape keys named by their keys."""
    names, poses = list(states), list(states.values())
    t = [thickness(u, v) for u, v in sheet.uv]
    edge = sheet.border()
    count = len(sheet.uv)

    def build(points):
        top, bottom = [], []
        for j in range(sheet.ny):
            for i in range(sheet.nx):
                n, _, _ = sheet.frame(points, i, j)
                p, h = Vector(points[sheet.at(i, j)]), t[sheet.at(i, j)] / 2
                top.append(p + n * h)
                bottom.append(p - n * h)
        ring = []
        for i, j, (ou, ov) in edge:
            n, du, dv = sheet.frame(points, i, j)
            out = (du * ou + dv * ov)
            out = (out - n * out.dot(n)).normalized()
            p, h = Vector(points[sheet.at(i, j)]), t[sheet.at(i, j)] / 2
            ring.append([p + n * h * math.cos(math.pi * k / rim) + out * h * math.sin(math.pi * k / rim) for k in range(1, rim)])
        return top + bottom + [q for r in ring for q in r]

    verts = build(poses[0])
    faces, uvs = [], []
    for f in sheet.faces():
        faces.append(f)
        uvs.append([sheet.uv[k] for k in f])
        faces.append(tuple(count + k for k in reversed(f)))
        uvs.append([sheet.uv[k] for k in reversed(f)])
    # The rim: the top's edge point, the ring round the edge, the bottom's edge point.
    base = 2 * count
    column = lambda e: [sheet.at(edge[e][0], edge[e][1])] + [base + e * (rim - 1) + k for k in range(rim - 1)] + [count + sheet.at(edge[e][0], edge[e][1])]
    along = 0
    for e in range(len(edge)):
        a, b = column(e), column((e + 1) % len(edge))
        step = (Vector(sheet.uv[a[0]]) - Vector(sheet.uv[b[0]])).length
        for k in range(rim):
            h = t[a[0]] / 2 * math.pi / rim
            faces.append((a[k], b[k], b[k + 1], a[k + 1]))
            uvs.append([(along, k * h), (along + step, k * h), (along + step, (k + 1) * h), (along, (k + 1) * h)])
        along += step
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    layer = mesh.uv_layers.new(name="UVMap")
    for poly, corner_uvs in zip(mesh.polygons, uvs):
        for loop, uv in zip(poly.loop_indices, corner_uvs):
            layer.data[loop].uv = uv
    mesh.materials.append(bpy.data.materials.get(material) or bpy.data.materials.new(material))
    for poly in mesh.polygons:
        poly.use_smooth = True
    mesh.validate()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if len(poses) > 1:
        obj.shape_key_add(name="Basis")
        for key, points in zip(names[1:], poses[1:]):
            shape = obj.shape_key_add(name=key)
            for k, v in enumerate(build(points)):
                shape.data[k].co = v
    return obj


def quilted(width, length, full, edge, cells, stitch, tuft=0.0, band=.06):
    """Thickness over (u, v): full between the stitch lines, pinched to stitch on them, rounded down to edge over the border band;
    with tuft, only knots at the stitch crossings pull it in, as on a shikibuton."""
    cu, cv = width / cells[0], length / cells[1]

    def thickness(u, v):
        x, y = u + width / 2, v
        border = smooth(0, band, min(x, width - x, y, length - y))
        if tuft:
            dx, dy = x % cu - cu / 2, y % cv - cv / 2
            puff = full - tuft * math.exp(-(dx * dx + dy * dy) / (2 * .02 ** 2))
        else:
            puff = stitch + (full - stitch) * (abs(math.sin(math.pi * x / cu)) * abs(math.sin(math.pi * y / cv))) ** .35
        return edge + (puff - edge) * border
    return thickness


def flat(sheet, centre, z):
    return [(centre[0] + u, centre[1] + sheet.length / 2 - v, z) for u, v in sheet.uv]


def mio():
    """Her body and rig from mio.glb, without the sword."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "assets", "models", "mio.glb"))
    body = next(o for o in bpy.data.objects if o not in before and o.name.startswith("Mio Tripo body"))
    for o in set(bpy.data.objects) - before:
        if o.type == 'MESH' and o != body:
            bpy.data.objects.remove(o)
    body.parent.animation_data_create()
    return body


def pose(body, name):
    action, frame = POSES[name]
    body.parent.animation_data.action = bpy.data.actions[action]
    bpy.context.scene.frame_set(frame)


def statue(body, name):
    """Her body in a pose as a still, closed collider. It is remeshed in voxels: her jacket's loose flaps and open seams snag the
    cloth's edge and fling it up, and a collider that moves while the cloth settles drags it off her."""
    pose(body, name)
    mesh = bpy.data.meshes.new_from_object(body.evaluated_get(bpy.context.evaluated_depsgraph_get()))
    mesh.transform(body.matrix_world)
    obj = bpy.data.objects.new("Statue", mesh)
    bpy.context.scene.collection.objects.link(obj)
    remesh = obj.modifiers.new("Remesh", 'REMESH')
    remesh.mode, remesh.voxel_size = 'VOXEL', .015
    collider(obj)
    return obj


def collider(obj, outer=.004):
    obj.modifiers.new("Collision", 'COLLISION')
    obj.collision.thickness_outer = outer
    obj.collision.cloth_friction = 40


def solid(name, points, faces):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def simulate(sheet, start):
    """Drops the cloth from its start onto the colliders in the scene and returns its points once it has settled. It is stiff to
    bend, as a quilt is, so it lies in a few broad folds rather than a sheet's many small ones."""
    obj = solid("Cloth", start, sheet.faces())
    cloth = obj.modifiers.new("Cloth", 'CLOTH')
    s, c = cloth.settings, cloth.collision_settings
    s.quality, s.mass, s.air_damping = 6, .3, 1
    s.tension_stiffness = s.compression_stiffness = 40
    s.shear_stiffness, s.bending_stiffness, s.bending_damping = 20, 40, 5
    c.collision_quality, c.distance_min, c.friction = 3, .032, 40
    cloth.point_cache.frame_start, cloth.point_cache.frame_end = 1, SETTLE
    clock = time.time()
    for f in range(1, SETTLE + 1):
        bpy.context.scene.frame_set(f)
    points = [tuple(v.co) for v in obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).data.vertices]
    print(f"BEDDING settled in {time.time() - clock:.0f} s", flush=True)
    bpy.data.objects.remove(obj)
    return points


def folded(sheet, foot, z, t, edge, fall=.2):
    """The sheet folded in half over itself, the head half on top, its fold toward the pillow and its ends at the futon's foot, the
    bottom layer's middle at z, the top one laid a little askew. Past x ±edge both layers droop off the futon to the floor in an S
    over fall metres, the top one lying along the bottom one. Worked out rather than simulated: the solver's self-collision flings two
    stacked layers apart."""
    r, half, drop = t / 2, sheet.length / 2, z - t / 2
    arc = math.pi * r
    # The S by arc length past the edge: its depth eases in and out, and its run is what the length leaves.
    table, x, step = [(0.0, 0.0, 0.0, 1.0)], 0.0, .001
    for k in range(1, 1000):
        slope = drop * (smooth(0, fall, k * step) - smooth(0, fall, (k - 1) * step)) / step
        x += step * math.sqrt(1 - slope * slope)
        table.append((x, drop * smooth(0, fall, k * step), slope, math.sqrt(1 - slope * slope)))
    points = []
    for u, v in sheet.uv:
        s = v - half
        if abs(s) <= arc / 2:
            a = math.pi / 2 - s / r
            y, lift = foot + half - arc / 2 + r * math.sin(a), r - r * math.cos(a)
        else:
            y, lift = foot + half - abs(s), 0 if s > 0 else 2 * r
        out = max(abs(u) - edge, 0)
        run, depth, nx, nz = table[min(round(out / step), len(table) - 1)]
        points.append((math.copysign(edge + run + lift * nx, u) if out else u, y + .04 * u * lift / t, z - depth + lift * nz))
    return points


def preview(out, body, parts):
    os.makedirs(out, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light, scene.display.shading.color_type = 'STUDIO', 'MATERIAL'
    scene.render.resolution_x, scene.render.resolution_y = 960, 640
    colours = {"cotton": (.91, .89, .85, 1), "pillow": (.56, .65, .71, 1), "cover": (.2, .3, .5, 1)}
    for m in bpy.data.materials:
        if m.name in colours:
            m.diffuse_color = colours[m.name]
    cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    blanket = parts["Blanket"]
    for state in ("asleep", "sit", "folded"):
        for key in blanket.data.shape_keys.key_blocks[1:]:
            key.value = 1.0 if key.name == state else 0.0
        if state in POSES:
            pose(body, state)
        body.hide_render = state not in POSES
        for view, eye, at in (("side", (1.6, -.2, .55), (0, -.3, .12)), ("top", (.01, -.25, 2.4), (0, -.25, 0)), ("foot", (.5, -2.2, .9), (0, -.2, .1))):
            cam.location = eye
            cam.rotation_euler = (Vector(at) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
            scene.render.filepath = os.path.join(out, f"{state}-{view}.png")
            bpy.ops.render.render(write_still=True)


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps, scene.frame_start, scene.frame_end = FPS, 1, SETTLE
    body = mio()
    floor = solid("Floor", [(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [(0, 1, 2, 3)])
    collider(floor)
    (fx, fy), fw, fl, fh = FUTON
    mat = Sheet(fw, fl, .04)
    futon = shell("Futon", mat, {"rest": flat(mat, (fx, fy), fh / 2)}, quilted(fw, fl, fh, .034, (4, 8), fh, tuft=.012, band=.05), "cotton")
    collider(futon)
    (px, py), pw, pl, ph = PILLOW
    pad = Sheet(pw, pl, .025)
    # Domed, the buckwheat hulls slumped to its corners, and a little lumpy.
    lump = lambda u, v: .004 * (jitter(round(u * 40), round(v * 40)) - .5)
    dome = lambda u, v: ((1 - abs(2 * u / pw) ** 3) * (1 - abs(2 * v / pl - 1) ** 3)) ** .6
    pillow = shell("Pillow", pad, {"rest": flat(pad, (px, py), ph / 2)}, lambda u, v: .022 + (ph - .022) * dome(u, v) + lump(u, v), "pillow", rim=3)
    pillow.data.transform(Matrix.Translation((px, py, 0)) @ Matrix.Rotation(math.radians(-7), 4, 'Z') @ Matrix.Translation((-px, -py, 0)))
    (cx, cy), cw, cl = COVER
    sheet = Sheet(cw, cl, STEP)
    thick = quilted(cw, cl, .06, .024, (4, 4), .026)
    cache = next((a.split("=", 1)[1] for a in args if a.startswith("--cache=")), "")
    if cache and os.path.exists(cache):
        with open(cache) as f:
            asleep, sit = json.load(f)
    else:
        # Sitting up she pushes the cover toward her feet: it drops onto her legs from below her waist.
        drapes = []
        for name, start in (("asleep", flat(sheet, (cx, cy), .5)), ("sit", flat(sheet, (cx, cy - SHOVE), .55))):
            still = statue(body, name)
            drapes.append(simulate(sheet, start))
            bpy.data.objects.remove(still)
        asleep, sit = drapes
        if cache:
            with open(cache, "w") as f:
                json.dump(drapes, f)
    fold = folded(sheet, fy - fl / 2 + .04, fh + .035, .06, fw / 2 - .03)
    blanket = shell("Blanket", sheet, {"asleep": asleep, "sit": sit, "folded": fold}, thick, "cover")
    parts = {"Futon": futon, "Pillow": pillow, "Blanket": blanket}
    for o in bpy.context.scene.objects:
        o.select_set(o.name in parts)
    bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True, export_normals=True,
                              export_morph=True, export_morph_normal=True, export_materials='EXPORT', export_image_format='NONE', export_yup=True)
    for name, o in parts.items():
        print(f"BEDDING {name} triangles={sum(len(p.vertices) - 2 for p in o.data.polygons)}")
    for a in args:
        if a.startswith("--preview="):
            preview(a.split("=", 1)[1], body, parts)
    print("BEDDING_OK")


main()
