# Legacy of the Temple: test report

All results come from real runs on 2026-11-16 (Unity 6000.6.8f1, macOS, the built `Builds/LegacyOfTheTemple.app`). Nothing here is a self-assigned score.

## 1. Automated play-mode tests (15 of 15 pass)
Run with `zsh Tools/dev/pt.sh "PuzzleFrameworkTests;CoreSystemsV3Tests;JeepPhysicsTests"`.

| Test | What it proves |
|---|---|
| Logic_Rules | Disc steps, water level sums, sound-lock order, mirror reflection, push direction |
| Discs_Solve_Lock_Reset_And_Save | A puzzle is solved only when every part is right; reset works; solved parts stop moving; states survive a reload |
| Hints_Three_Tiers_Cost_Score | H gives three hints in order; each is counted against the score; a solved puzzle gives no more |
| Plate_And_Block | Blocks move one step, stop at walls, reset from a corner; a block on the plate opens it |
| Water_Level_Puzzle_Has_An_Aha_And_No_Lockup | Both sluices flood past the door; draining back to the mark solves it; it never locks |
| Sound_Lock_Wrong_Order_Restarts_Quietly | A wrong strike restarts the order without a fail |
| Carry_Place_And_Shadow_Match | Wrong pieces do not fit; pieces can be taken back; the lamp counts only when put down on the spot |
| Light_Beam_Bounces_Off_Mirrors_To_A_Socket_And_Is_Blocked | A beam bounces off a turned mirror to a socket; a spoke in the way blocks it |
| Score_Grades_And_High_Scores | Points, grades S/A/C, high-score order, New Game keeps the table |
| Checkpoint_Save_And_Restore | Position, facing, zone and clock are restored |
| Evidence_Board_Shows_Only_What_Was_Found_And_Names_Once | Only found evidence is listed; naming needs three pieces; the choice cannot be changed |
| Graphics_Presets_Apply_And_High_Is_Default | High is the default; Low and Ultra change shadows and effects; 60 fps cap |
| Six_Zone_Looks_Are_Distinct_And_Use_ACES | Six local volumes, each a different grade, all ACES |
| Faces_Lip_Sync_And_Blink_On_The_Real_Models | Investigator jaw opens with a voice; Meera has visemes; both blink; Pujari and robbers have no face rig (reported, not faked) |
| Jeep_RestsOnFlatGround_NoHop | The jeep settles without hopping or tipping |

## 2. Whole-game run in the built app
Run with `zsh Tools/dev/v3bot.sh -v3Story Docs/QA/v3/story_run`. The bot starts a New Game and plays to the ending with real key presses (E, Space, mouse buttons through the game's own input), then writes `audit.md` and screenshots.

| Step | Result |
|---|---|
| Old scene doors | 0 usable (the V3 path stays around the temple) |
| Gate, priest | Objective advances to the bazaar after two lines |
| Goal card | Fades to 0 within 9 seconds of a new goal |
| Bazaar tail | Spotted once on purpose at 6 m in the open; then followed at a distance to the yard; street fight won; Dawn Bell and the list taken |
| Storeroom | Wrong order first (flagstone, lantern) does nothing; shards, lantern, lock plate, flagstone in order; lookout fight won; Chronicle taken |
| Vaults | Block pushed into a corner, reset lever restores both; blocks on plates open the gate; discs turned to the floor pattern; two guards beaten; Rosette taken |
| Stepwell | Panel stays shut early; both sluices flood to level 4; two drain turns bring level 2 and the raft to the panel; knife man and helper beaten; Vessel taken |
| Sun Wall | Four mirrors set; one beam still blocked by a spoke; pushing the wheel lights both carvings; Kishore reaches phase 2 and is beaten; case taken |
| River road | Jeep moved to the road start; bot drives without damage; three waves; at most two attackers at once; Bhaskar reaches phase 3; Madhav gives himself up; Finial taken |
| Ending | Story complete; 5 of 5 treasures; all four pieces of evidence; Madhav named correctly; every place graded |
| Errors | 0 exceptions in the player log |

**Which build:** the final build (commits after aa0919e) passed the whole game twice with no window: `Docs/QA/v3/story_final/audit.md` (story complete, grade S, 0 exceptions, jeep chase without damage, Bhaskar phase 3, Madhav at four places) and the recording run `Docs/QA/v3/story_rec/audit.md` (same result, plus the bell, 8 lamp stands and 6 reflection probes placed). Windowless runs use `-batchmode`; their screenshots are rendered from the camera and do not show the HUD.

Gameplay video: `Docs/QA/full_run.mov` (4 min 31 s, the whole game played by the bot, shown 1.7 times faster, no HUD). Beauty shots: `Docs/QA/v3/beauty_sheet.png` (22 pictures).
Screenshot sheets: `Docs/QA/v3/story_sheet.png`, `zones_sheet.png`, `zones_1_6_sheet.png`, `art_pass_sheet.png`, `madhav_sheet.png`, `hud_after.png`.

## 3. Sound effects (2026-11-17)
The windowless whole-game run counted every 3D sound effect that played: 1,238 plays of 29 different clips, 0 missing clips (`Docs/QA/v3/story_final/audit.md`).
- Fights: swings (quick 110, wide 25), hits (light 68, heavy 58), grunts (robbers 105, Bhaskar 21, Aryan 13), staggers 54, body falls 13, blocks and parries, dodges, cloth rustle.
- Footsteps of robbers, the courier, Bhaskar and walking villagers, by surface (mud 470, gravel 105, stone 69).
- Puzzles and doors: stone grinding (discs, blocks, wheel) 25, clicks (mirrors, valves, plates) 14, brass set 4, stone doors and gates 4, water 4, lantern lighting, the Sun Wall opening, the bell.
- Talking villagers play indistinct chatter while they gesture (heard within about 11 m).
Windowless runs count the sounds but cannot listen to them; loudness was not judged by ear.

## 4. Performance
See `Docs/TECHNICAL_REPORT.md` section 6. After the batching fix every place runs well above 60 fps on High (slowest place 8.5 ms average, 11.2 ms for the slowest 1 % of frames).

## 5. Problems found by testing and fixed
| Found | Fixed by |
|---|---|
| Skipping the intro threw the jeep into the air | Taking the driver out of the seat before collision returns |
| The jeep had no Rigidbody in the Editor (`??` on a Unity object) | Plain null checks |
| Brakes were stronger at the back | Front-biased brake split |
| A lantern with no oil could still be picked up | Disabled parts ignore direct use |
| The lantern on the stain stole the E press from the lock plate | Lock plate moved to the other door post |
| A false "section not available" box during V3 places | V3 places count as always built |
| The ending counted the Sun Wall's empty case as a sixth treasure | Excluded from the treasure count |
| Madhav looked identical to the Pujari (same body) | Indigo kurta, red turban, shawl |
| The jeep struck a fallen log in the chase | The bot keeps to the free side; the road is visual only |
| 17 to 40 ms render-thread cost | Baked materials (section 3) |

## 6. Known issues (not fixed)
- Robber models are fantasy sculpts (the only robber models in the originals).
- The Pujari, villagers and robbers have no face rig, so their mouths do not move.
- No kick, grab or knockdown animations exist in the project.
- Places are textured blocks with hero props, not fully modelled environments.
- One loading image shows a carried idol; it is the owner's artwork and was left in place (decision D-07).
- Windowless screenshots and the gameplay video do not show the HUD (the camera is rendered off screen).
- Two beauty shots are poorly framed (`beauty-stepwell-wet-stone.png`, `beauty-temple-low.png`).
