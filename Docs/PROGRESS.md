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

## Phase 1D status
(updated as each system lands)
