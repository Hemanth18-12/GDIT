# Legacy of the Temple — Master Build Asset Audit

Generated per Master Build Plan instructions.
Git tag: `backup-master-build`

## 1. Engine & Environment
- **Unity Version**: Unity 6000.6.8f1 (URP, macOS)
- **Project Path**: `/Users/hemanth/Desktop/GDIT/My project`
- **Git Remote**: `https://github.com/Hemanth18-12/GDIT.git` (branch `main`)
- **Input System**: New Input System (UnityEngine.InputSystem)

## 2. Environment & Temple Asset Audit
- **Source Temple Path**: `Art_Source/Temple/temple.fbx`
  - Source file size: 119 MB (kept out of git tracking to satisfy GitHub 100MB limit)
  - Triangle count: 114,511,428 triangles across 27,639 objects
  - Studio backdrop "Floor" (3595 x 4003 x 857) removed
  - Scaled to authentic 62m shikhara height (Kalinga architectural proportions)
- **Optimized Game Temple Path**: `Assets/_Project/Models/Temple/Temple_Game.fbx`
  - Mesh smoothing: AreaAndAngleWeighted smooth normals enabled across all pillars and stone carvings (eliminating polygonal faceting)
  - Triangle count: 1,867,721 triangles in 9 spatial area meshes for occlusion/frustum culling
  - LOD1 & Collision Mesh: `Assets/_Project/Models/Temple/Temple_Game_LOD1.fbx` (1,380,293 tris, 9 mesh colliders pre-baked at build time)
  - Materials: PBR sandstone wall, laterite stone, carved rosettes (`sandstone_blocks_08`), saffron/terracotta tier trim, gold kalash finial, waving red flag
- **Shrine / Background**:
  - `Assets/_Project/Models/Environment/Idol_Jagannath.fbx`: Non-interactable background shrine element. Never stolen, carried, or touched.

## 3. Character Models & Skeletons
| Character | Model Path | Rigging | Mesh Tris | Bones | Status |
|---|---|---|---|---|---|
| **Investigator Aryan** | `Assets/_Project/Models/Characters/Investigator.fbx` | Humanoid | 16,846 | 245 | Hero character, PBR textures, new unarmed punch combat clips |
| **Pujari Ananda** | `Assets/_Project/Models/Characters/Priest.fbx` | Generic/Humanoid | 24,809 | 29 | Head priest in traditional dhoti/kurta |
| **Meera** | `Assets/_Project/Models/Characters/Villager.fbx` | Humanoid | 89,061 | 65 | Village girl in traditional Odisha saree |
| **Villager Male** | `Assets/_Project/Models/Characters/Villager_Male.fbx` | Humanoid | 24,809 | 29 | Traditional male villager in dhoti/kurta (various khadi/ochre colors) |
| **Robber 1 (Bhaskar)** | `Assets/_Project/Models/Characters/Robbers/Robber1_Real.fbx` | Humanoid | 39,999 | 22 | Armoured warrior with sword (100% rigid hand weighting) |
| **Robber 2 (Kishore)** | `Assets/_Project/Models/Characters/Robbers/Robber2_Real.fbx` | Humanoid | 40,000 | 22 | Winged warrior with staff (100% rigid spine weighting on wings) |

## 4. Vehicle Assets (Jeep)
- **Reference**: `Docs/References/jeep_reference_sheet_v2.png` (rugged white/olive SUV, bull bar, roof rack with 4 spot lights, rear spare tyre, plate "OD 02 8047", right-hand drive interior)
- **Model**: `Assets/_Project/Models/Vehicles/Jeep/Car_Model.fbx` (35,504 tris)
  - LOD1: `Car_Model_LOD1.fbx` (16,210 tris)
  - LOD2: `Car_Model_LOD2.fbx` (6,180 tris)
  - Damage states: `Car_Damage_Damaged.fbx`, `Car_Damage_Wrecked.fbx`, `Car_Damage_Destroyed.fbx`
- **Controller Scripts**:
  - `Assets/_Project/Scripts/Gameplay/Vehicles/CarController.cs` (Ported from ilisescu03/Unity---Car-Controller to new Input System)
  - `CarAudio.cs`, `CarLights.cs`, `CarFollowCamera.cs`, `Jeep.cs`
- **Audio Clips**: Engine idle, run, rev, horn, door open/close in `Assets/_Project/Resources/Audio/Car/`

## 5. Animation Library Audit
- **Hero Locomotion**: `Idle.fbx`, `Walking.fbx`, `Running.fbx`, `Jump.fbx`, `Crouch.fbx`
- **Combat & Unarmed Animations**: Quaternius Universal Animation Library (CC0 Public Domain)
  - `Punch_Jab.anim` (Light attack 1 & combo finisher)
  - `Punch_Cross.anim` (Light attack 2 & Heavy strike)
  - `Hit_Chest.anim`, `Hit_Head.anim` (Damage reactions)
  - `Roll.anim` (Combat dodge)
  - `OHS_BlockHold.anim` (Defensive guard)
- **Interactive & Vehicle Clips**:
  - `Driving_Loop.anim` (Seated driver in Jeep)
  - `Sitting_Enter.anim`, `Sitting_Exit.anim` (Jeep enter/exit)
  - `Idle_Talking_Loop.anim` (NPC villager conversations)
  - `Fixing_Kneeling.anim`, `PickUp_Table.anim` (Evidence inspection)

## 6. Audio Architecture
- **Mixer**: `Assets/_Project/Audio/Resources/Audio/LotMixer.mixer`
  - Groups: Master, Music, SFX, Ambience, Voice with dynamic ducking
- **Ambience**: `amb_village.wav`, `amb_birds.wav`, `amb_rain.wav`, `amb_sanctum.wav`
- **Voice**: 41 data-driven dialogue voice hooks in `Assets/_Project/Resources/Voice/`
