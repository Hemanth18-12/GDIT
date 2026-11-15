# Legacy of the Temple — V3 level design

One connected world, about 700 m by 600 m, with the temple in the middle. Six zones, each with its own palette, light, sound, puzzle type and fight. Map picture: `Docs/QA/v3_map_overview.png`.

## World layout
```
                         N
        (6) JUNGLE ROAD AND RUINS ......... river ghat, buyer's boat
          |   dusk mist, torches                 ~~~~ river ~~~~
          |
   (5) SUN WALL AND            (4) STEPWELL AND
   CHARIOT-WHEEL PLAZA ------- MONSOON SHRINE
   west, open, midday          east, sunk in the ground, night rain
          \                        /
           \   (2) TEMPLE GATE    /
            \  AND COURTYARD     /        (3) UNDERGROUND VAULTS
             \   [ TEMPLE ] ----+-------- stair inside the storage hall,
              \      |                    runs under the courtyard
               old stone gate
                     |
        (1) VILLAGE BAZAAR AND TEA LANE
                     |   road bends twice; houses and banyans hide the temple
              jeep arrival road (south)
```
- **Roads:** the arrival road (spline, south), the temple ring road (around the compound wall), the jungle road (north, jeep only at speed), a cart track to the Sun Wall, a footpath of steps down to the stepwell.
- **Shortcuts (open after first visit):** vault back stair comes up beside the stepwell; a gap in the compound wall links courtyard and Sun Wall plaza; the ghat path links stepwell and ruins.
- **Landmarks:** the 62 m temple tower is visible from zones 2, 4 (as a silhouette in lightning), 5 and 6. It is hidden on the arrival road by two-storey houses and a banyan row until the player walks through the old stone gate. Other landmarks: the bazaar clock post, the stepwell's twin pillars, the chariot wheel (6 m), the ruined arch with torches.
- **Fast travel:** from the map (M), only to zone entrances already visited, only outside fights. Each jump costs 2 minutes on the Dawn Clock.
- **Guidance without markers:** light (the next goal is always the brightest or warmest thing in view), sound (bell, water, boat engine), and people (villagers look or point toward trouble).

## Rules that apply in every zone
- E only grabs, turns, pulls, pushes or places. Nothing is "examined" for an answer.
- Soft fails only: every puzzle has a reset lever or resets itself; a lost fight restarts from the checkpoint before it with no loading screen.
- Checkpoints: on entering a zone and just before each fight.
- H gives hint 1 (where to look), hint 2 (what the mechanism does), hint 3 (the step). Each hint costs score and 1 minute.

---
## Zone 1 — Village Bazaar and Tea Lane
- **Palette and light:** warm ochre walls, terracotta roofs, turmeric and indigo cloth. Late afternoon sun low from the west, turning to dusk during the tail. Practical lights come on one by one (tea-stall lanterns, shop bulbs).
- **Ambience and music:** crowd murmur, frying, a radio, bicycle bells. Calm sitar and tabla loop; a tension layer fades in during the tail.
- **Layout:** the arrival road ends in a T-shaped bazaar street (60 m) with a narrow tea lane (40 m) and three back alleys that loop. 14 to 18 villagers.
- **Entry:** by jeep after the intro. The player parks at the banyan.
- **Puzzle type: observation and tailing.**
  1. *Observe:* the photo in the tea stall shows the five treasures. Somewhere in the crowd a man's cloth bag rings faintly when he is bumped. The player finds him by ear and by a detail (red thread on his bag, the same thread as the cut bell thread).
  2. *Tail:* follow him without a marker. He checks behind him at set points; the player must be behind a stall, a cart or in a group of villagers when he turns. A suspicion meter is shown only as his body language (he slows, looks, then walks faster).
  3. *Aha:* he doubles back through an alley to lose a tail. The player who noticed the alleys loop can cut through the dyer's yard and be waiting.
  4. *Corner him* in the dead-end yard.
- **Must observe:** the bell sound, the red thread, the looping alleys.
- **Soft fail:** if he spots the player three times he runs; the chase then goes through the same alleys and ends in the same yard, with a lower stealth score.
- **Enemies:** the courier (quick, light) and one helper who steps out of a doorway. Short street fight, 30 to 45 s.
- **Item logic:** treasure 1, the Dawn Bell, is in his bag with the handwritten list (twist evidence 1).
- **Set-piece:** dusk falls and the lane lanterns light up as the tail starts.
- **Hints:** 1 "Listen near the cloth stalls." 2 "One bag rings when it is knocked." 3 "Follow the man with the red thread; stay behind stalls when he turns."

