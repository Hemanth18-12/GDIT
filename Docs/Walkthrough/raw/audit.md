# Runtime audit (2026-11-19 19:21, Standalone, 1920x1080)

## UI: MainMenu

## Play-through from New Game (bot uses the Guide target for every objective)
  loading bar: 31% at 1.38 s (image loading_a_temple_sunset)
  loading bar: 61% at 1.68 s (image loading_a_temple_sunset)
  loading bar: 100% at 1.70 s (image loading_a_temple_sunset)
  loading screen 1: image=loading_a_temple_sunset, bar=1.00

## Intro: playing=True
  intro t=30.6s shot=03b_pujari_closeup
  intro t=30.7s shot=03b_pujari_closeup
  intro t=30.8s shot=03b_pujari_closeup
  intro t=30.9s shot=03b_pujari_closeup
  intro t=31.0s shot=04a_aerial_follow
  intro t=31.2s shot=04a_aerial_follow
  intro t=31.3s shot=04a_aerial_follow
  intro t=31.4s shot=04a_aerial_follow
  intro t=33.5s shot=04a_aerial_follow
  intro t=38.0s shot=04b_low_front
  intro t=41.5s shot=04c_wheel_closeup
  intro t=46.0s shot=04d_side_tracking
  intro t=51.5s shot=04e_wide_stop
  intro t=54.0s shot=04f_boots_to_face
  intro t=56.5s shot=04f_boots_to_face
  intro t=60.5s shot=05a_over_shoulder
  intro t=64.0s shot=05b_two_shot
  intro t=68.0s shot=05c_handover
  intro ended after 40.6s real time: shots captured=18, player enabled=True, banner='', objective=obj_c0_reach

## Walkthrough extras
  map: captured w-map.png
  journal: captured w-journal.png
  evidence board: captured w-evidence-board.png
  guide: captured w-guide.png
  character meera: captured at (-1.8, 0.1, 11.5)
  character pujari: captured at (1.8, 0.1, 11.5)
  jeep: captured

## V3 main path: start objective obj_c0_reach (Reach Jagannath Temple)
  old scene doors still usable: 0
  through the gate: objective obj_v3_pujari (Find the priest)
  priest-start: prompt 'Talk to Pujari Ananda', objective now obj_v3_bazaar (The bell in the bazaar)
  goal card alpha 9 s after the goal changed: 0.00

## V3 zone 1 Bazaar tail: present=True, built=True, courier=True
  near the tea stall: courier phase Walking
  stood 6 m behind him in the open while he looked back: spotted 1
  tail finished: phase Arrived, spotted 1 time(s), 75 s
  fight begins: 2 enemy(ies): the courier (Knife, 70 hp), his helper (Brawler, 90 hp)
  fight result: won=True in 11s, health left 79, damage taken 21, max combo 8, parries 0, finishers 2, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False, highest phase 1
  take the Dawn Bell: prompt 'Take the Dawn Bell'
  bell taken=True, evidence list=1
  score bazaar: puzzle solved=True, hints=0, items=1, points=3645, grade=S
  after the bazaar: objective obj_v3_storeroom (The storeroom)
  PERF storeroom: 605 frames, average 6.6 ms (151 fps), 99% 8.8 ms, worst 16.0 ms, CPU main 2.4 ms, CPU render 0.9 ms, GPU 12.4 ms, graphics High, draw-visible villagers 0
  PERF vaults: 478 frames, average 8.4 ms (119 fps), 99% 11.9 ms, worst 19.5 ms, CPU main 3.7 ms, CPU render 2.5 ms, GPU 13.2 ms, graphics High, draw-visible villagers 0
  PERF stepwell: 581 frames, average 6.9 ms (145 fps), 99% 9.0 ms, worst 17.1 ms, CPU main 3.1 ms, CPU render 1.9 ms, GPU 12.3 ms, graphics High, draw-visible villagers 0
  PERF sunwall: 617 frames, average 6.5 ms (154 fps), 99% 9.1 ms, worst 17.1 ms, CPU main 2.4 ms, CPU render 1.3 ms, GPU 11.8 ms, graphics High, draw-visible villagers 0
  PERF river road: 512 frames, average 7.8 ms (128 fps), 99% 10.4 ms, worst 17.0 ms, CPU main 3.2 ms, CPU render 2.0 ms, GPU 13.5 ms, graphics High, draw-visible villagers 0
  PERF ruin court: 668 frames, average 6.0 ms (167 fps), 99% 10.1 ms, worst 17.0 ms, CPU main 3.1 ms, CPU render 2.2 ms, GPU 10.5 ms, graphics High, draw-visible villagers 0
  PERF bazaar: 611 frames, average 6.6 ms (152 fps), 99% 9.5 ms, worst 17.1 ms, CPU main 2.4 ms, CPU render 1.2 ms, GPU 12.5 ms, graphics High, draw-visible villagers 13
  PERF temple front: 690 frames, average 5.8 ms (172 fps), 99% 8.6 ms, worst 12.5 ms, CPU main 2.6 ms, CPU render 1.3 ms, GPU 10.9 ms, graphics High, draw-visible villagers 10
  Madhav at the storeroom: on stage (obj_v3_storeroom)

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
  fight result: won=True in 5s, health left 100, damage taken 0, max combo 6, parries 0, finishers 0, retries 0; most attackers at once 0 (limit 2); telegraph seen=False; boss phase 2 seen=False, highest phase 1
  take the Chronicle: prompt 'Take the Palm-Leaf Chronicle'
  treasure taken=True; score: puzzle solved=True, hints=0, items=1, points=3800, grade=S
  checkpoint: courtyard/before_fight
  after the storeroom: objective obj_v3_vaults (The vaults)
  Madhav at the vaults: on stage (obj_v3_vaults)

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
  fight result: won=True in 16s, health left 80, damage taken 20, max combo 17, parries 0, finishers 2, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False, highest phase 1
  take the rosette: prompt 'Take the Sun Rosette'
  rosette taken=True
  score vaults: puzzle solved=True, hints=0, items=1, points=3900, grade=S
  after the vaults: objective obj_v3_stepwell (The stepwell)
  Madhav at the stepwell: on stage (obj_v3_stepwell)

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
  fight result: won=True in 10s, health left 82, damage taken 18, max combo 6, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False, highest phase 1
  score stepwell: puzzle solved=True, hints=0, items=1, points=3710, grade=S
  after the stepwell: objective obj_v3_sunwall (The Sun Wall)
  Madhav at the sunwall: on stage (obj_v3_sunwall)

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
  fight result: won=True in 17s, health left 100, damage taken 0, max combo 21, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=True, highest phase 2
  take the case: prompt 'Take the finial's empty case and a boat receipt'
  case taken=True
  score sunwall: puzzle solved=True, hints=0, items=1, points=4000, grade=S
  after the Sun Wall: objective obj_v3_jungle (The river road)
  evidence found 4 of 4; naming Madhav: True

