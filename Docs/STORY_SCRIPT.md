# Story Script: Legacy of the Temple

Source of truth: `Assets/_Project/Resources/Story/story.json` (loaded at runtime by `StoryData` / `StoryDialogueSystem`). This file is generated from it. All three missing pieces are **fictional**; the background shrine is never touched.

## Cast
| id | Name | Role |
|---|---|---|
| aryan | Aryan | Investigator |
| pujari | Pujari Ananda | Temple Priest |
| meera | Meera | Village Girl |
| bhaskar | Bhaskar | Red Scarf |
| kishore | Kishore | Black Hood |
| scroll | Old Scroll | Temple Record |

## Conversations

### conv_courtyard_pujari (Courtyard, NPC: priest)
- **Pujari Ananda:** Namaste. Three pieces are gone from our storage hall.
- **Pujari Ananda:** A brass ceremonial vessel, a carved sandstone rosette, and a palm-leaf manuscript.
- **Pujari Ananda:** Please find them before the festival.
- **Aryan:** Stay calm and touch nothing. Every small detail matters.
- **Aryan:** Did anyone see something?
- **Pujari Ananda:** Meera was up before sunrise. Ask her.
  - Choice 1: *Why do these pieces matter so much?*
    - **Pujari Ananda:** They carry the community's memory. Families have cared for them for generations.
    - **Pujari Ananda:** (These objects are fictional, made for this story.)
  - Choice 2: *Who can open the storage hall?*
    - **Pujari Ananda:** Only temple staff know where the key is kept. That is what worries me.
  - Choice 3: *I'll start right away.*
    - **Aryan:** I'll speak to Meera, then see the hall.

### conv_courtyard_meera (Courtyard, NPC: villager)
- **Meera:** Before sunrise I saw two men near the storage door.
- **Meera:** One wore a red scarf. The other wore a black hood.
- **Aryan:** Thank you, Meera. That helps a lot.
  - Choice 1: *Which way did they run?*
    - **Meera:** Past the outer shrine, toward the old jungle path.
  - Choice 2: *Did they carry anything?*
    - **Meera:** Cloth bundles. They held them carefully, like they knew their worth.

### conv_sanctum (Sanctum, NPC: priest)
- **Aryan:** Three empty cloth-lined stands. Whoever came knew the hall.
- **Pujari Ananda:** Only temple staff know where the key is kept.
- **Aryan:** The lock was forced anyway. They wanted it to look like strangers.
- **Pujari Ananda:** The outer shrine is through that passage. Go carefully.

### conv_shrine (ShrineCrime, NPC: priest)
- **Aryan:** Muddy bootprints, two different sizes, and red silk caught on the door.
- **Pujari Ananda:** That cloth is not ours.
- **Aryan:** And a broken lamp. Someone knocked it over in a hurry.
- **Aryan:** Two suspects: the red scarf and the black hood. Just as Meera said.
  - Choice 1: *Where does this trail lead?*
    - **Pujari Ananda:** Toward the old wall. Few people know it is a door.
  - Choice 2: *Could it be temple staff?*
    - **Aryan:** Someone told them about the key. But these boots are not from here.

### conv_wall (PuzzleWall, NPC: priest)
- **Pujari Ananda:** My grandfather spoke of a wall that opens when four symbols face the rising sun.
- **Old Scroll:** Lotus, conch, wheel, kalash. Then turn the centre wheel.
- **Aryan:** Four symbols in order, then the centre. Let's try.

### conv_wall_open (PuzzleWall, NPC: none)
- **Aryan:** The passage is open. They went this way.

