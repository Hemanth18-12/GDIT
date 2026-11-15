# V3 honest audit (2026-11-16)

Evidence used:
- A real run of the built app on 2026-11-15/16 (`Docs/QA/before/run/`, 33 screenshots and `player.log`): main menu, loading screen, the full intro cinematic, the jeep check. The bot stopped after the jeep check, so the four later scenes were not replayed in that run.
- One screenshot of each later scene from the jeep test run the same day (`Docs/QA/before/s4-look-*.png`).
- Reading the code and assets (96 scripts, about 23,000 lines).
- Overview picture: `Docs/QA/before/before_sheet.png`.

Ratings are 1 (poor) to 5 (professional). "Not re-tested" means I am repeating an older QA result and did not check it again today.

## 1. Models and prefabs
| Item | Status | Visual | Evidence and notes |
|---|---|---|---|
| Investigator (Aryan) | Working | 3 | Humanoid, 16,846 triangles, hat and coat. Walks, runs, jumps, sits in the jeep (`before/run/s3-jeep-driver-front.png`). Face has no blendshapes. |
| Pujari Ananda | Working | 3 | `Priest.fbx`, 24,809 triangles. Textured. Stands and points. |
| Meera | Working | 3 | `Villager.fbx`, 89,061 triangles (heavy for a side character). Red and green saree. |
| Villagers | Partially | 2 | One male model (`Villager_Male.fbx`, same mesh as the priest) recoloured, plus the Meera model. No variety of body or clothes. |
| Robber 1 and Robber 2 | **Wrong model** | 2 | `Robber1_Real.fbx` is an armoured fantasy king with a sword; `Robber2_Real.fbx` is a winged fantasy boss. They are rigged and animate, but they do not look like village thieves (no red scarf and vest, no black hood and cross-strap). These are the only robber models in your originals, so by your rule 5 they stay; I am flagging it, not replacing them. |
| Jeep | Working | 4 | New olive SUV (2026-11-15). Drives, brakes, lights, driver seated, damage. `Docs/QA/Jeep/Final_game_sheet.png`. |
| Temple (`temple.fbx`) | Working | 4 | Source: 114.5 million triangles in 27,639 objects (a kit of about 25 buildings). Game copy `Temple_Game.fbx`: 1,867,721 triangles in 9 meshes, 258 x 115 x 62 m, tower 62 m. LOD1 1,380,293 triangles (too close to LOD0 to help). Not animated. |
| Idol_Jagannath.fbx | Working | n/a | Background shrine only, not interactable. |
| Village, roads, trees | Partially | 2 | Buildings are plain coloured boxes with flat roofs. One spline road with kerbs. One CC0 oak tree repeated; no Indian species. Flat ground, no terrain sculpting. `before/village_aerial.png`. |
| Interior scenes (Sanctum, ShrineCrime, PuzzleWall, JunglePath) | Partially | 2 | Flat single-colour walls and boxes, a few props. `before/s4-look-*.png`. |
| Props | Partially | 2 | Pots, lamps and stands are simple shapes. |

## 2. Scale, materials, colliders, LODs
- Scale: Working. Tower 62 m against a 1.8 m hero.
- Materials: Partially. Temple, characters and jeep are textured. Village and interior walls are flat colours (this breaks your "no flat boxes" rule).
- Colliders: Working for the temple (LOD1 mesh colliders) and jeep. Not re-tested elsewhere.
- LODs: Partially. Temple LOD1 exists; jeep LODs are exported but no LODGroup uses them; nothing else has LODs.

## 3. Lighting, sky, weather, post-processing
| Item | Status | Visual | Notes |
|---|---|---|---|
| Time of day and Dawn Clock | Working | 3 | Countdown HUD works. |
| Lighting and shadows | Partially | 2 | One sun per scene, strong orange and purple grade. Interiors are dark and flat. No baked light, no probes. |
| Post-processing | Partially | 2 | A colour grade is on. Bloom is visible on lamps. No depth of field outside the intro, no ambient occlusion visible. |
| Sky and fog | Partially | 2 | Gradient sky, distance fog. |
| Reflections | Missing | 1 | No reflection probes. |
| Rain | Removed | n/a | Rain was removed earlier at your request (commit 765e6b5). |

