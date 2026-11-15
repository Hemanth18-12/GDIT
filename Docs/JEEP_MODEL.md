# The investigator's jeep (version 2, olive SUV)

Built in Blender 5.2 by `Tools/blender/build_jeep_v2.py`, which follows the reference sheet `Docs/References/jeep_3d_design_reference.png`.
Run it headless from the repository root:

    /Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/blender/build_jeep_v2.py -- "$PWD"

The three textures (number plate, temple emblem, paint dirt) are made by `Tools/blender/make_jeep_textures.py` (own work).
The older white/red classic jeep script `Tools/blender/build_jeep.py` is kept only for history; its output files were replaced.

## What the model has
| Area | Parts |
|---|---|
| Body | Hollow lower body with wheel wells, engine bay and door openings; painted cabin with window openings; black roof cap; hood; 4 fender flares; grille with horizontal slats; front and rear bumper |
| Doors | 4 doors with window frames, handles and rub strips; each door owns its glass; the front doors carry the gold temple emblem |
| Glass | Windscreen, rear window, 2 quarter windows, 4 door windows |
| Lights | 2 round headlights with chrome rings, 2 fog lights, 4 roof spot lights, 2 tail lights, 2 reverse lights, 4 indicators |
| Wheels | 4 wheels and the rear spare: tyre with tread blocks and shoulder lugs, black six-twin-spoke alloy, brake disc, wheel nuts |
| Accessories | Bull bar with lower cross-bar, roof rack with rails and floor bars, chrome side steps, mirrors, wipers, antenna, number plates "IND 01 BX 7721" (fictional) |
| Interior | Right-hand drive: dashboard, instrument cluster, centre touch screen, steering wheel on its own column axis, gear lever, handbrake, console, pedals, 4 seats, rear-view mirror |
| Underneath | Chassis rails, axles, differentials, driveshaft, shocks, exhaust, skid plate, fuel tank, engine block, black wheel-well liners |

## Numbers (`Art_Source/Jeep/jeep_validation.txt`)
- 4.38 m long (with plates), 2.09 m wide with mirrors (1.96 m over the flares), 2.00 m tall with the rack; wheelbase 2.72 m; tyres 0.78 m.
- 52,778 triangles, 65 objects, 18 materials. LOD1 22,153, LOD2 6,331, LOD3 4,408.
- Damage states (whole visible car): Damaged 68,482, Wrecked 64,428, Destroyed 52,342 triangles.
- Collision boxes: `UCX_Jeep_Body`, `UCX_Jeep_Cabin`, `UCX_Jeep_Rack`.
- Pivots: wheels at the centre, doors at the front hinge, hood at the rear hinge, steering wheel on its column.

## Files
- `Art_Source/Jeep/Car_Model.blend`, `Car_Model.fbx`, `Car_Model.glb`, `Car_Model_LOD1|2|3.fbx`, `Car_Textures/`
- `Art_Source/Jeep/Car_Damaged.blend`, `Car_Damaged/Car_Damage_Damaged|Wrecked|Destroyed.fbx` (bent body and hood)
- `Art_Source/Jeep/Car_Collision_Meshes/Car_Collision.fbx`, `Car_Reference_Renders/` (normal: 8 views; each damage state: 3 views)
- `Docs/Blender/Jeep/Jeep_v001..v003.blend` (v001-v002 block-out, v003 the detailed model)
- In Unity: `Assets/_Project/Models/Vehicles/Jeep/` (FBX, `Materials/`, `Textures/`), prefab `Assets/_Project/Resources/Vehicles/Jeep.prefab` built by `Part2Tools.BuildJeep`.

## In the game (`Scripts/Gameplay/Vehicles/`)
The driving, braking, lights, horn, get in / get out, seated driver and crash damage code is the existing system. The new model uses the same
part names (`SM_Jeep_*`), so that code finds the wheels, doors, hood, lamps, glass and breakable parts without changes to how it works.
Code fixes made with this model are listed in `Docs/EXPLAINED.md` (2026-11-15, "New investigator jeep").

## Not done
- No Blender animation Actions: the game moves wheels, steering, doors, hood and lights from code.
- The tailgate does not open. Glass does not break into modelled shards (it disappears with a spark burst, as before).
- Colour variants are the four tints in `Jeep.SetPaint` (white, olive, red, blue), not separate Blender materials.
- Dirt is one simple texture; there are no baked normal or roughness maps.
