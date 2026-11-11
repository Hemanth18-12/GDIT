# Blocked items and what you must do by hand

## 1. Robber models (BLOCKER for final characters and combat animation)

The read-only originals in `Heritage Mystery Project/` (including `assets.blend`) contain these characters only:

| Original | What it is | Used as |
|---|---|---|
| `noir-detective` (meshes `M_MED_Gumshoe*`) | Man in a hat and long coat | **Aryan, the Investigator. Yes: the hat-and-coat hero is the Investigator.** |
| `24568fb5-...` (elderly priest) | Elderly priest | Pujari Ananda |
| `indian-woman-in-saree` (rig `AvatarRoot`) | Woman in a saree | Meera |
| `agent-arnold-matrix` | Man in a suit (static sculpt, about 1.5 million polygons) | Not used |
| `corrupted-king-in-black-armor-with-bone-crown` | Armoured king sculpt (about 2 million polygons) | Was Robber 1 |
| `corrupted-archangel-six-wings-boss` | Winged boss sculpt (about 2 million polygons) | Was Robber 2 |

**There is no red-scarf / dark-vest man (Bhaskar) and no black-hood / cross-strap man (Kishore) in the originals.**
The two "robber" files are fantasy sculpts with no skeleton, so they cannot walk, fight or be retargeted.

**What the game uses now (clearly flagged stand-ins):** `Robber1.prefab` and `Robber2.prefab` are rigged COPIES of
the detective model, tinted, with the hat and glasses removed. Bhaskar has a red scarf, a blade and a satchel;
Kishore has a black hood and a staff (simple shapes). They use the real walk and run clips. The prefabs carry
`NpcAnimDriver.standIn = true`. Rebuild them with `LegacyOfTemples.World.Editor.RobberStandInTools.Build`.
Your original files were not changed.

**What you must do**
1. Decide which models are Bhaskar and Kishore. Options: (a) give me two human models you have the right to use
   (FBX or GLB, T-pose or A-pose); (b) tell me to dress up `agent-arnold-matrix` as one of them (it still needs rigging);
   (c) keep the stand-ins.
2. For each human model: upload it to mixamo.com, let the auto-rigger place the skeleton, download
   **FBX for Unity, With Skin, T-pose**, and put it in `Assets/_Project/Models/Characters/` as `Bhaskar.fbx` / `Kishore.fbx`.

## 2. Combat animation clips (BLOCKER for attack, block, dodge and hit animations)

`Assets/_Project/Animations/Clips/` has only `Hero/Idle, Walking, Running, Crouch, Jump`. No combat clips were found.
The fight is fully playable, but attacks are shown by the glint, sound, sparks and movement only. No animation was faked.

Download these from mixamo.com as **FBX for Unity, Without Skin, 30 fps, "In Place" ticked where offered**, into
`Assets/_Project/Animations/Clips/Combat/` with exactly these file names (Mixamo search term in brackets; pick the closest match):

| File name | Search term | Used for |
|---|---|---|
| Fight_Idle.fbx | Fighting Idle | Aryan combat stance |
| Jab_Cross.fbx | Jab Cross | Aryan light attack 1 and 2 |
| Hook_Punch.fbx | Hook Punch | Aryan light attack 3 |
| Cross_Punch.fbx | Cross Punch | Aryan heavy attack |
| Body_Block.fbx | Body Block | Aryan block |
| Dodge_Left.fbx / Dodge_Right.fbx / Dodge_Back.fbx | Dodging Left / Dodging Right / Standing Dodge Backward | Aryan dodge |
| Head_Hit.fbx / Stomach_Hit.fbx | Head Hit / Stomach Hit | Aryan hit reactions |
| Knocked_Down.fbx / Getting_Up.fbx | Knocked Down / Getting Up | Aryan defeat and retry |
| Throw_Object.fbx | Throw Object | Pebble, Kishore's throw |
| Crouched_Walking.fbx | Crouched Walking | Stealth walk |
| Pulling_A_Rope.fbx | Pulling A Rope | Grapple and rope |
| Sword_Idle.fbx | Sword And Shield Idle | Bhaskar stance |
| Sword_Slash.fbx | Sword And Shield Slash | Bhaskar quick and double slash |
| Great_Sword_Slash.fbx | Great Sword Slash | Bhaskar wide swing |
| Sword_Thrust.fbx | Sword And Shield Attack | Bhaskar lunge |
| Sword_Kick.fbx | Sword And Shield Kick | Bhaskar kick |
| Sword_Impact.fbx | Sword And Shield Impact | Bhaskar blocked / hit |
| Stunned.fbx | Stunned | Flash stun |
| Fast_Run.fbx | Fast Run | Bhaskar flees, chase runner |
| Kneeling_Idle.fbx | Kneeling Idle | Bhaskar restrained |
| Nervous_Look.fbx | Nervously Look Around | Kishore idle |
| Melee_Horizontal.fbx | Standing Melee Attack Horizontal | Kishore staff swing |
| Football_Tackle.fbx | Football Tackle | Kishore tackles Bhaskar |
| Surrender.fbx | Surrender | Kishore gives up |
| Talking.fbx | Talking | All NPC conversations |

After they are in the folder, tell me: wiring them into the Animator controllers is one more short session.

## 3. Voices (not a blocker)

All 113 lines play macOS `say` placeholders (`Resources/Voice/<speaker>/<line_id>_tts.wav`). To replace one, record it
and save it as `Resources/Voice/<speaker>/<line_id>.wav` (for example `aryan/c5_ary_04.wav`). The recorded file wins
automatically; no code change. Line ids and tone are in `Docs/STORY_BIBLE_V2.md` and `Tools/story/build_story.py`.

## 4. Loading images (not a blocker)

The four pictures you attached are in `Assets/_Project/UI/Loading/`. To use your own photos, replace the files with
the same names, or drag a picture into a slot in `Assets/_Project/Resources/UI/LoadingImages.asset`, then run
menu **Legacy of the Temple > Sync loading images**. A missing picture shows a dark gradient and logs a warning.

## 5. Sound and music (not a blocker)

The 69 effects, ambience loops and five music stems are synthesised by `Tools/audio/build_audio.py` (own work).
They are placeholders in quality: clear and distinct, but not recorded foley or composed music. To upgrade, replace a
file in `Assets/_Project/Audio/Resources/Audio/` with a CC0 file of the same name and add it to ATTRIBUTION.md.
