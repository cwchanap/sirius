"""Import a rigged character or static model through a separate background Blender.

python3 tools/import_character.py model.glb --name hana --height 1.65
Use --help for selection, animation and output options. Mio's combat retargeting
remains in import_tripo_mio.py.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import os

ROOT = Path(__file__).resolve().parents[1]


def arguments(argv):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('source', type=Path, help='Saved .blend, .glb, .gltf or .fbx input (never overwritten)')
    parser.add_argument('--name', required=True, help='Asset filename, e.g. hana or koma7')
    parser.add_argument('--height', required=True, type=float, help='Grounded model height in metres')
    parser.add_argument('--armature', help='Armature to use when the source contains several')
    parser.add_argument('--mesh', action='append', default=[], help='Mesh to include; repeat for multiple meshes')
    parser.add_argument('--max-triangles', type=int, default=0, help='Optional total reduction budget; 0 retains geometry')
    parser.add_argument('--idle', choices=['none', 'breathing', 'head-turn'], default='none', help='Author an idle only when the selected model has no animations')
    parser.add_argument('--head-bone', help='Override automatic Head bone lookup for authored idle')
    parser.add_argument('--spine-bones', nargs=2, help='Override the two breathing-idle spine bones')
    parser.add_argument('--strip-bone-prefix', default='', help='Optional namespace removal, e.g. mixamorig:')
    parser.add_argument('--clip', action='append', default=[], metavar='OLD=NEW', help='Rename a supplied action, e.g. Rest=idle')
    parser.add_argument('--output-root', type=Path, default=ROOT, help='Project root; use an artifact directory for a trial import')
    parser.add_argument('--force', action='store_true', help='Replace existing generated outputs, never the input')
    parser.add_argument('--blender', default=os.environ.get('BLENDER_BIN', '/Applications/Blender.app/Contents/MacOS/Blender'), help='Blender executable; also configurable with BLENDER_BIN')
    args = parser.parse_args(argv)
    args.source = args.source.expanduser().resolve()
    args.output_root = args.output_root.expanduser().resolve()
    if not args.source.is_file() or args.source.suffix.lower() not in {'.blend', '.glb', '.gltf', '.fbx'}:
        parser.error('source must be an existing .blend, .glb, .gltf or .fbx file')
    if not re.fullmatch(r'[a-z][a-z0-9_-]*', args.name):
        parser.error('--name must start with a lowercase letter and contain only letters, digits, _ or -')
    if not math.isfinite(args.height) or args.height <= 0 or args.max_triangles < 0:
        parser.error('height must be finite and positive; triangle budget must be nonnegative')
    return args


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def export_model(args):
    import bpy
    from mathutils import Matrix
    if not bpy.app.background:
        raise ValueError('Run in background Blender to preserve the open working scene.')
    paths = {'model': args.output_root / f'assets/models/{args.name}.glb',
             'blend': args.output_root / f'assets/source/{args.name}-game.blend',
             'report': args.output_root / f'artifacts/model-imports/{args.name}/build.json'}
    # A rig's texture copies are committed; a static model's stay beside its game .blend, since Godot reads the GLB's own.
    texture_dirs = [args.output_root / f'assets/art/{args.name}-import', args.output_root / f'assets/source/{args.name}-textures']
    if args.source in [p.resolve() for p in paths.values()] or any(args.source.is_relative_to(d.resolve()) for d in texture_dirs):
        raise ValueError('Source overlaps an output path; choose a different name/output root.')
    existing = [p for p in [*paths.values(), *texture_dirs] if p.exists()]
    if existing and not args.force:
        raise ValueError('Outputs already exist; use --force to rebuild: ' + ', '.join(map(str, existing)))
    source_hash = digest(args.source)
    if args.source.suffix.lower() == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(args.source), use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        if args.source.suffix.lower() == '.fbx':
            bpy.ops.import_scene.fbx(filepath=str(args.source))
        else:
            bpy.ops.import_scene.gltf(filepath=str(args.source))
    source_scene = bpy.context.scene
    rigs = [o for o in source_scene.objects if o.type == 'ARMATURE']
    if args.armature:
        rig = source_scene.objects.get(args.armature)
        if rig not in rigs:
            raise ValueError('Unknown armature: ' + args.armature)
    elif len(rigs) > 1:
        raise ValueError('Choose --armature: ' + ', '.join(o.name for o in rigs))
    else:
        rig = rigs[0] if rigs else None
    texture_dir = texture_dirs[0 if rig else 1]

    def ancestors(obj):
        while obj.parent:
            obj = obj.parent
            yield obj

    meshes = [o for o in source_scene.objects if o.type == 'MESH']
    if args.mesh:
        missing = set(args.mesh) - {o.name for o in meshes}
        if missing:
            raise ValueError('Unknown meshes: ' + ', '.join(sorted(missing)))
        meshes = [o for o in meshes if o.name in args.mesh]
    elif rig:
        meshes = [o for o in meshes if rig in ancestors(o) or any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers)]
    elif args.source.suffix.lower() == '.blend' and len(meshes) > 1:
        raise ValueError('Unrigged .blend has multiple meshes; select with repeated --mesh arguments.')
    if not meshes:
        raise ValueError('No model meshes found.')
    keep = set(meshes + ([rig] if rig else []))
    for obj in list(keep):
        keep.update(ancestors(obj))
    if any(o.type == 'ARMATURE' and o != rig for o in keep):
        raise ValueError('Selected meshes span multiple rigs.')
    for obj in meshes:
        if any(m.type == 'ARMATURE' and m.object != rig for m in obj.modifiers):
            raise ValueError('Mesh is bound to a different rig: ' + obj.name)
    scene = bpy.data.scenes.new(args.name + '_Game')
    scene.render.fps, scene.render.fps_base = source_scene.render.fps, source_scene.render.fps_base
    scene.frame_start, scene.frame_end = source_scene.frame_start, source_scene.frame_end
    for obj in keep:
        scene.collection.objects.link(obj)
        obj.hide_set(False)
        obj.hide_viewport = False
    bpy.context.window.scene = scene
    for other in list(bpy.data.scenes):
        if other != scene:
            bpy.data.scenes.remove(other)
    for obj in list(bpy.data.objects):
        if obj not in keep:
            bpy.data.objects.remove(obj, do_unlink=True)
    if rig and args.strip_bone_prefix:
        names = [b.name.removeprefix(args.strip_bone_prefix) for b in rig.data.bones]
        if len(names) != len(set(names)) or any(not n for n in names):
            raise ValueError('Removing the bone prefix would create empty or duplicate names.')
        for bone, name in zip(rig.data.bones, names):
            bone.name = name
    animated = list(keep) + [o.data.shape_keys for o in meshes if o.data.shape_keys]
    actions = set()
    for obj in animated:
        if obj.animation_data:
            if obj.animation_data.action:
                actions.add(obj.animation_data.action)
            actions.update(s.action for t in obj.animation_data.nla_tracks for s in t.strips if s.action)
    for mapping in args.clip:
        old, sep, new = mapping.partition('=')
        match = next((a for a in actions if a.name == old), None)
        if not sep or not new or match is None or bpy.data.actions.get(new) not in (None, match):
            raise ValueError('Invalid or conflicting --clip mapping: ' + mapping)
        match.name = new
    if args.idle != 'none':
        if actions or rig is None:
            raise ValueError('--idle requires a rig with no supplied animations; use --idle none to preserve clips.')

        def bone_name(explicit, candidates):
            matches = [b.name for b in rig.pose.bones if b.name == explicit] if explicit else [b.name for c in candidates for b in rig.pose.bones if b.name.rsplit(':', 1)[-1].lower() == c.lower()]
            if not matches:
                raise ValueError('Idle bone not found; provide --head-bone/--spine-bones or use --idle none.')
            return matches[0]

        head = bone_name(args.head_bone, ['Head'])
        channels = [(head, 1, .07)]
        frames = 144
        if args.idle == 'breathing':
            spine = args.spine_bones or [None, None]
            channels = [(bone_name(spine[0], ['Spine01', 'Spine']), 0, .008),
                        (bone_name(spine[1], ['Spine02', 'Spine1']), 0, .012), (head, 0, -.006)]
            frames = 96
        rig.animation_data_create()
        action = bpy.data.actions.new('idle')
        rig.animation_data.action = action
        scene.render.fps, scene.render.fps_base = 24, 1
        for frame in range(frames + 1):
            for name, axis, amplitude in channels:
                bone = rig.pose.bones[name]
                bone.rotation_mode = 'XYZ'
                rotation = [0.0, 0.0, 0.0]
                rotation[axis] = amplitude * math.sin(frame / frames * math.tau)
                bone.rotation_euler = rotation
                bone.keyframe_insert('rotation_euler', frame=frame)
        scene.frame_start, scene.frame_end = 0, frames
        actions.add(action)
        scene.frame_set(0)
    if rig:
        rig.data.pose_position = 'REST'
    source_triangles = 0
    for obj in meshes:
        obj.data.calc_loop_triangles()
        source_triangles += len(obj.data.loop_triangles)
    if args.max_triangles and source_triangles > args.max_triangles:
        ratio = args.max_triangles / source_triangles
        for obj in meshes:
            if obj.data.shape_keys:
                raise ValueError('Cannot reduce a mesh with shape keys; omit --max-triangles.')
            if obj.data.users > 1:
                obj.data = obj.data.copy()
            bpy.ops.object.select_all(action='DESELECT')
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            mod = obj.modifiers.new('Game mesh budget', 'DECIMATE')
            mod.ratio = ratio
            mod.use_collapse_triangulate = True
            bpy.ops.object.modifier_move_to_index(modifier=mod.name, index=0)
            bpy.ops.object.modifier_apply(modifier=mod.name)
            for polygon in obj.data.polygons:
                polygon.use_smooth = True
            obj.data.normals_split_custom_set([(0, 0, 0)] * len(obj.data.loops))
    bpy.context.view_layer.update()
    lower, upper = [float('inf')] * 3, [float('-inf')] * 3
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in meshes:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        for vertex in mesh.vertices:
            point = evaluated.matrix_world @ vertex.co
            if not all(math.isfinite(v) for v in point):
                raise ValueError('Non-finite geometry in ' + obj.name)
            for axis in range(3):
                lower[axis], upper[axis] = min(lower[axis], point[axis]), max(upper[axis], point[axis])
        evaluated.to_mesh_clear()
        if rig and any(m.type == 'ARMATURE' for m in obj.modifiers):
            groups = {g.index for g in obj.vertex_groups if g.name in rig.data.bones}
            if any(sum(g.weight for g in v.groups if g.group in groups) <= 0 for v in obj.data.vertices):
                raise ValueError('Unweighted skinned vertices in ' + obj.name)
    if not math.isfinite(upper[2] - lower[2]) or upper[2] - lower[2] < 1e-6:
        raise ValueError('Model has no measurable vertical height.')
    scale = args.height / (upper[2] - lower[2])
    root = bpy.data.objects.new(args.name + '_Model', None)
    scene.collection.objects.link(root)
    for obj in keep:
        if obj.parent is None:
            obj.parent = root
            obj.matrix_parent_inverse = Matrix.Identity(4)
    root.scale = (scale,) * 3
    root.location = (-(lower[0] + upper[0]) * .5 * scale, -(lower[1] + upper[1]) * .5 * scale, -lower[2] * scale)
    if rig:
        rig.data.pose_position = 'POSE'
    # Stage every derivative before replacing a working GLB. Packed inputs stay untouched.
    paths['report'].parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='stage-', dir=paths['report'].parent) as tmp:
        stage = Path(tmp)
        textures = []
        images = {n.image for o in meshes for m in o.data.materials if m and m.node_tree for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image}
        for image in sorted(images, key=lambda i: i.name):
            if image.source not in {'FILE', 'GENERATED'}:
                raise ValueError('Unsupported tiled/movie image: ' + image.name)
            if not image.packed_file:
                image.pack()
            if not image.packed_file:
                raise ValueError('Missing texture: ' + image.name)
            data = bytes(image.packed_file.data)
            sha = hashlib.sha256(data).hexdigest()
            extension = '.png' if data.startswith(b'\x89PNG') else '.jpg' if data.startswith(b'\xff\xd8') else Path(image.filepath).suffix
            if not extension:
                raise ValueError('Unknown texture format: ' + image.name)
            filename = re.sub(r'[^a-zA-Z0-9_-]', '-', image.name) + '-' + sha[:12] + extension
            (stage / filename).write_bytes(data)
            image.filepath_raw = str(texture_dir / filename)
            textures.append({'name': image.name, 'size': list(image.size), 'file': str(texture_dir / filename), 'sha256': sha})
        bpy.ops.object.select_all(action='SELECT')
        bpy.context.view_layer.objects.active = rig or meshes[0]
        bpy.ops.export_scene.gltf(filepath=str(stage / 'model.glb'), export_format='GLB',
            use_selection=True, use_active_scene=True, export_animations=True,
            export_animation_mode='ACTIONS', export_anim_single_armature=False,
            export_apply=False, export_frame_range=False)
        raw = (stage / 'model.glb').read_bytes()
        if struct.unpack_from('<4sII', raw) != (b'glTF', 2, len(raw)):
            raise ValueError('Invalid exported GLB.')
        length, kind = struct.unpack_from('<I4s', raw, 12)
        gltf = json.loads(raw[20:20 + length])
        clips = [a['name'] for a in gltf.get('animations', [])]
        if {a.name for a in actions} - set(clips):
            raise ValueError(f'Export lost animation clips: expected {[a.name for a in actions]}, got {clips}')
        triangles = sum(gltf['accessors'][p['indices']]['count'] // 3 for m in gltf['meshes'] for p in m['primitives'])
        if args.max_triangles and triangles > args.max_triangles:
            raise ValueError(f'Export exceeds triangle budget: {triangles} > {args.max_triangles}')
        report = {'source': str(args.source), 'source_sha256': source_hash, 'asset': args.name,
                  'height_m': args.height, 'scale': scale, 'bones': len(rig.data.bones) if rig else 0,
                  'meshes': [m.name for m in meshes], 'source_triangles': source_triangles,
                  'triangles': triangles, 'clips': clips, 'textures': textures,
                  'model_sha256': hashlib.sha256(raw).hexdigest(), 'outputs': {k: str(v) for k, v in paths.items()},
                  'options': {k: str(v) if isinstance(v, Path) else v for k, v in vars(args).items()}}
        bpy.ops.wm.save_as_mainfile(filepath=str(stage / 'game.blend'))
        if digest(args.source) != source_hash:
            raise ValueError('Source file changed during import.')
        texture_dir.mkdir(parents=True, exist_ok=True)
        for filename in {Path(texture['file']).name for texture in textures}:
            os.replace(stage / filename, texture_dir / filename)
        for path in paths.values():
            path.parent.mkdir(parents=True, exist_ok=True)
        os.replace(stage / 'game.blend', paths['blend'])
        (stage / 'build.json').write_text(json.dumps(report, indent=2) + '\n')
        os.replace(stage / 'build.json', paths['report'])
        os.replace(stage / 'model.glb', paths['model'])
    print('CHARACTER_IMPORT_OK', json.dumps(report), flush=True)
    return report


def main(argv=None):
    argv = sys.argv[sys.argv.index('--') + 1:] if argv is None and '--' in sys.argv else sys.argv[1:] if argv is None else argv
    args = arguments(argv)
    try:
        import bpy
    except ModuleNotFoundError:
        executable = shutil.which(args.blender)
        if not executable:
            raise SystemExit('Blender not found; use --blender or BLENDER_BIN.')
        return subprocess.call([executable, '--background', '--factory-startup', '--disable-autoexec',
                                '--python-exit-code', '1', '--python', str(Path(__file__).resolve()), '--', *argv])
    export_model(args)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
