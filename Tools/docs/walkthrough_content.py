# Text for Docs/GAME_WALKTHROUGH.pdf. Every caption describes the real screenshot of the same name in Docs/Walkthrough/shots
# (captured by the walkthrough bot on 2026-11-19 from the built game). Keep it consistent with Docs/GAME_EXPLAINED.md.
COVER = {
    "title": "Legacy of the Temple",
    "subtitle": "Complete Walkthrough",
    "meta": ["A third-person heritage mystery made in Unity 6 (URP, C#)",
             "Project by Hemanth  -  Game Development project, GDIT",
             "Every picture is a real screenshot from the built game (development build, 19 November 2026)",
             "Date: 19 November 2026"],
    "image": "L0_03_loading_60.png",
}
TOC_NOTE = "Screenshots were taken by the walkthrough bot playing the real game with real key presses (see Docs/Walkthrough/bot_notes.md). Captions say what you see and what to do."

SECTIONS = []
def S(**k): SECTIONS.append(k)

# ---------------------------------------------------------------- page 2: about, story, controls, HUD
S(id="about", title="1. About the game",
  blocks=[
    ("h", "What the game is"),
    ("ol", ["A short **third-person mystery and action game**, about 20 to 30 minutes long, made in Unity 6 with URP and C#.",
            "**Setting:** Suryapur, a fictional Indian temple village, on the night before the temple festival.",
            "**Goal:** five fictional temple treasures were stolen. You are **Aryan**, an investigator. Bring all five back before dawn (05:45 on the Dawn Clock).",
            "**How you play:** physical puzzles (carry, fit, turn, push, place), a little stealth, a short fight in every place, and a jeep chase.",
            "**The twist:** someone opened the storeroom from the inside. Four small pieces of evidence point to **Madhav**, the friendly key-keeper."]),
    ("h", "The story in five lines"),
    ("ol", ["The night before the festival, five treasures vanish from the temple storeroom.",
            "Pujari Ananda asks Aryan to bring them back before the procession leaves at dawn.",
            "Aryan follows the robbers through the bazaar, the storeroom, the vaults, the stepwell and the Sun Wall, taking back one treasure at each.",
            "On the river road he chases the leader, Bhaskar, to the ruins and wins the last treasure in the final fight.",
            "Madhav gives himself up (\"I only opened a door\"), and Aryan returns the treasures to the priest."]),
    ("h", "Controls"),
    ("table", [["Action", "Key", "Notes"],
               ["Move", "W A S D", "Moves relative to the camera"], ["Look", "Mouse", ""],
               ["Run", "Shift (hold)", "Caps Lock toggles auto-run"], ["Crouch", "C", "Harder to see (stealth)"],
               ["Interact", "E", "Grab, carry, place, turn, push, pull, lift, talk, get in and out of the jeep"],
               ["Inspect", "(no separate key)", "In this version E does everything; there is no inspect key"],
               ["Journal", "J", "Goals, clues, people, pieces"], ["Evidence board", "B", "Name the inside man (see Known issues)"],
               ["Map", "M", "1-8 fast travel to places already reached (costs 2 clock minutes)"],
               ["Help", "H", "Guide for the current goal, three hints for the puzzle you stand at"],
               ["Pause", "Esc", "Settings, controls, save and quit"],
               ["Fight", "Left click, R, Q, V, F", "Strike, heavy, block (tap to parry), dodge, finish"]]),
  ])
