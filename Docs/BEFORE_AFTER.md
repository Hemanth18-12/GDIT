# Before and after (V3)

## Before (2026-11-16, tag `backup-v3-start`)
Overview picture: `Docs/QA/before/before_sheet.png`. Full notes: `Docs/AUDIT_V3.md`.

| Area | Before | Evidence | After |
|---|---|---|---|
| World | Five separate box scenes joined by doors | `QA/before/s4-look-*.png` | One connected place around the temple with six zones (bazaar, storeroom, vaults, stepwell, Sun Wall, river road and ruins) |
| Village | Flat-colour box houses, one tree type | `QA/before/village_aerial.png` | Same village, plus market stalls along the courier's road; blocks now draw through shared materials |
| Discovery | Walk up, press E, clue is given | `QA/before/s4-look-ShrineCrime.png` | Every answer is earned physically: carry, fit, turn, push, place (no 'examine' prompts on the V3 path) |
| Puzzles | One dial lock, one timeline board | code | Five physical puzzles and a stealth tail, built on a tested puzzle framework (15 tests) |
| Dialogue | Long conversations, 24 objective steps | `QA/before/dialogue_two_shot.png` | 9 steps, 29 short spoken lines, never more than two in a row, with voice |
| Jeep | New olive SUV, drives and breaks | `QA/Jeep/Final_game_sheet.png` | Unchanged look; drives the river-road chase; bot chase without damage |
| Combat | Sword swings, no dodge, parry or enemy types | code | Light combo, heavy, dodge, block and parry, finisher, at most two attackers, guarding, three-phase final boss |
| Faces | No lip sync, no blinking | code search | Lip sync and blinking for Aryan (jaw bone) and Meera (visemes); others have no face rig |
| Menu | Background faces a blank wall, no graphics presets | `QA/before/menu.png` | Live view of the real temple at golden hour; graphics presets, mouse speed, invert look |
| Loading | Bar moves with the real load | `QA/before/loading_60pct.png` | Unchanged (bar moves with the real load) |
| Scoring | None | code | Points and S/A/B/C grade per place and overall; high-score table; shown at the ending |
| Performance | Not measured | none | Every place above 60 fps on High (8.5 ms worst average, was 40.6 ms) |


## After (2026-11-17)
Side-by-side picture: `Docs/QA/v3/before_after_sheet.png`. Evidence for each line: `Docs/TEST_REPORT.md` and `Docs/TECHNICAL_REPORT.md`.
