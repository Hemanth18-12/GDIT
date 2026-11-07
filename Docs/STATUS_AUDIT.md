# STATUS AUDIT — Legacy of the Temple (Phase 0, 2026-11-07)

Method: backup tag `backup-audit-2` created. A runtime audit bot (`Assets/_Project/Scripts/UI/AuditBot.cs`, only active with the command-line flag `-auditBot <dir>`) was added and run **inside the built app** (`Builds/LegacyOfTheTemple.app`, development build, 1920x1080 window). It injects real key presses through the Input System, samples every Animator each frame, and measures every visible TextMeshPro element. Full raw output: `Docs/QA/audit-before/audit.md`, log `Docs/QA/audit-before/player.log`, screenshots `Docs/QA/audit-before/*.png`. Build log: `Docs/QA/last-build.txt` (Succeeded, 0 errors, 0 warnings).

Only two changes were needed to run the audit: the audit bot plus a plain build script (`Scripts/World/Editor/AuditBuildTools.cs`), and **one pre-existing compile error** in your uncommitted `WorldManager.cs:96` (`loadedScene.isValid` → `loadedScene.IsValid()`). Before that fix the project **did not compile at all**, so the uncommitted work had never been run.

## 1. What changed (history)
| Session (PROMPT_LOG / git) | Claimed | Current reality |
|---|---|---|
| P0–P1 (web prototype, then Unity base + 5 procedural scenes) | 5 environments | Scenes exist and load. They are procedural block geometry; JunglePath is mostly untextured grey (`audit-before/JunglePath-overview.png`). |
| P2–P3 (characters) | 5 characters, Heritage models | The models are yours. Old wrong names remain in code: `NPCDialogueTrigger.cs:14` default "Priest Madhav"; Robber names "Chhaya/Vikram" in PROMPT_LOG. |
| P4–P6 (input, legs, faces, PBR) | "natural gait" | See §3: the hero's walk/run drifts 3.2 m off the capsule. |
| P7 commit 993bd32 | "25-point checklist 100% passed" | Not supported by evidence; several items are broken (below). |
| P9 commit 234ef67 / QA_REPORT.md | "ALL 9 COMPLAINTS RESOLVED", "no text clipping" | False: the UI audit finds overlaps/overflow in every HUD (§6). |
| P10 commit 97315c8 + uncommitted work | grounding, showcase | The uncommitted work had a compile error (fixed above). |

## 2. Assets collected
- **Mixamo staging** is `~/Desktop/GDIT/Mixamo_Staging/` (there is no `~/Downloads/Mixamo_Staging`). 12 files: Crouch, Crouch Idle, Fight Idle To Standing Idle, Idle, Jump, Jumping Up, Running, Walking, Walking Hit Reaction, Walking Left Turn, X Bot@Idle, X Bot@Jumping Up.
- **Imported into the project**: `Animations/Clips/Hero/{Idle,Walking,Running,Crouch,Jump}.fbx`. All are Humanoid (`animationType: 3`), avatar valid at runtime (`InvestigatorAvatar isHuman=True avatarValid=True`), and used by `CharacterAnimator.controller`. All take name `mixamo.com`.
- **Not imported**: Crouch Idle, Jumping Up, Walking Left Turn, Fight Idle To Standing Idle, Walking Hit Reaction.
- **Old `.anim` files** `Clips/Investigator_*.anim` (6) are generated clips, not used by the controller → leftovers.
- **NPC rigs**: `Priest.fbx`, `Robber1.fbx`, `Robber2.fbx` are **Generic** (`animationType: 2`); their clips (`Priest_Idle/Talk/Point`, `Robber1_Idle/Walk/Threaten`, `Robber2_Idle/Stealth/Cornered`) are embedded Blender actions made in the earlier rigging script (`rig_characters.py`) and are **not Mixamo motion capture**. `Villager.fbx` (Meera) is Humanoid, but `VillagerAnimator` plays the **Priest's Generic talk/point clips** on her.
- **Clips still missing** (exact Mixamo search names, for a Humanoid "Without Skin" download, 30 fps, "In Place" ticked where offered):
  Hero: `Walking` (In Place), `Running` (In Place), `Start Walking`, `Walking To Stop` (or `Stop Walking`), `Left Turn 90`, `Right Turn 90`, `Left Strafe Walking`, `Right Strafe Walking`, `Walking Backwards`, `Falling Idle`, `Falling To Landing`, `Crouched Walking`, `Picking Up`, `Reading`, `Looking Around`, `Pointing`, `Talking`.
  Pujari/Meera: `Idle` (second variant e.g. `Standing Idle`), `Talking`, `Pointing`, `Praying`.
  Bhaskar: `Walking` (heavy/`Drunk Walk` style different gait), `Talking`, `Arguing`, `Surrender` (`Kneeling Idle` / `Hands Up`).
  Kishore: `Sneak Walk`, `Talking`, `Nervous Look Around`, `Surrender`.
