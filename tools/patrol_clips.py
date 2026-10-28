"""The patrol robot's clips, keyed by hand on its Tripo rig (Mixamo bones, prefix stripped), as Enemy.cs plays them.

import_tripo_patrol.py keys them on the Tripo source before the shared importer writes patrol.glb. Run on its own, this file
swaps them into the committed patrol.glb and renders contact sheets, so they can be tuned without the source:
xvfb-run -a blender --background --factory-startup --python tools/patrol_clips.py -- [--out=DIR] [--only=walk,attack2] [--view=front|side|quarter] [--glb]
Writes DIR/<clip>-<view>.png (default artifacts/patrol-clips) with ImageMagick's montage; --glb also writes DIR/patrol.glb.

A pose turns each bone relative to its parent, in degrees about the robot's own axes as they stand in the rest A-pose: x to its
right, y forward, z up, turned about x, then y, then z. +x swings a hanging limb forward and tips an upright bone back; +y lowers
the right arm out of the A-pose; +z turns to the robot's left. Left bones take mirrored values, so the same numbers pose either
side alike. A forearm or a leg given one number bends at the elbow or knee by that much, and 'curl' bends every finger joint
toward the palm. 'lift' moves the hips, in metres at game scale. The feet keep level with the ground.
"""
import math
import subprocess
import sys
from pathlib import Path
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
FPS = 24
# The importer's --height. Enemy.cs scales the patrols and scouts from it, and the lifts below are at this size.
HEIGHT = 1.83
# Its frame to the armature's: it faces -Y with its right hand on -X.
FLIP = Matrix(((-1, 0, 0), (0, -1, 0), (0, 0, 1)))
MIRROR = Matrix(((-1, 0, 0), (0, 1, 0), (0, 0, 1)))
FINGERS = [f'{s}Hand{f}{j}' for s in ('Left', 'Right') for f in ('Index', 'Middle', 'Ring', 'Pinky') for j in (1, 2, 3)]
STANCE = {
    'lift': (0, 0, -.02), 'Hips': (0, 0, 0), 'Spine': (-3, 0, 0), 'Spine1': (0, 0, 0), 'Spine2': (0, 0, 0), 'Neck': (0, 0, 0), 'Head': (4, 0, 0),
    'RightArm': (6, 26, 0), 'LeftArm': (6, 26, 0), 'RightForeArm': 18, 'LeftForeArm': 18, 'RightHand': (0, 0, 0), 'LeftHand': (0, 0, 0),
    'RightUpLeg': (4, 0, 0), 'LeftUpLeg': (4, 0, 0), 'RightLeg': 8, 'LeftLeg': 8, 'curl': 22,
}
# The windup Enemy.cs plays as 'guard' before a strike (and holds while it recovers): baton cocked over its right shoulder,
# left arm out in front, torso wound to its right. attack2 swings from here.
COCKED = {
    **STANCE, 'lift': (0, -.04, -.1), 'Hips': (0, 0, -10), 'Spine': (2, 0, -10), 'Spine1': (2, 0, -6), 'Spine2': (0, 0, -4), 'Head': (0, 0, 16),
    'RightArm': (120, 6, 0), 'RightForeArm': 100, 'LeftArm': (55, 8, 0), 'LeftForeArm': 85,
    'RightUpLeg': (-12, 0, 0), 'LeftUpLeg': (20, 0, 0), 'RightLeg': 22, 'LeftLeg': 28, 'curl': 40,
}
# Knocked back by a blow, then sagging: a stagger ends here, and a stun or posture break holds it.
JOLT = {**STANCE, 'lift': (0, -.08, -.04), 'Spine': (14, 0, 0), 'Spine1': (6, 0, 0), 'Head': (16, 0, 0),
        'RightArm': (-12, -14, 0), 'LeftArm': (-12, -14, 0), 'RightForeArm': 40, 'LeftForeArm': 40, 'RightUpLeg': (12, 0, 0), 'LeftUpLeg': (2, 0, 0), 'RightLeg': 18, 'LeftLeg': 12}
