#!/usr/bin/env python3
"""Builds Assets/_Project/Resources/Story/story.json (v2) from the script below.
   python3 build_story.py          -> story.json
   python3 build_story.py --tts    -> also placeholder voices Resources/Voice/<speaker>/<line_id>_tts.wav (macOS say)
   Recorded voices: drop <line_id>.wav next to them; they win automatically."""
import json, os, subprocess, sys
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Story/story.json")
SPK = {"ary": "aryan", "puj": "pujari", "mee": "meera", "bha": "bhaskar", "kis": "kishore", "scr": "scroll"}
ALL = {}  # id -> (speaker, tier, text)

def L(id, tier, t, when=None):
    s = SPK[id.split("_")[1]]
    assert len(t) <= 120, (id, len(t))
    assert id not in ALL, id
    ALL[id] = (s, tier, t)
    d = {"id": id, "s": s, "tier": tier, "t": t}
    if when: d["when"] = when
    return d

def C(id, text, effects, lines, next=None, when=None):
    d = {"id": id, "text": text, "effects": effects, "lines": lines}
    if next: d["next"] = next
    if when: d["when"] = when
    return d

speakers = [
 {"id": "aryan", "name": "Aryan", "role": "Investigator", "color": "#FFC857", "npc": "investigator"},
 {"id": "pujari", "name": "Pujari Ananda", "role": "Temple Priest", "color": "#FFC857", "npc": "priest"},
 {"id": "meera", "name": "Meera", "role": "Village Girl", "color": "#7FE0A0", "npc": "villager"},
 {"id": "bhaskar", "name": "Bhaskar", "role": "Red Scarf", "color": "#FFB4A8", "npc": "robber1"},
 {"id": "kishore", "name": "Kishore", "role": "Porter No. 17", "color": "#FFB4A8", "npc": "robber2"},
 {"id": "scroll", "name": "Old Scroll", "role": "Temple Record", "color": "#5CE1E6", "npc": ""}]

chapters = [
 {"id": "ch1", "n": 1, "title": "3:10 AM", "scene": "Courtyard", "budget": 25, "image": "loading_01_dawn_courtyard", "weather": 0.25},
 {"id": "ch2", "n": 2, "title": "THE EMPTY STANDS", "scene": "Sanctum", "budget": 30, "image": "loading_04_shrine", "weather": 0.4},
 {"id": "ch3", "n": 3, "title": "TRACKS IN THE DARK", "scene": "ShrineCrime", "budget": 25, "image": "loading_02_gate_night", "weather": 0.8},
 {"id": "ch4", "n": 4, "title": "THE WALL THAT REMEMBERS", "scene": "PuzzleWall", "budget": 35, "image": "loading_02_gate_night", "weather": 0.3},
 {"id": "ch5", "n": 5, "title": "BEFORE FIRST LIGHT", "scene": "JunglePath", "budget": 40, "image": "loading_03_jungle_path", "weather": 1.0},
 {"id": "ch6", "n": 6, "title": "FIRST LIGHT", "scene": "Courtyard", "budget": 0, "image": "loading_01_dawn_courtyard", "weather": 0.0}]

