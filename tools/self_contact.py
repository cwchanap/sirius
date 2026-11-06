"""Keep a character's hands and forearms, and Mio's sword, out of the rest of its own body. Godot skins a mesh without self-collision, so a
hand that a clip swings into a satchel, a hip or the other arm, or a blade it swings through a thigh, goes straight through it, and only
the clip can keep it out.

blender --background --factory-startup --python tools/self_contact.py -- --model=mio [--clips=idle,stand] [--gen | --cleared]
Reports, per clip and side, the most vertices of a hand and forearm inside the body in any sampled frame and what they are inside, for the
committed clips; --gen for tools/cast_clips.py's poses as keyed now, before clear(), --cleared after it. --fix=idle,guard clears those
committed clips and swaps them back into the GLB (tools/glb_swap.py), for clips no tool here keys, such as Mio's Tripo idle. --max=N exits
1 when any clip measured has more than N vertices inside.

A vertex of one side's forearm or hand is inside when the nearest triangle of the rest of the body, within REACH, faces away from it. The
rest leaves out that side's own arm, which the elbow and wrist fold into themselves. clear() turns the upper arm, a degree at a time, the
way the surfaces it is inside face, until fewer than ALLOWED vertices are; resting a hand on an arm or a hip is contact, not passing
through. A model with a sword (meshes named Sword under the rig) has a third side: the blade against all of the body but the hand
gripping it, turned at the wrist. tools/cast_clips.py runs it on every clip it keys, tools/patrol_clips.py on the robot's, and
import_tripo_mio.py on her Tripo clips.
"""
import math
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
ROOT = Path(__file__).resolve().parents[1]
REACH = .035
# Depth that counts: Tripo's layered cloth leaves a few millimetres everywhere.
TOLERANCE = .006
# Vertices that may be inside: a sleeve resting on a sleeve, a palm on a hip.
ALLOWED = 12
# The bone each side turns.
JOINT = {'Right': 'RightArm', 'Left': 'LeftArm', 'Sword': 'RightHand'}
# Frames a turn eases in and out over (an eighth of a second at 24 fps).
SPREAD = 3


class Body:
    def __init__(self, body, rig, metres=1):
        """metres: game metres per world unit, 1 on a model at game scale; Tripo's sources are not."""
        self.body, self.rig, self.metres = body, rig, metres
        me = body.data
        me.calc_loop_triangles()
        tv = np.zeros(len(me.loop_triangles) * 3, np.int32)
        me.loop_triangles.foreach_get('vertices', tv)
        self.tv = tv.reshape(-1, 3)
        names = [g.name.removeprefix('mixamorig:') for g in body.vertex_groups]
        weights = np.zeros((len(me.vertices), len(names)))
        for v in me.vertices:
            for g in v.groups:
                weights[v.index, g.group] = g.weight
        self.dominant = np.array(names)[weights.argmax(1)]

        def weight(side, parts):
            return weights[:, [i for i, n in enumerate(names) if n.startswith(side) and n[len(side):].startswith(parts)]].sum(1)
        self.movers, self.others, self.faces = {}, {}, {}
        for side in ('Right', 'Left'):
            self.movers[side] = np.where(weight(side, ('ForeArm', 'Hand')) > .5)[0]
            own = weight(side, ('Shoulder', 'Arm', 'ForeArm', 'Hand')) > .2
            self.others[side] = np.where(~own[self.tv].any(1))[0]
            self.faces[side] = self.tv[self.others[side]].tolist()
        self.swords = [o for o in rig.children if o.type == 'MESH' and o.name.startswith('Sword')]
        if self.swords:
            grip = weight('Right', ('Hand',)) > .3
            self.others['Sword'] = np.where(~grip[self.tv].any(1))[0]
            self.faces['Sword'] = self.tv[self.others['Sword']].tolist()

    def sides(self):
        return ('Right', 'Left', 'Sword') if self.swords else ('Right', 'Left')

    def points(self, obj=None):
        import bpy
        obj = obj or self.body
        depsgraph = bpy.context.evaluated_depsgraph_get()
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        co = np.zeros(len(mesh.vertices) * 3)
        mesh.vertices.foreach_get('co', co)
        evaluated.to_mesh_clear()
        m = np.array(obj.matrix_world)
        return (co.reshape(-1, 3) @ m[:3, :3].T + m[:3, 3]) * self.metres

    def blade(self):
        return np.concatenate([self.points(o) for o in self.swords])

    def contact(self, side, co=None):
        """(vertices inside, the way out summed over them, the parts they are inside by count) for one side in the current pose."""
        from mathutils import Vector
        from mathutils.bvhtree import BVHTree
        co = self.points() if co is None else co
        tris = self.others[side]
        tree = BVHTree.FromPolygons(co.tolist(), self.faces[side], all_triangles=True)
        count, push, parts = 0, np.zeros(3), {}
        for p in (self.blade() if side == 'Sword' else co[self.movers[side]]).tolist():
            loc, normal, index, dist = tree.find_nearest(p, REACH)
            if loc is not None and dist > TOLERANCE and (Vector(p) - loc).dot(normal) < 0:
                count += 1
                push += np.array(normal) * dist
                part = self.dominant[self.tv[tris[index]][0]]
                parts[part] = parts.get(part, 0) + 1
        return count, push, parts


