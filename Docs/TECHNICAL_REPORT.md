# Legacy of the Temple: technical report

## 1. Platform
Unity 6000.6.8f1, Universal Render Pipeline, C#, new Input System, Cinemachine and Timeline for the intro. Built for macOS (`Builds/LegacyOfTheTemple.app`, development build). The repository root holds `Assets`, `Packages` and `ProjectSettings`; `My project/` links to them. `Library/`, `Temp/` and `Builds/` stay out of git.

## 2. Code layout (about 20,500 lines of runtime C#, plus editor tools and tests)
| Assembly | Folder | Files | Lines | What it holds |
|---|---|---|---|---|
| LOT.Core | `Scripts/Core` | 9 | 923 | Events, options and settings, input gate, audio manager, on-screen text requests |
| LOT.Characters | `Scripts/Characters` | 11 | 1,556 | Player controller, orbit camera, grounding, NPC animation driver, face animator (lip sync and blinking) |
| LOT.World | `Scripts/World` | 9 | 1,893 | Village and road generation, weather, art helpers, light flicker |
| LOT.Gameplay | `Scripts/Gameplay` (+ `Puzzles`, `Vehicles`) | 59 | 9,451 | Story and objectives, Dawn Clock, the six zones, puzzle framework, fight system, scoring, checkpoints, evidence board, jeep |
| LOT.UI | `Scripts/UI` | 24 | 6,690 | HUD, menus, loading screen, guide, audio director, test bot |

## 3. Main systems
- **Story.** `Resources/Story/story.json` holds the 9 objectives, 29 spoken lines (barks and two short conversations), chapters, endings and hints. `StoryDialogueSystem` advances objectives when their completion fires (a talk, a "minigame" id, a scene). Each V3 zone reports its id when its treasure is taken, so the guide, map, clock, save and ending keep working unchanged.
- **Zones.** One C# builder per zone (`BazaarTail`, `BreakInPuzzle`, `VaultPuzzle`, `StepwellPuzzle`, `SunWallPuzzle`, `JungleChase`) builds its place at runtime in the Courtyard scene around the temple, after checking for clear ground. `ZoneDress` adds the Blender-made props and mouldings.
- **Puzzle framework** (`Gameplay/Puzzles`). `PuzzleElement` is a part with one integer state, saved by id; `PuzzleGroup` holds the solution conditions, three hints, locking and the score report. Parts: rotating disc, mirror, lever, pressure plate, pushable block, valve, water basin, sequence lock, carryable item, placement socket, proximity match, light socket, light beam emitter. Pure rules sit in `PuzzleLogic` so they can be tested without a scene.
- **Fights** (`Brawl.cs`). A fight owns its enemies and an attack-token list (at most two attackers). Enemies are a small state machine: approach, circle, telegraph, strike, recover, staggered, down. The player has a light three-hit combo, heavy strike, dodge, block with a parry window, and a finisher with slow motion and camera punch-in. Hit-stop, limited camera shake, sparks and dust. Defeat restarts the fight on the spot.
- **Scoring** (`ScoreSystem`), **checkpoints** (`Checkpoints`), **evidence board** (`EvidenceBoard`): small static stores saved in PlayerPrefs.
- **Look per zone** (`ZoneLook`, `ZoneAtmosphere`). Each zone has a local URP Volume (ACES tonemapping, exposure, white balance, contrast, saturation, bloom, vignette, grain). While the camera is inside a zone, the sun and fog blend to that zone's time of day.
- **Graphics presets** (`GraphicsPresets`). Low, Medium, High (default), Ultra: render scale, shadow distance and cascades, MSAA, LOD bias, effect strength; 60 fps cap with vsync.
- **Faces** (`FaceAnimator`). Reads the playing voice clip's loudness and spectrum; drives viseme blendshapes (Meera) or the jaw bone (Aryan), blinks with eyelid shapes or bones.
- **Jeep.** Own Blender model (`Tools/blender/build_jeep_v2.py`), WheelCollider controller ported from an MIT project, lights, horn, seated driver, damage by impact strength and place with detachable parts and three damage meshes.
- **Audio.** Mixer with music, ambience, effects and voice groups and ducking; adaptive music stems; footsteps by surface; voice lines made with the Mac's built-in Indian English voices (`Tools/audio/build_v3_voices.py`); the courier's bell and the jeep horn are made in code.

