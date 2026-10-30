"""The opening's acting clips for the human Tripo rigs (Mixamo bones, prefix stripped), as Game.Cue() plays them from
Prologue.Stage: Mio's waking in her room, Yui's and Renji's gestures. Keyed with patrol_clips.py's poses: degrees about the
character's own axes as it stands in the rest pose, its arms hanging, x to its right, y forward, z up; +x swings a hanging limb
forward and tips an upright bone back, +y lowers an arm toward the body, +z turns to the character's left, left bones mirrored.
'lift' moves the hips in metres at game scale; a 'Foot' angle adds to the one that keeps the sole level.

import_tripo_mio.py keys MIO on her source, import_tripo_cast.py keys YUI and RENJI on theirs. Run on its own, this renders contact
sheets from the committed model (or a prefix-stripped GLB of the source with --src), so the poses can be tuned without the source:
xvfb-run -a blender --background --factory-startup --python tools/cast_clips.py -- --model=mio [--src=GLB] [--only=wake,rise] [--view=side]
"""
import math
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from patrol_clips import gait, tween  # noqa: E402

# The importers' --height, and Mio's hip joint over the ground at rest: her seated and lying lifts are measured from it.
HEIGHTS = {'mio': 1.84, 'yui': 1.6, 'renji': 1.78}
MIO_HIP = 1.075
STANCE = {
    'lift': (0, 0, 0), 'Hips': (0, 0, 0), 'Spine': (0, 0, 0), 'Spine1': (0, 0, 0), 'Spine2': (0, 0, 0), 'Neck': (0, 0, 0), 'Head': (0, 0, 0),
    'RightArm': (2, 10, 0), 'LeftArm': (2, 10, 0), 'RightForeArm': 10, 'LeftForeArm': 10, 'RightHand': (0, 0, 0), 'LeftHand': (0, 0, 0),
    'RightUpLeg': (0, 0, 0), 'LeftUpLeg': (0, 0, 0), 'RightLeg': 2, 'LeftLeg': 2, 'RightFoot': (0, 0, 0), 'LeftFoot': (0, 0, 0), 'curl': 20,
}


def pose(base, **bones):
    return {**base, **bones}


def breathing(base, depth=1.5):
    """A loop's poses at its start, middle and end: the chest rises and the shoulders settle."""
    full = pose(base, Spine1=tuple(a + b for a, b in zip(base['Spine1'], (depth, 0, 0))),
                Spine2=tuple(a + b for a, b in zip(base['Spine2'], (depth * .6, 0, 0))), Head=tuple(a + b for a, b in zip(base['Head'], (-depth * .8, 0, 0))))
    return [(0, base), (.5, full), (1, base)]


def loop(length, keys):
    return length, lambda t: tween([(at * length, p) for at, p in keys])(t)


def hold(keys):
    return keys[-1][0], tween(keys)


# Mio. Lying on her back on the futon, then sitting up with her legs under the cover; the hips stay over the clip's origin.
LIE = pose(STANCE, lift=(0, 0, .13 - MIO_HIP), Hips=(90, 0, 0), Neck=(-16, 0, 0), Head=(-6, 0, 14), RightArm=(0, 14, 0), LeftArm=(0, 14, 0),
           LeftForeArm=12, RightForeArm=12, RightLeg=4, LeftLeg=4, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0), curl=30)
PROP = pose(LIE, lift=(0, 0, .16 - MIO_HIP), Hips=(50, 0, 0), RightUpLeg=(40, 0, 0), LeftUpLeg=(40, 0, 0), Spine=(-6, 0, 0), Neck=(-20, 0, 0),
            Head=(-14, 0, -6), RightArm=(-38, 4, 0), RightForeArm=30, LeftArm=(-30, 4, 0), LeftForeArm=24, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0))
SIT = pose(STANCE, lift=(0, 0, .2 - MIO_HIP), RightUpLeg=(88, 0, 0), LeftUpLeg=(88, 0, 0), RightLeg=6, LeftLeg=6, RightFoot=(70, 0, 0), LeftFoot=(70, 0, 0),
           Spine=(-8, 0, 0), Spine1=(-4, 0, 0), Head=(-6, 0, 0), RightArm=(26, 2, 10), LeftArm=(26, 2, 10), RightForeArm=40, LeftForeArm=40, curl=30)
RUB = pose(SIT, RightArm=(62, -6, 26), RightForeArm=138, Head=(-14, 0, -8), Spine1=(-8, 0, 0), curl=45)
# The left wrist's terminal raised to her eyes.
WRIST = dict(LeftArm=(42, 4, 28), LeftForeArm=88, LeftHand=(0, 0, -10), Head=(-30, 0, 10), Spine1=(-3, 0, 4))
KNEES = pose(SIT, RightUpLeg=(125, 0, 0), LeftUpLeg=(118, 0, 0), RightLeg=118, LeftLeg=112, RightFoot=(0, 0, 0), LeftFoot=(0, 0, 0),
             Spine=(-24, 0, 0), Spine1=(-8, 0, 0), RightArm=(46, 4, 6), LeftArm=(46, 4, 6), RightForeArm=30, LeftForeArm=30)
CROUCH = pose(STANCE, lift=(0, .05, .62 - MIO_HIP), RightUpLeg=(96, 0, 0), LeftUpLeg=(84, 0, 0), RightLeg=100, LeftLeg=88, Spine=(-34, 0, 0),
              Spine1=(-10, 0, 0), Head=(14, 0, 0), RightArm=(48, 6, 0), LeftArm=(48, 6, 0), RightForeArm=24, LeftForeArm=24)
