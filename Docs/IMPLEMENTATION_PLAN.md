# LEGACY OF THE TEMPLE — Implementation Plan (v2 thriller)

Status: PHASE A document, written after a read-only pass over the code. Nothing outside Docs/ changed.
Backup tag: `backup-script-v2`. Implementation starts only after the word "approved".

## 1. What the code looks like today (facts from the read-only pass)

| System | File | What it does now |
|---|---|---|
| Story data | Scripts/Gameplay/StoryData.cs, Resources/Story/story.json | JSON with speakers (6), conversations (8), objectives (17), one ending. Lines are `{s, t}` with no id. Choices only add lines; they set no flags. |
| Story flow | Scripts/Gameplay/StoryDialogueSystem.cs (430 lines) | Linear objective index. Completion types: dialogue, clues, scene, puzzle, minigame. Saves to PlayerPrefs (`Story_*`). Score formula. Loads scenes directly. |
| Guide | Scripts/UI/GuideSystem.cs | 3 hints per objective, diamond marker with distance. |
| Tools | Scripts/Gameplay/InvestigationTools.cs | Flashlight (G), ForensicKit (K, two questions), JungleStealth (follow, look-back, vision cone, resets). |
| Puzzle | Scripts/Gameplay/ChakraPuzzleController.cs | Four symbols in order, then the centre. No timer, no penalty. |
| Player | Scripts/Characters/PlayerController.cs (374 lines) | Walk, run, crouch, jump, interaction scoring, climb guard. Reads the keyboard directly. No health, stamina or combat. |
| Input | Scripts/Core/InputGate.cs | UI owns keys while panels are open. Good base for a combat mode. |
| Audio | Scripts/Core/AudioManager.cs (299 lines) | Five plain AudioSources (BGM, ambience, SFX, footsteps, dialogue), clips from Resources/Audio by name, **no Audio Mixer**, one stone footstep set. |
| Voice | Scripts/UI/VoicePlayer.cs | Plays `Resources/Voice/<speaker>/<md5 of text>.wav`, falls back to blips. Keyed by text hash, so editing a line breaks its clip. |
| Scene loading | SceneTransitionTrigger, StoryDialogueSystem, MainMenuController, pause menu, GuideSystem, GameLevelManager | Six places call `SceneManager.LoadScene` directly. WorldManager has an async loader that most of them do not use. **No loading screen.** |
| Cinematic | Scripts/Gameplay/OpeningCinematic.cs | Shot list with subtitles; skippable. Reusable for the cold open. |
| Look | Scripts/World/LookTuner.cs | Runtime global volume, fog, lights, material tint per scene. |
| Test harness | Scripts/UI/AuditBot.cs, Tests/PlayMode/* | Bot plays the story in the built app; PlayMode tests for ground, jump and Space. |
| Characters | Prefabs/Characters/* | Heights normalised. **Robber1 and Robber2 show the wrong models** (winged, armoured). Robbers have no humanoid avatar. |

## 2. Reuse, change, add, delete

### Reuse as it is
- ScreenText / ScreenTextRenderer (all new panels: Codex, clock, choices, loading text).
- HeritageFonts, HeritagePauseMenu, MainMenuController (add a difficulty option and a Codex button).
- InputGate, EventBus, Constants, CharacterGrounding, OrbitCamera, NPCDialogueTrigger.
- OpeningCinematic (new shot list for Chapter 0), ClueInspectable, InvestigationJournal.
- AuditBot and the PlayMode test pattern (extended in Phase F).

### Change
| File | Change |
|---|---|
| StoryData.cs | New classes for the v2 schema (section 3). Keep the loader. |
| StoryDialogueSystem.cs | Flags and trust; choices with effects; chapter and checkpoint state; new completion types (chase, relight, deduction, disarm, timed_puzzle, stealth, combat); conditional lines; endings by rule. Move saving to one JSON blob (`Story_SaveV2`). |
| VoicePlayer.cs | Look up by line id first (`<id>.wav`, then `<id>_tts.wav`), then the old hash, then blips. Add the effort tier (calm, firm, shout) to choose the mixer snapshot. |
| GuideSystem.cs | Per-difficulty rules (cost in story minutes; off in chases and the fight on Hard). |
| InvestigationTools.cs | Flashlight: battery and flash burst. ForensicKit: four real tools replace the two questions. JungleStealth: becomes the shared stealth sensor (detection meter, hearing, pebbles). |
| ChakraPuzzleController.cs | 90 s reset, wrong-order penalty, dust, hand-wheel crank phase. |
| PlayerController.cs | Combat mode (Space = dodge only in the arena), vault and slide, health and stamina hooks. Movement code stays; combat lives in a new component. |
| AudioManager.cs | Route every source through Audio Mixer groups; surface-based footsteps; snapshot control. Keep the public method names so callers do not change. |
| SceneTransitionTrigger, StoryDialogueSystem, MainMenuController, HeritagePauseMenu, GuideSystem, GameLevelManager | Replace direct `SceneManager.LoadScene` with one `LoadingScreen.Load(scene)`. |
| LookTuner.cs | Per-chapter lighting presets from the visual bible; dawn lerp driven by the clock. |
| UIManager.cs | HUD: Dawn Clock, health and stamina (fight only), detection meter, choice timer. |

### Add
| New file | Purpose | Phase |
|---|---|---|
| Gameplay/DawnClock.cs | Story time, budgets, penalties, pause rules, difficulty scale | B |
| Gameplay/GameDifficulty.cs | Easy / Normal / Hard table | B |
| Gameplay/StoryFlags.cs | trust_meera and other flags, save and load | B |
| Gameplay/CheckpointSystem.cs | Save and restore per chapter checkpoint | B |
| UI/CodexPanel.cs | Codex entries, unlock toast | B |
| UI/LoadingScreen.cs | Image, zoom, gold bar, chapter title, clock line, tip; async load | B |
| Gameplay/InterrogationTell.cs | Meera's tell mechanic | B |
| Gameplay/ChaseController.cs | Spline runner, rubber band, obstacles | B (Ch 1), reused in C |
| Gameplay/LampRelightPuzzle.cs, DeductionBoard.cs | Chapter 2 | B |
| Gameplay/TripwireTrap.cs, TimingRing.cs | Chapter 3 | B |
| Combat/CombatController.cs | Player attacks, block, counter, dodge, stamina | C |
| Combat/Health.cs, GuardMeter.cs | Shared stats | C |
| Combat/EnemyBrain.cs, BhaskarBoss.cs, KishoreAI.cs | State machines, telegraphs, phases | C |
| Combat/ArenaProps.cs | Brazier, rope and pole bundle | C |
| Combat/FinalGrapple.cs | Phase 3 pursuit and grapple | C |
| Gameplay/StealthSensor.cs, PebbleThrow.cs | Shared stealth | C |
| Audio/AudioMixer asset, Audio/MusicDirector.cs, Audio/SurfaceFootsteps.cs, Audio/ReverbZone triggers | Mixer, adaptive music, footsteps | D |
| Docs/VOICE_SCRIPT_SHEET.csv | Recording sheet generated from the story data | D |

### Delete (only after confirming nothing else needs them)
| File | Why | Check first |
|---|---|---|
| Gameplay/HelpHintSystem.cs | Replaced by GuideSystem; only EnvironmentBuilder (Editor) refers to it | Remove the EnvironmentBuilder reference |
| Gameplay/TutorialPromptManager.cs | Replaced by TutorialManager; same situation | Same |
| UI/GameQATestBot.cs | Replaced by AuditBot | Same |
| Gameplay/GameLevelManager.cs `CompleteLevel1..3` | Old three-level flow; endings move to the story system | StoryDialogueSystem and AuditBot use parts of it; migrate, then delete |
| Old objectives and conversations in story.json | Replaced by v2 content | Kept in git history and under tag `backup-script-v2` |
| The current forensic two-question quiz | Replaced by tools | — |

Not deleted: ShowcaseDirector and GroundingEvidenceCapture (used by the main menu's showcase tour).

## 3. Draft v2 story.json schema (document only; the real file is untouched)

```json
{
  "version": 2,
  "clock": { "start": "03:10", "dawn": "05:45",
             "secondsPerMinute": { "easy": 12, "normal": 9, "hard": 7 } },
  "flags": [ { "id": "trust_meera", "type": "int", "default": 0, "min": -2, "max": 3 },
             { "id": "has_message", "type": "bool", "default": false } ],
  "speakers": [ { "id": "aryan", "name": "Aryan", "role": "Investigator",
                  "color": "#FFC857", "npc": "investigator", "voice": "ary" } ],
  "chapters": [
    { "id": "ch1", "title": "3:10 AM", "scene": "Courtyard", "budget": 25,
      "loading": { "image": "loading_01_dawn_courtyard", "title": "CHAPTER 1 — 3:10 AM" },
      "music": "calm",
      "checkpoints": [ "ch1_start", "ch1_chase" ] }
  ],
  "conversations": [
    { "id": "conv_c1_meera", "chapter": "ch1", "npc": "villager",
      "lines": [
        { "id": "c1_mee_01", "s": "meera", "tier": "firm",
          "t": "I saw two men. A red scarf and a black hood." },
        { "id": "c1_ary_04", "s": "aryan", "tier": "calm",
          "t": "You looked longer at one of them." },
        { "id": "c1_mee_02", "s": "meera", "tier": "firm", "t": "It was dark.", "tell": true }
      ],
      "choices": [
        { "id": "C1-A-1", "text": "You are safe telling me.",
          "effects": [ { "flag": "trust_meera", "add": 1 }, { "flag": "chase_hint", "set": true } ],
          "lines": [ { "id": "c1_ary_05", "s": "aryan", "tier": "calm",
                       "t": "Whatever you saw, you are safe telling me. When you are ready." } ] },
        { "id": "C1-A-2", "text": "I do not have time for a lie.",
          "effects": [ { "flag": "trust_meera", "add": -1 } ], "lines": [] }
      ],
      "choiceTimer": 0 }
  ],
  "objectives": [
    { "id": "obj_c1_chase", "chapter": "ch1", "title": "Catch the runner",
      "instruction": "Chase the figure along the east wall.", "keys": "Shift run, Space vault, C slide",
      "target": "runner", "area": "East wall",
      "complete": { "type": "chase", "id": "chase_c1" },
      "fail": { "type": "distance", "meters": 25, "seconds": 45, "penalty": 2,
                "checkpoint": "ch1_chase" },
      "when": null,
      "hints": [ "He knows the courtyard.", "Cut inside the well.", "Vault the low wall with Space." ] }
  ],
  "penalties": { "wrong_lamp": 2, "bell_rung": 3, "wall_wrong_order": 4, "wall_timeout": 2,
                 "spotted": 3, "guide_used": 1 },
  "barks": [ { "id": "bk_ary_01", "s": "aryan", "tier": "shout", "t": "Drop the blade!",
               "trigger": "counter", "cooldown": 6, "variants": 1 } ],
  "codex": [ { "id": "surya_pradeepa", "title": "Surya Pradeepa", "unlock": "c1_puj_02",
               "text": "A brass lamp-vessel lit at the first-light rite...", "fiction": true } ],
  "tips": [ "The lock was not forced. Ask who had the key." ],
  "endings": [
    { "id": "dawn_light", "title": "DAWN LIGHT",
      "when": "recovered && clock <= dawn && kishore_turned" },
    { "id": "late_dawn", "title": "LATE DAWN", "when": "recovered" },
    { "id": "lost_dawn", "title": "LOST DAWN", "when": "!recovered" }
  ],
  "closing": "Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history."
}
```

Notes on the schema
- `id` on every line is the voice-file name. Text can now be edited without breaking audio.
- `when` on lines, choices and objectives is a small condition string over flags (`trust_meera >= 2`).
  Supported operators: `>= <= == != && || !`. No scripting language.
- `effects` are the only way a choice changes state. `tell: true` marks the line the interrogation mechanic checks.
- Unity's JsonUtility cannot read dictionaries or mixed types, so `penalties` and `secondsPerMinute`
  become small serialisable classes, and flag values are stored as strings and parsed.
- A validator (Editor menu and a test) checks: unique ids, every line at most 120 characters, every
  speaker known, every `when` parses, every chapter has a loading image name, every voiced id has a row in the script sheet.

## 4. Risks

| Risk | Impact | Mitigation |
|---|---|---|
| Robber prefabs show the wrong models; no humanoid avatars | Blocks all of Phase C | First task of Phase C: re-import Bhaskar and Kishore read-only from the originals, set up humanoid rigs. If the originals cannot be rigged as humanoid, you must decide on a fallback before combat work. |
| Mixamo combat clips are not in the project | No real fight animation; faking is forbidden | You download the list in COMBAT_DESIGN.md. Until then Phase C can be built with placeholder poses and is clearly marked unfinished. |
| Scope: this is roughly a second game on top of the first | Credits and time | Phases are independent and each ends in a playable state. Phase B alone already gives the thriller story, the clock and three new mechanics. |
| Combat feel is hard to tune without a human playing | A fight that passes tests but feels bad | The bot verifies rules (damage, windows, phases). Feel needs your play sessions; plan two feedback rounds in Phase C. |
| Voices sound synthetic | Undercuts the thriller tone | Recorded voices (AUDIO_BIBLE.md). The id system means you can record at any time. |
| Sonniss and some scan libraries cannot be redistributed | Public GitHub repo would breach their terms | Use CC0 and own work only, or keep such files out of git. |
| Licences were not checked online in Phase A | A wrong assumption | Each file is verified on its page at download and logged before commit. |
| 60 FPS with fog cards, SSAO and many lamps | Frame drops in the jungle | Budgets in the visual bible; per-map FPS check in Phase F; fixed order for cutting effects. |
| The loading images do not match the hero and show a weapon | Inconsistent first impression | Regenerate or crop (LOADING_AND_VISUAL_BIBLE.md, A3). The loader works with or without them. |
| Old saves use the v1 keys | Broken "Continue" | On first v2 launch, clear `Story_*` keys and start a new game. |
| Existing uncommitted work in Assets/ by you during Phase A | Merge trouble later | Phase B starts with `git status` and a new backup tag; nothing is overwritten without looking. |

## 5. Order and effort

Effort is a rough estimate. "Unity runs" means batch compiles, tests or builds, which are the slow
and costly part. Credit share is the expected share of the total remaining work, not an exact number.

| Phase | Contents | Sessions | Unity runs | Credit share | Needs from you |
|---|---|---|---|---|---|
| B | v2 story data and validator, flags and trust, choices, objectives for Ch 0 to 4 and the ending, Codex, Dawn Clock, difficulty, checkpoints, loading screens, interrogation tell, Ch 1 chase, lamp relight and deduction, tripwires and forensic tools, timed Sun Wall | 2 | 6 to 8 | about 30% | The 4 loading images (optional; fallback exists) |
| C | Robber re-import and rigs, combat controller, health/guard/stamina, Bhaskar and Kishore AI, arena props, three phases, grapple, stealth sensor and pebbles, Ch 5 chase | 2 to 3 | 8 to 10 | about 35% | Mixamo clips; two play-feedback rounds |
| D | Audio Mixer and snapshots, reverb zones, surface footsteps, SFX import with attribution, voice id pipeline, script sheet, adaptive music stems | 1 | 3 to 4 | about 12% | Voice decision; recordings if chosen; approval of downloaded sounds |
| E | Lighting presets per chapter, materials and wet stone, fog cards and shafts, post-processing, props and vegetation, bake, performance pass | 1 to 2 | 5 to 6 | about 15% | Approval of the asset list; any own Blender props |
| F | One full test pass (bot on all endings and difficulties), one build, one 3-minute recording route | 1 | 2 to 3 | about 8% | Watch the recording route and sign off |

Each phase ends with: zero compile errors and warnings, the relevant tests, a note in
Docs/EXPLAINED.md, a commit and a push. No phase starts without your word.

### Phase F recording route (3 minutes, planned now so the game is built to show well)

| Time | Shot |
|---|---|
| 0:00–0:20 | Cold open: the lamp tips, the bell, title card |
| 0:20–0:45 | Courtyard: Meera's lie and the choice; the chase and the token |
| 0:45–1:05 | Storage Hall: flashlight search, the last lamp relit |
| 1:05–1:20 | Outer Shrine: disarming a bell tripwire; the torn note |
| 1:20–1:40 | Sun Wall: the timer, falling dust, the wall opening |
| 1:40–2:05 | Jungle: grass stealth, a pebble distraction, the overheard radio call |
| 2:05–2:45 | Final fight: counter, brazier, Kishore turns, the grapple at the ford |
| 2:45–3:00 | First light: the lamp is lit; DAWN LIGHT |
