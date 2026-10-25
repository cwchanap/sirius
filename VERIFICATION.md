# MVP verification

## Settings: voice and music volume — 2026-10-25

The owner asked for a settings sub-menu to adjust the sound volume separately for the voice cast and the background music.

- **Code:** `scripts/Volume.cs` adds a `Voice` and a `Music` bus under Master at startup. The dialogue voice player and Mio's shout player (`Game.Bark`) play on `Voice`, every `Music.cs` track on `Music`, and the sound effects stay on Master. A volume is a whole number of steps, 0 to `Volume.Steps` (10), at `LinearToDb(level / 10)`, with 0 at -80 dB; it is kept in `user://settings.cfg` under `[audio]` beside the language, and `--self-test` ignores the saved values and never saves. `GameMode.Settings` opens from a Settings button on the title screen and the pause menu and closes back to whichever it came from (Esc or Back); two bare `HSlider`s take focus, the arrow keys, the d-pad and the mouse, and `Hud.Settings()` paints them as bars ticked at each step with a diamond at the end. Moving the Voice slider plays one of Mio's shouts if none is sounding.
- **Translation tables:** 92 interface strings to 95 in each of `ui.ja.json` and `ui.zh_TW.json` (Settings, Voice, Music), glossary rows added. 設, 楽 and 樂 needed `tools/build_fonts.py`; nothing was dropped.
- **Unit tests:** 47 pass (four new): each step quieter than the last and the bottom silent; both volumes start full; the screen's words translated in both tables.
- **Self-test:** **590 checks pass headless** in `en`, `ja` and `zh_TW` (cloud container, Godot 4.6.2 mono, dummy audio driver). New: both buses exist under Master at full volume; the voice player and every music track sit on their bus; the pause menu's Settings button opens the screen on the voice slider; through real key events, left lowers the voice bus one step and leaves the music alone, down moves to the music slider, three lefts set the music bus on its own, a bus above 0 is not muted, a slider at 0 mutes its bus, down from the last slider reaches Back, Esc and the Back button close to the pause menu, reopening shows the levels set; the title screen's Settings button opens it and Esc returns to the title; the volumes round-trip through a `ConfigFile` and an out-of-range saved value clamps. The fixed-width list adds the Settings button and the language loop draws the Settings screen in each language.
- **Captures:** Mesa software Vulkan under xvfb (`tools/review_languages.gd` now captures `settings` and `settings-title`); shapes and layout only.
- **Not yet:** the mouse was not exercised (headless and xvfb windows have no pointer, so injected clicks never reach the slider); nobody has heard the Voice slider's shout or the music change on speakers; no Metal run.

## Relay road and battle music from ElevenLabs — 2026-10-25

