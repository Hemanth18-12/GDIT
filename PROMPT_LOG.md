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
- **Verification**: Verified syntax and compilation integrity across all C# scripts and Assembly Definitions. Confirmed no gameplay, world, or character logic was introduced in this step.
