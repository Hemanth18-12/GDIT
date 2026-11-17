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

## Zones 3, 4, 5 (2026-11-16)
- Sun Wall (`SunWallPuzzle.cs`), Stepwell (`StepwellPuzzle.cs`) and Vaults (`VaultPuzzle.cs`) are built in the Courtyard scene around the temple and were each played end to end by the bot with real key presses: `Docs/QA/v3/zones_run/audit.md` (4 zones, 14,000 points, grade A, 0 exceptions), pictures `Docs/QA/v3/zones_sheet.png`.
- Evidence board on B (`EvidenceBoard.cs`): lists only found evidence, lets the player name the inside man once. Logic test written; the panel has not been looked at in the running game.
- Not done in these zones: fights (lookout, guards, knife man, Kishore), vault patrol stealth and lantern battery, art polish, sound.

## V3 is now the main story (2026-11-16)
- `Resources/Story/story.json` rewritten (old version kept at `Docs/story_v2_backup.json`): 7 steps (reach the temple, priest, storeroom, vaults, stepwell, Sun Wall, return), 12 short spoken lines. Old saves from the 24-step story start fresh. Doors to the old scenes are closed.
- Every zone has its fight: lookout (courtyard), two vault guards, knife man and helper (stepwell), Kishore with two phases (Sun Wall). `Brawl.cs`: light 3-hit combo, heavy, dodge roll, block and parry, finisher with slow motion, at most two attackers, enemies guard after three hits, retry on the spot.
- HUD: bottom help bar removed, goal card smaller and fades after 7 s, tutorial hints move on after 10 s, smaller lighter boxes.
- Bot played the whole path from New Game to "Mission Completed": `Docs/QA/v3/story_run/audit.md`, pictures `Docs/QA/v3/story_sheet.png`, 0 exceptions, grade S.
- No sound on 2026-11-16 was the Mac's own output being muted, not the game.


- Phase 1D core systems (see the list at the bottom of this file for what exists).

## Next (in order)
1. When the screen is free (not while studying): one full whole-game run on the final build (`zsh Tools/dev/v3bot.sh -v3Story Docs/QA/v3/story_run`) and the gameplay video (`zsh Tools/dev/v3bot.sh -v3Story <folder> -recordFrames <frames folder> -recordSeconds 290`, then ffmpeg to `Docs/QA/full_run.mov`). Both open a game window.
2. Then tag `release-v3` and push the tag.
3. Optional: more environment art (the places are still textured blocks with hero props).

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