- **Tracks:** `assets/audio/relay.ogg` (40.0 s, 20 bars at 120 BPM, 593 KB) and `battle.ogg` (51.2 s, 32 bars at 150 BPM, 770 KB), 44.1 kHz stereo Vorbis, each exactly one loop cut from a 90 s ElevenLabs Music take on Hapadona's Mac (ASSETS.md has the requests and cut points). Imported `loop=true`, `loop_offset=0`, so the whole file repeats. They replace the synthesized `relay.wav` and `battle.wav`, and `tools/build_audio.py` no longer writes those.
- **Seams:** measured on the Mac (the cut points sit on bar lines and the wrap is sample-exact), and previewed as `artifacts/music/relay-ingame.mp3` and `battle-ingame.mp3` (each file three times). The owner listened to the first cuts (intro once, then the loop): the relay track was fine, and the battle track was to lose its intro. Nobody has listened to these one-loop files yet. Both files start mid-waveform, which could click on a cold start; in the game every track fades in from silence over 1.5 s, so it cannot.
- **Levels:** the new takes measure −10.6 LUFS (relay) and −10.3 LUFS (battle) against the placeholders' −17.9 and −18.3, so `Music.Tracks` plays both at −24 dB, which puts them where the placeholders were (about −34.5 LUFS). Set by measurement, not by ear.
- **Tests:** the self-test passes 571 checks headless in `en`, `ja` and `zh_TW` (cloud container, dummy audio driver); new: the relay and battle streams are Vorbis looping whole from the start. Unit tests pass 43 and e2e 4.
- **Metal** (Hapadona's Mac, M1 Pro, Mobile renderer, engine-injected input): the graphical self-test passes 571 checks in `en` on the PR's head, the new loop check and every music check among them, with no error or warning lines and no audio messages. The first launch after cloning the import cache into a fresh worktree stopped at "Chiyo idle animates the rig" (76 checks in, far from the music); the same build rerun unchanged passed in full.
- **Not yet:** nobody has heard the tracks in the game, so the −24 dB levels are untested by ear.

## Stardust, levels, stamina and the save point — 2026-10-25

The owner asked for one Souls-like currency for level-ups and purchases, levels raised a stat at a time at a save point (Hoshikawa's the only one), and the stats HP, SP (running spends it), ATK and DEF.

- **Code:** `scripts/Stats.cs` holds the stat table, every tunable (rewards 150 / 60, cost 100 + 50 per level, run drain 20 SP/s, regen 40 SP/s after 0.5 s, winded until 30%) and `Growth`; `Game.Rest()` / `GameMode.Rest` is the save point's screen at the well (`World.SavePoint`, named `World.SavePointName`); `Game.Die()` drops the carried stardust at the fall and `Game._Process` takes it back on touch. Items gained a `Price`; only the resonance cell has one, because remedies refill on rest.
- **Self-test:** 617 checks pass headless in `en`, `ja` and `zh_TW` in a cloud container (590 before). New: running spends stamina, run dry Mio only walks until it is back to 30%, then runs again, and a dodge roll with Run held spends none; a shut-down patrol or scout pays its reward, once; the well is lit and its prompt names it; E rests Mio, brings back the scout she shut down but not the patrols, and freezes play under its screen; Raise buttons for every stat, Enter on one spends the level's cost and fills the stat, the cost rises, buttons disable and Back takes focus when she cannot afford anything; ATK and DEF scale a sword hit and a strike; Buy fills the resonance cells to what she can carry; a fall drops the stardust where she fell, recovering at the village leaves it, walking back takes it, a second fall loses the first. The language checks add the new strings' widths and the new screen's redraw. The stamina drain is switched off for the long runs across the valley and checked on its own.
- **Unit tests:** 54 pass (47 before): the stat table, its translations, the cost curve, `Growth.Raise` / `Buy`.
- **Fonts:** `tools/build_fonts.py` re-cut the four subsets for the new text (the Japanese run first listed 井 戸 屑 倒 買 御 as missing); the self-test's glyph check is what caught it.
- **Not yet:** no Metal run and nobody has played it by hand: the stamina and stardust numbers are first guesses. The stardust has no icon of its own (a star drawn in code) and the well's glow is a sprite and a light, not art; neither has a sound of its own (both borrow `heal`).

## Background music by area and danger — 2026-10-25

The owner asked for livelier music on the relay road and battle music under danger.

- **Code:** `scripts/Music.cs` keeps one looping player per track and crossfades toward the one `Game._Process` wants: `battle` while any living robot is out of `PATROL` (chasing, winding up, striking, recovering, staggered, stunned or broken) and for `Music.Calm` (3 s) of play after, else `relay` on the Old Relay Road, else `valley`. A pause, the items screen or a scene keeps the fight's music; a defeat ends it. A faded track pauses where it is. `Game._ExitTree` now stops every `AudioStreamPlayer` below it, the music's included, keeping the shutdown leak fix.
- **Tracks:** `relay.wav` and `battle.wav` were synthesized placeholders from `tools/build_audio.py` (22.05 kHz mono, 1.2 MB together); the ElevenLabs tracks above replace them.
- **Self-test:** 570 checks pass headless in `en`, `ja` and `zh_TW` in a cloud container (Godot's dummy audio driver), after the rebase onto the item system. New: only the valley's track plays at the start; in the relay plaza the relay track is at its level and the valley's paused; Komorebi brings the valley's back; a pursuing robot brings in the battle track, which outlasts the robot's shutdown and then gives way to the relay track; restarting the prologue from the pause menu while a scout is after Mio drops the battle track for the valley's (this check failed before `Retry()` cleared the fight). The unit tests pass 43, two of them new: each region's track and the battle override, and every track's file exists.
- **Not yet:** nobody has listened to the music or the crossfades on speakers, and there was no Metal run.

## Items: an item table, an inventory and the items screen — 2026-10-25

- **What changed:** `scripts/Items.cs` holds the item table and `Inventory`, which `Player.Inventory` holds. Remedies and the remedy pouch move into it (H still uses a remedy straight away; the pouch is a key item that raises the remedies Mio carries to four), joined by the wrist terminal (a key item she starts with) and the resonance cell, a new supply that restores one Resonance Cut charge, of which she carries up to three and which resting does not refill. Two cells lie on the relay road, behind the terrace's patrol and at the forest valley's eastern bend; `World.Pickups` replaces `World.Pouches`. The items screen (`GameMode.Inventory`) opens with I over play or from the pause menu's new Items button, freezes play, and closes back to where it opened with I, Esc or Back: supplies and key items in slots, the selected one's icon, name, count and text, and E or Use to spend a supply. A pickup's banner carries its icon. The items borrow the remedy, resonance and Zero icons.
- **Translation tables:** 80 interface strings to 92 in each of `ui.ja.json` and `ui.zh_TW.json`: the screen's five words, four item names and four texts added, `REMEDY POUCH` removed, the pause screen's E/H/ESC line given I. New glossary rows for the cell, the terminal and the screen's words. The new characters (使品池滿耗聲返音) needed `tools/build_fonts.py`; the same run dropped 36 characters only the strings #21 removed had used.
- **Unit tests:** `dotnet test tests/unit` passes 41; new `ItemsTest`: the table is well formed (unique ids, icons on disk, consumables and only they have a use, `Raises` names an item), every name and text is in both tables, the starting kit, the pouch's raise, and a spent item keeping its slot until a reset.
- **Self-test:** **556 checks pass headless** in `en`, `ja` and `zh_TW` (cloud container, Godot 4.6.2 mono). New: the starting kit; the pouch's banner icon and key-item row; each cell on standable ground, taken once, not spent at full charges, restoring one; a recovery refilling remedies but not cells; a remedy blocked mid-attack keeping its count; through real key events, I opening the screen on the first slot with a slot per item, play frozen under it, E using the remedy shown, the arrow keys moving the selection, key items hiding Use, I closing to play; the pause menu's Items button, its Use button spending the last cell (whose slot stays, Use disabled), Esc and Back closing to the pause menu; restart restoring the pickups and the kit. The fixed-width list adds the screen's words and the longer controls line, and each item's name must fit beside its icon and its text wrap in five lines.
- **Captures** (software Vulkan under Xvfb, 1440×900; layout only): `tools/review_languages.gd` in all three languages now adds `hud-pickup` (the E prompt at a resonance cell), `hud-obtained` (its banner with the icon) and `items` / `items-cell` (the screen showing the remedy and the cell); every name, text, count and button sits in its box in each language. The script keeps Mio invulnerable for these, since the terrace's patrol killed her at the first cell.
- **E2E:** CI's godot-e2e job passes on the PR.
- **Metal** (Hapadona's Mac, engine-injected input, not a hands-on session): the graphical self-test passes 556 checks in `en`, `ja` and `zh_TW` with no error lines, and `review_languages.gd`'s captures match the cloud ones, except that the unselected slots' 1 px borders broke into gaps where the HUD draws below 1:1; they are now 2 px, the selected one 3 px. The cell on the ground is small and pale, easy to miss.
- **Rebased onto the combat animation** (2026-10-25): 562 checks pass headless in `en`, `ja` and `zh_TW` and the unit tests pass 41; the remedy check now waits out the longer keyed swing (70 frames) before healing. Not rerun on Metal.
- **Not done:** no hands-on check yet; the thicker borders are not recaptured on Metal.

## Combat: Mio's combo and Resonance Cut animations — 2026-10-24

The owner found the attacks and Resonance Cut lame and asked for a connected three-swing combo and a fancier skill, made in Blender.

- **Clips:** `tools/mio_combat.py` keys attack1–3 and skill on the Tripo rig by two-bone IK from a few poses per clip (hand, blade direction, hips, torso, feet); the other eight clips are still Tripo's. Rendered in the cloud with Blender 4.0.2 (`tools/preview_mio_combat.py`, Workbench contact sheets from the front and the side) and judged frame by frame: attack1 a high windup into a diagonal cut, attack2 a rising backhand ending raised over the right shoulder, attack3 a hop into an overhead slam and a deep lunge, skill a crouch, a leap spinning twice and a slam to one knee with the blade in the ground. Feet stay on the floor except in the hops; measured in Godot from the trial GLB, the foot bones keep 0.17 m and the toes 0.01 m off the ground through attack1 and attack2.
- **Timing:** the NLA export keys frame 1 at 1/24 s, so the clips key pose *f* at (*f* + 1)/24 s and Player.cs's hit, link and slam times match what is drawn; the first in-game recording, made before this, showed the hit-stop catching attack1 still at the top of its windup.
- **Self-test:** 536 checks pass headless in `en`, `ja` and `zh_TW` with a trial GLB (`preview_mio_combat.py -- --glb`, the committed model with the four clips swapped in, never committed). New: a landed swing starts a hit-stop that wears off; a second press chains into attack2 at attack1's link, well before its end; the trail emits during the swing, its ribbon holds the blade's tip and spans the outer 0.36 m of the blade; two swings lunge Mio 0.94 m forward; Resonance Cut's slam leaves its wave. The skill's recast waits grew to its 1.3 s plus the slam's hit-stop. Unit tests pass 35.
- **In game:** `tools/review_combat.gd` recorded the combo and the skill against a patrol with Movie Maker, under software Vulkan (lavapipe, xvfb) in the cloud and under Metal on the Mac, at 1280×800: chained swings with no return to idle between them, the third swing's gold ground ring, the skill's closing ring at the cast and its 5.5 m wave at the slam, and hit-stops of 3, 3 and 6 frames on the three blows (counted from frame differences).
- **Sword trail:** neither first recording showed it. Setting `TopLevel` in `_Ready` kept Mio's spawn transform, so the ribbon's global vertices were drawn about 31 m away; it now resets to the identity, and the self-test checks the ribbon's bounds hold the blade's tip. Read through a `BoneAttachment3D` it also trailed a frame behind the blade, up to 0.7 m in a swing; it now reads the hand bone's pose. Shown at full size it was a pale sheet over the whole sweep, so it now covers the outer half of the blade, stays jade, peaks at 60% and lasts 0.12 s (0.3 s through the skill). Checked in cloud captures of attack1.
- **Mac rebuild:** the first rebuild passed 536 checks headless in all three languages and under Metal (M1 Pro), but against the cloud trial its right arm differed by up to 30°: the importer pointed the clips' blade along the hilt's axis, the preview along the curved blade's hilt-to-tip chord. The importer now seats the sword before keying and both read the chord through `mio_combat.sword()`. Rebuilt again from that: `import_tripo_mio.py` printed TRIPO_EXPORT_COMPLETE, `check_mio_export.py` passed (65 bones, 12 clips, 47,980 triangles), `compress_textures.py` changed nothing, and the self-test passed 536 checks headless in `en`, `ja` and `zh_TW` and under Metal in `en`. Every arm, hand, spine, hip and leg rotation in attack1–3 and skill now matches the cloud trial within 0.22°. A first Japanese run stalled after HOSHIKAWA_READY while two other runs shared the machine; run alone it passed in 90 s.
- **Metal recording:** `review_combat.gd` under Metal, judged frame by frame: the jade trail ends at the sword's tip over the outer half of the blade in every swing, a tall arc in the leaping slam and a level band round Mio in the skill's spin; the blade stays in line with hand and forearm from the windup to the kneel, the feet stay planted except in the slam's leap, and nothing sinks or floats. With one sample per drawn frame the ribbon shows straight segments in the fast spin.
- **Effects:** after the owner approved the animation and asked for better effects, `scripts/Effects.cs` replaced the old burst (a growing torus and eight boxes) with flashes, sparks and cut streaks, and added dust, gathering motes, a column of light and a screen tint; the trail got a spline and a shader. Recorded with `review_combat.gd` under software Vulkan and judged frame by frame: each landed swing flashes on the plating with sparks flying past the robot and a thin streak along the blade's path, and the robot flares white for the hit-stop; the third swing's arc is gold and throws dust; the skill's motes close in during the crouch, the spin draws a level ring of jade round Mio, and the slam shows two rings, the column, dust, jade sparks and the stunned robot's crackle. The first take's column was an opaque white cylinder filling the close camera, so it is now thinner, half as bright, shorter-lived and drawn mostly at its edges, with Mio visible inside. Headless the dummy renderer lists no shader uniforms, so the effects fade through `SetShaderParameter` rather than a property tween. New self-test checks: a landed swing leaves sparks and a cut streak, the slam its column, and a stunned robot crackles.
- **Sound:** after the effects the owner asked for ElevenLabs sounds with Mio's voice. `swing`, `impact`, `guard` and `deflect` are now ElevenLabs takes, with a new `slam` at Resonance Cut's slam (the relay keeps the synthesized `skill`) and a `stun` short-circuit once per slam that catches a robot; Mio shouts in Yuki's voice on every swing and the finisher (at first the first two swings shouted only half the time, and the owner heard nothing on them in the recording, so now every swing shouts), calls 共鳴斬 at the cast and cries out when hit (ASSETS.md lists prompts and takes). Takes were picked by their spectrograms and waveforms, not by ear. The self-test passes **538 checks** headless in `en`, `ja` and `zh_TW`; new: every combat sound and Mio's five shouts load. Unit tests pass 35. The self-test mutes sound, so which sound plays when was checked in a `review_combat.gd` recording with its audio (software Vulkan): its spectrogram shows the swings, clangs and the third swing's せいやぁっ in the combo, then 共鳴斬 at the cast, the slam's boom 0.78 s later and the stun's crackle to the end. A verbose headless run of the same script first reported swing, impact, slam and stun wavs still in use at exit: leaving the tree only pauses a player, so a sound still playing at quit leaked, and the long stun makes that likely. `Game._ExitTree` now stops every audio player, not just the ambience and the voice, and the verbose run reports nothing in use. A Movie Maker recording still reports one resource in use at exit; Movie Maker mixes audio only as it writes each frame, so the stun still sounding at the last frame is probably never released there. A second recording, after every swing was made to shout, shows はっ and やっ on the first two swings in its spectrogram.
- **Rebased onto the UI redesign** (2026-10-25): 540 checks pass headless in `en`, `ja` and `zh_TW`, and the unit tests pass 35. The redesign dropped the Resonance Cut toast and shortened the empty-charges message to `REST TO RECHARGE`, and the rebase keeps both. Its `Game.Flash()` icon shared field names with the slam's screen tint, so the tint's fields are now `TintColor` and `TintTime`. Nothing was re-recorded or run under Metal after the rebase.
- **Not yet:** nothing has been played by hand, and nobody has listened to the new sounds on speakers.

## UI redesign: icons and keycaps instead of text — 2026-10-25

- **What changed:** the play HUD keeps three kinds of text: the objective (one line, with a crest per patrol on the relay road, dimmed as each is shut down), the name of what E acts on, and short banners. Region subtitles, the chapter line, the footer, the enemy plate's name and hint sentence, the vitals and skill labels, SENSORY LINK and the always-on controls panel are gone. Vitals: Mio's face cut round from her dialogue portrait, two bars, a pouch icon per remedy she can carry and an H keycap. Resonance Cut: its icon in a ring (lit while cast), a dot per charge (flashing red when tried with none left) and a Q keycap. The enemy plate has a panel, the SIRIUS crest and a TAB keycap until locked. Over a robot, a `Sprite3D` badge replaces the text label: 危 (perilous, red), finish and shutdown (gold), stunned (jade); the plain windup keeps its ◇. Deflect and posture break flash an icon in the middle of the screen; GUARD and the Resonance Cut toast are gone; a remedy shows +45 over Mio. Toasts are titles only, except the prologue's ending. The title screen draws the logo and the chapter name; pause drops its subtitle and shows the key list on a card; defeated drops its subtitle and puts an R keycap by its button; dialogue shows E and ESC keycaps and Zero's emblem image. Residents' name labels show only within 17 m of the camera. Mio's portrait now imports with mipmaps, since the HUD draws its face at about a fifth of its size.
- **Translation tables:** 134 interface strings to 80 in each of `ui.ja.json` and `ui.zh_TW.json`: 58 removed, 4 added (`Clear the old relay road`, `REST TO RECHARGE`, `Return to Hoshikawa`, `LADDER`), their wording taken from the strings they replace. No new characters, so the fonts were not rebuilt.
- **Unit tests:** `dotnet test tests/unit` passes 35. `EveryStringTheCodeShowsHasATranslation`'s floor on how many shown strings it finds drops from 80 to 50; it finds 58.
- **Self-test:** **534 checks pass headless** in `en`, `ja` and `zh_TW` (cloud container, Godot 4.6.2 mono). New: all 13 icons and the logo load; residents' labels have a visibility range; a stunned robot's badge is the stunned icon, it turns to shutdown once Mio stands behind it, and clears when she shuts it down. The ladder and pouch checks read the prompt's icon instead of its text. The fixed-width list drops the region and chapter lines and adds `Clear the old relay road`, `Return to Hoshikawa` and the title's chapter line.
- **Captures** (software Vulkan under Xvfb, 1440×900; shapes and layout only): `tools/review_languages.gd` in all three languages under `artifacts/i18n/<lang>/`, and a robot's stunned and shutdown badges under `artifacts/ui-code/`.
- **Metal:** on the owner's Mac (Apple M1 Pro, Metal 4.0, Mobile renderer) the graphical self-test passes 534 checks in `en`, `ja` and `zh_TW` with no error or warning lines. `review_languages.gd -- --lang=en` there draws every icon crisp on a transparent ground, the keycaps clear of their text, Mio's round face and the logo; the stunned badge over a patrol shows with no box in `hud-village.png`. In that run's `hud-patrol.png` the HOSHIKAWA region toast covered the patrol's badge: the script put Mio 0.9 m under the junction's floor, which pushed her back south over the region line. It now stands her on the floor found by a ray; the cloud recapture shows the stunned badge whole under the capture's own toast.
- **E2E:** CI's godot-e2e job passes on the PR.
- **Not done:** no hands-on check of how the icons read in play.

## Combat: robots shut down from behind — 2026-10-24

- **Rules:** a patrol has 500 health and a scout 300; from the front Mio's sword takes 2 health and 5 posture and no longer staggers it; attacking within 60° of its back (`Enemy.BehindDot`) in finisher reach shuts it down outright. A robot neither moves nor turns while it recovers after a swing, a deflect or a stun, whether or not Mio is still in its sight. Strikes take 50 vitality, 75 for the perilous sweep, 60% of that from scouts. Resonance Cut stuns every robot within 5.5 m for 4 s and deals no damage; it has 3 charges and no cooldown, refilled by `Player.Rest()`: Aki, Emi and `Respawn()` (recovering at the village, restart). Deflect, posture break and the frontal finisher are unchanged. The constants are at the top of `scripts/Enemy.cs` and `Player.MaxSkillCharges`.
- **Unit tests:** `dotnet test tests/unit` passes 35; new: only a robot's back within 60° counts as behind, whatever the height.
- **Self-test:** **532 checks pass headless** in `en`, `ja` and `zh_TW` in a cloud container with Godot 4.6.2 mono. New or changed checks: Emi refills the charges; a perilous hit takes 75 and a patrol's real windup strike 50; a frontal swing takes exactly the chip damage; Resonance Cut spends one charge, stuns at 4.2 m without harm, casts three times and then refuses; a robot facing Mio cannot be shut down even stunned, one with its back to her can, and the attack kills it; the stun holds and wears off under the robot's own physics; a recovering robot stays put and keeps its facing after Mio leaves its sight; a stunned robot keeps its back turned while Mio is placed behind it, and the attack key shuts it down. Not run graphically: no Metal run, no capture of the new HUD charge counter or robot hints, and no hands-on feel check of the numbers.

## Texture compression — 2026-10-22, updated 2026-10-23

Every model texture (the 87 Godot extracts from `assets/models/*.glb` and the 20 copies under `assets/art/*-import/`, 107 in all) now imports VRAM Compressed with mipmaps; the 34 normal maps are flagged as normal maps. The first pass compressed 80 maps, 25 of them normal maps. After the rebase onto the Tripo batches 1 and 2, `compress_textures.py` compressed their 27 maps (boulder, relay, barricade, viaduct pier and span, shelter, overlook, signpost and ladder, each Color, NormalGL and ORM), and no other `.import` changed. Nothing else changed: the UI, portraits, fonts, foliage atlas, bark and village atlases keep their settings. No Tripo or ElevenLabs credits were spent.

- **Format:**
  - S3TC (`compress/high_quality=false`): DXT1 for colour, DXT5 with alpha, BC5 (RGTC) for the normal maps.
  - BC7 (`high_quality=true`) was tried first and dropped: its CPU encoder had imported 32% of the maps after ten minutes, about 35 minutes for all of them, which a cold cache in CI's 30-minute e2e job could not afford. S3TC imports all 80 in 4.7 s on the M1 Pro.
- **Video memory** (Metal, the village square after `Start()`, `Performance.RENDER_*_MEM_USED`):

  | | Before | After |
  |---|---|---|
  | With batches 1 and 2: video memory | 2917.2 MB | 854.0 MB (−71%) |
  | With batches 1 and 2: textures | 2636.4 MB | 573.3 MB (4.6× smaller) |
  | With batches 1 and 2: buffers | 76.2 MB | 76.2 MB |
  | First pass: video memory | 2506.8 MB | 779.3 MB (−69%) |
  | First pass: textures | 2233.5 MB | 506.0 MB (4.4× smaller) |
  | First pass: buffers | 73.1 MB | 73.1 MB |

  "Before" with the batches is main at the batches' merge, measured in a checkout of the same tree (`artifacts/tex-compression/rebase2/memory-{main-uncompressed,after}.log`). The batches' nine models take 67.2 MB of maps compressed, against 301.9 MB uncompressed: 11.2 MB for each 2048² set (relay, viaduct pier and span, shelter, overlook) and 2.8 MB for each 1024² set (boulder, barricade, signpost, ladder).

  A 4096² colour map went from 67.1 MB to 11.2 MB, a 4096² normal map to 22.4 MB, and a Tripo prop's three 2048² maps to 11.2 MB together. Geometry was never the cost: every mesh in the valley holds 73 MB of buffers.
- **Still large:** the eleven 4096² character maps (Mio, Chiyo, KOMA-7, Sora, Nao, Toma) now take 156.8 MB: three BC5 normals at 22.4 MB, eight colour and metallic-roughness maps at 11.2 MB. Halved to 2048² they would take 39.2 MB, saving another 118 MB. Not done here.
- **Render cost:** `render_cost.gd` reports the same draws and primitives before and after at all three viewpoints. First pass: 1075 / 1017 / 1165 draws, 3,202,585 / 2,964,494 / 3,081,424 primitives. With the batches: 1223 / 1299 / 1304 draws, 2,891,446 / 2,701,026 / 2,715,223 primitives, exactly as batch 2 recorded them uncompressed.
- **Close-ups** (Metal, before/after under `artifacts/tex-compression/{before,after}/`):
  - `eaves-front`, `eaves-kura` and `house-kura-front`: the Tripo houses under the eaves.
  - `prop-well`: the well.
  - `mio-portrait-close`, `mio-portrait` and `dialogue`: Mio's face in the portrait views and the dialogue background.
  - `koma7/`: KOMA-7's front, side, back, face, idle and dialogue views.
  - Side-by-sides: `artifacts/tex-compression/{mio-face,prop-well,eaves-kura,koma7-face}-before-after.png`.
  - The batches' models (`artifacts/tex-compression/rebase2/{before,after}/`, in English): the shelter from its footpath and under its eaves, two signposts close up and the overlook's plaque. Side-by-sides: `artifacts/tex-compression/rebase2/*-before-after.png`, and `signpost-1-zoom3x-before-after.png` at three times size. The mean per-channel difference is 2–5 levels out of 255; the board's grain is a little softer with mipmaps, and shows no blocks even at three times size.
  - Result: no block artifacts at these distances. The mean per-channel difference is 1–11 levels out of 255, mostly the wind on the foliage and grass; KOMA-7's panels, eyes and leaf emblem match. Mio's skin is slightly smoother now that her maps have mipmaps, and her irises read a shade less saturated.
- **Self-test:** `SELF_TEST_PASS` headless and on Metal, 500 CHECK lines each, before the rebase onto the map redesign (`artifacts/tex-compression/self-test-{headless,metal}-en.log`). After it: headless, 521 CHECK lines (`self-test-headless-en-rebased.log`), and `just unit` passes 34 of 34. The Metal run was not repeated after that rebase. After the rebase onto the Tripo batches: `SELF_TEST_PASS` headless and on Metal, 521 CHECK lines each (`artifacts/tex-compression/rebase2/self-test-{headless,metal}-en.log`), and `just unit` passes 34 of 34.
- **Tooling:**
  - `tools/compress_textures.py` writes these settings into the `.import` files: every model texture, or only the images named.
  - Godot extracts a GLB's maps as Lossless and never re-extracts one whose `.import` already exists, so the settings can only follow a first import. `import_character.py` now imports in Godot (`--godot`, default `GODOT_MONO`), compresses the new model's maps and imports again.
  - Mio's own importer does not; after it, run `compress_textures.py` and re-import.
  - In this checkout the re-import after the second rebase recompressed batch 2's maps, but it kept the Lossless caches it already had for batch 1's twelve maps, though their `.import` files had changed. Deleting those twelve files from `.godot/imported/` made Godot import them again, compressed. A fresh checkout, as in CI, imports from the committed settings.

## Tripo batch 2: Cedar Pass and the roadside — 2026-10-23

The Cedar Pass shelter, the valley overlook, the signposts and the teahouse ladder are now Tripo models (140 credits, `ASSETS.md`). The Cedar Pass and canyon-choke boulders, the quarry sled and the pouch's crate reuse earlier models at no cost.

- **Placement:** the shelter's slab and the overlook's deck stand 3 cm proud of their levelled pads, as the old floor and deck did, with box colliders fitted to the slab, posts and bench, and to the deck, both rails and the bench. The shelter's slab is wider than the old floor, so it is centred on the pad: on its first placement its front edge reached past the pad, where the ground falls away, and the self-test caught Mio stopped by a 13 cm step. The shelter's whole mesh is a camera-only collider. The five signposts' lettering sits on the blank board on both faces. The ladder's walk box, hinge, trestle and swing are unchanged; only its mesh is new.
- **Self-test:** **521 checks pass** headless in `en`, `ja` and `zh_TW`, and graphically under Metal in `en` (`artifacts/tripo-batch2/logs/self-test-{headless-en,headless-ja,headless-zh_TW,metal-en}.log`). Normal walking still reaches the overlook deck and the shelter floor, and the ladder is still lowered from the terrace, walked down and up, and drawn up again by a restart. `just unit` passes 34.
- **Render cost** (`render_cost.gd`): draws 1223/1299/1304 at the village viewpoints (batch 1 recorded 1231/1307/1310), primitives 2.89M/2.70M/2.72M, CPU 0.80–0.83 ms. With the scene frozen, hiding batch 2's models (the shelter, overlook, six signpost models, ladder and 41 reused boulders) removes 43 draws and about 290k primitives at Cedar Pass (−8, −184), 18 draws at the overlook and 18 at the relay junction. Draw counts repeat exactly between runs; primitive counts drift by up to 120k at the overlook and the junction, so only Cedar Pass's is given.
- **Metal captures** (`artifacts/tripo-batch2/captures/` in Japanese, the Mac's own language, and `captures-en/` under `--lang=en`): the shelter from the footpath, under its eaves and from behind; the overlook, its bench and its plaque; each of the five signposts up close; and the ladder drawn up on its trestle and lowered, from the terrace and from the junction. Nothing floats or shows air under it, and the longest lettering, Komorebi's in English, fits inside the signpost's board frame.

## Tripo batch 1: the relay road's props — 2026-10-22

The relay road's last placeholder props are now Tripo models (250 credits, `ASSETS.md`): the boulder, the relay cabinet, the viaduct's pier and deck span, and the station road's barricade.

- **Placement:** the 13 rockfall boulders and the 27 at the cliffs' feet keep their positions and sizes, each resting its flat base on the lowest ground under its footprint, sunk 5% of its size. The relay's door faces the plaza with the Jade "Offline terminal" lit in its window, and the antenna mast stands behind it on the plinth. The nine piers reach from the deck down to 0.3 m under the plaza, or into the hillside, and the seven spans fill their 9 m bays, the bay over the road still missing. Three barricades close the station road at x 41. The piers, boulders, plinth, cabinet and barricades have fitted colliders (the old pillars and boulders had none); the spans, out of reach, still have none.
- **Self-test:** **521 checks pass**, headless and graphically under Metal, in `en` (`artifacts/tripo-relay/self-test-{headless,metal}-en.log`); the road east from the plaza still stops Mio at its barrier, and the relay check still reaches the relay from the plaza. `just unit` passes 34.
- **Render cost** (`render_cost.gd`): draws 1231/1307/1310, primitives 2.855M/2.661M/2.697M, CPU 0.88–0.97 ms. Hiding the new models in the village view removes 43 draws and 87k primitives (the viaduct 22 draws, the relay and barricades 7, the boulders 14); boulders are culled beyond 80 m. At the relay plaza (9, −158) the frame is 733 draws and 2.21M primitives.
- **Metal captures** (`artifacts/tripo-relay/captures/`): the relay from the front, beside its window, obliquely and from behind; the viaduct from the plaza, from either side and from under a span; the barrier from the road and close up; the rockfall from above and close up; and three scattered boulders. No model floats or shows air under it. One scattered boulder lies inside a quarry block, as its sphere did, and stays hidden there.

## Map redesign, phase 2: the relay road — 2026-10-22

The relay road between the river and the viaduct now runs 164 m (z −14…−178) through a block of mountain cut by trails, and Cedar Pass, the Cedar River with its valley and Komorebi moved 100 m north (the play area is now z −300…80). Everything is placeholder geometry or reused models; no Tripo, Codex or ElevenLabs credits were spent.

- **Layout:** `World.Trails` lists nine trails, each a polyline of (x, level, z) with a half width; `RelayGround()` makes each a level floor and raises the mountain between them, meeting at a cliff where floors touch at different levels. The junction just over the bridge; the east switchback up to the teahouse terrace on the cliff above the junction; the high road; the relay plaza under the broken viaduct; and the forest valley west from the junction to the old quarry's dead end. The station road leaves the plaza east beside the viaduct: the way in for the relay road's extension toward the station, closed in the prologue by a wooden barrier at x 41 (solid), a sign (`E · STATION / ROCKFALL · ROAD CLOSED`) and, behind it, a rockfall: the unpainted `Rockfall` trail is a talus ramp rising 4.2 m up the cutting's end, with thirteen boulders resting on the lowest ground under each.
- **Things on it:** the scatter (trees, boulders, grass and wildflowers) draws from its own generators (7207, 7211), and the countryside still draws, and drops, everything it once placed there, so the rest of the valley is unchanged. The countryside's first boulder is now its first one outside the relay road; the self-test pins it there. Lanterns every 15 m up the switchback and the high road, a signpost at the junction, the ruined teahouse (the Copper Kettle's Tripo model, `World.OldTeahouse`), quarry blocks and a sled, the remedy pouch on a crate, the relay and the viaduct.
- **Robots:** the three patrols guard the junction, the teahouse terrace and the plaza. Four scouts on `patrol.glb` (60 health, 0.86 scale, 60% damage, half the wander) watch the switchback's corner, the high road, the forest valley and the quarry; the mission counts only the patrols. A robot no longer pursues Mio on another level (more than 2.5 m above or below it), so the terrace patrol does not walk off its cliff.
- **Shortcut and pouch:** the teahouse ladder is hinged at the terrace's brow and, drawn up, lies back over the terrace on a trestle. Lowered from beside its hinge (`E` within 2.2 m of a point 1.8 m east of it and 1 m behind the brow), it swings up over the hinge and down at 37.5° to the junction and is walkable both ways. The pouch raises the remedies carried from 3 to 4. Both survive a recovery at the village; `Restart()` resets them.
- **Road paint:** `World.RoadGap()` goes into the terrain's UV2.y and the shader fades the paint from 2 to 3.3 m; the shader's copy of `RoadX` is gone. The minimap draws the painted trails across the relay road.
- **Unit tests:** `dotnet test tests/unit` passes 34. New: the paint runs unbroken from the shrine steps to Komorebi's hall; a flood fill from the square reaches no relay-road ground more than 2.8 m off a floor (the mountain above the cliffs); with the switchback's first leg blocked, the junction cannot reach the terrace and the quarry cannot reach the plaza. After the first Metal review: no step anywhere in the relay road's ground, walked in 5 cm strides along every metre line (it finds 286 on the version that review saw).
- **Self-test:** **521 checks pass headless** in `en`, `ja` and `zh_TW` in a cloud container with Godot 4.6.2 mono. New checks: the buildings per region; three patrols and four scouts; the walk up the switchback to the terrace, along the high road into the plaza and on to Komorebi; the drawn-up ladder blocks the junction and cannot be lowered from below; lowered from the terrace it walks down and back up; the pouch makes four remedies; both survive a recovery and reset on restart, even mid-swing; scouts are smaller and hit lighter; the quest completes with the scouts standing; the road east from the plaza stops Mio at its barrier.
- **Metal, first round (6b117b4, M1 Pro):** the self-test passed graphically with 519 checks. `render_cost.gd`: draws 1153/1216/1240 (phase 1: 1122/1065/1209), primitives 2.746M/2.507M/2.589M (phase 1: 3.200M/2.980M/3.122M), CPU 0.76–0.79 ms, 120 fps. Its captures found:
  - the drawn-up ladder standing as a free 10 m pole leaning back over the terrace: it now lies back on a trestle;
  - two straight trenches just outside the play area beside the relay road (3.9 m deep in the east, 1.5 m in the west), where the edge's 15–20 m ridges eased down to the uplands' 3.5 m before the cliff rose: an edge already 5–10 m up keeps its level, and the deepest closed hollow along both sides is now under 0.5 m;
  - a 0.5–0.7 m step across the mountain from (−62, −84) to (−3, −99), where the nearest point on the forest valley's trail jumped from one reach to another: `RelayGround()` now blends reach by reach rather than trail by trail;
  - a quarry block half in the wall and floating 1.2 m: it moved onto the floor, and each block now rests on the lowest ground under its corners;
  - a cedar pushing into the forest valley's wall: valley trees were kept within 3.8 m of the path.
- **Metal, second round (7a783d1):** the self-test passed graphically with 520 checks. `render_cost.gd`: draws 1144/1207/1232, primitives 2.722M/2.480M/2.570M, CPU 0.83–0.85 ms, 120 fps. All five fixes held: the drawn-up ladder lies about 17° back on its trestle, not buried at its hinge, and from the junction only its end shows at the brow; lowered, it rests from the brow onto the junction; every quarry block sits on the floor; along x = ±66 the ground rises outward and no hollow holds more than 0.12 m; the old crease line is a smooth, rounded ridge. It found:
  - the forest valley sparse, only two cedars left on its floor: its cedars are now placed along its path instead, 36 tries of narrow cedars (sugi only, 0.8–1.15 scale) 2.5–3.6 m either side, so it reads as a forest and their boughs keep off its walls;
  - cosmetic, left: the stone escarpment at x ≈ ±68, now standing on the relay road's high edges, runs ruler-straight for about 80 m; the broken viaduct reads as two gateposts with timber beams rather than a railway (placeholder geometry, for the art tasks).
- **Komorebi's region** now starts at z −232, where its level ground and its sign begin; gameplay screenshots on Metal showed the sign under the Cedar Pass banner.
- **Metal, third round (022393e):** the self-test passed with 521 checks (graphically in `en`, headless in `ja` and `zh_TW`). `render_cost.gd`: draws 1201/1264/1289, primitives 2.878M/2.646M/2.727M, CPU 0.77–0.80 ms, 120 fps; about 57 draws and 160k primitives more than the second round, most likely the valley's cedars. The station road's barrier, sign and boulders stand on the ground, the sign draws 封 in Traditional Chinese, the valley reads as a cedar-lined path with no bough in its walls, and at Komorebi's sign the banner reads Komorebi. It found:
  - the viaduct pillar at x 15.5 standing on the brow of the plaza's east cliff, its plaza side hanging up to 3 m over the slope (its mirror at x −11.5 by 1.3 m): each pillar now stands on the lowest ground under its footprint;
  - seven pairs of valley cedars 0.7–2.4 m apart, their trunks doubled: a cedar now keeps 3 m from the others, over 48 tries (26 stand, none closer than 2.4 m);
  - the rockfall, six boulders at the foot of the cut's end wall, reading as a cul-de-sac: seven larger boulders now heap up the end wall behind them;
  - cosmetic, left: the boulders are the stone atlas on spheres, like every boulder in the valley, until the rock outcrops art task.
- **Metal, fourth round (f53c966):** the self-test passed graphically with 521 checks. `render_cost.gd`: draws 1199/1262/1287, primitives 2.869M/2.636M/2.717M, CPU 0.80–0.81 ms, 120 fps. Every pillar is buried 0.5 m at its lowest corner, and the 26 valley cedars stand at least 3.07 m apart. It found:
  - the heaped boulders set at the ground under their centres, so three hung on the cut's end wall with up to 5 m of air under them and three sat on the plateau above: the cut now ends in a talus ramp (the `Rockfall` trail) and each boulder rests on the lowest ground under it;
  - four pairs of brow trees on top of the relay block 1–1.8 m apart: brow trees and valley cedars now share the 3 m spacing;
  - cosmetic, left: one cedar's long lower bough crosses the view at eye level mid-valley.
- **Metal, fifth round (0027dc3):** the self-test passed graphically with 521 checks. `render_cost.gd`: draws 1190/1253/1278, primitives 2.823M/2.590M/2.677M, CPU 0.77–0.81 ms, 120 fps (the thinned brow trees). The station road now reads as a road buried under a slide: the talus climbs from 4.3 m to a landing at 8.5 m and the thirteen boulders heap up it to the brow behind the barrier, none with air under it. The cliffs above the ramp leave the cut's end in a rounded grass hump 4–5 m above the block's brow, merging into the slope that rises east. Of the 92 trees on the relay block, none stands within 3 m of another.

## Map redesign, phase 1: the valley's natural edges — 2026-10-22

The valley is now closed by its own terrain, and its water follows the land. Everything inside the play area (|x| ≤ 64, z −200…80) keeps its place. No Tripo, Codex or ElevenLabs credits were spent.

- **Edges:**
  - Beyond the play area `Outside()` carries the edge's height outward into an 8–12 m cliff and the mountains behind it.
  - Under the mountains the ground eases within 12 m of the edge to a level 3.5 m upland, so the river beds no longer run on as straight trenches across the range (the first Metal review measured them at 3–4 m deep). Nothing at the edge carries on as a crease either: the second review saw faint lines running out from the play area's corners, where the old channel filling switched direction.
  - South-west of Hoshikawa the ground runs down a beach under the sea instead. Four rocky capes break the shore, each narrowing and sinking towards its tip. Further from the valley, the headlands ease into mountainsides instead of standing as sheer sea cliffs.
  - The `Valley boundary` boxes and the ridge mesh are gone. The machine-city silhouette stands behind the eastern range (x 150–174).
- **Water:**
  - Both valley rivers flow west.
  - The Hoshikawa springs at 35 m in a fold of the eastern range and bends down its slope as a torrent. It drops over two falls of about 6 m into its gorge, crosses the valley at sea level and enters the sea.
  - The Cedar River springs at 45 m just under the range's 52 m summit further north, runs straight down its fall line and falls 9 m and then 5 m into Cedar Pass. It leaves through a slot in the western hill and a canyon closed by a boulder choke, then runs south-west down its own valley to a mouth on the bay, 70 m west of the Hoshikawa's.
  - Every fall has straight reaches on both sides. In the first Metal review, a bend just above each fall let the upper gorge floor show through the curtain.
  - Deep gorges open into V-shaped valleys, and above the falls into broader mountain valleys.
  - The second Metal review found the headwater gorges dead straight with squared-off heads, like cut channels. Each headwater now follows the range's folds: the Hoshikawa its natural valley line, the Cedar its fall line, which it used to cross at an angle. The floors wander in width. A spring's ravine starts as a fold in the hillside and cuts down over its first 24 m, where the water appears. Where a reach would run with the hillside lower than its water, its bank stands as a 2 m levee instead of letting it spill.
  - The walls follow a level and distance smoothed over the course, so bends leave no creases on them. A fall's step crosses the walls near the river and fades out within about 20 m.
  - Steep reaches (17–43%) now read as white water. Seen from below they used to read as pale gravel roads: the shader measured depth vertically, and up a steep reach that comes to nothing, so the water was clear. Depth is now measured square to each cell's sloped surface, and reaches steeper than about 1 in 8 break into streaks racing downhill. From above, the fourth review saw those streaks change direction cell by cell; they now run along each reach, and neighbouring cells share their normals.
  - A reach's water fills whatever of its ground lies under its level within 16 m of its course and is joined side to side to it. Water near the Cedar's mouth used to stop short of its banks in 4 m steps. The third Metal review found a pool standing in a hollow beyond the Cedar's levee beside its upper fall, cut off from the channel; such hollows now stay dry. The fourth review still saw the hollow painted as wet mud, under a thin sheet of water that the 4 m cells carried across the levee: joining is now traced on 1 m cells even under the 4 m water, and outside the play area the wet band is painted only around water that is there.
  - Outside the play area, water is laid on the terrain's own 1 m or 4 m grid, so no shore can surface.
  - The only invisible walls are off the beach (0.5 m deep) and behind the boulder choke.
- **Rendering fixes from the two Metal reviews:**
  - The 4 m terrain's cells along the 1 m ground's edge fan out to its vertices, so the two meshes share their edge. The first review saw sky through cracks at the join, and the second saw a dark notch at the waterline where the skirt that replaced those cracks showed. The skirt is gone, and a 4 m water cell on the join now counts the 1 m vertices too.
  - The wet band at every waterline is painted per pixel from the ground's height above the water (`World.Shore()`, carried in the terrain's UV2), so its edge follows the ground's own contour. Painted per vertex, it zigzagged across the 1 m triangles on the canyon wall behind the choke; widening it on steep rock only reduced that, and the third review still saw a sawtooth there. Inside the valley the band along the river and brook is a little narrower than before, now exactly the 35 cm it was meant to be.
  - The fourth review found a dry ring round the paddies, where the ground just outside a floor read as far from water. Ground away from any water in the valley now reads as the band's edge, so a paddy's wet floor fades out across one cell, as it did before. Beyond the valley it still reads as far from water: there the same fade spread over a 4 m cell, and the fifth review saw 2–3 m brown halos round every gorge reach. The sixth then saw no wet edge at all on those banks, the grass showing through the shallow margins, because a 4 m bank vertex beside the water read as far from it too; ground within one 4 m cell of a reach's water and above its level now carries its own height up the band, as in the fourth version. The seventh found the ground under the Cedar's upper curtain painted as grass, showing through it, because no reach's water covers it; the ground under each fall's curtain and a metre either side now counts as joined, so it reads wet.
  - `terrain.gdshader` samples the atlas with gradients from the unwrapped uv. It stops at mip 3 and fades into each cell's mean colour by mip 5, so the dashed grid along tile edges on distant slopes is gone.
  - Rock is projected from whichever side faces it, so cliffs are no longer streaked.
  - The far sea planes run 2 km west and 1.3 km south of the range, past any view. From the Cedar mouth the second review saw the old plane's straight end.
- **Unit tests:** `dotnet test tests/unit` passes 32. The new ones check that:
  - rivers never run uphill;
  - each course ends in a valley river or the sea at its level;
  - no reach, steep ones included, runs dry;
  - a flood fill from the square cannot reach more than 16 m past the edge.

  The flood fill counts ground under 50° as standable, allows a 1.24 m jump step, lets anything steeper be slid down only, and treats the walls as blocking.
- **Self-test:** **502 checks pass headless** in `en`, `ja` and `zh_TW` (`SELF_TEST_PASS` each), in a cloud container with Godot 4.6.2 mono. The first and third to eighth versions also passed natively on Metal (M1 Pro, 502 checks each). The old "northern map edge remains physically closed" check is replaced. New checks:
  - both rivers flow west;
  - the sea is still and at the river's level, outside the play area;
  - four falls run the waterfall shader;
  - every course has gorge reaches;
  - a chest-height ray north of Komorebi hits `Terrain collision`, and no boundary box exists;
  - a chest-height ray west into a cove hits `Invisible wall` where the seabed is more than 0.4 m under the water.
- **E2E:** `dotnet test tests/e2e` passes 4 against the headless game (godot-e2e v0.1.2).
- **Render cost** (`render_cost.gd`'s three village viewpoints):
  - First version, on Metal: draws 1118/1062/1208, primitives 3.225M/2.995M/3.154M, CPU median 0.73–0.82 ms, 175–215 fps.
  - Second version (92696dd), on Metal: draws 1123/1069/1212, primitives 3.22M/3.00M/3.12M, CPU median 0.77–0.81 ms, 118–120 fps at the display's cap.
  - Third version (789aec2), on Metal: draws 1123/1066/1210, primitives 3.201M/2.980M/3.122M, CPU median 0.73–0.78 ms, 120 fps at the display's cap.
  - Fourth version (83d4cac), on Metal: draws 1122/1065/1209, primitives 3.200M/2.980M/3.122M, CPU median 0.72–0.78 ms, 120 fps at the display's cap.
  - Fifth version (9181132), on Metal: the same counts as the fourth to within 8 primitives, 120 fps at the display's cap.
  - Under Mesa's software Vulkan in the cloud, 1b7fd57 → the first version: draws 1047/1002/1118 → 1126/1085/1194, primitives 3.086M/2.981M/2.839M → 3.104M/3.009M/2.881M. This version's first viewpoint reads draws 1123, primitives 3.201M; the run timed out at 1 fps before the other two.
- **Captures:**
  - The first version's ten `review_map.gd` views were taken natively on Metal. That review found the trenches, the cracks at the join, the tile grid, the notched curtains, the rectangular coast and the Cedar outlet's canal-like route, all fixed above.
  - The second version's twelve views were taken natively on Metal. That review found the pale steep reaches, the straight headwater gorges, the creases on the gorge walls and at the corners, the notch and the zigzag at the join, the sea plane's end and the stepped water near the Cedar's mouth, all fixed above.
  - The third version's twelve views, on Metal, confirmed those fixes and found two things left: the cut-off pool beside the Cedar's upper fall and a reduced sawtooth behind the choke, both fixed above. It also noted that the steep reaches' outlines step a metre cell at a time, visible from mid-distance and from the air.
  - The fourth version's views, on Metal, confirmed the pool and the sawtooth gone and found the dry ring round the paddies, the hollow's mud and water sheet, and the streaks' patchwork, all fixed above.
  - The fifth version's views, on Metal, confirmed those three fixed and found the brown halos on the 4 m ground, fixed above.
  - The sixth version's views (b89a4c9), on Metal, confirmed the halos gone and the earlier fixes holding, and found the 4 m banks with no wet edge, fixed above.
  - The seventh version's views (dc8f44c), on Metal, matched the fourth's wet edge with the hollow still dry, and found grass under the Cedar's upper curtain, fixed above.
  - The eighth version's views (c3c164b), on Metal, with close-ups of both upper falls, confirmed wet ground under every curtain, with the rock beside each reading as spray-wet, and found nothing new.
  - Each round of fixes was checked in the same twelve views under Mesa's software Vulkan (lavapipe, `xvfb-run`), which shows shapes and placement only.
- **Not re-measured:** render cost after the fifth version. The sixth to eighth changed only the wet band's input, not the geometry.
- **Limits:**
  - The cliffs still use the stone atlas, so they read as coursed masonry. A cliff texture is the separate art task A1.
  - The cliff tops are sharp creases.
  - Distant terrain beyond mip 5 is each material's flat mean colour.
  - Steep reaches are sloped sheets of white water, not stepped cascades; their outlines step a metre cell at a time, and their streaks turn abruptly where one reach meets the next.
  - Where the 4 m water meets the 1 m water (x = ±104), as above the Cedar's upper fall, its edge steps 4 m at a time.
  - The Cedar's steep stone near bank in the village shows almost no wet edge: the band is 35 cm high, and that bank is nearly vertical.
  - Up close, the terrain's 1 m cells show as a row of small brown spikes along the foot of each fall's curtain.
  - From the air the capes are flat-topped shelves. From the ground they read as rocky headlands.
  - The machine city's boxes look like a bar chart when there is no fog.
  - No physical keyboard/mouse session is claimed.

## Old-model cleanup — 2026-10-21

Every authored (pre-Tripo) model leftover is removed except `villager.glb` and `patrol.glb`, which stay on purpose. No Tripo or Codex credits were spent, and no game code changed.

- **Kept:**
  - `villager.glb` (58 bones): Ren, Aki, Hana, Emi and Yui, the five residents with no `World.TripoResidents` entry.
  - `patrol.glb`: the three hostile robots.
  - Their Blender build: `build_characters.py` (patrol), `refine_humans.py` (villager) and `amber-iris.png`.
  - The 2D Mio key art and turnaround sheets, which are design references rather than models.
- **Removed:**
  - 28 textures (about 50 MB) that Godot had extracted from old GLB exports of Mio, Chiyo and KOMA-7. None is embedded in a current GLB, and a fresh `--import` recreated none.
  - Every authored-Mio texture (face, hair, skin, fabric and iris paint, with their generation records, material notes and turnaround landmarks).
  - The earlier dense Tripo Mio's two 4K textures.
  - The authored Mio's build scripts `mio_illustrated.py` and `mio_turnaround.py`.
  - The authored Mio's Blender QA scripts `check_mio_{eye_cheek,eye_rim,hair,lids,orbit_contour}.py` and `render_human_review.py`.
- **`refine_humans.py`:**
  - It now builds only the villager. Its hero (Mio) branches, scarf and sword are gone.
  - It saves `assets/source/villager.blend`. Previously it overwrote `characters.blend`, whose `HumanV2_mio` scene `import_tripo_mio.py` still takes Mio's sword from.
  - Built in a scratch copy with factory-startup Blender, the old and trimmed scripts give the same `villager.glb`: identical glTF JSON (apart from Blender's `.NNN` name suffixes), vertex, UV, weight, skin and animation data, and the same triangles with the same winding. Only the triangle order in three index buffers differs, and it differs between the old script and the committed GLB too.
  - The committed `villager.glb` is unchanged.
- **Checks:** `dotnet build` and a headless `--import` are clean. The self-test passes 500 checks headless in `en`, `ja` and `zh_TW` (`SELF_TEST_PASS` each). The last native Metal run passed 462 checks before the rebase onto the language PR and was not repeated. `just unit` passes 28 and `just e2e` passes 4.

## Komorebi revamp — 2026-10-21

Komorebi redone to Hoshikawa's standard with existing assets and one new Blender tree: no Tripo or Codex credits. Hoshikawa is unchanged. Its well, notice board, handcart, scarecrow and hasa rack moved into shared helpers that add the same nodes in the same order, and its seeded streams draw as before.

- **Houses:** `World.KomorebiHouses` places the five buildings through `Minka()` with Hoshikawa's styles and sizes:
  - the lodge is the thatched farmhouse;
  - the Mountain Clinic is the clinic;
  - the Weavers House is the workshop;
  - the Orchard Store is the kura;
  - the meeting hall is the hall.

  The lodge, clinic, weavers and store face the road. The hall faces down the road from its north end, its back at z −181.78, inside the level ground (z > −182). Komorebi's ground is level, so the houses have no pads. The old `House()` and its awnings, banner, chest and worktable are gone. Door paths run from the road to the lodge and the clinic, and one crosses the road from the weavers' door to the store's.
- **Residents:** Nao, Emi and Yui stand beside their door paths facing the road, and Toma faces the east lane. Their lines are unchanged.
- **Square:**
  - the basin and box benches became a Tripo well (the same colliders and camera-only roof as Hoshikawa's), a notice board and two bench props;
  - two stone lanterns flank the hall;
  - six lamps replace seven;
  - the north festival masts moved to z −166.
- **Courtyards:** each of the three courtyard keyaki has three shrubs in place of nine sphere plantings, drawn from Komorebi's own generator (seed 5101).
- **Orchard:** a new `kaki` persimmon tree from `tools/build_props.py` (2,368 / 2,576 triangles, keyaki bark and leaves, no new material). Rebuilding left every other tree GLB byte-identical. Each tree consumes three `rng` draws, as `OrchardTree()` did. Fourteen persimmons per tree hang on the crown's lower outside as merged spheres; the sphere canopies and stems are gone.
- **Packing station:** the box table became a Tripo handcart, three barrels and a hasa rack along the fence. Six strings of hoshigaki hang under the store's front eave.
- **Gardens:** a Tripo scarecrow stands in the nearer cabbage bed.
- **Self-test:** **500 checks pass headless** in `en`, `ja` and `zh_TW` (phase 4d had 385). The last native Metal run passed 462 checks before the rebase onto the language PR and was not repeated (Forward Mobile, Apple M1 Pro). The house checks now run over both tables, replacing "Komorebi keeps its buildings":
  - each house sits on level ground;
  - it is Tripo's textured, brightened style with its footprint on the ground;
  - its sign hangs under its eaves;
  - its eaves stop the camera but not the player;
  - its front wall stops the player.

  New checks:
  - the Komorebi well is checked like the village well;
  - both stone lanterns, the handcart, the notice board and the scarecrow are textured, brightened, grounded and solid;
  - there are nine persimmon trees and nine courtyard shrubs;
  - 126 persimmons hang inside the orchard's crowns, the lowest 1.37 m up;
  - Komorebi's trees are grounded and meet the ground round their trunks.

  The walk to each resident takes the new approaches. `just unit` passes 28 cases (new: Komorebi houses stand on level ground, off the road except the hall, and in the Komorebi region). `just e2e` passes 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`):
  - at the three Hoshikawa viewpoints, draws 1086/1034/1159 → 1044/986/1134 and primitives 3.124M/2.872M/3.023M → 3.204M/2.965M/3.083M (Komorebi is in view far up the valley);
  - an ad-hoc run at Komorebi (0, 4.6, −138) and (0, 4.6, −158): draws 350/193 → 303/173, primitives 1.39M/1.06M → 1.87M/1.35M;
  - CPU median 0.24–0.75 ms.
- **Captures:** before (b573c77) and after pairs, in native Metal, of 20 `review_revamp.gd` views, in `captures/komorebi-revamp/`:
  - the overview from the Cedar Bridge side, the road, the square, the well, the hall, the orchard, the persimmons, the packing station, the scarecrow and the beds;
  - each house from the front and through the gameplay camera under its eaves.
- **Limits:**
  - The signpost and the south festival masts are still boxes.
  - The persimmons are spheres, not part of the tree model.
  - The handcart carries Hoshikawa's rice bales.
  - The hall stands on the road's painted end.
  - The kura's eaves view looks down over its roof, as Hoshikawa's does.
  - No physical keyboard/mouse session is claimed.

## Languages: Japanese and Traditional Chinese — 2026-10-21

The game now runs in English, Japanese and Traditional Chinese (Taiwan wording, `zh_TW`). Every drawn string goes through `Localization`: UI text is keyed by its English wording and registered with the `TranslationServer`, dialogue by scene position (`Intro.1`). Game state (speaker, script, objective, region, resident names) stays English, so quests and the self-test do not depend on the language. The Japanese voice plays under all three.

- **Translations:** 70 dialogue lines and 121 UI strings per language, written by Claude, not by a native speaker. The Chinese kanji names (蓮 千代 秋 花 空 直 繪美 冬馬 結衣, 零, 天狼, 千代奶奶) are proposals marked **?** in `assets/dialogue/glossary.md`.
- **Language choice:** `--lang=` beats `user://settings.cfg`, which beats the OS locale. The self-test forces English unless `--lang=` is given. The menu buttons on the title and pause screens switch at runtime and save the choice.
- **Fonts:** Noto Sans/Serif JP and TC subsets (1.3 MB, OFL) built by `tools/build_fonts.py`. Each language uses its own face, so Han characters take the right regional shapes; the language buttons draw each name in its own face, and the subset includes those names (繁 and 文 were missing until a review found it; a machine with CJK system fonts had hidden it by falling back).
- **Unit tests:** `just unit` passes 27 cases (16 before). The new ones check that every string the code shows has a translation, that every beat matches the English script, that ja and zh_TW translate the same keys, that format placeholders survive, that no translation is left over from removed code, and the language choice order.
- **Self-test:** **423 checks pass headless in `en`, `ja` and `zh_TW`** (385 before; the 38 new ones cover glyph coverage, the language buttons' own names in their own faces, fixed-width boxes, wrapping, labels, buttons, which button holds focus on the title and pause screens, restart and language switching). `just self-test` and CI run all three.
- **Mutation check:** with a placeholder broken in `ui.zh_TW.json` and `Homecoming.7` deleted from `zh_TW.json`, three unit cases fail (beat coverage, placeholder match, ja/zh key parity); restoring both files passes all 27.
- **Captures:** `tools/review_languages.gd --lang=<code>` writes 14 views per language into `artifacts/i18n/<code>/`: title, both intro scenes, the Ren and Zero briefing, Hana and KOMA-7, Chiyo's narration, Nao's lodge, the patrol and village HUD, the pause menu, the defeat screen and the two homecoming scenes. They were taken with software GL in the cloud (Xvfb, `gl_compatibility`, Mesa llvmpipe), not Metal. I looked at the HUD, pause, defeat and dialogue views in Japanese and Chinese and found the panels, toast, menu buttons and text box holding their text with no overflow; English matches what it was.
- **Limits:**
  - No native-speaker review of either language.
  - No Metal or physical keyboard/mouse run of this change. A graphical run on the Mac is still to do.
  - Zero's six relay reminders have Japanese and Chinese text but no voice (`Prologue.Unvoiced`).
  - There is no `--lang=pseudo` mode.
  - `just e2e` was not run here; CI runs it.

## Mio polish, second pass — 2026-10-20

From the first pass's summary image the user asked for more polish. This pass took the flaws visible there.

- **Face:** flat-texture close-ups before and after show the pale patch and grey smudge beside her left eye, the strand strokes crossing both brows, pale squares on her right cheek and small cheek flecks gone. The first pass had filled from only the lightest skin; this one solves a smooth fill from the surrounding skin texels. Hair-strand faces at her left temple keep the bake's grey.
- **Grip:** the measured hilt ran along the hand, within 11° of the bone from wrist to knuckles, so the closed fingers wrapped nothing. The importer now fits a circle to each curled finger and seats the hilt through their centres, blade out of the index side. Blender close-ups in idle, guard, attack1 and walk show the hilt across the fist; in-game captures under Metal show the guard block held horizontally and the ready stance with the blade upright.
- **Ready stance:** the attack1 frame raised her left hand to her head. The stance now takes the held guard's left arm, at her waist; the combo sheet shows all three attacks starting and ending in it.
- **Dodge:** the roll spends less time lying on her back. Game frame 10 now samples source frame 34 (sitting up) instead of 32.4, and frames 11-15 give the rise the time. A first version jumped from source frame 26 to 34 in one game frame, turning her hips 107° in 40 ms (review on PR #10); the even speed-up keeps the largest per-frame hip turn at 64°, against 55° for even sampling.
- **Checks:** `check_mio_export.py` passes (65 bones, 12 clips, 47,980 triangles). After rebasing on main's environment revamp, **385 checks pass headless and in native Metal**, both `SELF_TEST_PASS`; `just e2e` passes 4 and `just unit` 16.
- **Evidence:** `artifacts/mio-polish/mio-polish2-summary.jpg`, `ingame2.jpg`, `selftest2-*.log`, `build2.log`; scripts in `artifacts/mio-polish/second-pass/`.


## Environment revamp, phase 4d boat and washing steps — 2026-10-20

Two Tripo props on the river's village bank: 80 credits, balance 475 → 395 (task ids and prompts in ASSETS.md). The river's course is unchanged. No building or character changed, and the seeded streams draw as before; only where they place is filtered.

- **Boat:** Tripo's flat-bottomed boat (7,854 triangles) floats at `World.BoatRest`, x −13, bow upstream. Its keel is 0.14 m under the water and 0.25 m over the bed, and every vertex is inside the river sheet, more than 5.5 m from the bridge. `World._Process` bobs it in place (2.5 cm heave, about 1.5° roll and 0.5° pitch on unrelated periods), and a box collider rides with it. A `field_post` stands at the water's edge 2.6 m upstream, with a box collider; two stretched `field_rail` lengths make the rope sagging to the bow.
- **Washing steps:** Tripo's steps (8,826 triangles) descend the higher bank at `World.Kawabata`, x 10. Four treads land at 0.50, 0.30, 0.11 and −0.09 m, and the slab at −0.30 m, 8 cm over the water. `HamletGround()` cuts the bank under the treads, blending in 0.35 m inside the walls. The river's edge at z −4 there is now 0.03 m, still 0.41 m over the water, and the highest edge anywhere is still −0.106 m. The colliders are a top-tread box, a ramp under the treads' nosings, a slab box and a sloped box for each wall.
- **Clearing:** `World.Landing()` keeps reeds and river stones off both spots. The river's reeds drop from 5,976 to 5,814 blades and its stones from 25 to 23; every draw still runs, so the rest keep their places. The Cedar River is unchanged.
- **Self-test:** **385 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 4c had 379). The new checks, beside the river ones:
  - the boat is Tripo's textured, brightened model under 10k triangles, its keel under the water and over the bed, its gunwale 0.4 m above the water, all inside the river sheet and clear of the bridge;
  - it bobs in place within its 2.5 cm heave;
  - a ray meets its collider;
  - the mooring post stands on `Height()`, and its rope runs from the post to within 0.25 m of the hull;
  - the steps are Tripo's textured, brightened model under 10k triangles, their base under the bank or the water;
  - Mio walks from the bank down to the slab (−0.30 m) and back up to the bank (above 0.45 m), on the floor.

  The reeds check now also keeps the landings clear.

  The rig check for each Tripo resident's idle now seeks the clip to 0 before stepping it 1 s. `ActorVisual.Play(..., restart: true)` does not restart a clip that is already playing. The comparison therefore depended on how long earlier checks had run, and the boat's 20-frame bob check moved Chiyo's idle onto a matching pose.

  `just unit` passes 16 cases and `just e2e` passes 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`):
  - draws 1082/1030/1155 → 1086/1034/1159 (the two models and their shadows);
  - primitives 3.097M/2.827M/3.013M → 3.124M/2.872M/3.023M;
  - CPU median 0.69–0.75 ms;
  - fps capped at 120.
- **Captures:** before (45646c1) and after pairs, in native Metal, of six `review_revamp.gd` views, in `captures/hoshikawa-phase4d-boat-steps/`:
  - new: boat-bank, boat-far, kawabata-bank and kawabata-far;
  - river-bridge and bridge-village-bank.

  Two after-only close-ups: boat-close, and kawabata-close with Mio on the slab. `pipeline.jpg` puts the Codex references beside Tripo's previews.
- **Limits:**
  - The boat only bobs. It does not drift on the current, its rope does not move with it, and it cannot be boarded.
  - The washing steps' collision is a ramp, so walkers glide down rather than step.
  - No water ripples round the boat or the slab.
  - No physical keyboard/mouse session is claimed.

## Environment revamp, phase 4c bridges and banks — 2026-10-20

Both river bridges rebuilt and the river banks dressed, with no Tripo credits spent and the river's course unchanged. No building, character or existing seeded scatter stream changed.

- **Bridges:** `World.TimberBridge()` builds both from props:
  - 21 and 33 `plank` boards (new, 12 triangles, one atlas board wide and one tile long, so no seam crosses the deck), each jittered and every other one turned end for end (own generator 4441);
  - three `field_rail` log stringers, and a log cap on each pair of stone piers;
  - `field_post` posts every 2 m, with a top rail and a knee rail each side.

  The walkers' colliders are the flat bridges' own: one slab with its top at the old deck height (0.82 m and 4.1 m) and a box along each top rail. The stone piers, now footed 0.4 m into the ground, are solid.
- **Stone bridge:** `Height()`'s causeway now stops at the terrain mesh's z −13 and −3 rows, so the river runs open under the deck. Two stone abutments hide the one-metre slopes down to the natural banks, and two bents of piers stand in the water. Under the deck the bed is 1.5 m below the water.
- **Banks:**
  - The Cedar River gets reeds (own generators 4451/4455; 2,538 blades) at its waterline, clear of the Cedar Bridge's embankments.
  - Both rivers get pebble spills just above the waterline and the odd half-sunk rock just below it (own generator 4461): 15 along the Cedar River and 25 along the river, clear of both bridges and the brook's mouth.
  - The waterline search is now `Waterline()`, shared with the river reeds, whose 5,976 blades are unchanged.
  - Pebble spills, half-sunk rocks and bank stones now get numbered names, so the water wheel's clearance check sees every one, not just the first.
- **Self-test:** **379 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 4b had 375). The new checks:
  - each bridge is its planks on three stringers over stone piers, footed at least 0.3 m in the ground and under the deck;
  - a ray meets the stone bridge's deck at 0.82 m over a bed more than 1 m under the water;
  - the Cedar River's reeds stand only where it holds water and clear of its bridge;
  - more than ten stones line each river, none within 5.5 m of a bridge.

  The baked-props check now lists the plank. The terrain continuity check's bridge exemption now covers the approaches dropping into the river. Both existing walks still cross the decks: village to countryside, and relay over Cedar Bridge into Komorebi (deck floor ≥ 3.8 m). `just unit` passes 16 cases and `just e2e` passes 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`):
  - draws 1083/1032/1157 → 1082/1030/1155;
  - primitives 3.066M/2.794M/2.984M → 3.097M/2.827M/3.013M (+1.0–1.2%, the river stones);
  - CPU median 0.69–0.74 ms;
  - fps read 120/120/121, capped again on this run, so not comparable with phase 4b's.
- **Captures:** before (10089fe) and after pairs, in native Metal, of eight `review_revamp.gd` views, in `captures/hoshikawa-phase4c-bridges-banks/`:
  - river-bridge;
  - new: bridge-village-bank, bridge-far-bank and bridge-deck;
  - cedar-river;
  - new: cedar-bridge-west, cedar-banks and cedar-deck.

  The wet gravel banks read along the Cedar River's water.
- **Limits:**
  - The Cedar River is dry from x ≈ −56 to −2. The hill at (−29, −116) lifts its bed up to 3.7 m above the 0.7 m water sheet, which runs buried under it. Reeds and stones skip that stretch. This predates phase 4c and is left for the river-course decision.
  - The deck slab is one collider, so the planks' gaps and jitter are visual only.
  - No physical keyboard/mouse session is claimed.

## Environment revamp, phase 4b water wheel and scarecrow — 2026-10-20

Two Tripo props for the fields: 80 credits, balance 555 → 475 (task ids and prompts in ASSETS.md). No building, character or seeded scatter stream changed.

- **Water wheel:** Tripo's wheel (8,196 triangles after `tools/cut_glb.py` removed a stray floating log) turns in the brook beside the sluice at `World.WheelAxle`. Its 1.3 m rim dips 0.29 m under the water and clears the bed. `World._Process` turns it at the brook's flow speed over its radius (0.35 rad/s), its bottom moving downstream with the water. Crossed `field_rail` legs cradle the axle, standing on the brook bed and at the far waterline through `Height()`; a new `trough` prop carries its water over the sluice board to the channel on two posts. The wheel has a cylinder collider; the legs, trough and posts have box colliders. One pebble spill and one bank stone that stood in its frame are dropped (the pebbles' draws still run, so the others keep their places); the reeds already skipped the sluice's stretch.
- **Scarecrow:** Tripo's scarecrow (8,489 triangles) stands in paddy 1 at `World.Scarecrow`, facing the fields' fence opening, with a pole collider. The rice hills within 0.6 m of its pole are left out after their draws.
- **Self-test:** **375 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 4a had 368). The new checks, beside the paddy and brook ones:
  - the wheel turns, its bottom moving downstream (7.5 cm in 10 frames, on the 1.3 m rim);
  - the wheel is Tripo's textured, brightened model under 10k triangles, its rim at least 0.2 m under the water and every vertex at least 5 cm over the terrain mesh;
  - its four legs and two trough posts stand on the ground;
  - a ray meets its collider at the rim;
  - no reed, pebble spill or bank stone stands inside it;
  - the scarecrow stands in a paddy, with no rice blade within 0.35 m of its pole;
  - the Tripo props loop now covers the scarecrow too: textured, brightened, under 10k triangles, grounded within 5 cm, 1.7 m tall and solid.

  The baked-props check now lists the trough. The brook edge burial (−0.170 m), river edge, brook mouth, footbridge and z 33 climb-out checks all still pass. `just unit` passes 16 cases and `just e2e` passes 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1083/1032/1155 → 1083/1032/1157. Primitives 3.068M/2.794M/2.953M → 3.066M/2.794M/2.984M; the shrine viewpoint sees the wheel. CPU median 0.69–0.77 ms. Fps read 193/238/208, now uncapped, so it cannot be compared with phase 4a's capped 120.
- **Captures:** before (6d139eb) and after, in native Metal, of `review_revamp.gd`'s paddy-sluice and paddies-wide views, and three new after-only views: water-wheel (from the bank below it, with the trough), water-wheel-far (from the far bank, its legs in the brook) and scarecrow. `captures/hoshikawa-phase4b-wheel-scarecrow/` holds them, with the Codex references beside Tripo's previews in `pipeline.jpg`.
- **Limits:**
  - The trough is dry, and no water falls from the wheel's buckets or the trough's spout.
  - The trough and its posts block the bank path between the brook and the fence at z 37; the brook bed and the fields' fence opening are the ways round.
  - The wheel's frame is the fence's timber props, not part of the Tripo model.
  - No physical keyboard/mouse session is claimed.

## Environment revamp, phase 4a water and fields — 2026-10-20

The river, brook and farm fields, with no Tripo credits spent. No building, character or seeded scatter stream changed.

- **Water:** `water.gdshader` is rewritten for the river, the brook and the Cedar River. It reads the depth texture for the water depth under each pixel, giving clear shallows over the bed, blue-green depths, a soft shore fade and a thin foam band. Flowing ripple normals drift along X on the rivers and down the brook toward its mouth (`flow` per material), with a fresnel sky reflection. The old stripes are gone. In native Metal (Forward Mobile) the depth texture works: the captures show shallow edges over gravel and deep teal mid-river. Headless runs draw nothing, so there the shader only compiles. Its no-depth fallback (water reads 2 m deep, the old opaque look) was not exercised. The per-vertex wave is still 3.5 cm and every edge stays buried: brook edges 17 cm, river edges 10.6 cm, and the brook mouth still sits on the river's edge vertices.
- **Banks:** the terrain's vertex red is dryness from `World.Shore()`. `terrain.gdshader` paints wet earth darkening to gravel within 35 cm above each waterline, and on the paddy floors. The road constants are untouched and `TerrainTest` passes.
- **Dressing:** reeds in clumps along the brook and both river banks, clear of the stone bridge, the footbridge, the footpaths and the sluice (5,976 blades; own generators 4421/4425). There are 21 pebble spills at the brook's waterline and six half-sunk rocks mid-stream, clear of the footbridge and the z 33 climb-out (generator 4410). The bank stones, stepping stones and footbridge are kept.
- **Paddies:** `World.Paddies` holds four 6.2 × 8.4 m paddies. `Height()` levels each one 6 cm below the lowest ground it covers: −0.057 / 0.100 / −0.055 / 0.167 m, terraced up the slope toward +X. `RiceFields()` lays the aze as a 0.25 m heightfield over the terrain mesh, crests 25 cm above the higher ground beside them, with a trimesh collider you can walk on. Each paddy has its own still water (`paddy.gdshader`: sky sheen, muddy margin, faint ripples) 8 cm over a wet mud floor. There are 520 rice hills of two or three jittered tufts in two tones: `Grass("Rice")` keeps seed 2207, the paler hills are 2219, and the hill layout uses generator 2213. Aze grass uses 4431/4433. An irrigation channel runs from a sluice board on the brook bank, through a gap in the x 24 aze, into paddy 2 (x 24.4–30.6, z 30.4–38.8). A hasa rack of straw sheaves stands at z 40.7, beyond the paddies' z 39.2 aze, with a box collider.
- **Fences and beds:** `Fence()` places weathered post and split-rail props. `Merge()` bakes them, and their colliders are separate bodies, since a rail is stretched to its span. The node names stay `Fence post` and `Fence rail N` (148 rails), and the rail check now measures each rail's mesh bounds. Komorebi's beds grow 32 cabbages and 48 daikon in place of the sphere crops.
- **Self-test:** **368 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 3 had 360). The new checks:
  - all three sheets run the water shader, and the brook flows toward its mouth;
  - each paddy floor is level within 1 mm under its still water, with the aze cresting at least 17 cm above it (four checks);
  - the aze never sit more than 2 cm under the terrain mesh;
  - a ray onto the middle aze hits its collider;
  - the reeds keep off the footpaths and both bridges.

  The baked-props check now lists all thirteen Blender props. The fence-collision, rice-field route, footbridge, brook climb-out, garden-soil and first-boulder checks all still pass. `just unit` passes 16 cases (new: `PaddyFloorsAreLevel`) and `just e2e` passes 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1071/1029/1140 → 1083/1032/1155. Primitives 2.992M/2.749M/2.853M → 3.068M/2.794M/2.953M (+1.6–3.5%), from the rice, reeds, aze grass and props. CPU median 0.70–0.76 ms. Fps read 119–120 in every run this session, including one with the old water shader swapped back in, so the cap is the display, not this change. It cannot be compared with phase 3's 207–252.
- **Captures:** before (a15e599) and after, in native Metal, from the same points. These are `review_revamp.gd`'s new river-bridge, brook-close, paddies-wide, komorebi-beds and cedar-river views and the existing rice-paddies, plus `review_hamlet.gd`'s river, brook-bridge, brook-mouth, overview and east row. New after-only views show the paddy sluice and the hasa rack. `captures/hoshikawa-phase4a-water-fields/` holds the pairs.
- **Limits:**
  - The channel water is a flat strip 4 cm over the ground, not carved into `Height()`: the 1 m terrain mesh cannot hold a 60 cm ditch. It reads as a narrow grey ribbon.
  - The pebbles, rocks, reeds and sluice have no collision.
  - The Cedar River got the shader and wet banks but no reeds.
  - The hasa rack holds straw while the rice is green.
  - No physical keyboard/mouse session is claimed.

## Environment revamp, phase 3 houses — 2026-10-20

The other five Hoshikawa buildings (Kisaragi Workshop, Seed & Grain, Village Assembly, Hoshikawa Clinic, Mori Farmhouse) are now Tripo models, placed with the teahouse by one generalised `Minka()`. Five image-to-model calls, 40 credits each, no retries: 200 spent, 555 left. No character or terrain landform changed; four table centres and sizes did (ASSETS.md).

- **Captures:** `tools/review_revamp.gd` gained, per house, a front view, a 45° oblique view and the gameplay spring camera half a metre out from its front wall. `tools/review_hamlet.gd` gained a west-row view. These and the square, east row and overview were taken before (668b06e's code, identical to bbd7b10's) and after, in native Metal, framed from the same points. `captures/hoshikawa-phase3-houses/` holds the before/after pairs and a pipeline sheet per house (reference, Tripo preview, before, front, oblique, eaves).
- **Models:** 16,575 / 16,866 / 15,939 / 16,360 / 15,764 triangles (workshop, kura, hall, clinic, farmhouse) with 2048² maps, all under the 20k budget. Each stands on its pad, every vertex within 10 cm of its base on `Height()`'s level ground.
- **Eaves:** the self-test measures the camera-layer hit straight up from beside each sign: workshop 3.67 m, kura 3.05 m (its door hood), hall 4.10 m (its porch ceiling; the main eave is 3.6 m), teahouse 4.02 m, clinic 3.69 m, farmhouse 2.91 m. Only the teahouse and the hall's porch reach the brief's 4 m: at 4 m the other walls would be 12–14 m wide. In the eaves captures the spring arm stops under the workshop, hall, clinic and farmhouse eaves. The kura's eave overhangs only about 0.4 m, so the camera rises past it, as it did past the `Minka()` kura's.
- **Self-test:** **360 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; the pilot fix had 355). The five archetype checks became the teahouse's model check for every house: under 20k triangles, textured and tinted, taller than 6 m, footprint on the level pad. The eaves check now casts from beside each sign and asserts the hit is within 0.3 m of the table's eave height. The wall check, which only the teahouse had, runs for all six and stops at the model's front wall within 10 cm. The decks-and-steps check became a check that each of the four pots stands within 3 cm of the deck or ground under it. All four village corridors stay clear and the water edges stay buried. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1127/1071/1216 → 1071/1029/1140, since the timber frames' merged cells and roof meshes are gone. Primitives 2.773M/2.563M/2.642M → 2.992M/2.749M/2.853M (+7–8%), from the five 16k-triangle models in the colour and shadow passes. CPU median 0.68–0.75 ms; fps 207–252.
- **Limits:** the hall's front view has the well in the foreground, as before. The workshop model is open at its doorway, with its interior modelled; the walls box keeps the player out. No physical keyboard/mouse session is claimed.

## Environment revamp, phase 3 pilot fix — 2026-10-20

The user reviewed the pilot and asked to fix only the teahouse's eaves camera and its floating sign and lantern. No Tripo calls; the other five buildings are unchanged.

- **Eaves camera:** the model is re-imported at 7.2 m (was 5.93 m) from the same `teahouse-tripo-2k.glb`. Its eave underside is now about 4.0 m up at the front (was 3.3 m; the Minka's was 4.1 m). In the eaves-under view the spring arm again frames Mio from above the sign, as it did under the Minka eaves. I didn't raise the camera-only stop above the 3.3 m eave instead, because the camera would then sit inside the model's roof.
- **Sign and lantern:** the sign hangs at 3.2 m on two cords up into the eave, and the chochin hangs on its own cord with dark rims. Front close-ups show both cords meeting the eave beam.
- **Fit:** the walls are 9.7 × 6.5 m and stay on the pad's level apron. The roof (12.1 m) overhangs the apron's edge, so the teahouse check now tests the model's footprint, its vertices within 10 cm of its base, against `Height()`, instead of the whole bounding box. The tea bench, umbrella and stepping stones keep their places, since the front wall did not move. All four village corridors stay clear.
- **Self-test:** **355 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). The wall check also asserts that the camera-layer eave over the front is more than 3.8 m up. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1127/1071/1216, unchanged; primitives 2.772M/2.561M/2.641M → 2.773M/2.563M/2.642M from the cords and rims. CPU median 0.79–0.89 ms.
- **Captures:** the teahouse front and oblique, the three eaves views, the dialogue background and the east row, before (7c50cdf) and after, in native Metal.

## Environment revamp, phase 3 pilot — 2026-10-20

One Tripo building, The Copper Kettle teahouse, replaces its `Minka()` version. The other five Hoshikawa buildings stay on `Minka()` for comparison. No character, terrain or pad changed.

- **Captures:** `tools/review_revamp.gd` gained a front and an oblique view of the teahouse, framed from the ground at its centre. These, the three eaves spring-camera views, the dialogue background and the `review_hamlet.gd` east row, square and overview were taken before (5bb7119's code) and after, in native Metal.
- **Model:** 16,282 triangles with 2048² maps, 5.93 m tall against the Minka's 7.2 m to its ridge. In game it stands on its pad, inside the pad's flat apron, and its front wall is where the old one was. Its eaves are lower: their underside is about 3.3 m up at the front, against the Minka's 4.1 m. In the eaves-under view the spring arm now stops right behind Mio, because it meets that lower eave sooner.
- **Self-test:** **355 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 2b had 354). For the teahouse, the archetype check is replaced by a model check: under 20k triangles, textured and tinted, grounded on the pad, taller than 5.5 m, and its bounds inside the pad's flat apron. The sign check now casts up against every array mesh under a building instead of meshes named "roof", so for the teahouse the first face above the sign must be the model's eave underside. The eaves check is unchanged and passes against the model's trimesh. A new check casts at the front wall on layer 1 and stops at the model's wall line (3.5 m). Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1139/1071/1230 → 1127/1071/1216, primitives 2.72M/2.57M/2.60M → 2.77M/2.56M/2.64M. The Minka teahouse's merged material cells and its roof mesh cost more draws than the one model. Its 16k triangles, drawn in the colour and shadow passes, add up to 2% primitives where it is in view. CPU median 0.82–0.91 ms; fps 182–197.
- **Limits:** the other five buildings are still `Minka()`. No physical keyboard/mouse session is claimed.

## Environment revamp, phase 2b — 2026-10-19

The Tripo well and the cedar props (benches, lamps, barrels) are brightened. Three more Tripo models replace box-built props: the stone lantern (all four), the handcart and the notice board. No character, building or terrain changed.

- **Captures:** `tools/review_revamp.gd` gained three views framed from the ground under the prop: a village lantern, the cart and the notice board. All 22 views were taken before (4a1de3e) and after, in native Metal.
- **Models:** 7,748 / 8,168 / 7,605 triangles with 2048² maps. Tripo previews and in-game views from front and back show no baked-texture errors. The first capture showed the notice board's blank back to the road, so it was turned 180°.
- **Self-test:** **354 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; the pilot had 348). The six new checks cover each placement: under 10k triangles, textured and tinted, standing on `Height()` within 5 cm at the expected height, and solid to a ray at hip height. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1140/1072/1230 → 1139/1071/1230, primitives 2.63M/2.48M/2.50M → 2.72M/2.57M/2.60M. Each Tripo model adds a draw, which roughly offsets the merged primitive cells it replaced. The extra ~47k triangles, drawn in both the colour and shadow passes, account for the primitives. Four lanterns did not justify a MultiMesh. CPU median 0.72–0.80 ms; fps 150–241.
- **Limits:** the plant pots, field fences and Komorebi's benches are still box-built. The lanterns' paper screens no longer glow. No physical keyboard/mouse session is claimed.

## Environment revamp, phase 2 pilot — 2026-10-19

One Tripo prop (the village well) and five Blender props (lamp, bench, barrel, bamboo fence post and bay) replace box-built versions. No character, building or terrain changed.

- **Captures:** `tools/review_revamp.gd` gained seven fixed prop close-ups (two stone lanterns, the well, a bench, a street lamp, stored barrels and the bamboo fence). Their before shots were taken on the phase-1 build from the same coordinates, in native Metal.
- **Well:** the Tripo model has no baked-texture errors (renders from four sides and of its maps). In game it stands on `Height()` within 5 cm, its posts block the player, and its roof stops the camera. It reads darker than the surrounding props because Tripo's albedo is dark.
- **Props:** all five are baked by `Merge()` (2,382 static primitives and props into 244 meshes). Node names and colliders are unchanged, so the `Plant pot` and `Fence rail` checks still pass without edits. The bench reads very dark, because the cedar atlas cell is dark yakisugi.
- **Self-test:** **348 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; phase 1 had 345). The three new checks cover the baked props, the well's model and the well's collision. Both runs exit 0 with `SELF_TEST_PASS`. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1126–1129/1045/1212 → 1140/1072/1230, primitives 2.59M/2.48M/2.47M → 2.63M/2.48M/2.50M. That is +11 to +27 draws, from the props' three materials plus the well's own material, which is not merged. A first pass with stone and rope materials cost 1159/1092/1253, so those two were dropped. CPU median 0.74–0.82 ms; fps 119–120.
- **Limits:** the stone lanterns, plant pots, field fences and Komorebi's benches are still box-built. No physical keyboard/mouse session is claimed.

## Environment revamp, phases 0 and 1 — 2026-10-19

Vegetation, light and the camera at the eaves, so the environment matches the Tripo characters. No character model changed.

- **Captures:** `tools/review_revamp.gd` (twelve close-ups, including the spring camera at the tea house eaves and a dialogue background) and the nine `review_hamlet.gd` views, before and after, from the same viewpoints in native Metal. One close-up viewpoint was moved after the first pass, because it sat inside the workshop; its before shot was retaken from an `origin/main` worktree. Comparisons are in `artifacts/hoshikawa-revamp/compare/`.
- **Foliage:** the new atlas's alpha is clean: corners 0, no blue-tinted opaque pixels, 77–81% of each sprite's visible pixels fully opaque. Close up, the sakura reads as small blossoms, not large torn-paper flowers. Two artifacts were traced and removed. One was a dotted pattern on the cards: shadow acne from cards self-shadowing, plus alpha-to-coverage dithering cards that were faded edge-on. The other was a brown core on backlit crowns, where shadows blocked the transmitted light. Crowns now cast but do not receive shadows, and alpha-to-coverage was dropped.
- **Seeded streams:** `countryside scatter is unchanged` still passes (first boulder pinned). Meadow seed positions keep their `rng` draws. Extra tufts and colours come from per-field generators. `villageRng` keeps its draws, and nothing draws from it after the lawn loop. The rice plants never drew from a stream.
- **Camera:** in the eaves shots the spring arm now stops under the tea house roof instead of rising through it. A new check per Hoshikawa building casts up from under the front eave: the camera layer is hit and layer 1 is not.
- **Self-test:** **345 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro; main had 339 — the six new ones are the eaves checks). Both exit 0 with `SELF_TEST_PASS` and no shader errors. `just unit` passes 15 cases, `just e2e` 4.
- **Render cost** (`render_cost.gd`, `--disable-vsync --fixed-fps 60`): draws 1164/1098/1246 → 1126–1129/1045/1212, primitives 2.25M/2.21M/2.15M → 2.59M/2.48M/2.47M (+12–15%), from denser tree crowns and blade grass. Grass casts no shadow and its chunks drop beyond 90 m. CPU median 0.7–0.9 ms; fps is unchanged within run-to-run noise. The first viewpoint now varies by up to 3 draws between runs, likely a grass chunk at its visibility-range edge.
- **Limits:** the distant ridge mesh and the terrain textures were not changed in this phase. No physical keyboard/mouse session is claimed.

## Mio movement and combat polish — 2026-10-18

The user found the jump looked unwired, walking too fast, the dodge too short and the attack combo jerky. They also asked for a sword grip, the face strokes, the render-cost rise and attack2's timing.

- **Jump:** it was wired. Tripo's preset hops with straight legs, so once its height was removed the airborne frames looked like standing. `review_jump.gd` (its bone names updated for the new rig) now shows a crouch at takeoff, knees tucked in the air and a crouch on landing.
- **Walk and run:** walking drops from 4.3 to 2 m/s. In a game probe of planted-toe speed, the walk's foot slide fell from 3.09 to 0.36 m/s and the run's from 5.13 to 1.86 m/s. Tripo's run is a jog, so at 7.8 m/s some slide remains.
- **Dodge:** the whole 1.6 s roll used to be squeezed into 0.42 s. It now plays crouch, roll and rise in 0.6 s, at 7 m/s, so the distance is unchanged (4.2 m). Invulnerability is 0.4 s and the cooldown 0.85 s.
- **Combo:** Blender renders show all three attacks starting and ending in the same ready stance. The fastest right-hand frames are 7 of 15 for attack1 and 8 of 20 for attack3. attack2's short backhand spans frames 2–11 around its hit at about frame 7.
- **Grip:** close renders in idle, walk, guard and attack1 show the right fingers wrapped round the hilt.
- **Face:** before/after flat-texture renders show the cheek strokes, the dark band beside her left eye, the nose-bridge streak and smaller marks gone. A slightly paler patch remains beside her left eye.
- **Render cost:** primitives are unchanged from the previous build. The rise since the 65-bone rig was traced to level-of-detail choice. Old and new Mio meshes have identical LOD levels and the same mesh bounds. At the default LOD bias, Godot now draws the full-detail mesh (239,794 primitives per frame for Mio, against 121,334 before). At bias 0.01 both builds draw the same 2,030,620. Nothing is broken, and the change was left as it is.
- **Self-test:** `camera-relative walking` now expects 1.5–2.5 m in a second. **339 checks pass headless and in native Metal** (main had 339). Both exit 0 with `SELF_TEST_PASS`; the native log has no input restores. `just unit` passes 15 cases and `just e2e` 4, including Mio's steering test at the slower walk.
- **Evidence:** `artifacts/mio-polish/`.

## Zero's Japanese voice — 2026-10-18

- **Audio:** 18 Koume mp3s (eleven_v4, 44.1 kHz mono, 2.4 MB) in `assets/audio/voice/ja/Zero/`, one per Zero beat, each imported as `AudioStreamMP3`. `python3 tools/build_voice.py --check` passes for all 31 Japanese lines.
- **Self-test:** the voice-file check now covers all 31 Mio and Zero beats. Zero's opening line plays `Zero/Intro.0.mp3`, Mio's reply cuts it off and plays her own, and Ren, who has no voice files, stays silent. **339 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro); both runs exit 0 with `SELF_TEST_PASS` and no leak or error output.
- **Limits:** as for Mio's voice, the checks confirm which file plays, not how it sounds; no native-speaker review and no physical keyboard/mouse session.