def pose(rig, basis):
    import bpy
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.matrix_basis = basis[pb.name]
    bpy.context.view_layer.update()


def clear(body, rig, frames, limit=40, metres=1):
    """Turn each frame's upper arms (and the sword's wrist) out of the body, at most limit steps, keeping the turn that left the fewest
    vertices inside (where an arm lies between the chest and the other arm, the way out swings from side to side). frames: a clip's
    per-frame {bone: basis matrix}, changed in place. Returns the largest turn in degrees."""
    import bpy
    from mathutils import Quaternion, Vector
    measure = Body(body, rig, metres)
    n = len(frames)
    solved = {side: [] for side in measure.sides()}
    to_armature = rig.matrix_world.inverted()

    def lever(side, joint):
        """From the turning joint to what it swings: the hand, or the middle of the blade."""
        if side == 'Sword':
            return to_armature @ Vector(measure.blade().mean(0) / metres) - joint.head
        return rig.pose.bones[side + 'Hand'].head - joint.head
    for f in range(n):
        pose(rig, frames[f])
        for side in solved:
            arm = rig.pose.bones[JOINT[side]]
            start = arm.rotation_quaternion.copy()
            total, best, way = Quaternion(), None, None
            for _ in range(limit):
                count, push, _ = measure.contact(side)
                if best is None or count < best[0]:
                    best = (count, total.copy())
                if count < ALLOWED or not push.any():
                    break
                out = (to_armature.to_3x3() @ Vector(push)).normalized()
                way = out if way is None else (way + out).normalized()
                axis = lever(side, arm).cross(way)
                if axis.length < 1e-6:
                    break
                # About an armature-space axis through the shoulder: basis @ q, with q's axis in the bone's own frame.
                q = Quaternion((arm.matrix.to_3x3().inverted() @ axis).normalized(), math.radians(1.5))
                arm.rotation_quaternion = arm.rotation_quaternion @ q
                total = total @ q
                bpy.context.view_layer.update()
            if best[0] >= ALLOWED:
                # Inside something with walls on every side (a satchel), the way out swings about: try swinging the arm straight out to
                # the side, away from the hips, instead.
                arm.rotation_quaternion = start
                bpy.context.view_layer.update()
                hips = rig.pose.bones['Hips'].head
                hand = arm.head + lever(side, arm)
                up = to_armature.to_3x3() @ Vector((0, 0, 1))
                away = (hand - hips) - (hand - hips).project(up)
                axis = lever(side, arm).cross(away)
                if axis.length > 1e-6:
                    local = (arm.matrix.to_3x3().inverted() @ axis).normalized()
                    for degrees in range(2, 32, 2):
                        q = Quaternion(local, math.radians(degrees))
                        arm.rotation_quaternion = start @ q
                        bpy.context.view_layer.update()
                        count = measure.contact(side)[0]
                        if count < best[0]:
                            best = (count, q)
                        if count < ALLOWED:
                            break
            arm.rotation_quaternion = start @ best[1]
            bpy.context.view_layer.update()
            solved[side].append(best)
    # A clip whose last frame is its first loops: a turn near its end spreads over its start.
    loop = n > 2 and all(frames[0][b].to_quaternion().rotation_difference(frames[-1][b].to_quaternion()).angle < 1e-3 and
                         (frames[0][b].translation - frames[-1][b].translation).length < 1e-4 for b in frames[0])
    worst = 0
    for h in range(n):
        for side, made in solved.items():
            # A turn solved on one frame alone would flick the arm or blade for that frame: each eases in and out over SPREAD frames
            # either side, and a frame takes the largest that reaches it, unless that leaves more inside than its own.
            base, best = frames[h][JOINT[side]], Quaternion()
            for f in range(h - SPREAD, h + SPREAD + 1):
                if loop or 0 <= f < n:
                    u = 1 - abs(h - f) / (SPREAD + 1)
                    q = Quaternion().slerp(made[f % (n - 1) if loop else f][1], u * u * (3 - 2 * u))
                    if q.angle > best.angle:
                        best = q
            frames[h][JOINT[side]] = base @ best.to_matrix().to_4x4()
            if best.rotation_difference(made[h][1]).angle > 1e-4:
                pose(rig, frames[h])
                count = measure.contact(side)[0]
                if count >= ALLOWED and count > made[h][0]:
                    best = made[h][1]
                    frames[h][JOINT[side]] = base @ best.to_matrix().to_4x4()
            worst = max(worst, math.degrees(best.angle))
    return worst