conversations = [
 {"id": "conv_c0", "scene": "", "npc": "", "lines": [
   L("c0_kis_01", "calm", "We take only what we were told, Bhaskar. Nothing more."),
   L("c0_bha_01", "calm", "Quiet. The wall opens at first light, and we will not be late."),
   L("c0_kis_02", "calm", "These stands have never been empty. Not once in my life."),
   L("c0_bha_02", "firm", "Then tonight you learn something new. Move.")]},
 {"id": "conv_c1_pujari", "scene": "Courtyard", "npc": "priest", "lines": [
   L("c1_puj_01", "calm", "Aryan-ji, they came before the lamps burned low. Three pieces are gone."),
   L("c1_puj_02", "calm", "The Surya Pradeepa, the Padma Shila, and the Tala Patra. All three."),
   L("c1_ary_01", "calm", "Tell me what only you would notice."),
   L("c1_puj_03", "calm", "The key was in its place. The lock was not forced from outside."),
   L("c1_ary_02", "calm", "Then someone gave them the way in."),
   L("c1_puj_04", "firm", "The dawn rite is at 5:45. Without them, I have nothing to light and nothing to read."),
   L("c1_ary_03", "calm", "Then we have until sunrise. Who was awake?"),
   L("c1_puj_05", "calm", "Meera. She draws water before anyone else is up.")]},
 {"id": "conv_c1_meera", "scene": "Courtyard", "npc": "villager", "lines": [
   L("c1_mee_01", "firm", "I saw two men. A red scarf and a black hood."),
   L("c1_ary_04", "calm", "You looked longer at one of them."),
   L("c1_mee_02", "firm", "It was dark.")],
  "prompt": "PRESS THE POINT: which of her answers does not fit?",
  "choices": [
   C("C1-T-1", "\"I saw two men.\"", "clock-1", [L("c1_mee_06", "firm", "I saw what I saw. Ask the Pujari if you doubt me.")], next="conv_c1_meera_b"),
   C("C1-T-2", "\"A red scarf and a black hood.\"", "clock-1", [L("c1_mee_07", "firm", "You asked what I saw. That is what I saw.")], next="conv_c1_meera_b"),
   C("C1-T-3", "\"It was dark.\"", "tell_found=1", [L("c1_ary_12", "calm", "Dark. Yet you counted a scarf and a hood.")], next="conv_c1_meera_b")]},
 {"id": "conv_c1_meera_b", "scene": "Courtyard", "npc": "villager", "lines": [
   L("c1_ary_13", "calm", "Meera. One of those men, you knew.")],
  "prompt": "HOW DO YOU HANDLE HER?",
  "choices": [
   C("C1-A-1", "Gentle: \"You are safe telling me.\"", "trust_meera+1;chase_hint=1", [
     L("c1_ary_05", "calm", "Whatever you saw, you are safe telling me. When you are ready."),
     L("c1_mee_03", "calm", "...They went toward the east wall. That is all.")]),
   C("C1-A-2", "Hard: \"I do not have time for a lie.\"", "trust_meera-1", [
     L("c1_ary_06", "firm", "Dark enough to hide a lie. I do not have time for one."),
     L("c1_mee_04", "firm", "Then stop wasting it on me.")]),
   C("C1-A-3", "Say nothing. Write it down.", "chase_hint=1", [
     L("c1_ary_07", "calm", "Mm."),
     L("c1_mee_05", "calm", "What did you write?")])]},
 {"id": "conv_c2_token", "scene": "Sanctum", "npc": "priest", "lines": [
   L("c2_puj_02", "firm", "Number seventeen. Kishore. Our porter until last monsoon."),
   L("c2_puj_03", "calm", "I dismissed him myself. I thought it was the kinder thing."),
   L("c2_mee_01", "firm", "Don't hurt him. He is my brother."),
   L("c2_ary_06", "calm", "You knew his voice in the dark."),
   L("c2_mee_02", "calm", "I knew it before I saw the scarf. I have been paying his debt since the rains.")],
  "prompt": "MEERA LIED TO YOU. WHAT NOW?",
  "choices": [
   C("C2-A-1", "Trust her: \"Help me reach him.\"", "trust_meera+1;porter_path=1", [
     L("c2_ary_07", "calm", "Then help me reach him before Bhaskar does something worse."),
     L("c2_mee_03", "calm", "The porters have a path. I will mark it in chalk.")]),
   C("C2-A-2", "Reprimand: \"You cost me an hour.\"", "trust_meera-1;meera_out=1", [
     L("c2_ary_08", "firm", "You cost me an hour I did not have. Stay with the Pujari."),
     L("c2_mee_04", "firm", "And you will cost me a brother.")])]},
 {"id": "conv_c3", "scene": "ShrineCrime", "npc": "priest", "lines": [
   L("c3_ary_05", "calm", "\"Ford at moonset. Sun Wall at first light.\""),
   L("c3_ary_06", "firm", "They are not selling. They are opening something."),
   L("c3_puj_01", "firm", "The Sun Wall. It needs all three."),
   L("c3_puj_02", "calm", "Behind it lie our records. Every family that ever served this temple."),
   L("c3_ary_07", "calm", "And things a buyer would pay a great deal for.")],
  "prompt": "MEERA HAS FOLLOWED YOU HERE.",
  "choices": [
   C("C3-A-1", "Carry her message to Kishore", "trust_meera+1;has_message=1", [
     L("c3_mee_01", "calm", "Tell him the debt is paid. I sold the bangles. He owes Bhaskar nothing."),
     L("c3_ary_09", "calm", "I will tell him. In your words.")], when="meera_out==0"),
   C("C3-A-2", "Send her home", "", [
     L("c3_ary_10", "firm", "This stopped being a family matter at 3:10. Go home, Meera."),
     L("c3_mee_02", "calm", "It was never anything else.")], when="meera_out==0")]},
 {"id": "conv_c4", "scene": "PuzzleWall", "npc": "priest", "lines": [
   L("c4_puj_01", "calm", "It is closed again. They tried, and the wall refused them."),
   L("c4_scr_01", "calm", "Lotus for the waking. Conch for the call. Wheel for the road. Kalash for the return."),
   L("c4_scr_02", "calm", "Then the heart of the wall, and the sun will do the rest."),
   L("c4_ary_01", "calm", "The centre socket is empty. The Padma Shila is the key, and Bhaskar has it."),
   L("c4_puj_02", "calm", "My grandfather spoke of a hand-wheel, for the years the stone was away for repair."),
   L("c4_puj_03", "firm", "Turn the four, and the wheel will turn by hand.")]},
 {"id": "conv_c4_open", "scene": "PuzzleWall", "npc": "", "lines": [
   L("c4_ary_03", "calm", "The passage is open. Boot prints, two sizes, heading into the trees."),
   L("c4_ary_04", "firm", "Dawn in forty minutes."),
   L("c4_puj_05", "calm", "Bring them home, Aryan-ji. And bring that foolish boy home too.")]},
 {"id": "conv_c5_confront", "scene": "JunglePath", "npc": "", "lines": [
   L("c5_bha_05", "firm", "The investigator. Persistent. I respect that. It will not save you."),
   L("c5_ary_01", "firm", "Put the satchel down, Bhaskar. This ends quietly or it ends badly."),
   L("c5_bha_06", "calm", "You will starve with a clean conscience.")]},
 {"id": "conv_c5_after", "scene": "JunglePath", "npc": "", "lines": [
   L("c5_kis_09", "calm", "Take it. Tell Meera I am sorry."),
   L("c5_ary_08", "calm", "Tell her yourself."),
   L("c5_bha_10", "calm", "There will be another night, investigator. There is always a buyer."),
   L("c5_ary_09", "calm", "And there will always be someone who says no.")]},
 {"id": "conv_c6", "scene": "Courtyard", "npc": "priest", "lines": [
   L("c6_puj_01", "calm", "You kept your word, Aryan-ji. All three, and before the sun.", when="late==0"),
   L("c6_puj_02", "calm", "The sun is up without us. We will light it late, but we will light it.", when="late==1"),
   L("c6_mee_01", "calm", "You came back. I did not think you would come back."),
   L("c6_kis_10", "calm", "I carried it out. Let me be the one to carry it in."),
   L("c6_ary_01", "calm", "He will face the law, and your word will matter."),
   L("c6_ary_02", "calm", "So will mine. I saw what he did when it counted.", when="kishore_turned==1"),
   L("c6_puj_03", "calm", "The flame goes from house to house. It always has. It will today.")]},
]

