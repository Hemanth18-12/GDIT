# Known issues (honest QA, 2026-11-19)

**How this was tested:**
- One whole-game run of the built app by the test bot, from New Game to the ending with real key presses: `Docs/QA/viva_full/audit.md`.
- Focused runs for each fix: `Docs/QA/viva_speed/`, `Docs/QA/viva_ending/`, `Docs/QA/viva_ground/`.
- Runs are windowless (decision D-26), so screenshots show the 3D view but not the HUD.

**Severity:** High = blocks or badly hurts play. Medium = clearly wrong. Low = cosmetic or minor.

## Fixed today (re-tested)
| # | Where | Problem and how to reproduce | Severity | Fix | Evidence |
|---|---|---|---|---|---|
| 1 | Everywhere | Aryan walked at 1.6 m/s and ran at 4.2 m/s; it took over 5 minutes to walk to the final fight. The Animator was fed metres per second instead of 0..1, so he always played the Running clip in slow motion (moonwalking). | High | Walk 4.0, run 7.5, acceleration, Caps Lock auto-run; blend value and playback rate matched to the real speed (D-31). | `viva_speed/audit.md`: measured 4.00 and 7.50 m/s, animation ×1.00 and ×1.77; `t1-running.png` |
| 2 | Map (M) | The map listed the five old V2 scenes and had no travel. | Medium | Map lists the eight V3 places; 1–8 fast travel to reached places, not during fights, costs 2 clock minutes (D-32). | `viva_speed/audit.md`: lands 0.0 m from the storeroom point, clock +2.1 min; refuses an unreached place |
| 3 | Viva demo | No way to jump to an area quickly. | Medium | F9 demo menu (development builds or Demo mode on) (D-33). | `viva_speed/audit.md`: F9 > 8 starts the ruin-court fight with 3 robbers |
| 4 | Ending | Finishing before dawn still showed "LATE DAWN": the rule needed `kishore_turned`, a flag only the old V2 jungle fight sets. | High | The ending is "Dawn Light" when not late. | `viva_ending/audit.md`: clock 05:10, late flag 0, ending id dawn_light |
| 5 | Ending, menu | The high-score table existed but no run was ever saved to it and no screen showed it. | Medium | The run is saved when the ending appears; the ending shows its rank; Credits show the best five. | `viva_ending/audit.md`: table 0 -> 1 rows, log "rank 1" (test row then removed; the test now restores the table) |
| 6 | Main menu > Credits | The text described the old three-item story. | Low | Rewritten for V3 (five treasures, Madhav). | Code: `MainMenuController.cs` |
| 7 | Pause/Menu > Controls | Listed old keys (F inspect, G flashlight, K forensic kit, right-click block). V3 uses Q to block and F to finish. | Medium | Controls text matches the real keys in `Brawl.cs`, plus Caps Lock, map travel, horn. | Code: `ControlsText.cs` |
| 8 | Log | Warning: "The character used for Ellipsis is not available in font asset NotoSans SDF" (the only warning in the whole-game log). | Low | Two text fields switched from Ellipsis to Truncate overflow. | `viva_full/player.log` line 1027 before; 0 in `viva_ending/player.log` |
| 9 | Village ground | Very dark, flat-looking ground (your screenshot). The laterite soil texture is dark and had no tint, so at dawn it read as purple-black. | Medium | Ground tint brightened and warmed (×1.3 / 1.15 / 1.0). Partly fixed: the road surface is still dark, and the ground has no height variation. | `viva_ground/ground_before.png` vs `ground_after.png`, same camera: mean ground colour (84,44,59) -> (110,49,56), brightness 53.9 -> 62.5 |

## Still open (future work)
| # | Where | Problem | Severity | Suggested fix |
|---|---|---|---|---|
| 10 | Robbers | The robber models are fantasy sculpts (an armoured king, a winged boss), not village thieves (D-04). | Medium | New robber models (needs your decision and files). |
| 11 | Faces | The Pujari, villagers and robbers have no face rig, so their mouths do not move when they speak. | Low | Add a jaw bone or blendshapes in Blender. |
| 12 | Fights | No kick, grab or knockdown animations exist in the project. | Low | Download Mixamo clips and retarget. |
| 13 | Art | The places are textured blocks with Blender hero props, not fully modelled buildings. | Medium | Modular Blender kits. |
| 14 | Loading screen | One owner-supplied loading image shows a robber carrying an idol that looks like a real deity (D-07). | Medium (cultural) | Regenerate it showing only the brass chest. **Check this before the viva.** |
| 15 | Hints | The design says each hint costs 1 Dawn Clock minute; in code (`PuzzleGroup.NextHint`) hints cost only score. | Low | Add `DawnClock.Instance.Penalty(1f, "hint")`, or change the design text. |
| 16 | Old content | The five V2 scenes and their puzzles (lamps, deduction board, dial lock, bell traps, timeline, Sun Wall symbols) are still in the project, but the V3 story does not use them. An old help overlay (`GameIntroOverlay.cs`) still mentions "Fast-Travel between 5 Scenes"; it was not seen in V3 play. | Low | Remove the old scenes from the build list once you are sure. |
| 17 | Performance | The temple is 1.87 million triangles (LOD1 1.38 million), the largest single cost. Every place still runs above 60 fps on High on this Mac. | Low | A lighter temple mesh. |
| 18 | Build | Development build only (925 MB); no shader-stripping tuning. | Low | Release build (see `NEXT_STEPS.md`). |
| 19 | Tests | Windowless test screenshots and the video do not show the HUD (D-26). | Low | Run one windowed test for HUD pictures before the viva. |
| 20 | Sound | Sound plays are counted by the bot (1,238 plays of 29 clips) but loudness was never judged by ear. | Low | Listen once on speakers before the viva. |

| 21 | Village road | The stony road texture is still very dark at dawn. | Low | Lighter road tint or a lighter texture. |
| 22 | Final fight | In the whole-game run Bhaskar went down in phase 2; the third phase only appears if he is not finished early. | Low | Not a bug; **check before the viva** by fighting him yourself. |
| 23 | River road | The bot never caught up with Bhaskar during the chase (closest 9.9 m); the chase still ended correctly at the ruins (soft fail by design). | Low | None needed. |

## Whole-game run result (`Docs/QA/viva_full/audit.md`, build before today's fixes)
- New Game to the ending with real key presses, windowless: **story complete, 22,865 points, overall grade S, 0 exceptions, 1 warning** (item 8).
- Every place was solved with 0 hints: bazaar S, storeroom S, vaults S, stepwell S, Sun Wall S, river road A.
- All fights were won with no retries. At most 2 attackers at once. Kishore reached phase 2.
- 4 of 4 pieces of evidence; Madhav named correctly.
- 1,237 sound plays of 28 different clips.
- Not re-run start to finish after today's fixes. Instead, the ending, speed, travel and demo checks ran on the new build (`viva_ending`, `viva_ground`).
