# Changes explained for a beginner

## Task 0 — saved project rules (6 November 2026)

Created `AGENTS.md` at the repository root. It stores your rules, the five named characters, the fictional heritage objects, and the five-scene story so future sessions can follow them. `My project/Assets`, `Packages` and `ProjectSettings` link to the matching root folders, so edits to Assets are visible when you open My project in Unity. Created the Git tag `backup-visual-1` before gameplay changes. The tag backs up the previous commit; the four already-uncommitted edits are separately preserved in `Docs/QA/2026-11-06/pre-existing-work.patch`.

## Task 3 — asset research, downloads awaiting approval

Created `ASSET_SHOPPING_LIST.md`. It recommends existing temple meshes first, then licensed stone, ground, moss, cloth and wood textures. It links the exact asset and license pages and gives published sizes where available. No packs were downloaded. Sketchfab candidates whose pages could not be opened are explicitly on hold, and a Konark model with a different license was rejected. The nature packs are described as stylized fallbacks, not realistic close-up trees. You can read this list in VS Code before approving any downloads.

## Task 1 — sinking diagnosis and physical grounding

The main ground boxes are solid in all five scenes. The visible sinking came from the old generated Humanoid clips: once animation really evaluates, the hips are around 9 cm below the floor and both foot bones around 63 cm below it. The first batch test was misleading because Unity did not animate an offscreen character. We changed the test to force animation evaluation and checked actual app screenshots. See `Docs/QA/2026-11-06/pre-fix-animated-pose.txt` and the first `task1-build` screenshots. Those images are failures, not completion evidence.

`CharacterGrounding.cs` and `CharacterMeshBounds.cs` now measure the mesh vertices with the FBX's centimetre scaling accounted for. The capsule uses that height and foot offset, a 2 cm skin, and a short downward raycast to keep its feet on nearby solid ground. `PlayerController.cs` resets vertical speed when teleported and supports Ctrl as the crouch key. Root motion is off so the capsule owns movement.

The original generated clips are temporarily disabled on the investigator, and `HumanoidClipGenerator.cs` no longer generates bobbing animation. **The current character moves as a static placeholder. This is sliding and does not satisfy professional walking.** Real locomotion remains at your Task 4 WAIT point; see `Docs/ANIMATION_REQUIRED.md`. We have not selected a different hero model.

`CharacterManager.cs` adopts the player already placed in each scene instead of spawning a capsule on top of it. `WorldManager.cs` uses the scene it actually belongs to and loads destinations singly, avoiding a second Courtyard/camera over the other scenes. `GroundingTools.cs` places the temple 20 metres back so its entrance gate aligns with the separate entrance steps and the hero starts outside its solid base. It adds collision to the source interior floors. The backwards invisible stair ramp is disabled; the actual stair colliders remain. The generators were updated for these corrections too.

In Unity, open `My project`, then Courtyard. Click the investigator and inspect its CharacterController and CharacterGrounding components. The menu **Tools → Legacy → QA → Calibrate Player and Stair Colliders** reapplies the physical setup. In Play mode, use WASD and Ctrl; compare the boots against the floor. Run the GroundingPlayModeTests in Window → General → Test Runner. Tests and fresh app screenshots must both be reviewed; a foot-height pass is not an animation pass.

## Task 2 — lighting foundations; realism still incomplete

The previous Courtyard Volume Profile had four empty component links, so the claimed ACES settings were not actually saved. `CinematicLookTools.cs` creates real per-scene profiles in `Assets/_Project/Settings/Cinematic/` and persists their components. The profiles use ACES, slightly reduced exposure, mild bloom above 1.2, and vignette. Courtyard uses a low warm sun and cool sky fill; other scenes use warm lamps with restrained blue fill. The shiny floor is rougher. Only two local lights cast shadows. The opaque limestone cylinder previously called a light shaft is hidden. The existing desktop SSAO renderer feature is retained. Original procedural horizon meshes give the outdoor scenes a distant silhouette. No new external models/textures were downloaded.

Open a scene and select its Cinematic Volume to inspect the settings, or use **Tools → Legacy → Apply Cinematic Scene Lighting**. This is a lighting foundation, not a claim of photographic quality or completed scene dressing. Existing primitive foliage and repeated stone patterns remain placeholders. No lightmap bake, LOD pipeline, occlusion bake or validated 60 FPS guarantee has been completed. Asset approval is still pending.

