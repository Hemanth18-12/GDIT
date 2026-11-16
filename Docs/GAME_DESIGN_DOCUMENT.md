# Legacy of the Temple: game design document

Detailed sources: `Docs/STORY_V3.md` (story, all spoken lines), `Docs/LEVEL_DESIGN_V3.md` (zones and puzzles), `Docs/DECISIONS.md` (every change from the plan and why).

## 1. Concept
A compact third-person thriller. Very little talk, a ticking clock, physical puzzles instead of "press E to get the answer", short heavy fights, and a jeep. About 20 to 30 minutes from New Game to the ending.

**Pillars**
1. The world tells the story: objects, sound, light, a handful of short lines (29 spoken lines in the whole game, never more than two in a row).
2. Every answer is earned by doing something physical: carry, fit, turn, push, pull, place.
3. No markers or arrows by default. Light, sound and people show the way; H gives three hints when stuck.
4. Every place ends with a short, readable fight.

## 2. Story in one paragraph
The night before the festival, five treasures vanish from the temple storeroom. The lock was not forced: it was opened from the inside. Aryan follows the robbers through the bazaar, the storeroom, the vaults, the stepwell and the Sun Wall, recovering one treasure at each, while four small pieces of evidence (a list in neat handwriting, oil on the key hook, a ledger signed "M.", a dropped key ring) point at Madhav, the friendly key-keeper who appears in every place. On the river road Aryan chases the leader, Bhaskar, to the ruins and takes back the last treasure. Madhav gives himself up: "I only opened a door."

## 3. Cast
Investigator Aryan (player), Pujari Ananda, Meera, Bhaskar (leader, Robber 1), Kishore (Robber 2), the courier, the lookout, guards and knife men (recoloured and resized robbers), Madhav (the existing male villager model, set apart by an indigo kurta and red turban).

## 4. Structure
| # | Place | Puzzle | Fight | Treasure / evidence |
|---|---|---|---|---|
| 1 | Bazaar and tea lane | Tail a courier by sound; stay unseen when he looks back | Courier and helper in a yard | Dawn Bell; the list |
| 2 | Storeroom by the temple | Fit pot shards, light the lantern, stand it on the oil stain, turn the lock plate, lift the flagstone | The lookout | Palm-Leaf Chronicle; lock scratched inside |
| 3 | Vault hall | Push blocks onto two plates (reset lever), turn three discs to match the light pattern on the floor | Two guards (heavy and brawler) | Sun Rosette; ledger signed "M." |
| 4 | Stepwell tank (rain, night) | Open two sluices, ride the raft, drain to the carved mark, push the panel | Knife man and helper | Monsoon Vessel; key ring |
| 5 | Sun Wall plaza (midday) | Four mirrors carry two beams; push the chariot wheel so a spoke stops blocking one | Kishore (two phases) | Empty case and boat receipt |
| 6 | River road and ruins (dusk) | Jeep chase | Two waves, then Bhaskar (three phases) | Chariot Finial |

**Soft failure only:** every puzzle can be reset; a lost fight restarts on the spot; a lost tail or chase never ends the game.

## 5. Systems
- **Puzzle parts:** rotating discs, mirrors and light beams, levers, pressure plates, pushable blocks, valves and water level, sound locks, carry and place, shadow matching. Saved, resettable, three hint tiers each.
- **Evidence board (B):** only what was actually found; the inside man can be named once, with three of four pieces.
- **Combat:** light combo, heavy, dodge roll, block and perfect parry, finisher in slow motion, at most two attackers at once, telegraphed blows, enemies that guard after three hits, bosses with phases.
- **Dawn Clock:** five chapter deadlines up to dawn at 05:45; the ending depends on it.
- **Score and grades** per place and overall; local high-score table.
- **Faces:** lip sync and blinking where the models allow it (Meera: visemes; Aryan: jaw bone).

## 6. Look and sound per place
| Place | Light | Sound |
|---|---|---|
| Bazaar | Late afternoon, warm | Crowd, the courier's bell |
| Storeroom | Dawn gold, lantern light | Doves, flags |
| Vaults | Near dark, one cold beam from the roof | Drips, silence |
| Stepwell | Night, rain, lamps | Heavy rain |
| Sun Wall | Hard midday sun, no fog | Wind |
| River road | Dusk mist, torches | Jungle, engine |

## 7. Out of scope (honest)
- Robber models are the fantasy sculpts from the originals, not village thieves (decision D-04).
- No kick, grab or knockdown animations (the clips do not exist in the project).
- The places are built from textured blocks plus hero props, not fully modelled environments.
