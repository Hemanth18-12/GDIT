# Prompt Log - Legacy of Temples

## P0 - Web Scaffold & Core Contracts (Archived)
- **Date**: 2026-11-04
- **Summary**: Initial prototype web scaffold in Vite + Three.js.

## P1-v1 - World Environments (Archived Web Prototype)
- **Date**: 2026-11-04
- **Summary**: Implemented 5 complete Odisha-style 3D temple environments for Three.js web prototype.

## P0-Unity - Shared Unity 6 URP Base & Core Contract Setup
- **Date**: 2026-11-04
- **Engine**: Unity 6 (`6000.6.8f1`) URP
- **Summary**: Established the base Unity 6 Universal Render Pipeline (URP) project structure for a 3-person team on GitHub. Implemented team folder partitioning under `Assets/_Project/`, isolated Assembly Definitions (`LOT.Core`, `LOT.World`, `LOT.Characters`, `LOT.Gameplay`, `LOT.UI`), and defined the stable Core contract (`GameState`, `IInteractable`, `EventBus`, `Constants`). Configured Force Text serialization, Visible Meta Files, standard Unity `.gitignore`, `.gitattributes`, and full `.meta` tracking.
- **Files Created / Configured**:
  - `.gitignore`
  - `.gitattributes`
  - `README.md`
  - `PROMPT_LOG.md`
  - `ProjectSettings/` (EditorSettings, VersionControlSettings, Quality, URP settings)
  - `Packages/` (manifest.json, packages-lock.json)
  - `Assets/_Project/` folder hierarchy (`Scripts/Core`, `Scripts/World`, `Scripts/Characters`, `Scripts/Gameplay`, `Scripts/UI`, `Scenes/Environments`, `Scenes/Dev`, `Prefabs`, `Models/Characters`, `Models/Environment`, `Audio`, `Animations`) with `.gitkeep` and `.meta` files.
  - Assembly Definitions:
    - `Assets/_Project/Scripts/Core/LOT.Core.asmdef`
    - `Assets/_Project/Scripts/World/LOT.World.asmdef`
    - `Assets/_Project/Scripts/Characters/LOT.Characters.asmdef`
    - `Assets/_Project/Scripts/Gameplay/LOT.Gameplay.asmdef`
    - `Assets/_Project/Scripts/UI/LOT.UI.asmdef`
  - Core Contract Scripts:
    - `Assets/_Project/Scripts/Core/GameState.cs`
    - `Assets/_Project/Scripts/Core/IInteractable.cs`
    - `Assets/_Project/Scripts/Core/EventBus.cs`
    - `Assets/_Project/Scripts/Core/Constants.cs`
- **Verification**: Verified syntax and compilation integrity across all C# scripts and Assembly Definitions. Ran Unity 6 (`6000.6.8f1`) in batchmode to compile the project, confirming 0 compile errors and a clean exit code 0. Confirmed no gameplay, world, or character logic was introduced in this step.
- **Known Issues**: None.