## Unit and e2e test suites — 2026-10-18

- **Local runs:** in a Linux cloud container with Godot 4.6.2.stable.mono.official and .NET SDK 8.0.131: `just unit` passes 15 cases in under a second, `just e2e` passes 4 cases in about 30 s (one headless game start each), and the existing self-test passes 338 checks headless on Linux in 20 s.
- **CI:** the first GitHub Actions run of PR #8 (ubuntu-latest, Godot 4.6.2 from `chickensoft-games/setup-godot`) passed all three suites: 15 unit cases, `SELF_TEST_PASS` with every check, and 4 e2e cases in 26 s. The e2e job took under three minutes with an uncached import and then saved `.godot/imported` (480 MB) for later runs.
- **Mutation checks:** shifting a terrain-shader road constant, inserting an intro line before Mio's voiced beats, moving `NightFalls` and moving the clinic onto the road each fail the matching unit test; letting Escape only pause, and reversing Mio's forward input, fail the pause and steering e2e tests, which leave `scene_tree.json`, engine logs and process output under `test_output/`.
- **Limits:** e2e runs headless, so failure artifacts carry no screenshot. A windowed run under Xvfb with software Vulkan (llvmpipe) did not reach its first capture within four minutes, so CI does not attempt rendered runs; the Metal graphical self-test stays a local check. E2E input is engine-injected; no physical keyboard/mouse session is claimed.