def B(id, tier, t, trigger=""):
    d = L(id, tier, t); d["trigger"] = trigger; return d
barks = [
 B("c1_ary_08", "firm", "There. On the wall. Stay here, both of you.", "start:obj_c1_chase"),
 B("c1_ary_09", "shout", "Stop! I only want to talk!", "chase_mid"),
 B("c1_ary_10", "calm", "A porter's token. Tin. Number seventeen.", "clue:clue_token17"),
 B("c1_ary_11", "calm", "He ran like a man who knows every stone here.", "done:obj_c1_token"),
 B("c2_ary_01", "calm", "Every lamp is out. Somebody wanted this room blind.", "start:obj_c2_search"),
 B("c2_puj_01", "calm", "The lamps are lit in an order, Aryan-ji. East first, as the sun does.", "start:obj_c2_lamps"),
 B("c2_ary_02", "calm", "A key cut from wax. Fresh filings on the floor.", "clue:clue_broken_lock"),
 B("c2_ary_03", "calm", "An oily handprint, low on the stand. He knelt. He was careful.", "clue:clue_breached_case"),
 B("c2_ary_04", "calm", "A chalk mark on the third pillar. A sign for someone who came later.", "clue:clue_muddy_footprints"),
 B("c2_ary_05", "firm", "They did not break the lock. They had it opened.", "done:obj_c2_search"),
 B("c2_ary_09", "calm", "Kishore opened the door. Bhaskar brought the reason.", "done:obj_c2_deduce"),
 B("c3_ary_01", "calm", "Bells on a thread, ankle height. They expected to be followed.", "start:obj_c3_traps"),
 B("c3_ary_02", "calm", "Two boot sizes. One print pressed deep. He is carrying weight.", "forensic_boots"),
 B("c3_ary_03", "calm", "Red silk on the doorframe. He was in a hurry, or he did not care.", "clue:clue_fabric_fragment"),
 B("c3_ary_04", "calm", "A broken lamp. Still warm at the wick.", "forensic_lamp"),
 B("c3_ary_08", "firm", "That was heard. Move.", "bell_rung"),
 B("c4_ary_02", "firm", "It resets. Ninety seconds, then it forgets everything I have done.", "wall_reset"),
 B("c4_puj_04", "firm", "Wrong, Aryan-ji! The dust is falling. Begin again.", "wall_wrong"),
 B("c5_bha_01", "calm", "Moonset at the ford. Yes. All three.", "overhear_1"),
 B("c5_bha_02", "calm", "No. The wall first. What is behind it is worth ten of these.", "overhear_2"),
 B("c5_kis_01", "calm", "Bhaskar, the manuscript is the village's name.", "overhear_3"),
 B("c5_bha_03", "calm", "Names do not pay debts.", "overhear_4"),
 B("c5_kis_02", "calm", "You said nobody would be hurt. You said one night.", "overhear_5"),
 B("c5_bha_04", "firm", "I said what a frightened man needed to hear. Walk.", "overhear_6"),
 B("c5_ary_02", "shout", "Kishore! Meera sold the bangles. The debt is paid. You owe him nothing!", "message_yes"),
 B("c5_kis_03", "calm", "She... she sold them?", "message_yes_reply"),
 B("c5_ary_03", "shout", "Kishore! Walk away now or you go down with him!", "message_no"),
 B("c5_kis_04", "calm", "I cannot go back. Not now.", "message_no_reply"),
 B("c5_ary_04", "shout", "Enough, Bhaskar!", "phase1"),
 B("c5_ary_05", "shout", "It is not yours to sell!", "phase2"),
 B("c5_bha_07", "firm", "Everything is for sale. You only have to find the hour.", "phase2_reply"),
 B("c5_ary_06", "shout", "You cannot carry what it means!", "phase3"),
 B("c5_bha_08", "shout", "Then I will carry what it weighs!", "phase3_reply"),
 B("c5_kis_05", "shout", "Bhaskar, stop. I cannot.", "turn_1"),
 B("c5_kis_06", "shout", "Aryan-ji! Catch!", "turn_2"),
 B("c5_bha_09", "firm", "Porter. You were always only a porter.", "turn_3"),
 B("c5_kis_07", "firm", "Forgive me. I have nowhere else to stand.", "two_on_one"),
 B("c5_ary_07", "firm", "Then stand down, before I have to make you.", "two_on_one_reply"),
 B("c5_kis_08", "calm", "Enough... enough. I am done.", "kishore_down"),
 B("bk_ary_01", "shout", "Drop the blade!", "counter"),
 B("bk_ary_02", "shout", "Behind you!", "warn"),
 B("bk_ary_03", "shout", "Not today!", "dodge"),
 B("bk_ary_04", "shout", "Stay down!", "stagger"),
 B("bk_bha_01", "firm", "You are too late!", "boss_phase"),
 B("bk_bha_02", "firm", "Come closer!", "boss_taunt"),
 B("bk_bha_03", "firm", "One more step!", "boss_lunge"),
 B("bk_kis_01", "firm", "I do not want this!", "kishore_attack"),
]