S(id="hud", steps=False, title="2. The screen (HUD), journal, map and guide",
  blocks=[
    ("p", "These are the panels you will use all game. The gold numbers on the first picture are labels added in this document."),
    ("shots", [{"file": "L0_05_hud_goal.png", "labels": [(50, 11, "1 Dawn Clock"), (92, 89, "2 Guide (H)"), (50, 76, "3 Tip box"), (50, 35, "4 Gate to the temple")], "caption": "The game screen. **1 Dawn Clock** (top centre): story time and minutes left before dawn. **2 ? GUIDE** (bottom right): help, also on H. **3 Tip box**: a short control tip. **4** The way ahead (here the temple gate). There are no arrows or markers: you find the way by light, sound and people."}], 1),
    ("pair", [{"file": "L0_06_journal.png", "caption": "**Journal (J):** the current goal, clues, people and treasures, in four tabs (1-4)."},
              {"file": "L0_07_map.png", "caption": "**Map (M):** the eight places. Grey means not reached yet; reached places can be travelled to with 1-8."}], 2),
    ("pair", [{"file": "L0_08_guide.png", "caption": "**Guide (H):** what to do, where to go, and three hints (general, specific, direct). Hints lower the score."},
              {"file": "L0_09_pause.png", "caption": "**Pause (Esc):** resume, controls, guide, text size, volume, difficulty, graphics, demo mode, save and quit."}], 4),
    ("note", "Note: in the guide picture a second panel (\"What I know\") is still open at the bottom. It was opened by B and did not close: a real bug, listed in Known issues."),
  ])

# ---------------------------------------------------------------- start: menu, loading, intro
S(id="start", title="3. Start: menu, loading and intro",
  objective="Start a new game and watch Aryan arrive at the village.",
  blocks=[
    ("pair", [{"file": "L0_01_menu.png", "caption": "**Main menu.** Choose **NEW GAME** with the mouse. CONTINUE loads the last save. (The broken-looking shapes behind the menu are a real graphics bug: see Known issues.)"},
              {"file": "L0_02_controls.png", "caption": "**Controls** screen from the menu: every key in one place. BACK returns."}], 1),
    ("pair", [{"file": "L0_03_loading_30.png", "caption": "**Loading screen.** The gold bar follows the real loading progress; a tip is shown under it."},
              {"file": "L0_04_loading_100.png", "caption": "The bar at 100%. The game then starts the intro by itself."}], 3),
    ("shots", [{"file": "L0_10_intro_jeep.png", "caption": "**Intro (a cutscene):** Aryan drives his jeep into Suryapur village at first light. Nothing to press; hold **Esc** to skip it."}], 5),
    ("pair", [{"file": "L0_11_intro_side.png", "caption": "The jeep rolls along the village road and stops at the tea stall."},
              {"file": "L0_12_intro_meera.png", "caption": "Meera gives directions: \"Go straight along this road and follow the old stone entrance. The temple is at the end.\""}], 6),
  ])

# ---------------------------------------------------------------- level 1: reach the temple, meet the priest
S(id="l1", title="Level 1: Reach the temple and find the priest",
  objective="Walk up the village road, go through the old stone gate, and talk to Pujari Ananda. (Story steps 1 and 2 of 9.)",
  blocks=[
    ("pair", [{"file": "L1_01_road_to_gate.png", "caption": "After the intro you stand on the village road. The gate marked **TEMPLE COURTYARD** is straight ahead. Walk with **W A S D**, run with **Shift**. A tip box teaches the mouse look."}, {"file": "L1_02_priest_talk.png", "caption": "At the temple steps, press **E** near **Pujari Ananda**: \"Five treasures. Gone before sunrise.\""}, {"file": "L1_03_goal_card.png", "caption": "A **goal card** (top left) shows the next step: \"The bell in the bazaar. Listen for a bell. Follow, unseen. Step 3 of 9\"."}], 1),
    ("h", "How to complete it"),
    ("ol", ["Walk (or run) up the road and through the gate. The temple tower appears.", "Find the priest at the temple steps and press **E** to talk.", "Read the goal card; the bazaar is next."]),
    ("box", "**Teaches:** walking, running, looking, talking with E. **Time:** about 1 minute. **Why it is here:** it gives the goal (five treasures), the deadline (dawn) and the first view of the temple."),
  ])