### conv_jungle (JunglePath, NPC: robber1)
- **Bhaskar:** Keep moving. The investigator is close.
- **Kishore:** I told you the lamp would leave marks.
- **Aryan:** It's over. Return what belongs to the community.
- **Aryan:** Two boot sizes, your red silk on the door, and the lamp you broke.
- **Bhaskar:** Do you know what these fetch?
- **Aryan:** Their value isn't money. It's memory.
  - Choice 1: *Put them down gently. No one needs to get hurt.*
    - **Kishore:** ...He's right, Bhaskar. Put them down.
    - **Bhaskar:** Fine. Take them.
  - Choice 2: *The village already knows your faces.*
    - **Bhaskar:** ...Then running is pointless. Here.

### conv_return (Courtyard, NPC: priest)
- **Aryan:** The vessel, the rosette, and the manuscript. All three, unharmed.
- **Pujari Ananda:** You have returned more than objects. You returned our memory.
- **Meera:** The festival can begin at sunrise!

## Objectives (id, instruction, keys, marker target, 3 hints)

1. **obj_talk_pujari** (Courtyard): Speak with Pujari Ananda. Walk to the priest at the temple steps and talk to him. Keys: WASD to walk, E to talk. Marker: `npc:priest` (Temple steps). Done when: dialogue conv_courtyard_pujari.
   - Hint 1 (General): Someone at the temple needs your help.
   - Hint 2 (Specific): The priest in white stands near the temple steps ahead.
   - Hint 3 (Direct): Walk to Pujari Ananda and press E when the prompt appears.
2. **obj_talk_meera** (Courtyard): Ask Meera what she saw. Find Meera in the courtyard and talk to her. Keys: E to talk, 1-3 to choose. Marker: `npc:villager` (Courtyard, beside the priest). Done when: dialogue conv_courtyard_meera.
   - Hint 1 (General): A witness was awake before sunrise.
   - Hint 2 (Specific): Meera wears a red and green saree, near the priest.
   - Hint 3 (Direct): Walk to Meera and press E.
3. **obj_enter_hall** (Courtyard): Enter the storage hall. Go through the temple doorway into the storage hall. Keys: WASD to walk, E at the doorway. Marker: `exit:Sanctum` (Temple doorway). Done when: scene Sanctum.
   - Hint 1 (General): The theft happened inside the temple.
   - Hint 2 (Specific): The doorway is at the top of the temple steps.
   - Hint 3 (Direct): Walk into the glowing doorway and press E.
4. **obj_hall_clues** (Sanctum): Search the storage hall. Find and inspect 3 clues in the hall. Keys: F to inspect, mouse to rotate, Esc to close. Marker: `clues` (Storage hall). Done when: clues clue_broken_lock, clue_breached_case, clue_muddy_footprints.
   - Hint 1 (General): Look closely at the stands, the door and the floor.
   - Hint 2 (Specific): The lock, the empty stands and the floor near the entrance hold clues.
   - Hint 3 (Direct): Walk to each glowing item and press F to inspect it.
5. **obj_hall_pujari** (Sanctum): Tell the Pujari what you found. Talk to Pujari Ananda about the clues. Keys: E to talk. Marker: `npc:priest` (Storage hall). Done when: dialogue conv_sanctum.
   - Hint 1 (General): The priest knows the hall best.
   - Hint 2 (Specific): Pujari Ananda is in the hall with you.
   - Hint 3 (Direct): Walk to the priest and press E.
6. **obj_go_shrine** (Sanctum): Go to the outer shrine. Follow the passage to the outer shrine. Keys: WASD to walk, E at the exit. Marker: `exit:ShrineCrime` (Back passage). Done when: scene ShrineCrime.
   - Hint 1 (General): The thieves left another way.
   - Hint 2 (Specific): A passage leads to the outer shrine.
   - Hint 3 (Direct): Walk to the passage marked Outer Shrine and press E.
7. **obj_shrine_clues** (ShrineCrime): Examine the shrine. Inspect the red silk and the escape sketch. Keys: F to inspect. Marker: `clues` (Outer shrine). Done when: clues clue_fabric_fragment, clue_ancient_map.
   - Hint 1 (General): Something was torn on the way out.
   - Hint 2 (Specific): Check the doorframe and the ground near the lamp.
   - Hint 3 (Direct): Walk to each glowing item and press F.
