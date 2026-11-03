"""The cast's acting clips for the human Tripo rigs (Mixamo bones, prefix stripped), as Game.Cue() plays them from Prologue.Stage:
Mio's waking in her room and her wrist terminal, Yui's and Renji's gestures. A pose turns bones as patrol_clips.py's do: degrees about
the character's own axes as it stands in the rest pose, x to its right, y forward, z up; +x swings a hanging limb forward and tips an
upright bone back, +y lowers an arm toward the body, +z turns to the character's left, left bones mirrored. 'lift' moves the hips in
metres at game scale; a 'Foot' angle adds to the one that keeps the sole level. 'curl' bends the fingers, the index least and the pinky
most, unless a side gives its own (RightCurl, LeftCurl: one angle, or four from index to pinky).

A pose may place a hand instead of turning the arm: RightReach (or LeftReach) is the wrist from the shoulder in the chest's frame, in
character heights, x away from the body, and two-bone IK bends the elbow toward RightElbow; RightRoll turns the hand about the forearm,
palm up for positive, shared with the forearm so the skin twists along it. Between a key with a Reach and one without, the
wrist travels in a line from where the angles put it.

People don't move like a machine stopping at every key. A clip runs each channel through its keys on one smooth curve, at rest only where
it turns back or holds, and each joint trails the one it hangs from (LAG): the chest after the hips, the head after the chest, the
wrist and fingers after the arm. A held gesture ends in a pose that its <name>_hold loop keeps alive with breathing and a little sway,
which ActorVisual queues after it, so a character holding out a letter or reading the terminal doesn't freeze.

import_tripo_mio.py keys MIO on her source, import_tripo_cast.py keys YUI and RENJI on theirs. Run on its own, this renders contact
sheets from the committed model (or a prefix-stripped GLB of the source with --src), so the poses can be tuned without the source, and
--swap keys them on the committed model as its importer would and swaps them into its GLB (tools/glb_swap.py), leaving the rest as built:
xvfb-run -a blender --background --factory-startup --python tools/cast_clips.py -- --model=mio [--src=GLB] [--only=wake,rise] [--view=side] [--swap]
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from patrol_clips import FLIP, FPS, MIRROR, ROOT, Rig, gait  # noqa: E402

# The importers' --height, and Mio's hip joint over the ground at rest: her seated and lying lifts are measured from it.
HEIGHTS = {'mio': 1.84, 'yui': 1.6, 'renji': 1.78}
MIO_HIP = 1.075
STANCE = {
    'lift': (0, 0, 0), 'Hips': (0, 0, 0), 'Spine': (0, 0, 0), 'Spine1': (0, 0, 0), 'Spine2': (0, 0, 0), 'Neck': (0, 0, 0), 'Head': (0, 0, 0),
    'RightArm': (2, 10, 0), 'LeftArm': (2, 10, 0), 'RightForeArm': 14, 'LeftForeArm': 14, 'RightHand': (0, 0, 0), 'LeftHand': (0, 0, 0),
    'RightUpLeg': (0, 0, 0), 'LeftUpLeg': (0, 0, 0), 'RightLeg': 2, 'LeftLeg': 2, 'RightFoot': (0, 0, 0), 'LeftFoot': (0, 0, 0), 'curl': 22,
}
# The elbow's pull, as RightElbow: out, back and down.
ELBOW = (.4, -.3, -1)
# Seconds each channel trails its keys.
LAG = {'Spine1': .03, 'Spine2': .05, 'Neck': .07, 'Head': .1}
# The breathing and sway a loop adds: degrees the chest rises, and how much the head, arms and hands drift.
IDLE = dict(breath=1.6, sway=1)
HOLD = dict(breath=1.2, sway=.6)


def lag(channel):
    return LAG.get(channel, .05 if channel.endswith('ForeArm') else .1 if channel.endswith(('Hand', 'Roll')) else .14 if channel.endswith(('curl', 'Curl')) else 0)


def pose(base, **bones):
    return {**base, **bones}


def sample(keys, t, loop=None):
    """Catmull-Rom through (time, value) keys, flat at a key where a component turns back, so holds and extremes rest; a loop wraps."""
    if loop:
        t %= loop
        keys = [(keys[-2][0] - loop, keys[-2][1])] + keys + [(keys[1][0] + loop, keys[1][1])] if len(keys) > 2 else keys
    if t <= keys[0][0] or len(keys) == 1:
        return keys[0][1]
    if t >= keys[-1][0]:
        return keys[-1][1]
    i = max(k for k in range(len(keys) - 1) if keys[k][0] <= t)
    vec = isinstance(keys[0][1], tuple)

    def value(j):
        return list(keys[j][1]) if vec else [keys[j][1]]

    def tangent(j):
        p = value(j)
        if j <= 0 or j >= len(keys) - 1:
            return [0] * len(p)
        a, b, span = value(j - 1), value(j + 1), keys[j + 1][0] - keys[j - 1][0]
        return [0 if (p[k] - a[k]) * (b[k] - p[k]) <= 0 else (b[k] - a[k]) / span for k in range(len(p))]
    (t0, _), (t1, _) = keys[i], keys[i + 1]
    h, s = t1 - t0, (t - t0) / (t1 - t0)
    p0, p1, m0, m1 = value(i), value(i + 1), tangent(i), tangent(i + 1)
    v = [(2 * s ** 3 - 3 * s ** 2 + 1) * p0[k] + (s ** 3 - 2 * s ** 2 + s) * m0[k] * h + (-2 * s ** 3 + 3 * s ** 2) * p1[k] + (s ** 3 - s ** 2) * m1[k] * h
         for k in range(len(p0))]
    return tuple(v) if vec else v[0]


def add(p, channel, delta):
    value = p.get(channel, 0 if not isinstance(delta, tuple) else (0,) * len(delta))
    p[channel] = tuple(a + b for a, b in zip(value, delta)) if isinstance(delta, tuple) else (tuple(v + delta for v in value) if isinstance(value, tuple) else value + delta)


def alive(p, t, length, breath, sway):
    """Breathing and a slow sway, whole cycles over the loop so it closes, and nothing at its start, so a hold picks up where its gesture
    left off. The hips and feet stay put."""
    w = 2 * math.pi / length
    b = math.sin(max(1, round(length / 4)) * w * t)
    s1, s2 = math.sin(w * t), math.sin(2 * w * t)
    add(p, 'Spine1', (breath * b, 0, 0))
    add(p, 'Spine2', (breath * .6 * b, 0, 0))
    add(p, 'Head', (-breath * .5 * b + 1.2 * sway * s2, 0, 2.5 * sway * s1))
    add(p, 'Neck', (0, 0, sway * s1))
    add(p, 'Spine', (0, 0, -.8 * sway * s1))
    for side, sign in (('Right', 1), ('Left', -1)):
        add(p, side + 'Arm', (1.5 * sway * sign * s1, -breath * .4 * b, 0))
        add(p, side + 'Hand', (3 * sway * sign * s2, 0, 0))
        add(p, side + 'Curl', 4 * sway * sign * s1)
        if side + 'Reach' in p:
            add(p, side + 'Reach', (.004 * sway * s1, .003 * sway * s2, .004 * breath * b))


def hands(p):
    """Each side's curl as four fingers, the index least and the pinky most, as a relaxed hand falls."""
    for side in ('Right', 'Left'):
        c = p.get(side + 'Curl', p['curl'])
        p[side + 'Curl'] = c if isinstance(c, tuple) else (c * .75, c * .95, c * 1.1, c * 1.25)
    return p


