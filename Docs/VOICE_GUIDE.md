# Voice guide

How the voice-over works, where the lines live, how to change them, and how to explain it in the viva.

## 1. The idea in one paragraph
The game is not talk-heavy. Voice is mostly short lines (2 to 8 words) that react to what the player does: running, getting into the jeep, a crash, a puzzle step, a hit in a fight, the Dawn Clock, a villager nearby. A few story lines and conversations come on top. One **VoiceManager** decides which line plays, so two voices never talk at the same time and the same line is not repeated over and over.

## 2. Where everything is
| What | Where |
|---|---|
| All lines (the single source of truth) | `Docs/VOICE_LINES.csv`: 166 lines: 133 event lines and 33 story and intro lines |
| Voice per character (Kokoro blend, speed, pitch) | `Tools/tts/voices.json` |
| Script that makes every clip | `Tools/tts/generate_voices.py` |
| The clips | `Assets/_Project/Audio/Voice/<Speaker>/<line id>.wav` (44.1 kHz mono, -16 LUFS) |
| Your own recordings (optional) | `Assets/_Project/Audio/Voice/Recorded/<line id>.wav` (or .mp3, .ogg, .aiff) |
| The database Unity reads | `Assets/_Project/Resources/VoiceDB/VoiceDatabase.asset` (built from the CSV) |
| The code | `Scripts/UI/VoiceManager.cs` (decides and plays), `Scripts/UI/VoiceWatchers.cs` (notices events), `Scripts/Core/VoiceData.cs` (data and hooks), `Scripts/UI/VoicePlayer.cs` (story lines) |
| Test scene | `Assets/_Project/Scenes/Dev/VoiceTestScene.unity`: a button for every trigger |
| Tests | `Tests/PlayMode/VoiceSystemTests.cs` (8 tests) |
| Helpers for level design | `Scripts/UI/VoiceTrigger.cs` (speaks when the player walks into a trigger collider), `Scripts/UI/VoiceOnEvent.cs` (speaks when a button, animation event or Timeline signal calls `Fire()`) |
| Quality reports | `Docs/QA/voice/summary.txt`, `clip_report.csv`, `audition.txt`; `Docs/QA/voice_trigger_log.txt` |

## 3. The CSV columns
| Column | Meaning |
|---|---|
| id | Unique name, also the clip's file name. Story lines use the same id as in `story.json`. |
| speaker | aryan, pujari, meera, madhav, bhaskar, kishore, courier, robber, villager_m, villager_f, narrator |
| text | What is said and shown as the subtitle |
| trigger | The event that can play it (for example `jeep_enter`, `fight_hurt`, `clock_half`). `story` = played by the story system. |
| conditions | Extra rules, separated by `;`: `zone=Vaults`, `objective=obj_v3_jungle`, `notobjective=...`, `jeep`, `nojeep`, `fight`, `nofight`, `rain`, and event details such as `enemy=kishore`, `evidence=list`, `speaker=pujari` |
| priority | 1 chatter, 2 reaction, 3 gameplay, 4 story bark, 5 critical (conversation, intro, ending) |
| cooldown | Seconds before this trigger may speak again |
| chance | 0 to 1: how often the trigger speaks when it happens |
| once | 1 = only once per save |
| subtitle | 1 = show a subtitle |
| fx | none, `cabin` (inside the jeep), `vault` (stone hall echo) |
| clip | Where the clip is saved |
| source, note | Where a story line comes from |

## 4. How the VoiceManager decides (what to say in the viva)
1. **An event happens.** `VoiceWatchers` checks the game every frame and fires a trigger only when something changes (edge-triggered). Examples: the player starts running, the jeep's damage state goes up, a puzzle part reaches its right position, the clock passes half time.
2. **Is a voice allowed now?** Not during a cutscene, a conversation or the pause menu (except the ending narration).
3. **Cooldown.** Each trigger waits its cooldown before speaking again, so a long run does not produce a line every second.
4. **Pick a line.**
   - Only lines whose conditions fit (the right place, in the jeep or not, the right enemy).
   - Never a once-only line that already played.
   - Never the same line as last time for this trigger.
   - A small line (priority 1 or 2) is not said again within 45 s, even when it is the trigger's only line (the **repeat gap**).
