---
name: voicing-dialogue
description: Use when giving a Hoshikawa character a Japanese voice, auditioning ElevenLabs voices, voicing new or edited prologue lines, translating Prologue.cs dialogue into Japanese, or when tools/build_voice.py refuses to run because Prologue.cs changed.
---

# Voicing dialogue (Japanese, ElevenLabs)

## Overview

Japanese text is data: `assets/dialogue/ja.json` holds every translated line, keyed by its position in `scripts/Prologue.cs`. The same file is the source for a future Japanese localization, so it stores the displayed text, never TTS markup. `tools/build_voice.py` turns it into speech. In-game text stays English; the dialogue plays the Japanese voice under it.

**Playback.** When `Game.Advance()` shows beat *i* of a scene, it plays `res://assets/audio/voice/ja/<Speaker>/<scene>.<i>.mp3` if that file exists, and stops it on the next beat, Esc or the end of the scene. The scene name comes from `Prologue.SceneNames`, so a new scene array must be added there to be voiced; one-line `Play((speaker,text))` calls build a fresh array and stay silent. Any speaker is voiced as soon as their files exist; there is no per-character switch. `IntegrationCheck` requires every Mio and Zero beat in a named scene to have its mp3, so a new or moved line of theirs fails the self-test until it is voiced. Zero's relay one-liners (`RelayAsleep`, `RelayGuarded`, `RelaySteady`) are single strings and stay silent.

| What | Where | In git |
|---|---|---|
| Japanese lines | `assets/dialogue/ja.json` | yes |
| Model, cast, auditions | constants at the top of `tools/build_voice.py` | yes |
| Auditions | `artifacts/voice-audition/<Speaker>/<voice>/<key>.mp3` + `index.html` | no (artifacts/ is ignored) |
| Game lines | `assets/audio/voice/ja/<Speaker>/<key>.mp3` | yes, committed directly like the other audio |
| API key | `ELEVENLABS_API_KEY` in the shell, or `.env` at the repo root | never (.env is ignored) |

## Keys and the line file

A key is `<Array>.<index>` into `Prologue.cs`: `Intro.1`, `Briefing.6`, `FirstMeeting.Chiyo.2`. Indices count every beat, narration and other speakers included. The single-string constants (`RenReminder`, `RelaySteady`, ...) have no key yet; extend `english()` in the script when one needs voicing.

```json
"Homecoming.8": {"speaker": "Mio", "en": "<exact English from Prologue.cs>", "ja": "<displayed Japanese>", "tts": "[softly] <spoken Japanese>"}
```

- `en` must equal the current English beat; the script refuses to run otherwise, because a moved or edited line would voice the wrong text.
- `ja` is what a player would read. No audio tags, no kana spelled out for pronunciation.
- `tts` is optional, only when the spoken text differs: eleven_v4 audio tags (`[softly]`, `[sighs]`, `[warmly]`) or a kana reading for a misread kanji.
- Style: names in katakana (ミオ, ゼロ, レン, チヨ), places in kanji (星川, 星川線). Mio talks plainly to friends and Zero, a little dry ("はいはい"), and uses です/ます with elders such as Chiyo. Zero is a railway system talking like a curious child: it uses no first-person pronoun, counts things, reaches for railway words (発車, 十一両編成, 進行信号, 停止信号, 側線) and says 「保存しておくね」 for a smell it keeps. Its `tts` spells 四番線 as よんばんせん and KOMA as コマ.

## Workflow

