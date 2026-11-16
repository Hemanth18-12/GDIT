# Runtime audit (2026-11-16 07:34, Standalone, 1600x900)

## UI: MainMenu

#### UI table: MainMenu @ requested 1280x720 (actual 1280x720); TMP texts=8; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 1920x1080 (actual 1920x1080); TMP texts=8; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 2560x1440 (actual 2560x1440); TMP texts=8; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
Problems in this view: 0

## Play-through from New Game (bot uses the Guide target for every objective)
  loading bar: 31% at 0.72 s (image loading_c_investigator)
  loading bar: 60% at 1.23 s (image loading_c_investigator)
  loading bar: 100% at 2.05 s (image loading_c_investigator)
  loading screen 1: image=loading_c_investigator, bar=1.00

## Intro: playing=True
  intro skipped: finished=True, player enabled=True, banner='OBJECTIVE: REACH JAGANNATH TEMPLE', objective=obj_c0_reach, clock hold=False

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
  flagstone state=1, puzzle solved=True, treasure usable=True
  take the Chronicle: prompt 'Take the Palm-Leaf Chronicle'
  treasure taken=True; score: puzzle solved=True, hints=0, items=1, points=3500, grade=A
  checkpoint: courtyard/storeroom

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
  take the case: prompt 'Take the finial's empty case and a boat receipt'
  case taken=True
  score sunwall: puzzle solved=True, hints=0, items=1, points=3500, grade=A

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
  score stepwell: puzzle solved=True, hints=0, items=1, points=3500, grade=A

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
  take the rosette: prompt 'Take the Sun Rosette'
  rosette taken=True
  score vaults: puzzle solved=True, hints=0, items=1, points=3500, grade=A

## V3 run total: 14000 points, overall grade for 4 zones A
