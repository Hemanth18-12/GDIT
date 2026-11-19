# Walkthrough bot notes

## What was used
- **Build:** `Builds/LegacyOfTheTemple.app`, a development build made on 2026-11-19 from commit `ce5ea68` plus the walkthrough bot code (0 errors, 0 warnings).
- **Scene:** the Courtyard scene. All six V3 places are in it.
- **How it ran:** **windowed** at 1920 x 1080, so the HUD shows in every picture. Command:
  ```
  open -n Builds/LegacyOfTheTemple.app --args -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 \
       -auditBot Docs/Walkthrough/raw -logFile Docs/Walkthrough/raw/player.log -noIntroVideo -walkthrough
  ```
- **Bot code:** the `-walkthrough` mode in `Assets/_Project/Scripts/UI/AuditBot.cs`:
  - `WalkthroughExtras()` captures the HUD, map, journal, evidence board, guide, pause menu, characters and jeep.
  - The existing `V3StoryCheck()` then plays all 9 story steps with real key presses.
  - The bot only runs when the game is started with `-auditBot`, so the normal game is not affected. It is a test tool, like the earlier `-v3Story` mode.
- **Screenshots:** taken with `ScreenCapture.CaptureScreenshot` at the moment named in each file. They are not edited. The PDF uses 1600 px JPEG copies.

## How the bot plays
- **Moving:** it teleports the hero to standing points and turns him to face the target. It does not walk the whole way with W.
- **Actions:** it presses the real E key, through the input system, at each part.
- **Camera:** it sets the camera for some pictures, then gives control back to the normal camera.
- **Fights:** the bot steps next to the nearest robber (by teleport) and presses strike, heavy, finisher, block and dodge through the game's on-screen button input (`VirtualInput`), the same path the on-screen fight buttons use. It does not aim with the mouse.
- **Jeep chase:** the bot drives the jeep with the car's autopilot steering, aimed at the road points. It cannot see the road, so a human must steer with A and D.
- **Score table:** a New Game in test mode resets the run. The bot saves your best-runs table first and gives it back at the end.

## Result of the run (2026-11-19, 19:13 to 19:24)
- Story complete, all 9 steps, **ending Dawn Light**, 5 of 5 treasures, 4 of 4 clues, Madhav named correctly.
- Grades S, S, S, S, S and A (river road). Total 22,555 points. Clock 05:21. Play time 8:15.
- **0 exceptions** in `player.log`. The best-runs table was given back afterwards.
- **Pictures:** 93 raw captures, of which 61 were used (see D-38). Contact sheet: `Docs/Walkthrough/contact_sheet.png`.

## Things the bot could not do, or did differently from a player
- **Walking:** it does not walk the whole route; it teleports to each standing point. A player walks or runs (4 or 7.5 m/s) or uses map fast travel.
- **Jeep chase:** it steers the jeep through the car's autopilot, not with A and D.
- **Fights:** it teleports next to robbers and presses fight actions through `VirtualInput`; it never parried (0 parries).
- **Evidence board:** the story run names Madhav through the board's code, not by clicking through the panel.
- **The moment the courier spots you:** this happened (1 sighting at 6 m), but the picture was taken from inside a tree and was not used.
- **Main-menu Credits and Settings screens:** not captured.
- **Rejected frames:** four intro frames came out black or near black (the cutscene fades in) and were not used.