def act(keys, loop=None, life=None):
    """A clip from (time, pose) keys: (length, pose at a time). A one-shot clip lasts until its slowest channel has settled."""
    keys = [(t, hands(dict(p))) for t, p in keys]
    tracks = {}
    for _, p in keys:
        for c in p:
            tracks.setdefault(c, [(t, q[c]) for t, q in keys if c in q])
    for side in ('Right', 'Left'):
        if side + 'Reach' in tracks:
            tracks[side + 'IK'] = [(t, float(side + 'Reach' in q)) for t, q in keys]
    if loop:
        tracks = {c: k + [(loop, k[0][1])] if k[-1][0] < loop else k for c, k in tracks.items()}
    length = loop or keys[-1][0] + max(lag(c) for c in tracks)

    def at(t):
        p = {c: sample(k, t - lag(c), loop) for c, k in tracks.items()}
        p = hands(p)
        if life:
            alive(p, t, length, **life)
        return p
    return length, at


def held(gesture, keys, life=HOLD, length=4):
    """A gesture into its last pose, and the loop that holds it."""
    return {gesture: act(keys), gesture + '_hold': act([(0, keys[-1][1])], loop=length, life=life)}


class Cast(Rig):
    """patrol_clips.Rig's angles, then two-bone IK for each arm a pose places by its Reach."""
    def __init__(self, rig, unit, height):
        super().__init__(rig, unit)
        from mathutils import Matrix
        self.height = height
        bones = rig.data.bones
        self.rest = {b.name: b.matrix_local.copy() for b in bones}
        self.parent = {b.name: b.parent.name if b.parent else None for b in bones}
        self.rel = {n: self.rest[p].inverted() @ self.rest[n] if p else self.rest[n] for n, p in self.parent.items()}
        self.order = [b.name for b in bones if b.parent is None]
        for name in self.order:
            self.order += [c.name for c in bones[name].children]
        self.identity = Matrix.Identity(4)
        # Each elbow's hinge in its upper arm's and forearm's own frames, read off the arm bent by its forearm angle.
        self.hinge = {}
        for side in ('Right', 'Left'):
            bent = self.fk(self.matrices(Rig.bases(self, hands(pose(STANCE, **{side + 'ForeArm': 90})))))
            axis = bent[side + 'Arm'].col[1].to_3d().cross(bent[side + 'ForeArm'].col[1].to_3d()).normalized()
            for name in (side + 'Arm', side + 'ForeArm'):
                self.hinge[name] = bent[name].to_3x3().inverted() @ axis

    def matrices(self, bases):
        from mathutils import Matrix, Vector
        return {n: Matrix.Translation(loc if loc is not None else Vector()) @ q.to_matrix().to_4x4() for n, (q, loc) in bases.items()}

    def fk(self, basis):
        pose = {}
        for n in self.order:
            p = self.parent[n]
            pose[n] = (pose[p] if p else self.identity) @ self.rel[n] @ basis.get(n, self.identity)
        return pose

    @staticmethod
    def frame(y, axis):
        """The rotation taking local Y to y and the local X to (the part normal to y of) axis."""
        from mathutils import Matrix
        y = y.normalized()
        x = (axis - axis.dot(y) * y).normalized()
        return Matrix((x, y, x.cross(y))).transposed()

    def bases(self, p):
        from mathutils import Matrix, Quaternion, Vector
        out = super().bases(p)
        sides = [s for s in ('Right', 'Left') if p.get(s + 'IK', 0) > 1e-4]
        if not sides:
            return out
        basis = self.matrices(out)
        posed = self.fk(basis)
        chest = posed['Spine2'].to_3x3() @ self.rest['Spine2'].to_3x3().inverted()
        scale = self.height / self.unit
        for side in sides:
            w = p[side + 'IK']
            arm, fore, hand = side + 'Arm', side + 'ForeArm', side + 'Hand'
            out_of = (FLIP @ MIRROR) if side == 'Left' else FLIP
            s, elbow0, wrist0 = posed[arm].translation, posed[fore].translation, posed[hand].translation
            target = wrist0.lerp(s + chest @ out_of @ Vector(p[side + 'Reach']) * scale, w)
            pole = (elbow0 - s).normalized().lerp((chest @ out_of @ Vector(p.get(side + 'Elbow', ELBOW))).normalized(), w)
            a, b = (elbow0 - s).length, (wrist0 - elbow0).length
            to = target - s
            d = min(max(to.length, abs(a - b) + 1e-4), (a + b) * .999)
            u = to.normalized()
            v = pole - pole.dot(u) * u
            v = v.normalized() if v.length > 1e-6 else Vector((0, 0, -1)).cross(u).normalized()
            cos = (a * a + d * d - b * b) / (2 * a * d)
            elbow = s + a * (cos * u + math.sqrt(max(0, 1 - cos * cos)) * v)
            wrist = s + d * u
            axis = (elbow - s).cross(wrist - elbow)
            axis = axis.normalized() if axis.length > 1e-9 else u.cross(v).normalized()
            # Mirrored bones roll the other way about their own length.
            roll = math.radians(p.get(side + 'Roll', 0)) * w * (-1 if side == 'Left' else 1)
            for name, start, end, share in ((arm, s, elbow, 0), (fore, elbow, wrist, .6)):
                rot = self.frame(end - start, axis) @ self.frame(Vector((0, 1, 0)), self.hinge[name]).transposed()
                rot = Quaternion((end - start).normalized(), roll * share).to_matrix() @ rot
                above = posed[self.parent[name]]
                basis[name] = self.rel[name].inverted() @ above.inverted() @ (Matrix.Translation(start) @ rot.to_4x4())
                posed[name] = above @ self.rel[name] @ basis[name]
            basis[hand] = Quaternion((0, 1, 0), roll * .4).to_matrix().to_4x4() @ basis[hand]
            for name in (arm, fore, hand):
                out[name] = (basis[name].to_quaternion(), None)
            posed = self.fk(basis)
        return out