8. **obj_shrine_pujari** (ShrineCrime): Work out who did it. Talk to the Pujari and put the clues together. Keys: E to talk. Marker: `npc:priest` (Outer shrine). Done when: dialogue conv_shrine.
   - Hint 1 (General): You have enough clues now.
   - Hint 2 (Specific): Discuss them with Pujari Ananda.
   - Hint 3 (Direct): Walk to the priest and press E.
9. **obj_go_wall** (ShrineCrime): Follow the trail to the old wall. Take the exit toward the ancient wall. Keys: WASD to walk, E at the exit. Marker: `exit:PuzzleWall` (Shrine exit). Done when: scene PuzzleWall.
   - Hint 1 (General): The trail continues.
   - Hint 2 (Specific): The exit to the ancient wall is past the shrine.
   - Hint 3 (Direct): Walk to the exit marked Surya Chakra Wall and press E.
10. **obj_wall_pujari** (PuzzleWall): Hear the legend of the wall. Talk to Pujari Ananda about the wall. Keys: E to talk. Marker: `npc:priest` (Wall chamber). Done when: dialogue conv_wall.
   - Hint 1 (General): The priest remembers an old story.
   - Hint 2 (Specific): Pujari Ananda stands near the wall.
   - Hint 3 (Direct): Walk to the priest and press E.
11. **obj_solve_wall** (PuzzleWall): Open the sun wall. Turn the symbols: lotus, conch, wheel, kalash. Then the centre wheel. Keys: E to use, 1-3 pick ring, W/S turn. Marker: `puzzle` (Centre of the wall). Done when: puzzle chakra_wall.
   - Hint 1 (General): The scroll gave an order.
   - Hint 2 (Specific): Lotus, conch, wheel, kalash must face the sun at the top.
   - Hint 3 (Direct): Use the wall (E), set each ring with 1-3 and W/S in that order, then turn the centre.
12. **obj_go_jungle** (PuzzleWall): Go through the open passage. Walk through the passage into the jungle. Keys: WASD to walk, E at the passage. Marker: `exit:JunglePath` (Open passage). Done when: scene JunglePath.
   - Hint 1 (General): The wall has opened.
   - Hint 2 (Specific): The passage is in the wall.
   - Hint 3 (Direct): Walk into the passage and press E.
13. **obj_confront** (JunglePath): Confront the two men. Approach Bhaskar and Kishore and talk calmly. Keys: E to talk, 1-2 to choose. Marker: `npc:robber1` (Jungle clearing). Done when: dialogue conv_jungle.
   - Hint 1 (General): The thieves are ahead.
   - Hint 2 (Specific): Two men argue in the clearing near the torches.
   - Hint 3 (Direct): Walk to Bhaskar (red scarf) and press E.
14. **obj_recover** (JunglePath): Recover the three pieces. Pick up the vessel, the rosette and the manuscript. Keys: F to pick up. Marker: `clues` (Jungle clearing). Done when: clues relic_pradip, relic_rosette, relic_manuscript.
   - Hint 1 (General): They set the pieces down.
   - Hint 2 (Specific): The three pieces lie on the ground near the men.
   - Hint 3 (Direct): Walk to each glowing piece and press F.
15. **obj_return** (Courtyard): Return the pieces to the Pujari. Give the three pieces back to Pujari Ananda at sunrise. Keys: E to talk. Marker: `npc:priest` (Temple steps). Done when: dialogue conv_return.
   - Hint 1 (General): The temple is waiting.
   - Hint 2 (Specific): Pujari Ananda is at the temple steps.
   - Hint 3 (Direct): Walk to the priest and press E.

## Ending

> Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history.

Then **MISSION COMPLETED** with evidence list, score, time and hints used.

Longest line: 82 characters (limit 140).
