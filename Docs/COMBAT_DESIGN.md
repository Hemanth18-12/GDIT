# LEGACY OF THE TEMPLE — Combat, Stealth and Chase Design

Status: PHASE A document. Tone: stylised, readable, no gore, nobody dies. Enemies are **subdued**.
The fight takes place in the jungle ruins' brazier courtyard, away from any shrine.

## 1. Controls

Combat mode switches on automatically in the fight arena and off afterwards. Outside combat, Space
is still jump and the mouse buttons do nothing.

| Action | Keyboard / mouse | Gamepad | Notes |
|---|---|---|---|
| Move | WASD | Left stick | Camera soft-locks on the current target |
| Switch target | Tab | Right stick click | Only in two-on-one |
| Light attack | Left mouse | X | 3-hit chain |
| Heavy attack | Right mouse (hold up to 0.6 s) | Y | Breaks a block; slow |
| Block | Q (hold) | LB | Drains stamina per hit blocked |
| Counter | Tap Q just before a hit lands | Tap LB | Window by difficulty |
| Dodge | Space + direction | B | Short invulnerable step |
| Flashlight flash | G | RB | Stun; 12 s recharge |
| Environment | E near a prop | A | Brazier, rope |
| Grapple prompts | E / A-D alternate | A / stick | Phase 3 only |

Stealth and chase controls reuse existing keys: C crouch, Shift run, Space vault, right mouse aim
pebble, left mouse throw.

## 2. Aryan

| Stat | Value (Normal) |
|---|---|
| Health | 100. Regenerates to 40 if below it, after 6 s without damage. |
| Stamina | 100. Regenerates 25/s after 1 s without spending. |
| Light attack | 6 guard damage, no stamina cost, chain of 3 (third hit = small stagger) |
| Heavy attack | 16 guard damage, 20 stamina, breaks block |
| Block | Stops 100% of fist/kick damage and 70% of blade damage; 15 stamina per hit. At 0 stamina the guard breaks (1 s stagger). |
| Counter | Negates the hit, 20 guard damage, opens a 1.5 s punish window |
| Dodge | 22 stamina, 0.3 s invulnerable. A dodge within 0.15 s of impact is a "perfect dodge": time slows for 0.4 s. |
| Flash | Cone 8 m, 40°. Stuns 2.0 s (Bhaskar 1.2 s in phase 3). No damage. |

Aryan has no weapon. He disarms, trips, restrains.

## 3. Enemies use a Guard meter, not a health bar

Each enemy has a **Guard** meter. Hits, counters and environment tricks drain it. At zero the enemy is
*staggered* and a finisher prompt appears (E): a disarm or takedown animation. That ends a phase or
subdues the enemy. This keeps the fight non-lethal and makes every phase end on a clear beat.

| Enemy | Guard | Attack damage |
|---|---|---|
| Bhaskar | 120 per phase | Slash 18, wide swing 26, lunge 30, kick 10 |
| Kishore (two-on-one only) | 60 total | Shove 6, wild swing 10 (a wooden staff) |

## 4. Enemy AI

### States

`Idle → Circle → Telegraph → Attack → Recover → (Circle)`, plus `Blocked`, `Stunned`, `Staggered`,
`Taunt`, and in phase 3 `Flee`. Stealth uses `Patrol → Suspicious → Search → Alert`.

### Bhaskar — moves and telegraphs

Every attack has a visual and an audio tell. Unblockable attacks have a red blade glint; blockable
ones a white glint.

| Move | Phase | Telegraph (time before hit) | Answer |
|---|---|---|---|
| Quick slash | 1+ | Blade drawn back at the hip, white glint, short swish (0.45 s) | Block or counter |
| Double slash | 1+ | Same, plus a step forward (0.45 s, 0.35 s) | Block both, or counter the second |
| Kick | 1+ | Shoulder turn, grunt (0.4 s) | Block; it pushes Aryan back |
| Wide swing | 2+ | Blade raised high across the body, red glint, long swish (0.7 s) | Dodge. Cannot be blocked. Leaves him open 1.2 s. |
| Lunge | 2+ | Bark "One more step!", low stance (0.8 s) | Sidestep; if he misses near a brazier he stumbles into it |
| Scarf feint | 2+ | Flicks the red scarf, then a delayed slash (0.9 s) | Wait, then counter. Punishes panic blocking. |
| Satchel swing | 3 | Grabs the satchel strap two-handed (0.6 s) | Dodge back |