1. **Translate.** Add or fix the speaker's lines in `ja.json`. `python3 tools/build_voice.py --check` validates against `Prologue.cs` with no key and no credits, and lists every translated speaker's lines still missing Japanese.
2. **Find voices.** With the ElevenLabs connector: `creative_list_voices` with `languages: ["ja"]`, gender, age and `use_cases: ["characters_animation"]`. Voice IDs come only from that tool or the user. Pick about three that span the character brief (e.g. warm, low and dry, anime-heroine) rather than three similar ones.
3. **Audition.** Put the candidates in `AUDITION` (`{'Speaker': {'shortname': voice_id}}`) and up to five of that speaker's translated lines in `AUDITION_KEYS['Speaker']`, including an emotional one if there is one. Remove finished auditions from both so they are not re-rendered, then `python3 tools/build_voice.py --audition` and `open artifacts/voice-audition/<Speaker>/index.html`. The user picks; do not wire audio in before that.
4. **Cast.** Set `CAST['Speaker']=voice_id`. Copy the winner's audition takes into the game folder first (same text, voice and model), so only the remaining lines are paid for. Run `python3 tools/build_voice.py`, listen to every file, then `$GODOT_MONO --headless --editor --path . --import` and commit the mp3s with their `.import` files. Run the self-test (headless and graphical) to confirm the lines play. Record the pick in the table below and in `ASSETS.md`.
5. **New or changed lines later.** Update `ja.json`, delete the affected mp3s (their `.import` files can stay; Godot reimports), or pass `--force` to redo all, and rerun step 4. Existing files are skipped so reruns only pay for what changed.

**No API key in the shell?** Ask the user to export it or put it in `.env`; never ask for it in chat, print it, or commit it. Meanwhile the ElevenLabs connector works without one: `creative_create_flow`, then one `creative_generate_speech` per take (`model_id: eleven_v4`, `generations_count: 1`, prompt = the line's `tts` or `ja`), then a single `creative_get_flow_run_status` for all session ids, download each media `master_url` to the same path the script would use, and call the script's `audition_page()` to write `index.html`. Never call `creative_generate_speech` again to retry; each call charges. The connector takes no seed or voice settings.

## Settings

| Setting | Value |
|---|---|
| Model | `eleven_v4` (supports audio tags) |
| Output | `mp3_44100_128`, mono, about 16 KB per second |
| Language | `language_code: ja` |
| Seed | 2189, the repo's world seed, so a rerun of the same text and voice is reproducible |
| Voice settings | the voice's own defaults |

## Voice record

Add a row with the character brief before auditioning a new speaker.

| Speaker | Brief | Status | Voices (name: id) |
|---|---|---|---|
| Zero | city railway system heard only through Mio's wrist terminal; curious like a child, counts things, gender unstated ("it") | cast 2026-10-18: **Koume** | Koume (sweet, playful, childlike): `nvAEN8lxUT4UJcAyxcCQ`, chosen. Auditioned: Kei (light, androgynous anime): `XKJWAGK4dewYkzr1hsNs`; Toru (calm neutral boy): `wGSNAQwhUbFf1J43d5Tw` |
| Mio | 22, scavenger and repairer; practical, warm, a little dry | cast 2026-10-18: **Yuki** | Yuki (calm anime): `JTlYtJrcTzPC71hMLOxo`, chosen. Auditioned: Kurea (warm, expressive): `IgiTcMDSHJPIJjnlwIdM`; Haruno (calm, low): `b02kNc4YmZR6NvjjXr2a` |

Mio's audition rendered Intro.1/3/6/8 and Homecoming.8 through the connector (eleven_v4, default settings, no seed) for about 450 credits. On 2026-10-18 all 13 of her lines landed in `assets/audio/voice/ja/Mio/` (1.1 MB): Yuki's five audition takes were copied over unchanged and the other eight were rendered the same way for 184 credits. Zero's audition rendered Intro.0/2, RelayOnline.0 and Homecoming.9/11 the same way for 759 credits; no Japanese library voice is tagged robotic, so its candidates span androgynous, childlike girl and boy. All 18 of Zero's lines then landed in `assets/audio/voice/ja/Zero/` (2.4 MB): Koume's five audition takes copied over and thirteen more for 453 credits.

## Common mistakes

- Writing tags or kana readings into `ja`: they belong in `tts`.
- Editing `Prologue.cs` and voicing anyway: run `--check` first; a stale key means the Japanese and its audio are both out of date.
- Committing audition audio: it stays in `artifacts/`.
- Trusting kanji readings: listen for proper nouns such as 星川 (ほしかわ), and fix a misreading with kana in `tts`.
