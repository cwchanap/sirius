"""Build Mio's game asset from the user's Tripo sources in background Blender.

Blender --background --factory-startup --python tools/import_tripo_mio.py
The sources stay untouched. The low-poly body keeps Tripo's 65-bone Mixamo rig and the
clips the user made in Tripo Studio. Each clip is cut to its motion, retimed to the length
Player.cs was tuned against and kept over the hips; translation belongs to Godot.
"""
import bpy
import bmesh
import json
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/mio-tripo-animation'
SOURCE = 'assets/source/tripo-bridge-20261016-mio-anim.blend'
FIXED = 'assets/source/mio-lowpoly-fixed.blend'
# Game clip: Tripo action (after 'Armature|', names are cut at 63 characters), source frames,
# game frames. Game lengths are the previous clips'. Windows put the fastest swing near the
# hit Player.cs times (frames 6/7/8 of attack1/2/3, 17 of skill); attack2's swing opens its
# clip, so it lands a little early. Jump frames 2/6/9 are takeoff/apex/descent, 10-13 landing.
CLIPS = {
    'idle': ('idle', 1, 369, 369),
    'walk': ('walk', 1, 57, 57),
    'run': ('run', 1, 31, 31),
    'jump': ('jump', 1, 13, 13),
    'attack1': ('A swordswoman steps forward and swings a one-handed sword in he', 24, 48, 15),
    'attack2': ('A swordswoman holding a one-handed sword in her right hand does', 1, 10, 16),
    'attack3': ('A swordswoman holding a one-handed sword in her right hand rais', 16, 48, 20),
    'guard': ('A swordswoman in a defensive guard stance: knees bent, her righ', 17, 25, 48),
    'dodge': ('A swordswoman does a quick forward combat roll over her shoulde', 1, 48, 16),
    'skill': ('A swordswoman crouches and pulls her sword back with her right ', 1, 50, 31),
    'stagger': ('A swordswoman holding a sword in her right hand is hit hard in ', 1, 48, 18),
    'death': ('A swordswoman is struck by a fatal blow, drops to her knees, th', 1, 60, 36),
}


def activate(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]