Behaviour rules: he circles at 2.5 to 3.5 m; attacks every 1.6 to 2.4 s (faster each phase); never
attacks while Kishore is mid-attack (so two-on-one stays fair); taunts if the player stays beyond 6 m
("Come closer!") and then closes the distance.

### Kishore — only if trust_meera ≤ 1

Hesitant. Long telegraphs (0.9 s), attacks only every 4 to 5 s, backs away when hit, never attacks
from behind without a shout ("I do not want this!"). Subduing him (guard to zero) plays his surrender
line and he drops the manuscript. He is designed as pressure, not as a second boss.

## 5. The three phases

**Arena:** a ruined stepped courtyard about 14 m across. Four braziers at the corners, a rope holding
a bundle of roof poles over the centre, wet stone floor, broken pillars for cover. Exit steps lead
down to the ford.

| Phase | What happens | New enemy moves | Player tools introduced | Ends when |
|---|---|---|---|---|
| 1 "Enough, Bhaskar!" | Duel, or two-on-one if trust is low. If trust is high, Kishore turns at the end of this phase. | Quick slash, double slash, kick | Block, counter, light chain (prompts on Easy/Normal) | Bhaskar's guard hits zero: Aryan knocks him back into a pillar. Checkpoint. |
| 2 "It is not yours to sell!" | He fights wide and angry. Braziers and the rope become usable. | Wide swing, lunge, scarf feint | Dodge, flash stun, brazier, rope | Guard zero: Aryan disarms the blade (finisher). Checkpoint (not on Hard). |
| 3 "You cannot carry what it means!" | Bhaskar grabs the satchel and runs for the ford. A 30 s sprint across crumbling steps, then the grapple. | Satchel swing, thrown debris | Sprint, vault, final grapple | Grapple won: Bhaskar restrained. |

**Phase 3 detail.** The steps crumble behind Bhaskar: three vault or slide prompts. He throws two
loose stones back (sidestep). At the ford's edge Aryan catches the satchel strap: a tug-of-war
(alternate A/D to fill a bar while Bhaskar pulls back), then a single timed E press to twist the
strap and bring him down. Missing the timed press lets him break free: one more 10 s sprint and a
second try. If he gets 20 m ahead or reaches the water, the phase restarts; three restarts offer the
LOST DAWN ending.

## 6. Environment tricks

| Trick | How | Effect | Limit |
|---|---|---|---|
| Knock a brazier | E beside a brazier while an enemy is within 3 m on the far side | Embers scatter: enemy recoils, 25 guard damage, 1.5 s stagger. No burning-person visuals. | Each brazier once; 4 in the arena |
| Lunge into a brazier | Sidestep a lunge with a brazier behind Aryan | Same as above, and bark "Not today!" | Same braziers |
| Pull the rope | E at the rope cleat when an enemy stands under the pole bundle (marked by a shadow) | Poles drop: 40 guard damage, enemy knocked down 2 s | Once per fight |
| Flashlight flash | G facing the enemy | Stun. A flash during a telegraph cancels the attack. | 12 s recharge |
| Wet stone | Passive: heavy attacks near the puddles make the enemy slip (+0.3 s recovery) | Rewards positioning | — |

## 7. How the items are recovered

| Object | Carried by | Recovered when |
|---|---|---|
| Tala Patra (manuscript) | Kishore | Trust high: he throws it to Aryan at the end of phase 1 (catch prompt; a miss means it lands safely on the steps). Trust low: he drops it when subdued in phase 2. |
| Surya Pradeepa and Padma Shila | Bhaskar's satchel | The satchel falls open after the phase 3 grapple. A short interaction shows each object being lifted out and wrapped. |

The objects are never used as weapons and never shown damaged.

## 8. Stealth and chase system (shared, small)