SLUMP = {**STANCE, 'lift': (0, 0, -.1), 'Spine': (-14, 0, 0), 'Spine1': (-8, 0, 0), 'Head': (-22, 0, 0),
         'RightArm': (8, 36, 0), 'LeftArm': (8, 36, 0), 'RightForeArm': 10, 'LeftForeArm': 10, 'RightUpLeg': (14, 0, 0), 'LeftUpLeg': (14, 0, 0), 'RightLeg': 24, 'LeftLeg': 24, 'curl': 10}
KNEEL = {**SLUMP, 'lift': (0, -.06, -.56), 'Spine': (-12, 0, 0), 'Head': (6, 0, 0), 'RightUpLeg': (4, 0, 0), 'LeftUpLeg': (8, 0, 0),
         'RightLeg': 95, 'LeftLeg': 92, 'RightArm': (2, 30, 0), 'LeftArm': (2, 30, 0)}


def gait(stride, load, swing, arm, elbow, lean, bob, sway):
    """A walk or run cycle by phase (0 to 1): the right foot strikes at 0, the left at a half."""
    def at(p):
        pose = dict(STANCE)
        for side, q in (('Right', p), ('Left', p + .5)):
            c, s = math.cos(math.tau * q), math.sin(math.tau * q)
            pose[side + 'UpLeg'] = (stride * c + 4, 0, 0)
            pose[side + 'Leg'] = 8 + (load * s if s > 0 else -swing * s)
            pose[side + 'Arm'] = (-arm * c + 6, 26, 0)
            pose[side + 'ForeArm'] = elbow + arm * .3 * max(0, -c)
        pose['lift'] = (0, 0, -.02 - bob * math.cos(2 * math.tau * p))
        pose['Hips'] = (0, 0, sway * math.cos(math.tau * p))
        pose['Spine'] = (-lean, 0, -sway * 1.6 * math.cos(math.tau * p))
        pose['Head'] = (4 + lean * .6, 0, sway * .6 * math.cos(math.tau * p))
        return pose
    return at


def tween(keys):
    """Ease from key to key, each channel stopping at every key: a machine's start and stop."""
    def at(t):
        i = max(k for k in range(len(keys)) if keys[k][0] <= t + 1e-6)
        if i == len(keys) - 1:
            return keys[i][1]
        (t0, a), (t1, b) = keys[i], keys[i + 1]
        u = (t - t0) / (t1 - t0)
        u = u * u * (3 - 2 * u)
        return {k: tuple(x + (y - x) * u for x, y in zip(a[k], b[k])) if isinstance(a[k], tuple) else a[k] + (b[k] - a[k]) * u for k in a}
    return at


