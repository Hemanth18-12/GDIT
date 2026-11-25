# E-key sinking — verified correction, 25 November 2026

## Reproduced failure

The user reported that E makes Aryan dip underground and rise again during tasks as well as conversations. The active `Base Layer.Interact` state in `CharacterAnimator.controller` had no motion. The state restored a collapsed Humanoid pose even though the CharacterController capsule stayed grounded.

The first headless check did not reproduce the visual issue because animation was culled outside the camera. The regression was corrected to keep Aryan visible, evaluate the animator, and measure baked skinned-mesh vertices plus the head bone across the interaction. It then failed on the real bug: feet started at -0.029 m and reached -0.883 m; head went from 1.586 m to 0.512 m; capsule root remained -0.015 m. Real E input still turned the mirror. See [before-fix XML](before-fix.xml). This explains why collision-only QA missed the complaint.

## Correction

The active interaction state now uses the existing `Clips/Hero/Interact.anim`, a CC0 Quaternius standing interaction clip already in the project. The importer has the same fallback so controller regeneration cannot silently leave that state empty. No generated animation, new download, original Blender edit or scenery replacement was used. ATTRIBUTION.md now explicitly includes this existing clip.

## Editor and build evidence

- [After-fix regression](after-fix.xml): 1 passed, 0 failed. Visible feet minimum -0.052 m, head minimum 1.533 m: about 2 cm foot movement, rather than the previous approximately 85 cm drop. The mirror still changes state from 0 to 1.
- [Final original-route suite](editor-final.xml): **5 passed, 0 failed**. Includes E pose, original layout/real mirrors/doorway, real R recovery, local retry and five audio controls.
- [Build identity](build-result.txt): **build 2026-11-25 09:52**, succeeded with **0 errors, 0 warnings**.
- [Editor during-action capture](editor-e-interaction.png) was opened and inspected. The standing character remains above the plaza floor.

## Rebuilt standalone app — real E input

The actual macOS app was launched with `-reviewEInteractions`, which stages each location, presses the real E key, captures before/during/after images and measures the pose for 2.8 seconds. [Audit](app/audit.md): **6/6 cases passed**, with the expected target selected, action working and pose remaining grounded:

| Action | Observed response | Feet drop from starting pose |
|---|---|---|
| [Sun Wall mirror](app/01-mirror-during.png) | State 0 → 1 | 2.5 cm |
| [Same mirror again](app/02-mirror-repeat-during.png) | State 1 → 2 | 2.3 cm |
| [Centre wheel](app/03-centre-wheel-during.png) | State 0 → 1 | 3.2 cm |
| [Stepwell sluice](app/04-stepwell-sluice-during.png) | State 0 → 1 | 2.2 cm |
| [Vault disc](app/05-vault-disc-during.png) | State 0 → 1 | 2.3 cm |
| [Pujari dialogue](app/06-priest-dialogue-during.png) | Conversation starts | 2.3 cm |

I opened all six during-action screenshots: the feet remain at the local floor rather than dropping into it. The wheel partly occludes the upper body in that view, but feet and lower legs remain visible; head/mesh measurements cover the whole action. Pujari dialogue uses depth of field, while the pose measurement confirms the character remains standing. These selected cases cover the shared normal interaction path; they do not prove every possible terrain position or future animation.

Preferences were exported before the standalone audits and imported afterward, so QA progress and volume changes do not replace the player's saved settings. The original temple Blender SHA-256 and modification time still match the earlier baseline. Builds remain Git-ignored.

## Completion and evaluation documents

A full six-zone scripted run of the preceding **09:32** app completed in `../2026-11-25-completion-check/story-final-build/`: story complete=True, 23325 bot-run points, grade S, and MISSION COMPLETED captured. That run preceded this animation assignment. The **09:52** fixed app was checked separately with the six focused E cases; it is not claimed as another complete story run. Scripted story QA uses controlled positioning between tasks, not uninterrupted manual traversal.

The four-page methodology report and ten-slide PPT/PDF were regenerated to describe the original locations, the separate 32/32 wider and 5/5 focused Editor checks, and this E regression. All four report pages and ten rendered slides were visually inspected. The supplied Heritage Mystery template remains untouched. Page/slide preview PNGs are retained locally under `../2026-11-25-completion-check/report/` and `slides/`. Player study, listening feedback and controlled FPS/memory benchmarking remain pending; no human results are invented.

## How to see it

Quit any older game window. Launch `Builds/LegacyOfTheTemple.app` from this project; the pause menu's build stamp should read **2026-11-25 09:52**. Press E at a task mirror, sluice, disc or NPC. Aryan makes a short standing interaction gesture and the target responds. In Unity, run PlayMode → `OriginalRouteReviewTests`. The explanatory beginner note is appended to `Docs/EXPLAINED.md`.
