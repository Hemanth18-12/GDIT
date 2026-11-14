# Animation inventory (2026-11-15)

## What the project has
| Clip | File | Rig | Used by |
|---|---|---|---|
| Idle | `Assets/_Project/Animations/Clips/Hero/Idle.fbx` | Humanoid (Mixamo) | Investigator, villagers, robbers (via `CharacterAnimator.controller`) |
| Walking | `.../Hero/Walking.fbx` | Humanoid | same |
| Running | `.../Hero/Running.fbx` | Humanoid | same |
| Jump | `.../Hero/Jump.fbx` | Humanoid | Investigator |
| Crouch | `.../Hero/Crouch.fbx` | Humanoid | Investigator (crouch) |
| Point | embedded take in `Models/Characters/Priest.fbx` | Generic | Pujari (`PriestAnimator.controller`), used in the intro when he finds the broken lock |
| Pray (namaste) | embedded take in `Models/Characters/Villager.fbx` | Humanoid | Meera (`VillagerAnimator.controller`), used in the intro when she greets and gives directions |
| Staging copies | `Mixamo_Staging/*.fbx` (12 files, outside Assets) | X Bot | Not imported (duplicates of the above, plus Walking Left Turn, Walking Hit Reaction, Fight Idle To Standing Idle) |

## Sources checked in Part 2
| Source | Licence | Result |
|---|---|---|
| github.com/GrokCodeReliant/OpenKit3D | CC0 | Its 31 clips (KayKit and UAL2 extracts, one-hand-sword combat) are **Generic clips bound to its own HeroMale/HeroFemale rig**, not Humanoid. They cannot be put on our characters without re-exporting them from source as Humanoid. Nothing was imported. |
| github.com/firstkindgamer/QuaterniusUnityUtils | Public domain | An importer for the Quaternius Universal Animation Library. Copied to `Scripts/World/Editor/ThirdParty/QuaterniusUtils.cs`, but the library itself is not in any of these repos (it is downloaded from quaternius.com). |

## Gaps (still missing, honestly)
Talking with hand gestures, looking around, picking up, opening a door, phone call, carrying a bag, getting out of a vehicle, shocked reaction, kneel / inspect, and all fight moves.
Nothing was faked with generated clips. Where a beat needs one of these, the cinematic uses camera framing and the real clips above.
Fastest fix: download these Mixamo clips as **FBX for Unity, Without Skin, 30 fps** (In Place for locomotion) into `Assets/_Project/Animations/Clips/`:
"Talking" (x2 variants), "Looking Around", "Picking Up", "Opening A Door", "Talking On Phone", "Carrying" (bag), "Exit Car", "Surprised", "Kneeling", "Sneaking Forward", plus the fight list in `Docs/COMBAT_DESIGN.md`.

## Stutter / freeze check
Characters that move during scripted beats (robbers, villagers, the investigator in the intro) are driven by `NpcAnimDriver`, which feeds the Animator from real movement each frame, so walk speed and clip match and nothing slides with an idle pose.