# ---------------------------------------------------------------- level 2: bazaar
S(id="l2", title="Level 2: The bell in the bazaar (stealth and street fight)",
  objective="Follow the courier with the ringing bag without being seen, then beat him and his helper and take the Dawn Bell.",
  blocks=[
    ("shots", [{"file": "L2_01_courier_sets_off.png", "caption": "Near the tea stall the courier starts walking. Listen for his bell and follow him at a distance. If he stops and looks back, stay far away, behind something, or crouch with **C**."}], 1),
    ("pair", [{"file": "L2_02_the_yard.png", "caption": "He always ends in a walled **yard** (even if he sees you and runs)."},
              {"file": "L2_03_fight_hud.png", "caption": "The **street fight** starts: the courier and his helper. HEALTH and STAMINA bars appear with the fight keys."}], 2),
    ("shots", [{"file": "L2_04_fight.png", "caption": "Fighting: **left click** strikes (three hits make a combo), **R** heavy, **Q** block, **V** dodge, **F** finish a staggered robber. Then take the **Dawn Bell** with **E**; the courier's list is noted as evidence."}], 4),
    ("h", "How to complete it"),
    ("ol", ["Wait near the tea stall until the courier walks off.", "Follow him. When he looks back, be far, hidden or crouching.", "In the yard, beat both robbers.", "Press **E** on the Dawn Bell."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Standing close and in the open when he turns: he spots you. After three times he runs, but you can still follow him to the yard.",
            "Hints (H): \"Go back to the tea stall on the village road and listen.\" / \"A man with a ringing bag walks south. Stay well behind him.\" / \"When he stops and turns, be far away, behind something, or crouching (C).\""]),
    ("box", "**Teaches:** stealth by distance and cover, and the fight controls. **Time:** about 2 minutes (the bot's tail took 75 s). **Why it is here:** treasure 1 (the Dawn Bell) and the first clue to the inside man (a neat handwritten list)."),
    ("note", "Not shown: the moment he spots you. That picture was taken from inside a tree and was left out."),
  ])

# ---------------------------------------------------------------- level 3: storeroom (pot pieces and lantern)
S(id="l3", title="Level 3: The storeroom (broken pot and lantern puzzle)",
  objective="Rebuild the break-in: mend the pot, place the lit lantern on the oil stain, turn the lock plate, lift the flagstone, beat the lookout, take the Palm-Leaf Chronicle.",
  blocks=[
    ("shots", [{"file": "L3_01_storeroom_wide.png", "caption": "The storeroom: a barred door between two pillars, a stone **plinth** on the left, a lamp stand and pot pieces on the ground. A dark **oil stain** runs in front of the door."}], 1),
    ("pair", [{"file": "L3_02_plinth_and_lantern.png", "caption": "The empty **plinth** (the stone block) and the **lantern**, still unlit. The lantern only works once the pot holds oil again."},
              {"file": "L3_03_pot_whole_lantern_lit.png", "caption": "All three **shards** carried to the plinth (**E** to pick up, **E** to place): the pot is whole and the banner says \"THE POT HOLDS OIL AGAIN. The lantern is lit.\""}], 2),
    ("shots", [{"file": "L3_04_lantern_on_stain.png", "caption": "Carry the lit lantern and put it down on the **dark stain by the barred door**. Its light shows the loose flagstone. (The mended pot is on the plinth on the right.) Turn the **lock plate** on the door post twice with **E**: the scratches are on the inside."}], 4),
    ("pair", [{"file": "L3_05_fight_lookout.png", "caption": "Lift the pale **flagstone** (**E**). The **lookout** attacks: beat him."},
              {"file": "L3_06_chronicle_recovered.png", "caption": "Take the **Palm-Leaf Chronicle** (**E**). The banner confirms it; Aryan says \"Two.\""}], 5),
    ("h", "How the pot and lantern puzzle works"),
    ("ul", ["Three pot shards lie in different places. Each shard fits only its own place on the plinth (`PlacementSocket`).",
            "When all three are in, the whole pot appears, it \"holds oil\", and the lantern's light switches on (`BreakInPuzzle.Refresh`).",
            "The lantern counts only when it is put down within about 1 m of the oil stain (`ProximityMatch`). Only then can the flagstone be lifted.",
            "The lock plate turns in 4 stops; at stop 2 it shows the inside scratches (the second clue)."]),
    ("h", "How to complete it"),
    ("ol", ["Pick up each of the 3 shards and place it on the plinth.", "Pick up the lit lantern; put it down on the oil stain by the door.", "Turn the lock plate twice (clue).", "Lift the flagstone, beat the lookout, take the Chronicle."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Trying the flagstone or the lantern first: nothing happens until the pot is whole and the lantern is on the stain.",
            "Hints (H): \"The pot broke here. Its pieces did not stay here.\" / \"A lit lantern on the oil stain shows which stone is loose.\" / \"Fit the three shards on the plinth, stand the lantern on the dark stain by the bars, then lift the pale flagstone in front of it.\""]),
    ("box", "**Teaches:** look at the scene and put things back where they were. **Time:** about 2 to 3 minutes. **Why it is here:** it rebuilds the crime from objects (the robber's lamp stood on the spilled oil) and gives treasure 2 and clue 2 (opened from inside)."),
    ("note", "Not shown: the lock plate close-up. That picture was blocked by Aryan's hat and was left out."),
  ])