## P1-Unity - World Architecture & Procedural Environments
- **Date**: 2026-11-05
- **Engine**: Unity 6 (`6000.6.8f1`) URP
- **Summary**: Implemented the complete Phase 1 World system for "Legacy of Temples", featuring `WorldManager` with smooth additive scene streaming, `SpawnPoint` named registry, `InteractableObject` interaction base, `LightFlicker` Perlin noise warmth, and `WorldDevTools` with scene switching keys 1-5, graphics quality toggle (Q key), and HUD. Created procedural environment builders under `Tools/Legacy/...` generating 5 complete Odisha-style temple environments with URP Lit materials, atmospheric fog, box colliders, named spawn points (`player_start`, `robber_spawn`, `priest_spawn`, `villager_spawn`), interactables, and <= 8 real-time lights per scene for steady 60 FPS performance.
- **Environments Generated**:
  1. `Courtyard`: Sunrise lighting, orange-pink fog, 32m tiered Odisha Rekha Deula sikhara temple with bhumis, amalaka, kalasa, and saffron flag, wet reflective stone plaza, 2 Gajasimha guardian lion statues, and 4 brass oil lamps.
  2. `Sanctum`: Dark sacred chamber, 8 carved stone colonnade pillars, hanging brass bells, crimson and saffron ceremonial silk curtains, marigold garlands, light shaft volume, and empty idol pedestal.
  3. `PuzzleWall`: Ancient chamber with central spotlight, 8-spoke rotating chakra wheel (`wheel_center`), 4 relief panels (`panel_lotus`, `panel_conch`, `panel_wheel`, `panel_kalash`), Sanskrit parchment scroll on ledge (`scroll`), and flanking braziers.
  4. `ShrineCrime`: Crime scene with cool moonlight through smashed wooden doors, overturned brass oil lamp (`broken_lamp`), torn red robber silk cloth (`red_cloth`), muddy footprints trail (`footprints`), and scattered petals.
  5. `JunglePath`: Moonlit night jungle, ravine boulder boundaries, reflective stream with stepping stones, 7-tier mossy ruined stone steps, giant banyan trees with drooping vines, torches, and distant glowing temple spire.
- **Files Created / Modified**:
  - `Assets/_Project/Scripts/World/SpawnPoint.cs` (+ meta)
  - `Assets/_Project/Scripts/World/InteractableObject.cs` (+ meta)
  - `Assets/_Project/Scripts/World/LightFlicker.cs` (+ meta)
  - `Assets/_Project/Scripts/World/WorldManager.cs` (+ meta)
  - `Assets/_Project/Scripts/World/WorldDevTools.cs` (+ meta)
  - `Assets/_Project/Scripts/World/Editor/LOT.World.Editor.asmdef` (+ meta)
  - `Assets/_Project/Scripts/World/Editor/EnvironmentBuilder.cs` (+ meta)
  - `Assets/_Project/Models/Environment/Materials/` (14 URP Lit materials + meta files)
  - `Assets/_Project/Scenes/Environments/Courtyard.unity` (+ meta)
  - `Assets/_Project/Scenes/Environments/Sanctum.unity` (+ meta)
  - `Assets/_Project/Scenes/Environments/PuzzleWall.unity` (+ meta)
  - `Assets/_Project/Scenes/Environments/ShrineCrime.unity` (+ meta)
  - `Assets/_Project/Scenes/Environments/JunglePath.unity` (+ meta)
  - `ProjectSettings/EditorBuildSettings.asset`
- **Verification**: Built and verified in Unity 6 batch mode for all 6 steps in sequence: (1) Core world scripts, (2) Courtyard, (3) Sanctum, (4) PuzzleWall, (5) ShrineCrime, (6) JunglePath. Executed `Tools/Legacy/Build All Environments` in batch mode with 0 compile errors and 0 warnings.
- **Known Issues**: None.