# name: (seconds, pose at a time; a looped clip ends where it began)
CLIPS = {
    'idle': (2, tween([(0, STANCE), (.5, {**STANCE, 'Head': (2, 0, 14)}),
                       (1, {**STANCE, 'Spine2': (3, 0, 0), 'RightArm': (6, 23, 0), 'LeftArm': (6, 23, 0), 'lift': (0, 0, -.01)}),
                       (1.5, {**STANCE, 'Head': (2, 0, -14)}), (2, STANCE)])),
    'walk': (1, lambda t: gait(stride=22, load=12, swing=48, arm=16, elbow=18, lean=4, bob=.025, sway=5)(t / 1)),
    'run': (.75, lambda t: gait(stride=36, load=22, swing=95, arm=34, elbow=70, lean=14, bob=.045, sway=7)(t / .75)),
    'guard': (1.3, tween([(0, COCKED), (.65, {**COCKED, 'lift': (0, -.05, -.12), 'Spine': (4, 0, -14), 'RightArm': (126, 6, 0)}), (1.3, COCKED)])),
    'attack2': (.7, tween([
        (0, COCKED),
        (.12, {**STANCE, 'lift': (0, .1, -.12), 'Hips': (0, 0, 8), 'Spine': (-16, 0, 10), 'Spine1': (-4, 0, 6), 'Spine2': (0, 0, 4), 'Head': (10, 0, -12),
               'RightArm': (62, 4, 22), 'RightForeArm': 14, 'LeftArm': (-22, 18, 0), 'LeftForeArm': 45,
               'RightUpLeg': (2, 0, 0), 'LeftUpLeg': (26, 0, 0), 'RightLeg': 28, 'LeftLeg': 30, 'curl': 40}),
        (.26, {**STANCE, 'lift': (0, .12, -.13), 'Hips': (0, 0, 12), 'Spine': (-20, 0, 14), 'Spine1': (-4, 0, 6), 'Spine2': (0, 0, 4), 'Head': (12, 0, -14),
               'RightArm': (30, 14, 40), 'RightForeArm': 24, 'LeftArm': (-18, 22, 0), 'LeftForeArm': 40,
               'RightUpLeg': (0, 0, 0), 'LeftUpLeg': (26, 0, 0), 'RightLeg': 30, 'LeftLeg': 32, 'curl': 40}),
        (.7, STANCE)])),
    'stagger': (.75, tween([(0, STANCE), (.1, JOLT), (.35, {**SLUMP, 'Spine': (2, 0, 0), 'Head': (0, 0, 0)}), (.75, SLUMP)])),
    'death': (1.5, tween([(0, STANCE), (.12, JOLT), (.45, {**SLUMP, 'lift': (0, -.03, -.3), 'RightUpLeg': (40, 0, 0), 'LeftUpLeg': (44, 0, 0), 'RightLeg': 80, 'LeftLeg': 84}),
                          (.85, KNEEL),
                          (1.5, {**KNEEL, 'Spine': (-34, 0, 6), 'Spine1': (-16, 0, 0), 'Head': (-30, 0, 14), 'RightArm': (16, 40, 0), 'LeftArm': (16, 40, 0),
                                 'RightForeArm': 6, 'LeftForeArm': 6, 'curl': 6})])),
}
LOOPS = ('idle', 'walk', 'run', 'guard')


class Rig:
    def __init__(self, rig, unit):
        """unit: game metres per armature unit."""
        self.bones, self.unit = rig.data.bones, unit

    def rotation(self, name, value):
        bone = self.bones[name]
        m = bone.matrix_local.to_3x3().normalized()
        if isinstance(value, tuple):
            r = Euler([math.radians(v) for v in value]).to_matrix()
            if name.startswith('Left'):
                r = MIRROR @ r @ MIRROR
        else:
            d = FLIP @ (bone.tail_local - bone.head_local).normalized()
            toward = Vector((0, 1, 0)) if 'ForeArm' in name else Vector((0, -1, 0)) if name.endswith('Leg') else Vector((0, 0, -1))
            r = Matrix.Rotation(math.radians(value), 3, d.cross(toward).normalized())
        return (m.inverted() @ FLIP @ r @ FLIP @ m).to_quaternion()

    def bases(self, pose):
        """Bone name -> (rotation, location) from a pose."""
        out = {n: (self.rotation(n, v), None) for n, v in pose.items() if n in self.bones}
        hips = self.bones['Hips'].matrix_local.to_3x3().normalized()
        out['Hips'] = (out['Hips'][0], hips.inverted() @ FLIP @ Vector(pose['lift']) / self.unit)
        for side in ('Left', 'Right'):
            flex = pose[side + 'Leg']
            out[side + 'Foot'] = (self.rotation(side + 'Foot', (flex - pose[side + 'UpLeg'][0] - pose['Hips'][0], 0, 0)), None)
        for name in FINGERS:
            out[name] = (self.rotation(name, pose['curl']), None)
        return out