def make(height):
    return lambda rig, unit: Cast(rig, unit, height)


# Mio. Lying on her back on the futon, then sitting up with her legs under the cover; the hips stay over the clip's origin.
LIE = pose(STANCE, lift=(0, 0, .13 - MIO_HIP), Hips=(90, 0, 0), Neck=(-16, 0, 0), Head=(-6, 0, 14), RightArm=(0, 14, 0), LeftArm=(0, 14, 0),
           LeftForeArm=12, RightForeArm=12, RightLeg=4, LeftLeg=4, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0), curl=30)
PROP = pose(LIE, lift=(0, 0, .16 - MIO_HIP), Hips=(50, 0, 0), RightUpLeg=(40, 0, 0), LeftUpLeg=(40, 0, 0), Spine=(-6, 0, 0), Neck=(-20, 0, 0),
            Head=(-14, 0, -6), RightArm=(-38, 4, 0), RightForeArm=30, LeftArm=(-30, 4, 0), LeftForeArm=24, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0))
SIT = pose(STANCE, lift=(0, 0, .2 - MIO_HIP), RightUpLeg=(88, 0, 0), LeftUpLeg=(88, 0, 0), RightLeg=6, LeftLeg=6, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0),
           Spine=(-8, 0, 0), Spine1=(-4, 0, 0), Head=(-6, 0, 0), RightArm=(26, 2, 10), LeftArm=(26, 2, 10), RightForeArm=40, LeftForeArm=40, curl=30)
