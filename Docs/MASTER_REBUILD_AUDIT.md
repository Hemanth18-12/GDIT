# Master rebuild: project audit (before Part 2)

Written 2026-11-15 after `git tag backup-before-master-rebuild` and `backup-before-part2`. Only real paths and object names.

## Engine
- Unity 6000.6.8f1, Universal Render Pipeline. Project: `My project/` (Assets, Packages, ProjectSettings are symlinks to the repo root).
- Input: new Input System. Cinemachine 6.6.0, Timeline 6.6.0 and Splines 2.9.1 were added in Part 2 (they ship inside this Unity version; nothing was downloaded).

## Scenes (`Assets/_Project/Scenes/`)
| Scene | Use |
|---|---|
| `Dev/MainMenu.unity` | Main menu |
| `Environments/Courtyard.unity` | Chapter 1 and the ending; the village is built round it at runtime |
| `Environments/Sanctum.unity` | Storage hall (chapter 2) |
| `Environments/ShrineCrime.unity` | Outer shrine (chapter 3) |
| `Environments/PuzzleWall.unity` | Sun Wall (chapter 4) |
| `Environments/JunglePath.unity` | Jungle ruins, stealth and fight (chapter 5) |
| `Dev/CharacterTest.unity` | Test scene |

## Characters (`Assets/_Project/Models/Characters/`, prefabs in `Assets/_Project/Prefabs/Characters/`)
| Model | Prefab | Rig | Notes |
|---|---|---|---|
| `Investigator.fbx` | `Investigator.prefab` | Humanoid | Aryan (from `Heritage Mystery Project/noir-detective`) |
| `Priest.fbx` | `Priest.prefab` | Generic | Pujari Ananda; has an embedded "Point" take |
| `Villager.fbx` | `Villager.prefab` | Humanoid | Meera (from `indian-woman-in-saree`); has an embedded "Pray" (namaste) take |
| `Robber1.fbx` | `Robber1.prefab` | Humanoid | **Stand-in**: tinted copy of the detective rig with scarf and blade (see `Docs/BLOCKED.md`) |
| `Robber2.fbx` | `Robber2.prefab` | Humanoid | **Stand-in**: tinted copy with hood and staff |

The original `Heritage Mystery Project/` (assets.blend, temple and god .blend files, zips) is read-only and was not changed.
Real Bhaskar and Kishore models are still missing: `assets.blend` contains no human robber models (see `Docs/BLOCKED.md`).

## Environment
- `Models/Environment/Temple_Jagannath.fbx`, `Idol_Jagannath.fbx` (background shrine only, never touched by gameplay).
- `Assets/temple.fbx` (added by the owner, 125 MB, 114,511,446 triangles in 27,640 objects, 12 materials, no textures).
  Moved unchanged to `Art_Source/Temple/temple.fbx` (kept out of git: over GitHub's 100 MB limit). A game copy is made from it.
- Level geometry is Unity primitives with Poly Haven CC0 PBR materials (`Resources/Mats`) and props (`Resources/Props`).

## Animations (see `Docs/ANIMATION_INVENTORY.md`)
Five Mixamo clips in `Animations/Clips/Hero/` (Idle, Walking, Running, Jump, Crouch), plus the two takes embedded in Priest.fbx and Villager.fbx.

## Vehicles
None before Part 2. The investigator's jeep is now built in Blender from the reference sheet (`Tools/blender/build_jeep.py`).

## Audio
`Assets/_Project/Audio/Resources/Audio/` (own synthesised effects, ambience and music; mixer `LotMixer.mixer`), voices in `Resources/Voice/` (macOS text-to-speech placeholders).

## UI
Runtime-built uGUI/TextMeshPro: `UI/UIManager.cs`, `UI/MainMenuController.cs`, `UI/HeritagePauseMenu.cs`, `UI/LoadingScreen.cs`, `UI/GuideSystem.cs`, `UI/ScreenTextRenderer.cs`, `UI/FightHud.cs`, `UI/TouchControls.cs`.
The 12-step `UI/TutorialManager.cs` opened over the main menu (owner's screenshot, `Docs/References/before_menu_tutorial_overlap.png`); it no longer starts by itself.
