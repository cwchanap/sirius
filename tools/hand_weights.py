"""Unbind body parts that Tripo's auto-skinning tied to the hands.

Mio's Tripo rig gives about 2,500 vertices of each thigh and coat panel, 8 to 44 cm from any hand bone, a third of their weight on the
last joint of the pinky, which hangs beside them in the rest pose: wherever her hands go, her coat and thighs follow, as dark shards at
her hips. A hand vertex is never more than 5 cm from a hand bone, so beyond RADIUS the hand bones' weights are dropped and the rest
renormalized; a vertex left with almost nothing takes the weights of the nearest vertex the hands never touched.

python3 tools/hand_weights.py MODEL.glb...   # patch committed GLBs in place: their skin weights only, every other byte as it was
import_tripo_mio.py calls unbind_blender() on her body before export.
"""
import struct
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
RADIUS = .07


def unbind(points, weights, names, segments, scale, radius=RADIUS):
    """points (N, 3) and weights (N, J) by joint names; segments: side -> [(a, b)] of that hand's bones, in the points' space, scale metres
    per unit. Returns the new weights and how many vertices changed."""
    weights = weights.copy()
    changed = np.zeros(len(points), bool)
    for side, segs in segments.items():
        hand = [i for i, n in enumerate(names) if n.startswith(side + 'Hand')]
        near = np.full(len(points), np.inf)
        for a, b in segs:
            ab = b - a
            u = np.clip((points - a) @ ab / max(ab @ ab, 1e-12), 0, 1)
            near = np.minimum(near, np.linalg.norm(points - (a + u[:, None] * ab), axis=1) * scale)
        far = (near > radius) & (weights[:, hand].sum(1) > 0)
        before = weights[far].sum(1)
        weights[np.ix_(far, hand)] = 0
        changed |= far
        lost = np.where(far)[0][weights[far].sum(1) < .2 * before]
        clean = np.where(weights[:, hand].sum(1) == 0)[0]
        for i in lost:
            weights[i] = weights[clean[np.argmin(np.linalg.norm(points[clean] - points[i], axis=1))]]
    weights[changed] /= weights[changed].sum(1, keepdims=True)
    return weights, int(changed.sum())


def trs(node):
    t = np.array(node.get('translation', [0, 0, 0]), float)
    x, y, z, w = node.get('rotation', [0, 0, 0, 1])
    r = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    m = np.eye(4)
    m[:3, :3] = r * np.array(node.get('scale', [1, 1, 1]), float)
    m[:3, 3] = t
    return np.array(node['matrix'], float).reshape(4, 4).T if 'matrix' in node else m


def patch_glb(path, write=True):
    """Unbind the skinned body of a committed GLB in place: only its WEIGHTS_0 and JOINTS_0 bytes change. Returns how many vertices were
    bound to a hand from too far; with write=False it only counts them."""
    import glb_swap
    raw = Path(path).read_bytes()
    gltf, binary = glb_swap.load(raw)
    binary = bytearray(binary)
    parent = {c: i for i, n in enumerate(gltf['nodes']) for c in n.get('children', [])}

    def world(i):
        m = trs(gltf['nodes'][i])
        while i in parent:
            i = parent[i]
            m = trs(gltf['nodes'][i]) @ m
        return m

    def array(index, dtype, width):
        a = gltf['accessors'][index]
        v = gltf['bufferViews'][a['bufferView']]
        assert v.get('byteStride') in (None, np.dtype(dtype).itemsize * width) and 'sparse' not in a
        start = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        return start, np.frombuffer(bytes(binary[start:start + a['count'] * width * np.dtype(dtype).itemsize]), dtype).reshape(a['count'], width)
    skin = gltf['skins'][0]
    joints = skin['joints']
    names = [gltf['nodes'][j]['name'] for j in joints]
    _, ibm = array(skin['inverseBindMatrices'], '<f4', 16)
    rest = {j: np.linalg.inv(m.reshape(4, 4).T)[:3, 3] for j, m in zip(joints, ibm)}
    body = max((i for i, n in enumerate(gltf['nodes']) if 'mesh' in n and n.get('skin') == 0),
               key=lambda i: gltf['accessors'][gltf['meshes'][gltf['nodes'][i]['mesh']]['primitives'][0]['attributes']['POSITION']]['count'])
    # glTF ignores a skinned mesh's own node: its bind is a joint's world matrix times that joint's inverse bind.
    bind = world(joints[0]) @ ibm[0].reshape(4, 4).T
    scale = float(np.cbrt(abs(np.linalg.det(bind[:3, :3]))))
    segments = {}
    for side in ('Right', 'Left'):
        segments[side] = [(rest[j], rest[c] if c in rest else rest[j]) for j in joints if gltf['nodes'][j]['name'].startswith(side + 'Hand')
                          for c in gltf['nodes'][j].get('children', [j])]
    total = 0
    for prim in gltf['meshes'][gltf['nodes'][body]['mesh']]['primitives']:
        at = prim['attributes']
        assert gltf['accessors'][at['JOINTS_0']]['componentType'] == 5121 and gltf['accessors'][at['WEIGHTS_0']]['componentType'] == 5126
        assert 'JOINTS_1' not in at
        _, points = array(at['POSITION'], '<f4', 3)
        js, jnt = array(at['JOINTS_0'], 'u1', 4)
        ws, wgt = array(at['WEIGHTS_0'], '<f4', 4)
        dense = np.zeros((len(points), len(names)))
        np.add.at(dense, (np.repeat(np.arange(len(points)), 4), jnt.ravel().astype(int)), wgt.ravel())
        dense, n = unbind(points.astype(float), dense, names, segments, scale)
        total += n
        top = np.argsort(-dense, axis=1)[:, :4]
        w = np.take_along_axis(dense, top, 1)
        w /= w.sum(1, keepdims=True)
        top[w == 0] = 0
        binary[js:js + jnt.nbytes] = top.astype('u1').tobytes()
        binary[ws:ws + wgt.nbytes] = w.astype('<f4').tobytes()
    if write:
        head = raw[:len(raw) - len(binary) - 8]
        Path(path).write_bytes(head + struct.pack('<I4s', len(binary), b'BIN\0') + bytes(binary))
    return total


def unbind_blender(body, rig):
    """The same on a Blender body before export, in world space, which is in metres once the importer has scaled it."""
    groups = [g.name for g in body.vertex_groups]
    me = body.data
    points = np.array([body.matrix_world @ v.co for v in me.vertices])
    dense = np.zeros((len(points), len(groups)))
    for v in me.vertices:
        for g in v.groups:
            dense[v.index, g.group] = g.weight
    world = rig.matrix_world
    segments = {side: [(np.array(world @ b.head_local), np.array(world @ b.tail_local)) for b in rig.data.bones if b.name.startswith(side + 'Hand')]
                for side in ('Right', 'Left')}
    dense, n = unbind(points, dense, groups, segments, 1)
    for i in np.where((dense > 0).any(1))[0]:
        for gi, w in enumerate(dense[i]):
            if w > 0:
                body.vertex_groups[gi].add([int(i)], float(w), 'REPLACE')
            elif any(g.group == gi for g in me.vertices[i].groups):
                body.vertex_groups[gi].remove([int(i)])
    return n


if __name__ == '__main__':
    for model in sys.argv[1:]:
        print('HAND_WEIGHTS', model, patch_glb(model), 'vertices unbound from the hands')
