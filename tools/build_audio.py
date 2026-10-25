"""Original quiet pentatonic ambience and short combat cues; Python stdlib only."""
import math, random, struct, wave
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]/'assets/audio'
OUT.mkdir(parents=True,exist_ok=True)
RATE=22050
random.seed(2189)
def write(name,samples):
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE)
        f.writeframes(b''.join(struct.pack('<h',int(max(-1,min(1,x))*32767)) for x in samples))
notes=[293.665,369.994,440,554.365,587.33,440,369.994,329.628,293.665,440,369.994,329.628]
length=24
samples=[];wind=0
for i in range(RATE*length):
    t=i/RATE;wind=wind*.98+random.uniform(-1,1)*.02
    value=wind*.22
    for j in range(3):
        index=int(t/2)-j
        if index<0:continue
        age=t-index*2;freq=notes[index%len(notes)]
        value+=.10*math.exp(-age*1.8)*(math.sin(t*math.tau*freq)+.35*math.sin(t*math.tau*freq*2.005))
    # Gentle distant birds, each phrase fades to silence at the loop seam.
    phase=t%6
    if 2<phase<2.6:
        env=math.sin((phase-2)/.6*math.pi)**2
        value+=.019*env*math.sin(math.tau*(1800*t+70*math.sin(t*14)))
    value*=min(1,t/.5,(length-t)/.8)
    samples.append(value)
write('valley',samples)
# swing, impact, guard, deflect, slam and stun are ElevenLabs sound effects now (ASSETS.md); this no longer writes them.
params={'break':(180,.55),'skill':(330,.75),
        'hurt':(90,.22),'jump':(260,.13),'dodge':(150,.16),'heal':(660,.65),'warning':(540,.17)}
for name,(freq,duration) in params.items():
    data=[]
    for i in range(int(RATE*duration)):
        t=i/RATE;env=math.exp(-t/duration*5)*min(1,t/.006)
        noisy=name in ('hurt','dodge','break')
        tone=math.sin(math.tau*freq*t*(1+.2*t/duration))
        high=math.sin(math.tau*freq*2.7*t)
        value=(random.uniform(-1,1)*.5+tone*.35) if noisy else (tone*.4+high*.2)
        data.append(value*env*.55)
    write(name,data)
# Placeholder music for the relay road and for a fight, until ElevenLabs tracks replace them. Each note is written modulo
# the loop's length, so a tail running past the end comes back in at the start and the loop has no seam.
def midi(m):return 440*2**((m-69)/12)
def render(name,bpm,bars,events):
    n=round(RATE*60/bpm*4*bars);buf=[0.0]*n
    for at,dur,amp,sound in events(60/bpm):
        s=round(at*RATE)
        for i in range(int(dur*RATE)):buf[(s+i)%n]+=amp*sound(i/RATE)
    peak=max(abs(x) for x in buf);write(name,[x/peak*.8 for x in buf])
def kick(t):return math.sin(math.tau*(50*t+90*(1-math.exp(-25*t))/25))*math.exp(-t*9)
def snare(t):return (random.uniform(-1,1)*.7+.3*math.sin(math.tau*190*t))*math.exp(-t*18)
def hat(t):return random.uniform(-1,1)*math.exp(-t*60)
def taiko(t):return math.sin(math.tau*(55*t+50*(1-math.exp(-12*t))/12))*math.exp(-t*5)+random.uniform(-1,1)*math.exp(-t*30)*.4
def pluck(f,decay=6,bright=.25):return lambda t:(math.sin(math.tau*f*t)+.5*math.sin(2*math.tau*f*t)+bright*math.sin(3*math.tau*f*t))*math.exp(-t*decay)*min(1,t/.003)
def bass(f,dur):return lambda t:(math.sin(math.tau*f*t)+.3*math.sin(2*math.tau*f*t)+.15*math.sin(3*math.tau*f*t))*min(1,t/.01,(dur-t)/.02)
def relay(beat):
    # Dm Bb C Am Dm Bb C A at 128 BPM: kick, snare, hats, bouncing bass and a plucked arpeggio.
    for bar,(root,minor) in enumerate([(38,1),(34,0),(36,0),(33,1),(38,1),(34,0),(36,0),(33,0)]):
        b=bar*4*beat;tones=[root,root+(3 if minor else 4),root+7,root+12,root+(15 if minor else 16)]
        for k in (0,2,2.5):yield b+k*beat,.4,.9,kick
        for k in (1,3):yield b+k*beat,.3,.5,snare
        if bar%4==3:
            for k in (3.25,3.5,3.75):yield b+k*beat,.25,.35,snare
        for k in range(8):yield b+k*beat/2,.08,.12 if k%2 else .07,hat
        for k in range(8):f=midi(root-12+(12 if k%2 else 0));yield b+k*beat/2,beat/2,.45,bass(f,beat/2)
        for k,i in enumerate([0,1,2,3,2,1,2,3,0,1,2,3,4,3,2,1]):yield b+k*beat/4,.6,.16+(.04 if k%4==0 else 0),pluck(midi(tones[i]+36))
def battle(beat):
    # E phrygian at 156 BPM: taiko, rim clicks, a sixteenth bass ostinato and bright shamisen-like stabs.
    for bar,root in enumerate([40,40,41,40,40,40,43,41]):
        b=bar*4*beat
        for k in (0,.75,1.5,2,3):yield b+k*beat,.6,.9,taiko
        if bar%4==3:
            for j,k in enumerate((3,3.25,3.5,3.75)):yield b+k*beat,.5,.5+.12*j,taiko
        for k in range(8):yield b+(k+.5)*beat/2,.05,.12,hat
        for k,o in enumerate([0,0,12,0,0,0,1,0,0,0,12,0,3,0,1,0]):f=midi(root-12+o);yield b+k*beat/4,beat/4,.4,bass(f,beat/4)
        for k,o in [(0,0),(.75,3),(1.5,1),(2.5,7),(3,5),(3.5,3)]:yield b+k*beat,.5,.2,pluck(midi(root+24+o),9,.6)
render('relay',128,8,relay)
render('battle',156,8,battle)
print('Generated valley loop, 7 original combat cues and placeholder relay and battle music.')
