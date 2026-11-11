# LEGACY OF THE TEMPLE — Audio Bible

Status: PHASE A document. Nothing has been downloaded or changed.

**Licence check status:** the licences below are stated from general knowledge of each source. They
were **not re-verified online in this phase**. Every file must have its licence confirmed on its own
download page at download time (Phase D) and recorded in the attribution table (section 9).

## 1. Honest note on voices

The game currently uses macOS `say` voices. They sound synthetic: flat emotion, wrong stress on names
such as "Aryan-ji" and "Surya Pradeepa", and no real shout. No amount of processing fixes that.

**Recommendation, best first:**
1. **Recorded voices** (you and friends, a decent mic). Best result by far, and free.
2. Recorded voices for Aryan, Bhaskar and the fight; `say` placeholders for the rest until recorded.
3. `say` voices for everything, with the processing chain below. Acceptable for a demo only.

The system below is built so recordings replace the placeholders **without code changes**: drop a file
with the right line id into the right folder.

## 2. Voice-line ids and file layout

- Every line in STORY_BIBLE_V2.md has an id, e.g. `c5_ary_04`. The id is the file name.
- Path: `Assets/_Project/Resources/Voice/<speaker>/<line_id>.wav` (speakers: aryan, pujari, meera, bhaskar, kishore, scroll).
- Lookup order at runtime: (1) `<line_id>.wav` recorded file, (2) `<line_id>_tts.wav` placeholder, (3) the existing text-hash clip, (4) the existing procedural blip.
- Variants for barks: `bk_ary_05_a.wav`, `_b`, `_c` (picked at random, no immediate repeat).
- Id scheme: `c<chapter>_<spk>_<nn>` for script lines, `bk_<spk>_<nn>` for barks. Speaker codes: ary, puj, mee, bha, kis, scr.
- Format: WAV, mono, 48 kHz, 24-bit source; imported as Vorbis quality 70, "Load in Background", mono.
- Loudness target per file: −18 LUFS integrated for calm and firm, −16 LUFS for shout; peaks below −1 dBTP.

## 3. Voice direction

| Character | Age / colour | Pace | Direction | Avoid |
|---|---|---|---|---|
| Aryan | 30s, calm baritone | Slow, exact | Says less than he knows. Dry humour without smiling. Under pressure he drops in pitch and gets quieter before he gets louder. | Sarcasm, shouting outside the fight |
| Pujari Ananda | 70s, soft gravel | Unhurried, breath before long lines | Warm, anxious, dignified. Fear shows as care with words, not volume. | Trembling "old man" caricature |
| Meera | Early 20s, bright, sharp | Quick; clipped when lying | Protective. The lie ("It was dark") is too fast and too flat. Confession lines slow down and lose the edge. | Whining |
| Bhaskar | 40s, smooth, low | Even, patient | Quiet menace. He sounds reasonable, which is what makes him frightening. He only shouts once, in phase 3. | Villain laugh, growling |
| Kishore | Late 20s, light, breathy | Uneven, rushes then stalls | Guilt in every line. He apologises with his tone even when the words do not. | Comic cowardice |
| Scroll / Codex | Ageless, neutral | Measured | Like something read many times. | Drama |

### Aryan's vocal effort tiers

| Tier | Where | How to perform | Mic distance |
|---|---|---|---|
| calm | Investigation, most dialogue | Chest voice, close, low volume, almost private | 10 to 15 cm, pop filter |
| firm | Orders, deductions, clock lines, chase | Same pitch, more breath support, harder consonants, no smile | 20 cm |
| shout | Final fight and barks | Projected from the diaphragm, pitched *down* not up, open vowels, commanding rather than angry | 40 to 60 cm, turned 20° off-axis, gain lowered 10 to 12 dB |

Shout lines must really be performed loud. Boosting a calm take never sounds projected.

## 4. Recording kit and script sheet

**What is needed**
- A USB condenser or dynamic mic (or a phone in a quiet room as a fallback), pop filter, headphones.
- A quiet, soft room: a wardrobe full of clothes or a duvet over the head works well. No fan, no fridge.
- Audacity (free). Record 48 kHz, 24-bit, mono. Peaks around −12 dB for calm, re-set gain for shout.
- 5 seconds of room silence at the start of each session (used for noise reduction).
- Three takes per line; clap or say the id before each line.

**Script sheet (generated in Phase D as `Docs/VOICE_SCRIPT_SHEET.csv`, one row per line)**

| line_id | speaker | tier | text | tone note | context | takes | best take | file delivered |
|---|---|---|---|---|---|---|---|---|
| c1_ary_02 | Aryan | calm | Then someone gave them the way in. | Realisation, not accusation | After the Pujari mentions the lock | 3 | | |
| c5_ary_04 | Aryan | shout | Enough, Bhaskar! | Command. Pitch down. | Fight phase 1 start | 3 | | |