5. **Chance and the quiet gaps.** Low-priority lines (1 and 2) are rolled against their chance. After a small line there is a quiet gap:
   - Chatter (priority 1) waits the whole gap after any small line.
   - A reaction (priority 2) waits only a third of it after another reaction, and chatter never holds it back. So "Let's go." when getting into the jeep is not silenced by "Our ride." a moment earlier.

   The **Voice lines** setting changes the chance, the cooldowns and the gaps:
   - **Low:** chance × 0.5, cooldown × 1.6, gap 30 s, repeat gap 72 s.
   - **Normal:** chance × 1, cooldown × 1, gap 12 s, repeat gap 45 s.
   - **High:** chance × 1.4, cooldown × 0.7, gap 6 s, repeat gap 32 s.

   Story lines are never reduced.
6. **The priority queue (no overlap).**
   - If nothing is playing, the line plays.
   - If a lower-priority line is playing, it fades out in 0.07 s and the new line plays.
   - If an equal or higher line is playing, an important line (priority 3 or more) waits in the queue, and gives up after 6 s (12 s for priority 4 and 5) because it is no longer relevant. Chatter is simply dropped.
7. **Playing.**
   - Aryan and the narrator are 2D, like a voice in your head; NPC event lines are 3D at the speaker's head, so they sound from where the person stands. Story lines stay 2D, as before.
   - The clip goes through the mixer's `VoiceAryan` or `VoiceNPC` group. **Ducking:** music drops 6 dB and ambience about 4 dB while anyone speaks.
   - The subtitle shows the speaker's name in their colour (setting: Subtitles).
   - **Lip sync:** `FaceAnimator` follows the clip's loudness and sound. Meera uses mouth shapes (visemes), Aryan his jaw bone, and the others (models with no face rig) nod with the voice. Models that have eyelids (blend shapes or bones) also blink.
   - The NPC turns its head toward the player (up to 45 degrees) and plays its talking gesture where the model has one.
8. **Pause and scenes.** The pause menu holds the voice where it is and it continues after; a scene change stops everything; a conversation or cutscene stops event lines.
9. **Debug.** F10 (development build or Demo mode) shows the last 10 decisions and why each line played or not.

**Why this design:**
- **A queue with priorities** means important lines are never lost and nothing overlaps.
- **Cooldowns, the chatter gap and "no repeat"** stop the game feeling like constant chatter.
- **Edge-triggered watchers** keep voice code out of the gameplay scripts, and a state that lasts says its line once.
- **A CSV as the source** means a line can be changed without touching code.

## 5. How the clips are made
`Tools/tts/generate_voices.py` reads the CSV. For each line:
1. **Use your recording** if `Voice/Recorded/<id>.*` exists.
2. **Otherwise Kokoro-82M** (Apache-2.0, offline) speaks the text with the character's voice: a blend of Kokoro voice styles, a speed and a pitch from `voices.json`. The Indian Kokoro voices speak English for the Indian cast (chosen by an audition: `Docs/QA/voice/audition.txt`).
3. **Clean:** trim silence, 10 ms fade in, 40 ms fade out; for `cabin`, a band-limited car-interior sound, and for `vault`, an echo.
4. **Level:** 44.1 kHz mono, gain to -16 LUFS, peak limiter at -1.5 dB true peak.
5. **Check:** Whisper (offline) transcribes the clip. If it hears different words, up to three more takes are tried and the clearest is kept. Results: `Docs/QA/voice/clip_report.csv` and `summary.txt`.
6. Unchanged lines are skipped (`Assets/_Project/Audio/Voice/manifest.json`).

### Setup (once, about 500 MB in the git-ignored `Tools/tts/.venv` and `.models`)
```
python3 -m venv Tools/tts/.venv
Tools/tts/.venv/bin/pip install kokoro-onnx soundfile numpy faster-whisper
cd Tools/tts/.models   # create it first
curl -LO https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/kokoro-v1.0.onnx
curl -LO https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/voices-v1.0.bin
```
ffmpeg is needed (`brew install ffmpeg`). The Whisper model downloads by itself the first time.