## P2-Unity - Characters, Locomotion & NPC Systems
- **Date**: 2026-11-05
- **Engine**: Unity 6 (`6000.6.8f1`) URP
- **Summary**: Implemented the complete Phase 2 Characters system for "Legacy of Temples". Built `CharacterManager` singleton supporting scene-driven and manual character spawning, `CharacterConfig` scriptable objects, `PlayerController` with third-person camera-relative locomotion, sprint, jump, gravity, and interactable scanning HUD, and `OrbitCamera` with smooth damping, zoom, and wall collision avoidance. Generated 5 procedural stylized Indian 3D character prefabs with dedicated URP Lit materials: Investigator Aryan, Priest Madhav, Villager Ramesh, Robber 1 Chhaya, and Robber 2 Vikram. Implemented NPC helpers (`NPCFacePlayer`, `NPCDialogueTrigger` with `IInteractable` support and dialogue UI), created the `CharacterTest` dev scene, and placed the Investigator player directly at `player_start` in the Courtyard scene alongside Priest and Villager NPCs to verify collider collision, interaction, and camera tracking.
- **Characters Built**:
  1. `Investigator Aryan` (Player): Trench coat, dhoti, leather straps, satchel bag for relics, brass loupe, saffron pagri, `PlayerController`, `CharacterController`.
  2. `Priest Madhav` (NPC): Sacred saffron silk dhoti, white silk angavastram, rudraksha mala, sandalwood tilak, brass aarti offering dish, `NPCFacePlayer`, `NPCDialogueTrigger`.
  3. `Villager Ramesh` (NPC): Earthy green kurta, cotton lungi, saffron gamucha towel, expressive witness pose, `NPCFacePlayer`, `NPCDialogueTrigger`.
  4. `Robber 1 Chhaya` (NPC): Midnight stealth tunic, crimson face mask, dagger, stolen jewel pouch, `NPCFacePlayer`, `NPCDialogueTrigger`.
  5. `Robber 2 Vikram` (NPC): Heavy leather vest, red armbands, stolen idol sack, iron crowbar, `NPCFacePlayer`, `NPCDialogueTrigger`.
- **Files Created / Modified**:
  - `Assets/_Project/Scripts/Characters/LOT.Characters.asmdef` (+ references `LOT.World`)
  - `Assets/_Project/Scripts/Characters/CharacterConfig.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/CharacterManager.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/PlayerController.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/OrbitCamera.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/NPCDialogueTrigger.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/NPCFacePlayer.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/Editor/LOT.Characters.Editor.asmdef` (+ meta)
  - `Assets/_Project/Scripts/Characters/Editor/CharacterBuilder.cs` (+ meta)
  - `Assets/_Project/Prefabs/Characters/` (Investigator, Priest, Villager, Robber1, Robber2 prefabs + meta files)
  - `Assets/_Project/Models/Characters/` (5 CharacterConfig asset files + 12 Character materials + meta files)
  - `Assets/_Project/Scenes/Dev/CharacterTest.unity` (+ meta)
  - `Assets/_Project/Scenes/Environments/Courtyard.unity` (Player at `player_start`, Orbit Camera, Priest & Villager NPCs)
  - `Assets/_Project/Scripts/World/Editor/EnvironmentBuilder.cs`
- **Verification**: Built and tested in Unity 6 batch mode for all 4 steps in sequence: (1) CharacterManager + CharacterConfig + Investigator prefab, (2) PlayerController + OrbitCamera + CharacterTest scene, (3) 4 other characters, (4) NPC helpers and Courtyard player placement. Confirmed zero compile errors and zero warnings.
- **Known Issues**: None.

## P3-Heritage - Real 3D Characters Asset Swap & Placeholder Cleanup
- **Date**: 2026-11-05
- **Engine**: Unity 6 (`6000.6.8f1`) URP
- **Summary**: Replaced all procedural primitive placeholder characters with high-fidelity authentic 3D models extracted and rigged from the user's `Heritage Mystery Project` (`assets.blend` and source asset libraries). Automated the export pipeline in Blender 5.2.2 batch mode with mesh decimation for optimal real-time performance, standard human scaling (1.7m - 1.85m), feet pivot alignment, and FBX conversion. Configured Unity ModelImporter with Humanoid avatar for the Villager (Mixamo rig) and Generic avatars with skinned/static mesh renderers for Investigator, Priest, Robber1, and Robber2. Generated URP Lit PBR materials with Albedo, Normal, Metallic, and Smoothness maps. Created the state machine `CharacterAnimator.controller` supporting Locomotion, Idle, Walk, Run, Crouch, and Interact states with Catwalk Idle animation integration. Completely deleted primitive placeholder builder scripts and materials. Renamed and updated the Villager character to Meera (female villager in an authentic red and green saree with gold embroidery) with tailored dialogue lines. Exported Lord Jagannath idol and temple architectures to `Models/Environment/` for future environment integration.
- **Assets Imported**:
  - `Investigator.fbx` (2.9 MB): Noir Detective Gumshoe hero with trench coat, hat, glasses, 121-bone skeleton, 5 PBR materials.
  - `Villager.fbx` (13 MB): Meera, Indian woman in red/green saree, 73-bone Mixamo Humanoid rig, Catwalk Idle animation clip (`AvatarRoot|mixamo.com|Layer0`), 6 PBR materials.
  - `Priest.fbx` (4.2 MB): Priest Ananda, elderly Hindu temple priest with dhoti, shawl, and rudraksha beads, 4K PBR textures.
  - `Robber1.fbx` (2.8 MB): Shadow Robber Leader (Corrupted King in Black Armor with Bone Crown), PBR textures.
  - `Robber2.fbx` (2.8 MB): Shadow Robber Accomplice (Corrupted Archangel Boss with Six Wings), PBR textures.
  - `Idol_Jagannath.fbx` (6.4 MB) & `Temple_Jagannath.fbx` (2.0 MB): Staged in `Assets/_Project/Models/Environment/`.
  - `CharacterAnimator.controller`: Complete state machine with Idle, Walk, Run, Crouch, Interact states.
