"""Render tools/mio_combat.py's clips on the committed mio.glb as contact sheets, without the Tripo sources.

xvfb-run -a blender --background --factory-startup --python tools/preview_mio_combat.py -- [--out=DIR] [--only=attack1,skill] [--view=front|side|back|top] [--glb]
Each frame shows the pose and, inside the clip's trail window, the blade's last 0.16 s (0.3 s in Resonance Cut) as Godot's sword trail draws it.
Writes DIR/<clip>-<view>.png (default artifacts/mio-combat) with ImageMagick's montage. --glb also writes DIR/mio.glb,
the committed model with these clips swapped in, to try in Godot; the game's model comes from import_tripo_mio.py.
mio_ready.json is the ready stance with the grip that import_tripo_mio.py keys the clips from, as the GLB's attack1 opened
before these clips replaced it.
"""
import bpy
import json
import subprocess
import sys
from pathlib import Path
import numpy as np
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools'))
import mio_combat  # noqa: E402

VIEWS = {'front': ((-1.9, -2.7, 1.4), (0, -.45, .95)), 'side': ((-3.1, -.5, 1.15), (0, -.5, .95)), 'back': ((1.6, 2.4, 1.6), (0, -.45, 1.0)), 'top': ((-.4, -1.0, 4.2), (0, -.5, .6))}


def args():
    out = {'out': str(ROOT / 'artifacts/mio-combat'), 'only': 'attack1,attack2,attack3,skill', 'view': 'front'}
    for a in sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []:
        key, _, value = a.lstrip('-').partition('=')
        out[key] = value
    return out


def main():
    opts = args()
    out = Path(opts['out'])
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'assets/models/mio.glb'))
    for obj in list(bpy.data.objects):
        if obj.type == 'MESH' and obj.parent is None:
            bpy.data.objects.remove(obj)
    rig = bpy.data.objects['Mio']
    scene = bpy.context.scene
    ready = {n: Matrix([m[i:i + 4] for i in range(0, 16, 4)]) for n, m in json.loads((ROOT / 'tools/mio_ready.json').read_text()).items()}
    grip = {n: m.copy() for n, m in ready.items() if n.startswith('RightHand') and n != 'RightHand'}
    rig.animation_data.action = None
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    # The blade's own axes in the hand's frame, from the skinned steel at rest.
    steel = bpy.data.objects['Sword blade steel']
    points = [rig.matrix_world.inverted() @ steel.matrix_world @ v.co for v in steel.data.vertices]
    hand = rig.data.bones['RightHand'].matrix_local
    tip = max(points, key=lambda p: (p - hand.translation).length)
    hilt = max(points, key=lambda p: (p - tip).length)
    centred = np.array([p - sum(points, Vector()) / len(points) for p in points])
    edge = Vector(np.linalg.svd(centred)[2][1])
    rot = hand.to_3x3().inverted()
    blade = (rot @ (tip - hilt)).normalized()
    edge = rot @ edge
    # The edge faces the knuckles, as the importer seats it.
    edge = (edge - edge.dot(blade) * blade).normalized() * (1 if edge.y > 0 else -1)
    ends = [hand.inverted() @ hilt.lerp(tip, .15), hand.inverted() @ tip]
    clips = mio_combat.clips(rig, ready, (blade, edge), grip)
    if 'glb' in opts:
        for name, poses in clips.items():
            action = bpy.data.actions.new(name)
            rig.animation_data.action = action
            for t, basis in enumerate(poses, 1):
                for pb in rig.pose.bones:
                    pb.rotation_mode = 'QUATERNION'
                    pb.matrix_basis = basis[pb.name]
                    for path in ('rotation_quaternion', 'location', 'scale'):
                        pb.keyframe_insert(path, frame=t)
            rig.animation_data.action = None
            tracks = rig.animation_data.nla_tracks
            tracks.remove(tracks[name])
            track = tracks.new()
            track.name = name
            track.strips.new(name, 1, action)
            track.mute = True
        bpy.ops.object.select_all(action='SELECT')
        bpy.context.view_layer.objects.active = rig
        bpy.ops.export_scene.gltf(filepath=str(out / 'mio.glb'), export_format='GLB', use_selection=True,
                                  export_animations=True, export_animation_mode='NLA_TRACKS', export_apply=False)

    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'TEXTURE'
    scene.render.resolution_x, scene.render.resolution_y = 360, 440
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new('w')
    bpy.ops.mesh.primitive_plane_add(size=8)
    floor = bpy.context.object
    floor.data.materials.append(bpy.data.materials.new('floor'))
    floor.data.materials[0].diffuse_color = (.35, .37, .33, 1)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.lens = 32
    eye, look = VIEWS[opts['view']]
    cam.location = eye
    cam.rotation_euler = (Vector(look) - Vector(eye)).to_track_quat('-Z', 'Y').to_euler()
    ribbon = bpy.data.objects.new('trail', bpy.data.meshes.new('trail'))
    scene.collection.objects.link(ribbon)
    glow = bpy.data.materials.new('glow')
    glow.diffuse_color = (.45, .9, .8, 1)
    ribbon.data.materials.append(glow)
    for name in opts['only'].split(','):
        frames, path = [], []
        for f, basis in enumerate(clips[name]):
            for pb in rig.pose.bones:
                pb.rotation_mode = 'QUATERNION'
                pb.matrix_basis = basis[pb.name]
            bpy.context.view_layer.update()
            m = rig.matrix_world @ rig.pose.bones['RightHand'].matrix
            t0, t1 = mio_combat.TRAIL[name]
            t = mio_combat.time(f)
            path = path[-int(mio_combat.FPS * (.3 if name == 'skill' else .16)):] if t0 <= t <= t1 else []
            if t0 <= t <= t1:
                path.append((m @ ends[0], m @ ends[1]))
            mesh = bpy.data.meshes.new('trail')
            verts = [v for pair in path for v in pair]
            mesh.from_pydata(verts, [], [(2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2) for i in range(len(path) - 1)])
            mesh.materials.append(glow)
            old = ribbon.data
            ribbon.data = mesh
            bpy.data.meshes.remove(old)
            frame = out / f'{name}-{opts["view"]}-{f:02d}.png'
            scene.render.filepath = str(frame)
            bpy.ops.render.render(write_still=True)
            frames.append(str(frame))
        sheet = out / f'{name}-{opts["view"]}.png'
        labels = [a for i, p in enumerate(frames) for a in ('-label', f'{name} {i + 1}/{mio_combat.FPS}s', p)]
        subprocess.run(['montage', *labels, '-tile', '7x', '-geometry', '+2+2', '-pointsize', '14', str(sheet)], check=True)
        for p in frames:
            Path(p).unlink()
        print('COMBAT_PREVIEW', sheet, flush=True)


if __name__ == '__main__':
    main()
