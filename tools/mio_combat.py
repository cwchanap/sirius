"""Mio's sword combo and Resonance Cut, keyed procedurally on the Tripo rig.

import_tripo_mio.py calls clips(); tools/preview_mio_combat.py renders them in background Blender.
Every channel is in Mio's own frame, metres and degrees: x to her right, y forward, z up, the origin
on the ground under her hips. The right wrist follows a keyed path, the blade a keyed direction with
its edge leading the swing, and two-bone IK places the arms and legs; hips and spine are turned
directly. The three attacks are one performance cut at its link poses: each follow-through is the
next swing's windup, so an attack chained at Player.cs's link time starts on the pose it left.
Feet are keyed where they stand on the ground; Player.cs moves Mio forward by LUNGE, so a planted
foot slides back in her frame by as much and stays put in the world.
"""
import math
import numpy as np
from mathutils import Matrix, Quaternion, Vector

FPS = 24
# Seconds: (length, lunge metres, lunge start, lunge end). Player.cs repeats these.
LUNGE = {'attack1': (.55, .45, .04, .2), 'attack2': (.55, .5, .02, .15), 'attack3': (.85, 1.2, .06, .3), 'skill': (1.3, 0, 0, 0)}
STANCE = {
    'hips': (0, 0, .03), 'yaw': -15, 'pitch': 4, 'roll': 0, 'twist': 10, 'bend': 0, 'side': 0, 'spin': 0,
    'hand': (.24, .3, 1.02), 'blade': (-.15, .85, .5), 'lhand': (-.24, .08, .98),
    'lfoot': (-.2, .14, 0), 'rfoot': (.2, -.12, 0), 'lheel': 0, 'rheel': 0,
}
UPPER = ('hand', 'blade', 'lhand', 'yaw', 'twist', 'hips', 'pitch')
# Each clip: (time, channels keyed then) from its start pose. A channel eases between its own keys;
# 'hold' keys every channel where it stands.
CLIPS = {
    'attack1': [
        (.1, {'hand': (.32, -.04, 1.62), 'blade': (.25, -.55, .8), 'lhand': (-.12, .38, 1.2), 'yaw': -40, 'twist': -20,
              'hips': (0, -.02, 0), 'pitch': -4}),
        (.13, {'rfoot': (.19, .2, .1)}),
        (.17, {'hand': (.06, .52, 1.24), 'blade': (-.4, .85, .1), 'yaw': 0, 'twist': 12, 'hips': (0, .04, -.05), 'pitch': 12,
               'rfoot': (.19, .55, 0)}),
        (.26, {'hand': (-.3, .26, .8), 'blade': (-.55, -.15, -.6), 'lhand': (-.3, -.16, 1.0), 'yaw': 35, 'twist': 25,
               'hips': (0, .02, -.07), 'pitch': 15}),
        (.3, 'hold'),
        (.55, {k: STANCE[k] for k in UPPER}),
    ],
    'attack2': [
        (.05, {'hand': (-.38, .12, .78), 'blade': (-.5, -.45, -.55), 'yaw': 45, 'twist': 30}),
        (.07, {'lfoot': (-.2, .2, .12)}),
        (.11, {'hand': (.02, .54, 1.28), 'blade': (.35, .8, .3), 'lhand': (-.2, .3, 1.15), 'yaw': 0, 'twist': 0,
               'hips': (0, .04, -.03), 'pitch': 8, 'lfoot': (-.18, .62, 0)}),
        (.2, {'hand': (.4, .2, 1.72), 'blade': (.35, -.2, .9), 'lhand': (-.32, .05, 1.05), 'yaw': -35, 'twist': -25,
              'hips': (0, 0, .02), 'pitch': -5}),
        (.26, {'hand': (.26, -.02, 1.78), 'blade': (.1, -.55, .83), 'lhand': (-.25, .25, 1.2), 'yaw': -20, 'twist': -10, 'pitch': -6}),
        (.55, {k: STANCE[k] for k in UPPER}),
    ],
    'attack3': [
        (.08, {'hips': (0, 0, -.1), 'hand': (.2, 0, 1.85), 'lhand': 'hilt', 'pitch': 6}),
        (.12, {'lfoot': (-.18, .3, .1), 'rfoot': (.2, -.1, .15)}),
        (.2, {'hips': (0, .05, .3), 'hand': (.12, -.08, 2.1), 'blade': (0, -.85, .3), 'yaw': 0, 'twist': 0, 'pitch': -15,
              'lfoot': (-.18, .9, .3), 'rfoot': (.2, .8, .38)}),
        (.25, {'hand': (.06, .3, 1.95), 'blade': (0, .35, .94)}),
        (.3, {'hips': (0, .08, -.24), 'hand': (.02, .55, .55), 'blade': (0, .75, -.66), 'pitch': 25,
              'lfoot': (-.2, .7, 0), 'rfoot': (.2, 1.45, 0), 'lheel': 40}),
        (.36, {'hips': (0, .08, -.28)}),
        (.45, {'hips': (0, .06, -.25)}),
        (.85, {**{k: STANCE[k] for k in UPPER}, 'lheel': 0}),
    ],
    'skill': [
        (.3, {'hips': (0, -.05, -.18), 'yaw': -50, 'twist': -30, 'pitch': 10, 'hand': (.45, -.35, .8), 'blade': (.35, -.8, -.3),
              'lhand': (-.3, .45, 1.25), 'lfoot': (-.32, .2, 0), 'rfoot': (.3, -.25, 0)}),
        (.36, {'hips': (0, -.05, -.22)}),
        (.38, {'spin': 0}),
        (.44, {'hand': (.62, .15, 1.3), 'blade': (.95, .25, 0), 'lhand': (-.1, .2, 1.3), 'yaw': -10, 'twist': 0, 'pitch': 0}),
        (.52, {'hips': (0, 0, .42), 'lfoot': (-.12, .05, .35), 'rfoot': (.12, -.1, .4)}),
        (.62, {'hips': (0, 0, .4)}),
        (.66, {'hand': (.62, .15, 1.3), 'blade': (.95, .25, 0)}),
        (.7, {'spin': 720, 'yaw': 0, 'hand': (.1, .1, 2.0), 'blade': (0, -.5, .85), 'lhand': 'hilt', 'hips': (0, 0, .1)}),
        (.74, {'hand': (.05, .4, 1.75), 'blade': (0, .6, .8)}),
        (.78, {'hips': (0, .05, -.34), 'pitch': 20, 'hand': (0, .55, .5), 'blade': (0, .42, -.91),
               'lfoot': (-.18, .3, 0), 'rfoot': (.18, -.45, .06), 'rheel': 50}),
        (1.0, {'hips': (0, .05, -.32)}),
        (1.3, {**{k: STANCE[k] for k in UPPER + ('lfoot', 'rfoot')}, 'rheel': 0}),
    ],
}
# attack2 starts where attack1 links, attack3 where attack2 links: the link is the last key before the tail.
CHAIN = (('attack1', 'attack2'), ('attack2', 'attack3'))
LINK = {'attack1': .3, 'attack2': .26}
# Seconds the sword trail shows, as Player.cs times it: around each hit, and through the skill's spin to its slam.
TRAIL = {'attack1': (.08, .29), 'attack2': (.02, .23), 'attack3': (.21, .42), 'skill': (.38, .82)}