# ---------------------------------------------------------------- level 4: vaults
S(id="l4", title="Level 4: The vaults (blocks, plates and stone discs)",
  objective="Push two stone blocks onto two floor plates to open the gate, turn three stone discs to match the light on the floor, beat two guards, take the Sun Rosette.",
  blocks=[
    ("shots", [{"file": "L4_01_arrive.png", "caption": "The vault hall is meant to be near dark. Madhav says \"Take my lantern. It is dark below.\" (he is the inside man, but you do not know that yet)."}], 1),
    ("pair", [{"file": "L4_02_blocks_on_plates.png", "caption": "Push the two **stone blocks** (**E** pushes one stop) onto the two **floor plates**. One block must go round the pillar. A lever resets both."},
              {"file": "L4_03_light_pattern.png", "caption": "The roof light draws **three rings** on the floor, each with one bright mark. That is the answer for the discs."}], 2),
    ("pair", [{"file": "L4_04_discs_solved.png", "caption": "Turn the small, middle and large **discs** (**E**) until they match: **2, 5 and 3 turns**. The chest opens; the ledger signed \"M.\" is noted, and two guards attack."},
              {"file": "L4_05_fight_guards.png", "caption": "Beat the **two guards** (a heavy and a brawler). Then take the **Sun Rosette**."}], 4),
    ("h", "How to complete it"),
    ("ol", ["Push block A four stops onto plate 1; push block B two stops round the pillar, then two more onto plate 2. The gate opens.", "Look at the rings of light on the floor.", "Turn the discs: small 2, middle 5, large 3 (6 stops each).", "Beat the guards, take the Sun Rosette."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Pushing a block into a corner: pull the **reset lever**.", "Turning discs at random: the answer is the light pattern you walked past.",
            "Hints (H): \"Two floor plates hold the gate. Stone is heavier than a man.\" / \"The light from the roof draws three rings on the floor. Each ring has one bright mark.\" / the full steps."]),
    ("box", "**Teaches:** plan moves, read the environment. **Time:** about 3 minutes. **Why it is here:** treasure 3 and clue 3 (a ledger signed \"M.\")."),
  ])


