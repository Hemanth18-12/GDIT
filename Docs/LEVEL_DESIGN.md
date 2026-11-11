# LEGACY OF THE TEMPLE — Level Design v2

Status: PHASE A document. Chapter text and line ids are in STORY_BIBLE_V2.md.

Rule for every map: it must have its own gameplay verb, a real threat, and a way to fail. No map is
"walk and collect clues".

## 1. Difficulty options

Chosen on New Game, changeable in the pause menu (applies from the next checkpoint).

| Setting | Easy | Normal | Hard |
|---|---|---|---|
| Real seconds per story minute (Dawn Clock) | 12 | 9 | 7 |
| Time penalties | × 0.5 | × 1 | × 1.5 |
| Guide marker | always available | available, costs 1 story minute per use | off in chases and the fight |
| Enemy sight range / cone | 9 m / 30° | 12 m / 35° | 15 m / 45° |
| Detection fill time (seen → caught) | 2.0 s | 1.2 s | 0.7 s |
| Counter window | 0.35 s | 0.25 s | 0.15 s |
| Aryan damage taken | × 0.7 | × 1 | × 1.3 |
| Bhaskar guard meter | × 0.8 | × 1 | × 1.25 |
| Sun Wall reset timer | 120 s | 90 s | 70 s |
| Checkpoints | all | all | no mid-fight checkpoint in phase 2 |

Normal should take a first-time player about 25 to 30 minutes, with 2 to 5 retries overall.

## 2. Map summary

| Map (scene) | Chapter | Verb | Threat | Fail |
|---|---|---|---|---|
| Courtyard | 1 | Interrogate and chase | The runner escapes; the clock | Runner gets 25 m ahead or 45 s pass |
| Sanctum (Storage Hall) | 2 | Search in the dark and relight | Flashlight battery; wrong lamp order | Budget runs out |
| ShrineCrime (Outer Shrine) | 3 | Analyse and disarm | Tripwire bell traps | Budget runs out (bells cost minutes) |
| PuzzleWall (Sun Wall) | 4 | Solve under pressure | 90 s reset; falling dust | Budget runs out |
| JunglePath (Jungle Ruins) | 5 | Sneak, chase, fight | Two alert robbers; the boss | Health reaches zero; Bhaskar reaches the ford |
| Courtyard | 6 | Ending (no gameplay threat) | None | None |

## 3. Chapter 1 — Courtyard, "3:10 AM"

- **Verbs:** talk, read a lie, sprint, vault.
- **Flow:** (1) Pujari briefing. (2) Meera, with the trust check (choice C1-A). (3) A shadow on the
  east wall triggers the chase. (4) The runner drops token No. 17; pick it up.
- **Interrogation:** during Meera's lines a small "tell" icon appears twice (she glances away). The
  player may press F once to "press the point". Pressing on the real tell ("It was dark") opens the
  choice; pressing on the wrong line makes her guarded (−1 story minute). This is the only place the
  mechanic is taught.
- **Chase:** about 35 s along the courtyard edge: around the well, between lamp posts, over a low
  wall (Space), through a market stall gap (crouch, C). The runner (Kishore, hooded) never gets caught;
  he drops the token when the player is within 6 m at the last corner, or automatically at the end.
- **Threats:** losing the runner; overturned baskets he throws down (hit = stumble, 1 s).
- **Fail:** runner more than 25 m ahead, or 45 s elapsed → "He is gone." Retry from the chase start (−2 story minutes on Normal).
- **Budget:** 25 story minutes. **Checkpoints:** after the Pujari talk; chase start.
- **Win:** token No. 17 in the Journal.
- **Different because:** it is the only map with interrogation and a daylight-free sprint through a populated space (two villagers react and step aside).

## 4. Chapter 2 — Storage Hall, "The Empty Stands"

- **Verbs:** light, search, order, deduce.
- **Flow:** (1) Enter in darkness. (2) Find three clues with the flashlight: wax-cut key, oily
  handprint, chalk mark. (3) Relight five lamps in ritual order. (4) Pujari identifies the token;
  Meera's confession (choice C2-A). (5) Deduction board.
- **Flashlight battery:** 100%, drains 1% per 1.5 s while on. Two spare cells are hidden in the hall.
  At 0% the light flickers to a weak cone (clues cannot be confirmed) until a cell is used.
