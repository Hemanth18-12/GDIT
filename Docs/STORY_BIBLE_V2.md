# LEGACY OF THE TEMPLE — Story Bible v2

Status: PHASE A document. Nothing in the game has changed yet. Supersedes Docs/STORY_SCRIPT.md once approved.

## 1. Premise

At 03:10 AM three heritage objects vanish from a temple's storage hall. The dawn rite is at 05:45 and
cannot happen without them. The thieves are not simply selling: they need all three to open the Sun Wall
at first light. Aryan has until sunrise.

**Fiction notice (shown in the Codex and the credits):** the three objects, the Sun Wall, the rite and
all characters are fictional. They are inspired by craft traditions (brass lamp work, sandstone carving,
palm-leaf manuscripts) and do not depict any real temple's property or ritual.

**Respect rules that the script obeys**
- The shrine figure (Idol_Jagannath.fbx) is background only. Nobody touches, moves, steals or fights near it.
- The final fight happens in the jungle ruins, away from any shrine.
- No gore. Combat is stylised. Bhaskar and Kishore are subdued and handed to the law, never killed.

## 2. The Dawn Clock

- Story time runs from **03:10** to **05:45** (155 story minutes). The HUD shows "Dawn in H:MM".
- The clock runs only during play. It pauses in dialogue, cinematics, the Codex, menus and loading.
- Each chapter has a budget (see LEVEL_DESIGN.md). Mistakes cost story minutes.
- Chapters 1 to 4: running out of budget means retrying from the last checkpoint with the budget restored.
- Chapter 5: the clock can run past 05:45 during the last pursuit. That decides the ending, not a retry.

| Chapter | Starts | Budget | Must end by |
|---|---|---|---|
| 1 Courtyard | 03:10 | 25 min | 03:35 |
| 2 Storage Hall | 03:35 | 30 min | 04:05 |
| 3 Outer Shrine | 04:05 | 25 min | 04:30 |
| 4 Sun Wall | 04:30 | 35 min | 05:05 |
| 5 Jungle Ruins | 05:05 | 40 min | 05:45 |

Time saved in a chapter carries forward as a bonus (maximum 5 minutes per chapter).

## 3. Cast and voice

| Id | Character | Who they are | Voice |
|---|---|---|---|
| aryan | Aryan | Investigator. Calm, dry wit, quiet authority. | Calm baritone; lower and firmer under pressure; powerful and projected in the final fight. |
| pujari | Pujari Ananda | Elderly priest. Warm, anxious, dignified. | Soft gravel, unhurried. |
| meera | Meera | Young, sharp, protective. Recognised her brother's voice and hides it. | Quick, clipped when lying, warm when honest. |
| bhaskar | Bhaskar | Cold, charismatic, ruthless. Red scarf, curved blade, heavy satchel with the vessel and the rosette. | Quiet menace. Rarely raises his voice until phase 3. |
| kishore | Kishore | Meera's elder brother. Porter, token No. 17, dismissed last monsoon, in debt. Carries the manuscript. | Nervous, breathy, guilt in every line. |
| scroll | Scroll / Codex | Narration for the scroll and Codex. | Neutral, old, measured. |

## 4. Flags and consequences