- **Models**: `Models/Characters/{Investigator,Priest,Villager,Robber1,Robber2}.fbx`, `Models/Environment/*` incl. `Idol_Jagannath.fbx` (background only, not interactive in code).
- **Audio**: 28 files in `Assets/_Project/Audio` (ambience, bgm, sfx), listed in `Audio/ATTRIBUTION.md`.
- **Screenshots**: `Docs/QA/*.png` (old, pre-today), `Docs/QA/2026-11-06/*` (previous session), `Docs/QA/audit-before/*` (this audit).

## 3. Animation test (measured in the built app)
Metrics per phase: playing clip, Speed/Grounded/Crouch params, body speed, foot speed at the 10th percentile (planted foot ≈ 0), hips horizontal distance from the character root, lowest foot height above ground.

| Character | idle | walk | run | crouch | jump | talk |
|---|---|---|---|---|---|---|
| Aryan (player) | **works** (Speed 0, feet 0.10 m — slightly high) | **broken**: Speed=4.5 Grounded=T, but hips drift **3.19 m** from the capsule (root motion not baked → mesh walks away from the collider), feet never planted (1.26/0.93 m/s) | **broken**, same drift 3.24 m | **partial**: crouch clip plays, feet float 0.15 m | **broken**: Space press → no jump clip, Grounded stays T, clip unchanged | **missing** (no talk clip) |
| Pujari Ananda | Generic Blender clip plays; feet 0.15 m above ground in Courtyard, **−0.21 m (sinking)** in Sanctum | none | none | none | none | Generic Blender clip plays |
| Meera | **floating** 0.16 m | none | none | none | none | **broken**: plays Priest's Generic clip on a Humanoid → feet −0.66 m (sinks) |
| Bhaskar | Generic Blender idle OK | Generic "walk" clip plays in place; robbers never move | – | – | – | **missing** (no IsTalking) |
| Kishore | Generic Blender idle OK | "Stealth" clip in place | – | – | – | **missing** |

Also: no foot IK, no look-at, no blink; footsteps are a timer (`PlayerController.cs` footstepTimer), not foot events.
Duplicate priests: exactly **one** Priest animator per scene was found (Courtyard, Sanctum, ShrineCrime, PuzzleWall). No duplicate at runtime.

## 4. Function test (built app, injected keys, Courtyard)
| Function | Result |
|---|---|
| Journal J | works (IMGUI, tiny fonts) |
| Map M | works (IMGUI) |
| Help H | works, but H is also bound in `UIManager.cs:218`, `GameIntroOverlay.cs:68` and `TutorialManager` → conflicting handlers |
| Photo mode P | works |
| Pause Esc | works (timeScale 1→0→1) |
| Interaction E (talk) | works for Meera and Pujari |
| Dialogue choices (1/2) | **broken**: pressing **1** to pick a choice also triggers the dev hotkey in `WorldDevTools.cs:74` and **switches scene** |
| Clue inspect F | **not testable in Courtyard: 0 clues there**; clues exist only in later scenes |
| Tutorial | partial: shows as a modal brief; skipped by bot |
| Puzzle wall | partial: dialogue auto-starts; solve not exercised |
| Scene changes | **broken**: after loading Sanctum and ShrineCrime the camera still shows the Courtyard (`audit-before/Sanctum-overview.png`, `ShrineCrime-overview.png`); the objective stays "Speak with Head Priest Ananda" in every scene |
| Start view | **broken**: in the Courtyard the gameplay camera faces a dark curtain and the player is not visible (`audit-before/Courtyard-player-walk.png`) |
| Save/Load | **missing** (only settings and tutorial flag in PlayerPrefs) |
| Showcase mode | exists (`ShowcaseDirector.cs`); not exercised |
| Guide button | **missing** |
| Gamepad Start | bound to both Map (`TempleMapOverlay.cs:45`) and Pause (`InputManager.cs:181`) |