Line counts: Aryan about 50 (including barks), Pujari about 22, Meera about 12, Bhaskar about 18,
Kishore about 16, Scroll 2 plus 5 Codex readings. Roughly 125 lines; about 2 hours of recording in total.

## 5. Mixer design (Unity Audio Mixer)

```
Master
├── Music        (stems: Calm, Tension, Stealth, Combat, Resolve)
├── Ambience     (beds + emitters)
├── SFX
│   ├── Footsteps
│   ├── Foley     (cloth, props)
│   ├── World     (doors, gears, flames, bells)
│   └── Combat    (impacts, swishes)
├── Voice
│   ├── Voice_Aryan
│   └── Voice_NPC
├── UI
└── Reverb return (send bus)
```

### Aryan's voice chain (Voice_Aryan group)

| Order | Effect | Calm / firm | Shout (fight snapshot) |
|---|---|---|---|
| 1 | High-pass | 80 Hz | 100 Hz |
| 2 | Low shelf (warmth) | +2 dB at 160 Hz | +3 dB at 180 Hz |
| 3 | Presence (ParamEQ) | +2 dB at 3 kHz, Q 1 | +4 dB at 2.5 kHz, Q 0.8 |
| 4 | De-harsh | −2 dB at 7 kHz | −3 dB at 6.5 kHz |
| 5 | Compressor | Ratio 2.5:1, threshold −20 dB, attack 15 ms, release 120 ms | Ratio 4:1, threshold −16 dB, attack 5 ms, release 80 ms, +2 dB make-up |
| 6 | Send to reverb | −14 dB | −8 dB, plus a 110 ms slap echo at −18 dB (the shout bounces off stone) |

NPC voices: high-pass 100 Hz, compressor 2:1, presence +1.5 dB, same reverb sends by zone.

### Reverb zones (snapshots blended by trigger volumes)

| Zone | Decay | Pre-delay | Character | Used in |
|---|---|---|---|---|
| Open courtyard | 0.9 s | 20 ms | Short slap from walls, mostly dry | Courtyard |
| Stone hall | 2.4 s | 35 ms | Dense, dark, long tail | Storage Hall, Sun Wall chamber |
| Shrine forecourt | 1.4 s | 25 ms | Medium, bright | Outer Shrine |
| Jungle | 0.5 s | 10 ms | Nearly dry, absorbed by foliage | Jungle Ruins stealth |
| Ruin courtyard | 1.2 s | 30 ms | Broken stone, uneven echo | Fight arena |

### Distance and occlusion

- Voices and world emitters are 3D with a custom rolloff: full volume to 3 m, −6 dB at 8 m, inaudible at 30 m.
- Low-pass by distance: cutoff 22 kHz at 0 m falling to 3.5 kHz at 25 m.
- Occlusion: one raycast from listener to emitter; if blocked, low-pass 1.2 kHz and −6 dB (overheard robbers behind stones sound muffled).
- Bhaskar's radio half-call: band-pass 400 Hz to 3 kHz on his radio's reply clicks only; his own voice stays natural.

### Ducking

Music −6 dB and ambience −4 dB while a voice line plays (side-chain from the Voice group, 150 ms
attack, 600 ms release). Combat SFX never duck voice barks; barks are short.

## 6. Adaptive music

One piece in one key and tempo (D minor, 80 BPM; combat doubles the feel to 160), exported as five
loop-synchronised stems. All stems play together; **Audio Mixer snapshots** fade their volumes.

| Stem | Instruments (direction) | Snapshot volumes: Calm / Tension / Stealth / Combat / Resolve |
|---|---|---|
| Calm | Bansuri-like flute, soft tanpura drone, sparse santoor | 100 / 40 / 0 / 0 / 60 |
| Tension | Low strings, slow heartbeat drum, bowed metal | 0 / 100 / 60 / 50 / 0 |
| Stealth | Muted tabla pattern, plucked low notes, shaker | 0 / 20 / 100 / 0 / 0 |
| Combat | Dhol and frame drums, driving ostinato, brass stabs | 0 / 0 / 0 / 100 / 0 |
| Resolve | Warm strings, temple bell tones, flute theme returns | 0 / 0 / 0 / 0 / 100 |

| Game state | Snapshot | Transition time |
|---|---|---|
| Exploring, dialogue | Calm | 3 s |
| Clock under 5 budget minutes, puzzle timer under 30 s, trap nearby | Tension | 1.5 s |
| Stealth section, enemy `Suspicious` | Stealth (Tension mixed in when the meter is over 50%) | 1 s |
| Chase and fight | Combat | 0.5 s, on the next bar |
| Phase end, recovery, ending | Resolve | 4 s |

Stingers (one-shots over the music): clue found, choice made, spotted, phase clear, objects recovered,
ending title. A 10-second silence-with-drone is used before the fight begins.

## 7. Sound effects list