- **Relight puzzle:** five wall lamps. The order is hinted by the Pujari ("East first, as the sun
  does") and by small sun-path carvings above each lamp (sunrise, mid-morning, noon, afternoon, dusk).
  A wrong lamp blows all of them out (−2 story minutes). Each correct lamp permanently lights part of
  the hall, so the room becomes safer as the player succeeds.
- **Deduction board:** drag three clues onto three conclusions. A wrong match costs 1 story minute and
  gives a spoken nudge from Aryan.
- **Threats:** the dark (clues are invisible without light), battery, the clock.
- **Fail:** budget runs out → retry from the last checkpoint.
- **Budget:** 30 story minutes. **Checkpoints:** on entry; after the third clue; after the lamps.
- **Win:** deduction solved ("Kishore opened the door. Bhaskar brought the reason.").
- **Different because:** it is the only map built around light as a resource, and the only one where the room visibly changes as you progress.

## 5. Chapter 3 — Outer Shrine, "Tracks in the Dark"

- **Verbs:** disarm, measure, match, reconstruct.
- **Flow:** (1) Cross the forecourt past four tripwire bell traps. (2) Four evidence stations:
  bootprints, red silk, broken lamp, torn note. (3) Forensic kit analysis for each. (4) Reassemble the
  torn note. (5) Pujari names the Sun Wall. Optional choice C3-A with Meera.
- **Tripwire bells:** thin threads at ankle height, visible only in the flashlight beam or moonlight
  glints. Crouch (C) next to one and hold E for a timing ring: stop the moving marker inside the green
  arc, three times. A miss or walking through rings the bell: −3 story minutes, and the flashlight
  dims for 5 s as Aryan freezes to listen.
- **Forensics (K):** bootprints: measure both lengths with a ruler tool and judge depth (the deep one
  carries the satchel). Silk: compare weave under the lens against three samples. Lamp: touch test for
  warmth to set the time ("minutes, not hours"). Note: rotate and join four torn pieces.
- **Threats:** bell traps; the clock.
- **Fail:** budget runs out → retry from the last checkpoint.
- **Budget:** 25 story minutes. **Checkpoints:** on entry; after the traps; after two stations.
- **Win:** the note is read aloud: "Ford at moonset. Sun Wall at first light."
- **Different because:** it is the only hands-on tool map: precise, quiet, close-up work with a physical trap to respect.

## 6. Chapter 4 — Sun Wall, "The Wall That Remembers"

- **Verbs:** read, turn, time, crank.
- **Flow:** (1) Read the scroll. (2) Turn the four symbol discs in order: lotus, conch, wheel, kalash.
  (3) Find and fit the hand-wheel crank in the centre socket. (4) Crank the centre while the timer runs.
- **Pressure:** the mechanism resets after 90 s (Normal). Each disc must be rotated until its symbol
  faces the sunrise slot (hold A/D, release inside the notch). The discs are spread over a two-level
  wall face, so the route between them matters. The crank is found only after the fourth disc clicks:
  a wall niche opens.
- **Crank phase:** hold E and alternate A/D in rhythm with the gear clicks for 12 s. Stopping lets the
  wheel roll back.
- **Mistakes:** wrong disc order → stone dust falls, all discs spin back, −4 story minutes. Timer
  expiry → reset, −2 story minutes. Falling dust briefly blurs the screen and hides symbols for 2 s.
- **Threats:** the timer; the dust; the clock (largest penalties in the game).
- **Fail:** budget runs out → retry from the chapter checkpoint.
- **Budget:** 35 story minutes. **Checkpoints:** on entry; when the crank is found.
- **Win:** the wall opens; "Dawn in forty minutes."
- **Different because:** it is the only pure mechanical puzzle, on a hard real-time timer, with a moving environment.

## 7. Chapter 5 — Jungle Ruins, "Before First Light"

- **Verbs:** sneak, distract, pursue, fight.
- **Section A, stealth (about 4 min):** follow the two robbers across a ruined stepped courtyard.
  Tools: crouch-walk, tall grass (hidden unless within 3 m), carved stones as cover, and pebbles (aim
  with right mouse, throw with left; the nearest robber investigates for 6 s). Listening posts: stay
  within 10 m unseen for 8 s to hear the radio call and the argument.
  - Detection meter above each robber fills when Aryan is in sight. Full = spotted.
  - Spotted the first two times: return to the last cover checkpoint (−3 story minutes).
    Spotted a third time, or at the end of the route: the story moves on to the chase.
  - If porter_path is set, Meera's chalk marks show one extra safe route and add a checkpoint.
- **Section B, chase (about 40 s):** Bhaskar and Kishore run for the brazier courtyard. Vault roots,
  slide under a fallen lintel, avoid a collapsing step. Falling behind costs story minutes, not a retry.
- **Section C, final fight:** three phases, see COMBAT_DESIGN.md.
- **Threats:** sight cones, noise (running within 8 m is heard), the boss, the clock.
- **Fail:** health zero → retry from the phase checkpoint. Bhaskar reaches the ford in phase 3 →
  retry phase 3; three failures offer the LOST DAWN ending or another try.
- **Budget:** 40 story minutes; the clock may overrun in phase 3 (LATE DAWN).
- **Checkpoints:** stealth start; each cover checkpoint (3); chase start; fight phases 1, 2 and 3.
- **Win:** Bhaskar restrained, all three objects recovered.
- **Different because:** it is the only map with enemies who look for you, and the only combat.

## 8. Chapter 6 — Courtyard at first light

Short walk (20 s) carrying the objects to the Pujari, then the ending cinematic. No threat. The
lighting state depends on the ending (first ray for DAWN LIGHT, full daylight for LATE DAWN).

## 9. Teaching order

| New skill | Taught in | Tested hard in |
|---|---|---|
| Sprint, vault, crouch-slide | Ch 1 chase | Ch 5 chase, phase 3 |
| Flashlight | Ch 2 | Ch 3 traps, Ch 5 flash stun |
| Crouch and precise interaction | Ch 3 traps | Ch 5 stealth |
| Timed inputs | Ch 3 timing ring, Ch 4 crank | Ch 5 counter and grapple |
| Block, dodge, counter | Ch 5 phase 1 (prompts on Easy and Normal) | Phase 2 and 3 |