## Mio's Japanese voice — 2026-10-18

- **Audio:** 13 Yuki mp3s (eleven_v4, 44.1 kHz mono, 1.1 MB) in `assets/audio/voice/ja/Mio/`, one per Mio beat, each imported by Godot as `AudioStreamMP3`. `python3 tools/build_voice.py --check` confirms `ja.json` still matches `Prologue.cs`.
- **Self-test:** five new checks. Every Mio beat in a named scene has an mp3 longer than half a second. Zero's opening line plays nothing. Mio's reply plays `Intro.1.mp3` and keeps playing once its text is completed. The homecoming plays `Homecoming.6.mp3`. Esc stops the voice. **338 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro); both runs exit 0 with `SELF_TEST_PASS` and no leak or error output.
- **Limits:** the checks confirm which file plays and that the player reports playing; nobody listened to the in-game mix during the run, and the Japanese readings were not reviewed by a native speaker. No physical keyboard/mouse session is claimed.

## Mio's Tripo Studio clips — 2026-10-16

The user rigged the low-poly Mio in Tripo Studio and made her clips there. Two bridge sends and one FBX download carried no animation or an incomplete set; the third bridge send held all fifteen actions.

- **Preservation:** the bridge scene was saved unchanged with `save_as_mainfile(copy=True)`. The open session was not edited.
- **Identification:** six frames of every action were rendered and the right hand's path was traced, to match each action to its prompt. The weak first attack1 and the unused slash and standing_relax presets were set aside.
- **Same mesh:** the animated send's vertices and UVs equal the first send's exactly, so the repaired textures apply unchanged.
- **Timing:** each clip keeps the previous clip's length. Measured in the exported game copy, the fastest right-hand frame is 7 of 15 for attack1 (hit at about 6), 5–6 of 16 for attack2 (about 7), 8 of 20 for attack3 (about 8) and 12–16 of 31 for skill (about 17).
- **GLB audit:** `check_mio_export.py` passes: 65 bones, 12 clips, 47,980 triangles, maximum skin weight-sum error 9.69e-8, exact embedded base-color and normal bytes. The GLB is 8.6 MB.
- **Poses:** Blender renders of all 12 clips beside the previous ones, and close renders of the sword hand in idle, walk, guard and attack1, show the sword held in the right palm.
- **Self-test:** the player rig checks now expect 65 bones and Mixamo bone names. **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and the native log has no input restores.
- **Render cost:** draw calls are unchanged. Primitives rose by 118,460 at each of the three viewpoints compared with the dense-rig low-poly build (2,133,766 → 2,252,226 at (0, 31)). The triangle count is the same, so this is more passes over Mio, probably shadow cascades; the cause was not investigated.
- **Captures:** `tools/review_tripo.gd` saved all eleven views in the current village. They and side-by-side comparisons with the previous build are in `artifacts/mio-tripo-animation/`.
- **Limits:**
  - Tripo's hands stay open, so the sword rests in an open palm.
  - attack2's swing opens its clip, so it lands about two frames before the hit.
  - The guard loop is the held block played forward and back.