def tracks(name, start):
    """Per channel, the (time, value) keys of a clip, its first key the start pose."""
    keys = {c: [(0, v)] for c, v in start.items()}
    for t, change in CLIPS[name]:
        for c, v in (change if change != 'hold' else {c: k[-1][1] for c, k in keys.items()}).items():
            keys[c].append((t, v))
    return keys


def sample(keys, t):
    """Catmull-Rom through the keys, flat at a key where a component turns, so holds and extremes stop."""
    if t <= keys[0][0]:
        return keys[0][1]
    if t >= keys[-1][0]:
        return keys[-1][1]
    i = max(k for k in range(len(keys) - 1) if keys[k][0] <= t)
    (t0, a), (t1, b) = keys[i], keys[i + 1]
    if isinstance(a, str) or isinstance(b, str):
        return a if t - t0 < t1 - t else b
    vec = isinstance(a, tuple)

    def value(j):
        v = keys[j][1]
        return list(v) if vec else [v]

    def tangent(j):
        p = value(j)
        if j <= 0 or j >= len(keys) - 1 or isinstance(keys[j - 1][1], str) or isinstance(keys[j + 1][1], str):
            return [0] * len(p)
        prev, nxt = value(j - 1), value(j + 1)
        span = keys[j + 1][0] - keys[j - 1][0]
        return [0 if (p[k] - prev[k]) * (nxt[k] - p[k]) <= 0 else (nxt[k] - prev[k]) / span for k in range(len(p))]
    h = t1 - t0
    s = (t - t0) / h
    p0, p1, m0, m1 = value(i), value(i + 1), tangent(i), tangent(i + 1)
    v = [(2 * s ** 3 - 3 * s ** 2 + 1) * p0[k] + (s ** 3 - 2 * s ** 2 + s) * m0[k] * h + (-2 * s ** 3 + 3 * s ** 2) * p1[k] + (s ** 3 - s ** 2) * m1[k] * h for k in range(len(p0))]
    return tuple(v) if vec else v[0]