def O(id, ch, scene, title, instruction, keys, target, area, complete, hints, start=None, when=None):
    d = {"id": id, "chapter": ch, "scene": scene, "title": title, "instruction": instruction, "keys": keys,
         "target": target, "area": area, "complete": complete, "hints": hints}
    if start: d["start"] = start
    if when: d["when"] = when
    return d
dlg = lambda i: {"type": "dialogue", "id": i}
mg = lambda i: {"type": "minigame", "id": i}
sc = lambda i: {"type": "scene", "id": i}
cl = lambda *i: {"type": "clues", "ids": list(i)}

objectives = [
 O("obj_c1_pujari", "ch1", "Courtyard", "Speak with Pujari Ananda", "Walk to the priest at the temple steps and talk to him.", "WASD walk, E talk", "npc:priest", "Temple steps", dlg("conv_c1_pujari"),
   ["Someone at the temple is waiting for you.", "The priest stands near the temple steps ahead.", "Walk to Pujari Ananda and press E."]),
 O("obj_c1_meera", "ch1", "Courtyard", "Question Meera", "Talk to Meera. Listen for the answer that does not fit, then decide how to handle her.", "E talk, 1-3 choose", "npc:villager", "Near the well", dlg("conv_c1_meera_b"),
   ["Meera was awake before anyone.", "She stands to the right of the temple steps.", "Press E beside Meera. \"It was dark\" is the answer that does not fit."], start="conv_c1_meera"),
 O("obj_c1_chase", "ch1", "Courtyard", "Catch the runner", "A hooded figure is running along the east wall. Sprint after him before he gets away.", "Shift sprint, Space hop", "obj:Marker_Runner", "East wall", mg("chase_c1"),
   ["Someone was listening.", "Hold Shift and W. Stay within 25 metres of him.", "Follow the small marker. He drops something at the last corner."]),
 O("obj_c1_token", "ch1", "Courtyard", "Pick up what he dropped", "The runner dropped something small and metal. Inspect it.", "E inspect", "clues", "East wall", cl("clue_token17"),
   ["He dropped something as he ran.", "Look on the ground where the runner turned.", "Walk to the tin token and press E."]),
 O("obj_c1_enter", "ch1", "Courtyard", "Enter the storage hall", "Go into the storage hall where the three pieces were kept.", "E enter", "exit:Sanctum", "Temple door", sc("Sanctum"),
   ["The theft happened inside.", "The storage hall is through the temple door.", "Walk to the door and press E."]),
 O("obj_c2_search", "ch2", "Sanctum", "Search the dark hall", "The lamps are out. Turn on your flashlight and find three clues. Watch the battery.", "G flashlight, E inspect", "clues", "Storage hall", cl("clue_broken_lock", "clue_breached_case", "clue_muddy_footprints"),
   ["It is too dark to see detail without light.", "Press G. Look at the lock, the stands and the floor.", "Follow the marker to each clue and press E. Spare cells glow faintly blue."]),
 O("obj_c2_lamps", "ch2", "Sanctum", "Relight the five lamps", "Light the wall lamps in the order the sun travels: east first. A wrong lamp blows them all out.", "E light a lamp", "obj:Marker_Lamps", "Hall walls", mg("lamps"),
   ["The Pujari said: east first, as the sun does.", "Each lamp has a sun carving: sunrise, morning, noon, afternoon, dusk.", "Order: Sunrise, Morning, Noon, Afternoon, Dusk."]),
 O("obj_c2_pujari", "ch2", "Sanctum", "Show the token to the Pujari", "Show porter token No. 17 to Pujari Ananda.", "E talk", "npc:priest", "Storage hall", dlg("conv_c2_token"),
   ["Someone here knows whose token that is.", "The Pujari is waiting in the hall.", "Walk to Pujari Ananda and press E."]),
 O("obj_c2_deduce", "ch2", "Sanctum", "Work out how they got in", "Open the deduction board and match each clue to what it proves.", "B board, 1-3 choose", "", "Anywhere in the hall", mg("deduction"),
   ["Three clues, three conclusions.", "Press B to open the deduction board.", "Key: inside help. Handprint: a careful carrier. Chalk: a guide for the second man."]),
 O("obj_c2_exit", "ch2", "Sanctum", "Go to the outer shrine", "Follow the trail out to the outer shrine.", "E enter", "exit:ShrineCrime", "Hall exit", sc("ShrineCrime"),
   ["The trail leads outside.", "Find the exit toward the outer shrine.", "Walk to the exit and press E."]),
 O("obj_c3_traps", "ch3", "ShrineCrime", "Disarm the bell traps", "Threads with bells cross the forecourt. Crouch beside each one and disarm it. Do not walk through them.", "C crouch, hold E disarm", "obj:Marker_Trap", "Shrine forecourt", mg("traps"),
   ["They expected to be followed.", "The threads glint in your flashlight beam. Crouch with C next to one.", "Press E when the moving mark is inside the green zone, three times per trap."]),
 O("obj_c3_clues", "ch3", "ShrineCrime", "Examine the evidence", "Inspect the red silk and the torn note.", "E inspect", "clues", "Shrine doorway", cl("clue_fabric_fragment", "clue_ancient_map"),
   ["Two things were left behind at the door.", "Look at the doorframe and the floor near it.", "Follow the marker to each and press E."]),
 O("obj_c3_forensics", "ch3", "ShrineCrime", "Use the forensic kit", "Measure the bootprints, test the lamp and join the torn note.", "K kit, A/D adjust, E confirm", "", "Shrine forecourt", mg("forensics"),
   ["The prints, the lamp and the note still have things to say.", "Press K to open the kit. There are three tasks.", "Line the ruler up with each print, hold the lamp test, and rotate the pieces until they join."]),
 O("obj_c3_pujari", "ch3", "ShrineCrime", "Tell the Pujari what the note says", "Read the note to Pujari Ananda.", "E talk", "npc:priest", "Outer shrine", dlg("conv_c3"),
   ["The note names a place.", "The Pujari is close by.", "Walk to Pujari Ananda and press E."]),
 O("obj_c3_exit", "ch3", "ShrineCrime", "Go to the Sun Wall", "Take the passage to the Sun Wall.", "E enter", "exit:PuzzleWall", "Passage", sc("PuzzleWall"),
   ["The note said: Sun Wall at first light.", "Find the passage to the Sun Wall.", "Walk to the exit and press E."]),
 O("obj_c4_pujari", "ch4", "PuzzleWall", "Hear the scroll", "Ask Pujari Ananda how the wall opens.", "E talk", "npc:priest", "Before the wall", dlg("conv_c4"),
   ["The wall has an order.", "The Pujari knows the old scroll.", "Walk to Pujari Ananda and press E."]),
 O("obj_c4_wall", "ch4", "PuzzleWall", "Open the Sun Wall", "Turn the four symbols in the scroll's order, then crank the centre by hand before the wall resets.", "E use, 1-4 symbols, A/D crank", "puzzle", "The wall", {"type": "puzzle", "id": "chakra_wall"},
   ["Lotus, conch, wheel, kalash. Then the heart.", "Press E at the wall. The timer resets the wall, so be quick.", "Press 1, 2, 3, 4, then Enter and alternate A and D to crank."]),
 O("obj_c4_exit", "ch4", "PuzzleWall", "Follow them into the jungle", "The passage is open. Follow the boot prints.", "E enter", "exit:JunglePath", "Open passage", sc("JunglePath"),
   ["They went through.", "The open passage leads to the jungle.", "Walk through the passage and press E."]),
 O("obj_c5_stealth", "ch5", "JunglePath", "Follow them unseen", "Follow the two men through the ruins. Stay low, use grass and stones, throw pebbles to distract.", "C crouch, T throw pebble", "npc:robber1", "Jungle ruins", mg("stealth"),
   ["They must not know you are close.", "Crouch with C. Tall grass hides you. Stay out of the cones.", "Stay 6 to 14 metres behind them, crouched, and hide when the meter rises."]),
 O("obj_c5_fight", "ch5", "JunglePath", "Stop Bhaskar", "Subdue Bhaskar. Block or counter the white glint, dodge the red glint, use the braziers and your flashlight.", "LMB attack, RMB block, V dodge, G flash", "npc:robber1", "Brazier courtyard", mg("fight"),
   ["He is strong, but he telegraphs every swing.", "White glint: block (right mouse). Red glint: dodge (V). Then hit back.", "Flash him with G, knock a brazier with E when he stands near it, and attack while he is stunned."]),
 O("obj_c5_recover", "ch5", "JunglePath", "Recover the three pieces", "Lift the Surya Pradeepa, the Padma Shila and the Tala Patra from the open satchel.", "E inspect", "clues", "The ford", cl("relic_pradip", "relic_rosette", "relic_manuscript"),
   ["The satchel has fallen open.", "The three pieces lie near Bhaskar.", "Follow the marker to each piece and press E."]),
 O("obj_return", "ch6", "Courtyard", "Return to Pujari Ananda", "Bring the three pieces home before the dawn rite.", "E talk", "npc:priest", "Temple steps", dlg("conv_c6"),
   ["The rite is waiting.", "The Pujari is at the temple steps.", "Walk to Pujari Ananda and press E."]),
]