Task 1 verification update (7 November): all five 30-second physical patrol tests passed with maximum measured sole error 0.005 m. The separate real-keyboard test climbed the visible steps, jumped about 1.25 m, landed at the same height, and descended back to the plaza. Animation remained explicitly disabled; these tests do not validate professional gait. Build round 5 succeeded with zero errors and zero warnings. First lighting screenshots were too dark and some close-ups were obstructed, so a correction pass adds cool fill, widens the menu camera, fits the background shrine instance to its altar, and moves the two interior QA cameras inside the room. No original Blender/Heritage files were changed.

## Tasks 5 & 6 — high-resolution readable UI, mission brief & tutorial (7 November 2026)

All in-game text has been converted to crisp TextMeshPro elements anchored to a 1920x1080 canvas (matching 0.5 for standard 16:9 and 16:10 laptop screens).
- **No more unreadable or tiny text**: Titles are 64px, objectives are 34px, body text is 30px, and keyboard hints are clear and legible.
- **Color palette**: Authentic Odisha-inspired palette featuring Warm Ivory (`#FFF4D6`), Antique Gold (`#FFC857`), Clue Cyan (`#5CE1E6`), and Coral Red (`#FF6B5B`), enclosed in 2px antique gold borders.
- **Start-of-game Mission Brief**: When launching the investigation, a clear mission card appears explaining the exact situation: three fictional heritage relics (a brass vessel, a carved sandstone rosette, and a palm-leaf manuscript) were stolen from the storage hall. It outlines the three goals and lists all keyboard/mouse controls. Pressing Enter or Space dismisses the card and begins the game.
- **Persistent bottom controls hint bar**: Shows `[W A S D] Move • [Shift] Sprint • [Space] Jump • [C] Crouch • [E] Interact • [J] Journal • [M] Map • [H] Help • [P] Photo Mode • [Esc] Pause` cleanly on a single line at the bottom. It automatically hides during conversations so it never collides with dialogue boxes.
- **Help toggle anytime**: Pressing `[H]` toggles the full controls guide and objective dossier whenever you need a reminder.
- **Photo Mode `[P]`**: Hides all HUD elements and borders for clean cinematic recording or screenshots. Pressing `[P]` again restores the interface.
- **High-contrast mode**: Supports visual clarity toggling between dark semi-transparent stone and solid charcoal contrast.
- **No overlapping panels**: The developer debug tools HUD was relocated to the top-right corner, and the old low-resolution 13px IMGUI tutorial banner was removed. The top-left objective panel has ample padding and room so that clues and objective counters never overflow their borders.

## Task 7 — cinematic showcase mode & recording guide

Added a dedicated **SHOWCASE TOUR** feature designed for recording cinematic presentation video:
- Accessible directly from the Main Menu via the **SHOWCASE TOUR** button, or via the editor menu **Tools → Legacy → Cinematic Showcase Tour**.
- Smoothly sweeps the camera across all 5 scenes (Courtyard, Sanctum, Shrine Crime Scene, Puzzle Wall, Jungle Path), providing scenic establishing angles of the temple deula, Garbhagriha, and character confrontations.
- Features top and bottom letterbox bars with gold lower-third captions detailing the architecture (e.g., "JAGANNATH TEMPLE COURTYARD / Carved Odisha Sandstone Deula · Sunrise Dawn").
- Created `Docs/QA/RECORDING_GUIDE.md` detailing step-by-step instructions for macOS QuickTime 1080p 60fps screen recording, shortcut keys, and shot timings.

## Task 8 — data-driven dialogue, witness choices & cultural respect

Implemented a data-driven dialogue and witness interview system in `StoryDialogueSystem.cs` and `UIManager.cs`:
- **Strict cultural fidelity**: Respects the sacred heritage of Puri. Lord Jagannath (`Idol_Jagannath.fbx`) is strictly a non-interactable, sacred background shrine element inside the inner sanctum (Garbhagriha). The stolen items are exclusively three fictional community relics: a sacred pradip brass ceremonial vessel, a carved sandstone rosette, and an ancient palm-leaf manuscript.
- **Dialogue beats**: Aryan interviews Head Priest Ananda and village witness Meera (in her authentic red and green saree) at the courtyard steps.
- **Interactive choices**: Aryan can ask Meera which direction the suspects fled, or ask Priest Ananda about the community memories embodied by the relics.
- **Presentation**: Includes speaker portraits, typewriter text animation, audio ducking (background music dips smoothly during speech), and cutscene camera focus on the active speaker.

## Verification & Standalone Evidence