## Low-poly Mio — 2026-10-16

The user sent a 47,383-triangle Mio over Tripo DCC Bridge. It replaces the 150,600-triangle decimation of the dense import.

- **Preservation:** the live bridge scene was saved unchanged with `save_as_mainfile(copy=True)`, and the open session was not edited. All repairs ran in background Blender on copies.
- **Identification:** renders of "3d fantasy character model" match the in-game Mio's hair, coat, scarf and satchel. Its surface lies a median 5 mm from the dense import at the same scale, so it is a separate Tripo generation, not a reduction of it.
- **Texture repair:** before/after renders of the head from front, both three-quarters and back show skin and white gone from the bangs, side locks and strands behind the ears. Eyes, ears and collar are unchanged. Outside the repaired texels, the JPEG re-save changes the base color by a mean 0.35/255.
- **GLB audit:** `check_mio_export.py` passes: 41 bones, 12 clips, 47,980 triangles, maximum skin weight-sum error 1.27e-7, and embedded base-color and normal bytes equal `assets/art/mio-lowpoly-*`. The GLB is 8.0 MB, down from 17.4 MB.
- **Deformation:** Blender renders of all 12 clips beside the previous model's showed matching poses. Close renders of run, attack1, attack3, skill, guard and stagger showed no tearing or stretching.
- **Self-test:** **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro), the same count as main. Both runs exit 0 with `SELF_TEST_PASS`. The native log has the two expected `TEST_INPUT_RESTORE` lines.
- **Render cost:** `render_cost.gd` primitives fell by 69,060 at each of the three viewpoints. Draw calls did not change.

  | viewpoint | draws | primitives (before → now) |
  |---|---|---|
  | (0, 31) | 1164 | 2,202,826 → 2,133,766 |
  | (0, 16) | 1098 | 2,158,544 → 2,089,484 |
  | Shrine steps | 1246 | 2,100,611 → 2,031,551 |

