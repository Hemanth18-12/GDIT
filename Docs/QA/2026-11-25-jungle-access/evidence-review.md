# Blocked jungle entrance and missing front damage key — 25 November 2026

Scope: only the Jeep/jungle entrance problem shown in the user's screenshots. No report/PPT, audio, animations, puzzles, scenery layout or original Blender source changed.

## Two failures reproduced

The user's Player.log (saved locally as user-player-before.log) records the Jeep colliding with `TempleBoundary`, followed by `KeyNotFoundException` in `Jeep.ApplyDamage` for `front`. Repair used `ZoneDamage.Clear()`, deleting the keys that the next impact indexed.

The [before-fix PlayMode results](before.xml) contain **2 failed tests**:

- Repair, then impact: throws the same missing `front` key exception.
- Road entrance across scene reload: a Jeep-width box cast is clear on the first load but hits a full-width TempleBoundary on the second. Logs show that the old static temple footprint lets the jungle builder run before the current boundary exists, so its existing opening is never cut on that reload.

## Two small gameplay changes

- `Jeep.cs`: R sets all five known damage-zone values to zero instead of removing their entries.
- `TempleSite.cs`: a new instance clears the previous footprint in Awake. Dependent builders then wait for the current scene's temple before modifying its boundary.

The intended road opening and scenery positions are preserved. `JungleRoadAccessTests.cs` adds the two regression checks. `AuditBot.cs` adds an opt-in standalone verification mode; its temporary collision barrier exists only with `-reviewJungleAccess`.

## Verification

- [After-fix Editor results](after.xml): **2 passed, 0 failed**. Tests cover fresh/reloaded Courtyard road clearance and damage in all five zones after repeated repairs.
- [Build result](build-result.txt): **build 2026-11-25 10:27**, succeeded with **0 errors, 0 warnings**.
- [Actual standalone audit](app/audit.md): after reloading Courtyard, the bot enters with E and uses W/A/D to drive across the exact entrance from the temple side. No teleport or direct vehicle translation occurs during that driving segment. It records **48.8 m forward progress**, past the boundary and into the jungle.
- Real R input preserves all five damage keys. A temporary QA obstacle then causes a real front impact: **18.7 damage, 0 errors**. After a second R, driving continues at **5.5 m/s**, damage zero, errors zero.
- Scanned final Editor/build/player logs contain no C# diagnostics, KeyNotFoundException, NullReferenceException or MissingReferenceException. The standalone audit counts all error/exception/assert callbacks and reports zero.

## Screenshot inspection

All four runtime captures were opened and inspected:

1. [Entrance before driving](app/01-jungle-entrance-before-driving.png): the same temple apron, forest road, Jeep and trees as the reported location.
2. [Entrance crossed](app/02-jungle-entrance-crossed.png): the Jeep is inside the forest road, beyond the former invisible wall, with no red error console.
3. [Post-repair impact](app/03-impact-after-repair.png): this camera view is partly obscured by a foreground rock and shows the temporary QA barrier. It is not used as visual proof of contact; the physics collision log and audit establish the impact and damage.
4. [Driving after second repair](app/04-jungle-driving-after-second-repair.png): the Jeep is moving on the jungle road, with its repair message and approximately 20 km/h HUD speed. No missing-key console is visible.

## Limits and saved state

This is a focused entrance-and-repair regression, not a new whole-game audit or proof of every forest obstacle. The scripted test stages the Jeep before the entrance; the crossing itself uses normal input. The original Blender hash and modification time still match the prior baseline. Existing tag backup-visual-1 was present before changes. Builds remain ignored by Git.

Player preferences/progress were exported before the checks and restored after the standalone audit. Quit any old game window before opening the rebuilt local app, then Continue and drive through the same entrance. Confirm the pause-menu build stamp is **2026-11-25 10:27**. The fix applies when the scene loads; it cannot change an already-running older app.