def lunge(name, t):
    _, distance, t0, t1 = LUNGE[name]
    if distance == 0:
        return 0
    u = min(1, max(0, (t - t0) / (t1 - t0)))
    return distance * u * u * (3 - 2 * u)


class Rig:
    """Two-bone IK and forward kinematics over the armature's rest matrices, no depsgraph involved."""
    def __init__(self, rig, ready, blade):
        self.bones = {b.name: b for b in rig.data.bones}
        self.order = [b.name for b in rig.data.bones if b.parent is None]
        for name in self.order:
            self.order += [c.name for c in self.bones[name].children]
        self.rest = {n: b.matrix_local.copy() for n, b in self.bones.items()}
        self.rel = {n: (self.rest[b.parent.name].inverted() @ self.rest[n] if b.parent else self.rest[n]) for n, b in self.bones.items()}
        self.ready = ready
        world = rig.matrix_world
        self.unit = 1 / world.to_scale()[0]
        self.world = world
        # Her frame in armature space: she faces -Y with her right hand on -X.
        self.right, self.forward, self.up = Vector((-1, 0, 0)), Vector((0, -1, 0)), Vector((0, 0, 1))
        pose = self.fk(ready)
        self.ready_pose = pose
        self.blade = blade
        self.length = {n: (self.rest[n].translation - self.rest[self.bones[n].parent.name].translation).length for n in self.bones if self.bones[n].parent}
        # The elbow and knee bend about these axes in each bone's own frame, read from the ready pose.
        self.hinge = {}
        for upper, lower, side in (('RightArm', 'RightForeArm', 1), ('LeftArm', 'LeftForeArm', 1), ('RightUpLeg', 'RightLeg', 1), ('LeftUpLeg', 'LeftLeg', 1)):
            a, b = pose[upper].col[1].to_3d(), pose[lower].col[1].to_3d()
            axis = a.cross(b)
            if axis.length < 1e-3:
                axis = (self.right if 'Leg' in upper else self.forward).copy()
            axis.normalize()
            self.hinge[upper] = pose[upper].to_3x3().inverted() @ axis
            self.hinge[lower] = pose[lower].to_3x3().inverted() @ axis
        self.ground = self.point((0, 0, 0))
        self.ankle = {s: (pose[s + 'Foot'].translation - self.ground).dot(self.up) for s in ('Left', 'Right')}

    def point(self, p):
        """Her frame (metres) to armature space."""
        world = Vector((-p[0], -p[1], p[2]))
        return self.world.inverted() @ world

    def direction(self, d):
        return (self.right * d[0] + self.forward * d[1] + self.up * d[2]).normalized()

    def fk(self, basis):
        pose = {}
        for n in self.order:
            parent = self.bones[n].parent
            pose[n] = (pose[parent.name] if parent else Matrix.Identity(4)) @ self.rel[n] @ basis[n]
        return pose

    def place(self, basis, pose, name, matrix):
        """Set a bone's basis so its armature-space pose is matrix, under its parent's current pose."""
        parent = self.bones[name].parent
        above = pose[parent.name] if parent else Matrix.Identity(4)
        basis[name] = self.rel[name].inverted() @ above.inverted() @ matrix
        pose[name] = matrix

    def turn(self, basis, pose, name, rotation):
        m = pose[name]
        self.place(basis, pose, name, Matrix.Translation(m.translation) @ (rotation @ m.to_quaternion()).to_matrix().to_4x4())

    def frame(self, y, axis):
        """A rotation taking a bone's local Y to y and its local hinge axis to (the part of) axis normal to y."""
        y = y.normalized()
        x = (axis - axis.dot(y) * y).normalized()
        return Matrix((x, y, x.cross(y))).transposed()

    def reach(self, basis, pose, upper, lower, end, target, pole, hand=None, twist=.6):
        s = pose[upper].translation
        a, b = self.length[lower], self.length[end]
        to = target - s
        d = min(max(to.length, abs(a - b) + 1e-4), (a + b) * .999)
        u = to.normalized()
        v = pole - pole.dot(u) * u
        v = v.normalized() if v.length > 1e-6 else self.up.cross(u).normalized()
        cos = (a * a + d * d - b * b) / (2 * a * d)
        elbow = s + a * (cos * u + math.sqrt(max(0, 1 - cos * cos)) * v)
        wrist = s + d * u
        axis = (elbow - s).cross(wrist - elbow)
        if axis.length < 1e-6:
            axis = u.cross(v)
        axis.normalize()

        def oriented(name, y):
            local = self.frame(Vector((0, 1, 0)), self.hinge[name])
            return self.frame(y, axis) @ local.transposed()
        top = oriented(upper, elbow - s)
        self.place(basis, pose, upper, Matrix.Translation(s) @ top.to_4x4())
        fore = oriented(lower, wrist - elbow)
        if hand is not None:
            # Share the wrist's roll with the forearm so the skin twists along it, not at the wrist alone.
            y = (wrist - elbow).normalized()

            def roll(m, r):
                x = m.col[0] - m.col[0].dot(y) * y
                ref = r.col[0] - r.col[0].dot(y) * y
                return math.atan2(ref.cross(x).dot(y), ref.dot(x))
            rest = self.rest[lower].to_3x3().inverted() @ self.rest[end].to_3x3()
            angle = roll(hand, fore @ rest)
            fore = Quaternion(y, angle * twist).to_matrix() @ fore
        self.place(basis, pose, lower, Matrix.Translation(elbow) @ fore.to_4x4())
        self.place(basis, pose, end, Matrix.Translation(wrist) @ (hand if hand is not None else pose[lower].to_3x3() @ self.rel[end].to_3x3() @ basis[end].to_3x3()).to_4x4())
        for child in self.bones[end].children_recursive:
            parent = child.parent.name
            pose[child.name] = pose[parent] @ self.rel[child.name] @ basis[child.name]

    def grip(self, blade, edge):
        """The hand rotation that points the sword along blade with its edge toward edge."""
        b_local, e_local = self.blade
        e = (edge - edge.dot(blade) * blade).normalized()
        world = Matrix((blade, e, blade.cross(e))).transposed()
        local = Matrix((b_local, e_local, b_local.cross(e_local))).transposed()
        return world @ local.transposed()

    def solve(self, c, edge):
        basis = {n: m.copy() for n, m in self.ready.items()}
        pose = self.fk(basis)
        spin = Quaternion(self.up, math.radians(c['spin']))

        def spun(p):
            return spin @ self.direction(p)
        hip = self.ready_pose['Hips']
        lean = Quaternion(self.up, math.radians(c['yaw'])) @ Quaternion(self.right, -math.radians(c['pitch'])) @ Quaternion(self.forward, math.radians(c['roll']))
        offset = (self.right * c['hips'][0] + self.forward * c['hips'][1] + self.up * c['hips'][2]) * self.unit
        self.place(basis, pose, 'Hips', Matrix.Translation(hip.translation + spin @ offset) @ ((spin @ lean) @ hip.to_quaternion()).to_matrix().to_4x4())
        pose = self.fk(basis)
        torso = spin @ lean
        for name in ('Spine', 'Spine1', 'Spine2'):
            r = Quaternion(torso @ self.up, math.radians(c['twist'] / 3)) @ Quaternion(torso @ self.right, -math.radians(c['bend'] / 3)) @ Quaternion(torso @ self.forward, math.radians(c['side'] / 3))
            self.turn(basis, pose, name, r)
            pose = self.fk(basis)
        # Keep her eyes near the way she faces: the neck and head take back most of the torso's yaw.
        back = Quaternion(self.up, -math.radians(c['yaw'] + c['twist']) * .35)
        for name in ('Neck', 'Head'):
            self.turn(basis, pose, name, back)
            pose = self.fk(basis)
        origin = self.point((0, 0, 0))

        def at(p):
            return origin + spin @ (self.point(p) - origin)
        blade = spun(c['blade'])
        hand = self.grip(blade, edge)
        wrist = at(c['hand'])
        pole = spun((.6, -.4, -.6))
        self.reach(basis, pose, 'RightArm', 'RightForeArm', 'RightHand', wrist, pole, hand)
        if c['lhand'] == 'hilt':
            left = wrist - blade * .1 * self.unit
        else:
            left = at(c['lhand'])
        basis['LeftHand'] = Matrix.Identity(4)
        self.reach(basis, pose, 'LeftArm', 'LeftForeArm', 'LeftHand', left, spun((-.6, -.4, -.6)), None)
        for side, foot, heel in (('Left', c['lfoot'], c['lheel']), ('Right', c['rfoot'], c['rheel'])):
            # A raised heel pivots the foot on its ball, which lifts the ankle.
            ankle = at((foot[0], foot[1], 0)) + self.up * (self.ankle[side] + (foot[2] + .13 * math.sin(math.radians(heel))) * self.unit)
            flat = self.ready_pose[side + 'Foot'].to_quaternion()
            rot = (spin @ Quaternion(spin @ self.right, -math.radians(heel)) @ flat).to_matrix()
            knee = spun((.15 if side == 'Right' else -.15, 1, 0))
            self.reach(basis, pose, side + 'UpLeg', side + 'Leg', side + 'Foot', ankle, knee, rot, twist=0)
        return basis