## 6. Add or change a line
1. Edit `Docs/VOICE_LINES.csv`:
   - **To change a line:** edit its text.
   - **To add a line:** add a row with a new id and an existing trigger, or a new trigger (see step 4).
2. Make the clip: `Tools/tts/.venv/bin/python Tools/tts/generate_voices.py`. Only changed lines are made; `--force` remakes all, and `--only id1,id2` remakes some.
3. Build the database in Unity with **Tools > Voice > Build database from CSV**. From Terminal: `Tools/dev/cc.sh LegacyOfTemples.World.Editor.VoiceDatabaseBuilder.Build`.
4. A **new trigger** also needs something that fires it:
   - `VoiceEvents.Fire("my_trigger")` from any script;
   - or `VoiceManager.Fire(...)`;
   - or a check in `VoiceWatchers`;
   - or, with no code, in the Unity Editor: **Add Component > Legacy of the Temple > Voice Trigger (area)** on an object with a trigger collider (speaks when the player walks in), or **Voice On Event** (connect its `Fire()` to a button, an animation event or a Timeline signal).

   The manager still decides: cooldown, chance, priority and conditions all apply.
5. Test it in `VoiceTestScene` (press Play, click the trigger's button) and run the tests: `zsh Tools/dev/pt.sh VoiceSystemTests`.

## 7. Replace the TTS with real recordings
1. Record each line (any microphone; WAV, MP3, OGG or AIFF).
2. Name the file exactly like the line id, for example `Recorded/v3_puj_01.wav`, and put it in `Assets/_Project/Audio/Voice/Recorded/`.
3. Run `generate_voices.py`. It uses the recording instead of Kokoro, cleans and levels it, and writes it to the speaker's folder.
4. Build the database (step 6.3). Without running the script, the database builder still uses your raw recording.

## 8. Settings the player sees
In Pause > settings and the main menu settings:
- **Voice lines:** Low, Normal (the default) or High.
- **Subtitles:** On or Off.
- **Voice volume.**

## 9. Viva: likely questions and short answers
- **How do you stop two characters talking at once?**
  - One manager owns every voice.
  - It has one "now playing" line and a short queue.
  - A more important line fades the current one out in 0.07 s.
  - A less important line waits in the queue (and gives up after 6-12 s) or is dropped.
  - The bot counted 0 frames with two voices in a whole game.
- **Why priorities?** A story line or "the clock is nearly out" matters more than "Up we go." when jumping, so it must win.
- **Why cooldowns and chance?**
  - Without them a running player hears a line every second.
  - Cooldown is per trigger; chance makes it feel natural, not mechanical.
  - The repeat gap stops one line coming back too soon.
- **Why a CSV?**
  - Writers can change lines in a spreadsheet without touching code.
  - The editor tool turns it into a ScriptableObject that Unity loads fast.
- **How is the voice made?**
  - Kokoro-82M, an open text-to-speech model (Apache-2.0), running offline.
  - Then ffmpeg cleans and levels each clip.
  - Whisper checks that each clip says the right words.
  - Recordings of real actors replace any clip without code changes.
- **How does lip sync work?**
  - Every frame the face reads the playing clip's loudness and frequency spectrum.
  - Meera's mouth uses visemes and Aryan opens his jaw bone; models with no face rig nod with the voice. Models with eyelids also blink.
- **What is ducking?** While someone speaks the mixer turns the music down 6 dB, so words stay clear.
- **Why are some voices 3D?**
  - A villager or a robber taunting should sound from where they stand.
  - Aryan's own lines are in the player's head, so they are 2D.
- **How did you test it?**
  - 8 play-mode tests: every trigger can speak, priorities, no overlap, cooldowns, the frequency setting, the repeat gap, the story path and the helpers.
  - A bot played the whole built game and wrote every decision to `Docs/QA/voice_trigger_log.txt`.
  - A demo video records the game's own picture and sound.