RUB = pose(SIT, RightArm=(62, -6, 26), RightForeArm=138, Head=(-14, 0, -8), Spine1=(-8, 0, 0), curl=45)
# The left wrist's terminal brought up before her face, the elbow kept in by her ribs, the face of it turned to her eyes; she looks down
# at it. RAISE is the way up, the hand still low and turning.
WRIST = dict(LeftReach=(-.06, .17, -.05), LeftElbow=(.3, -.2, -1), LeftRoll=-60, LeftHand=(-6, 0, 0), LeftCurl=(28, 36, 44, 52), Head=(-22, 0, 10),
             Neck=(-6, 0, 4), Spine1=(-3, 0, 4))
RAISE = dict(LeftReach=(.01, .1, -.22), LeftElbow=(.3, -.2, -1), LeftRoll=-25, LeftHand=(10, 0, 0), Head=(-10, 0, 5), Neck=(-2, 0, 2))
KNEES = pose(SIT, RightUpLeg=(125, 0, 0), LeftUpLeg=(118, 0, 0), RightLeg=118, LeftLeg=112, RightFoot=(0, 0, 0), LeftFoot=(0, 0, 0),
             Spine=(-24, 0, 0), Spine1=(-8, 0, 0), RightArm=(46, 4, 6), LeftArm=(46, 4, 6), RightForeArm=30, LeftForeArm=30)
CROUCH = pose(STANCE, lift=(0, .05, .62 - MIO_HIP), RightUpLeg=(96, 0, 0), LeftUpLeg=(84, 0, 0), RightLeg=100, LeftLeg=88, Spine=(-34, 0, 0),
              Spine1=(-10, 0, 0), Head=(14, 0, 0), RightArm=(48, 6, 0), LeftArm=(48, 6, 0), RightForeArm=24, LeftForeArm=24)
STRETCH = pose(STANCE, lift=(0, 0, .02), Spine=(8, 0, 0), Spine1=(6, 0, 0), Head=(16, 0, 0), RightArm=(168, -6, 0), LeftArm=(168, -6, 0),
               RightForeArm=18, LeftForeArm=18, curl=8)
MIO = {
    'sleep': act([(0, LIE)], loop=4, life=dict(breath=2, sway=0)),
    'wake': act([(0, LIE), (.6, pose(LIE, Head=(-6, 0, -10), Spine=(4, 0, 0))), (1.5, PROP), (2.2, SIT), (2.6, RUB),
                 (2.9, pose(RUB, RightArm=(60, -6, 30), Head=(-12, 0, -4))), (3.2, RUB), (3.7, SIT)]),
    'sit': act([(0, SIT)], loop=4, life=dict(breath=1.5, sway=.5)),
    **held('terminal_sit', [(0, SIT), (.3, pose(SIT, **RAISE)), (.7, pose(SIT, **WRIST))]),
    'rise': act([(0, SIT), (.6, KNEES), (1.2, CROUCH), (1.9, STANCE), (2.5, STRETCH), (3, pose(STRETCH, Spine=(10, 0, 4), RightArm=(172, -4, 0))),
                 (3.6, STANCE)]),
    **held('terminal', [(0, STANCE), (.3, pose(STANCE, **RAISE)), (.7, pose(STANCE, **WRIST))]),
}

