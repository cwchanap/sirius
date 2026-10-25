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
print('Generated valley loop and 7 original combat cues.')
