"""Repaint a Tripo character's face where its multi-view bake put one surface's colour on another: hair strands painted over the
brows, eyelids, forehead and temples, and skin painted along the edges of the bangs and side locks.

blender --background --factory-startup --python tools/face_paint.py -- [mio] [yui] [--source=MAP] [--map]
On a committed model, repaints the base colour map its GLB carries (or --source, such as the map before an earlier repaint, from git)
and writes it into the GLB, the committed copy and Godot's extracted copy. A map named by import_character.py's digest is renamed to the
new digest, as a rebuild would name it; then import in Godot, run tools/compress_textures.py and import again. --map writes only
artifacts/face-paint/NAME-map.jpg, the repainted map, and NAME-{before,after}.png: the face from the front with its hair layers left
out, gridded every 0.05 head lengths (the darker lines are x = 0 and z = 0), which is where a FACES entry is measured.
import_tripo_cast.py repaints Yui's source map before export; Mio's committed map is the one import_tripo_mio.py packs.

Each head triangle is hair or skin by its geometry first. One that is not the first surface out from the skull's centre lies over another
(the bangs, side locks, loose strands), so it is hair, unless it is skin-coloured and joins the face (the ears, the nose). The face's own
surface is skin where its colour mostly is or inside the entry's core, hair otherwise (the hair cap, locks lying on a cheek), smoothed
over the mesh so a lone smeared triangle follows its neighbours. Each texel then takes its triangle's side: a hair-coloured texel on skin
becomes the skin round it and a skin-coloured texel on hair the hair round it, each averaged from the nearest clean texels in 3D. The eyes,
nose and mouth are kept as painted; the brows, which the smears broke, are drawn again as tapered curves. Only what lies within 75
degrees of the front changes on the skin, 95 on the hair: the ears, the back of the head and its ornaments stay as baked.
"""
import sys
import tempfile
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
# Front view of the head, in lengths of its Head bone from the bone's head: x to the character's left, z up.
# core: ellipses (x, z, rx, rz) of the face that are skin whatever their colour. keep: ellipses kept as painted (eyes, mouth, nose).
# brows: (inner end, peak, outer end) per brow, drawn brow[0] wide at the inner end tapering to brow[1].
# fringe: (z, band), the height above which the forehead under the bangs takes the hair's colour, fading in over the band below it, so
# the gaps between the strands read as more hair rather than holes.
FACES = {
    'mio': dict(core=[(-.085, .3, .33, .42), (.31, .27, .07, .07)],
                keep=[(-.305, .255, .15, .085), (.155, .258, .1, .08), (-.085, -.097, .12, .04), (-.085, .01, .03, .03)],
                brows=[((-.165, .405), (-.285, .43), (-.39, .412)), ((.06, .405), (.16, .43), (.25, .412))], brow=(.014, .004)),
    'yui': dict(core=[(-.01, .29, .28, .29)], fringe=(.4, .05),
                keep=[(-.205, .18, .1, .07), (.2125, .18, .12, .07), (.003, -.12, .13, .045), (0, -.025, .045, .035)],
                brows=[((-.12, .318), (-.2, .335), (-.29, .31)), ((.1, .318), (.18, .335), (.27, .31))], brow=(.012, .004)),
}


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def lab(rgb):
    c = np.where(rgb > .04045, ((rgb + .055) / 1.055) ** 2.4, rgb / 12.92)
    xyz = c @ np.array([[.4124, .3576, .1805], [.2126, .7152, .0722], [.0193, .1192, .9505]]).T / np.array([.9505, 1, 1.089])
    f = np.where(xyz > .008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def geometry(body, rig):
    """The rest mesh in world space: triangle vertices, their UVs, each vertex's weight on the head, the Head bone's ends."""
    me = body.data
    if hasattr(me, 'calc_loop_triangles'):
        me.calc_loop_triangles()
    n = len(me.loop_triangles)
    tv, tl = np.zeros(n * 3, np.int32), np.zeros(n * 3, np.int32)
    me.loop_triangles.foreach_get('vertices', tv)
    me.loop_triangles.foreach_get('loops', tl)
    co = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get('co', co)
    m = np.array(body.matrix_world)
    co = co.reshape(-1, 3) @ m[:3, :3].T + m[:3, 3]
    uv = np.zeros(len(me.loops) * 2)
    me.uv_layers.active.data.foreach_get('uv', uv)
    groups = {g.index for g in body.vertex_groups if g.name.endswith(('Head', 'HeadTop_End'))}
    head = np.array([sum(g.weight for g in v.groups if g.group in groups) for v in me.vertices])
    bones = rig.data.bones
    at = lambda name: np.array(rig.matrix_world @ next(b for b in bones if b.name.endswith(name)).head_local)
    # The rigs face -Y: their toes are forward of their ankles.
    assert at('LeftToeBase')[1] < at('LeftFoot')[1]
    return dict(tv=tv.reshape(n, 3), uv=uv.reshape(-1, 2)[tl.reshape(n, 3)], co=co, head=head, H=at('Head'), T=at('HeadTop_End'))


def innermost(g, tris, centre):
    """Whether each triangle is the first surface out from the skull's centre at two of three points on it."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    tree = BVHTree.FromPolygons([Vector(v) for v in g['co']], [tuple(map(int, t)) for t in g['tv']], all_triangles=True)
    c, corners, out = Vector(centre), g['co'][g['tv']], np.zeros(len(g['tv']), bool)
    for t in tris:
        hits = 0
        for w in ((1 / 3, 1 / 3, 1 / 3), (.6, .2, .2), (.2, .2, .6)):
            to = Vector(np.dot(w, corners[t])) - c
            hit = tree.ray_cast(c, to.normalized(), to.length + .01)
            hits += hit[0] is not None and hit[3] >= to.length * .99
        out[t] = hits >= 2
    return out


def raster(g, tris, size):
    """The texels under each triangle (half a texel beyond its edges, so seams are covered), each once, to the triangle it lies most
    inside: flat texel index, triangle, barycentric weights."""
    p = np.stack([g['uv'][tris, :, 0] * size, (1 - g['uv'][tris, :, 1]) * size], -1) - .5
    keys, owners, bary, inside = [], [], [], []
    for k, t in enumerate(tris):
        a, b, c = p[k]
        (x0, y0), (x1, y1) = np.floor(p[k].min(0)).astype(int), np.ceil(p[k].max(0)).astype(int)
        x, y = np.meshgrid(np.arange(max(x0, 0), min(x1, size - 1) + 1), np.arange(max(y0, 0), min(y1, size - 1) + 1))
        x, y = x.ravel(), y.ravel()
        u, v = b - a, c - a
        den = u[0] * v[1] - v[0] * u[1]
        if abs(den) < 1e-12:
            continue
        l1 = ((x - a[0]) * v[1] - v[0] * (y - a[1])) / den
        l2 = (u[0] * (y - a[1]) - (x - a[0]) * u[1]) / den
        l = np.stack([1 - l1 - l2, l1, l2], 1)
        edge = .5 / max(1e-6, min(np.linalg.norm(u), np.linalg.norm(v), np.linalg.norm(c - b)))
        m = l.min(1) >= -edge
        keys.append(y[m] * size + x[m]), owners.append(np.full(m.sum(), t)), bary.append(l[m]), inside.append(l[m].min(1))
    keys, owners, bary, inside = map(np.concatenate, (keys, owners, bary, inside))
    order = np.lexsort((-inside, keys))
    first = np.r_[True, keys[order][1:] != keys[order][:-1]]
    pick = order[first]
    return keys[pick], owners[pick], bary[pick]


def coverage(g, size):
    """Every texel any triangle covers, half a texel beyond its edges."""
    covered = np.zeros(size * size, bool)
    p = np.stack([g['uv'][..., 0] * size, (1 - g['uv'][..., 1]) * size], -1) - .5
    for a, b, c in p:
        lo, hi = np.floor(np.minimum(np.minimum(a, b), c)).astype(int), np.ceil(np.maximum(np.maximum(a, b), c)).astype(int)
        x, y = np.meshgrid(np.arange(max(lo[0], 0), min(hi[0], size - 1) + 1), np.arange(max(lo[1], 0), min(hi[1], size - 1) + 1))
        u, v = b - a, c - a
        den = u[0] * v[1] - v[0] * u[1]
        if abs(den) < 1e-12:
            continue
        l1 = ((x - a[0]) * v[1] - v[0] * (y - a[1])) / den
        l2 = (u[0] * (y - a[1]) - (x - a[0]) * u[1]) / den
        edge = .5 / max(1e-6, min(np.linalg.norm(u), np.linalg.norm(v), np.linalg.norm(c - b)))
        m = (l1 >= -edge) & (l2 >= -edge) & (1 - l1 - l2 >= -edge)
        covered[(y[m] * size + x[m])] = True
    return covered


def neighbours(g, tris, scale):
    """Up to six triangles sharing an edge with each, by welded positions (Tripo's GLBs split vertices at UV seams); -1 pads."""
    weld = np.unique(np.round(g['co'] / (scale * 1e-4)).astype(np.int64), axis=0, return_inverse=True)[1].ravel()
    edges = {}
    for t in tris:
        a, b, c = weld[g['tv'][t]]
        for e in ((a, b), (b, c), (c, a)):
            edges.setdefault((min(e), max(e)), []).append(t)
    out = np.full((len(g['tv']), 6), -1)
    fill = np.zeros(len(g['tv']), int)
    for ts in edges.values():
        for i in ts:
            for j in ts:
                if i != j and fill[i] < 6 and j not in out[i, :fill[i]]:
                    out[i, fill[i]] = j
                    fill[i] += 1
    return out


def ellipse(x, z, e):
    cx, cz, rx, rz = e
    return np.sqrt(((x - cx) / rx) ** 2 + ((z - cz) / rz) ** 2)


def nearest(points, values, queries, k):
    """The inverse-distance mean of the k nearest points' values at each query."""
    from mathutils.kdtree import KDTree
    tree = KDTree(len(points))
    for i, p in enumerate(points):
        tree.insert(p, i)
    tree.balance()
    out = np.zeros((len(queries), values.shape[1]))
    for i, q in enumerate(queries):
        found = tree.find_n(q, k)
        w = np.array([1 / (d + 1e-5) for _, _, d in found])
        out[i] = w @ values[[j for _, j, _ in found]] / w.sum()
    return out


def repaint(g, pixels, face):
    """pixels: the base colour map as float sRGB, top row first. Returns the repainted map and the triangles' classes."""
    size = pixels.shape[0]
    H, T = g['H'], g['T']
    scale = np.linalg.norm(T - H)
    centre = H + (T - H) * .45
    tris = np.where(g['head'][g['tv']].mean(1) > .5)[0]
    inner = innermost(g, tris, centre)
    corners = g['co'][g['tv']]
    mid = corners.mean(1)
    normal = np.cross(corners[:, 1] - corners[:, 0], corners[:, 2] - corners[:, 0])
    front = (normal[:, 1] < 0) & (mid[:, 1] < centre[1])
    keys, owner, bary = raster(g, tris, size)
    flat = pixels.reshape(-1, 3)
    rgb = flat[keys]
    col = lab(rgb)
    pos = np.einsum('nk,nkj->nj', bary, corners[owner])
    x, z = (pos[:, 0] - H[0]) / scale, (pos[:, 2] - H[2]) / scale
    n = len(g['tv'])
    # Skin is what the core's face mostly is, hair what the strands over it mostly are.
    qx, qz = (mid[:, 0] - H[0]) / scale, (mid[:, 2] - H[2]) / scale
    core = np.zeros(n, bool)
    core[tris] = inner[tris] & front[tris] & (np.min([ellipse(qx[tris], qz[tris], e) for e in face['core']], 0) < 1)
    outer = np.zeros(n, bool)
    outer[tris] = ~inner[tris]
    skin = np.median(col[core[owner] & (col[:, 0] > 60)], 0)
    hair = np.median(col[outer[owner] & (col[:, 0] < 50)], 0)
    axis = skin - hair
    t = (col - hair) @ axis / (axis @ axis)
    delta = np.linalg.norm(col - skin, axis=1)
    # A hair texel off the hair's hue toward the skin's, or nearly as light as the skin, is the bake's blend of the two; a highlight keeps
    # the hue and most of the darkness, an ornament (Mio's gold band) lies far off the line between them.
    hue = np.linalg.norm(col[:, 1:] - hair[1:], axis=1)
    off = np.linalg.norm(col - hair - np.clip(t, 0, 1)[:, None] * axis, axis=1)
    frac = np.bincount(owner, t > .6, n) / np.maximum(np.bincount(owner, minlength=n), 1)
    near = neighbours(g, tris, scale)
    # The face's own surface: its colour's majority, the core always skin, then each triangle follows its neighbours where they agree.
    face_tris = tris[inner[tris]]
    is_skin = np.zeros(n, bool)
    is_skin[face_tris] = (frac[face_tris] >= .5) | core[face_tris]
    for _ in range(12):
        nb = near[face_tris]
        valid = (nb >= 0) & inner[np.maximum(nb, 0)]
        skins = (valid & is_skin[np.maximum(nb, 0)]).sum(1)
        hairs = (valid & ~is_skin[np.maximum(nb, 0)]).sum(1)
        step = core[face_tris] | ((1 - frac[face_tris]) + .4 * hairs < frac[face_tris] + .4 * skins)
        if (step == is_skin[face_tris]).all():
            break
        is_skin[face_tris] = step
    # The layers over it are hair, but for the skin that stands off the face: the ears, skin-coloured out at the sides, and the nose,
    # skin-coloured triangles joined to the face under the eyes.
    turn = np.degrees(np.abs(np.arctan2(mid[:, 0] - centre[0], centre[1] - mid[:, 1])))
    is_skin[tris] |= outer[tris] & (turn[tris] > 55) & (frac[tris] >= .5)
    queue = [i for i in tris if is_skin[i]]
    while queue:
        for j in near[queue.pop()]:
            if j >= 0 and outer[j] and not is_skin[j] and frac[j] >= .5 and qz[j] < .1:
                is_skin[j] = True
                queue.append(j)
    on_skin = is_skin[owner]
    keep = np.where(front[owner], np.max([1 - smoothstep(1, 1.1, ellipse(x, z, e)) for e in face['keep']], 0), 0)
    # Only the face and the hair round it change: the skin within 75 degrees of the front, the hair within 95, so the ears, the back of
    # the head and its ornaments stay as baked.
    zone = turn[owner] < np.where(on_skin, 75, 95)
    weight = np.where(on_skin, (1 - smoothstep(.78, .95, t)) * (1 - keep), np.maximum(smoothstep(6, 12, hue), smoothstep(.55, .75, t)) * (1 - smoothstep(22, 30, off)))
    weight *= zone
    # Widen each repaint by a texel within its side, so a smear's soft rim goes with it.
    canvas = np.zeros(size * size, np.float32)
    canvas[keys] = weight
    sides = np.zeros(size * size, np.int8)
    sides[keys] = np.where(on_skin, 1, 2)
    grid, sides = canvas.reshape(size, size), sides.reshape(size, size)
    grown = grid.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx or dy:
                moved = np.roll(np.roll(grid, dy, 0), dx, 1) * (np.roll(np.roll(sides, dy, 0), dx, 1) == sides)
                np.maximum(grown, .8 * moved, out=grown)
    weight = np.maximum(weight, grown.ravel()[keys] * (1 - keep) * zone)
    fix = weight > .01
    clean_skin = on_skin & (weight < .02) & (t > .9) & (delta < 15)
    clean_hair = ~on_skin & (t < .3) & (hue < 6)
    target = rgb.copy()
    for donors, side_mask, k, stride in ((clean_skin, on_skin, 16, 2), (clean_hair, ~on_skin, 8, 3)):
        donor = np.where(donors)[0][::stride]
        query = np.where(fix & side_mask)[0]
        if len(query):
            target[query] = nearest(pos[donor], rgb[donor], pos[query], k)
    out = rgb + (target - rgb) * weight[:, None]
    if 'fringe' in face:
        line, band = face['fringe']
        under = np.where(on_skin & front[owner] & (z > line - band))[0]
        donor = np.where(clean_hair)[0][::3]
        out[under] += (nearest(pos[donor], rgb[donor], pos[under], 8) - out[under]) * smoothstep(line - band, line, z[under])[:, None]
    # Brows: a tapered curve through the inner end, the peak and the outer end, in the face's own dark hair colour.
    dark = np.where(clean_hair)[0]
    brow_rgb = rgb[dark[col[dark, 0] <= np.percentile(col[dark, 0], 20)]].mean(0)
    paint = on_skin & front[owner]
    for p0, peak, p2 in face['brows']:
        p0, peak, p2 = map(np.array, (p0, peak, p2))
        control = 2 * peak - (p0 + p2) / 2
        s = np.linspace(0, 1, 65)[:, None]
        curve = (1 - s) ** 2 * p0 + 2 * s * (1 - s) * control + s ** 2 * p2
        lo, hi = curve.min(0) - .05, curve.max(0) + .05
        sel = np.where(paint & (x > lo[0]) & (x < hi[0]) & (z > lo[1]) & (z < hi[1]))[0]
        q = np.stack([x[sel], z[sel]], 1)
        a, b = curve[:-1], curve[1:]
        ab = b - a
        u = np.clip(((q[:, None] - a) * ab).sum(2) / (ab * ab).sum(1), 0, 1)
        d = np.linalg.norm(q[:, None] - (a + u[..., None] * ab), axis=2)
        i = d.argmin(1)
        along = (i + u[np.arange(len(sel)), i]) / (len(curve) - 1)
        width = (face['brow'][0] + (face['brow'][1] - face['brow'][0]) * along) / 2
        alpha = (1 - smoothstep(width - .003, width + .003, d.min(1)))[:, None]
        out[sel] = out[sel] * (1 - alpha) + brow_rgb * alpha
    result = flat.copy()
    result[keys] = np.clip(out, 0, 1)
    # Texture filtering reads the gutter round each chart: carry the head's texels into the unused texels up to four away.
    moved = keys
    done = coverage(g, size)
    for dy, dx in sorted(((dy, dx) for dy in range(-4, 5) for dx in range(-4, 5) if dy or dx), key=lambda d: d[0] ** 2 + d[1] ** 2):
        y, x0 = np.divmod(moved, size)
        ok = (y + dy >= 0) & (y + dy < size) & (x0 + dx >= 0) & (x0 + dx < size)
        src, dst = moved[ok], ((y + dy) * size + x0 + dx)[ok]
        free = ~done[dst]
        result[dst[free]] = result[src[free]]
        done[dst[free]] = True
    return result.reshape(pixels.shape), dict(inner=inner, skin=is_skin, tris=tris, changed=int((np.abs(out - rgb).max(1) > 2 / 255).sum()))


def face_map(g, pixels, path, keep_tris):
    """The face from the front, rays along +Y hitting only keep_tris and the rest of the body, gridded every 0.05 head lengths."""
    from mathutils import Vector
    from mathutils.bvhtree import BVHTree
    from mathutils.geometry import barycentric_transform
    H, T = g['H'], g['T']
    scale = np.linalg.norm(T - H)
    tris = np.where(keep_tris | (g['head'][g['tv']].mean(1) <= .5))[0]
    tree = BVHTree.FromPolygons([Vector(v) for v in g['co']], [tuple(map(int, g['tv'][t])) for t in tris], all_triangles=True)
    size, step = pixels.shape[0], .0025
    xs, zs = np.arange(-.6, .6, step), np.arange(.9, -.4, -step)
    image = np.full((len(zs), len(xs), 3), .8)
    one = (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    for i, z in enumerate(zs):
        for j, x in enumerate(xs):
            hit = tree.ray_cast(Vector((H[0] + x * scale, H[1] - 1, H[2] + z * scale)), Vector((0, 1, 0)), 2)
            if hit[0] is None:
                continue
            t = tris[hit[2]]
            w = barycentric_transform(hit[0], *(Vector(v) for v in g['co'][g['tv'][t]]), *one)
            u, v = np.dot(w, g['uv'][t])
            image[i, j] = pixels[min(size - 1, max(0, int((1 - v) * size))), min(size - 1, max(0, int(u * size)))]
    for j, x in enumerate(xs):
        if abs(x / .05 - round(x / .05)) < .01:
            image[:, j] *= .4 if abs(x) < 1e-6 else .7
    for i, z in enumerate(zs):
        if abs(z / .05 - round(z / .05)) < .01:
            image[i] *= .4 if abs(z) < 1e-6 else .7
    write(np.repeat(np.repeat(image, 2, 0), 2, 1), path, 'PNG')


def read(image):
    """A Blender image's pixels as float sRGB RGB, top row first."""
    w, h = image.size
    px = np.zeros(w * h * 4, np.float32)
    image.pixels.foreach_get(px)
    return px.reshape(h, w, 4)[::-1, :, :3].copy()


def write(pixels, path, kind='JPEG', quality=92):
    import bpy
    h, w = pixels.shape[:2]
    image = bpy.data.images.new('face_paint_out', w, h)
    rgba = np.concatenate([pixels[::-1], np.ones((h, w, 1))], 2).astype(np.float32)
    image.pixels.foreach_set(rgba.ravel())
    image.file_format = kind
    image.filepath_raw = str(path)
    image.save(filepath=str(path), quality=quality)
    bpy.data.images.remove(image)


def base_colour(body):
    """The image node feeding the body's Base Color."""
    tree = body.data.materials[0].node_tree
    bsdf = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node = bsdf.inputs['Base Color'].links[0].from_node
    while node.type != 'TEX_IMAGE':
        node = next(link.from_node for i in node.inputs for link in i.links)
    return node


def character(objects):
    rig = next(o for o in objects if o.type == 'ARMATURE')
    body = max((o for o in objects if o.type == 'MESH' and o.find_armature() == rig), key=lambda o: len(o.data.polygons))
    return body, rig


def repaint_blend(name):
    """In the open source file, swap the body's base colour image for the repainted map, packed as JPEG under the same name."""
    import bpy
    body, rig = character(bpy.data.objects)
    node = base_colour(body)
    old = node.image
    pixels, report = repaint(geometry(body, rig), read(old), FACES[name])
    path = Path(tempfile.mkdtemp()) / f'{name}-basecolor.jpg'
    write(pixels, path)
    image = bpy.data.images.load(str(path))
    image.pack()
    image.colorspace_settings.name = old.colorspace_settings.name
    image.name, old.name = old.name, old.name + '-tripo'
    node.image = image
    print('FACE_PAINT', name, report['changed'], 'texels', flush=True)


def main():
    import bpy
    import hashlib
    import re
    import glb_swap
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    opts = dict(a.lstrip('-').partition('=')[::2] for a in argv if a.startswith('--'))
    for name in [a for a in argv if not a.startswith('--')] or list(FACES):
        glb = ROOT / f'assets/models/{name}.glb'
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(glb))
        body, rig = character(bpy.data.objects)
        g = geometry(body, rig)
        image = base_colour(body).image
        title = image.name
        if 'source' in opts:
            image = bpy.data.images.load(str(Path(opts['source']).resolve()))
        before = read(image)
        after, report = repaint(g, before, FACES[name])
        print('FACE_PAINT', name, report['changed'], 'texels', flush=True)
        if 'map' in opts:
            out = ROOT / 'artifacts/face-paint'
            out.mkdir(parents=True, exist_ok=True)
            face = np.zeros(len(g['tv']), bool)
            face[report['tris']] = report['inner'][report['tris']]
            face_map(g, before, out / f'{name}-before.png', face)
            face_map(g, after, out / f'{name}-after.png', face)
            write(after, out / f'{name}-map.jpg')
            continue
        path = Path(tempfile.mkdtemp()) / 'map.jpg'
        write(after, path)
        data = path.read_bytes()
        raw = glb.read_bytes()
        renamed = re.sub(r'-[0-9a-f]{12}$', '-' + hashlib.sha256(data).hexdigest()[:12], title)
        glb.write_bytes(glb_swap.replace_image(raw, title, renamed, data))
        art = next((ROOT / 'assets/art').glob(f'**/{title}.jpg'))
        extracted = glb.with_name(f'{name}_{title}.jpg')
        if renamed == title:
            art.write_bytes(data)
            extracted.write_bytes(data)
        else:
            # Godot extracts the renamed map from the GLB on its next import.
            for old in (art, extracted):
                old.unlink()
                old.with_name(old.name + '.import').unlink(missing_ok=True)
            art.with_name(f'{renamed}.jpg').write_bytes(data)
        print('FACE_PAINT_WROTE', name, renamed, len(data), 'bytes', flush=True)


if __name__ == '__main__':
    main()