- **Captures:** `tools/review_tripo.gd` saved all eleven views of both the previous and new GLB in the current village. Both are in `artifacts/mio-lowpoly-integration/`.
- **Limits:**
  - The first in-game captures showed a greyer, noisier face than the previous model. The bake's broken normals were then flattened, and Tripo's roughness/metallic maps were replaced by the previous matte values.
  - A fine dot pattern from the sun's soft shadow remains on the face in close views. The previous model shows the same pattern in the same view, and it disappears when Mio casts no shadow.
  - The dark strokes near the eye corners come from Tripo's bake and were left as they are.

## Sora, Nao and Toma Tripo integration — 2026-10-16

Three Tripo Studio characters reached a live Blender session over DCC Bridge as rigged FBX, and nothing had saved them.

- **Preservation:** the scene was saved without change through the Blender MCP (`save_as_mainfile(copy=True)`), leaving the open session's file path and state as they were. The save reopened in background Blender with three 65-bone armatures, three meshes and twelve packed textures.
- **Identification:** renders of each character were matched to the v3 four-view references. anime is Sora, female is Nao and adventurer is Toma.
- **Originals:** the per-character originals were split from that save. Its hash was unchanged afterwards.

| | Sora | Nao | Toma |
|---|---|---|---|
| Source triangles = GLB triangles | 19,591 | 19,530 | 18,215 |
| Height (Blender audit of the GLB, soles at 0) | 1.78 m | 1.64 m | 1.74 m |
| Ankle above ground (self-test) | 0.129 m | 0.093 m | 0.146 m |

