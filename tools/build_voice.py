"""Japanese voice lines from ElevenLabs text-to-speech; Python stdlib only.

python3 tools/build_voice.py --check           # ja.json still matches Prologue.cs; no key, no credits
python3 tools/build_voice.py --audition        # every AUDITION voice reads its speaker's AUDITION_KEYS into artifacts/voice-audition/
python3 tools/build_voice.py                   # each CAST voice reads all its speaker's lines into assets/audio/voice/ja/
Needs ELEVENLABS_API_KEY in the environment or in the git-ignored .env at the repo root. Existing files are kept
unless --force, so a rerun only spends credits on new or changed lines. See .claude/skills/voicing-dialogue/SKILL.md."""
import argparse, html, json, os, re, sys, urllib.error, urllib.request
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
LINES=ROOT/'assets/dialogue/ja.json'
MODEL='eleven_v4'
FORMAT='mp3_44100_128'
SEED=2189
# The chosen voice per speaker, from the voice record in .claude/skills/voicing-dialogue/SKILL.md.
CAST={'Mio':'JTlYtJrcTzPC71hMLOxo','Zero':'nvAEN8lxUT4UJcAyxcCQ'}
# Candidates for a speaker still being auditioned, e.g. {'Ren':{'shortname':voice_id}} and {'Ren':['Briefing.0',...]}.
AUDITION={}
AUDITION_KEYS={}
def english():
    """Prologue.cs as {'Intro.1': ('Mio', text), 'FirstMeeting.Chiyo.2': ...}: the keys the dialogue files use."""
    lines,scene,index,table={},None,0,None
    for line in (ROOT/'scripts/Prologue.cs').read_text().splitlines():
        line=line.strip()
        if m:=re.search(r'\)\[\] (\w+)=$',line):scene,index=m[1],0
        elif m:=re.search(r'Dictionary<string,\(string Speaker,string Text\)\[\]> (\w+)=new\(\)$',line):table=m[1]
        elif m:=re.match(r'\["([^"]+)"\]=new',line):scene,index=f'{table}.{m[1]}',0
        elif m:=re.match(r'\("([^"]*)","(.*)"\),?$',line):lines[f'{scene}.{index}']=(m[1],m[2]);index+=1
    return lines
def api_key():
    if os.environ.get('ELEVENLABS_API_KEY'):return os.environ['ELEVENLABS_API_KEY']
    env=ROOT/'.env'
    for line in env.read_text().splitlines() if env.exists() else []:
        name,_,value=line.partition('=')
        if name.strip().removeprefix('export ').strip()=='ELEVENLABS_API_KEY':return value.strip().strip('"\'')
    sys.exit('ELEVENLABS_API_KEY is not set: export it, or put ELEVENLABS_API_KEY=... in .env at the repo root.')
def speak(key,voice,text):
    body=json.dumps({'text':text,'model_id':MODEL,'language_code':'ja','seed':SEED}).encode()
    request=urllib.request.Request(f'https://api.elevenlabs.io/v1/text-to-speech/{voice}?output_format={FORMAT}',body,
        {'xi-api-key':key,'Content-Type':'application/json','Accept':'audio/mpeg'})
    try:
        with urllib.request.urlopen(request,timeout=120) as response:return response.read()
    except urllib.error.HTTPError as error:sys.exit(f'ElevenLabs {error.code} for voice {voice}: {error.read().decode(errors="replace")}')
def render(key,lines,voice,keys,folder,force):
    folder.mkdir(parents=True,exist_ok=True)
    for k in keys:
        out=folder/f'{k}.mp3'
        if out.exists() and not force:continue
        out.write_bytes(speak(key,voice,lines[k].get('tts',lines[k]['ja'])));print('wrote',out.relative_to(ROOT))
def audition_page(speaker,lines,voices,keys,folder):
    rows=''.join(f'<tr><td><b>{k}</b><br>{html.escape(lines[k]["ja"])}<br><small>{html.escape(lines[k]["en"])}</small></td>'
        +''.join(f'<td><audio controls preload="none" src="{name}/{k}.mp3"></audio></td>' for name in voices)+'</tr>' for k in keys)
    head=''.join(f'<th>{name}<br><small>{voice}</small></th>' for name,voice in voices.items())
    (folder/'index.html').write_text(f'<!doctype html><meta charset="utf-8"><title>{speaker} voice audition</title>'
        f'<style>body{{font:15px sans-serif;margin:24px}}td,th{{padding:8px;border-bottom:1px solid #ddd;text-align:left}}</style>'
        f'<h1>{speaker} voice audition</h1><p>{MODEL}</p><table><tr><th>Line</th>{head}</tr>{rows}</table>')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');parser.add_argument('--audition',action='store_true');parser.add_argument('--force',action='store_true')
    args=parser.parse_args()
    lines=json.loads(LINES.read_text());en=english()
    # Keys are positions in Prologue.cs, so an inserted or edited English line leaves its Japanese line (and audio) stale.
    stale=[k for k,line in lines.items() if en.get(k)!=(line['speaker'],line['en'])]
    if stale:sys.exit(f'Prologue.cs no longer matches {stale}: update {LINES.relative_to(ROOT)} before voicing.')
    for speaker in sorted({line['speaker'] for line in lines.values()}|set(AUDITION)|set(CAST)):
        missing=[k for k,(who,_) in en.items() if who==speaker and k not in lines]
        if missing:print(f'{speaker} has no Japanese yet for {missing}')
    if args.check:return print(f'{len(lines)} Japanese lines match Prologue.cs')
    key=api_key()
    if args.audition:
        for speaker,voices in AUDITION.items():
            folder=ROOT/'artifacts/voice-audition'/speaker
            for name,voice in voices.items():render(key,lines,voice,AUDITION_KEYS[speaker],folder/name,args.force)
            audition_page(speaker,lines,voices,AUDITION_KEYS[speaker],folder);print('open',(folder/'index.html').relative_to(ROOT))
    for speaker,voice in ({} if args.audition else CAST).items():
        render(key,lines,voice,[k for k,line in lines.items() if line['speaker']==speaker],ROOT/'assets/audio/voice/ja'/speaker,args.force)
if __name__=='__main__':main()
