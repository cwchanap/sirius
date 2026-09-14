"""Build Mio's game asset from the preserved Tripo source in background Blender.

Blender --background --factory-startup --python tools/import_tripo_mio.py
The source import stays untouched. The four Tripo clips retain their poses;
translation belongs to Godot. Existing combat clips are retargeted by rest basis.
"""
import bpy
import json
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'artifacts/tripo-integration'
COMBAT = ('attack1', 'attack2', 'attack3', 'guard', 'dodge', 'skill', 'stagger', 'death')


def activate(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]


def main():
    bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'assets/source/mio-tripo-original.blend'))
    scene = bpy.context.scene
    scene.name = 'Mio_Tripo_Game'
    rig = bpy.data.objects['Mio']
    body = bpy.data.objects['tripo_node_b15809b7']
    source_actions = list(bpy.data.actions)
    assert len(source_actions) == 4 and len(rig.data.bones) == 41
    images = []
    for mat in body.data.materials:
        for node in mat.node_tree.nodes:
            if node.type == 'TEX_IMAGE' and node.image:
                img = node.image
                assert img.packed_file and tuple(img.size) == (4096, 4096)
                filename = 'mio-tripo-basecolor.jpg' if img.name == 'Mio_basecolor' else 'mio-tripo-normal.png'
                (ROOT / 'assets/art' / filename).write_bytes(img.packed_file.data)
                images.append({'name': img.name, 'size': list(img.size)})
    # Remove the import's object offset. The mesh and armature share a rest space.
    rig.animation_data.action = None
    rig.matrix_world = Matrix.Identity(4)
    body.matrix_parent_inverse = Matrix.Identity(4)
    body.matrix_basis = Matrix.Identity(4)
    rig.data.pose_position = 'REST'
    scene.frame_set(1)
    bpy.context.view_layer.update()
    zmin = min(v.co.z for v in body.data.vertices)
    zmax = max(v.co.z for v in body.data.vertices)
    scale = 1.84 / (zmax - zmin)
    root = bpy.data.objects.new('MioTripo', None)
    scene.collection.objects.link(root)
    root.scale = (scale,) * 3
    root.location.z = -zmin * scale
    rig.parent = root
    rig.data.pose_position = 'POSE'
    actions = []
    for source in source_actions:
        action = source.copy()
        action.name = source.name.rsplit(':', 1)[-1]
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for f in list(bag.fcurves):
                        if not f.data_path.startswith('pose.bones['):
                            bag.fcurves.remove(f)
        activate(rig, action)
        # Hip is below a stationary Root; remove horizontal travel and the import offset.
        hip = rig.pose.bones['Hip']
        basis = hip.bone.matrix_local.to_3x3().inverted()
        samples = []
        # The supplied jump is five hops. Keep its first takeoff/contact/recovery only.
        end = 13 if action.name == 'jump' else int(action.frame_range[1])
        scene.frame_set(1)
        foot_floor = min(rig.pose.bones[n].head.z for n in ('L_Foot', 'R_Foot'))
        for frame in range(int(action.frame_range[0]), end + 1):
            scene.frame_set(frame)
            p = hip.matrix.translation
            lift = max(0, min(rig.pose.bones[n].head.z for n in ('L_Foot', 'R_Foot')) - foot_floor) if action.name == 'jump' else 0
            samples.append((frame, hip.location - basis @ Vector((p.x, p.y, lift))))
        for frame, location in samples:
            hip.location = location
            hip.keyframe_insert('location', frame=frame)
        if action.name == 'jump':
            for layer in action.layers:
                for strip in layer.strips:
                    for bag in strip.channelbags:
                        for curve in bag.fcurves:
                            for i in range(len(curve.keyframe_points)-1, -1, -1):
                                if curve.keyframe_points[i].co.x > end:
                                    curve.keyframe_points.remove(curve.keyframe_points[i], fast=True)
                            curve.update()
            action.use_frame_range = True
            action.frame_start, action.frame_end = 1, end
        actions.append(action)
    rig.animation_data.action = None
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    # Decimate the game copy before skin deformation; retain UVs and vertex groups.
    original_faces = len(body.data.polygons)
    bpy.ops.object.select_all(action='DESELECT')
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    decimate = body.modifiers.new('Game mesh budget', 'DECIMATE')
    decimate.ratio = 150000 / original_faces
    decimate.use_collapse_triangulate = True
    bpy.ops.object.modifier_move_up(modifier=decimate.name)
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    # FBX custom corner normals refer to the dense surface; recalculate after reduction.
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    body.data.normals_split_custom_set([(0, 0, 0)] * len(body.data.loops))
    body.name = 'Mio Tripo body'
    print('TRIPO_DECIMATED', len(body.data.vertices), len(body.data.polygons), flush=True)

    with bpy.data.libraries.load(str(ROOT / 'assets/source/characters.blend')) as (src, dst):
        dst.scenes = ['HumanV2_mio']
    old_scene = dst.scenes[0]
    old = next(o for o in old_scene.objects if o.type == 'ARMATURE')
    scene.collection.objects.link(old)
    for track in old.animation_data.nla_tracks:
        track.mute = True
    mapping = {'Root': 'Root', 'Hip': 'Hips', 'Spine01': 'Spine',
               'Spine02': 'Chest', 'NeckTwist01': 'Neck', 'Head': 'Head'}
    # The old author's L/R names were reversed relative to the imported skeleton.
    for new, previous in [('R', 'L'), ('L', 'R')]:
        for a, b in [('Clavicle', 'Clavicle'), ('Upperarm', 'Arm'), ('Forearm', 'Forearm'),
                     ('Hand', 'Hand'), ('Thigh', 'Thigh'), ('Calf', 'Shin'),
                     ('Foot', 'Foot'), ('ToeBase', 'Toe')]:
            mapping[new + '_' + a] = previous + b
    for name in COMBAT:
        old_action = next(t.strips[0].action for t in old.animation_data.nla_tracks if t.name == name)
        activate(old, old_action)
        action = bpy.data.actions.new('TripoCombat_' + name)
        rig.animation_data.action = action
        end = round((old_action.frame_range[1] - 1) * 24 / old_scene.render.fps) + 1
        for frame in range(1, end + 1):
            source_frame = 1 + (frame - 1) * old_scene.render.fps / 24
            scene.frame_set(int(source_frame), subframe=source_frame % 1)
            for pb in rig.pose.bones:
                pb.matrix_basis = Matrix.Identity(4)
                pb.rotation_mode = 'QUATERNION'
            for target, source in mapping.items():
                pb, src = rig.pose.bones[target], old.pose.bones[source]
                basis = pb.bone.matrix_local.to_3x3().inverted() @ src.bone.matrix_local.to_3x3()
                pb.rotation_quaternion = (basis @ src.matrix_basis.to_3x3() @ basis.inverted()).to_quaternion()
                if target in ('Root', 'Hip'):
                    pb.location = basis @ src.location / scale
                    if target == 'Hip':
                        h = pb.bone.head_local
                        pb.location -= pb.bone.matrix_local.to_3x3().inverted() @ Vector((h.x, h.y, 0))
            if name == 'dodge':
                # Roll around the torso instead of rotating the head through the ground.
                bpy.context.view_layer.update()
                delta = Vector((0, 0, rig.data.bones['Hip'].head_local.z)) - rig.pose.bones['Hip'].head
                pivot = rig.pose.bones['Root']
                pivot.location += pivot.bone.matrix_local.to_3x3().inverted() @ delta
                bpy.context.view_layer.update()
                evaluated = body.evaluated_get(bpy.context.evaluated_depsgraph_get())
                low = min((evaluated.matrix_world @ Vector(c)).z for c in evaluated.bound_box)
                if low < .02:
                    pivot.location += pivot.bone.matrix_local.to_3x3().inverted() @ Vector((0, 0, (.02 - low) / scale))
            for pb in rig.pose.bones:
                pb.keyframe_insert('rotation_quaternion', frame=frame)
                pb.keyframe_insert('location', frame=frame)
                pb.keyframe_insert('scale', frame=frame)
        actions.append(action)
    # Keep the original sword, fitted to the new right-hand rest transform.
    hand = rig.data.bones['R_Hand']
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
        sword.vertex_groups.new(name='R_Hand').add(list(range(len(used))), 1, 'REPLACE')
        sword.modifiers.new('Sword hand', 'ARMATURE').object = rig
        weapon_faces += len(faces)
    assert weapon_faces > 0
    scene.collection.objects.unlink(old)
    for obj in list(old_scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.scenes.remove(old_scene)
    rig.animation_data.action = None
    for action in list(bpy.data.actions):
        if action not in actions:
            bpy.data.actions.remove(action)
    for action in actions:
        action.name = action.name.removeprefix('TripoCombat_')
        action.use_fake_user = True
        track = rig.animation_data.nla_tracks.new()
        track.name = action.name
        track.strips.new(action.name, 1, action)
        track.mute = True
    for pb in rig.pose.bones:
        pb.matrix_basis = Matrix.Identity(4)
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
    report = {'source': 'assets/source/mio-tripo-original.blend', 'bones': len(rig.data.bones),
              'source_faces': original_faces, 'body_faces': len(body.data.polygons),
              'height_m': 1.84, 'scale': scale, 'images': images,
              'imported_clips': ['idle', 'walk', 'run', 'jump'], 'retargeted_clips': list(COMBAT),
              'clips': {a.name: list(a.frame_range) for a in actions}, 'weapon_faces': weapon_faces}
    (OUT / 'build.json').write_text(json.dumps(report, indent=2) + '\n')
    print('TRIPO_EXPORT_COMPLETE', json.dumps(report), flush=True)


if __name__ == '__main__':
    main()