## V3 zone 6 Jungle road: present=True, built=True, runner=True
  jeep at the road start: 0.0 m
  in the jeep: True; chase phase Chase
  chase over in 32 s: phase Court, closest gap 9.9 m, caught up=False, jeep damage state Normal (total damage 0)
  fight begins: 3 enemy(ies): a brawler (Brawler, 90 hp), a knife man (Knife, 70 hp), a brawler (Brawler, 90 hp)
  fight result: won=True in 30s, health left 79, damage taken 132, max combo 8, parries 0, finishers 4, retries 1; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False, highest phase 1
  wave 2 (inside man named correctly=True):
  fight begins: 1 enemy(ies): a heavy (Heavy, 160 hp)
  fight result: won=True in 9s, health left 100, damage taken 0, max combo 11, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=False, highest phase 1
  Bhaskar:
  fight begins: 1 enemy(ies): Bhaskar (Boss, 320 hp)
  fight result: won=True in 14s, health left 84, damage taken 16, max combo 21, parries 0, finishers 1, retries 0; most attackers at once 1 (limit 2); telegraph seen=True; boss phase 2 seen=True, highest phase 3
  take the Chariot Finial: prompt 'Take the Chariot Finial'
  finial taken=True, phase Done
  score jungle: puzzle solved=True, hints=0, items=1, points=3500, grade=A
  after the river road: objective obj_return (Return the treasures)
  art pass props placed so far: 101; Madhav on stage at 
  priest-return: prompt 'Talk to Pujari Ananda', objective now (story complete)
  3D sound effects played: 1779 (33 different): fs_gravel_1 x82, fs_gravel_2 x95, fs_mud_1 x427, fs_mud_2 x428, fs_stone_1 x17, fs_stone_2 x18, fs_stone_3 x19, fs_wet_2 x1, fs_wet_3 x1, fs_wood_1 x1, fs_wood_2 x1, sfx_block x1, sfx_body_fall x15, sfx_brass_set x4, sfx_cloth x74, sfx_dodge x5, sfx_door_stone x4, sfx_drip x4, sfx_gear_click x14, sfx_grunt_aryan x18, sfx_grunt_bhaskar x21, sfx_grunt_pain x121, sfx_hit_heavy x69, sfx_hit_light x74, sfx_hit_taken x18, sfx_lamp_ignite x1, sfx_rope x2, sfx_stagger x58, sfx_stone_grind x25, sfx_swish_quick x124, sfx_swish_wide x32, sfx_token x4, sfx_wall_open x1
  story complete=True; total 22555 points, grade S
