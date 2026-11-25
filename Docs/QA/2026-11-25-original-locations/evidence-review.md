# Original locations — verification review, 25 November 2026

The user's final reference is the established village entrance, Courtyard and Sun Wall wheel plaza. The main route is `Courtyard`; the experimental TempleRing replacement is not the reference. The existing detailed temple game prefab is restored with its established footprint. No original Blender/Heritage Mystery file was edited.

## Build and Editor checks

- Latest standalone: `Builds/LegacyOfTheTemple.app`, **build 2026-11-25 09:32**. `../last-build.txt`: **Succeeded, errors=0, warnings=0**. The final build log is retained locally as `build-original-round5.log`.
- Wider Editor PlayMode run: [`editor-original-round2.xml`](editor-original-round2.xml), **32 passed, 0 failed**. Includes original-route layout/mirrors, pause/audio and local retry, plus ReviewRegressionTests, JeepPhysicsTests, PuzzleFrameworkTests and VoiceSystemTests.
- Final focused Editor run: [`editor-final-round5.xml`](editor-final-round5.xml), **4 passed, 0 failed**. Covers the complete temple footprint, sealed Courtyard doorway, real mirror rays and wheel obstruction, real R-key repair on the original approach road, same-scene retry/clock restoration, rejection of a foreign-scene checkpoint, visible pause viewport and five independent audio sliders.
- These runs overlap; their totals are not added together. Final LOD/UI appearance was also checked in the rebuilt app. Scanned logs contain no C# compile diagnostics or NullReference/MissingReference/Assertion/IndexOutOfRange exceptions.

## Latest standalone captures inspected

The game was launched as a real macOS window at 1280×720, with `-reviewConfirmedPlaces`. The audit positions the player/camera for reproducible comparisons. These are screenshots from the rebuilt game, not mockups. Runtime measurements are in [`app-final-round5/audit.md`](app-final-round5/audit.md).

| Capture | What I checked visually |
|---|---|
| [Village approach](app-final-round5/01-confirmed-village-and-minimap.png) | Established tree-lined road and Courtyard gateway retained; detailed temple silhouette visible behind it. Square radar draws the road, player, destination and distance. The old distant reduced-mesh roof fragments are no longer the active appearance. |
| [Courtyard seal](app-final-round5/01b-original-courtyard-sealed-door.png) | The original stepped entrance contains a closed wooden doorway and stone jambs, replacing the black opening. Original surrounding walls and elephant plinths remain. |
| [Sun Wall](app-final-round5/02-confirmed-sunwall-and-minimap.png) | The original large wheel, four mirrors, socket wall and detailed adjacent temple remain in the reference layout. This capture is the unsolved state. |
| [R recovery](app-final-round5/03-original-road-r-recovery.png) | Jeep upright between the original road kerbs with repair message. Audit reports tilt 0°, damage 0; this run presses R after placing the Jeep tilted beside the road. |
| [Local retry](app-final-round5/04-original-sunwall-local-retry.png) | Player remains at the original Sun Wall, with its local retry message. Audit reports Courtyard, checkpoint distance 0.00 m, restored clock approximately 6 minutes. |
| [Pause top](app-final-round5/05-pause-menu-top.png) | PAUSED, Resume, Controls and Retry Section Here inside the screen. Repair/retry messages do not cover the pause title. |
| [Five sliders](app-final-round5/06-pause-five-audio-sliders.png) | Master, Music, Ambience, Effects and Voice are individually visible and draggable. Settings bindings are tested in Editor. |
| [Pause bottom](app-final-round5/07-pause-menu-bottom.png) | Lower settings and Save and Quit are reachable by scrolling; build stamp visible at the bottom. |

## Full original-route story audit

The earlier restored-layout standalone run completed the established six-zone route and reached **MISSION COMPLETED**, with 5/5 recovered objects, 4/4 investigation evidence and all six zone scores. See [ending screenshot](mission-completed-preview.png) and [`story-round1/audit.md`](story-round1/audit.md). This run preceded the final minimap, pause menu and distant-mesh appearance refinements; it is not represented as a full story run of the last binary.

The scripted audit interacted with the puzzle controls: the Sun Wall retained an obstructed ray until the wheel turned, then both sockets lit. It also ran the Jeep chase, encounters and return dialogue. It uses teleporting between test locations and controlled encounter setup, so it does not prove an uninterrupted manual journey or every possible player choice. Its grade S and score are bot-run outputs, not player-study data. The full-resolution raw story captures and logs are retained locally in `story-round1/`; the repository contains the compact ending preview and audit text.

## Scope and limits

- Original temple source SHA-256 and modification time match before/after: [`original-source-before.json`](original-source-before.json), [`original-source-after.json`](original-source-after.json). The read-only inspection script renders a separate QA reference and never saves the Blender source. Backup tag `backup-visual-1` already existed before these edits.
- Keeping the complete existing temple game mesh avoids silhouette loss but costs more geometry than its damaged reduced LODs. No fresh frame-rate or memory benchmark was established. No “60 FPS” guarantee is made.
- Saved Master and Music settings are both 0% in these captures. They were preserved; raise them to hear music. The QA checks the five channel bindings and source playback hooks, not a human listening study.
- Original locations include exposed platform edges and dense decorative materials. Retaining the reference layout does not establish that every visual detail is finished.
- The Evaluation PDF/PPT predate this location correction and describe the earlier technical snapshot. The README and current summary explicitly distinguish that snapshot from the restored main route. Player study remains pending.
- Builds stay ignored by Git. The rebuilt app is local; source, tests and selected evidence are pushed. No external assets were downloaded and no software installed.

## Repeat the focused review

Open `My project` in Unity, use Test Runner → PlayMode → `OriginalRouteReviewTests`. For the standalone capture audit, launch the app executable with `-auditBot <project QA output directory> -reviewConfirmedPlaces -noIntroVideo -introSkip -screen-width 1280 -screen-height 720`. Use an empty output directory to keep previous evidence intact. Normal players launch the app and choose **New Game**.