- **Assets Cleaned / Deleted**:
  - Deleted `Assets/_Project/Scripts/Characters/Editor/CharacterBuilder.cs` (+ meta).
  - Deleted 12 obsolete procedural materials: `mat_Char_TrenchCoat`, `mat_Char_DhotiKhaki`, `mat_Char_LeatherBrown`, `mat_Char_SkinTone`, `mat_Char_SaffronAccent`, `mat_Char_GoldBrass`, `mat_Char_DarkHair`, `mat_Char_PriestSilk`, `mat_Char_PriestAngavastram`, `mat_Char_RobberDark`, `mat_Char_RobberRedMask`, `mat_Char_VillagerKurta` (+ meta files).
- **Files Created / Modified**:
  - `Assets/_Project/Scripts/Characters/Editor/HeritageCharacterImporter.cs` (+ meta)
  - `Assets/_Project/Scripts/Characters/PlayerController.cs` (Animator parameter binding)
  - `Assets/_Project/Scripts/Characters/NPCFacePlayer.cs`
  - `Assets/_Project/Models/Characters/` (5 FBX files + meta, 12 PBR materials + meta, Textures folder)
  - `Assets/_Project/Models/Environment/` (`Idol_Jagannath.fbx`, `Temple_Jagannath.fbx` + meta)
  - `Assets/_Project/Animations/CharacterAnimator.controller` (+ meta)
  - `Assets/_Project/Prefabs/Characters/` (All 5 prefabs updated + meta)
  - `Assets/_Project/Scenes/Environments/Courtyard.unity` (Real characters spawned at player_start, priest_spawn, villager_spawn)
  - `Assets/_Project/Scenes/Dev/CharacterTest.unity` (Showcases all 5 real characters side-by-side)
  - `README.md` (Added Art & Asset Credits section)
- **Verification**: Executed Unity 6 batch mode importer and validator (`HeritageCharacterImporter.ValidateHeritageCharacters`). Confirmed 0 errors, 0 warnings, and clean exit code 0.
- **Known Issues**: None.