clues = [
 {"id": "clue_broken_lock", "title": "Wax-cut key and filings", "text": "The lock is whole. Beside it lie brass filings and a smear of wax: a key was copied.", "note": "Not forced. Someone with access made a copy."},
 {"id": "clue_breached_case", "title": "Oily handprint on the stand", "text": "A handprint in lamp oil, low on the cloth-lined stand.", "note": "He knelt to lift it. A careful carrier, used to heavy loads."},
 {"id": "clue_muddy_footprints", "title": "Chalk mark on the pillar", "text": "A small chalk arrow on the third pillar, pointing at the stands.", "note": "A guide left for someone who did not know the hall."},
 {"id": "clue_fabric_fragment", "title": "Red silk on the doorframe", "text": "A torn strip of red silk caught on a splinter.", "note": "The man in the red scarf left in a hurry."},
 {"id": "clue_ancient_map", "title": "Torn note", "text": "Four torn pieces of a note. Pieced together: \"Ford at moonset. Sun Wall at first light.\"", "note": "They are not selling. They are opening something."},
 {"id": "clue_token17", "title": "Porter token No. 17", "text": "A tin porter's token stamped 17.", "note": "The runner knew every stone of this courtyard."},
 {"id": "relic_pradip", "title": "Surya Pradeepa", "text": "The brass lamp-vessel, unharmed.", "note": "Fictional object."},
 {"id": "relic_rosette", "title": "Padma Shila", "text": "The carved sandstone rosette, unharmed.", "note": "Fictional object."},
 {"id": "relic_manuscript", "title": "Tala Patra", "text": "The palm-leaf manuscript, unharmed.", "note": "Fictional object."},
]
FIC = "  <i>(Fictional. Inspired by traditional crafts; not a real temple's property.)</i>"
codex = [
 {"id": "surya_pradeepa", "title": "Surya Pradeepa", "unlock": "conv_c1_pujari", "text": "A brass lamp-vessel lit at the first-light rite. Its flame is carried from household to household, so every home's morning lamp is lit from the same fire." + FIC},
 {"id": "padma_shila", "title": "Padma Shila", "unlock": "conv_c1_pujari", "text": "A carved sandstone rosette, the temple's foundation emblem. It is also the centre key of the Sun Wall." + FIC},
 {"id": "tala_patra", "title": "Tala Patra", "unlock": "conv_c1_pujari", "text": "A palm-leaf manuscript holding the words of the dawn rite and the register of the families who serve the temple. Without it the rite cannot be recited." + FIC},
 {"id": "sun_wall", "title": "The Sun Wall", "unlock": "conv_c3", "text": "A sealed chamber wall that opens at first light when four symbols are turned in order and the centre key is set. Behind it the community keeps generations of records." + FIC},
 {"id": "token_17", "title": "Porter Token No. 17", "unlock": "conv_c2_token", "text": "A tin token issued to temple porters. Kishore's was never handed back." + FIC},
]
tips = ["The lock was not forced. Ask who had the key.", "A lamp that is still warm was lit by someone nearby.", "The deeper print carries the heavier load.",
 "A wrong turn at the Sun Wall costs minutes you do not have.", "Crouching makes no sound. Running does.", "A thrown pebble buys you six seconds.",
 "The red glint cannot be blocked. Step aside.", "Your flashlight can blind as well as reveal.", "People lie fastest about the things they love.",
 "Bells on a thread: someone expected to be followed.", "Time saved in one chapter is time earned for the next.", "Dawn does not wait."]