STRETCH = pose(STANCE, lift=(0, 0, .02), Spine=(8, 0, 0), Spine1=(6, 0, 0), Head=(16, 0, 0), RightArm=(168, -6, 0), LeftArm=(168, -6, 0),
               RightForeArm=18, LeftForeArm=18, curl=8)
MIO = {
    'sleep': loop(4, breathing(LIE, 2)),
    'wake': hold([(0, LIE), (.6, pose(LIE, Head=(-6, 0, -10), Spine=(4, 0, 0))), (1.5, PROP), (2.2, SIT), (2.6, RUB),
                  (2.9, pose(RUB, RightArm=(60, -6, 30), Head=(-12, 0, -4))), (3.2, RUB), (3.7, SIT)]),
    'sit': loop(4, breathing(SIT)),
    'terminal_sit': hold([(0, SIT), (.5, pose(SIT, **WRIST))]),
    'rise': hold([(0, SIT), (.6, KNEES), (1.2, CROUCH), (1.9, STANCE), (2.5, STRETCH), (3, pose(STRETCH, Spine=(10, 0, 4), RightArm=(172, -4, 0))),
                  (3.6, STANCE)]),
    'terminal': hold([(0, STANCE), (.5, pose(STANCE, **WRIST))]),
}
MIO_LOOPS = ('sleep', 'sit')

# Yui and Renji. Yui is quick and bright: a bouncing walk, a run, a wave, arms folded to sulk; Renji leans over his bench.
GIVE = pose(STANCE, lift=(0, .03, 0), RightArm=(70, 4, 12), RightForeArm=24, Spine=(-6, 0, 0), Head=(-6, 0, 0), curl=36)
POINT = pose(STANCE, RightArm=(98, 0, 24), RightForeArm=4, Spine=(2, 0, -8), Head=(6, 0, -6), curl=10)
READ = pose(STANCE, RightArm=(36, 4, 30), LeftArm=(36, 4, 30), RightForeArm=98, LeftForeArm=98, Head=(-24, 0, 0), curl=36)
FOLD = pose(STANCE, Hips=(0, 0, 5), Spine=(4, 0, 0), Head=(4, 0, -8), RightArm=(18, 4, 42), RightForeArm=105, LeftArm=(16, 4, 40), LeftForeArm=102,
            LeftUpLeg=(4, 0, 0), curl=40)
WAVE = pose(STANCE, RightArm=(150, -26, 0), RightForeArm=26, Head=(4, 0, -4), Spine=(2, 0, -4), curl=6)
STRIKE = dict(RightArm=(48, 0, 16), RightForeArm=40)
BENCH = pose(STANCE, lift=(0, 0, -.04), Spine=(-18, 0, 0), Spine1=(-8, 0, 0), Head=(-20, 0, 0), LeftArm=(42, 0, 26), LeftForeArm=62,
             RightArm=(96, -10, 16), RightForeArm=100, RightUpLeg=(6, 0, 0), RightLeg=10, LeftLeg=8, curl=45)
PEOPLE = {
    'idle': loop(4, breathing(STANCE)),
    'give': hold([(0, STANCE), (.5, GIVE), (1, pose(GIVE, RightArm=(72, 4, 12)))]),
    'point': hold([(0, STANCE), (.45, POINT), (.9, pose(POINT, RightArm=(96, 0, 24)))]),
    'read': hold([(0, STANCE), (.5, READ)]),
}
YUI = {
    **PEOPLE,
    'walk': (.9, lambda t: gait(stride=26, load=10, swing=52, arm=20, elbow=16, lean=2, bob=.04, sway=5, stance=STANCE)(t / .9)),
    'run': (.7, lambda t: gait(stride=34, load=20, swing=90, arm=32, elbow=70, lean=12, bob=.05, sway=6, stance=STANCE)(t / .7)),
    'fold': loop(3, breathing(FOLD, 2)),
    'wave': loop(1.2, [(0, WAVE), (.5, pose(WAVE, RightArm=(150, -26, 22), RightForeArm=40)), (1, WAVE)]),
}
RENJI = {
    **PEOPLE,
    'walk': (1.1, lambda t: gait(stride=22, load=12, swing=46, arm=14, elbow=14, lean=3, bob=.025, sway=4, stance=STANCE)(t / 1.1)),
    'work': loop(1.2, [(0, BENCH), (.25, pose(BENCH, **STRIKE)), (.36, pose(BENCH, RightArm=(56, 0, 16), RightForeArm=48)), (1, BENCH)]),
}
CLIPS = {'mio': MIO, 'yui': YUI, 'renji': RENJI}
LOOPS = {'mio': MIO_LOOPS, 'yui': ('idle', 'walk', 'run', 'fold', 'wave'), 'renji': ('idle', 'walk', 'work')}


def poses(rig, unit, clips, loops, extra):
    """name -> per-frame bases of every bone, as import_tripo_mio.py keys her clips; extra bases (her grip) override the pose's."""
    from mathutils import Matrix
    from patrol_clips import FPS, Rig
    solver = Rig(rig, unit)
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


if __name__ == '__main__':
    from patrol_clips import preview
    model = next((a.split('=', 1)[1] for a in sys.argv if a.startswith('--model=')), 'mio')
    preview(model, CLIPS[model], LOOPS[model], HEIGHTS[model])
