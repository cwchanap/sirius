"""Build Mio's game asset from the user's Tripo sources in background Blender.

Blender --background --factory-startup --python tools/import_tripo_mio.py
The sources stay untouched. The low-poly body keeps Tripo's 65-bone Mixamo rig and the
clips the user made in Tripo Studio. Each clip is cut to its motion, retimed to the length
Player.cs was tuned against and kept over the hips; translation belongs to Godot. The three
attacks and Resonance Cut are keyed by tools/mio_combat.py on the same rig instead, and the opening's
waking and terminal clips by tools/cast_clips.py.
"""
import bpy
import bmesh
import json
import math
import numpy as np
import sys
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
import cast_clips  # noqa: E402
import mio_combat  # noqa: E402
OUT = ROOT / 'artifacts/mio-tripo-animation'
SOURCE = 'assets/source/tripo-bridge-20261016-mio-anim.blend'
FIXED = 'assets/source/mio-lowpoly-fixed-face2.blend'
# Game clip: Tripo action (after 'Armature|', names are cut at 63 characters), source frames,
# game frames. Game lengths are the previous clips', except the dodge, which fills Player.cs's
# 0.6 s roll. Jump frames 2/6/9 are takeoff/apex/descent, 10-13 landing.
CLIPS = {
    'idle': ('idle', 1, 369, 369),
    'walk': ('walk', 1, 57, 57),
    'run': ('run', 1, 31, 31),
    'jump': ('jump', 1, 13, 13),
    'guard': ('A swordswoman in a defensive guard stance: knees bent, her righ', 17, 25, 48),
    'dodge': ('A swordswoman does a quick forward combat roll over her shoulde', 8, 46, 15),
    'stagger': ('A swordswoman holding a sword in her right hand is hit hard in ', 1, 48, 18),
    'death': ('A swordswoman is struck by a fatal blow, drops to her knees, th', 1, 60, 36),
}
# Generated clips leave the fingers straight. Close the right fingers around the hilt (degrees per
# joint about their curl axis, local X) and take the thumb from the slash preset's tightest fist.
GRIP_CURL = (70, 85, 45)
GRIP_THUMB = 'slash'
# mio_combat.py keys its clips from the attack1 prompt's opening stance. That frame raises the left
# hand to her head; the stance takes the held guard's left arm, braced at the waist.
READY = 'A swordswoman steps forward and swings a one-handed sword in he'
# Tripo's roll lands on her back and sits up (source frames 27-35). Knots of (game frame from 0,
# source frame) speed the whole roll evenly so that stretch passes sooner and the rise gets the
# time; the hips then turn at most 64 degrees a frame (55 when sampled evenly).
DODGE_KNOTS = ((0, 8), (4, 18), (9, 34), (14, 46))
# Tripo's jump is a straight-legged hop. Per game frame: crouch on the ground (1-2, 10-13) and
# tuck in the air (3-9). Degrees about the armature's +X axis; she faces -Y.
JUMP_CROUCH = (.5, .25, 0, 0, 0, 0, 0, 0, 0, .2, .7, .45, .15)
JUMP_TUCK = (0, 0, .35, .7, .9, 1, .9, .6, .25, 0, 0, 0, 0)


def turn(bone, degrees):
    rest = bone.bone.matrix_local.to_3x3()
    return (rest.inverted() @ Quaternion((1, 0, 0), math.radians(degrees)).to_matrix() @ rest).to_quaternion()


def activate(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]


def principal(points):
    c = sum(points, Vector()) / len(points)
    axes = np.linalg.svd(np.array([p - c for p in points]))[2]
    return c, Vector(axes[0]), Vector(axes[1])


