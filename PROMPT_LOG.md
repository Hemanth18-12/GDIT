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