## 4. Content pipeline
| Tool | Makes |
|---|---|
| `Tools/blender/build_jeep_v2.py` | Jeep, LODs, damage meshes, collision, renders |
| `Tools/blender/build_breakin_pot.py` | The break-in pot and its shards |
| `Tools/blender/build_v3_props.py` | Carved pillar, coping, plinth, lamp stand, arch |
| `Tools/blender/make_jeep_textures.py` | Number plate, emblem, paint dirt |
| `Tools/audio/build_v3_voices.py` | The 29 V3 voice lines |
| `Tools/dev/cc.sh`, `pt.sh`, `v3bot.sh` | Compile and build, play-mode tests, the hidden whole-game test bot |

## 5. Testing
See `Docs/TEST_REPORT.md`. In short: play-mode tests for the puzzle framework, scoring, checkpoints, evidence board, graphics presets, zone looks, faces and jeep physics; and a bot that plays the whole built game from New Game to the ending with real key presses and writes a report with screenshots.

## 6. Performance
Measured in the built game on this Mac by the test bot (4 seconds standing at each place, High preset, 1600 x 900 window). See the table that follows.

| Place | Before: average (fps) | Before: CPU render | After: average (fps) | After: 99 % frame | After: CPU main / render / GPU |
|---|---|---|---|---|---|
| storeroom | 25.0 ms (40) | 22.0 ms | 8.5 ms (117) | 11.2 ms | 3.6 / 2.3 / 12.8 ms |
| vaults | 18.6 ms (54) | 16.7 ms | 3.6 ms (275) | 6.5 ms | 2.8 / 1.8 / 6.7 ms |
| stepwell | 20.0 ms (50) | 17.3 ms | 5.6 ms (177) | 8.4 ms | 3.1 / 1.8 / 9.4 ms |
| sunwall | 12.7 ms (79) | 10.5 ms | 5.0 ms (198) | 8.1 ms | 2.2 / 1.1 / 9.1 ms |
| river road | 22.3 ms (45) | 19.3 ms | 5.4 ms (185) | 7.7 ms | 2.9 / 2.0 / 10.6 ms |
| ruin court | 4.4 ms (229) | 0.7 ms | 3.2 ms (312) | 5.7 ms | 1.5 / 0.6 / 7.1 ms |
| bazaar | 40.6 ms (25) | 37.7 ms | 7.8 ms (129) | 10.6 ms | 4.6 / 4.5 / 11.1 ms |
| temple front | 39.5 ms (25) | 36.1 ms | 6.5 ms (154) | 10.0 ms | 4.4 / 3.8 / 9.9 ms |

Low preset after the fix: storeroom 4.1 ms, vaults 2.8 ms, stepwell 4.1 ms, sunwall 3.4 ms, river road 3.3 ms, ruin court 1.8 ms, bazaar 6.5 ms, temple front 4.0 ms.

**What was wrong and what fixed it.** The CPU render thread was the bottleneck (17 to 40 ms a frame); the GPU was not (the Low preset was barely faster, and the ruin court, far from the village, ran at 229 fps). The world builders tinted and tiled thousands of static blocks with per-object property blocks, which take an object out of URP's SRP Batcher. `World/BatchFriendly.cs` now bakes each static object's colour and tiling into a shared, cached material after the scene is built (3,806 objects into 1,988 materials) and clears the block. Moving and glowing things are left alone. Two smaller changes went in at the same time and measured almost nothing on their own: per-area levels of detail for the temple (`Tools/blender/temple_lods.py`) and not drawing villagers beyond 55 m (`Gameplay/CrowdCulling.cs`).

Method: `Tools/dev/v3bot.sh -v3Perf <folder>`, 4 seconds standing at each place, vsync off while measuring, CPU and GPU times from FrameTimingManager. Raw results: `Docs/QA/v3/perf_before/audit.md` and `Docs/QA/v3/perf_run/audit.md`. In play the frame rate is capped at 60 with vsync.


## 7. Known technical limits
- The temple model is 1.87 million triangles (LOD1 1.38 million), the biggest single cost.
- Zones are built at runtime; there is no baked lighting or occlusion data for them.
- The development build has no shader stripping tuning; three URP post shaders (depth of field, Panini) are stripped and show a warning in the log.
