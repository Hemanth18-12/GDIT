# Runtime audit (2026-11-16 18:48, Standalone, 1600x900)

## UI: MainMenu

#### UI table: MainMenu @ requested 1280x720 (actual 1280x720); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: SETTINGS | 31 | 18 | 16.2 |  |
| BtnKeyHint: CREDITS | 31 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 31 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 31 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 29 | 26 | 13.8 |  |
| BtnKeyHint: NEW GAME | 31 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 31 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 58 | 48 | 9.8 |  |
| BtnKeyHint: CONTINUE | 31 | 18 | 16.2 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 1920x1080 (actual 1920x1080); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: SETTINGS | 31 | 18 | 16.2 |  |
| BtnKeyHint: CREDITS | 31 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 31 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 31 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 29 | 26 | 13.8 |  |
| BtnKeyHint: NEW GAME | 31 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 31 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 58 | 48 | 9.8 |  |
| BtnKeyHint: CONTINUE | 31 | 18 | 16.2 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 2560x1440 (actual 2560x1440); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: SETTINGS | 31 | 18 | 16.2 |  |
| BtnKeyHint: CREDITS | 31 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 31 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 31 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 29 | 26 | 13.8 |  |
| BtnKeyHint: NEW GAME | 31 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 31 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 58 | 48 | 9.8 |  |
| BtnKeyHint: CONTINUE | 31 | 18 | 16.2 |  |
Problems in this view: 0

## Play-through from New Game (bot uses the Guide target for every objective)
  loading bar: 30% at 0.70 s (image loading_c_investigator)
  loading bar: 60% at 1.23 s (image loading_c_investigator)
  loading bar: 100% at 2.05 s (image loading_c_investigator)
  loading screen 1: image=loading_c_investigator, bar=1.00

## Intro: playing=True
  intro skipped: finished=True, player enabled=True, banner='OBJECTIVE: REACH JAGANNATH TEMPLE', objective=obj_c0_reach, clock hold=False

## V3 main path: start objective obj_c0_reach (Reach Jagannath Temple)
  old scene doors still usable: 0
  through the gate: objective obj_v3_pujari (Find the priest)
  priest-start: prompt 'Talk to Pujari Ananda', objective now obj_v3_bazaar (The bell in the bazaar)
  goal card alpha 9 s after the goal changed: 0.00

## V3 zone 1 Bazaar tail: present=True, built=True, courier=True
  near the tea stall: courier phase Walking
  stood 6 m behind him in the open while he looked back: spotted 1
  tail finished: phase Arrived, spotted 1 time(s), 76 s
  fight begins: 2 enemy(ies): the courier (Knife, 70 hp), his helper (Brawler, 90 hp)
  fight result: won=True in 11s, health left 91, damage taken 9, max combo 8, parries 0, finishers 2, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False
  take the Dawn Bell: prompt 'Take the Dawn Bell'
  bell taken=True, evidence list=1
  score bazaar: puzzle solved=True, hints=0, items=1, points=3705, grade=S
  after the bazaar: objective obj_v3_storeroom (The storeroom)

## V3 zone 2 break-in puzzle: present=True, built=True
  start: lantern usable=False, flagstone locked=True, treasure usable=False
  wrong order tried first (flagstone, lantern): flagstone state=0 (0 = still down), flagstone prompt='', holding=False
  shard 1 pick up: prompt 'Pick up pot shard'
  shard 1 at the plinth: prompt 'Place pot shard'
    socket states: 1 0 0
  shard 2 pick up: prompt 'Pick up pot shard'
  shard 2 at the plinth: prompt 'Place pot shard'
    socket states: 1 1 0
  shard 3 pick up: prompt 'Pick up pot shard'
  shard 3 at the plinth: prompt 'Place pot shard'
    socket states: 1 1 1
  pot whole=True, lantern usable=True
  lantern pick up: prompt 'Pick up lantern'
  lantern put down on the oil stain: prompt 'Put down lantern'
  lantern on the stain=True (distance 0.10 m), flagstone locked=False
  lock plate turn 1: prompt 'Turn lock plate'
  lock plate turn 2: prompt 'Turn lock plate'
  lock plate state=2 (2 = inside face showing), evidence saved=1, banner='NOTED: Scratches on the inside. Opened, not forced.'
  lift the flagstone: prompt 'Lift flagstone'
  flagstone state=1, puzzle solved=True, treasure usable=False
  fight begins: 1 enemy(ies): the lookout (Knife, 70 hp)
  fight result: won=True in 5s, health left 100, damage taken 0, max combo 6, parries 0, finishers 0, retries 0; most attackers at once 0 (limit 2); telegraph seen=False; boss phase 2 seen=False
  take the Chronicle: prompt 'Take the Palm-Leaf Chronicle'
  treasure taken=True; score: puzzle solved=True, hints=0, items=1, points=3800, grade=S
  checkpoint: courtyard/before_fight
  after the storeroom: objective obj_v3_vaults (The vaults)