| Flag | Range | Set by | Used for |
|---|---|---|---|
| trust_meera | −2 to +3, starts 0 | Choices C1-A, C2-A, C3-A | **≥ 2: Kishore turns on Bhaskar in the fight. ≤ 1: he fights beside Bhaskar.** |
| has_message | yes / no | Choice C3-A (carry Meera's message) | Unlocks the call-out to Kishore in Chapter 5 (+1 trust if used). |
| porter_path | yes / no | trust_meera ≥ 1 at the end of Chapter 2 | Meera chalks the porters' path: one extra hiding spot and checkpoint in Chapter 5. |
| chase_hint | yes / no | Choice C1-A not hostile | Meera points the way: the runner is marked for the first 5 seconds of the chase. |

## 5. Script

Every line is at most 120 characters. The id is the voice-line id (see AUDIO_BIBLE.md). Tier is the
vocal effort: calm, firm or shout.

### Chapter 0 — Cold Open (cinematic, about 60 s, Storage Hall, midnight)

Shots: a hand lifts a cloth; three stands; Kishore's trembling fingers on the manuscript; Bhaskar's
scarf in lamplight; a lamp tips; black; one bell.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c0_kis_01 | Kishore | calm | We take only what we were told, Bhaskar. Nothing more. |
| c0_bha_01 | Bhaskar | calm | Quiet. The wall opens at first light, and we will not be late. |
| c0_kis_02 | Kishore | calm | These stands have never been empty. Not once in my life. |
| c0_bha_02 | Bhaskar | firm | Then tonight you learn something new. Move. |

Title card: **LEGACY OF THE TEMPLE** — "3:10 AM".

### Chapter 1 — Courtyard, "3:10 AM" (investigate, interrogate, short chase)

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c1_puj_01 | Pujari | calm | Aryan-ji, they came before the lamps burned low. Three pieces are gone. |
| c1_puj_02 | Pujari | calm | The Surya Pradeepa, the Padma Shila, and the Tala Patra. All three. |
| c1_ary_01 | Aryan | calm | Tell me what only you would notice. |
| c1_puj_03 | Pujari | calm | The key was in its place. The lock was not forced from outside. |
| c1_ary_02 | Aryan | calm | Then someone gave them the way in. |
| c1_puj_04 | Pujari | firm | The dawn rite is at 5:45. Without them, I have nothing to light and nothing to read. |
| c1_ary_03 | Aryan | calm | Then we have until sunrise. Who was awake? |
| c1_puj_05 | Pujari | calm | Meera. She draws water before anyone else is up. |

Meera, arms folded, by the well.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c1_mee_01 | Meera | firm | I saw two men. A red scarf and a black hood. |
| c1_ary_04 | Aryan | calm | You looked longer at one of them. |
| c1_mee_02 | Meera | firm | It was dark. |

**CHOICE C1-A (trust check)**

| Option | Lines | Effect |
|---|---|---|
| 1. Gentle | c1_ary_05 Aryan: "Whatever you saw, you are safe telling me. When you are ready." / c1_mee_03 Meera: "...They went toward the east wall. That is all." | trust +1, chase_hint yes |
| 2. Hard | c1_ary_06 Aryan: "Dark enough to hide a lie. I do not have time for one." / c1_mee_04 Meera: "Then stop wasting it on me." | trust −1, chase_hint no |
| 3. Silent | c1_ary_07 Aryan: "Mm." (he writes one word in his notebook: *voice*) / c1_mee_05 Meera: "What did you write?" | trust 0, chase_hint yes, Journal note |

A shadow crosses the east wall. Chase.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c1_ary_08 | Aryan | firm | There. On the wall. Stay here, both of you. |
| c1_ary_09 | Aryan | shout | Stop! I only want to talk! |
| c1_ary_10 | Aryan | calm | A porter's token. Tin. Number seventeen. |
| c1_ary_11 | Aryan | calm | He ran like a man who knows every stone here. |

### Chapter 2 — Storage Hall, "The Empty Stands" (dark search, relight the lamps, deduction)

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c2_ary_01 | Aryan | calm | Every lamp is out. Somebody wanted this room blind. |
| c2_puj_01 | Pujari | calm | The lamps are lit in an order, Aryan-ji. East first, as the sun does. |
| c2_ary_02 | Aryan | calm | A key cut from wax. Fresh filings on the floor. |
| c2_ary_03 | Aryan | calm | An oily handprint, low on the stand. He knelt. He was careful. |
| c2_ary_04 | Aryan | calm | A chalk mark on the third pillar. A sign for someone who came later. |
| c2_ary_05 | Aryan | firm | They did not break the lock. They had it opened. |
| c2_puj_02 | Pujari | firm | Number seventeen. Kishore. Our porter until last monsoon. |
| c2_puj_03 | Pujari | calm | I dismissed him myself. I thought it was the kinder thing. |
| c2_mee_01 | Meera | firm | Don't hurt him. He is my brother. |
| c2_ary_06 | Aryan | calm | You knew his voice in the dark. |
| c2_mee_02 | Meera | calm | I knew it before I saw the scarf. I have been paying his debt since the rains. |

**CHOICE C2-A**

| Option | Lines | Effect |
|---|---|---|
| 1. Trust her | c2_ary_07 Aryan: "Then help me reach him before Bhaskar does something worse." / c2_mee_03 Meera: "The porters have a path. I will mark it in chalk." | trust +1 |
| 2. Reprimand | c2_ary_08 Aryan: "You cost me an hour I did not have. Stay with the Pujari." / c2_mee_04 Meera: "And you will cost me a brother." | trust −1, Meera does not appear in Chapter 3 |

Deduction board (player matches clue to conclusion): copied key → inside help; handprint → careful
carrier; chalk mark → a guide for the second man. Solving it plays:

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c2_ary_09 | Aryan | calm | Kishore opened the door. Bhaskar brought the reason. |

### Chapter 3 — Outer Shrine, moonlit, "Tracks in the Dark" (forensics, tripwire bell traps)

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c3_ary_01 | Aryan | calm | Bells on a thread, ankle height. They expected to be followed. |
| c3_ary_02 | Aryan | calm | Two boot sizes. One print pressed deep. He is carrying weight. |
| c3_ary_03 | Aryan | calm | Red silk on the doorframe. He was in a hurry, or he did not care. |
| c3_ary_04 | Aryan | calm | A broken lamp. Still warm at the wick. |
| c3_ary_05 | Aryan | calm | "Ford at moonset. Sun Wall at first light." |
| c3_ary_06 | Aryan | firm | They are not selling. They are opening something. |
| c3_puj_01 | Pujari | firm | The Sun Wall. It needs all three. |
| c3_puj_02 | Pujari | calm | Behind it lie our records. Every family that ever served this temple. |
| c3_ary_07 | Aryan | calm | And things a buyer would pay a great deal for. |

If a bell trap rings: c3_ary_08 Aryan (firm): "That was heard. Move." (costs 3 story minutes).

**CHOICE C3-A** (only if Meera is present, i.e. she was not reprimanded)

| Option | Lines | Effect |
|---|---|---|
| 1. Carry her message | c3_mee_01 Meera: "Tell him the debt is paid. I sold the bangles. He owes Bhaskar nothing." / c3_ary_09 Aryan: "I will tell him. In your words." | trust +1, has_message yes |
| 2. Refuse | c3_ary_10 Aryan: "This stopped being a family matter at 3:10. Go home, Meera." / c3_mee_02 Meera: "It was never anything else." | trust 0, has_message no |

### Chapter 4 — Sun Wall, "The Wall That Remembers" (timed pressure puzzle)

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c4_puj_01 | Pujari | calm | It is closed again. They tried, and the wall refused them. |
| c4_scr_01 | Scroll | calm | Lotus for the waking. Conch for the call. Wheel for the road. Kalash for the return. |
| c4_scr_02 | Scroll | calm | Then the heart of the wall, and the sun will do the rest. |
| c4_ary_01 | Aryan | calm | The centre socket is empty. The Padma Shila is the key, and Bhaskar has it. |
| c4_puj_02 | Pujari | calm | My grandfather spoke of a hand-wheel, for the years the stone was away for repair. |
| c4_puj_03 | Pujari | firm | Turn the four, and the wheel will turn by hand. |
| c4_ary_02 | Aryan | firm | It resets. Ninety seconds, then it forgets everything I have done. |
| c4_puj_04 | Pujari | firm | Wrong, Aryan-ji! The dust is falling. Begin again. |
| c4_ary_03 | Aryan | calm | The passage is open. Boot prints, two sizes, heading into the trees. |
| c4_ary_04 | Aryan | firm | Dawn in forty minutes. |
| c4_puj_05 | Pujari | calm | Bring them home, Aryan-ji. And bring that foolish boy home too. |

### Chapter 5 — Jungle Ruins, "Before First Light" (stealth, chase, final fight)

Stealth section. Overheard lines play as Aryan gets within range.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c5_bha_01 | Bhaskar | calm | Moonset at the ford. Yes. All three. |
| c5_bha_02 | Bhaskar | calm | No. The wall first. What is behind it is worth ten of these. |
| c5_kis_01 | Kishore | calm | Bhaskar, the manuscript is the village's name. |
| c5_bha_03 | Bhaskar | calm | Names do not pay debts. |
| c5_kis_02 | Kishore | calm | You said nobody would be hurt. You said one night. |
| c5_bha_04 | Bhaskar | firm | I said what a frightened man needed to hear. Walk. |

Spotted, or the stealth route ends: chase to the stepped courtyard with braziers.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c5_bha_05 | Bhaskar | firm | The investigator. Persistent. I respect that. It will not save you. |
| c5_ary_01 | Aryan | firm | Put the satchel down, Bhaskar. This ends quietly or it ends badly. |
| c5_bha_06 | Bhaskar | calm | You will starve with a clean conscience. |

**CHOICE C5-A (only if has_message; timed, 6 seconds, during phase 1)**

| Option | Lines | Effect |
|---|---|---|
| 1. Deliver it | c5_ary_02 Aryan (shout): "Kishore! Meera sold the bangles. The debt is paid. You owe him nothing!" / c5_kis_03 Kishore: "She... she sold them?" | trust +1 |
| 2. Threaten | c5_ary_03 Aryan (shout): "Kishore! Walk away now or you go down with him!" / c5_kis_04 Kishore: "I cannot go back. Not now." | trust −1 |

Final fight (details in COMBAT_DESIGN.md).

| Id | Speaker | Tier | Line | When |
|---|---|---|---|---|
| c5_ary_04 | Aryan | shout | Enough, Bhaskar! | Phase 1 start |
| c5_ary_05 | Aryan | shout | It is not yours to sell! | Phase 2 start |
| c5_bha_07 | Bhaskar | firm | Everything is for sale. You only have to find the hour. | Phase 2 |
| c5_ary_06 | Aryan | shout | You cannot carry what it means! | Phase 3 start |
| c5_bha_08 | Bhaskar | shout | Then I will carry what it weighs! | Phase 3, he runs |

**Branch: trust_meera ≥ 2 — Kishore turns (end of phase 1)**

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c5_kis_05 | Kishore | shout | Bhaskar, stop. I cannot. |
| c5_kis_06 | Kishore | shout | Aryan-ji! Catch! |
| c5_bha_09 | Bhaskar | firm | Porter. You were always only a porter. |

He throws the manuscript to Aryan and tackles Bhaskar, who shakes him off (Kishore is winded, out of
the fight, unharmed).

**Branch: trust_meera ≤ 1 — two against one**

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c5_kis_07 | Kishore | firm | Forgive me. I have nowhere else to stand. |
| c5_ary_07 | Aryan | firm | Then stand down, before I have to make you. |
| c5_kis_08 | Kishore | calm | Enough... enough. I am done. | 

(c5_kis_08 plays when Kishore is subdued in phase 2. He drops the manuscript.)

After the fight: Bhaskar is restrained with his own scarf. The satchel falls open: the vessel and the rosette.

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c5_kis_09 | Kishore | calm | Take it. Tell Meera I am sorry. |
| c5_ary_08 | Aryan | calm | Tell her yourself. |
| c5_bha_10 | Bhaskar | calm | There will be another night, investigator. There is always a buyer. |
| c5_ary_09 | Aryan | calm | And there will always be someone who says no. |

### Chapter 6 — Ending (Courtyard, first light)

| Id | Speaker | Tier | Line |
|---|---|---|---|
| c6_puj_01 | Pujari | calm | You kept your word, Aryan-ji. All three, and before the sun. |
| c6_puj_02 | Pujari | calm | (LATE DAWN) The sun is up without us. We will light it late, but we will light it. |
| c6_mee_01 | Meera | calm | You came back. I did not think you would come back. |
| c6_kis_10 | Kishore | calm | I carried it out. Let me be the one to carry it in. |
| c6_ary_01 | Aryan | calm | He will face the law, and your word will matter. |
| c6_ary_02 | Aryan | calm | (if Kishore turned) So will mine. I saw what he did when it counted. |
| c6_puj_03 | Pujari | calm | The flame goes from house to house. It always has. It will today. |

The Pujari lights the Surya Pradeepa as the first ray hits the temple. Meera holds Kishore's hand.

Closing text: **"Every temple holds a story. Every artifact carries a memory. Protecting heritage means
protecting our history."**

Then **MISSION COMPLETED** with time, evidence found, hints used, trust, and the ending name.

| Ending | Condition | Final image |
|---|---|---|
| DAWN LIGHT | All three recovered by 05:45 and Kishore turned | The lamp is lit as the first ray lands. Meera and Kishore together. |
| LATE DAWN | All three recovered, but after 05:45, or Kishore fought to the end | The rite begins in full daylight. Kishore is led away; Meera watches. |
| LOST DAWN | Bhaskar reaches the ford (final pursuit failed 3 times, or the player gives up) | The Pujari alone before three empty stands. "Retry the final chapter" is offered. |

## 6. Fight barks (short, repeatable, cooldown 6 s)

| Id | Speaker | Line | Trigger |
|---|---|---|---|
| bk_ary_01 | Aryan | Drop the blade! | Successful counter |
| bk_ary_02 | Aryan | Behind you! | Kishore turned and Bhaskar targets him |
| bk_ary_03 | Aryan | Not today! | Perfect dodge |
| bk_ary_04 | Aryan | Stay down! | Enemy staggered or knocked down |
| bk_ary_05 | Aryan | (effort grunt, 3 variants) | Heavy attack |
| bk_bha_01 | Bhaskar | You are too late! | Phase start |
| bk_bha_02 | Bhaskar | Come closer! | Player keeps distance for 6 s |
| bk_bha_03 | Bhaskar | One more step! | Before his lunge (also a telegraph) |
| bk_bha_04 | Bhaskar | (pain grunt, 3 variants) | Hit |
| bk_kis_01 | Kishore | I do not want this! | Two-on-one, when he attacks |
| bk_kis_02 | Kishore | (panic breath, 2 variants) | Staggered |

## 7. Codex entries (in-game, unlocked when first mentioned; each ends with the fiction line)

**Surya Pradeepa** — A brass lamp-vessel lit at the first-light rite. Its flame is not kept in the
temple: it is carried from household to household, so every home's morning lamp is lit from the same
fire. Losing it means a village that wakes in separate darkness. *Fictional object, inspired by
traditional brass lamp craft.*

**Padma Shila** — A carved sandstone rosette, the temple's foundation emblem, set down before the
first wall was raised. It is also the centre key of the Sun Wall. *Fictional object, inspired by
sandstone carving traditions.*

**Tala Patra** — A palm-leaf manuscript holding the words of the dawn rite and the register of the
families who have served the temple. Without it the rite cannot be recited, and the families lose
their written place in the temple's memory. *Fictional object, inspired by palm-leaf manuscript
traditions.*

**The Sun Wall** — A sealed chamber wall that opens only at first light, when four symbols are turned
in order and the centre key is set. Behind it the community keeps generations of records and
ceremonial items. It was built to be opened by the community together, never by one person at night.
*Fictional place.*

**Porter Token No. 17** — A tin token issued to temple porters. Kishore's was never handed back.
*Fictional.*