## 5. Script status
- Dialogue is **hard-coded** in `Scripts/Gameplay/StoryDialogueSystem.cs` (not data-driven), no portraits (just a letter), no voice hooks.
- All 5 beats exist roughly as written; Courtyard has the 2 choices.
- Problems: the Pujari's opening line block contains **Meera's line** under the Pujari's conversation (speaker label in text only); Meera's line is repeated twice (Pujari block and her own). Sanctum/ShrineCrime/Jungle speaker headers are scene titles ("Crime Scene Analysis"), not people. Lines exceed 140 chars (the opening line is 166). The Jungle scene has no surrender/return beat; the ending jumps straight to `CompleteLevel3`. Objectives have no ids, markers or hint tiers.
- Wrong story / names: `GameIntroOverlay.cs:111` and `:278` ("revering the sacred sanctum deity", "the sacred deity altar"), `TempleMapOverlay.cs:103` "(Deity Shrine)", `NPCDialogueTrigger.cs:106` fallback "sacred deities…", default name "Priest Madhav"; relic id `relic_pradip` (a lamp) instead of the brass vessel; event named `OnIdolRecovered`; menu text "sacred Jagannath Temple". No real deity is stolen in dialogue.

## 6. UI status (automated, built app)
Totals across all views: 1280x720 / 1920x1080 / 2560x1440 give identical results (the Canvas Scaler works). **But 12–15 of the screens are drawn with IMGUI `OnGUI`** (main menu, journal, map, hints, puzzle, intro, NPC labels, dev tools), which cannot be measured and uses 11–24 px fonts in code (`GameIntroOverlay.cs:153-226`).

TextMeshPro issues found (px at 1080p / minimum / worst-case contrast):
| Element | px | min | contrast | issues |
|---|---|---|---|---|
| ObjectiveTitle | 42 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW |
| ObjectiveBody | 36 | 34 | 7.8 | OVERFLOW, **OVERLAPS ObjectiveTitle and ObjectiveCounter** (visible in screenshot) |
| ObjectiveCounter | 29 | 34 | 5.4 | SMALL, LOW-CONTRAST |
| HintText (key bar) | 25 | 32 | 7.8 | SMALL |
| DialoguePrompt | 34 | 36 | 5.4 | SMALL, LOW-CONTRAST, OVERFLOW |
| SpeakerName | 47 | 36 | 5.5 | LOW-CONTRAST, OVERFLOW; DialogueBody overlaps it |
| CardTitle (How to play) | 49 | 64 | 5.5 | SMALL, LOW-CONTRAST, OVERFLOW |
| ContinuePrompt | 36 | 34 | 5.4 | LOW-CONTRAST, OVERFLOW |
| PauseTitle | 83 | 64 | 5.5 | LOW-CONTRAST |
| ToastText | 26 | 30 | 9.8 | SMALL |
Problems per view: Courtyard/Sanctum/ShrineCrime/Jungle HUD 4, PuzzleWall 6, Dialogue 6, Pause 7. Fonts: only the TMP default (LiberationSans); no heritage fonts.

## 7. Duplicates and placeholders
- No duplicate priest at runtime (one Priest animator per scene).
- Leftovers: 6 unused generated `.anim` clips; `Docs/QA/*.png` from older sessions with claims not matching reality; the dev-tools overlay (scene hotkeys 1–5) is active in Development builds and collides with dialogue choices.
- The Courtyard "Priest Ananda / Meera" name-plates are tiny IMGUI labels.

## Top priorities derived from this audit
1. Hero locomotion drift/jump (blocked partly on missing Mixamo clips, see list).
2. Start camera / scene-change camera / objective not updating.
3. Dev hotkeys colliding with choices.
4. UI: overlaps, sizes, contrast; IMGUI screens → TMP.
5. Data-driven script + Guide button.
