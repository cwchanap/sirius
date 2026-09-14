"""Check the actual game GLB after exporting Mio from Blender.

Run: python3 tools/check_mio_export.py
"""
import hashlib
import json
import math
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[1]


def main():
    raw = (ROOT / 'assets/models/mio.glb').read_bytes()
    assert struct.unpack_from('<4sII', raw) == (b'glTF', 2, len(raw))
    chunks = {}
    offset = 12
    while offset < len(raw):
        length, kind = struct.unpack_from('<I4s', raw, offset)
        chunks[kind] = raw[offset + 8:offset + 8 + length]
        assert len(chunks[kind]) == length
        offset += 8 + length
    assert offset == len(raw)
    gltf = json.loads(chunks[b'JSON'])
    binary = chunks[b'BIN\x00']

    def accessor(index):
        a = gltf['accessors'][index]
        view = gltf['bufferViews'][a['bufferView']]
        assert view.get('buffer', 0) == 0 and 'sparse' not in a
        count = {'SCALAR': 1, 'VEC3': 3, 'VEC4': 4}[a['type']]
        fmt = '<' + {5121: 'B', 5123: 'H', 5125: 'I', 5126: 'f'}[a['componentType']] * count
        size = struct.calcsize(fmt)
        stride = view.get('byteStride', size)
        start = view.get('byteOffset', 0) + a.get('byteOffset', 0)
        assert start + (a['count'] - 1) * stride + size <= len(binary)
        return [struct.unpack_from(fmt, binary, start + i * stride) for i in range(a['count'])]

    assert len(gltf['skins']) == 1
    bones = len(gltf['skins'][0]['joints'])
    assert bones == 41
    clips = [a['name'] for a in gltf['animations']]
    expected = {'idle', 'walk', 'run', 'attack1', 'attack2', 'attack3', 'guard',
                'dodge', 'jump', 'skill', 'stagger', 'death'}
    assert len(clips) == 12 and set(clips) == expected, clips
    assert all(n.get('skin') == 0 for n in gltf['nodes'] if 'mesh' in n)
    vertices = triangles = 0
    maximum_error = 0.0
    for mesh in gltf['meshes']:
        for primitive in mesh['primitives']:
            attrs = primitive['attributes']
            assert 'JOINTS_1' not in attrs and 'WEIGHTS_1' not in attrs
            positions = accessor(attrs['POSITION'])
            joints = accessor(attrs['JOINTS_0'])
            weights = accessor(attrs['WEIGHTS_0'])
            assert len(positions) == len(joints) == len(weights)
            assert all(math.isfinite(x) for p in positions for x in p)
            for js, ws in zip(joints, weights):
                assert all(0 <= j < bones for j in js)
                assert all(math.isfinite(w) and 0 <= w <= 1 for w in ws)
                maximum_error = max(maximum_error, abs(sum(ws) - 1))
            vertices += len(positions)
            assert primitive.get('mode', 4) == 4
            triangles += gltf['accessors'][primitive['indices']]['count'] // 3
    assert maximum_error < 1e-5 and triangles < 160000

    sources = [ROOT / 'assets/art' / name for name in
               ['mio-tripo-basecolor.jpg', 'mio-tripo-normal.png']]
    expected_images = {hashlib.sha256(p.read_bytes()).hexdigest(): p for p in sources}
    images = []
    for img in gltf['images']:
        view = gltf['bufferViews'][img['bufferView']]
        start = view.get('byteOffset', 0)
        data = binary[start:start + view['byteLength']]
        digest = hashlib.sha256(data).hexdigest()
        assert digest in expected_images, img
        images.append({'name': img.get('name'), 'bytes': len(data), 'sha256': digest,
                       'source': str(expected_images[digest].relative_to(ROOT))})
    assert {i['sha256'] for i in images} == set(expected_images)
    report = dict(sha256=hashlib.sha256(raw).hexdigest(), bytes=len(raw), bones=bones,
                  meshes=len(gltf['meshes']), export_vertices=vertices, triangles=triangles,
                  clips=clips, maximum_weight_sum_error=maximum_error, images=images)
    (ROOT / 'artifacts/mio-export-check.json').write_text(json.dumps(report, indent=2) + '\n')
    print(f'MIO_EXPORT_OK: {bones} bones, {len(clips)} clips, {triangles} triangles; textures and skinning verified.')


if __name__ == '__main__':
    main()