def seat_sword(rig, grip, swords):
    # The old rig carried the hilt along the hand; Tripo's swings expect it across the closed fist.
    # Each curled finger rings the hilt: fit a circle to its joints, run the grip through the centres,
    # blade out of the index side and edge along the knuckles.
    for name, basis in grip.items():
        rig.pose.bones[name].matrix_basis = basis
    bpy.context.view_layer.update()
    centres = []
    for finger in ('Index', 'Middle', 'Ring', 'Pinky'):
        bones = [rig.pose.bones[f'RightHand{finger}{j}'] for j in (1, 2, 3)]
        p = [b.head for b in bones] + [bones[-1].tail]
        c, u, v = principal(p)
        q = np.array([((x - c).dot(u), (x - c).dot(v)) for x in p])
        a, b, _ = np.linalg.lstsq(np.c_[2 * q, np.ones(len(q))], (q ** 2).sum(1), rcond=None)[0]
        centres.append(c + float(a) * u + float(b) * v)
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
    fist = sum(centres, Vector()) / 4
    across = (centres[0] - centres[3]).normalized()
    parts = {o.name.removeprefix('Sword '): [o.matrix_basis @ v.co for v in o.data.vertices] for o in swords}
    hilt, along, _ = principal(parts['oxblood leather'])
    blade, _, edge = principal(parts['blade steel'])
    if along.dot(blade - hilt) < 0:
        along.negate()
    turn = along.rotation_difference(across)
    edge = turn @ edge
    knuckles = rig.data.bones['RightHand'].matrix_local.col[1].to_3d()
    roll = math.atan2(edge.cross(knuckles).dot(across), edge.dot(knuckles))
    turn = Quaternion(across, (roll + math.pi / 2) % math.pi - math.pi / 2) @ turn
    seat = Matrix.Translation(fist) @ turn.to_matrix().to_4x4() @ Matrix.Translation(-hilt)
    for obj in swords:
        obj.data.transform(seat)


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
            if len(a) < len(b):
                a, b = b, a
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
    thumb = [pb for pb in rig.pose.bones if pb.name.startswith('RightHandThumb')]
    activate(rig, source_actions[GRIP_THUMB])
    first, last = map(int, source_actions[GRIP_THUMB].frame_range)
    frame = max(range(first, last + 1), key=lambda f: (scene.frame_set(f), sum(pb.matrix_basis.to_quaternion().angle for pb in thumb))[1])
    scene.frame_set(frame)
    grip = {pb.name: pb.matrix_basis.copy() for pb in thumb}
    for finger in ('Index', 'Middle', 'Ring', 'Pinky'):
        for joint, degrees in enumerate(GRIP_CURL, 1):
            grip[f'RightHand{finger}{joint}'] = Quaternion((1, 0, 0), math.radians(degrees)).to_matrix().to_4x4()
    activate(rig, source_actions[READY])
    scene.frame_set(1)
    ready = {pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones}
    ready['Hips'].translation -= basis @ Vector((hip.matrix.translation.x, hip.matrix.translation.y, 0))
    activate(rig, source_actions[CLIPS['guard'][0]])
    scene.frame_set(CLIPS['guard'][2])
    ready.update({pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones
                  if pb.name.startswith(('LeftShoulder', 'LeftArm', 'LeftForeArm', 'LeftHand'))})
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
    seat_sword(rig, grip, [o for o in scene.objects if o.name.startswith('Sword ')])
    for obj in list(old_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.scenes.remove(old_scene)
    # mio_combat.py points the seated blade, hilt to tip, with its edge leading.
    steel = scene.objects['Sword blade steel']
    blade, edge, _, _ = mio_combat.sword(rig, [steel.matrix_basis @ v.co for v in steel.data.vertices])
    clips = {}
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
            if name == 'dodge':
                frame = float(np.interp(t, *zip(*DODGE_KNOTS)))
            scene.frame_set(int(frame), subframe=frame % 1)
            p = hip.matrix.translation
            # Tripo's jump travels upward; Godot's physics owns the height.
            lift = max(0, min(f.head.z for f in feet) - foot_floor) if name == 'jump' else 0
            pose = {pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones}
            pose['Hips'].translation -= basis @ Vector((p.x, p.y, lift))
            pose.update({n: m.copy() for n, m in grip.items()})
            poses.append(pose)
        if name == 'jump':
            rig.animation_data.action = None
            for t, pose in enumerate(poses):
                for pb in rig.pose.bones:
                    pb.rotation_mode = 'QUATERNION'
                    pb.matrix_basis = pose[pb.name]
                c, k = JUMP_CROUCH[t], JUMP_TUCK[t]
                for side in ('Left', 'Right'):
                    for bone, degrees in (('UpLeg', -50 * c - 60 * k), ('Leg', 90 * c + 80 * k), ('Foot', -40 * c - 20 * k)):
                        pb = rig.pose.bones[side + bone]
                        pb.rotation_quaternion = turn(pb, degrees) @ pb.rotation_quaternion
                spine = rig.pose.bones['Spine']
                spine.rotation_quaternion = turn(spine, 20 * c + 10 * k) @ spine.rotation_quaternion
                if c:
                    # A crouch on the ground lowers the hips until the feet are back on the floor.
                    bpy.context.view_layer.update()
                    hip.location -= basis @ Vector((0, 0, min(f.head.z for f in feet) - foot_floor))
                pose.update({pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones})
        clips[name] = poses
    clips.update(mio_combat.clips(rig, ready, (blade, edge), grip))
    clips.update(cast_clips.poses(rig, scale, cast_clips.MIO, cast_clips.MIO_LOOPS, grip))
    actions = []
    for name, poses in clips.items():
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
              'clips': {n: {'tripo': CLIPS[n][0], 'source_frames': list(CLIPS[n][1:3]), 'frames': CLIPS[n][3]} if n in CLIPS
                        else {'keyed': 'tools/cast_clips.py' if n in cast_clips.MIO else 'tools/mio_combat.py', 'frames': len(p)}
                        for n, p in clips.items()},
              'weapon_faces': weapon_faces}
    (OUT / 'build.json').write_text(json.dumps(report, indent=2) + '\n')
    print('TRIPO_EXPORT_COMPLETE', json.dumps(report), flush=True)


if __name__ == '__main__':
    main()