- **GLB contents:** each GLB holds one mesh, a 65-joint skin and a closed four-second `idle`.
  - The maximum skin weight-sum error is 9.69e-8.
  - Embedded base-color and normal bytes equal the source textures. Roughness and metallic are combined by Blender.
  - Godot uses the default scene import settings, matching `koma7.glb`.
- **Code:** `World.TripoResidents` now holds the asset, height and label height of all five Tripo residents, replacing the Chiyo/KOMA-7 conditionals. Chiyo and KOMA-7 keep their exact previous values.
- **New checks:** IntegrationCheck runs the same set over all five residents.
  - rig contract, own GLB, looping idle that moves the rig, planted foot, textured material, terrain placement
  - collision capsule fitted to the table height
  - idle length (six seconds for KOMA-7, four for the others)
  - ankle 0–0.2 m above ground
  - hips and feet fixed at five points through the idle
- **Results:** before merging main, 323 checks passed: 272 plus 51 new. After merging main's visual-novel prologue (282 checks on its own), **333 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings, errors or input restores. The existing Komorebi walk-to-service checks still reach Nao and Toma with their new capsules.
- **Render cost:** `render_cost.gd` draws fell at all three viewpoints, because each model is one surface where the shared villager had several. Primitives rose 0.4–4.4% at the same viewpoints.

  | viewpoint | draws (main → now) | primitives (main → now) |
  |---|---|---|
  | (0, 31) | 1229 → 1164 | 2,110,640 → 2,202,826 |
  | (0, 16) | 1154 → 1098 | 2,149,466 → 2,158,544 |
  | Shrine steps | 1311 → 1246 | 2,054,133 → 2,100,611 |

- **Captures:** `tools/review_villagers.gd` saved front, face, idle, side, back and real-dialogue captures for each, under `artifacts/{sora,nao,toma}-integration/`. All 18 were inspected. After the merge, the dialogue capture first finishes the typed-out line, because the capture stops processing. It was recaptured: the visual-novel layout shows each resident's portrait opposite Mio, with the full line and the 3D model behind. Hashes, the GLB audit, and the import, test, review and render-cost logs are in `artifacts/villagers-integration/`.
- **Limits:**
  - The heights are judgement calls; the references give ages and builds but no heights.
  - The idles are authored, because Tripo supplied no animation.
  - All input was engine-injected.

## Scene 1 visual-novel dialogue — 2026-10-16

The prologue's conversations now play as visual-novel scenes: portraits, name plates, typed-out lines, narration and Zero's link emblem.

- **New checks:** ten. Beginning the journey opens on Zero. Mio has a portrait and Zero has none. A line types out, the first press completes it, and the intro takes exactly two presses per line. Chiyo's first meeting mentions Shio, and talking to her again plays only her usual line. The homecoming's night falls before Mio mentions her mother, and Zero's next line changes the subject. Esc skips the rest of a scene, and restarting the prologue from the pause menu replays the opening. Keys go in through `Input.ParseInputEvent`; no physical keyboard or mouse was used.
- **Headless:** 282 checks pass (was 272), exit 0 with `SELF_TEST_PASS`, and the log has no warnings or errors.
- **Graphical:** this run was in a Linux container with no GPU, under xvfb with Mesa's software OpenGL and `--rendering-method gl_compatibility`, not native Metal on the Mobile renderer. All 281 checks pass at 640×400 on commit `3ead5f1` (before the resident portraits), exit 0 with `SELF_TEST_PASS`. The log's only errors and warnings are the container's: no audio device and no V-Sync control. Software rendering made the run take about two hours; an earlier attempt at 1440×900 was stopped after 142 passing checks.
- **Captures:** `tools/review_dialogue.gd` under the same xvfb setup, inspected by eye: the intro (both speakers and a line mid-typing), Ren's briefing, Zero's line in it, Hana with KOMA-7, Chiyo's closing narration, nightfall and the line about Mio's mother. Recaptured after the Codex portraits: Mio and the resident she speaks with stand at either side, head to hip above the text box, the listener dimmed, with Zero's emblem between them. The night overlay darkens the frame from "Night falls" onward.
- **Portraits:** the portrait check covers Mio and all ten residents. With `yui.png` moved away it fails with `SELF_TEST_FAIL`. How the eleven were generated, cut and checked is in `assets/art/portraits/portraits-generation.md`.
- **Native Metal:** on the owner's Mac, `tools/review_dialogue.gd` ran graphically on commit `8b4a35d` under Metal and printed `DIALOGUE_CAPTURE_OK`; the nine captures show the Codex portraits placed as above, with Mio left facing right and the resident right facing left. That Mac's headless self-test also passed.
- **Not covered:** the full graphical self-test under native Metal.

## Static geometry merge — 2026-10-14

Draw calls fell by 70–76%, and the frame looks the same. Before the change, World's static boxes, spheres and cylinders were 2,675 separate meshes. Each cost a draw call, and most cost a second one in the sun's shadow pass. `World.Merge()` now bakes them into 233 meshes, one per material and 32 m cell, after `_Ready` builds the world. The originals stay hidden, so colliders and every name-based check still work.

| `render_cost.gd` viewpoint | Draws before → after | Primitives before → after | Median CPU render ms before → after |
|---|---|---|---|
| (0, 31) | 4666 → 1229 | 2,024,204 → 2,110,640 | 1.92–1.94 → 0.77 |
| (0, 16) | 3897 → 1154 | 2,074,466 → 2,149,466 | 1.64 → 0.71 |
| Shrine steps | 5389 → 1311 | 1,996,473 → 2,054,133 | 2.16–2.30 → 0.79–0.80 |

- **Measurement:** draws and primitives repeat exactly under `--fixed-fps 60`. The CPU times come from back-to-back runs on the same machine, at load average about 5. fps read 118–120 both before and after, so a cap limits it, not the render.
- **Primitives:** they rise about 4%, because a merged 32 m cell is drawn whole when any part of it is in view.
- **Materials:** `Mat()` now returns one shared material per colour, emission and `twoSided`, which cut its materials from 916 to 101. `Merge()` groups by material reference. Grouping by serialized material content gave the same draws but took about 300 ms at startup; the merge now takes 80–93 ms. Five call sites used to set `CullMode` on a `Mat()` result, and they now pass `twoSided` instead. World clears the cache in `_ExitTree`, and six `--verbose` quit runs report no leaked resources.
- **Exclusions:** transparent materials, `PlaneMesh`, meshes with visual children and meshes under any non-`Node3D` parent are not merged.
- **Pixel diff:** frames captured at the three viewpoints match the pre-change frames. The (0, 31) view has no pixel off by more than 4/255. The other two views have 1 and 2 pixels over 25/255, at triangle edges. Normals use the inverse transpose, because many spheres are scaled unevenly.
- **New check:** the merged meshes must hold exactly the hidden originals' 165,084 triangles, with the same world-space bounds. Removing the per-object transform from `Merge()` makes it fail.
- **Results:** 272 checks pass headless and in native Metal. Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings or errors.

The frame-rate recheck below predates this change; its draw counts are now the "before" column. Probes, frames, diffs and logs are in `artifacts/perf/`.

## Brook mouth joint — 2026-10-14

The dark line where the brook strip met the river sheet is gone.

- **Cause:** there were two height steps at z −4. The brook sat 2 cm below the river (−.40 against −.38). And the water shader's 3.5 cm wave is computed per vertex. The river's edge has a vertex every 1.26 m, so it followed the sine. The brook's mouth row had only its two side vertices, 4.2 m apart, which is almost exactly one wave period (4.19 m), so that edge rose and fell flat. The gap between the edges reached about 7 cm and showed the dark channel bed through it.
- **Fix:** both sheets now sit at `World.WaterLevel` (−.38). The brook's mouth row reuses the river sheet's own edge vertices and fans into the strip's first full row at z −3.
- **Checks:** a new check requires every mouth vertex to lie on the river's vertex grid at the river's level, spanning the brook's full width. The brook-edge check now takes the strip's sides as the lengthwise edges that only one triangle uses. Its old rule, edges that don't cross the brook's centreline, also caught the mouth fan's inner edges over open water. On the old two-vertex strip both rules pick the same edges. The highest brook edge is now −0.168 m (was −0.188), still beyond the 4 cm margin.
- **Results:** 271 checks pass headless and in native Metal. Both runs exit 0 with `SELF_TEST_PASS`, and neither log has warnings or errors.
- **Captures:** a throwaway probe filmed the joint from two angles at eight moments across one 2π s wave period, on the old and new code. The line shows in most old frames and in no new ones. In a pixel diff of a whole frame, only the joint and the brook's west waterline change. The waterline moves because the brook is 2 cm higher.
- **Side effect:** part of the mouth now falls outside a shadow that used to cover it. The patch has stair-stepped shadow-map edges like those already on the river. It is not the water shadowing itself: turning off shadow casting on both water meshes changes only 311 pixels.

Evidence is in `artifacts/hamlet/joint/`: the probe, the before/after frames, the contact sheets, `review/` recaptures and both self-test logs.

## Shutdown audio leak — 2026-10-14

The intermittent shutdown error is fixed. It was "1 resources still in use at exit" plus an ObjectDB leak warning, printed after `SELF_TEST_PASS`.

- **What leaked:** `--verbose` names `res://assets/audio/valley.wav` and its `AudioStreamPlaybackWAV`, both at reference count 1. This matches the earlier verbose logs in `artifacts/tripo-jump/`.
- **Cause:** the ambience player stops its playback when it leaves the tree at quit. The AudioServer only drops a stopped playback, and the stream it holds, on the audio thread's next mix step. When the engine shut the AudioServer down first, both leaked.
- **Fix:** `Game._ExitTree` stops the ambience and blocks for 100 ms, so that mix step runs first. Every quit path goes through it: the self-test, the HUD Quit buttons, closing the window and `--quit-after`.
- **Evidence:** the repro is `Godot --headless --verbose --path . --quit-after 120 -- --play`. Without the fix it leaked in 6 of 20 runs, and in 20 of 20 in a control rerun just after the fixed runs. With the fix it leaked in 0 of 40.
- 270 checks pass headless and in native Metal, both runs with `--verbose`, and neither log reports a leak.
- The verbose graphical log also shows 15 "Image format RGB8 not supported by hardware" warnings. These appear only under `--verbose`; the non-verbose graphical log of the same tree has none.

Evidence, including every repro run log and both self-test logs, is in `artifacts/shutdown-leak/`.

## Frame-rate recheck — 2026-10-14

The minor fixes and the river widening cost nothing measurable. `main` (`d78ff65`) and `3670287` were measured back to back with `tools/render_cost.gd`, at the same three viewpoints as before, with vsync off and `--fixed-fps 60`. Repeat runs gave identical draw and primitive counts.

| Viewpoint | Draw calls (main / base) | Primitives (main / base) |
|---|---|---|
| (0, 31) | 4666 / 4665 | 2,024,184 / 2,023,554 |
| (0, 16) | 3897 / 3898 | 2,074,446 / 2,074,692 |
| Shrine steps | 5389 / 5389 | 1,996,457 / 1,996,771 |

- Primitives differ by at most 0.03%.
- Median CPU render time is 1.75–2.43 ms on both commits, and uncapped fps is 123–156 on both (Apple M1 Pro, Metal, Forward Mobile). The run-to-run spread is larger than any difference between the commits.
- Absolute fps is not comparable with the 118–120 fps recorded below. Other apps put the machine's load average between 9 and 56 during these runs, and vsync no longer caps at 60 Hz: a vsync-on run reached 110–144 fps.
- GPU render time reads 0 under Metal Forward Mobile, so the probe does not sample it.
- The old `fps.gd` probe left the mouse captured after `Start()`, so moving the real mouse turned the camera, and runs varied 6× in draw calls. `render_cost.gd` releases the mouse. Without `--fixed-fps`, walking actors still shift the counts by a few draws between runs.

Evidence, including `summary.txt` and every run log, is in `artifacts/hamlet/fps-recheck/`.

## River banks — 2026-09-30

The Hoshikawa river sheet now ends inside its banks. **270 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors.

- A new check samples both long edges of the river sheet against the 1 m terrain mesh. It skips only the stretch where the brook strip carries on from the edge. Before the fix, the edges were up to 0.86 m above the ground (x = 36 on the village bank). Now they are at least 10.6 cm below it.
- The sheet was 4.8 m wide, narrower than its dip, so both edges hung over the dry hollow. It is now 8 m wide (z −12 to −4).
- Widening alone could not fix the village bank at x ≈ 34–38 and x ≈ −58 to −62. There the terrain's swell troughs left the ground within 1 cm of the water for over 10 m. `BaseHeight` now adds a 0.3 m village-side bank centred on z = −4. The countryside bank already met the water within 3.25 m and is unchanged. The brook's bed is still carved to −0.95 m.
- The brook strip now starts at the sheet's new edge (z −4). The brook-edge check no longer excludes the mouth's first metre: every brook edge is at least 18.8 cm below the bank.
- The first-boulder pin, the terrain continuity grid and the 31 village trees are unchanged.
- After the self-review, both edge checks require at least 4 cm of burial, up from 2 cm. Water's vertex wave lifts edges by up to 3.5 cm, so a 2 cm limit could pass an edge that still breaks the surface at a crest. The measured margins are unchanged (18.8 cm brook, 10.6 cm river), and 270 checks pass again headless and in native Metal. Those logs are `selfreview-headless.log` and `selfreview-graphical.log`. One headless run printed a resource-in-use error and an ObjectDB leak warning at shutdown, after `SELF_TEST_PASS`. A headless rerun was clean, matching the intermittent shutdown report recorded under the 81-check run below.
- The self-review's remaining nits are now written down and the behaviour is unchanged. The sign check fails when no roof is overhead, and its message now says so; all six signs hang under their eaves. A probe confirmed the brook-mouth boulder skip removes exactly one boulder, at (19.1, −4.2) on the −0.95 m channel bed, so its z > −9 window stays. `CLAUDE.md` now notes that the water sheets depend on the dip, bank and channel in `Height()`. 270 checks pass headless and in native Metal, both runs clean (`nits-headless.log`, `nits-graphical.log`).

Frame rate was not re-measured. Evidence is in `artifacts/hamlet/river/`: both test logs, and `review_hamlet.gd` captures including a new `river` view. `before/` holds the same `river` and `brook-mouth` views from the previous commit.

## Hamlet review minors — 2026-09-29

The ten minor findings deferred from the hamlet's whole-branch review are fixed. **269 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Both runs exit 0 with `SELF_TEST_PASS`, and neither log contains warnings or errors. Each new check below failed before its fix:

- Brook water edges stay under the bank's 1 m mesh triangles. They were up to 0.46 m above the mesh; from z −4.5 south they are now at least 8 cm below it. The strip's half-width is 2.1 m, and it starts flush with the river sheet, which removes the dark seam where the two met. The mouth's first metre still lies over the river's dip, as the river sheet's own edges always have.
- No countryside boulder sits in the brook mouth. The boulder there is skipped only after its random draws, so the first-boulder pin still passes.
- Potted plants stand clear of decks and steps. The hall's pots now flank its porch step, and the clinic's stand on the engawa.
- The shrine mound's mesh faces are at most 25°. They were 32.4°; the mound's falloff radius grew from 11 to 14 m.
- Village grass keeps off the footpaths, which are now the shared `World.Footpaths` data.
- Fence rails follow the ground. A rail's two ends were up to 0.81 m apart in height above ground. Rails now run post to post, in every fence including Komorebi's.
- Every Hoshikawa sign hangs clear of its roof. The hall's sign had clipped its porch roof; it now hangs 0.25 m lower.
- The terrain continuity grid now extends north to z −8.9. This caught a 0.38 m step at z = 0 in `BaseHeight` that predates the hamlet; the village flattening now fades in over 3 m. The old bridge causeway (|x| < 4.5, z < −3) is skipped because its banks are steep by design.
- Each resident's talk spot is standable: the dialogue check now lets physics settle the player for 10 frames before `Interact()`. This check passed on its first run. It hardens the old teleport-only dialogue check rather than reproducing a defect.

