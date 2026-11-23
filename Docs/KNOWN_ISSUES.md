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

## Found while making the walkthrough (2026-11-19, windowed run `Docs/Walkthrough/raw/audit.md`)
| # | Where | Problem and how to reproduce | Severity | Suggested fix |
|---|---|---|---|---|
| 24 | B key | B opens **two panels at once**: the old V2 "CODEX" (with "Trust (Meera)") and the V3 "WHAT I KNOW" evidence board. Pressing B again closes the codex but the board stays open and then overlaps the guide (H). Reproduce: New Game, press B, press B, press H. Picture: `Docs/Walkthrough/shots/L0_09b_evidence_board.png`, `L0_08_guide.png`. Cause: `StoryDialogueSystem.cs` line 328 and `EvidenceBoard.cs` line 49 both react to B. | Medium | Remove the old codex toggle from `StoryDialogueSystem` (V3 does not use it). |
| 25 | Main menu | The temple behind the menu draws as **broken spiky shards**, not a temple. Picture: `L0_01_menu.png`, `L0_02_controls.png`. The same broken shapes show on the temple far away in some in-game views. | High (first impression) | Check the temple mesh or LOD used by `MenuTempleBackdrop.cs`; use the full model or a clean low-detail copy. |
| 26 | Madhav | His **whole model is tinted indigo, skin included** (he looks blue), and his "shawl" is a **box floating** across his chest. Picture: `C_04_madhav.png`. | Medium | Tint only the clothing material; attach the shawl to the spine bone or remove it. |
| 27 | Settings on this Mac | The pause menu shows **Volume: 0%**, so the game is silent here. This is a saved setting, not a code bug. Picture: `L0_09_pause.png`. | Note | Raise Volume in Pause > settings before the viva. |
| 28 | Docs | `Docs/QA/v3_map_overview.png` is the design plan ("not to scale"); the real places differ (D-15). | Low | Use `Docs/Walkthrough/shots/map_plan_from_coordinates.png`, drawn from real coordinates. |

## Voice-over (2026-11-23)
| # | Where | Problem | Severity | Suggested fix |
|---|---|---|---|---|
| 29 | 9 clips | Whisper heard 9 clips slightly differently from the script (accent sounds and homophones, for example "Two." heard as "Dude.", "Got it." as "God it."). They may sound fine; nobody has listened yet. List: `Docs/QA/voice/summary.txt`. | Low | Listen in `VoiceTestScene`; if one sounds wrong, put a recording named `<line id>.wav` in `Assets/_Project/Audio/Voice/Recorded/` and run the generator. |
| 30 | Synthetic voices | All 166 clips are text-to-speech (Kokoro). They are clear and levelled, but not acted. | Note | For a public release, record real voice actors (with their consent); the CSV and the Recorded folder are ready for it. |
| 31 | Triggers the bot never reaches | A clean bot run never asks for hints, never runs low on the Dawn Clock, never loses a fight, never makes a wrong puzzle move and never drives near a wall at speed, so `hint_1-3`, `clock_5`, `clock_1`, `clock_penalty`, `fight_retry`, `puzzle_wrong`, `jeep_near_miss`, `danger_drop`, `run_tired` and `bazaar_far` did not fire there. The rain remark needs 20 s at the stepwell; the bot stays less. `jeep_engine_dead` needs a destroyed jeep, and the bot keeps it drivable for the story. | Note | All of them speak in the play-mode test `Every_Trigger_Can_Speak` and in `VoiceTestScene`; a human play-test should also hear them. |
| 32 | Faces | Only Meera has mouth shapes and only Aryan a jaw bone; the Pujari, Madhav, robbers and villagers only nod while they talk (their models have no face rig, so no mouth and no blinking). | Low | Add blend shapes or a jaw bone to those models in Blender. |
| 33 | Startup video | The rendered intro video keeps its old baked voices (D-50). | Low | Re-render the video with the new clips. |