# ---------------------------------------------------------------- level 5: stepwell
S(id="l5", title="Level 5: The stepwell (water level puzzle)",
  objective="Fill the stone tank with two sluice wheels, ride the raft, drain the water to the long mark, push the carved panel, beat the knife man and his helper, take the Monsoon Vessel.",
  blocks=[
    ("pair", [{"file": "L5_01_arrive.png", "caption": "Night and rain. A ramp leads up to the rim of the walled stone tank."},
              {"file": "L5_02_tank_empty.png", "caption": "The tank is empty. Two **sluice wheels** on the rim let water in; the raft lies at the bottom."}], 1),
    ("pair", [{"file": "L5_03_tank_full.png", "caption": "Both sluice wheels turned (**E** at each): the tank floods. The carved panel is now **under water** (too much water)."},
              {"file": "L5_04_water_on_mark.png", "caption": "Standing on the **raft**, work the **drain chain** twice (**E**). The water rests on the long mark, where the raft meets the panel."}], 3),
    ("pair", [{"file": "L5_05_chamber.png", "caption": "**Push the carved panel** (**E**). Behind it: the Monsoon Vessel; the banner notes \"A temple key ring on the chamber floor\" (clue 4)."},
              {"file": "L5_06_fight_knife_man.png", "caption": "The **knife man and his helper** attack (2 robbers). Meera: \"Only one man carries those keys.\" Beat them and take the vessel."}], 5),
    ("h", "How to complete it"),
    ("ol", ["Turn the south sluice wheel and the north sluice wheel.", "Step onto the raft.", "Work the drain chain twice; the water settles on the long mark.", "Push the panel, win the fight, take the Monsoon Vessel."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Opening both sluices and waiting: the panel stays under water. Draining is the key step.", "The drain always brings the water back down, so you can never get stuck.",
            "Hints (H): \"The east wall has old water marks. One is longer than the others.\" / \"Too much water hides the carved panel. The raft carries a drain chain.\" / the full steps."]),
    ("box", "**Teaches:** cause and effect; the obvious move (open everything) is not the answer. **Time:** about 2 minutes. **Why it is here:** treasure 4 and clue 4 (the temple key ring)."),
  ])

# ---------------------------------------------------------------- level 6: Sun Wall
S(id="l6", title="Level 6: The Sun Wall (mirrors and the stone wheel)",
  objective="Turn four bronze mirrors so two sunbeams reach two carvings on the wall, push the great stone wheel one stop, beat Kishore, take the empty case.",
  blocks=[
    ("shots", [{"file": "L6_01_plaza.png", "caption": "The midday plaza: the carved wall with a closed door, the great **stone wheel**, and four round **bronze mirrors** on posts."}], 1),
    ("pair", [{"file": "L6_02_mirrors_set.png", "caption": "Each mirror turned **two stops** (**E**, twice each). The beams bend toward the wall, but one is still blocked by a **spoke** of the wheel."},
              {"file": "L6_03_beams_reach_wall.png", "caption": "**Push the wheel one stop** (**E**). Both beams (thin orange lines) reach the carvings and the door opens. The fight banner shows **Kishore**."}], 2),
    ("pair", [{"file": "L6_04_door_open.png", "caption": "The hidden door in the wall is open. Behind it is the finial's **empty case** and a boat receipt."},
              {"file": "L6_05_fight_kishore.png", "caption": "**Kishore**, a mini boss with two phases. When he is down, take the case (**E**)."}], 4),
    ("h", "How to complete it"),
    ("ol", ["Turn each of the four mirrors two stops.", "Push the stone wheel one stop so no spoke blocks the beam.", "Beat Kishore.", "Take the empty case: the last treasure is on the river road."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Turning mirrors too far: each has 16 stops; keep turning and it comes round again.", "Forgetting the wheel: one beam can never reach the wall while a spoke is in the way.",
            "Hints (H): \"Light has to reach two carvings on the wall.\" / \"One beam must pass between the spokes of the great wheel.\" / the full steps."]),
    ("box", "**Teaches:** angles and blocking (a simple light puzzle). **Time:** about 2 minutes. **Why it is here:** it reveals that the fifth treasure is already moving to the river (the empty case)."),
    ("note", "Left out: a second Kishore fight picture, which was blocked by a wooden post."),
  ])

# ---------------------------------------------------------------- reference pages
S(id="map", steps=False, title="Map overview",
  blocks=[
    ("p", "The whole game is one connected world around the temple. The plan below was **drawn from the real positions the game logs** when it builds each place (it is a diagram, not a screenshot). Straight-line distances from the arrival point: storeroom 133 m, vaults 262 m, stepwell 244 m, Sun Wall 244 m, river road 282 m, ruin court 508 m."),
    ("shots", [{"file": "map_plan_from_coordinates.png", "caption": "Top-down plan, north up, 50 m grid. Numbers are the order you visit the places."}], 1),
    ("note", "In the game, press **M** for the map list (Level 1 picture of the screen) and **1-8** to fast travel to any place the story has reached (costs 2 minutes on the Dawn Clock)."),
  ])