endings = [
 {"id": "dawn_light", "title": "DAWN LIGHT", "text": "All three pieces came home before first light, and Kishore chose the right side when it counted."},
 {"id": "late_dawn", "title": "LATE DAWN", "text": "The three pieces are home. The rite began late, but it began."},
 {"id": "lost_dawn", "title": "LOST DAWN", "text": "Bhaskar reached the ford. The stands are still empty."}]

story = {"version": 2, "speakers": speakers, "chapters": chapters, "conversations": conversations, "objectives": objectives,
         "barks": barks, "clues": clues, "codex": codex, "tips": tips, "endings": endings,
         "ending": {"title": "MISSION COMPLETED", "text": "Every temple holds a story. Every artifact carries a memory. Protecting heritage means protecting our history."}}
json.dump(story, open(OUT, "w"), indent=1, ensure_ascii=False)
print("story.json:", len(ALL), "voiced lines,", len(objectives), "objectives")

if "--tts" in sys.argv:
    VOICE = {"aryan": ("Aman", 165), "pujari": ("Rishi", 140), "meera": ("Tara", 182), "bhaskar": ("Rocko (English (UK))", 150),
             "kishore": ("Reed (English (UK))", 185), "scroll": ("Grandpa (English (UK))", 140)}
    made = 0
    for id, (s, tier, t) in ALL.items():
        d = os.path.join(ROOT, "Assets/_Project/Resources/Voice", s); os.makedirs(d, exist_ok=True)
        wav = os.path.join(d, id + "_tts.wav")
        if os.path.exists(wav): continue
        v, rate = VOICE[s]
        rate += {"calm": 0, "firm": 8, "shout": 14}[tier]
        text = t.replace("Aryan-ji", "Aaryan jee").replace("...", ", ")
        aiff = "/tmp/_lot_tts.aiff"
        subprocess.run(["say", "-v", v, "-r", str(rate), "-o", aiff, text], check=True)
        subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@22050", "-c", "1", aiff, wav], check=True)
        made += 1
    print("tts files made:", made)