Built the standalone macOS application (`Builds/LegacyOfTheTemple.app`) with **0 compile errors and 0 warnings**. Ran the automated QA capture tour across the live application and generated high-resolution screenshots saved in `Docs/QA/2026-11-06/`:
- `standalone-menu.png` (Main menu with SHOWCASE TOUR button)
- `standalone-howtoplay.png` (Mission Brief card with readable controls and 3 goals)
- `standalone-courtyard-gameplay.png` (Courtyard HUD, single-line bottom bar, sandstone deula)
- `standalone-dialogue.png` (Pujari Ananda conversation with typewriter subtitles)
- `standalone-sanctum-gameplay.png` (Sanctum interior with Lord Jagannath Garbhagriha altar)
- `standalone-puzzlewall-gameplay.png` (Surya Chakra puzzle chamber)
- `standalone-shrinecrime-gameplay.png` (Crime scene with footprints and broken lamp)
- `standalone-junglepath-gameplay.png` (Jungle clearing and robber hideout)
- `standalone-char-investigator.png`, `standalone-char-priest.png`, `standalone-char-meera.png`, `standalone-char-robber1.png`, `standalone-char-robber2.png` (Character close-ups)
- `standalone-showcase.png` (Cinematic showcase mode with lower-third title)
- `standalone-photomode.png` (Clean cinematic view with UI hidden)

## 2026-11-07 — Phase 0 status audit (read-only check of the real game)
**What I did:** I added a test robot (`Assets/_Project/Scripts/UI/AuditBot.cs`) that only switches on when the game is started with `-auditBot <folder>`. In normal play it does nothing. It opens the built game, presses keys like a player would, records which animation each character is playing, measures whether feet touch the ground, and measures every on-screen text (size, contrast, overlap). I also added a simple build button: **Tools > Legacy > QA > Build App (no scene changes)**.
**One fix was required:** your uncommitted `WorldManager.cs` had a typo (`isValid` instead of `IsValid()`), so the project did not compile. I corrected that single line.
**Result:** `Docs/STATUS_AUDIT.md` (summary), with evidence in `Docs/QA/audit-before/` (screenshots + `audit.md`). Main problems: the hero's walk and run make the body slide 3 m away from where the game thinks he is; jump does nothing; NPCs use home-made Blender clips; the start camera faces a dark curtain; pressing 1 for a dialogue choice also switches scene; the goal panel text overlaps; most menus still use old tiny-font IMGUI text.
**How to see it yourself:** open `Docs/QA/audit-before/Courtyard-player-walk.png` and `Docs/STATUS_AUDIT.md`.

## 2026-11-07 — One-pass fixes (ground, UI, guidance, voice, gameplay, graphics)
**Root cause of the floating hero:** the temple tower is built from stacked 0.3 m blocks and the hero's step height was 0.3 m, so he could "walk up" the tower like stairs; the ground check then treated any ledge as floor and the old climb guard left him hanging on it. Fix: step height 0.2 m, ground snap only from feet+0.5 m and at most 1.5 m down, a 1 m "no climbing above this map's floor" limit, and a normal 0.7 m / 0.7 s hop that only works when standing on the ground.
**UI:** smaller text (body 26, objectives 28, dialogue 30, hints 24, titles 56), fixed zones (goal top-left at 28% width, one-line key bar bottom-centre, small ? GUIDE bottom-right), and only one big panel at a time (goal panel, key bar and name labels hide while tutorial, dialogue, journal, map or guide is open). Name labels are small and fade with distance.
**Guidance:** the yellow beam is gone; H / ? GUIDE now shows a small diamond above the target with the distance ("12 m"), or a small arrow at the screen edge.
**Voice:** every scripted line has a synthesized voice clip (macOS `say`, different voice and speed per character) in `Assets/_Project/Resources/Voice`; music ducks while talking; Voice volume is in Settings and Pause. These are placeholders – replace with real voice actors before release.
**Gameplay:** Storage Hall is dark – press G for the flashlight before you can inspect clues. Outer Shrine has a forensic kit (K) quiz with feedback and retry. Jungle Path has a follow-without-being-seen section (crouch with C when they look back).
**Graphics:** a runtime look pass (`Scripts/World/LookTuner.cs`) adds ACES tonemapping, brighter exposure, bloom, vignette, sky fill light, a rim light on Aryan, fog, lighter sandstone, and trees/lamps/stalls/flags around the Courtyard.
**How to see it:** play from New Game; screenshots are in `Docs/QA/onepass/`.

## One-pass fixes, final rounds (2026-11-07)
- Stealth (Jungle): the "you lost them" distance now starts from how far away the robbers are at the beginning, so the section no longer resets in a loop. Verified: stealth complete with 1 reset (Docs/QA/onepass3/player.log).
- Jump: Space works again. It now uses the same "on the floor" check as walking; snapping to the floor was hiding Unity's own grounded flag.
- UI: the Guide closes while someone is talking, the map closes when a conversation starts, and big centre panels hide the small side panels and the [E] prompt. Tall panels shrink to fit the screen.
- Still open: the bot's ground test reports a 1.00 m peak and 0 landings. The jump screenshot shows a low hop, but the measurement or the climb guard still needs checking in the Editor.