def channels(name, t, start):
    keys = tracks(name, start)
    return {k: sample(v, t) for k, v in keys.items()}


def sword(rig, steel):
    """The blade, hilt to tip, and its edge toward the knuckles, both in the hand's frame, then the hilt and tip,
    from the blade's vertices in armature space with the hand at rest."""
    hand = rig.data.bones['RightHand'].matrix_local
    tip = max(steel, key=lambda p: (p - hand.translation).length)
    hilt = max(steel, key=lambda p: (p - tip).length)
    centre = sum(steel, Vector()) / len(steel)
    rot = hand.to_3x3().inverted()
    blade = (rot @ (tip - hilt)).normalized()
    edge = rot @ Vector(np.linalg.svd(np.array([p - centre for p in steel]))[2][1])
    edge = (edge - edge.dot(blade) * blade).normalized()
    return blade, edge if edge.y > 0 else -edge, hilt, tip


def time(f):
    """The importer keys pose f at frame f + 1, which the NLA export puts at (f + 1) / 24 s; Player.cs times from 0."""
    return (f + 1) / FPS


def clips(rig, ready, blade, grip):
    """name -> per-frame bone bases, from the ready pose's bases, the hand-local blade and edge axes and the grip."""
    solver = Rig(rig, ready, blade)
    out = {}
    starts = {'attack1': dict(STANCE), 'skill': dict(STANCE)}
    for name in ('attack1', 'attack2', 'attack3', 'skill'):
        start = starts[name]
        length = LUNGE[name][0]
        frames = round(length * FPS)
        values = [channels(name, time(f), start) for f in range(frames)]
        for f, v in enumerate(values):
            # Feet are keyed on the ground; Mio has moved forward by the lunge since the clip began.
            slide = lunge(name, time(f))
            for foot in ('lfoot', 'rfoot'):
                v[foot] = (v[foot][0], v[foot][1] - slide, v[foot][2])
        tips = [solver.direction(v['blade']) for v in values]
        hands = [solver.point(v['hand']) for v in values]
        poses = []
        edge = None
        for f, v in enumerate(values):
            # The edge leads: it faces the way the blade's tip is travelling, held while the blade rests.
            a, b = max(0, f - 1), min(frames - 1, f + 1)
            spin = [Quaternion(solver.up, math.radians(values[i]['spin'])) for i in (a, b)]
            move = (hands[b] + spin[1] @ tips[b] * .9 * solver.unit) - (hands[a] + spin[0] @ tips[a] * .9 * solver.unit)
            if move.length > .05 * solver.unit:
                edge = move.normalized()
            elif edge is None:
                edge = solver.forward.copy()
            basis = solver.solve(v, edge)
            basis.update({n: m.copy() for n, m in grip.items()})
            poses.append(basis)
        out[name] = poses
        for a, b in CHAIN:
            if a == name:
                link = channels(name, LINK[name], start)
                slide = lunge(name, LINK[name])
                for foot in ('lfoot', 'rfoot'):
                    link[foot] = (link[foot][0], link[foot][1] - slide, link[foot][2])
                starts[b] = link
    return out