- **Senses:** sight cone and range by difficulty (LEVEL_DESIGN.md); hearing radius 8 m for running, 3 m for walking, 0 for crouching.
- **Detection meter:** fills while Aryan is visible; drains at half speed when hidden. Yellow at 50% (enemy turns to look: `Suspicious`), red at 100% (`Alert`).
- **Cover:** tall grass hides Aryan unless an enemy is within 3 m. Carved stones block sight by raycast.
- **Pebble:** unlimited, 1.5 s cooldown. Enemies walk to the sound, look for 6 s, return. The same enemy ignores a third pebble within 20 s (and becomes `Suspicious`).
- **Chase:** the quarry follows a spline at a speed tied to the player's distance (rubber band between 8 and 18 m). Obstacles require Space (vault), C (slide) or a sidestep. A stumble costs 1 s.

This reuses the existing JungleStealth look-back logic as the base.

## 9. Feedback

Hit-stop of 0.06 s on heavy hits and counters; a short camera shake; a controller rumble pulse; a
bright spark on blade-to-block; a slow-motion beat on perfect dodge and on each finisher; heartbeat
audio and vignette when health is below 30. Subtitles show barks. All flashes respect a "reduce
flashes" accessibility toggle.

## 10. Animations needed (download from Mixamo later)

Download as **FBX for Unity, Without Skin** (except one "With Skin" per rig if needed), **In Place**
ticked where offered, 30 fps. Names below are Mixamo search terms; the exact clip titles must be
confirmed on mixamo.com when downloading. Do not generate or fake any of these.

| Use | Mixamo search term |
|---|---|
| Aryan combat idle | Fighting Idle |
| Aryan light 1, 2, 3 | Jab Cross; Hook Punch; Elbow Punch |
| Aryan heavy | Cross Punch (or "Mma Kick" as an alternative) |
| Aryan block idle and block hit | Body Block; Block Idle |
| Aryan dodge left / right / back | Dodging Left; Dodging Right; Standing Dodge Backward |
| Aryan counter / disarm | Sword Disarm (or "Armada To Esquiva" as fallback); Arm Bar / Takedown |
| Aryan hit reactions | Head Hit; Stomach Hit; Big Hit To Head |
| Aryan knocked down and getting up | Knocked Down; Getting Up |
| Aryan flashlight raise | Pointing Forward (upper-body layer) |
| Aryan pebble throw | Throw Object |
| Aryan vault, slide | Running Jump; Running Slide |
| Aryan crouch walk | Crouched Walking; Crouch Idle |
| Aryan grapple pull | Pulling A Rope |
| Aryan kick brazier | Kicking (or "Push") |
| Aryan catch manuscript | Catching |
| Bhaskar blade idle | Sword And Shield Idle (or "Great Sword Idle") |
| Bhaskar quick slash, double slash | Sword And Shield Slash; Stable Sword Outward Slash |
| Bhaskar wide swing | Great Sword Slash |
| Bhaskar lunge | Sword And Shield Attack (thrust) |
| Bhaskar kick | Sword And Shield Kick |
| Bhaskar blocked / stunned / staggered | Sword And Shield Impact; Stunned; Standing React Large From Front |
| Bhaskar taunt | Taunt |
| Bhaskar run with satchel | Running (carry) / Fast Run |
| Bhaskar restrained | Kneeling Idle; Defeated |
| Kishore nervous idle | Nervously Look Around |
| Kishore staff swing, shove | Standing Melee Attack Horizontal; Push |
| Kishore tackle | Football Tackle |
| Kishore throw manuscript | Throw Object |
| Kishore surrender | Surrender / Hands Up; Kneeling Down |
| Both robbers sneaking walk, look back | Sneaking Forward; Looking Behind |
| Chase runner | Fast Run; Jump Over |

## 11. Known blockers for Phase C

- **The Robber1 and Robber2 prefabs currently show the wrong models** (winged, armoured figures with
  swords, seen in Docs/QA/scale_check.png). The real Bhaskar and Kishore models from the Heritage
  Mystery Project must be re-imported, read-only from the originals, before any combat work.
- The robbers' Animators have no humanoid avatar assigned; Mixamo retargeting needs humanoid rigs.
- A curved blade prop and a satchel prop are needed (own Blender work or CC0).