## P4 - Input System Migration & Courtyard Visual Upgrade
- **Date**: 2026-11-05
- **Engine**: Unity 6 (`6000.6.8f1`) URP
- **Summary**:
  1. **New Input System Migration**: Resolved `InvalidOperationException` by completely replacing all legacy `UnityEngine.Input` and `KeyCode` references across `Assets/_Project` with the new Input System (`UnityEngine.InputSystem`: `Keyboard.current`, `Mouse.current`, `Gamepad.current`). Migrated `PlayerController` (WASD/stick movement, LeftShift sprint, Space jump, C crouch, E/Gamepad buttonWest interaction), `OrbitCamera` (mouse delta look, right-click orbit, scroll wheel zoom, gamepad right stick), `NPCDialogueTrigger` (Space/Enter/E/South button dialogue advance), and `WorldDevTools` (keys 1-5 scene jump, Q quality cycle). Added `Unity.InputSystem` references to all project asmdefs and maintained `activeInputHandler: 1` ("Input System Package (New)").
  2. **Courtyard Visual Upgrade**:
     - Replaced procedural box-built `Temple_RekhaDeula` with authentic `Temple_Jagannath.fbx` (182 architectural meshes exported cleanly from Blender with ground pivot at Y=0 and scale 1:1, non-convex `MeshCollider` on all primary walls, towers, base, and sanctum structures).
     - Built 5-step grand stone entrance stairs (`EntranceSteps`) leading into the sanctum portal.
     - Plaza floor updated to natural warm grey stone (`_BaseColor`: 0.48, 0.44, 0.40, `_Smoothness`: 0.38, `_Metallic`: 0.0), completely eliminating blue mirror tint and high glare.
     - Crafted rich sunrise mood: procedural sunrise skybox (`mat_SunriseSkybox`), low directional sun at 14° elevation with warm peach-gold illumination, soft orange atmospheric fog (`RenderSettings.fog`), and a global URP `Volume` profile with ACES filmic Tonemapping, subtle warm Bloom, ColorAdjustments, and Vignette. Camera post-processing enabled.
     - Maintained strict 5 real-time lights budget (1 Directional Sun + 4 Brass Deepastambha Lamps <= 8 max limit) ensuring 60 FPS performance.
- **Files Created / Modified**:
  - `Assets/_Project/Scripts/Characters/PlayerController.cs` (Input System migration)
  - `Assets/_Project/Scripts/Characters/OrbitCamera.cs` (Input System migration)
  - `Assets/_Project/Scripts/Characters/NPCDialogueTrigger.cs` (Input System migration)
  - `Assets/_Project/Scripts/World/WorldDevTools.cs` (Input System migration)
  - `Assets/_Project/Scripts/World/LOT.World.asmdef`
  - `Assets/_Project/Scripts/Characters/LOT.Characters.asmdef`
  - `Assets/_Project/Scripts/Gameplay/LOT.Gameplay.asmdef`
  - `Assets/_Project/Scripts/UI/LOT.UI.asmdef`
  - `Assets/_Project/Scripts/World/Editor/LOT.World.Editor.asmdef`
  - `Assets/_Project/Scripts/Characters/Editor/LOT.Characters.Editor.asmdef`
  - `Assets/_Project/Models/Environment/Temple_Jagannath.fbx` (Clean 182-mesh export)
  - `Assets/_Project/Models/Environment/Materials/` (`mat_StoneFloorWet`, `mat_SunriseSkybox`, `mat_WarmTempleStone`, `mat_LightStone`, `mat_DarkStone`, `mat_SacredMetal`, `mat_SanctumDarkness`, `mat_TempleRed`, `mat_TempleWood`, `mat_CourtyardStone`, `mat_InvestigationClue` + meta files)
  - `Assets/_Project/Scenes/Environments/Courtyard_VolumeProfile.asset` (+ meta)
  - `Assets/_Project/Scenes/Environments/Courtyard.unity` (Rebuilt with upgraded visuals, Temple, steps, sunrise lighting, volume profile, player & NPCs)
  - `Assets/_Project/Scripts/World/Editor/EnvironmentBuilder.cs` (Upgraded `BuildCourtyard` and added `VerifyCourtyardVisuals`)
  - `ProjectSettings/EditorBuildSettings.asset`
- **Verification**: Batch mode compilation and verification executed via `EnvironmentBuilder.VerifyCourtyardVisuals` and `HeritageCharacterImporter.ValidateHeritageCharacters`. Confirmed 0 compile errors, 0 runtime exceptions, 182 temple meshes with colliders, 5 real-time lights, and exit code 0.
- **Known Issues**: None.