Naming: `sfx_<group>_<name>_<nn>.wav`. Variation counts in brackets. Footsteps pick by the ground's
physics material or tag.

| Group | Sounds |
|---|---|
| Footsteps: stone | walk (6), run (6), land (2), scuff (2) |
| Footsteps: wet stone | walk (6), run (6), splash land (2) |
| Footsteps: wood | walk (5), run (5) |
| Footsteps: gravel | walk (6), run (6) |
| Footsteps: grass | walk (6), run (6), tall-grass rustle loop (1) |
| Footsteps: mud | walk (5), run (5), squelch land (2) |
| Cloth / foley | coat movement (4), scarf flick (2), satchel shift (3), kneel (2), notebook open/close, pen scratch |
| Brass | vessel lift, vessel set down, lamp lid, tin token drop (2), token pick-up |
| Bells | single temple bell (soft, hard), bell tail, tripwire bell jingle (3), bell muffled by hand |
| Flames | oil lamp loop, lamp ignite (3), lamp blown out (2), brazier loop, brazier knocked (2), ember scatter (2), torch whoosh |
| Night jungle | crickets bed, frogs bed, night birds (5 one-shots), distant animal call (3), leaves drip, branch crack (3) |
| Wind | courtyard wind bed, hall draught loop, jungle gust (3), cloth flag flap |
| Water | well water, puddle step (4), stream bed, ford rush loop, drip in hall (4) |
| Stone doors and gears | door grind open/close, disc rotate loop, disc notch click (3), wrong-order rumble, dust fall (2), crank ratchet (4), wall open (long), niche open |
| Tools | flashlight click on/off, flash burst, battery swap, forensic kit open/close, ruler slide, lens focus, paper pieces (3), chalk scrape |
| Stealth | pebble throw whoosh, pebble land (stone, grass, water), detection rise tone, spotted sting |
| Impacts | punch (5), heavy punch (3), block thud (4), blade on block (4), body fall on stone (3), stumble (2), pole bundle crash |
| Blade | quick swish (4), wide swish (3), draw, drop on stone (2) |
| Efforts and grunts | Aryan effort (3), pain (3); Bhaskar effort (3), pain (3); Kishore panic breath (2), pain (2) |
| Chase | vault (2), slide (2), step crumble (3), stone thrown (2) |
| Tension | heartbeat slow / fast loops, breath loop, low rumble riser (3 lengths), clock tick for the last minute |
| UI | hover, select, back, pause open/close, journal page, codex unlock, objective new, objective complete, choice appear, choice confirm, clock penalty, checkpoint, loading bar complete |

About 260 files. Phase D can ship with roughly 120 (the first variation of each) and add the rest later.

## 8. Source plan

| Source | Licence as understood (verify per file) | Use for | Caution |
|---|---|---|---|
| Freesound | Per-sound licence. Use the **CC0 filter only**. | Bells, flames, jungle, water, stone, foley | Skip CC-BY-NC and "Sampling+". Keep each sound's URL and author. |
| Kenney (kenney.nl) | CC0 | UI sounds, impacts, some footsteps | Stylised; fine for UI, less so for realism. |
| OpenGameArt | Per-asset licence. Use **CC0 only**. | Footstep sets, music candidates | Avoid GPL and CC-BY-SA audio in this project. |
| Sonniss GDC Game Audio bundles | Royalty-free under Sonniss's own licence; no attribution required | High-quality impacts, swishes, ambience | **The raw files may not be redistributed. This repository is public on GitHub, so Sonniss files must not be committed.** Either keep them in a git-ignored folder (then a fresh clone has no audio) or prefer CC0 sources. Recommended: CC0 only, to keep the repo complete. |
| Own recordings | Own work | Voices, cloth, footsteps on real stone, brass taps | Best match and no licence risk. |
| Music | Commission from a friend, own composition, or CC0 stems | Adaptive stems | A single CC0 track rarely comes as synchronised stems. Realistic plan: build five stems from CC0 loops in the same key and tempo in a free DAW, or record simple parts. |

Rules: CC0 or royalty-free only; no CC-BY-NC, no "editorial use"; nothing ripped from games, films or
YouTube; every downloaded file gets a row in the table below **before** it is committed.

## 9. ATTRIBUTION table template (append to ATTRIBUTION.md)

| File in project | Title | Author | Source URL | Licence | Date checked | Changes made |
|---|---|---|---|---|---|---|
| Audio/SFX/sfx_bell_temple_01.wav | (title on the source page) | (author) | (direct URL) | CC0 1.0 | YYYY-MM-DD | Trimmed, normalised |
| Resources/Voice/aryan/c1_ary_01.wav | Aryan line c1_ary_01 | (performer's name, with permission) | Own recording | Own work, used with permission | YYYY-MM-DD | Noise reduction, EQ |

Voice performers should confirm in writing (a message is enough) that their recordings may be used in
the game and in the public repository.