def fcurves(action):
    return action.fcurves if hasattr(action, 'fcurves') and len(action.fcurves) else [c for layer in action.layers for strip in layer.strips for bag in strip.channelbags for c in bag.fcurves]


def sample(rig, action):
    """A committed clip's first keyed frame and per-frame bases up to its last."""
    import bpy
    rig.animation_data.action = action
    first, last = map(int, action.frame_range)
    out = []
    for f in range(first, last + 1):
        bpy.context.scene.frame_set(f)
        out.append({pb.name: pb.matrix_basis.copy() for pb in rig.pose.bones})
    rig.animation_data.action = None
    return first, out


def load(model):
    import bpy
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / f'assets/models/{model}.glb'))
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    body = max((o for o in bpy.data.objects if o.type == 'MESH' and o.find_armature() == rig), key=lambda o: len(o.data.polygons))
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    rig.animation_data.action = None
    return rig, body


def action(name):
    """The imported clip: the glTF importer names it after the armature too (terminal_Mio), and terminal_hold_Mio is not terminal."""
    import bpy
    return next(a for a in bpy.data.actions if name in (a.name, a.name.rsplit('_', 1)[0]))


def fix(model, names):
    """Clear committed clips and swap them back into the model's GLB, keyed from the same first frame."""
    import bpy
    import tempfile
    import glb_swap
    rig, body = load(model)
    made = []
    for name in names:
        first, frames = sample(rig, action(name))
        turned = clear(body, rig, frames)
        new = bpy.data.actions.new(name + '_cleared')
        rig.animation_data.action = new
        for t, basis in enumerate(frames, first):
            for pb in rig.pose.bones:
                pb.rotation_mode = 'QUATERNION'
                pb.matrix_basis = basis[pb.name]
                for channel in ('rotation_quaternion', 'location', 'scale'):
                    pb.keyframe_insert(channel, frame=t)
        rig.animation_data.action = None
        made.append((name, new))
        print('SELF_CONTACT_FIX', model, name, f'{turned:.1f} degrees at most', flush=True)
    for track in list(rig.animation_data.nla_tracks):
        rig.animation_data.nla_tracks.remove(track)
    for name, new in made:
        track = rig.animation_data.nla_tracks.new()
        track.name = name
        track.strips.new(name, int(new.frame_range[0]), new)
        track.mute = True
    donor = Path(tempfile.mkdtemp()) / f'{model}.glb'
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(donor), export_format='GLB', use_selection=True, export_animations=True,
                              export_animation_mode='NLA_TRACKS', export_apply=False)
    path = ROOT / f'assets/models/{model}.glb'
    path.write_bytes(glb_swap.swap_animations(path.read_bytes(), donor.read_bytes(), names))


def report(model, names, mode):
    """Print each clip's worst sampled frame per side; returns the most vertices inside in any of them."""
    rig, body = load(model)
    measure = Body(body, rig)
    if mode:
        import cast_clips
        clips = cast_clips.poses(rig, rig.matrix_world.to_scale()[0], {n: cast_clips.CLIPS[model][n] for n in names}, cast_clips.LOOPS[model],
                                 cast_clips.grip(model), cast_clips.HEIGHTS[model], body if mode == 'cleared' else None)
    else:
        clips = {n: sample(rig, action(n))[1] for n in names}
    most = 0
    for name in names:
        frames = clips[name]
        worst = {}
        for f in range(0, len(frames), max(1, len(frames) // 12)):
            pose(rig, frames[f])
            co = measure.points()
            for side in measure.sides():
                count, _, parts = measure.contact(side, co)
                if count > worst.get(side, (0,))[0]:
                    worst[side] = (count, f, parts)
        most = max([most] + [w[0] for w in worst.values()])
        print('SELF_CONTACT', model, name, ' '.join(f'{s}={c}@{f}({",".join(f"{k}:{n}" for k, n in sorted(p.items(), key=lambda x: -x[1])[:2])})'
                                                     for s, (c, f, p) in sorted(worst.items())) or 'clear', flush=True)
    return most


def main():
    import glb_swap
    opts = {'model': 'mio'}
    for a in sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []:
        k, _, v = a.lstrip('-').partition('=')
        opts[k] = v
    model = opts['model']
    if 'fix' in opts:
        fix(model, opts['fix'].split(','))
        return
    mode = 'cleared' if 'cleared' in opts else 'gen' if 'gen' in opts else None
    if 'clips' in opts:
        names = opts['clips'].split(',')
    elif mode:
        import cast_clips
        names = list(cast_clips.CLIPS[model])
    else:
        names = [a['name'] for a in glb_swap.load((ROOT / f'assets/models/{model}.glb').read_bytes())[0]['animations']]
    most = report(model, names, mode)
    if 'max' in opts and most > int(opts['max']):
        print('SELF_CONTACT_FAIL', model, most, 'vertices inside, over', opts['max'], flush=True)
        sys.exit(1)


if __name__ == '__main__':
    main()
