# V3 progress (resume file)

Say "Continue from Docs/PROGRESS.md" to resume. Last update: 2026-11-16.

## Done (committed)
- Tag `backup-v3-start`.
- New olive jeep in the game on the existing driving, braking and damage code (commit 523cbf7).
- blender-skills pack installed in `.claude/skills/` (MIT); Blender connection verified.
- Phase 1A audit: `Docs/AUDIT_V3.md`, before pictures in `Docs/QA/before/`, `Docs/BEFORE_AFTER.md` (before side).
- Phase 1B story: `Docs/STORY_V3.md` (44 spoken lines, twist, flow diagram).
- Phase 1C level design: `Docs/LEVEL_DESIGN_V3.md`, map `Docs/QA/v3_map_overview.png`.
- `Docs/DECISIONS.md` (D-01 to D-10).

## Zone 2 (2026-11-16)
- Break-in puzzle built in the Courtyard scene (`BreakInPuzzle.cs`) and played end to end by the bot with real key presses: `Docs/QA/v3/zone2_run/audit.md`, pictures `Docs/QA/v3/zone2_sheet.png` (sheet is from the run before the lock plate was moved).
- Main menu now shows the real temple (`MenuTempleBackdrop.cs`).
- Not done in zone 2: the lookout fight, the journal evidence board, art polish (walls are textured boxes), old objective panel still shows the old story.

## In progress
- Phase 1D core systems (see the list at the bottom of this file for what exists).

## Next (in order)
1. Finish Phase 1D: journal evidence board, checkpoints, settings (graphics presets, difficulty), menu background, lip-sync jaw driver, per-zone Volume profiles.
2. Phase 2: world scene; zone 2 (courtyard) first, then 1, 3, 5, 4, 6. Temple animation. Villagers on NavMesh. New intro.
3. Phase 3: combat depth, audio mix, polish, submission pack, push, tag `release-v3`.

## Known bugs and open questions
- Robber models are fantasy sculpts (D-04). Needs your decision.
- One loading image shows an idol being carried (D-07). Needs your decision.
- Performance has never been measured.
- The old five scenes are still the playable game until the new zones replace them (D-09).

## Phase 1D status (2026-11-16)
| System | State | Evidence |
|---|---|---|
| Puzzle framework: discs, mirrors, levers, plates, pushable blocks, valves and water level, sound lock, carry and place, shadow match, light beams and sockets; saved, resettable, three hints | Built and tested (logic only, no art yet) | `Docs/QA/v3/core_systems_tests.txt` |
| Scoring and grades, high-score table | Built and tested; no screen shows it yet | same |
| Checkpoints (save and restore position) | Built and tested; not yet called by any zone | same |
| Puzzle hints in the H guide | Wired in code; not seen in the running game yet | code |
| Graphics presets, fullscreen, mouse speed, invert look | Built; preset test passes; the new menu rows have not been looked at in the running game | same |
| Per-zone look profiles (6) | Built and tested; not placed in any scene yet | same |
| Lip sync and blinking | Built; works on Meera (visemes) and the Investigator (jaw bone); Pujari, villagers and robbers have no face rig; not yet watched with a real voice line | same |
| Journal evidence board for the inside man | NOT STARTED | |
| Menu live background on the temple | NOT STARTED (still faces a wall) | |
| Death and retry flow | NOT STARTED | |
| Lighting pipeline items (probes, reflection probes, SSAO, wet shader, decals) | NOT STARTED | |
| Performance measurement | NOT STARTED | |

Phase 1 is therefore NOT fully closed: the documents and the tested systems above are done, the five rows marked NOT STARTED remain.