## Zone 2 — Temple Gate and Courtyard
- **Palette and light:** honey sandstone, saffron flags, deep blue shadow. Low gold key light through the gate, long shadows, lamp glow in the storage hall.
- **Ambience and music:** doves, a distant conch, wind in flags. Slow drone with a single flute.
- **Layout:** old stone gate, 40 m courtyard, lamp row, the storage hall on the east side, the priest's room. The reveal of the tower happens as the player walks through the gate.
- **Entry:** on foot from the bazaar through the gate.
- **Puzzle type: reconstruct the break-in with physical evidence.**
  1. *Fit pieces:* three pieces of the broken lamp lie in different places. Carrying them back to the lamp stand and fitting them shows where it fell from and which way the oil ran.
  2. *Cast shadows:* a movable standing lamp. Placed on the oil trail, its light throws the shadow of the storeroom grille onto the wall; the shadow lines match scratch marks only when the lamp stands where the robber's lamp stood. That spot faces a loose flagstone.
  3. *Read marks:* two boot sizes go out; one sandal print goes in and comes back. The lock plate turns in the player's hands (grab and rotate): the scratches are on the inside.
  4. *Aha:* the loose flagstone lifts to show the rope the lookout used, and the rope leads to the bell tower ledge where he is still hiding.
- **Must observe:** oil direction, shadow match, inside scratches, lamp oil on the key hook (twist evidence 2).
- **Soft fail:** pieces can be dropped and picked up again; the lamp can be moved any number of times.
- **Enemies:** the lookout drops from the ledge. Mini fight, about 30 s.
- **Item logic:** treasure 2, the Palm-Leaf Chronicle, is strapped to his back. The flagstone also shows a stair mark pointing to the vaults.
- **Set-piece:** the first sunlight hits the tower top as the fight ends.
- **Hints:** 1 "The lamp broke somewhere else." 2 "Light from the right spot makes the marks line up." 3 "Stand the lamp on the oil stain by the grille, then lift the flagstone it points at."

## Zone 3 — Underground Storage Vaults
- **Palette and light:** black-brown laterite, green damp, one cold blue shaft from a roof grate. The player's lantern is the key light (warm, 8 m, battery 4 minutes, recharged at oil niches).
- **Ambience and music:** drips, distant footsteps, a low pulse. Silence when hiding.
- **Layout:** stair from the storage hall, a pillared hall with three side vaults, a gear room, the disc door, the treasure vault, the back stair up to the stepwell.
- **Entry:** the stair under the flagstone hall. Madhav hands over the lantern.
- **Puzzle type: gears, rotating stone discs, pressure plates, stealth.**
  1. *Stealth:* two guards patrol the pillared hall on loops. Lantern off and crouching makes the player hard to see; light on a guard alerts him.
  2. *Pressure plates:* the gear room door needs two plates held. One takes a pushable stone block; the other must be held by a guard's patrol path, so the player times the door to his walk.
  3. *Gears:* three loose gears of different sizes go on four pegs; only one arrangement links the crank to the disc door.
  4. *Aha, discs:* three stone discs with carved rings. The right alignment is not written anywhere; it is the pattern the roof-grate light draws on the floor at the centre of the hall, which the player walked past.
- **Must observe:** patrol timing, the light pattern on the floor, the ledger on the guard's table (twist evidence 3).
- **Soft fail:** being seen starts the fight early in the pillared hall (harder ground, both guards at once); discs and gears have a reset lever.
- **Enemies:** two guards, one heavy and one brawler. Heavier fight, 60 to 90 s, in the treasure vault.
- **Item logic:** treasure 3, the Sun Rosette, is in a crate the heavy guard is sitting on.
- **Set-piece:** the disc door grinds open and dust falls through the light shaft.
- **Hints:** 1 "The guards walk the same path every time." 2 "The light from the roof draws a shape." 3 "Turn the discs until their rings match the three arcs of light on the hall floor."

## Zone 4 — Stepwell and Monsoon Shrine
- **Palette and light:** blue-black night, wet grey stone, silver highlights. Moon key light, lightning flashes, three shrine lamps under shelter. Wet reflective floor.
- **Ambience and music:** heavy rain, thunder, running water, frogs. Low strings; percussion enters in the fight.
- **Layout:** a square stepwell 30 m across and 12 m deep, four flights of steps, three stone platforms at different heights, two sluice gates feeding it from a channel, a drain wheel, and a hidden chamber behind a carved panel at the second level.
- **Entry:** steps down from the east path, or the vault back stair.
- **Puzzle type: water flow.**
  1. Open the upper sluice (turn a valve wheel): water rises to level 1 and floats a wooden raft to the first platform.
  2. The second sluice is jammed by a beam on the far platform; ride the raft, pull the beam.
  3. *Aha:* with both sluices open the water rises too far and covers the hidden chamber door. The player must open the drain wheel half way so the level settles exactly at the carved water-line mark on the wall, which is the only level where the raft meets the chamber ledge.
  4. Push the carved panel in.
- **Must observe:** the carved water-line marks, the beam, how each wheel changes the level.
- **Soft fail:** the drain wheel always brings the water back to empty; falling in just means swimming to the steps.
- **Slippery footing:** on wet stone, running turns have a slide; sprint attacks can overshoot. Sheltered areas are dry.
- **Enemies:** the knife man (fast, dodges) on the wet second-level terrace with one helper. 45 to 75 s.
- **Item logic:** treasure 4, the Monsoon Vessel, sits in the hidden chamber; the knife man is waiting inside. The key ring and oil cloth lie on the chamber floor (twist evidence 4).
- **Set-piece:** lightning shows the temple tower in silhouette at the moment the chamber opens.
- **Hints:** 1 "The wall has old water marks." 2 "Too much water hides the door." 3 "Open both sluices, then turn the drain wheel until the water sits on the middle mark."