S(id="chars", steps=False, title="Characters",
  blocks=[
    ("pair", [{"file": "C_01_aryan.png", "caption": "**Aryan**, the investigator (the player), next to his jeep."},
              {"file": "C_02_pujari.png", "caption": "**Pujari Ananda**, the temple priest. He gives the goal and receives the treasures at the end."}], 1),
    ("pair", [{"file": "C_03_meera.png", "caption": "**Meera**, a village girl in a saree. She gives directions and notices what others miss."},
              {"file": "C_04_madhav.png", "caption": "**Madhav**, the key-keeper and the inside man, seen near the storeroom. His whole model is tinted indigo (the skin too) and his shawl is a floating box: real bugs, see Known issues."}], 3),
    ("pair", [{"file": "C_05_robbers.png", "caption": "**Robbers** (the courier and helpers), recoloured and resized versions of the two robber models."},
              {"file": "C_06_kishore.png", "caption": "**Kishore** (mini boss at the Sun Wall) and, at the ruins, **Bhaskar**, the leader."}], 5),
  ])
S(id="jeep", steps=False, title="The jeep",
  blocks=[
    ("shots", [{"file": "J_01_jeep.png", "caption": "Aryan's olive jeep, a Blender model made for this game (52,778 triangles, 3 levels of detail, 3 damage states)."}], 1),
    ("table", [["Key", "Action"], ["E", "Get in or out (near the driver's door)"], ["W / S", "Drive / reverse"], ["A / D", "Steer"], ["Space", "Brake"], ["L", "Lights"], ["G", "Horn"]]),
    ("p", "The jeep is used in the intro (it drives itself) and in the river-road chase. It has real wheel physics (WheelColliders), lights, a horn, a visible driver and damage by impact strength."),
  ])
S(id="combat", steps=False, title="Combat controls",
  blocks=[
    ("shots", [{"file": "K_01_fight_hud.png", "caption": "The fight HUD: HEALTH and STAMINA bars, the combo counter and the fight keys. Heavy strikes and dodges use stamina."}], 1),
    ("table", [["Key", "Move", "Use it when"], ["Left click", "Strike", "Three in a row make a combo"], ["R", "Heavy strike", "A robber is guarding (after three hits)"],
               ["Q (hold)", "Block", "A blow is coming; tap just before it lands to parry"], ["V", "Dodge roll", "A heavy or unblockable blow is coming"], ["F", "Finisher", "A robber is staggered (slow motion)"]]),
    ("ul", ["At most **two robbers** attack at the same time; the others circle.", "Robbers **telegraph** blows (a wind-up) before they strike.", "If you lose, the fight **restarts on the spot**. There is no game over."]),
  ])

# ---------------------------------------------------------------- level 7: river road and ruins
S(id="l7", title="Level 7: The river road (jeep chase and final fight)",
  objective="Chase Bhaskar in the jeep to the ruins, survive two waves, beat Bhaskar and take the Chariot Finial.",
  blocks=[
    ("pair", [{"file": "L7_01_jungle_road.png", "caption": "The jungle road at dusk. The jeep waits at the road start; Aryan: \"An empty case. The jungle road.\""},
              {"file": "L7_02_chase.png", "caption": "**Jeep chase:** get in with **E**, drive with **W**, steer with **A D**, brake with **Space**. If you fall far behind, Bhaskar slows down: you cannot lose."}], 1),
    ("pair", [{"file": "L7_03_ruin_court.png", "caption": "At the **ruin court**, get out of the jeep and walk in. Bhaskar: \"Do you know what this fetches?\" Wave 1: **3 robbers**."},
              {"file": "L7_04_wave1.png", "caption": "Wave 1: two brawlers and a knife man. Only two attack at once."}], 3),
    ("pair", [{"file": "L7_05_wave2.png", "caption": "Wave 2: **a heavy** (one robber, because Madhav was named correctly on the evidence board; otherwise two more join)."},
              {"file": "L7_06_bhaskar.png", "caption": "**Bhaskar**, the leader and final boss. Block (**Q**), dodge (**V**), and finish him (**F**) when he staggers."}], 5),
    ("shots", [{"file": "L7_07_madhav_gives_up.png", "caption": "Bhaskar is down. Madhav then gives himself up (\"I only opened a door\"; he is not in this picture). Take the **Chariot Finial** with **E**."}], 7),
    ("h", "How to complete it"),
    ("ol", ["Walk to the jeep at the road start and press **E**.", "Follow Bhaskar along the road to the ruins.", "Get out (**E**) and walk into the court.", "Win wave 1, wave 2 and the Bhaskar fight.", "Take the Chariot Finial."]),
    ("h", "Common mistakes and hints"),
    ("ul", ["Staying in the jeep at the court: the fight starts only on foot.", "Fighting three at once in the open: fight with your back to a wall.",
            "Hints (H): \"The road north of the temple leads to the river.\" / \"Your jeep waits at the start of that road.\" / \"Drive after Bhaskar to the ruins, then get out and fight.\""]),
    ("box", "**Teaches:** using the jeep and everything learned in fights. **Time:** about 3 to 4 minutes (the bot's chase took about 30 s). **Why it is here:** the climax: treasure 5 and the end of the robbers' plan."),
  ])

