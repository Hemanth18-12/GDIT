# The investigator's jeep

Built in Blender 5.2 by `Tools/blender/build_jeep.py` (run headless: `Blender --background --python Tools/blender/build_jeep.py -- Art_Source/Jeep Assets/_Project/Models/Vehicles/Jeep`).
Workflow and checks follow the MIT "blender-skills" pack (vehicle-artist, hard-surface, validation-checklist, naming-conventions); Blender MCP was not connected in this session, so the same steps run as a script.

## Reference analysis (from `Docs/References/jeep_reference_sheet.png`, owner's direction: red, classic jeep look)
| Category | Decision |
|---|---|
| Subject | Drivable gameplay vehicle, forward -Y in Blender (+Z in Unity) |
| Silhouette | Boxy classic jeep: flat hood, near-upright windshield, flat roof, square tail, separate flat front fenders |
| Proportions | 4.35 m long, 1.8 m wide (2.1 m over steps), 2.1 m to the roof lights; wheelbase 2.8 m; tyre radius 0.40 m |
| Materials | Gloss red paint, black hardtop and trim, satin alloy wheels, chrome bezels and bolts, tinted glass, gold temple emblem |
| Features kept from the sheet | Roof rack with 4 spot lights, bull bar, rear spare wheel, side steps, black arch flares, mirrors, wipers, number plate (fictional "LOT 7721") |
| Classic details added | 9-slot body-colour grille with round lamps (not the 7-slot trademark), exposed door hinges, rubber hood T-latches, windshield frame with hinges, bolted flares, red tow hooks, fuel cap, mud flaps |

## Validation (`Art_Source/Jeep/jeep_validation.txt`)
- LOD0 35,504 triangles (drivable budget 30,000–80,000); LOD1 13,248; LOD2 3,455.
- 72 objects, all `SM_Jeep_*`; materials `MAT_*`; collections `COL_Jeep_*`; convex collision `UCX_Jeep_Body`, `UCX_Jeep_Cabin`.
- No unapplied scale, no loose vertices; bevels plus weighted normals (sharp above 35°).
- Moving parts with correct pivots: 4 wheels (axle centre), 4 doors (front hinge), hood (cowl hinge), rear door (left hinge), steering wheel (column).

## Files
- `Art_Source/Jeep/Car_Model.blend`, `Car_Model.fbx`, `Car_Model.glb`, `Car_Model_LOD1.fbx`, `Car_Model_LOD2.fbx`
- `Art_Source/Jeep/Car_Damaged.blend`, `Car_Damaged/Car_Damage_Damaged|Wrecked|Destroyed.fbx` (deformed body and hood shells; scuffed and burnt paint)
- `Art_Source/Jeep/Car_Collision_Meshes/Car_Collision.fbx`
- `Art_Source/Jeep/Car_Reference_Renders/` (front, back, left, right, three-quarter, top-rear; normal and destroyed)
- In Unity: `Assets/_Project/Models/Vehicles/Jeep/` (FBX + `Materials/`), prefab `Assets/_Project/Resources/Vehicles/Jeep.prefab` built by `Part2Tools.BuildJeep`.

## In the game (`Scripts/Gameplay/Vehicles/`)
- `Jeep.cs` wires the model at runtime: WheelColliders (grippy sideways friction so it does not slide), anti-roll bars, rigidbody 1,650 kg, body and cabin box colliders, head / brake / reverse / indicator lights, engine audio, E to get in and out at the driver's door, driving camera, speed panel.
- Damage depends on impact strength (speed change on contact, bumps under 2.2 m/s and kerbs ignored) and location (front, rear, left, right, top). Parts in the hit zone break at their own thresholds: glass shatters (shards, glass sound), lamps go dark and fall off, bumpers, bull bar, plates, mud flaps, side steps, doors, hood, roof rack and spot lights detach with physics. Debris is pooled (at most 18 pieces, the oldest put away; each removed after 25 s). Total damage moves the body through Normal → Damaged (20) → Wrecked (55, smoke) → Destroyed (100, engine dead, burnt paint).
