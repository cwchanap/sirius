"""Blender regression check: --background --factory-startup --python-exit-code 1 --python tools/check_character_import.py"""
import bpy
import contextlib
import io
import json
from pathlib import Path
import struct
import sys
import tempfile
from mathutils import Matrix, Quaternion, Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
from import_character import arguments, digest, export_model


def check_glb(path, height, clips, triangles):
    raw = path.read_bytes()
    length = struct.unpack_from('<I', raw, 12)[0]
    g = json.loads(raw[20:20 + length])
    binary = raw[28 + length:]
    assert {a['name'] for a in g.get('animations', [])} == clips
    assert sum(g['accessors'][p['indices']]['count'] // 3 for m in g['meshes'] for p in m['primitives']) == triangles
    parents = {child: i for i, n in enumerate(g['nodes']) for child in n.get('children', [])}

    def accessor(index):
        a = g['accessors'][index]
        v = g['bufferViews'][a['bufferView']]
        fmt = '<' + {5121: 'B', 5123: 'H', 5126: 'f'}[a['componentType']] * {'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
        start = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        return [struct.unpack_from(fmt, binary, start + j * v.get('byteStride', struct.calcsize(fmt))) for j in range(a['count'])]

    def world(i):
        n = g['nodes'][i]
        if 'matrix' in n:
            transform = Matrix([n['matrix'][j:j + 4] for j in range(0, 16, 4)]).transposed()
        else:
            q = n.get('rotation', [0, 0, 0, 1])
            transform = Matrix.LocRotScale(Vector(n.get('translation', [0, 0, 0])), Quaternion([q[3], *q[:3]]), Vector(n.get('scale', [1, 1, 1])))
        return world(parents[i]) @ transform if i in parents else transform

    for animation in g.get('animations', []):
        rotations = [accessor(animation['samplers'][c['sampler']]['output']) for c in animation['channels'] if c['target']['path'] == 'rotation']
        assert any(any(sum(abs(x - y) for x, y in zip(values[0], frame)) > .01 for frame in values) for values in rotations), animation['name']
    points = []
    for i, node in enumerate(g['nodes']):
        if 'mesh' not in node:
            continue
        for primitive in g['meshes'][node['mesh']]['primitives']:
            attrs = primitive['attributes']
            positions = accessor(attrs['POSITION'])
            if 'skin' in node:
                skin = g['skins'][node['skin']]
                matrices = [world(joint) @ Matrix([ib[k:k + 4] for k in range(0, 16, 4)]).transposed() for joint, ib in zip(skin['joints'], accessor(skin['inverseBindMatrices']))]
                for p, joints, weights in zip(positions, accessor(attrs['JOINTS_0']), accessor(attrs['WEIGHTS_0'])):
                    points.append(sum((matrices[j] @ Vector(p) * w for j, w in zip(joints, weights)), Vector((0, 0, 0))))
            else:
                points.extend(world(i) @ Vector(p) for p in positions)
    assert abs(min(p.y for p in points)) < 1e-4, ('floor', min(p.y for p in points))
    assert abs(max(p.y for p in points) - height) < 1e-4, ('height', max(p.y for p in points), height, g['nodes'])
    for axis in (0, 2):
        assert abs(min(p[axis] for p in points) + max(p[axis] for p in points)) < 1e-4


def run():
    with tempfile.TemporaryDirectory(prefix='character-import-check-') as folder:
        tmp = Path(folder)
        bpy.ops.wm.read_factory_settings(use_empty=True)
        rig = bpy.data.objects.new('UnfamiliarRig', bpy.data.armatures.new('RobotRig'))
        bpy.context.scene.collection.objects.link(rig)
        bpy.context.view_layer.objects.active = rig
        rig.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT')
        bone = rig.data.edit_bones.new('Robot:Head')
        bone.head, bone.tail = (0, 0, 0), (0, 0, 1)
        bpy.ops.object.mode_set(mode='OBJECT')
        rig.location, rig.scale, rig.rotation_euler.z = (4, -3, 2), (1.2, 1.4, .8), .42
        mat = bpy.data.materials.new('Test material')
        image = bpy.data.images.new('Packed colour', width=4, height=4)
        image.generated_color = (.2, .5, .3, 1)
        image.filepath_raw = str(tmp / 'colour.png')
        image.file_format = 'PNG'
        image.save()
        image.source = 'FILE'
        image.reload()
        image.pack()
        tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
        tex.image = image
        shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        mat.node_tree.links.new(tex.outputs['Color'], shader.inputs['Base Color'])
        for name, location, scale in [('Shell', (0, 0, 1), (.5, .4, 1)), ('Cap', (0, 0, 2.25), (.3, .3, .25))]:
            bpy.ops.mesh.primitive_cube_add()
            body = bpy.context.object
            body.name, body.parent, body.location, body.scale = name, rig, location, scale
            body.data.materials.append(mat)
            body.vertex_groups.new(name='Robot:Head').add(list(range(8)), 1, 'REPLACE')
            body.modifiers.new('Rig', 'ARMATURE').object = rig
        rig.animation_data_create()
        for name, amplitude in [('Rest', .1), ('Look', .2)]:
            action = bpy.data.actions.new(name)
            rig.animation_data.action = action
            head = rig.pose.bones['Robot:Head']
            head.rotation_mode = 'XYZ'
            for frame, angle in [(0, 0), (12, amplitude), (24, 0)]:
                head.rotation_euler.y = angle
                head.keyframe_insert('rotation_euler', frame=frame)
            rig.animation_data.action = None
            track = rig.animation_data.nla_tracks.new()
            track.name = name
            track.strips.new(name, 0, action)
            track.mute = True
        bpy.context.scene.frame_set(0)
        bpy.ops.object.select_all(action='SELECT')
        for suffix, options in [('glb', {'export_format': 'GLB'}), ('gltf', {'export_format': 'GLTF_SEPARATE'})]:
            bpy.ops.export_scene.gltf(filepath=str(tmp / ('input.' + suffix)), use_selection=True, export_animation_mode='ACTIONS', export_anim_single_armature=False, **options)
        bpy.ops.export_scene.fbx(filepath=str(tmp / 'input.fbx'), use_selection=True, bake_anim=False, add_leaf_bones=False, path_mode='COPY', embed_textures=True)
        bpy.ops.mesh.primitive_cube_add(location=(100, 100, 100))
        bpy.context.object.name = 'Unrelated scenery'
        bpy.ops.wm.save_as_mainfile(filepath=str(tmp / 'input.blend'))
        for suffix in ('blend', 'glb', 'gltf', 'fbx'):
            source = tmp / ('input.' + suffix)
            before = digest(source)
            extra = ['--idle', 'head-turn', '--strip-bone-prefix', 'Robot:'] if suffix == 'fbx' else ['--clip', 'Rest=idle']
            if suffix == 'blend':
                extra += ['--strip-bone-prefix', 'Robot:']
            argv = [str(source), '--name', 'test-' + suffix, '--height', '1.7', '--output-root', str(tmp / 'out'), *extra]
            result = export_model(arguments(argv))
            assert digest(source) == before
            assert result['bones'] == 1 and len(result['meshes']) == 2
            assert len(result['textures']) == 1
            assert digest(Path(result['textures'][0]['file'])) == result['textures'][0]['sha256']
            path = Path(result['outputs']['model'])
            check_glb(path, 1.7, {'idle'} if suffix == 'fbx' else {'idle', 'Look'}, 24)
            old = digest(path)
            try:
                export_model(arguments(argv))
                raise AssertionError('overwrite was accepted without --force')
            except ValueError as error:
                assert 'already exist' in str(error)
            assert digest(path) == old
            print('CHECK_IMPORT', suffix, 'selection, transforms, grounding, textures, animations, source and overwrite protection')
        bad = [str(tmp / 'input.blend'), '--name', 'bad', '--height', '1', '--output-root', str(tmp / 'out')]
        for extra, message in [(['--mesh', 'missing'], 'Unknown meshes'), (['--clip', 'missing=idle'], 'mapping'), (['--idle', 'head-turn'], 'supplied animations')]:
            try:
                export_model(arguments(bad + extra))
                raise AssertionError('invalid input was accepted')
            except ValueError as error:
                assert message in str(error), str(error)
        with contextlib.redirect_stderr(io.StringIO()):
            for extra in [['--height', 'nan'], ['--name', '../escape'], ['--max-triangles', '-1']]:
                try:
                    arguments(bad + extra)
                    raise AssertionError('invalid arguments were accepted')
                except SystemExit as error:
                    assert error.code == 2
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.mesh.primitive_cube_add(location=(2, 3, 4))
        bpy.ops.wm.save_as_mainfile(filepath=str(tmp / 'static.blend'))
        result = export_model(arguments([str(tmp / 'static.blend'), '--name', 'static', '--height', '2', '--output-root', str(tmp / 'out')]))
        check_glb(Path(result['outputs']['model']), 2, set(), 12)
        assert result['bones'] == 0 and not result['textures']
        print('CHARACTER_IMPORT_CHECK_PASS: four formats, multi-mesh animated rig, static mesh and invalid-input guards')


if __name__ == '__main__':
    run()