# ---------------------------------------------------------------- ending
S(id="ending", title="Ending and score",
  objective="Bring the five treasures back to the priest.",
  blocks=[
    ("pair", [{"file": "E_01_return.png", "caption": "Back at the temple the banner shows **RECOVERED: the Chariot Finial**. Talk to Pujari Ananda (**E**): \"You brought our memory home.\""},
              {"file": "E_02_ending.png", "caption": "**MISSION COMPLETED.** In this run: ending **Dawn Light**, 5 of 5 treasures, all 4 clues, Madhav named (right), grades S, S, S, S, S and A, clock 05:21, play time 8:15. Scroll with the mouse wheel for the total score."}], 1),
    ("ul", ["**Dawn Light:** finished before 05:45. **Late Dawn:** finished after. **Lost Dawn:** the boss escaped.",
            "Each place is graded **S, A, B or C** (puzzle without hints, staying unseen, speed, little damage, treasure, combos).",
            "The run is saved in the **best-runs table** (shown in Credits). Press **Enter** to return to the main menu."]),
  ])

# ---------------------------------------------------------------- cheat sheet
S(id="cheat", title="Quick cheat sheet (all solutions)",
  blocks=[
    ("table", [["Place", "Solution", "Fight"],
               ["1 Temple", "Walk through the gate; talk to the Pujari (E)", "none"],
               ["2 Bazaar", "Follow the courier; when he looks back be far, hidden or crouching (C)", "Courier + helper"],
               ["3 Storeroom", "3 shards on the plinth -> lantern lights -> lantern on the oil stain -> lock plate x2 -> lift flagstone", "The lookout"],
               ["4 Vaults", "Block A 4 pushes, block B 2 round the pillar + 2 -> gate; discs small 2, middle 5, large 3", "Two guards"],
               ["5 Stepwell", "Both sluice wheels -> step on the raft -> drain chain x2 -> push the panel", "Knife man + helper"],
               ["6 Sun Wall", "Each of 4 mirrors 2 stops -> push the wheel 1 stop", "Kishore (2 phases)"],
               ["7 River road", "Jeep chase (cannot be lost) -> get out at the court", "Wave 1 (3), wave 2 (1 or 3), Bhaskar"],
               ["Evidence board", "Name **Madhav** (needs 3 of 4 clues) for a shorter wave 2", "-"],
               ["Ending", "Talk to the Pujari at the temple", "-"]]),
    ("ul", ["**Stuck?** Press **H** for three hints (they lower the score).", "**Lost?** Press **M**: the map lists the places; 1-8 travels there.",
            "**Fights:** left click, R heavy, Q block, V dodge, F finish. You retry on the spot if you lose."]),
  ])

# reading order: levels first, then the reference pages
ORDER = ["about", "hud", "start", "l1", "l2", "l3", "l4", "l5", "l6", "l7", "ending", "map", "chars", "jeep", "combat", "cheat"]
SECTIONS.sort(key=lambda s: ORDER.index(s["id"]))
