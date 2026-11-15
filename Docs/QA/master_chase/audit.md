# Runtime audit (2026-11-15 19:34, Standalone, 1920x1080)

## UI: MainMenu

#### UI table: MainMenu @ requested 1280x720 (actual 1280x720); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTINUE | 43 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 1920x1080 (actual 1920x1080); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTINUE | 43 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
Problems in this view: 0

#### UI table: MainMenu @ requested 2560x1440 (actual 2560x1440); TMP texts=9; active IMGUI(OnGUI) scripts not measurable=0
| text | px@1080p | min | contrast(worst) | issues |
|---|---|---|---|---|
| BtnKeyHint: CREDITS | 43 | 18 | 16.2 |  |
| BtnKeyHint: QUIT | 43 | 18 | 16.2 |  |
| BtnKeyHint: NEW GAME | 43 | 18 | 16.2 |  |
| BtnKeyHint: SETTINGS | 43 | 18 | 16.2 |  |
| BtnKeyHint: CONTINUE | 43 | 18 | 16.2 |  |
| MenuTitle: LEGACY OF THE TEMPLE | 80 | 48 | 9.8 |  |
| MenuSubtitle: A heritage mystery | 40 | 26 | 13.8 |  |
| BtnKeyHint: CONTROLS | 43 | 18 | 16.2 |  |
| BtnKeyHint: SHOWCASE TOUR | 43 | 18 | 16.2 |  |
Problems in this view: 0

## Play-through from New Game (bot uses the Guide target for every objective)
  loading bar: 30% at 0.69 s (image loading_d_village_aerial)
  loading bar: 60% at 1.25 s (image loading_d_village_aerial)
  loading bar: 100% at 2.08 s (image loading_d_village_aerial)
  loading screen 1: image=loading_d_village_aerial, bar=1.00

## Intro: playing=True
  intro skipped: finished=True, player enabled=True, banner='OBJECTIVE: REACH JAGANNATH TEMPLE', objective=obj_c0_reach, clock hold=False
  loading bar: 30% at 0.68 s (image loading_e_robbery)
  loading bar: 60% at 1.23 s (image loading_e_robbery)
  loading bar: 100% at 2.05 s (image loading_e_robbery)
  loading screen 2: image=loading_e_robbery, bar=1.00

## Lock check: objective obj_c2_lock
  minigame 'lock'
  lock: case present=True, prompt='Try the dial lock'
  lock: panel open=True
  lock: wrong code 1-0-0 -> still open=True, objective=obj_c2_lock
  lock: code 1-7-5 -> panel open=False, banner='CASE OPENED: AN EMPTY CLOTH LINING, STILL WARM WITH LAMP SOOT', objective now obj_c2_deduce
  loading bar: 30% at 0.68 s (image loading_a_temple_sunset)
  loading bar: 60% at 1.23 s (image loading_a_temple_sunset)
  loading bar: 100% at 2.05 s (image loading_a_temple_sunset)
  loading screen 3: image=loading_a_temple_sunset, bar=1.00

## Timeline board: objective obj_c3_timeline
  minigame 'timeline'
  timeline: board open=True
  timeline: wrong first pick -> step=0
  timeline: after the right order -> step=5, banner='TIMELINE COMPLETE: TWO MEN, A KEY, AND A HURRIED EXIT', objective now obj_c3_pujari

## PuzzleWall: after the scroll talk the objective is obj_c4_exit (Sun Wall step removed)
  loading screen 4: image=loading_b_thieves, bar=0.57
  rain intensity=0.00, wall solved=True, wall prompt radius=0.0, sun=0.3
  loading screen 5: image=loading_c_investigator, bar=0.57

## Jungle jeep chase: objective obj_c5_chase
  minigame 'jeep_chase'
  chase: present=True, jeep=True, gap at start=16 m
  chase: in the jeep=True
  chase: after 25.0 s -> attempts=10, banner='THEY ARE RUNNING: TAKE THE JEEP AND CATCH UP', objective now obj_c5_chase, jeep occupied=True
