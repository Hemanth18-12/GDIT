# Current targeted fix pass — 23 November 2026

Resume here; older entries below are historical. New brief: targeted checks only; commit each task; make one final build.

- Backup: `backup-fixpass` created. Existing user changes and CarFootball project preserved.
- Task 1 VERIFIED: removed the 1 m climb clamp; 40 cm steps; actual courtyard/jungle stairs passed Play Mode and courtyard passed standalone keyboard check. Evidence: `Docs/QA/2026-11-23-review/playmode-round3.xml`, `editor/`, `app-look/audit.md`. Both stepwell ramps, one-metre ledge hop and blocked-wall recovery also passed: `fixpass-traversal.xml`. Final standalone screenshots of the added bridge remain for the final build.
- Task 2 PARTIAL: blocked-driver-door exit passed with feet at y=0.005 m; capsule-based entry and safe exit search added. Remaining: roof contact and transition height evidence.
- Task 3 PENDING: existing hit-stop/slow motion/damage reactions retained; add reusable impact/trail/flash feedback and test a representative encounter.
- Task 4 PARTIAL: plain NPC voice hook fixed; missing clips are subtitle-only. Eight existing voice tests and new hook test pass.
- Tasks 5–6: existing sound/mixer/music and lighting corrections present; targeted evidence/mix checks remaining.
- Task 7: puzzle framework checks passed; targeted six-zone puzzle checks pending.
- Task 8 PENDING: sourced Heritage Notes, loading fact and learned summary.
- Task 9 PENDING: combined `Docs/LEGACY_REPORT_AND_GUIDE.pdf`, all-page visual validation. Earlier report builder is an unfinished draft and has not produced a PDF.
- Final build NOT YET MADE under the new brief. Earlier build succeeded with 0 errors/0 warnings; do not treat it as final.

---

# Progress (resume file)

## Voice-over system (started 2026-11-23) - resume here
Say "continue the voice-over from Docs/PROGRESS.md".
- [x] Tag `backup-voice`. Audit: `Docs/VOICE_AUDIT.md`.
- [x] 166 lines in `Docs/VOICE_LINES.csv` (133 new event lines + 33 story and intro lines), 73 event triggers.
- [x] Clips made with Kokoro-82M (`Tools/tts/generate_voices.py`), levelled to -16 LUFS, checked by Whisper (`Docs/QA/voice/summary.txt`).
- [x] `VoiceDatabase` asset (`Tools > Voice > Build database from CSV`), `VoiceManager` (queue, priorities, cooldowns, repeat gap, subtitles, lip sync, 3D, pause, recording-aware clock), `VoiceWatchers` (all triggers), helpers `VoiceTrigger` and `VoiceOnEvent`, settings (Voice lines Low/Normal/High, Subtitles), `VoiceTestScene`.
- [x] Play-mode tests `VoiceSystemTests`: 8 of 8 pass.
- [x] Bot runs of the whole game found watcher bugs (D-53, D-57), all fixed.
- [ ] Final whole-game run with the trigger log (`Docs/QA/voice_trigger_log.txt`) on the final build. (The run on the previous build: 69 event + 27 story lines, 42 of 73 triggers spoke, 0 overlap frames, 0 missing clips, 0 exceptions, story complete grade S; its file was overwritten by the restarted run, which was stopped.)
- [x] Demo video `Docs/QA/voice_demo.mov` (64 s, 1280x720, the game's own sound, -21 LUFS, no clipping); Whisper heard 8 of the 10 lines in it (`Docs/QA/voice_demo_transcript.txt`).
- [x] `Docs/VOICE_GUIDE.md` (with viva answers), EXPLAINED note, KNOWN_ISSUES 29-33, DECISIONS D-41 to D-58.
- [ ] Final build, commit, push.

## V3 progress (earlier)

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

## Finished (2026-11-17)
- Final whole-game run on the final build, windowless: `Docs/QA/v3/story_final/` and `Docs/QA/v3/story_rec/`.
- Gameplay video `Docs/QA/full_run.mov`; beauty shots `Docs/QA/v3/beauty_sheet.png`.
- Temple life (bell, lamp stands), reflection probes, wet stone, villager routines.
- Tag `release-v3`.

## Next (optional)
- Better models for the robbers and face rigs for the Pujari, villagers and robbers (needs your files or a decision).
- Kick, grab and knockdown animation clips (needs Mixamo downloads).
- More modelled environment art.

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