## V3 zone 3 Vaults: present=True, built=True
  start: gate open=False, plates 0 0
  block B pushed the wrong way (west): prompt 'Push'
  block B pushed the wrong way (west): prompt 'Push'
  block B pushed the wrong way (west): prompt 'Push'
  block B pushed the wrong way (west): prompt 'Push'
  block B after wrong pushes at (-10.0, 0.5, -1.0)
  reset lever: prompt 'Pull the reset lever'
  after the lever: block A at (-8.0, 0.5, 3.0), block B at (-6.0, 0.5, -1.0)
  block A east: prompt 'Push'
  block A east: prompt 'Push'
  block A east: prompt 'Push'
  block A east: prompt 'Push'
  block B south (round the pillar): prompt 'Push'
  block B south (round the pillar): prompt 'Push'
  block B east: prompt 'Push'
  block B east: prompt 'Push'
  blocks placed: A at (-4.0, 0.5, 3.0), B at (-4.0, 0.5, -3.0), plates 1 1, gate open=True
  disc 1 turn 1: prompt 'Turn the stone disc'
  disc 1 turn 2: prompt 'Turn the stone disc'
  disc 2 turn 1: prompt 'Turn the stone disc'
  disc 2 turn 2: prompt 'Turn the stone disc'
  disc 2 turn 3: prompt 'Turn the stone disc'
  disc 2 turn 4: prompt 'Turn the stone disc'
  disc 2 turn 5: prompt 'Turn the stone disc'
  disc 3 turn 1: prompt 'Turn the stone disc'
  disc 3 turn 2: prompt 'Turn the stone disc'
  disc 3 turn 3: prompt 'Turn the stone disc'
  discs at 2 5 3 (answer 2 5 3), solved=True, evidence ledger=1
  fight begins: 2 enemy(ies): the heavy guard (Heavy, 160 hp), the vault guard (Brawler, 90 hp)
  fight result: won=True in 15s, health left 68, damage taken 32, max combo 17, parries 0, finishers 2, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False
  take the rosette: prompt 'Take the Sun Rosette'
  rosette taken=True
  score vaults: puzzle solved=True, hints=0, items=1, points=3840, grade=S
  after the vaults: objective obj_v3_stepwell (The stepwell)

## V3 zone 4 Stepwell: present=True, built=True
  start: water level 0, panel locked=True
  panel tried early: state=0 (0 = shut)
  south sluice wheel: prompt 'Turn the sluice wheel'
  north sluice wheel: prompt 'Turn the sluice wheel'
  both sluices open: water level 4, raft top at 5.35 m (rim 5.2), panel locked=True
  drain chain 1: prompt 'Work the drain chain'
  drain chain 2: prompt 'Work the drain chain'
  drained twice: water level 2, player height above the tank floor 2.95 m (rode the raft down), panel locked=False
  push the panel: prompt 'Push the carved panel'
  panel state=1, solved=True, evidence key ring=1
  take the vessel: prompt 'Take the Monsoon Vessel'
  vessel taken=True
  fight begins: 2 enemy(ies): the knife man (Knife, 70 hp), his helper (Brawler, 90 hp)
  fight result: won=True in 10s, health left 82, damage taken 18, max combo 6, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False
  score stepwell: puzzle solved=True, hints=0, items=1, points=3710, grade=S
  after the stepwell: objective obj_v3_sunwall (The Sun Wall)

## V3 zone 5 Sun Wall: present=True, built=True
  start: sockets lit 0 0, wheel stop 0, door item usable=False
  mirror 1 turn 1: prompt 'Turn mirror'
  mirror 1 turn 2: prompt 'Turn mirror'
  mirror 2 turn 1: prompt 'Turn mirror'
  mirror 2 turn 2: prompt 'Turn mirror'
  mirror 3 turn 1: prompt 'Turn mirror'
  mirror 3 turn 2: prompt 'Turn mirror'
  mirror 4 turn 1: prompt 'Turn mirror'
  mirror 4 turn 2: prompt 'Turn mirror'
  four mirrors set, wheel not moved: sockets lit 0 1 (the right one must still be dark: a spoke blocks its beam)
  push the wheel: prompt 'Push the wheel'
  wheel stop 1: sockets lit 1 1, solved=True
  fight begins: 1 enemy(ies): Kishore (Boss, 320 hp)
  fight result: won=True in 18s, health left 100, damage taken 0, max combo 21, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=True
  take the case: prompt 'Take the finial's empty case and a boat receipt'
  case taken=True
  score sunwall: puzzle solved=True, hints=0, items=1, points=4000, grade=S
  after the Sun Wall: objective obj_v3_jungle (The river road)
  evidence found 4 of 4; naming Madhav: True

## V3 zone 6 Jungle road: present=True, built=True, runner=True
  jeep at the road start: 4.0 m
  in the jeep: True; chase phase Chase
  chase over in 38 s: phase Court, closest gap 4.7 m, caught up=True, jeep damage state Wrecked
  fight begins: 3 enemy(ies): a brawler (Brawler, 90 hp), a knife man (Knife, 70 hp), a brawler (Brawler, 90 hp)
  fight result: won=True in 17s, health left 43, damage taken 57, max combo 7, parries 0, finishers 3, retries 0; most attackers at once 2 (limit 2); telegraph seen=True; boss phase 2 seen=False
  wave 2 (inside man named correctly=True):
  fight begins: 1 enemy(ies): a heavy (Heavy, 160 hp)
  fight result: won=True in 8s, health left 100, damage taken 0, max combo 11, parries 0, finishers 1, retries 0; most attackers at once 0 (limit 2); telegraph seen=False; boss phase 2 seen=False
  Bhaskar:
  fight begins: 1 enemy(ies): Bhaskar (Boss, 320 hp)
  fight result: won=True in 14s, health left 84, damage taken 16, max combo 21, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=True
  take the Chariot Finial: prompt 'Take the Chariot Finial'
  finial taken=True, phase Done
  score jungle: puzzle solved=True, hints=0, items=1, points=3635, grade=S
  after the river road: objective obj_return (Return the treasures)
  priest-return: prompt 'Talk to Pujari Ananda', objective now (story complete)
  story complete=True; total 22690 points, grade S
