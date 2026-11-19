# Legacy of the Temple: the whole game explained

Written from the real project on 2026-11-19: the Courtyard scene, the scripts in `Assets/_Project/Scripts`, `Resources/Story/story.json` (version 3, 9 steps) and the V3 design documents. Where something is a placeholder or has no real reason, it says so.

---

## 1. What the game is (5 lines)
1. A short **third-person mystery and action game** made in Unity 6 (URP, C#), about 20 to 30 minutes long.
2. **Setting:** Suryapur, a fictional Indian temple village, on the night before the temple festival.
3. **Goal:** five fictional temple treasures were stolen. You are Aryan, an investigator. Get all five back before dawn (05:45 on the Dawn Clock).
4. **How you play:** physical puzzles (carry, fit, turn, push, place), a little stealth, a short fight in every place, and one jeep chase.
5. **The twist:** someone opened the storeroom from the inside. Four small pieces of evidence point to Madhav, the friendly key-keeper.

---

## 2. The story in order
Everything happens in **one scene** (`Courtyard.unity`). The six places are built around the temple when the scene loads.

| Step (story.json id) | What happens | Why it is there |
|---|---|---|
| Intro (`IntroCinematic.cs`) | Aryan drives his jeep into the village. Meera gives directions. | Sets the place, the hero and the jeep in 60 seconds with very little talk. |
| 1. Reach Jagannath Temple (`obj_c0_reach`) | Walk up the village road and through the old stone gate. The temple appears. | Teaches walking. The temple reveal is the first "wow" moment. |
| 2. Find the priest (`obj_v3_pujari`) | Pujari Ananda: "Five treasures. Gone before sunrise." | Gives the goal and the deadline. |
| 3. The bell in the bazaar (`obj_v3_bazaar`) | Follow a courier with a ringing bag without being seen. Fight him and a helper in a yard. | Treasure 1 (the Dawn Bell) and evidence 1 (a list in neat handwriting). Teaches stealth and fighting. |
| 4. The storeroom (`obj_v3_storeroom`) | Rebuild the break-in: broken pot, lantern, oil stain, lock plate, loose flagstone. Fight the lookout. | Treasure 2 (the Palm-Leaf Chronicle) and evidence 2 (the lock was opened from inside). |
| 5. The vaults (`obj_v3_vaults`) | Push stone blocks onto plates, then turn three stone discs to match the light on the floor. Fight two guards. | Treasure 3 (the Sun Rosette) and evidence 3 (a ledger signed "M."). |
| 6. The stepwell (`obj_v3_stepwell`) | Fill the tank with two sluice wheels, ride the raft, drain to the right mark, push the panel. Fight the knife man and a helper. | Treasure 4 (the Monsoon Vessel) and evidence 4 (a temple key ring). |
| 7. The Sun Wall (`obj_v3_sunwall`) | Turn four mirrors so two sunbeams hit two carvings; push the stone wheel so a spoke stops blocking. Fight Kishore (two phases). | Finds the empty case of treasure 5 and the route to the river. |
| 8. The river road (`obj_v3_jungle`) | Chase Bhaskar in the jeep to the ruins. Fight wave 1, wave 2, then Bhaskar (three phases). Madhav gives himself up. | Treasure 5 (the Chariot Finial). The final fight. |
| 9. Return the treasures (`obj_return`) | Bring them back to the priest. | Closes the story. Then the ending screen. |

---

## 3. How to complete the game (walkthrough)
Controls are in section 5. **E** does every action (grab, turn, push, place, talk). **H** gives three hints for the puzzle you stand at (each costs score).

1. **New Game.** The intro plays (hold **Esc** to skip). Aryan arrives by jeep.
2. **Reach the temple.** Walk up the village road (Shift to run) through the old stone gate.
3. **Find the priest** at the temple steps and talk to him (**E**).
4. **Bazaar.** Listen for a bell: a courier with a ringing bag walks the village road. Follow him. When he stops and looks back, be far away, behind something, or crouching (**C**). If he sees you three times he runs, but he still ends in the same yard. In the yard, beat the courier and his helper. Take the **Dawn Bell** (**E**). His list is noted as evidence.
5. **Storeroom** (the broken pot and lantern puzzle):
   1. Find the **three pot shards** lying around the storeroom front. Carry each one (**E** to pick up, **E** to place) to the **plinth** on the left. When all three are on, the pot is whole, holds oil, and the **lantern lights**.
   2. Carry the lit **lantern** and put it down on the **dark oil stain** by the barred door.
   3. Turn the **lock plate** on the door post (**E**, twice): the scratches are on the inside. Evidence noted.
   4. **Lift the pale flagstone** in front of the lantern. The lookout attacks. Beat him, then take the **Palm-Leaf Chronicle**.
6. **Vaults.**
   1. Push **block A** four times toward the gate side onto plate 1. Push **block B** twice round the pillar, then twice the same way onto plate 2. The bar gate opens. (Wrong push? Pull the **reset lever**.)
   2. Look at the **rings of light on the floor**. Turn the three discs to match: **small disc 2 turns, middle disc 5 turns, large disc 3 turns** (each disc has 6 stops). The chest opens: the ledger signed "M." is noted.
   3. Beat the two guards, take the **Sun Rosette**.
7. **Stepwell** (night, rain).
   1. Turn the **south sluice wheel** and the **north sluice wheel** on the rim. The tank floods and the raft rises.
   2. Step onto the **raft** and pull the **drain chain twice**. The water rests on the long mark, where the raft meets the carved panel.
   3. **Push the panel.** Beat the knife man and his helper, take the **Monsoon Vessel**. The key ring is noted.
8. **Evidence board (B).** With three or four pieces you can name the inside man once. The right answer is **Madhav**. Naming him correctly makes the last fight shorter.
9. **Sun Wall** (midday).
   1. Turn each of the **four mirrors two stops** (**E** twice each).
   2. **Push the stone wheel one stop** so no spoke blocks the beam. Both carvings light and the wall opens.
   3. Fight **Kishore** (two phases). Take the **empty case**.
10. **River road.** Get in the jeep (**E**), drive after Bhaskar (W, A, D; Space brakes). You cannot lose: if you fall behind he slows down. At the ruins, get out and walk into the court.
11. **Final fight.** Wave 1 (three robbers), wave 2 (one heavy, plus two more if you did not name Madhav), then **Bhaskar** in three phases. Take the **Chariot Finial**.
12. **Return** to the priest at the temple and talk to him. The ending screen shows treasures, evidence, a grade per place, the clock, the score and your rank. Press **Enter** for the main menu.

**Fight tips:** left click strikes (three hits make a combo), **R** heavy, **Q** hold to block and tap just before a hit to parry, **V** dodge roll, **F** finish a staggered robber. If you lose a fight it restarts on the spot.

---

## 4. Every puzzle explained

### What you asked about: the broken pot and the lamp
- **The broken pot puzzle exists** (the storeroom, `BreakInPuzzle.cs`). The three shards are pieces of one pot, modelled in Blender (`Tools/blender/build_breakin_pot.py`, prefab `Resources/Props/V3/BreakInPot`).
- **The lamp puzzle exists in two versions.** The V3 game has the **lantern** step in the storeroom. The older V2 game also had a "relight five lamps in the order of the sun" puzzle (`Chapters.cs`, Sanctum scene). That old one is **not in the V3 story** any more.

### Storeroom (pot, lantern, lock plate, flagstone): `BreakInPuzzle.cs`
- **What the player does:** carries three shards to the plinth, carries the lantern to the oil stain, turns the lock plate, lifts the flagstone.
- **How it works in code:**
  - Each shard is a `Carryable`. The plinth has three `PlacementSocket`s, and each one only accepts its own shard.
  - When all three sockets are full, `Refresh()` shows the whole pot and turns on the lantern's `Light`.
  - A `ProximityMatch` checks whether the lantern is within 0.95 m of the stain.
  - The flagstone is a `PuzzleLever`. It stays locked until the pot is whole and the lantern is on the stain.
  - The lock plate is a `RotatingDisc` with 4 stops. At stop 2 it records the evidence "scratches on the inside".
  - A `PuzzleGroup` holds the solution (3 sockets full, flagstone up), the three hints, and the score.
- **Why it is there:** it rebuilds the crime from objects instead of a text clue. The pot spilled oil, the lamp stood on the oil, and its light shows the loose stone.
- **What the player learns:** look at the scene, put things back where they were, and the place tells you what happened. The lock plate also starts the inside-man mystery.

### Bazaar tail: `BazaarTail.cs`
- **What the player does:** follows a courier who stops and looks back at set points.
- **How it works:** the courier can "see" the player when close, in the open and not crouching. Three sightings and he runs, but he still ends in the same yard (soft fail). His bell sound is made in code (two struck tones that fade).
- **Why:** it teaches crouching and distance without a tutorial, and the sound guides you instead of a marker.
- **What the player learns:** stealth and observation.

### Vaults: `VaultPuzzle.cs`
- **What the player does:** pushes blocks onto plates, then turns three discs.
- **How it works:**
  - Two `PressurePlate`s and two `PushableBlock`s. A block moves one step per push and stops at walls. A `PuzzleLever` resets both blocks.
  - Three `RotatingDisc`s with 6 stops each. The answer is `{2, 5, 3}` (`VaultPuzzle.Answer`). It is not written anywhere; the light from the roof grate draws it on the floor.
- **Why:** the "aha" is noticing the light pattern you walked past.
- **What the player learns:** read the environment, plan moves (block B must go round the pillar).

### Stepwell: `StepwellPuzzle.cs`
- **What the player does:** opens two sluice wheels, rides the raft, drains twice, pushes the panel.
- **How it works:**
  - `WaterValve` wheels change the level of a `WaterBasin`. The raft follows the water level.
  - The panel only counts at water level 2 (the solution is level 2 plus the panel pushed).
  - Too much water hides the panel, which is the "aha".
- **Why:** cause and effect with water; the soft fail is that the drain always brings it back.
- **What the player learns:** the obvious move (open everything) is not the answer.

### Sun Wall: `SunWallPuzzle.cs`
- **What the player does:** turns mirrors and pushes a big stone wheel.
- **How it works:** four `LightMirror`s (16 stops each) bounce two `LightBeamEmitter` beams. A beam lights a `LightSocket` when it hits it. The wheel is a `RotatingDisc` (8 stops) whose spoke blocks one beam until it is pushed one stop.
- **Why:** it is a light puzzle (the planned four sockets and timed beam were simplified, decision D-16).
- **What the player learns:** angles and blocking, in a simple form.

### River road: `JungleChase.cs`
- **What the player does:** chases Bhaskar in the jeep, then fights three waves at the ruins.
- **How it works:** Bhaskar runs along a list of road points. His speed depends on the gap: he slows to 3.2 m/s if you are more than 55 m behind, so you can never lose. Then the waves, then the boss, all through `Brawl.Begin`.
- **Why:** it is the climax, and the jeep finally has a job.

### Old puzzles still in the project (not in the V3 story)
- The lamp order, deduction board, dial lock, bell traps, evidence timeline and Sun Wall symbols are V2 content in `Chapters.cs`, `ChakraPuzzleController.cs` and the old scenes. They are kept in git and in the project.
- **Check this before the viva:** do not demo them as part of the current game.

---

## 5. Systems explained
| System | What it does | Where | Why this way |
|---|---|---|---|
| **Movement** | Walk 4 m/s, run 7.5 m/s (Shift), crouch 1.8 m/s, Caps Lock auto-run, jump | `Characters/PlayerController.cs` | A `CharacterController` (capsule that slides along walls) is simpler and steadier than a physics body for a walking hero. |
| **Animation** | Idle, walk and run are blended by speed; playback speed follows the real speed so the feet do not slide | `CharacterAnimator.controller`, `PlayerController.cs` | Mixamo clips on a Humanoid avatar, so one set of clips works on any human model. |
| **Camera** | Third-person orbit camera that follows the player and avoids walls | `Characters/OrbitCamera.cs` | Standard for third-person games. |
| **Interaction** | E acts on the nearest usable thing; a prompt shows what it does | `IInteractable`, `PlayerController`, `Puzzles/*` | One key for everything keeps controls simple. |
| **Puzzle framework** | Each part has one whole-number state, saved by id; a group checks the solution, gives 3 hints, reports the score | `Gameplay/Puzzles` (`PuzzleElement`, `PuzzleGroup`, `PuzzleLogic`) | Logic is separate from art, so puzzles can be tested automatically (15 tests pass). |
| **Dawn Clock** | Story time from 03:10 to 05:45, with five chapter deadlines; mistakes and fast travel cost minutes; it decides the ending | `Gameplay/DawnClock.cs` | A ticking clock gives tension without a fail screen. |
| **Dialogue** | 29 short spoken lines (barks and two short talks) from `story.json`, with subtitles and synthetic voices | `StoryDialogueSystem.cs`, `StoryData.cs` | Data-driven: the story is in a JSON file, not hard-coded. |
| **Journal, evidence board, map** | J journal, B evidence board (name the inside man once), M map with fast travel | `EvidenceBoard.cs`, `TempleMapOverlay.cs`, `V3Places.cs` | Helps the player remember and travel. |
| **Combat** | Light combo, heavy, dodge, block and parry, finisher; at most two attackers; health and stamina; retry on the spot | `Gameplay/Brawl.cs` | Short, readable fights; no game over. |
| **Score and grades** | Points per place (puzzle, stealth, time, damage, treasure, combo), grade S/A/B/C, top-10 table | `ScoreSystem.cs` | Replay value. |
| **Save and load** | Story step, clues, flags, clock, puzzle states, checkpoints and scores in PlayerPrefs | `StoryDialogueSystem`, `PuzzleSave`, `Checkpoints` | PlayerPrefs is built into Unity and enough for a single save. |
| **Jeep** | WheelCollider driving (ported MIT controller), lights, horn, seated driver, damage by impact strength and place | `Gameplay/Vehicles/` | Wheel colliders give real suspension and grip. |
| **Audio** | Mixer groups (music, ambience, effects, voice) with ducking; footsteps by surface; 3D sounds for fights and puzzles | `AudioManager`, `AudioDirector`, `GameSfx` | Sound guides the player (the courier's bell). |
| **Graphics** | URP, a local Volume per place (colour grade, bloom, vignette), presets Low to Ultra, 60 fps cap | `ZoneLook`, `ZoneAtmosphere`, `GraphicsPresets` | Each place has its own mood in one scene. |
| **UI** | Panels and prompts drawn by a renderer from text requests; loading screen with a real progress bar | `Core/ScreenText.cs`, `UI/ScreenTextRenderer.cs`, `UI/LoadingScreen.cs` | One place to style every box. |
| **Intro** | Cinemachine cameras on a Timeline, built in code | `IntroCinematic.cs` | Cinematic shots without hand-animating the camera. |

### Unity words used in this project
- **GameObject:** any thing in the scene. **Component:** a part attached to it (a light, a collider, a script).
- **Prefab:** a saved GameObject you can place many times (the jeep, the villagers, the pot).
- **Collider:** an invisible shape for collisions. **Trigger:** a collider that only reports "something entered", without blocking.
- **Rigidbody:** makes physics move an object (used on the jeep).
- **CharacterController:** a capsule that moves without physics forces (used on the player).
- **Animator / Animator Controller:** plays and blends animation clips. **Humanoid avatar:** lets Mixamo clips drive any human rig.
- **Input System:** Unity's newer input package (keyboard, mouse and gamepad in one place).
- **URP:** the Universal Render Pipeline, Unity's fast, scalable renderer. **Volume:** post-processing settings for an area.
- **Canvas:** the UI layer. **Cinemachine and Timeline:** smart cameras and a sequencer, used for the intro.
- **ScriptableObject:** a data asset. **Not used** for the story; the story is a JSON file read by `StoryData.cs`.
- **NavMesh:** **not used**. NPCs walk on simple scripted paths (`RoadWalker`, the courier's waypoints). No real design reason; it was simpler.

---

## 6. Assets: own work and external
Full list with licences: `ATTRIBUTION.md` (repository root).
- **Owner-supplied (Heritage Mystery Project):** the temple kit, the background shrine, the investigator, the saree villager, the priest and the two robber sculpts. The original creators of several of these are **not established** in the files (ATTRIBUTION.md says so). **Check this before the viva.**
- **Made for this project:**
  - The jeep and its damage meshes, plus the break-in pot and the V3 props (pillar, coping, plinth, lamp stand, arch), all built with Blender scripts in `Tools/blender`.
  - The village, roads and zones, generated by C# at run time.
  - Particle textures and the audio mixer.
  - Synthetic voices made with macOS `say`.
- **External with a licence:**
  - Poly Haven models and textures (CC0).
  - Mixamo animations (Mixamo terms, free use) and Open Kit 3D sword animations.
  - Fonts Cinzel and Noto Sans (SIL Open Font License).
  - A CC0 oak tree and a car controller ported from an MIT-licensed project. Both are in ATTRIBUTION.md.
- **Loading images:** supplied by the owner (AI-generated). One shows a carried idol (decision D-07). **Check this before the viva.**