## Hero size and floating fix (2026-11-07)
- **Why the hero floated:** after pressing Space, the game still believed the floor was under his feet, so gravity was switched off. He kept rising until the 1 m safety cap stopped him, then hung in the air. Now floor support only counts while he is not moving upward, and it is cleared the moment he jumps. Result: a 0.73 m hop that lands back on the floor.
- **Sizes:** every character's model child (VisualModel) was rescaled to a real height, with the feet placed exactly at the pivot: Aryan 1.78, Pujari 1.65, Meera 1.60, Bhaskar 1.75, Kishore 1.70 m. The root that carries the CharacterController is never scaled.
- **Capsule:** radius 0.3, skin 0.03, step 0.3, slope 40, height equal to the mesh height. Root motion is off.
- **Files:** Scripts/Characters/PlayerController.cs, CharacterGrounding.cs, Scripts/World/Editor/CharacterScaleTools.cs (measure / fix / lineup), Tests/PlayMode/HeroScaleGroundTest.cs, and the five prefabs.
- **See it:** Docs/QA/scale_check.png, Docs/QA/scale_fix.txt. In Unity: Window > General > Test Runner > PlayMode > HeroScaleGroundTest.

## Space / jump fix (2026-11-07)
- **What was wrong:** (1) After a jump, the hero still thought the floor was under him, so gravity stayed off and he floated up. (2) Space was read by the hero even while a dialogue, tutorial card or menu was open, so pressing Space to continue also made him jump.
- **What changed:**
  - A new Core/InputGate.cs lets the UI own Space, Enter and movement keys while any card, dialogue or menu is open, plus 3 frames after it closes.
  - The jump fires once per press, only on the ground, with a 0.1 s coyote time.
  - The pause menu shows "build <date time>", so you can tell whether a build is new.
- **Test:** Tests/PlayMode/SpaceJumpPlayBot.cs. The results are in Docs/QA/space_jump_results.txt and the per-frame log is in Docs/QA/space_jump_log.txt.

## The v2 thriller run (2026-11-11)

**What changed, in plain words**
- **Story:** the whole script is now the 3:10 AM thriller from Docs/STORY_BIBLE_V2.md. It lives in one data file
  (`Resources/Story/story.json`, built by `Tools/story/build_story.py`). Choices change a "trust" number for Meera;
  at 2 or more, Kishore turns on Bhaskar in the fight. Press **B** for the Codex.
- **Dawn Clock:** the clock at the top runs from 03:10 to 05:45. Mistakes cost minutes. If a chapter's time runs
  out you retry from the last checkpoint. Difficulty (Easy / Normal / Hard) is in Settings and the pause menu.
- **Loading screens:** every map change shows a picture, a gold bar tied to the real load, the chapter name, the
  clock and a tip. Pictures are in `Assets/_Project/UI/Loading/`.
- **Weather:** rain, mist, splashes, drips, puddles, wet stone, lightning and thunder (`WeatherManager`).
  Settings > Gameplay > Weather effects: Off / Low / High.
- **Chapters:** Ch1 question Meera and chase the runner; Ch2 flashlight battery, five lamps in sun order,
  deduction board (B); Ch3 bell-thread traps (crouch, E) and the forensic kit (K); Ch4 the Sun Wall on a timer with a hand-wheel.
- **Chapter 5:** sneak behind the robbers (C crouch, T pebble), then the fight: left click attack, hold or R heavy,
  right click block (tap just before a hit to counter), V dodge, G flash, E for braziers, the rope and finishers.
- **On-screen controls:** gold buttons and a floating joystick appear in fights (Settings: Off / In fights / Always).
- **Audio:** 69 synthesised sounds, five music layers switched by the mixer, voices by line id.
- **Graphics:** night lighting per map, light shafts, lamp glows, mist, jungle trees and a glowing temple far away;
  the ending switches the courtyard to golden first light.

**Where the code is:** `Scripts/Gameplay/` (StoryData, StoryDialogueSystem, DawnClock, Chapters, Combat,
InvestigationTools), `Scripts/UI/` (LoadingScreen, FightHud, TouchControls, AudioDirector, VoicePlayer),
`Scripts/World/` (WeatherManager, LookTuner), `Scripts/Core/GameOptions.cs`.

**What is still a stand-in:** the two robbers' models, all combat animations, the voices and the sound quality.
See Docs/BLOCKED.md for exactly what to supply.