def key(rig, unit):
    """Key every clip on the armature as an action in its own muted NLA track, as the shared importer collects them."""
    import bpy
    solver = Rig(rig, unit)
    rig.animation_data_create()
    for name, (length, at) in CLIPS.items():
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        rig.animation_data.action = action
        last = {}
        for f in range(round(length * FPS) + 1):
            for bone, (q, loc) in solver.bases(at(f / FPS if name not in LOOPS or f < round(length * FPS) else 0)).items():
                pb = rig.pose.bones[bone]
                pb.rotation_mode = 'QUATERNION'
                if bone in last and last[bone].dot(q) < 0:
                    q = -q
                last[bone] = q
                pb.rotation_quaternion = q
                pb.keyframe_insert('rotation_quaternion', frame=f)
                if loc is not None:
                    pb.location = loc
                    pb.keyframe_insert('location', frame=f)
        for curve in action.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = 'LINEAR'
        track = rig.animation_data.nla_tracks.new()
        track.name = name
        track.strips.new(name, 0, action)
        track.mute = True
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion, pb.location = (1, 0, 0, 0), (0, 0, 0)


def height(rig):
    """The rest model's height in armature units, from its skinned meshes."""
    meshes = [o for o in rig.children if o.type == 'MESH']
    return max(v.co.z for o in meshes for v in o.data.vertices) - min(v.co.z for o in meshes for v in o.data.vertices)


VIEWS = {'front':(0, -1, .15), 'side': (-1, 0, .15), 'quarter': (-1, -1, .4)}


def preview():
    import bpy
    opts = {'out': str(ROOT / 'artifacts/patrol-clips'), 'only': ','.join(CLIPS), 'view': 'quarter'}
    for a in sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []:
        k, _, v = a.lstrip('-').partition('=')
        opts[k] = v
    out = Path(opts['out'])
    out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=opts.get('src', str(ROOT / 'assets/models/patrol.glb')))
    for obj in list(bpy.data.objects):
        if obj.type == 'MESH' and obj.parent is None:
            bpy.data.objects.remove(obj)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    rig.animation_data_clear()
    # The committed model carries the importer's scale on its root; the Tripo source (--src) is at its own size.
    unit = HEIGHT / height(rig) if 'src' in opts else rig.matrix_world.to_scale()[0]
    if 'src' in opts:
        rig.scale = (unit,) * 3
    key(rig, unit)
    if 'glb' in opts:
        bpy.ops.export_scene.gltf(filepath=str(out / 'patrol.glb'), export_format='GLB', export_animations=True,
                                  export_animation_mode='ACTIONS', export_apply=False)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light, scene.display.shading.color_type = 'STUDIO', 'TEXTURE'
    scene.render.resolution_x = scene.render.resolution_y = 400
    bpy.ops.mesh.primitive_plane_add(size=6)
    camera = bpy.data.objects.new('camera', bpy.data.cameras.new('camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type, camera.data.ortho_scale = 'ORTHO', 2.4
    d = Vector(VIEWS[opts['view']]).normalized()
    target = Vector((0, 0, .95))
    camera.location = target + d * 8
    camera.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    for name in opts['only'].split(','):
        length = CLIPS[name][0]
        rig.animation_data.action = bpy.data.actions[name]
        frames = sorted({round(i * length * FPS / 7) for i in range(8)})
        shots = []
        for f in frames:
            scene.frame_set(f)
            scene.render.filepath = str(out / f'.{name}-{f:03d}.png')
            bpy.ops.render.render(write_still=True)
            shots.append(scene.render.filepath)
        subprocess.run(['montage', *shots, '-tile', f'{len(shots)}x1', '-geometry', '+2+2', '-title', name, str(out / f'{name}-{opts["view"]}.png')], check=True)
        for s in shots:
            Path(s).unlink()
    print('PATROL_CLIPS_OK', out)


if __name__ == '__main__':
    preview()