## 4. UI
| Item | Status | Notes |
|---|---|---|
| Main menu | Partially | Works and has Continue, New Game, Settings, Controls, Credits, Quit. The live background looks at a blank wall (`before/menu.png`). No graphics presets, no difficulty. |
| Loading screen | Working | The gold bar moves with the real load (30, 60, 100 % shots). **Concern:** the "robber" loading image shows a thief carrying an idol that looks like a real deity. Your own project rule and the V3 brief both say that must not be shown. I left your image in place and logged it for your decision (see DECISIONS.md, D-07). |
| HUD | Partially | Readable gold-on-dark panels. The objective panel, tutorial hint and prompt can stack up to four boxes at once (`before/s4-look-Sanctum.png`). |
| Tutorial | Working | Five small hints. |
| Guide button | Working | Small, bottom right. |
| Name tags over NPC heads | Working | These count as markers; the V3 design removes them. |

## 5. Controls, camera, movement
- Hero movement, jump, crouch: Working (not re-tested today; earlier tests in `Docs/QA/v2_playmode_results.xml`).
- Camera: Working. Orbit camera with collision.
- NPC movement: Partially. Scripted walks only; no NavMesh routines.
- Character freezing: none seen in today's intro run.

## 6. Animation
- Clips present: about 55 hero clips from the Quaternius Universal Animation Library (idle, walk, jog, sprint, crouch, jump, roll, punch jab and cross, hit reactions, death, pick up, interact, sitting, talking idle, driving, push), 11 one-hand-sword clips (OpenKit3D) and the 5 original Mixamo clips.
- Used today: idle, walk, run, jump, crouch for the hero; sword set for the robbers; point for the priest; namaste for Meera.
- Not used although present: Idle_Talking_Loop, Roll, Hit_Chest, Hit_Head, Death01, PickUp_Table, Interact, Push_Loop, Driving_Loop.
- Missing: kick, block, grab or takedown, knockdown and get-up, strafe, turn in place.
- Foot sliding: not measured today.

## 7. Lip sync and faces
- Mouth movement: **Missing.** No script touches a jaw bone or a blendshape (searched all scripts for lip sync, viseme, blendshape, jaw).
- Blinking, eye focus: Missing.
- Head turn toward the speaker: Partially (`NPCFacePlayer` turns the body).
- Talking gestures: Missing in play (the clip exists but is not wired).

## 8. Audio
- 225 audio files; an AudioManager with mixer routing; TTS voice lines for the intro.
- Not listened to in this audit (a bot cannot judge loudness). The engine loop plays at spawn even when nobody is in the jeep.
- Adaptive music: Missing.

## 9. Gameplay
- **"Press E to get the answer" is everywhere.** Clues are `ClueInspectable` objects: walk up, press E, the clue is added (`before/s4-look-ShrineCrime.png` shows "[E] Examine muddy footprints").
- 24 objective steps, about 28 dialogue lines in JSON plus intro lines; talk-heavy by the V3 budget.
- Puzzles: one brass dial lock (code 1-7-5) and an evidence timeline board. The Sun Wall puzzle was removed earlier.
- Combat: `Combat.cs` exists with sword clips. Not re-tested today. No dodge, parry, grab or enemy archetypes.
- Five separate scenes joined by doors; no connected world.
- Save: Continue exists. No per-zone checkpoints, no scoring, no high scores.
- Console: 0 exceptions in today's log.

## 10. Performance
Not measured today. No frame-time figures exist in the project. The temple alone is 1.87 million triangles, which is the main risk. Measuring is the first job of the performance pass.

## 11. Prioritised fix list
| # | Fix | Impact | Effort |
|---|---|---|---|
| 1 | Puzzle framework and removal of press-E clues | High | Medium |
| 2 | Connected world with six zones (replaces five box scenes) | High | Very high |
| 3 | Real materials on village and interiors (CC0 textures) | High | Medium |
| 4 | Lighting and post-processing per zone, reflection probes | High | Medium |
| 5 | Combat depth (dodge, block, hit reactions) with the clips already in the project | High | Medium |
| 6 | Scoring, checkpoints, grades | Medium | Low |
| 7 | Lip sync (jaw bone), blinking where the rigs allow it | Medium | Medium |
| 8 | Temple animation (flag, lamps, bell) and a real LOD | Medium | Medium |
| 9 | Menu background, graphics presets, difficulty | Medium | Low |
| 10 | Robber look (needs a decision from you, see section 1) | Medium | Blocked |
| 11 | Performance measurement and budget | High | Low to start |