# Yui and Renji. Yui is quick and bright: a bouncing walk, a run, a wave, arms folded to sulk; Renji leans over his bench.
# Giving: the hand comes up and out, palm up, a little past where it stops; pointing: the index out along the arm, the others closed.
GIVE = pose(STANCE, lift=(0, .03, 0), Spine=(-5, 0, -3), Head=(-8, 0, 0), RightReach=(-.02, .25, -.11), RightElbow=(.3, -.4, -1), RightRoll=50,
            RightHand=(-8, 0, 0), RightCurl=(16, 24, 30, 38))
POINT = pose(STANCE, Spine=(2, 0, -8), Head=(6, 0, -6), RightReach=(.1, .26, -.02), RightElbow=(.6, -.3, -.8), RightRoll=-10,
             RightHand=(4, 0, 0), RightCurl=(0, 80, 88, 96))
READ = pose(STANCE, Head=(-24, 0, 0), Spine1=(-4, 0, 0), RightReach=(-.07, .2, -.12), LeftReach=(-.07, .2, -.12), RightRoll=-55, LeftRoll=-55,
            RightHand=(-12, 0, 0), LeftHand=(-12, 0, 0), curl=34)
# Arms folded, the elbows bending before the arms cross so the hands don't reach out first.
FOLD = pose(STANCE, Hips=(0, 0, 5), Spine=(4, 0, 0), Head=(4, 0, -8), RightArm=(18, 4, 36), RightForeArm=94, LeftArm=(16, 4, 38), LeftForeArm=92,
            LeftUpLeg=(4, 0, 0), curl=66)
BEND = pose(STANCE, RightArm=(8, 8, 18), LeftArm=(8, 8, 16), RightForeArm=80, LeftForeArm=78, curl=30)
# Waving: the elbow out at the shoulder's height, the forearm up, swinging from the elbow with the open hand trailing it.
WAVE = pose(STANCE, Head=(4, 0, -5), Spine=(2, 0, -4), RightReach=(.14, .05, .12), RightElbow=(1, -.1, -.3), RightRoll=0, RightHand=(0, 0, 0), RightCurl=5)
STRIKE = dict(RightArm=(48, 0, 16), RightForeArm=40)
BENCH = pose(STANCE, lift=(0, 0, -.04), Spine=(-18, 0, 0), Spine1=(-8, 0, 0), Head=(-20, 0, 0), LeftArm=(42, 0, 26), LeftForeArm=62,
             RightArm=(96, -10, 16), RightForeArm=100, RightUpLeg=(6, 0, 0), RightLeg=10, LeftLeg=8, curl=45)
PEOPLE = {
    'idle': act([(0, STANCE)], loop=4, life=IDLE),
    **held('give', [(0, STANCE), (.3, pose(GIVE, RightReach=(0, .14, -.2), RightRoll=20, lift=(0, .01, 0))),
                    (.6, pose(GIVE, RightReach=(-.02, .27, -.1))), (.85, GIVE)]),
    **held('point', [(0, STANCE), (.25, pose(STANCE, RightReach=(.03, .12, -.17), RightCurl=(30, 50, 55, 60), Head=(2, 0, -3))),
                     (.55, pose(POINT, RightReach=(.11, .28, -.01))), (.8, POINT)]),
    **held('read', [(0, STANCE), (.55, READ)]),
}
SWING = [(i * .3, pose(WAVE, RightReach=(.19 if i % 2 else .1, .05, .12), RightHand=(0, 0, 16 if i % 2 else -16))) for i in range(9)]
YUI = {
    **PEOPLE,
    'walk': (.9, lambda t: hands(gait(stride=26, load=10, swing=52, arm=20, elbow=16, lean=2, bob=.04, sway=5, stance=STANCE)(t / .9))),
    'run': (.7, lambda t: hands(gait(stride=34, load=20, swing=90, arm=32, elbow=70, lean=12, bob=.05, sway=6, stance=STANCE)(t / .7))),
    **held('fold', [(0, STANCE), (.3, BEND), (.65, FOLD)], life=dict(breath=2, sway=.5), length=3),
    'wave': act([(0, STANCE), (.25, pose(STANCE, RightReach=(.2, .04, -.14), RightElbow=(1, -.1, -.6), RightCurl=12)), (.5, SWING[0][1])]),
    'wave_hold': act(SWING[:-1], loop=2.4, life=dict(breath=1, sway=.3)),
}
RENJI = {
    **PEOPLE,
    'walk': (1.1, lambda t: hands(gait(stride=22, load=12, swing=46, arm=14, elbow=14, lean=3, bob=.025, sway=4, stance=STANCE)(t / 1.1))),
    'work': act([(0, BENCH), (.3, pose(BENCH, **STRIKE)), (.43, pose(BENCH, RightArm=(56, 0, 16), RightForeArm=48))], loop=1.2),
}
CLIPS = {'mio': MIO, 'yui': YUI, 'renji': RENJI}
LOOPS = {'mio': ('sleep', 'sit', 'terminal_sit_hold', 'terminal_hold'), 'yui': ('idle', 'walk', 'run', 'fold_hold', 'wave_hold', 'give_hold', 'point_hold', 'read_hold'),
         'renji': ('idle', 'walk', 'work', 'give_hold', 'point_hold', 'read_hold')}


