"""Swap images or animations into a committed GLB without re-exporting it, so a session without the Tripo sources can update a model's
maps (face_paint.py) or clips (cast_clips.py --glb) and leave the rest of the file as the importer wrote it.

python3 tools/glb_swap.py MODEL.glb DONOR.glb CLIP...   # MODEL's clips of these names, or new ones, from DONOR, matched by node name
A donor is the model round-tripped through Blender's glTF importer and exporter with the clips re-keyed: the round trip keeps every
node's rest transform within 1e-5, so its clips' local transforms hold for the committed nodes.
"""
import json
import struct
import sys
from pathlib import Path


def load(raw):
    assert raw[:4] == b'glTF'
    length = struct.unpack_from('<I', raw, 12)[0]
    gltf = json.loads(raw[20:20 + length])
    offset = 20 + length
    size = struct.unpack_from('<I', raw, offset)[0]
    return gltf, raw[offset + 8:offset + 8 + size]


def view(gltf, binary, index):
    v = gltf['bufferViews'][index]
    return binary[v.get('byteOffset', 0):v.get('byteOffset', 0) + v['byteLength']]


def save(gltf, views):
    """Pack the buffer from views (bufferView index -> bytes, every view used), dropping accessors and views nothing refers to."""
    assert not any('sparse' in a for a in gltf['accessors'])
    used = set()
    for mesh in gltf.get('meshes', []):
        for p in mesh['primitives']:
            used |= set(p['attributes'].values()) | ({p['indices']} if 'indices' in p else set())
            for target in p.get('targets', []):
                used |= set(target.values())
    for skin in gltf.get('skins', []):
        used |= {skin['inverseBindMatrices']} if 'inverseBindMatrices' in skin else set()
    for anim in gltf.get('animations', []):
        for s in anim['samplers']:
            used |= {s['input'], s['output']}
    accessors = sorted(used)
    amap = {old: new for new, old in enumerate(accessors)}
    gltf['accessors'] = [gltf['accessors'][i] for i in accessors]
    for mesh in gltf.get('meshes', []):
        for p in mesh['primitives']:
            p['attributes'] = {k: amap[v] for k, v in p['attributes'].items()}
            if 'indices' in p:
                p['indices'] = amap[p['indices']]
            if 'targets' in p:
                p['targets'] = [{k: amap[v] for k, v in t.items()} for t in p['targets']]
    for skin in gltf.get('skins', []):
        if 'inverseBindMatrices' in skin:
            skin['inverseBindMatrices'] = amap[skin['inverseBindMatrices']]
    for anim in gltf.get('animations', []):
        for s in anim['samplers']:
            s['input'], s['output'] = amap[s['input']], amap[s['output']]
    kept = sorted({a['bufferView'] for a in gltf['accessors'] if 'bufferView' in a} | {i['bufferView'] for i in gltf.get('images', []) if 'bufferView' in i})
    vmap = {old: new for new, old in enumerate(kept)}
    binary = bytearray()
    out = []
    for old in kept:
        v = dict(gltf['bufferViews'][old])
        binary += b'\0' * (-len(binary) % 4)
        v['byteOffset'], v['byteLength'] = len(binary), len(views[old])
        binary += views[old]
        out.append(v)
    binary += b'\0' * (-len(binary) % 4)
    gltf['bufferViews'] = out
    for a in gltf['accessors']:
        if 'bufferView' in a:
            a['bufferView'] = vmap[a['bufferView']]
    for i in gltf.get('images', []):
        if 'bufferView' in i:
            i['bufferView'] = vmap[i['bufferView']]
    gltf['buffers'] = [{'byteLength': len(binary)}]
    text = json.dumps(gltf, separators=(',', ':')).encode()
    text += b' ' * (-len(text) % 4)
    total = 12 + 8 + len(text) + 8 + len(binary)
    return struct.pack('<4sII', b'glTF', 2, total) + struct.pack('<I4s', len(text), b'JSON') + text + struct.pack('<I4s', len(binary), b'BIN\0') + bytes(binary)


def replace_image(raw, name, new_name, data):
    gltf, binary = load(raw)
    views = {i: view(gltf, binary, i) for i in range(len(gltf['bufferViews']))}
    image = next(i for i in gltf['images'] if i['name'] == name)
    views[image['bufferView']] = data
    image['name'] = new_name
    return save(gltf, views)


def swap_animations(raw, donor_raw, names):
    gltf, binary = load(raw)
    donor, donor_binary = load(donor_raw)
    views = {i: view(gltf, binary, i) for i in range(len(gltf['bufferViews']))}
    nodes = {n['name']: i for i, n in enumerate(gltf['nodes'])}
    for name in names:
        source = next(a for a in donor['animations'] if a['name'] == name)
        anim = {'name': name, 'channels': [], 'samplers': []}
        for channel in source['channels']:
            target = donor['nodes'][channel['target']['node']]['name']
            if target not in nodes:
                continue
            sampler = dict(source['samplers'][channel['sampler']])
            for key in ('input', 'output'):
                accessor = dict(donor['accessors'][sampler[key]])
                views[len(gltf['bufferViews'])] = view(donor, donor_binary, accessor['bufferView'])
                gltf['bufferViews'].append({'buffer': 0, 'byteLength': 0})
                accessor['bufferView'] = len(gltf['bufferViews']) - 1
                gltf['accessors'].append(accessor)
                sampler[key] = len(gltf['accessors']) - 1
            anim['samplers'].append(sampler)
            anim['channels'].append({'sampler': len(anim['samplers']) - 1, 'target': {'node': nodes[target], 'path': channel['target']['path']}})
        old = [i for i, a in enumerate(gltf['animations']) if a['name'] == name]
        if old:
            gltf['animations'][old[0]] = anim
        else:
            gltf['animations'].append(anim)
    return save(gltf, views)


if __name__ == '__main__':
    model, donor, *clips = sys.argv[1:]
    Path(model).write_bytes(swap_animations(Path(model).read_bytes(), Path(donor).read_bytes(), clips))
    print('GLB_SWAP', model, clips)