## Zone 5 — Sun Wall and Chariot-Wheel Plaza
- **Palette and light:** bleached sandstone, white dust, hard blue sky, black shadows. Midday sun almost overhead; the only zone with no fog.
- **Ambience and music:** cicadas, hot wind, creaking wood. Sparse percussion; brass stabs in the boss fight.
- **Layout:** a 50 m paved plaza, a 6 m stone chariot wheel on an axle in the centre, a 12 m carved wall on the north side with four sun sockets, three bronze mirrors on pivots, a sundial pillar.
- **Entry:** cart track from the west, or the gap in the compound wall.
- **Puzzle type: mirror, light and wheel with timing.**
  1. Turn the three mirrors to carry sunlight from the open south side to the wall. Each socket lights when a beam rests on it.
  2. The wheel's spokes block the beams. Push the wheel (grab the rim and walk) so the gaps line up.
  3. *Aha:* the fourth socket can only be reached by a beam that passes through the wheel hub, and the hub hole only lines up for a few seconds as the wheel rolls back under its own weight. The player has to set the mirror first, then release the wheel and let the timing do it. The sundial's shadow shows the same moment.
  4. The wall's hidden door slides open.
- **Must observe:** where each beam lands, the hub hole, the sundial shadow.
- **Soft fail:** the wheel always rolls back to rest; mirrors hold their angle.
- **Enemies:** Kishore, mini boss, two phases (staff reach, then fast and reckless when low). He uses the wheel as cover. 60 to 90 s.
- **Item logic:** behind the door is the finial's empty case and a receipt with a boat drawing: the fifth treasure is already on the jungle road.
- **Set-piece:** four beams cross the plaza and the wall splits.
- **Hints:** 1 "Light has to reach four carvings." 2 "One beam must go through the middle of the wheel." 3 "Aim the last mirror at the hub, pull the wheel back and let go."

## Zone 6 — Jungle Road and Ruins
- **Palette and light:** deep green, orange torchlight, violet dusk mist. Low sun through trees as light shafts; torches on the ruins.
- **Ambience and music:** insects, a far boat engine that gets louder, engine and gravel in the chase. Full combat theme, then one flute.
- **Layout:** 600 m of winding jungle road with two fallen-log chicanes and a ford, ending at a ruined arch and a walled ruin court (35 m) above the river ghat.
- **Entry:** by jeep from the temple ring road.
- **Sequence:**
  1. *Jeep chase:* Bhaskar's cart truck ahead. The road has soft hazards (logs slow the jeep, the ford slides it). Damage to the jeep carries into the score. Falling behind does not fail the chase; it costs time on the Dawn Clock.
  2. *Ruin court, wave 1:* three robber variants (brawler, knife, brawler); at most two attack at once.
  3. *The inside man:* if the player named Madhav in the journal he is found at the arch and gives up after a short exchange; if not, he attacks from behind during wave 2 and must be put down first.
  4. *Wave 2:* a heavy and a knife man.
  5. *Bhaskar,* three phases: sword at range, torch swings that set dry grass alight (hazard strips), then bare-handed and desperate at the ghat steps.
- **Puzzle beat (light):** the court gate is barred from inside; the player pulls a torch bracket that is really a counterweight lever (the only bracket with rope wear).
- **Soft fail:** each wave is its own checkpoint.
- **Item logic:** treasure 5, the Chariot Finial, is in Bhaskar's satchel and drops on his defeat.
- **Set-piece:** the boat lamp leaves the ghat without its cargo. Cut to the courtyard ending.
- **Hints:** 1 "One torch bracket is worn." 2 "Fight with your back to a wall." 3 "Parry the torch swing, then finish him at the steps."

---
## Per-zone lighting profiles (Volume settings to build)
| Zone | Exposure | White balance | Contrast and saturation | Bloom | Fog | Extra |
|---|---|---|---|---|---|---|
| 1 Bazaar | 0 | warm +15 | medium, +10 | medium | thin dust | lanterns as practicals |
| 2 Courtyard | +0.3 | warm +25 | medium, +5 | medium | light morning haze | long shadows |
| 3 Vaults | -1.5 | cool -10 | high, -15 | low | thick, short | lantern key light, vignette strong |
| 4 Stepwell | -1.0 | cool -25 | high, -20 | high on lamps | rain haze | wet floor reflections, lightning |
| 5 Sun Wall | +0.7 | neutral | very high, -5 | low | none | hard shadows |
| 6 Jungle | -0.3 | warm +10 with violet shadows | medium, +5 | medium | height fog, shafts | torches |

## Build order
Zone 2 (the temple already stands), then 1, 3, 5, 4, 6. Each zone is built as a section of one world scene with its own Volume and its own puzzle, tested with a focused play-bot before the next is started.