def main():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT / SOURCE))
    scene = bpy.context.scene
    scene.name = 'Mio_Tripo_Game'
    rig = bpy.data.objects['3d fantasy character model.002']
    body = next(o for o in rig.children if o.type == 'MESH')
    assert len(rig.data.bones) == 65 and len(body.data.polygons) == 47383
    for obj in list(bpy.data.objects):
        if obj not in (rig, body):
            bpy.data.objects.remove(obj)
    rig.name, body.name = 'Mio', 'Mio Tripo body'
    # The same mesh as the repaired source: take its hair-colour and normal-map repairs.
    with bpy.data.libraries.load(str(ROOT / FIXED)) as (src, dst):
        dst.images = ['3d_fantasy_character_model_basecolor', '3d_fantasy_character_model_normal']
    fixed = {i.name.rsplit('_', 1)[-1].split('.')[0]: i for i in dst.images}
    tree = body.data.materials[0].node_tree
    bsdf = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
    # Tripo's roughness and metallic bakes carry the same projection errors; keep Mio matte.
    for socket, value in (('Roughness', .9), ('Metallic', 0)):
        for link in list(bsdf.inputs[socket].links):
            tree.nodes.remove(link.from_node)
        bsdf.inputs[socket].default_value = value
    images = []
    for node in tree.nodes:
        if node.type == 'TEX_IMAGE' and node.image:
            kind = node.image.name.rsplit('_', 1)[-1].split('.')[0]
            img = node.image = fixed[kind]
            data = bytes(img.packed_file.data)
            filename = f'mio-lowpoly-{kind}.' + ('png' if data.startswith(b'\x89PNG') else 'jpg')
            assert (ROOT / 'assets/art' / filename).read_bytes() == data
            img.name = 'Mio_lowpoly_' + kind
            img.filepath_raw = '//' + filename
            images.append({'name': img.name, 'file': filename, 'size': list(img.size)})
    assert sorted(i['file'] for i in images) == ['mio-lowpoly-basecolor.jpg', 'mio-lowpoly-normal.png']
    # Drop the three isolated triangles left by Tripo's reduction, as in the repaired source.
    bm = bmesh.new()
    bm.from_mesh(body.data)
    parts = {v: {v} for v in bm.verts}
    for e in bm.edges:
        a, b = parts[e.verts[0]], parts[e.verts[1]]
        if a is not b:
            a |= b
            for v in b:
                parts[v] = a
    main_part = max(parts.values(), key=len)
    stray = [f for f in bm.faces if f.verts[0] not in main_part]
    assert len(stray) == 3
    bmesh.ops.delete(bm, geom=stray, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(body.data)
    bm.free()
    # Recalculate the bridge FBX's corner normals, as for the previous game copy.
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    body.data.normals_split_custom_set([(0, 0, 0)] * len(body.data.loops))
    for bone in rig.data.bones:
        bone.name = bone.name.removeprefix('mixamorig:')
    # Renaming only fixes the assigned action's channels.
    for action in bpy.data.actions:
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        curve.data_path = curve.data_path.replace('pose.bones["mixamorig:', 'pose.bones["')
    rig.matrix_world = Matrix.Identity(4)
    body.matrix_parent_inverse = Matrix.Identity(4)
    body.matrix_basis = Matrix.Identity(4)
    zmin = min(v.co.z for v in body.data.vertices)
    zmax = max(v.co.z for v in body.data.vertices)
    scale = 1.84 / (zmax - zmin)
    root = bpy.data.objects.new('MioTripo', None)
    scene.collection.objects.link(root)
    root.scale = (scale,) * 3
    root.location.z = -zmin * scale
    rig.parent = root
    source_actions = {a.name.removeprefix('Armature|'): a for a in bpy.data.actions}
    hip = rig.pose.bones['Hips']
    basis = hip.bone.matrix_local.to_3x3().inverted()
    feet = [rig.pose.bones[n] for n in ('LeftFoot', 'RightFoot')]
    actions = []
    for name, (source, first, last, frames) in CLIPS.items():
        activate(rig, source_actions[source])
        scene.frame_set(first)
        foot_floor = min(f.head.z for f in feet)
        poses = []
        for t in range(frames):
            u = t / (frames - 1)
            # Guard holds its block: play the held span forward and back so the loop closes.
            if name == 'guard':
                u = 1 - abs(1 - 2 * u)
            frame = first + (last - first) * u
            scene.frame_set(int(frame), subframe=frame % 1)
            p = hip.matrix.translation
            # Tripo's jump travels upward; Godot's physics owns the height.
            lift = max(0, min(f.head.z for f in feet) - foot_floor) if name == 'jump' else 0
            pose = {pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones}
            pose['Hips'].translation -= basis @ Vector((p.x, p.y, lift))
            poses.append(pose)
        action = bpy.data.actions.new(name)
        activate(rig, action)
        for t, pose in enumerate(poses, 1):
            for pb in rig.pose.bones:
                pb.rotation_mode = 'QUATERNION'
                pb.matrix_basis = pose[pb.name]
                pb.keyframe_insert('rotation_quaternion', frame=t)
                pb.keyframe_insert('location', frame=t)
                pb.keyframe_insert('scale', frame=t)
        actions.append(action)
    for action in list(bpy.data.actions):
        if action not in actions:
            bpy.data.actions.remove(action)
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)

    # Keep the original sword, fitted to the new right-hand rest transform.
    with bpy.data.libraries.load(str(ROOT / 'assets/source/characters.blend')) as (src, dst):
        dst.scenes = ['HumanV2_mio']
    old_scene = dst.scenes[0]
    old = next(o for o in old_scene.objects if o.type == 'ARMATURE')
    hand = rig.data.bones['RightHand']
    old_hand = old.data.bones['LHand']
    fit = hand.matrix_local @ Matrix.Scale(1 / scale, 4) @ old_hand.matrix_local.inverted()
    weapon_faces = 0
    for obj in list(old_scene.objects):
        if obj.type != 'MESH' or obj.name not in ('antique brass', 'oxblood leather', 'jade terminal', 'blade steel'):
            continue
        group = obj.vertex_groups.get('LHand')
        if not group:
            continue
        indices = {v.index for v in obj.data.vertices if any(g.group == group.index and g.weight > .99 for g in v.groups)}
        faces = [list(p.vertices) for p in obj.data.polygons if all(v in indices for v in p.vertices)]
        if not faces:
            continue
        used = sorted({i for f in faces for i in f})
        remap = {v: i for i, v in enumerate(used)}
        mesh = bpy.data.meshes.new('Sword ' + obj.name)
        mesh.from_pydata([fit @ (obj.matrix_world @ obj.data.vertices[i].co) for i in used], [],
                         [[remap[i] for i in f] for f in faces])
        mesh.materials.append(obj.data.materials[0])
        sword = bpy.data.objects.new(mesh.name, mesh)
        scene.collection.objects.link(sword)
        sword.parent = rig
        sword.vertex_groups.new(name='RightHand').add(list(range(len(used))), 1, 'REPLACE')
        sword.modifiers.new('Sword hand', 'ARMATURE').object = rig
        weapon_faces += len(faces)
    assert weapon_faces > 0
    for obj in list(old_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.scenes.remove(old_scene)
    for action in actions:
        action.use_fake_user = True
        track = rig.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name, 1, action)
        track.mute = True
    scene.frame_set(1)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(ROOT / 'assets/models/mio.glb'), export_format='GLB',
        use_selection=True, use_active_scene=True, export_animations=True,
        export_animation_mode='NLA_TRACKS', export_apply=False, export_frame_range=False)
    activate(rig, next(a for a in actions if a.name == 'idle'))
    scene.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'assets/source/mio-tripo-game.blend'))
    OUT.mkdir(parents=True, exist_ok=True)
    report = {'source': SOURCE, 'textures_from': FIXED, 'bones': len(rig.data.bones),
              'body_faces': len(body.data.polygons), 'height_m': 1.84, 'scale': scale, 'images': images,
              'clips': {n: {'tripo': CLIPS[n][0], 'source_frames': list(CLIPS[n][1:3]), 'frames': CLIPS[n][3]} for n in CLIPS},
              'weapon_faces': weapon_faces}
    (OUT / 'build.json').write_text(json.dumps(report, indent=2) + '\n')
    print('TRIPO_EXPORT_COMPLETE', json.dumps(report), flush=True)


if __name__ == '__main__':
    main()