def poses(rig, unit, clips, loops, extra, height=HEIGHTS['mio']):
    """name -> per-frame bases of every bone, as import_tripo_mio.py keys her clips; extra bases (her grip) override the pose's."""
    from mathutils import Matrix
    solver = Cast(rig, unit, height)
    out = {}
    for name, (length, at) in clips.items():
        frames = round(length * FPS)
        out[name] = []
        for f in range(frames + 1):
            basis = {pb.name: Matrix.Identity(4) for pb in rig.pose.bones}
            for bone, (q, loc) in solver.bases(at(f / FPS if name not in loops or f < frames else 0)).items():
                basis[bone] = q.to_matrix().to_4x4()
                if loc is not None:
                    basis[bone].translation = loc
            basis.update({n: m.copy() for n, m in extra.items()})
            out[name].append(basis)
    return out


def swap(model):
    """Key the clips on the committed model as its importer would, Mio's at frame 1 on with her sword grip, and swap them into its GLB."""
    import bpy
    import json
    import tempfile
    from mathutils import Matrix
    import glb_swap
    from patrol_clips import key
    path = ROOT / f'assets/models/{model}.glb'
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    rig.animation_data_clear()
    unit = rig.matrix_world.to_scale()[0]
    if model == 'mio':
        ready = json.loads((ROOT / 'tools/mio_ready.json').read_text())
        grip = {n: Matrix([m[i:i + 4] for i in range(0, 16, 4)]) for n, m in ready.items() if n.startswith('RightHand') and n != 'RightHand'}
        rig.animation_data_create()
        for name, frames in poses(rig, unit, CLIPS[model], LOOPS[model], grip).items():
            action = bpy.data.actions.new(name)
            rig.animation_data.action = action
            for t, basis in enumerate(frames, 1):
                for pb in rig.pose.bones:
                    pb.rotation_mode = 'QUATERNION'
                    pb.matrix_basis = basis[pb.name]
                    for channel in ('rotation_quaternion', 'location', 'scale'):
                        pb.keyframe_insert(channel, frame=t)
            rig.animation_data.action = None
            track = rig.animation_data.nla_tracks.new()
            track.name = name
            track.strips.new(name, 1, action)
            track.mute = True
        mode = 'NLA_TRACKS'
    else:
        key(rig, unit, CLIPS[model], LOOPS[model], Cast(rig, unit, HEIGHTS[model]))
        mode = 'ACTIONS'
    donor = Path(tempfile.mkdtemp()) / f'{model}.glb'
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=str(donor), export_format='GLB', use_selection=True, export_animations=True,
                              export_animation_mode=mode, export_anim_single_armature=False, export_apply=False)
    path.write_bytes(glb_swap.swap_animations(path.read_bytes(), donor.read_bytes(), list(CLIPS[model])))
    print('CAST_SWAP', model, sorted(CLIPS[model]), flush=True)


if __name__ == '__main__':
    from patrol_clips import preview
    model = next((a.split('=', 1)[1] for a in sys.argv if a.startswith('--model=')), 'mio')
    if '--swap' in sys.argv:
        swap(model)
    else:
        preview(model, CLIPS[model], LOOPS[model], HEIGHTS[model], make(HEIGHTS[model]))
