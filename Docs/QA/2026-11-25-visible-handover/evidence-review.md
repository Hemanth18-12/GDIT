# Visible history in the original game — 25 November 2026

The previous feature was restricted to TempleRing, while the normal game opens Courtyard. This revision adds the history to the original route without replacing its locations or puzzle rules.

## Final runtime evidence

- Rebuilt `Builds/LegacyOfTheTemple.app`: **build 2026-11-25 22:08**.
- BuildReport: **Succeeded: errors=0, warnings=0** (`build-result.txt`).
- `app-final/checks.txt`: **ALL CHECKS PASSED**, including all six original-route history handovers.
- Actual E pickup starts a three-second countdown, returns to the existing original Pujari and starts the dialogue automatically. No T or extra priest E is used in this test.
- The visible `SKIP LEVEL [K]` button and K alternate across later levels; both recover the skipped zone's piece and trigger the same history.
- Narration automatically advances to Ananda. Each zone checks the real playing `Priest_Talk` clip, visible history title, actual historical paragraph in rendered TMP text, two choice paths, saved delivery and exactly 500 bonus points.
- The script positions the player and stages the first pickup with its encounter marked complete. It tests the production E input and automatic handover rather than claiming an uninterrupted manual puzzle playthrough.

## Images personally inspected

- `app-final/01-visible-skip-button.png`: original world and the visible SKIP LEVEL [K] button above the minimap.
- `app-final/02-skip-return-countdown.png`: original Pujari/location retained; return countdown shown after skip.
- `app-final/history-zone-1.png` through `history-zone-6.png`: Ananda and Aryan at the original temple, titled historical text panel and voiced subtitle dialogue. The title panel shows the full introductory fact while the lower subtitle types in. Facts now lead the spoken dialogue; the fictional-replica note is secondary.
- `app-final/09-original-route-mission-complete.png`: all six deliveries and explanations recorded.

## Editor and voice checks

- `history-final.xml`: **2 passed, 0 failed**, covering the complete original automatic flow and the existing TempleRing delivery/reload checks.
- `round2.xml`: the five existing OriginalRouteReviewTests passed; the added review's initial scene-load wait failed when run after another test and was corrected. `round3.xml` and later handover runs pass. Earlier failures are retained.
- No C# compiler diagnostics or managed exceptions found in the final test/player logs.
- Voice database rebuilt: **204 lines, 0 missing clips**. Eighteen updated historical explanations generated offline with existing Kokoro. All 38 lore clips validated as mono 44.1 kHz WAVs longer than 0.5 seconds (`voices.csv`). ASR/pronunciation assessment was not run.
- History references are in ATTRIBUTION.md. No new art, models, animation clips or software were downloaded.

## Scope and limits

Normal puzzles, movement, world layout and models are retained. Skip resolves a task only on the player's request. Existing imported Interact/Point/Talk clips supply the gestures; no new animation was generated. No physical sacred image is handled; recovered pieces remain fictional teaching props.

The build report contains zero warnings, but the existing pre-build LoadingImageTools step still logs four absent legacy loading-image references. These are separate from the BuildReport count and are not claimed fixed. The feature's final runtime/test logs have no missing-clip warnings.

Saved player preferences/progress were backed up before QA and restored after the final standalone run. Quit an old running app and use the 22:08 build to see the change.