The foliage back-face shading was checked with a render probe, since headless runs have no renderer. A leaf card lit from above measured luminance 1.000 from above and 0.627 from below. With `cull_disabled`, Godot flips `NORMAL` on back faces. After `foliage.gdshader` undoes that flip, both sides read 1.000.

Frame rate was not re-measured for these changes. Evidence is in `artifacts/hamlet/minors/`: both test logs, `foliage-probe.txt`, and eight `review_hamlet.gd` captures. These include a new `brook-mouth` view. The `terrace` camera was also moved, because the earlier capture had it inside a keyaki crown.

## Organic hamlet — 2026-09-29

Hoshikawa was rebuilt as an organic hamlet. The changes are new terrain terms in `Height()`, six Japanese building archetypes, a brook and footbridge, a terrace, a shrine, generated tree models and village dressing. The C# build passes with the expected NU1900 warning, and so does the Godot import. **242 checks pass headless and in native Metal** (Forward Mobile, Apple M1 Pro). Each run exits 0 with `SELF_TEST_PASS`, and neither log contains runtime warnings or errors. The new checks cover:

- every house corner on a level pad within 5 cm;
- the carved brook, the footbridge carrying the player, and climbing out of the brook channel;
- walking up the torii steps onto the shrine plateau;
- reaching the west terrace by its ramp;
- ray-cast clearance of the spawn → rice-field route;
- archetype roofs, with Komorebi's buildings unchanged;
- each tree species loading with game materials and standing grounded (31 village trees and shrubs);
- the first countryside boulder's exact position, so the seeded scatter stream stays fixed.

Three more checks were added after the whole-branch review:
- `Height()` has no step larger than 0.1 m across a 5 cm grid of the village. This caught a 0.48 m step where two house aprons overlapped; the pad blend is now continuous.
- Every face of the 1 m terrain mesh along the brook banks is at most 40°. The banks were 44°; the widened channel brings them to 37.4°.
- Each village trunk's base meets the ground on every side, so no base floats on a slope. Trunks now start 0.5 m below ground.

All previous route, dialogue, combat and quest checks still pass. These are engine-injected checks, not a physical keyboard/mouse session.

Frame rate: at the 60 Hz vsync cap the village holds 59–60 fps at three viewpoints. With vsync off it reaches 118–120 fps, against 144–160 fps at the base commit, so the new trees and buildings cost about 1.5–2 ms per frame. No visibility ranges were needed.

Evidence is in `artifacts/hamlet/`:

- `headless-test.log` and `graphical-test.log`;
- `fps.txt`;
- `before-after.png`, comparing `--overview`, `--portrait` and the `original-village` landscape view;
- `final/`: seven `review_hamlet.gd` captures plus the six `review_valley.gd` captures;
- `task1`–`task5/`: per-step captures;
- `before/valley/`.

Running `review_valley.gd` during this work overwrote the gitignored `artifacts/countryside/` renders. They were regenerated from base commit `7c3af66` in a scratch worktree and restored, but they are re-renders, not the original files. Generated textures are used unedited, with two exceptions: the foliage atlas had its blue key removed, and the bark atlas was split into quadrants. Provenance is in `ASSETS.md`.

## Shared character importer — 2026-09-20

`tools/import_character.py` now handles saved Blender, GLB, glTF and FBX models; Chiyo and KOMA-7 rebuild through small compatibility presets. `tools/check_character_import.py` passes in Blender 5.2.1, exercising all four formats, transformed multi-mesh skinning, source/texture preservation, grounding, clip renaming and actual animated samples, optional authored idle, static meshes and invalid-input/overwrite guards. This is a local export-ready-model workflow, not automatic rigging or combat retargeting; Mio's specialized importer remains unchanged.

Both production NPC GLBs were rebuilt with the shared importer. Chiyo retains 41 bones, 89,999 triangles, a 1.58 m height and breathing idle; KOMA-7 retains 65 bones, 96,316 triangles, a 1.25 m height and head-turn idle. Every embedded image matches the preceding exports byte for byte, including combined roughness/metallic maps. Original source hashes and Mio/villager/patrol GLB hashes are unchanged. The exporter still reports the existing sampler-combination warning and Chiyo's four-influence skin-weight normalization warning.

C# build and Godot asset import pass. **138 checks pass both headless and in native Metal**, each exiting 0 with `SELF_TEST_PASS` and no runtime warnings/errors. The build retains the expected NU1900 audit-feed warning. Fresh front, face/idle, side, back and dialogue captures were inspected, with front views compared directly to the preceding exports; no visible import regression was found. This uses engine-injected input, not physical keyboard/mouse testing. Evidence: `artifacts/generic-import/`; per-asset source hashes/options/export reports: `artifacts/model-imports/{chiyo,koma7}/build.json`. Usage and the reproducible importer check are in README's import section.

## KOMA-7 Tripo integration — 2026-09-20

KOMA-7 now uses the supplied cream-and-jade gardener model instead of the patrol robot variant. The source has 65 bones and no actions. The game copy retains all 48,138 source vertices / 96,316 triangles, scales to 1.25 m, and includes one authored six-second head-turn idle. His capsule and label fit the smaller model; village position, dialogue and role are unchanged.

C# build and Godot asset import pass. **138 checks pass in both headless and native Metal runs**, each exiting 0 with `SELF_TEST_PASS` and no runtime warnings/errors in the final logs. Checks cover the exact KOMA-7 model and rig, textured material, idle playback/deformation/duration, fitted collision, stationary hips/both feet, dialogue, and the existing movement/combat/quest/restart behavior. The build retains the expected NU1900 vulnerability-feed warning. These are engine-injected integration checks, not a physical keyboard/mouse acceptance session.

The 24,144,100-byte GLB audit confirms 65 bones, 96,316 triangles, a closed six-second animation and normalized skin weights (maximum sum error 9.78e-8). Embedded base-color and normal images match the original packed bytes. Four original 4K texture files are preserved; Blender combines roughness and metallic for glTF. The original `.blend` and Mio/Chiyo/villager/patrol GLB hashes are unchanged. Blender reports a sampler-selection warning while combining texture nodes; the exported material and textures were inspected in Godot.

Six native captures were inspected: front, face, idle, side, back and dialogue. Front/face/idle/dialogue use the actual village placement. Side/back temporarily place KOMA-7 on the street, grounded by a physics ray, for an unobstructed view; review close-ups hide his name label. Evidence and reproducible capture script: `artifacts/koma7-integration/` and `tools/review_koma7.gd`. Sandbox-only Blender startup/editor-directory failures were resolved by running the installed applications with their normal permissions.

## Chiyo Tripo integration — 2026-09-16

Chiyo's supplied 41-bone model replaces her shared villager instance. The original contained no actions; the game copy has a four-second authored breathing idle with stationary hips/feet, a 1.58 m height, and 89,999 triangles. Her placement uses the existing terrain height, with her label and capsule fitted to the new model. She faces the village road and retains her existing dialogue.

C# build and Godot import pass. **124 checks pass in both headless and native Metal runs**, each printing `SELF_TEST_PASS`, exiting 0, and showing no runtime warnings/errors. New checks verify Chiyo's exact rig and source scene, idle playback/loop/deformation, planted foot, textured material and terrain placement. The existing dialogue, player movement/jump, combat, other residents and full quest/restart checks still pass. The expected NU1900 audit-feed warning remains in the build.

The exported GLB audit verifies normalized four-joint skinning (maximum weight-sum error 1.42e-7), 89,999 triangles, the closed four-second animation loop, and unchanged embedded base-color/normal bytes. Roughness and metallic maps are combined by the glTF exporter. The original Chiyo source and Mio/villager/patrol runtime hashes are unchanged. Native front, face, side, back and dialogue captures were inspected. The front/face/dialogue show her actual village placement; side/back temporarily move her to the street in the capture process to avoid building occlusion. No separate physical keyboard/mouse acceptance session is claimed. Evidence: `artifacts/chiyo-integration/`.

## Tripo jump correction — 2026-09-13

The source jump contains five hops. The runtime export now retains frames 1–13, removes the extra vertical travel and maps ascent/descent to player velocity. Contact recovery starts only after a physical landing; long falls hold the descent pose. The original Tripo source remains unchanged, and all eleven other animation channel payloads match the preceding export byte for byte.

C# build and Godot import pass. All **118 gameplay checks pass**, including standing, running, consecutive jumps and long falls. Native before/after recordings and sampled bone motion confirm the repeated bouncing is gone. These checks use engine-injected input, not a separate physical keyboard session. The build retains the NU1900 audit-feed warning. Verbose self-test shutdown identifies the outstanding resource warning as `assets/audio/valley.wav` and its audio playback instance; it is not an animation assertion failure. Evidence and reproduction commands are in `artifacts/tripo-jump/README.md`.

## Second arrangement review fixes — 2026-09-13

Moved the lodge/store signs onto their awnings, supported all three workshop tables, placed the clinic marker clear of its window planter, and hung orchard fruit below the canopy with stems. Also moved Ren clear of the workshop table after the close-up exposed his overlap.

The C# build passes with the existing NU1900 audit-endpoint warning. All **81 headless integration checks pass**; the final run exits 0 without warnings or errors. Thirteen native Metal captures were inspected, with passing checks for both signs' visibility past the awnings and Ren's table clearance. Evidence and the retained first-run certificate-store error are documented in [the fix results](/Users/chanwaichan/workspace/sirius/artifacts/arrangement-fixes-2/RESULT.md). No new physical keyboard/mouse session is claimed.

## Asset arrangement follow-up — 2026-09-13

All **81 integration checks pass** in headless Godot and the native Metal renderer. Both final runs exit 0 without runtime errors or warnings. Earlier headless runs intermittently reported one resource still in use during shutdown; the original occurrence remains in `headless-test-first.log`, and its intermittent cause is not established. The C# build succeeds with the existing NU1900 vulnerability-audit endpoint warning.

The controller now walks onto the shelter and overlook floors without jumping, reaches each Komorebi service from the street, enters both growing areas through fence openings, and reaches Hoshikawa's rice fields. Closed fence sections have collision. Raycasts against the real terrain collider measure a maximum 0.051 m separation for the new garden surfaces. The original combat, dialogue, bridge route, mission and restart checks still pass.

Early graphical attempts were interrupted by OS focus changes releasing injected input. `graphical-focus-check.log` records focus loss at physics frame 116 and the held movement action disappearing. The test now maintains its intended held actions across those resets; the late-guard check waits until the actual deflect window closes. The final graphical log records restored run, forward and guard actions and still passes every check. This is engine-injected input validation, not a physical keyboard/mouse acceptance session. Gameplay input handling is unchanged.

Evidence is under `artifacts/arrangement-after/`: `headless-test.log`, `graphical-test.log`, `audit.log`, and the rendered arrangement captures. `tools/review_arrangement.gd` reproduces the inspection views, including rice-plant contact in `rice-grounding.png`. The original review is retained in `artifacts/arrangement-review/REVIEW.md`; pre-edit source copies are text files in `artifacts/arrangement-before/`.

## Countryside expansion — 2026-09-12

C# build passes with the existing NU1900 audit-endpoint warning. The expanded map passes **71 checks** in both headless Godot and the native Metal renderer, with `SELF_TEST_PASS`, exit 0 and no runtime errors or warnings in the final logs.

The real player controller traverses the new route from the relay through the viaduct and across Cedar Bridge into Komorebi, then returns via the overlook and shelter. Checks cover all 11 buildings and 10 residents, Emi's recovery service, the northern collision boundary, and the original combat and complete relay mission. This is engine-driven input validation; no separate physical keyboard/mouse session is claimed.

Evidence: `artifacts/countryside-headless-test.log`, `artifacts/countryside-graphical-test.log`, and six inspected landscape renders under `artifacts/countryside/`. Regenerate the renders with `--script res://tools/review_valley.gd`. The pre-expansion world sources are saved as text under `artifacts/countryside-before/`.

## Earlier character validation

Latest character import and graphical gameplay check validated locally on 2026-09-09 with Godot **4.6.2 Mono**, .NET 8 target, and Apple M1 Pro / native Metal rendering.

| Gate | Result |
|---|---|
| C# build | Pass, 0 errors. One NU1900 warning: NuGet vulnerability-audit endpoint unreachable. The required Godot packages were available and restored. |
| Asset import | Pass: generated PNGs, audio and three animated GLBs. |
| In-engine gameplay integration | **57 checks passed**, `SELF_TEST_PASS`, exit 0. |
| Final graphical integration runtime log | No errors or warnings. |
| Visual inspection | Village, terrain, character rig, combat feedback, dialogue and title screen inspected using actual Godot-rendered screenshots. |
| Physical keyboard/mouse session | Not repeated for this model revision. Engine input actions were exercised by the integration check. |

The integration check covers actual physics movement, running, jumping, landing, dodge displacement and invulnerability, walking from the village across the bridge, all six NPC conversations, mission acceptance, deflection timing, guard posture, unblockable strikes, one-hit-per-swing behavior, skill range/cooldown, posture break/finisher, healing, enemy patrol and pursuit, an enemy's own timed attack, pause, relay activation, mission completion and fresh restart.

Final graphical command:

```sh
/Applications/Godot_mono.app/Contents/MacOS/Godot --path . --fixed-fps 60 --log-file artifacts/human-graphical-test.log -- --self-test
```

Evidence:

- `artifacts/village.png`: final normal gameplay view.
- `artifacts/village-overview.png`: village layout.
- `artifacts/combat-deflect.png`: timed-deflect feedback during the integration check.
- `artifacts/mission-complete.png`: completed prologue dialogue with Ren in the village.
- `artifacts/title.png`: illustrated entry screen.
- `artifacts/human-graphical-test.log`: detailed local results; runtime logs are git-ignored.

Frame-rate readings in screenshot logs are brief local samples, not a sustained benchmark. No hosted build, export-package testing, or physical input validation is claimed.

## Mio model from turnarounds — 2026-09-09

| Asset | Skinned meshes | Triangles | Vertices with significant blended weights |
|---|---:|---:|---:|
| Mio | 15 | 157,979 | 27,023 |
| Shared human villager | 14 | 133,500 | 24,063 |

The full-body and head sheets were generated before this revision and retained in `assets/art/`. Five calibrated reference planes are stored in the `MioTurnaroundGuides` Blender scene, outside the runtime export. The existing 58-bone, 12-clip rig and deformable base are retained; both mesh coordinates and bone rest positions use the same reference fit. Sleeve weights were corrected at the authored cross-sections.

`python3 tools/check_mio_export.py` passes against the final GLB: finite geometry, normalized four-weight skinning, valid bone indices, all named clips, polygon budget, and byte-for-byte identity of all four embedded source textures. Godot's extracted new costume and head-sheet PNGs also match the source bytes. `artifacts/mio-export-check.json` records the exact asset hash.

Godot Mono import passes. The latest Blender facial volume revision passes all **57 checks**, reports `SELF_TEST_PASS`, exits 0 and has no errors or warnings. An earlier turnaround revision had an intermittent timed-deflect failure while Blender was rendering; its failed log remains at `artifacts/turnaround-first-graphical-test.log`. No C# gameplay changes were made.

The latest Godot integration log and inspected Blender front, profile, three-quarter, clay, full-body, running and raised-arm renders are collected in `artifacts/MIO_BLENDER_REFINEMENT.md`. The preceding model, source and renders are preserved in `artifacts/face-volume-before/`. The model still simplifies the reference's fine cloth folds, ornaments and facial nuance; passing functional checks does not establish visual parity.

The preceding satchel revision initially failed the running-speed check; the unchanged rerun passed all 57 checks. The failure is preserved in `artifacts/satchel-first-graphical-test.log`; its intermittent cause remains unresolved.
# Tripo player replacement — 2026-09-13

Mio now uses the user-provided Tripo model. Final GLB validation passed with 41 bones, 12 clips, 150,600 triangles, normalized skin weights and exact matches to both packed 4K textures. The four supplied clips deform the imported skeleton and remain centered on the physical controller. C# build, Godot import and the final in-engine self-test passed (105 checks); native village captures verify the model and animation poses. The build reported one NU1900 warning because NuGet vulnerability metadata was unavailable. Details, backups, the recovered Blender session and remaining hand/cloth limitations are in `artifacts/tripo-integration/README.md`. Earlier model-specific counts below are historical.
